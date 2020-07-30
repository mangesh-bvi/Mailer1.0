using MailerConsole;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
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
        //private static string _Connectionstring;
        public static string[] _CustomerKeyword = null;
        public static string[] _TicketKeyword = null;
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
            obj.StartProcess();
        }


        public void StartProcess()
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


                string interval = mySettingsConfig.IntervalInMinutes;

                _CustomerKeyword = mySettingsConfig.Customerkeyword.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                _TicketKeyword = mySettingsConfig.Ticketkeyword.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                double intervalInMinutes = Convert.ToDouble(interval);

                Thread _Individualprocessthread = new Thread(new ThreadStart(InvokeMethod));
                _Individualprocessthread.Start();


            }
            catch
            {

            }




        }


        public void InvokeMethod()
        {
            var builder = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
               .AddUserSecrets<Program>()
               .AddEnvironmentVariables();

            IConfigurationRoot configuration = builder.Build();
            var mySettingsConfig = new MySettingsConfig();
            configuration.GetSection("MySettings").Bind(mySettingsConfig);

            string interval = mySettingsConfig.IntervalInMinutes;

            int intervalInMinutes = Convert.ToInt32(interval);

            while (true)
            {
                GetConnectionStrings();

                Thread.Sleep(intervalInMinutes);

            }

        }


        public void CallEveryMin(string ConStrings)
        {
            var builder = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
               .AddUserSecrets<Program>()
               .AddEnvironmentVariables();

            IConfigurationRoot configuration = builder.Build();
            var mySettingsConfig = new MySettingsConfig();
            configuration.GetSection("MySettings").Bind(mySettingsConfig);
            string interval = mySettingsConfig.IntervalInMinutes;


            CreateTicket(ConStrings);

            AddToMailerQueue(ConStrings);
            AddStoreToMailerQueue(ConStrings);


        }

        public void AddToMailerQueue(string ConStrings)
        {
            //errorlogs.SendErrorToText(new NullReferenceException("Student object is null."), ConStrings, "Mail Process Started");
            try
            {
                #region get Mailer List

                Global global = new Global(ConStrings);

                MailerList = Global.RetrieveFromDB(ConStrings);
                //errorlogs.SendErrorToText(new NullReferenceException("Student object is null."), ConStrings, "Retrive DBConnection Started");
                if (MailerList.Count > 0)
                {
                    //errorlogs.SendErrorToText(new NullReferenceException("Student object is null."), ConStrings, "Mail Count " + MailerList.Count);
                    for (int i = 0; i < MailerList.Count; i++)
                    {
                        try
                        {
                            //errorlogs.SendErrorToText(new NullReferenceException("Student object is null."), ConStrings, "In Loop 1 :" + i);
                            if (MailerList[i]._Smtp != null)
                            {
                                //errorlogs.SendErrorToText(new NullReferenceException("Student object is null."), ConStrings, "In Loop 2 :" + i);

                                isMailsent = Global.SendEmail(MailerList[i]._Smtp, MailerList[i]._ToEmail, MailerList[i]._TikcketMailSubject, MailerList[i]._TicketMailBody,
                                    string.IsNullOrEmpty(MailerList[i]._UserCC) ? null : MailerList[i]._UserCC.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries),
                                    string.IsNullOrEmpty(MailerList[i]._UserBCC) ? null : MailerList[i]._UserBCC.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries),
                                    MailerList[i]._TenantID, ConStrings);
                                //errorlogs.SendErrorToText(new NullReferenceException("Student object is null."), ConStrings, "In Loop 3 :" + i);
                                if (isMailsent)
                                {
                                    //errorlogs.SendErrorToText(new NullReferenceException("Student object is null."), ConStrings, "Mail Sent:" + isMailsent);
                                    successcount++;
                                    MailSuccessList.Add(MailerList[i]._MailID);
                                }
                                else
                                {
                                    failcount++;

                                }
                                //errorlogs.SendErrorToText(new NullReferenceException("Student object is null."), ConStrings, "Mail Sending status Fail Cnt : " + failcount + "Success Cnt :" + successcount);
                            }
                            else
                            {
                                smtperrorcount++;
                            }
                        }
                        catch (Exception ex)
                        {
                            errorlogs.SendErrorToText(ex, ConStrings);
                        }

                    }

                    if (MailSuccessList.Count > 0)
                    {
                        updatecount = Global.UpdateMailerQue(string.Join(",", MailSuccessList), ConStrings);
                        //errorlogs.SendErrorToText(null, ConStrings, "Mailer count updated in DB");
                    }

                    ConsoleMsg += "Mail sent SuccesFully for " + successcount + " records \n";
                    ConsoleMsg += "Mail Failed for " + failcount + " records \n";
                    ConsoleMsg += "Mail Failed due to SMTP error for " + smtperrorcount + " records \n";

                    //errorlogs.SendErrorToText(null, ConStrings, "Mailer Process complete");
                }


                #endregion
            }
            catch (Exception ex)
            {

                errorlogs.SendErrorToText(ex, ConStrings);
            }


        }

        public void AddStoreToMailerQueue(string ConStrings)
        {
            try
            {
                #region get Mailer List

                Global global = new Global(ConStrings);

                MailerList = Global.RetrieveFromStoreDB(ConStrings);

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
                        updatecount = Global.UpdateStoreMailerQue(string.Join(",", MailSuccessList), ConStrings);
                    }

                    ConsoleMsg += "Mail sent SuccesFully for " + successcount + " records \n";
                    ConsoleMsg += "Mail Failed for " + failcount + " records \n";
                    ConsoleMsg += "Mail Failed due to SMTP error for " + smtperrorcount + " records \n";


                }


                #endregion
            }
            catch (Exception ex)
            {

                errorlogs.SendErrorToText(ex, ConStrings);
            }


        }

        public void CreateTicket(string ConStrings)
        {
            //errorlogs.SendErrorToText(new NullReferenceException("Create Ticket Started"), ConStrings, "Create Ticket Started");
            TicketThruMail fm = new TicketThruMail(ConStrings, _CustomerKeyword, _TicketKeyword);
            List<TenantMailDetailsModel> tenantDetails = new List<TenantMailDetailsModel>();
            int CustomerID = 0; int TicketID = 0; ; int ticketcount = 0;
            ConsoleMsg = string.Empty;
            try
            {
                //errorlogs.SendErrorToText(new NullReferenceException("Befor Constring "), ConStrings, "Befor Constring ");
                tenantDetails = fm.GetTenantMailConfig(ConStrings);
                //errorlogs.SendErrorToText(new NullReferenceException("After Constring"), ConStrings, "After Ticket Started" + ConStrings);
                if (tenantDetails != null && tenantDetails.Count > 0)
                {
                    for (int i = 0; i < tenantDetails.Count; i++)
                    {
                        #region Get Email list for each tenant

                        if (!string.IsNullOrEmpty(tenantDetails[i].SMTPHost) && !string.IsNullOrEmpty(tenantDetails[i].EmailSenderID) &&
                               !string.IsNullOrEmpty(tenantDetails[i].EmailPassword) && tenantDetails[i].SMTPPort != 0)
                        {

                            DataTable dtMail = new DataTable();
                            dtMail = fm.getEmail(tenantDetails[i], ConStrings);

                            if (dtMail != null && dtMail.Rows.Count > 0)
                            {
                                //errorlogs.SendErrorToText(new NullReferenceException("Getting Data from DB" + ConStrings), ConStrings, "Getting Data from DB" + ConStrings);
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
                                        CustomerID = fm.GetIDFromEmailBody(emailbody, "customer", ConStrings);

                                        #endregion

                                        CustomerID = fm.IsCustomerExists(tenantDetails[i].TenantID, emailID, CustomerID, CustomerName, ConStrings);
                                        //errorlogs.SendErrorToText(new NullReferenceException("Getting Data from DB" + dtMail.Rows.Count), ConStrings, "Getting Data from DB" + CustomerID);

                                        if (CustomerID > 0)
                                        {
                                            //errorlogs.SendErrorToText(new NullReferenceException("CustomerID > 0" + CustomerID), ConStrings, "CustomerID > 0" + CustomerID);
                                            #region Read TicketID from MailBody


                                            TicketID = fm.GetIDFromEmailBody(emailsubject, "ticket", ConStrings);

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


                                                emailbody = Regex.Replace(emailbody, @"<(?!br[\x20/>])[^<>]+>", String.Empty);

                                            }
                                            #endregion

                                            ticketcount += fm.CreateTicket(tenantDetails[i].TenantID, CustomerID, TicketID, emailID, emailsubject, emailbody, attachment, ConStrings);


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
                        //errorlogs.SendErrorToText(new NullReferenceException("ticketcount"), ConStrings, "ticketcount" + ConsoleMsg);
                    }

                }


            }
            catch (Exception ex)
            {

                errorlogs.SendErrorToText(ex, ConStrings);
                ConsoleMsg = ex.ToString() + "\n" + ex.InnerException;
            }


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
            catch
            {

            }

            return MySettingsConfigMoal;
        }


        public void GetConnectionStrings()
        {
            string ServerName = string.Empty;
            string ServerCredentailsUsername = string.Empty;
            string ServerCredentailsPassword = string.Empty;
            string DBConnection = string.Empty;


            try
            {
                DataTable dt = new DataTable();
                IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json", true, true).Build();
                var constr = config.GetSection("ConnectionStrings").GetSection("DefaultConnection").Value;
                MySqlConnection con = new MySqlConnection(constr);
                MySqlCommand cmd = new MySqlCommand("SP_HSGetAllConnectionstrings", con);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Connection.Open();
                MySqlDataAdapter da = new MySqlDataAdapter(cmd);
                da.Fill(dt);
                cmd.Connection.Close();

                if (dt.Rows.Count > 0)
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        DataRow dr = dt.Rows[i];
                        ServerName = Convert.ToString(dr["ServerName"]);
                        ServerCredentailsUsername = Convert.ToString(dr["ServerCredentailsUsername"]);
                        ServerCredentailsPassword = Convert.ToString(dr["ServerCredentailsPassword"]);
                        DBConnection = Convert.ToString(dr["DBConnection"]);

                        string ConString = "Data Source = " + ServerName + " ; port = " + 3306 + "; Initial Catalog = " + DBConnection + " ; User Id = " + ServerCredentailsUsername + "; password = " + ServerCredentailsPassword + "";
                        CallEveryMin(ConString);
                    }
                }
            }
            catch (Exception ex)
            {

              //  errorlogs.SendErrorToText(ex, ConString);
            }
            finally
            {

                GC.Collect();
            }


        }
    }
}
