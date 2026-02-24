using Microsoft.EntityFrameworkCore;
using SWD.BLL.Interfaces;
using SWD.BLL.Services;
using SWD.DAL.Models;
using SWD.DAL.Repositories.Implementations;
using SWD.DAL.Repositories.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.Cookies; // Thêm cái này
using SWD.API.Services;

var builder = WebApplication.CreateBuilder(args);

// --- 1. CONFIG MÔI TRƯỜNG & BIẾN ENV ---
// Workaround for Render/Docker inotify limits
Environment.SetEnvironmentVariable("DOTNET_USE_POLLING_FILE_WATCHER", "true");

var envVars = builder.Configuration.GetSection("environmentVariables");
if (envVars.Exists())
{
    foreach (var item in envVars.GetChildren())
    {
        Environment.SetEnvironmentVariable(item.Key, item.Value);
    }
}

// --- 2. ADD SERVICES ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Gộp Swagger Config lại làm 1
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SWD IoT API",
        Version = "v1",
        Description = "IoT Data Analysis API with JWT Authentication"
    });

    // Cấu hình nút Authorize nhập Token
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token in the format: {your token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });

    // Load XML Comments (nếu file tồn tại)
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// Config DB Context
builder.Services.AddDbContext<IoTFinalDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.EnableRetryOnFailure()
    );
});

// --- 3. ĐĂNG KÝ REPOSITORIES & SERVICES ---
// Repositories
builder.Services.AddScoped<ISensorRepository, SensorRepository>();
builder.Services.AddScoped<IAlertRepository, AlertRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<ILogRepository, LogRepository>();
builder.Services.AddScoped<ISiteRepository, SiteRepository>();
builder.Services.AddScoped<IHubRepository, HubRepository>();
builder.Services.AddScoped<IOrganizationRepository, OrganizationRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Services
builder.Services.AddScoped<ISensorService, SensorService>();
builder.Services.AddScoped<IAlertService, AlertService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ISystemLogService, SystemLogService>();
builder.Services.AddScoped<ISiteService, SiteService>();
builder.Services.AddScoped<IHubService, HubService>();
builder.Services.AddScoped<IOrganizationService, OrganizationService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IRealtimeService, RealtimeService>();

// Hosted Services (Background Jobs)
builder.Services.AddHostedService<SWD.API.Services.MqttWorkerService>();
builder.Services.AddHostedService<SWD.API.Services.StatusMonitorService>();

// SignalR & HealthCheck
builder.Services.AddSignalR();
builder.Services.AddHealthChecks(); // Quan trọng cho UptimeRobot

// --- 4. CONFIG CORS (Quan Trọng Cho FE) ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("MyCorsPolicy", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "http://localhost:5173",
                "https://swd-fe-project.vercel.app"
                // Thêm domain FE của bạn vào đây nếu có thay đổi
              )
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // BẮT BUỘC ĐỂ LOGIN HOẠT ĐỘNG
    });
});

// --- 5. AUTHENTICATION & COOKIE POLICY ---
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];

if (string.IsNullOrEmpty(secretKey))
{
    throw new InvalidOperationException("JWT SecretKey is missing in appsettings.json");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// !!! KHẮC PHỤC LỖI LOGIN CHÉO TRANG !!!
builder.Services.ConfigureApplicationCookie(options =>
{
    // Cho phép Cookie đi qua domain khác (FE -> BE)
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Render có HTTPS nên dùng Always
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(1);
});

// --- 6. PIPELINE (APP RUN) ---
builder.Services.AddAuthorization();
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// Thứ tự cực kỳ quan trọng:
// 1. Cors -> 2. Authen -> 3. Author -> 4. Controllers
app.UseCors("MyCorsPolicy"); 

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Endpoint cho UptimeRobot (Trả về status 200)
app.MapHealthChecks("/health");

// Map Controllers & Hubs
app.MapControllers();

// Map SignalR Hub
app.MapHub<SWD.API.Hubs.SensorHub>("/sensorHub");

app.Run();

