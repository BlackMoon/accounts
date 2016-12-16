using System.Collections.Generic;
using Kit.Core.Configuration;

namespace accounts
{
    public class AppSettings : BaseSettings
    {
        /// <summary>
        /// autocomplete="on|off" для input type="password"
        /// </summary>
        public bool AutoComplete { get; set; }

        /// <summary>
        /// Выводить диалог подтверждениея при выходе
        /// </summary>
        public bool EnableSignOutPrompt { get; set; }

        /// <summary>
        /// Persistent cookie?
        /// </summary>
        public bool Persistent { get; set; }

        /// <summary>
        /// Время жизни, сек
        /// </summary>
        public int Timeout { get; set; } = 1800;

        // ReSharper disable once CollectionNeverUpdated.Global
        public Dictionary<string, string> ErrorMessages { get; set; }
    }
}
