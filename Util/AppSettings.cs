namespace AuctionApi.Util
{
    public class AppSettings
    {
        public int RefreshTokenTTL { get; set; }
        public string AccessTokenExpirationMinutes { get; set; } = string.Empty;
        public string RefreshTokenExpirationMinutes { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
    }
}
