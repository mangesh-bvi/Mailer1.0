using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MailerConsole
{
    public class TenantMailDetailsModel
    {
        public int TenantID { get; set; }
        public string AppID { get; set; }
        public string EmailID { get; set; }
        public int TenantStatusID { get; set; }
        public string SMTPHost { get; set; }
        public int SMTPPort { get; set; }
        public int ID { get; set; }
        public int IsActive { get; set; }
        public int EnabledSSL { get; set; }
        public string EmailUserID { get; set; }
        public string EmailSenderName { get; set; } 
        public string EmailSenderID { get; set; }
        public string EmailPassword { get; set; }
     
    }
}
