using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;


namespace MailerConsole
{
    public class Global
    {
        
        public static MySqlConnection conn = null;

        public static MySqlDataAdapter sda = new MySqlDataAdapter();

        public Global(string Connectionstring)
        {
            conn = new MySqlConnection(Connectionstring);
        }


        #region CONSOLE MAILER 

        #region GetMailer List
        public static List<TicketingMailerModel> RetrieveFromDB()
        {
            DataSet Mailerds = new DataSet();
            List<TicketingMailerModel> MailerList = new List<TicketingMailerModel>();
            MySqlCommand cmd = new MySqlCommand();

            try
            {
                //_connectionstring = System.Configuration.ConfigurationManager.ConnectionStrings["conn"].ConnectionString;
                
                if (conn != null && conn.State == ConnectionState.Closed)
                {
                    conn.Open();
                }

                cmd.Connection = conn;
                MySqlCommand cmd1 = new MySqlCommand("SP_GetTiketingMailerDetails", conn);
                cmd1.CommandType = CommandType.StoredProcedure;
                sda.SelectCommand = cmd1;
                sda.Fill(Mailerds);

                if (Mailerds != null && Mailerds.Tables[0] != null)
                {
                    if(Mailerds.Tables[0].Rows.Count > 0)
                    {
                        foreach (DataRow dr in Mailerds.Tables[0].Rows)
                        {
                            try
                            {
                                TicketingMailerModel obj = new TicketingMailerModel();

                                obj._MailID = dr["MailID"] == DBNull.Value ? 0 : Convert.ToInt32(dr["MailID"]);
                                obj._TicketID = dr["TicketID"] == DBNull.Value ? 0 : Convert.ToInt32(dr["TicketID"]);
                                obj._TenantID = dr["TenantID"] == DBNull.Value ? 0 : Convert.ToInt32(dr["TenantID"]);
                                obj._AlertID = dr["AlertID"] == DBNull.Value ? 0 : Convert.ToInt32(dr["AlertID"]);
                                obj._TikcketMailSubject = dr["TikcketMailSubject"] == DBNull.Value ? string.Empty : Convert.ToString(dr["TikcketMailSubject"]);
                                obj._TicketMailBody = dr["TicketMailBody"] == DBNull.Value ? string.Empty : Convert.ToString(dr["TicketMailBody"]);
                                obj._TicketSource = dr["TicketSource"] == DBNull.Value ? 0 : Convert.ToInt32(dr["TicketSource"]);
                                obj._ToEmail = dr["ToEmail"] == DBNull.Value ? string.Empty : Convert.ToString(dr["ToEmail"]);
                                obj._UserCC = dr["UserCC"] == DBNull.Value ? string.Empty : Convert.ToString(dr["UserCC"]);
                                obj._UserBCC = dr["UserBCC"] == DBNull.Value ? string.Empty : Convert.ToString(dr["UserBCC"]);
                                obj._IsSent = dr["IsSent"] == DBNull.Value ? 0 : Convert.ToInt32(dr["IsSent"]);
                                obj._PriorityID = dr["PriorityID"] == DBNull.Value ? 0 : Convert.ToInt32(dr["PriorityID"]);
                                obj._Smtp = GetSMTPDetails(dr["TenantID"] == DBNull.Value ? 0 : Convert.ToInt32(dr["TenantID"]));
                                

                                MailerList.Add(obj);
                            }
                            catch(Exception ex)
                            {
                                throw ex;
                            }
                            

                            
                        }
                               
                    }
                }

            }
            catch(Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (conn != null)
                    conn.Close();
                if (Mailerds != null)
                    Mailerds.Dispose();
            }
            return MailerList;
        }

        #endregion

        #region UpdateMailerQue

        public static double UpdateMailerQue(string MailIds)
        {
            double updatecount = 0;
            MySqlCommand cmd = new MySqlCommand();
            try
            {
                if (!string.IsNullOrEmpty(MailIds))
                {
                    if (conn != null && conn.State == ConnectionState.Closed)
                    {
                        conn.Open();
                    }
                    cmd.Connection = conn;
                    MySqlCommand cmd1 = new MySqlCommand("SP_UpdateTicketingMailerDetails", conn);
                    cmd1.Parameters.AddWithValue("_MailId", MailIds);
                    cmd1.CommandType = CommandType.StoredProcedure;
                    updatecount=cmd1.ExecuteNonQuery();

                }
                
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (conn != null)
                    conn.Close();
            }

            return updatecount;
        }
        #endregion

        #region GetSMTPDetails
        public static SMTPDetails GetSMTPDetails(int TenantID)
        {
            DataSet ds = new DataSet();
            SMTPDetails sMTPDetails = new SMTPDetails();

            try
            {
                MySqlCommand cmd = new MySqlCommand();

                if (conn != null && conn.State == ConnectionState.Closed)
                {
                    conn.Open();
                }
                cmd.Connection = conn;
                MySqlCommand cmd1 = new MySqlCommand("SP_getSMTPDetails", conn);
                cmd1.CommandType = CommandType.StoredProcedure;
                cmd1.Parameters.AddWithValue("@Tenant_ID", TenantID);
                MySqlDataAdapter da = new MySqlDataAdapter();
                da.SelectCommand = cmd1;
                da.Fill(ds);
                if (ds != null && ds.Tables[0] != null)
                {
                    sMTPDetails.EnableSsl = Convert.ToBoolean(ds.Tables[0].Rows[0]["EnabledSSL"]);
                    sMTPDetails.SMTPPort = Convert.ToString(ds.Tables[0].Rows[0]["SMTPPort"]);
                    sMTPDetails.FromEmailId = Convert.ToString(ds.Tables[0].Rows[0]["EmailUserID"]);
                    sMTPDetails.IsBodyHtml = Convert.ToBoolean(ds.Tables[0].Rows[0]["IsBodyHtml"]);
                    sMTPDetails.Password = Convert.ToString(ds.Tables[0].Rows[0]["EmailPassword"]);
                    sMTPDetails.SMTPHost = Convert.ToString(ds.Tables[0].Rows[0]["SMTPHost"]);
                    sMTPDetails.SMTPServer = Convert.ToString(ds.Tables[0].Rows[0]["SMTPHost"]);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
               
                if (ds != null)
                    ds.Dispose();
            }

            return sMTPDetails;
        }
        #endregion

        #region SendMail
        public static bool SendEmail(SMTPDetails smtpDetails, string emailToAddress, string subject, string body, string[] cc = null, string[] bcc = null, int tenantId = 0)
        {
            bool isMailSent = false;
            try
            {
          
                SmtpClient smtpClient = new SmtpClient(smtpDetails.SMTPServer, Convert.ToInt32(smtpDetails.SMTPPort));
                smtpClient.EnableSsl = smtpDetails.EnableSsl;
                smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtpClient.UseDefaultCredentials = true;
                smtpClient.Credentials = new NetworkCredential(smtpDetails.FromEmailId, smtpDetails.Password);
                {
                    using (MailMessage message = new MailMessage())
                    {
                        message.From = new MailAddress(smtpDetails.FromEmailId, "EasyRewardz");

                        if (cc != null)
                        {
                            if (cc.Length > 0)
                            {
                                for (int i = 0; i < cc.Length; i++)
                                {
                                    message.CC.Add(cc[i]);
                                }

                            }
                        }

                        if (bcc != null)
                        {
                            if (bcc.Length > 0)
                            {
                                for (int k = 0; k < bcc.Length; k++)
                                {
                                    message.CC.Add(bcc[k]);
                                }

                            }
                        }
                        message.Subject = subject == null ? "" : subject;
                        message.Body = body == null ? "" : body;
                        message.IsBodyHtml = smtpDetails.IsBodyHtml;
                        message.To.Add(emailToAddress);

                        smtpClient.Send(message);
                    }
                }

                isMailSent = true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return isMailSent;
        }


        #endregion

        #endregion

     

    }
}
