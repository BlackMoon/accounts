namespace accounts.UI.Logout
{
    public class LogoutViewModel
    {
        /// <summary>
        /// URL источника запроса (для возврата)
        /// </summary>
        public string Referer { get; set; }

        public string ReturnUrl { get; set; }
    }
}
