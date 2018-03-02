namespace Kite.AutoTrading.Common.Models

{
    public class UserSessionModel
    {
        public string UserId { get; set; }
        public string ApiKey { get; set; }
        public string AppSecret { get; set; }
        public string AccessToken { get; set; }
        public string PublicToken { get; set; }
    }
}
