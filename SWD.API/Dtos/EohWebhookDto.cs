namespace SWD.API.Dtos
{
    public class EohWebhookDto
    {
        public double v1 { get; set; } // Temperature
        public double v2 { get; set; } // Humidity
        public double v3 { get; set; } // Pressure
        public string? v4 { get; set; } // Time
        public string? v5 { get; set; } // Province
        public string? v6 { get; set; } // City
        public int v7 { get; set; } // Additional field (numeric)
        public int v8 { get; set; } // Additional field (numeric)
        public string? v9 { get; set; } // Weather condition
        public string? v10 { get; set; } // Weather description
        public string? v11 { get; set; } // Icon
        public string? v12 { get; set; } // Hub MAC Address
        public string? deviceId { get; set; } // Optional: in case it's in body
    }
}
