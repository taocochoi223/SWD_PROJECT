using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SWD.BLL.Interfaces;
using Firebase.Database;
using Firebase.Database.Query;

namespace SWD.API.Services
{
    public class FirebaseService : IFirebaseService
    {
        private readonly ILogger<FirebaseService> _logger;
        private readonly IConfiguration _configuration;
        private readonly FirebaseClient _firebaseClient;

        public FirebaseService(ILogger<FirebaseService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;

            var configPath = _configuration["FirebaseSettings:ConfigFilePath"];
            var dbUrl = _configuration["FirebaseSettings:DatabaseUrl"];

            if (string.IsNullOrEmpty(configPath) || string.IsNullOrEmpty(dbUrl))
            {
                _logger.LogError("Firebase configuration is missing in appsettings.json");
                return;
            }

            // Initialize Firebase Admin SDK if not already initialized
            if (FirebaseApp.DefaultInstance == null)
            {
                FirebaseApp.Create(new AppOptions()
                {
                    Credential = GoogleCredential.FromFile(Path.Combine(AppContext.BaseDirectory, configPath)),
                });
            }

            // Initialize FirebaseClient with a token factory
            _firebaseClient = new FirebaseClient(
                dbUrl,
                new FirebaseOptions
                {
                    AuthTokenAsyncFactory = async () =>
                    {
                        var credential = GoogleCredential.FromFile(Path.Combine(AppContext.BaseDirectory, configPath))
                            .CreateScoped("https://www.googleapis.com/auth/userinfo.email", "https://www.googleapis.com/auth/firebase.database");
                        return await credential.UnderlyingCredential.GetAccessTokenForRequestAsync();
                    }
                });
        }

        public async Task UpdateSensorDataAsync(string chipId, object data)
        {
            try
            {
                await _firebaseClient
                    .Child("Sensors")
                    .Child(chipId)
                    .PutAsync(data);
                
                _logger.LogInformation($"[Firebase] Updated data for chipId: {chipId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[Firebase] Error updating data for chipId: {chipId}");
            }
        }

        public async Task UpdateHubStatusAsync(int hubId, bool isOnline)
        {
            try
            {
                await _firebaseClient
                    .Child("Hubs")
                    .Child(hubId.ToString())
                    .Child("IsOnline")
                    .PutAsync(isOnline);

                await _firebaseClient
                    .Child("Hubs")
                    .Child(hubId.ToString())
                    .Child("LastUpdated")
                    .PutAsync(DateTime.UtcNow);

                _logger.LogInformation($"[Firebase] Updated status for Hub: {hubId} -> {(isOnline ? "ONLINE" : "OFFLINE")}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[Firebase] Error updating status for Hub: {hubId}");
            }
        }
    }
}
