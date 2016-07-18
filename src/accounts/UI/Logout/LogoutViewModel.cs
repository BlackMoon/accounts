namespace accounts.UI.Logout
{
    public class LogoutViewModel
    {
        public string LogoutId { get; set; }

        /// <summary>
        /// URL источника запроса (для возврата)
        /// </summary>
        public string Referer { get; set; }
    }
}
