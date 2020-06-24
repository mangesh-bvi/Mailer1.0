using MailerConsole;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace MailConsole
{
    class Program
    {
        private static string _Connectionstring;
        public static string[] _CustomerKeyword = null;// System.Configuration.ConfigurationManager.AppSettings["customerkeyword"].Split(new char [] { ','},StringSplitOptions.RemoveEmptyEntries);
        public static string[] _TicketKeyword = null;  // System.Configuration.ConfigurationManager.AppSettings["ticketkeyword"].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        ErrorLogs errorlogs = new ErrorLogs();
        #region variables

        List<TicketingMailerModel> MailerList = new List<TicketingMailerModel>();
        List<int> MailSuccessList = new List<int>();
        SMTPDetails smtpDetails = new SMTPDetails();
        double failcount = 0; double successcount = 0; double smtperrorcount = 0; double updatecount = 0;
        string ConsoleMsg = string.Empty;
        bool isMailsent = false;

        DataTable dtMail = new DataTable();

        #endregion

        static void Main(string[] args)
        {
            Program obj = new Program();
            try
            {
                var builder = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
               .AddUserSecrets<Program>()
               .AddEnvironmentVariables();

                IConfigurationRoot configuration = builder.Build();
                var mySettingsConfig = new MySettingsConfig();
                configuration.GetSection("MySettings").Bind(mySettingsConfig);

                _Connectionstring = configuration.GetConnectionString("DefaultConnection");
                string interval = mySettingsConfig.IntervalInMinutes;

                _CustomerKeyword = mySettingsConfig.Customerkeyword.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                _TicketKeyword = mySettingsConfig.Ticketkeyword.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                obj.CreateTicket();
                //obj.AddToMailerQueue();
                double intervalInMinutes = Convert.ToDouble(interval);// 60 * 5000; // milliseconds to one min


                //obj.CreateTicket();
                //obj.AddToMailerQueue();

                Thread _Individualprocessthread = new Thread(new ThreadStart(obj.CallEveryMin));
                _Individualprocessthread.Start();

                //Timer checkForTime = new Timer(intervalInMinutes);
                //checkForTime.Elapsed += new ElapsedEventHandler(obj.CallEveryMin);
                //checkForTime.Enabled = true;

                //obj.CallEveryMin();
            }
            catch (Exception ex)
            {

                //Console.WriteLine(ex.ToString() + "\n" + ex.InnerException);
            }



            // Console.ReadLine();
        }


        public void CallEveryMin()
        {
            var builder = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
               .AddUserSecrets<Program>()
               .AddEnvironmentVariables();

            IConfigurationRoot configuration = builder.Build();
            var mySettingsConfig = new MySettingsConfig();
            configuration.GetSection("MySettings").Bind(mySettingsConfig);

            _Connectionstring = configuration.GetConnectionString("DefaultConnection");
            string interval = mySettingsConfig.IntervalInMinutes;


            while (true)
            {
                CreateTicket();
                AddToMailerQueue();
                AddStoreToMailerQueue();
                Thread.Sleep(Convert.ToInt32(interval));
            }
        }


        //public void CallEveryMin(object sender, ElapsedEventArgs e)
        //// public void CallEveryMin()
        //{
        //    try
        //    {
        //        Task.Factory.StartNew(CreateTicket);
        //        Task.Factory.StartNew(AddToMailerQueue);

        //        // CreateTicket();
        //        //AddToMailerQueue();
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}

        public void AddToMailerQueue()
        {
            try
            {
                #region get Mailer List

                Global global = new Global(_Connectionstring);

                MailerList = Global.RetrieveFromDB();

                if (MailerList.Count > 0)
                {
                    for (int i = 0; i < MailerList.Count; i++)
                    {
                        if (MailerList[i]._Smtp != null)
                        {

                            isMailsent = Global.SendEmail(MailerList[i]._Smtp, MailerList[i]._ToEmail, MailerList[i]._TikcketMailSubject, MailerList[i]._TicketMailBody,
                                string.IsNullOrEmpty(MailerList[i]._UserCC) ? null : MailerList[i]._UserCC.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries),
                                string.IsNullOrEmpty(MailerList[i]._UserBCC) ? null : MailerList[i]._UserBCC.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries),
                                MailerList[i]._TenantID);

                            if (isMailsent)
                            {
                                successcount++;
                                MailSuccessList.Add(MailerList[i]._MailID);
                            }
                            else
                            {
                                failcount++;

                            }

                        }
                        else
                        {
                            smtperrorcount++;
                        }
                    }

                    if (MailSuccessList.Count > 0)
                    {
                        updatecount = Global.UpdateMailerQue(string.Join(",", MailSuccessList));
                    }

                    ConsoleMsg += "Mail sent SuccesFully for " + successcount + " records \n";
                    ConsoleMsg += "Mail Failed for " + failcount + " records \n";
                    ConsoleMsg += "Mail Failed due to SMTP error for " + smtperrorcount + " records \n";

                    // Console.WriteLine(ConsoleMsg);
                }


                #endregion
            }
            catch (Exception ex)
            {
                //Console.WriteLine(ex.ToString() + "\n" + ex.InnerException);

                errorlogs.SendErrorToText(ex);
            }


        }

        public void AddStoreToMailerQueue()
        {
            try
            {
                #region get Mailer List

                Global global = new Global(_Connectionstring);

                MailerList = Global.RetrieveFromStoreDB();

                if (MailerList.Count > 0)
                {
                    for (int i = 0; i < MailerList.Count; i++)
                    {
                        if (MailerList[i]._Smtp != null)
                        {
                            if (MailerList[i]._ToEmail != null)
                            {
                                isMailsent = Global.SendEmail(MailerList[i]._Smtp, MailerList[i]._ToEmail, MailerList[i]._TikcketMailSubject, MailerList[i]._TicketMailBody,
                                string.IsNullOrEmpty(MailerList[i]._UserCC) ? null : MailerList[i]._UserCC.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries),
                                string.IsNullOrEmpty(MailerList[i]._UserBCC) ? null : MailerList[i]._UserBCC.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries),
                                MailerList[i]._TenantID);

                                if (isMailsent)
                                {
                                    successcount++;
                                    MailSuccessList.Add(MailerList[i]._MailID);
                                }
                                else
                                {
                                    failcount++;

                                }
                            }
                            else
                            {
                                failcount++;
                            }
                        }
                        else
                        {
                            smtperrorcount++;
                        }
                    }

                    if (MailSuccessList.Count > 0)
                    {
                        updatecount = Global.UpdateStoreMailerQue(string.Join(",", MailSuccessList));
                    }

                    ConsoleMsg += "Mail sent SuccesFully for " + successcount + " records \n";
                    ConsoleMsg += "Mail Failed for " + failcount + " records \n";
                    ConsoleMsg += "Mail Failed due to SMTP error for " + smtperrorcount + " records \n";

                    // Console.WriteLine(ConsoleMsg);
                }


                #endregion
            }
            catch (Exception ex)
            {
                //Console.WriteLine(ex.ToString() + "\n" + ex.InnerException);

                errorlogs.SendErrorToText(ex);
            }


        }

        public void CreateTicket()
        {
            TicketThruMail fm = new TicketThruMail(_Connectionstring, _CustomerKeyword, _TicketKeyword);
            List<TenantMailDetailsModel> tenantDetails = new List<TenantMailDetailsModel>();
            int CustomerID = 0; int TicketID = 0; ; int ticketcount = 0;
            ConsoleMsg = string.Empty;
            try
            {
                tenantDetails = fm.GetTenantMailConfig();

                if (tenantDetails != null && tenantDetails.Count > 0)
                {
                    for (int i = 0; i < tenantDetails.Count; i++)
                    {
                        #region Get Email list for each tenant

                        if (!string.IsNullOrEmpty(tenantDetails[i].SMTPHost) && !string.IsNullOrEmpty(tenantDetails[i].EmailSenderID) &&
                               !string.IsNullOrEmpty(tenantDetails[i].EmailPassword) && tenantDetails[i].SMTPPort != 0)
                        {

                            DataTable dtMail = new DataTable();
                            dtMail = fm.getEmail(tenantDetails[i]);

                            if (dtMail != null && dtMail.Rows.Count > 0)
                            {
                                for (int j = 0; j < dtMail.Rows.Count; j++)
                                {
                                    string CustomerName = Convert.ToString(dtMail.Rows[j]["FromName"]);
                                    string emailID = Convert.ToString(dtMail.Rows[j]["FromID"]);
                                    string emailsubject = Convert.ToString(dtMail.Rows[j]["Subject"]);
                                    string emailbody = Convert.ToString(dtMail.Rows[j]["Body"]);
                                    string emailHTMLbody = Convert.ToString(dtMail.Rows[j]["HTML"]);
                                    string attachment = Convert.ToString(dtMail.Rows[j]["FileName"]);
                                    string MessageID = Convert.ToString(dtMail.Rows[j]["MessageID"]);

                                    #region Read CustomerID from MailBody
                                    if (!emailID.Contains("facebook") && !emailID.Contains("googlemail"))
                                    {
                                        CustomerID = fm.GetIDFromEmailBody(emailbody, "customer");

                                        #endregion

                                        CustomerID = fm.IsCustomerExists(tenantDetails[i].TenantID, emailID, CustomerID, CustomerName);

                                        if (CustomerID > 0)
                                        {
                                            #region Read TicketID from MailBody

                                            //TicketID = fm.GetIDFromEmailBody(emailbody, "ticket");
                                            TicketID = fm.GetIDFromEmailBody(emailsubject, "ticket");

                                            if (TicketID == 0)
                                            {
                                                string[] numbers = Regex.Split(emailsubject, @"\D+");
                                                foreach (string value in numbers)
                                                {
                                                    if (!string.IsNullOrEmpty(value))
                                                    {
                                                        TicketID = int.Parse(value);
                                                    }
                                                }
                                            }

                                            if (TicketID > 0)
                                            {

                                                if (MessageID.Contains("yahoo"))
                                                {
                                                    string[] spearatoryahoo = { "<br> <blockquote " };

                                                    string[] emailHTMLbodySplit = emailHTMLbody.Split(spearatoryahoo, 10, StringSplitOptions.None);

                                                    if (emailHTMLbodySplit.Length > 1)
                                                    {
                                                        emailbody = emailHTMLbodySplit[0];
                                                    }
                                                }
                                                else if (MessageID.Contains("outlook"))
                                                {
                                                    string[] spearatoroutlook = { "<div id=\"divRplyFwdMsg\"" };

                                                    string[] emailHTMLbodySplit = emailHTMLbody.Split(spearatoroutlook, 10, StringSplitOptions.None);

                                                    if (emailHTMLbodySplit.Length > 1)
                                                    {
                                                        emailbody = emailHTMLbodySplit[0];
                                                    }
                                                }
                                                else
                                                {
                                                    string[] spearator = { "<br><div class=\"gmail_quote\"><div dir=\"ltr\" class=\"gmail_attr\">" };

                                                    string[] emailHTMLbodySplit = emailHTMLbody.Split(spearator, 10, StringSplitOptions.None);

                                                    if (emailHTMLbodySplit.Length > 1)
                                                    {
                                                        emailbody = emailHTMLbodySplit[0];
                                                    }
                                                    else
                                                    {
                                                        string[] spearatoroutlook = { "<div id=\"divRplyFwdMsg\"" };
                                                        emailHTMLbodySplit = emailHTMLbody.Split(spearatoroutlook, 10, StringSplitOptions.None);
                                                        if (emailHTMLbodySplit.Length > 1)
                                                        {
                                                            emailbody = emailHTMLbodySplit[0];
                                                        }
                                                    }
                                                }

                                                //emailbody = Regex.Replace(emailbody, "<.*?>", String.Empty);
                                                emailbody = Regex.Replace(emailbody, @"<(?!br[\x20/>])[^<>]+>", String.Empty);

                                            }
                                            #endregion

                                            ticketcount += fm.CreateTicket(tenantDetails[i].TenantID, CustomerID, TicketID, emailID, emailsubject, emailbody, attachment);
                                        }
                                    }

                                }

                            }
                            else
                            {
                                continue;
                            }

                        }
                        else
                        {
                            continue;
                        }

                        #endregion

                        ConsoleMsg += ticketcount + " tickets created/updated for tenantID " + tenantDetails[i].TenantID + "\n";

                    }

                }


            }
            catch (Exception ex)
            {
                //SendErrorToText(ex);
                errorlogs.SendErrorToText(ex);
                ConsoleMsg = ex.ToString() + "\n" + ex.InnerException;
            }
            // Console.WriteLine(ConsoleMsg);

        }


        public MySettingsConfigMoal GetConfigDetails()
        {
            MySettingsConfigMoal MySettingsConfigMoal = new MySettingsConfigMoal();

            try
            {
                var builder = new ConfigurationBuilder()
              .SetBasePath(Directory.GetCurrentDirectory())
              .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
              .AddUserSecrets<Program>()
              .AddEnvironmentVariables();

                IConfigurationRoot configuration = builder.Build();
                var mySettingsConfig = new MySettingsConfig();
                configuration.GetSection("MySettings").Bind(mySettingsConfig);

                MySettingsConfigMoal.Connectionstring = configuration.GetConnectionString("DefaultConnection");

            }
            catch (Exception ex)
            {
                // Console.WriteLine("Error getting data from appsetting.json");
            }

            return MySettingsConfigMoal;
        }
    }
}
