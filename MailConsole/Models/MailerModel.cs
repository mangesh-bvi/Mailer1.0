using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MailerConsole
{
   public class TicketingMailerModel
    {
        public int _MailID { get; set; }
        public int _TicketID { get; set; }
        public int _TenantID { get; set; }
        public int _AlertID { get; set; }
        public string _TikcketMailSubject { get; set; }
        public string _TicketMailBody { get; set; }
        public int _TicketSource { get; set; }
        public string _ToEmail { get; set; }
        public string _UserCC { get; set; }
        public string _UserBCC { get; set; }
        public int _IsSent { get; set; }
        public int _PriorityID { get; set; }
        public int _CreatedBy { get; set; }
        public SMTPDetails _Smtp { get; set; }
        //public string _CreatedDate { get; set; }
        //public string _ModifiedBy { get; set; }
        //public string _ModifiedDate { get; set; }
    }

    public class SMTPDetails
    {
        /// <summary>
        /// Frome Email Id
        /// </summary>
        public string FromEmailId { get; set; }

        /// <summary>
        /// Password
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Enable SSL
        /// </summary>
        public bool EnableSsl { get; set; }

        /// <summary>
        /// SMTP Port
        /// </summary>
        public string SMTPPort { get; set; }

        /// <summary>
        /// SMTP Server
        /// </summary>
        public string SMTPServer { get; set; }

        /// <summary>
        /// Is body HTML
        /// </summary>
        public bool IsBodyHtml { get; set; }

        /// <summary>
        /// SMTP Host
        /// </summary>
        public string SMTPHost { get; set; }
    }
}
