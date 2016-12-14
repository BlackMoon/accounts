namespace accounts.Models
{
    public class LoggedOutViewModel
    {
        public string ClientName { get; set; }
        public string PostLogoutRedirectUri { get; set; }
        public string SignOutIframeUrl { get; set; }
    }
}
