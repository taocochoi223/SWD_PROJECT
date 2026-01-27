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

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SWD IoT API",
        Version = "v1",
        Description = "IoT Data Analysis API with JWT Authentication"
    });

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
});

// 1. Đăng ký DB Context
//builder.Services.AddDbContext<IoTFinalDbContext>();
// 1. Đăng ký DB Context (DÙNG ENV Connection String)
builder.Services.AddDbContext<IoTFinalDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.EnableRetryOnFailure()
    );
});


// 2. Đăng ký Repositories (DAL)
builder.Services.AddScoped<ISensorRepository, SensorRepository>();
builder.Services.AddScoped<IAlertRepository, AlertRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<ILogRepository, LogRepository>();

// Infrastructure Repositories (Separated)
builder.Services.AddScoped<ISiteRepository, SiteRepository>();
builder.Services.AddScoped<IHubRepository, HubRepository>();
builder.Services.AddScoped<IOrganizationRepository, OrganizationRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// 3. Đăng ký Services (BLL)
builder.Services.AddScoped<ISensorService, SensorService>();
builder.Services.AddScoped<IAlertService, AlertService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ISystemLogService, SystemLogService>();

// Infrastructure Services (Separated)
builder.Services.AddScoped<ISiteService, SiteService>();
builder.Services.AddScoped<IHubService, HubService>();
builder.Services.AddScoped<IOrganizationService, OrganizationService>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IJwtService, JwtService>();

// 79. Register Hosted Services
builder.Services.AddHostedService<SWD.API.Services.MqttWorkerService>();
builder.Services.AddHostedService<SWD.API.Services.StatusMonitorService>();

// 80. Add SignalR
builder.Services.AddSignalR();

// 81. Configure CORS for SignalR
builder.Services.AddCors(options =>
{
    options.AddPolicy("SignalRPolicy", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "http://localhost:5173",
                "https://swd-fe-project.vercel.app"
              )
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// 4. Cấu hình JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");

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

builder.Services.AddSwaggerGen(c =>
{
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

builder.Services.AddAuthorization();
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// Enable CORS
app.UseCors("SignalRPolicy");

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Map SignalR Hub
app.MapHub<SWD.API.Hubs.SensorHub>("/sensorHub");

app.Run();