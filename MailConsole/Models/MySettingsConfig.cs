using System;
using System.Collections.Generic;
using System.Text;

namespace MailConsole
{
    public class MySettingsConfig
    {
        public string IntervalInMinutes { get; set; }
        public string Ticketkeyword { get; set; }
        public string Customerkeyword { get; set; }
    }

    public class MySettingsConfigMoal
    {
        public string Connectionstring { get; set; }
        public string IntervalInMinutes { get; set; }
        public string Ticketkeyword { get; set; }
        public string Customerkeyword { get; set; }
    }
}
