namespace EwtLinkGenerator
{
    public class EwtRequest
    {
        public bool IsM365 { get; set; }
        public string SenderEmail { get; set; }
        public string RecipientEmail { get; set; }
        public string DistributionList { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public string TenantId { get; set; }
        public string EwsUrl { get; set; }
        public string AppId { get; set; }
        public string ClientSecret { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        public EwtRequest() { }

        public EwtRequest(bool isM365, string senderEmail, string recipientEmail, string subject, string body, Dictionary<string, string> headers, string tenantId = null, string appId = null, string clientSecret = null, string ewsUrl = null, string username = null, string password = null) : this()
        {
            IsM365 = isM365;
            SenderEmail = senderEmail;
            RecipientEmail = recipientEmail;
            Subject = subject;
            Body = body;
            Headers = headers ?? new();
            TenantId = tenantId;
            EwsUrl = ewsUrl;
            Username = username;
            Password = password;
            AppId = appId;
            ClientSecret = clientSecret;
        }
    }
}
