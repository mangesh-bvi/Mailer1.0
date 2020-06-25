
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenPop.Common;
using OpenPop.Mime;
using OpenPop.Pop3;
using OpenPop.Mime.Decode;
using MySql.Data.MySqlClient;
using Microsoft.Extensions.Configuration;

namespace MailerConsole
{
    public class TicketThruMail

    {  
        Dictionary<string, string[]> dictKeyword = null;
        public static string strUserName = string.Empty;
        public static MySqlConnection conn = null;
        public static  MySqlCommand cmd = new MySqlCommand();
        public static MySqlDataAdapter sda = new MySqlDataAdapter();
        ErrorLogs errorlogs = new ErrorLogs();
        public  TicketThruMail(string Connectionstring, string[] CustomerKeyword, string[] TicketKeyword)
        {
            dictKeyword = new Dictionary<string, string[]>()
            {
                {"customer", CustomerKeyword},
                 {"ticket", TicketKeyword}

            };
            conn = new MySqlConnection(Connectionstring);
        }

        #region FETCH MAIL LIST
        //this is the mail function, it will connect to email server and will do all further process
        public DataTable getEmail(TenantMailDetailsModel tenantMailConfig,string ConStrings)
        {
          
            Pop3Client objPOP3Client = new Pop3Client();
            object[] objMessageParts;

            
            tenantMailConfig.SMTPHost = "pop.gmail.com";
            string strHostName = tenantMailConfig.SMTPHost,  strPassword = tenantMailConfig.EmailPassword;
            strUserName = tenantMailConfig.EmailSenderID;
            int smtpPort = 995;

            int intTotalEmail;
            DataTable dtEmail = new DataTable();

            try
            {
                
                if (objPOP3Client.Connected)
                    objPOP3Client.Disconnect();

               
                objPOP3Client.Connect(strHostName, smtpPort, true);

                //authenticate with server
                objPOP3Client.Authenticate(strUserName, strPassword);

                //get total email counts
                intTotalEmail = objPOP3Client.GetMessageCount();

               

                //put all mail content in this data table, so get blank table structure
                dtEmail = GetAllEmailStructure();

                //go through all emails
                for (int i = 1; i <= intTotalEmail; i++)
                {
                    objMessageParts = GetMessageContent(i, ref objPOP3Client,ConStrings);

                    if (objMessageParts != null && objMessageParts[0].ToString() == "0")
                    {
                        AddToDtEmail(objMessageParts, i, dtEmail, ConStrings);
                    }
                }
            }
            catch (Exception ex)
            {
                errorlogs.SendErrorToText(ex,ConStrings);
            }
            finally
            {
                if (objPOP3Client.Connected)
                    objPOP3Client.Disconnect();
            }
            return dtEmail;
        }

        //this function will add mapping-encoding especially for gmail email reading issue
        public void AddMapping(string ConStrings)
        {
            try
            {
                EncodingFinder.AddMapping("English (en) iso-8859-1", System.Text.Encoding.UTF8);
            }
            catch (Exception ex)
            {
                errorlogs.SendErrorToText(ex, ConStrings);
            }
            EncodingFinder.FallbackDecoder = CustomFallbackDecoder;
        }

        System.Text.Encoding CustomFallbackDecoder(string characterSet)
        {
            // Is it a "foo" encoding?
            if (characterSet.StartsWith("foo"))
                return System.Text.Encoding.ASCII; // then use ASCII

            // If no special encoding could be found, provide UTF8 as default.
            // You can also return null here, which would tell OpenPop that
            // no encoding could be found. This will then throw an exception.
            return System.Text.Encoding.UTF8;
        }

        //this function will get the message content by messageid
        public object[] GetMessageContent(int intMessageNumber, ref Pop3Client objPOP3Client,string ConStrings)
        {
            object[] strArrMessage = new object[10];
            Message objMessage;
            MessagePart plainTextPart = null, HTMLTextPart = null;
            string strMessageId = "";

            try
            {
                strArrMessage[0] = "";
                strArrMessage[1] = "";
                strArrMessage[2] = "";
                strArrMessage[3] = "";
                strArrMessage[4] = "";
                strArrMessage[5] = "";
                strArrMessage[6] = "";
                strArrMessage[7] = null;
                strArrMessage[8] = null;
                strArrMessage[7] = "";
                strArrMessage[8] = "";

                objMessage = objPOP3Client.GetMessage(intMessageNumber);
                strMessageId = (objMessage.Headers.MessageId == null ? "" : objMessage.Headers.MessageId.Trim());


                strArrMessage[0] = "0";
                strArrMessage[1] = objMessage.Headers.From.Address.Trim();     // From EMail Address
                strArrMessage[2] = objMessage.Headers.From.DisplayName.Trim(); // From EMail Name
                strArrMessage[3] = objMessage.Headers.Subject.Trim();           // Mail Subject     
                plainTextPart = objMessage.FindFirstPlainTextVersion();
                strArrMessage[4] = (plainTextPart == null ? "" : plainTextPart.GetBodyAsText().Trim());
                HTMLTextPart = objMessage.FindFirstHtmlVersion();
                strArrMessage[5] = (HTMLTextPart == null ? "" : HTMLTextPart.GetBodyAsText().Trim());
                strArrMessage[6] = strMessageId;
                List<MessagePart> attachment = objMessage.FindAllAttachments();
                strArrMessage[7] = null;
                strArrMessage[8] = null;
                if (attachment.Count > 0)
                {
                    strArrMessage[7] = string.Join(",",attachment.AsEnumerable().Select(x => x.FileName.Trim()).ToList());
                    strArrMessage[8] = attachment[0];
                }
            }
            catch (Exception ex)
            {
                errorlogs.SendErrorToText(ex, ConStrings);
            }
            return strArrMessage;
        }

        //this function will return structure to store one mail item
        public DataTable GetAllEmailStructure()
        {
            DataTable dt = new DataTable();
            DataColumn dc = new DataColumn();

            dc.ColumnName = "SrNo";
            dc.DataType = Type.GetType("System.Int32");
            dc.AutoIncrement = true;
            dc.AutoIncrementSeed = 1;
            dc.AutoIncrementStep = 1;
            dt.Columns.Add(dc);

            dt.Columns.Add("FromID", typeof(string));
            dt.Columns.Add("FromName", typeof(string));
            dt.Columns.Add("Subject", typeof(string));
            dt.Columns.Add("Body", typeof(string));
            dt.Columns.Add("Html", typeof(string));
            dt.Columns.Add("MessageID", typeof(string));
            dt.Columns.Add("FileName", typeof(string));
            dt.Columns.Add("Attechments", typeof(object));
            dt.Columns.Add("MailNo", typeof(int));

            return dt;
        }

        //this function will add one mail item (object) into mail's data table
        public void AddToDtEmail(object[] objMessageParts, int intRow, DataTable dtEmail,string ConStrings)
        {
            DataRow dr;

            string strSubject, strBody, strHtml, strFromID;

            try
            {
                dr = dtEmail.NewRow();

                strFromID = objMessageParts[1].ToString();
                strSubject = objMessageParts[3].ToString();
                strBody = objMessageParts[4].ToString();
                strHtml = objMessageParts[5].ToString();

                dr["MailNo"] = intRow.ToString();
                dr["FromID"] = strFromID;
                dr["FromName"] = objMessageParts[2].ToString();
                dr["Subject"] = strSubject;
                dr["Body"] = strBody;
                dr["Html"] = strHtml;
                dr["MessageID"] = objMessageParts[6].ToString();
                dr["FileName"] = null;
                dr["Attechments"] = null;

                if (objMessageParts[7] != null && objMessageParts[8] != null)
                {
                    dr["FileName"] = objMessageParts[7].ToString();
                    dr["Attechments"] = objMessageParts[8];
                }

                if(!strFromID.Equals(strUserName))
                dtEmail.Rows.Add(dr);
            }
            catch (Exception ex)
            {
                errorlogs.SendErrorToText(ex,ConStrings);
            }
        }

        #endregion

        #region GET TENANT DETAILS

        public List<TenantMailDetailsModel> GetTenantMailConfig(string ConStrings)
        {
            DataSet ds = new DataSet();
            List<TenantMailDetailsModel> tenantDetails = new List<TenantMailDetailsModel>();

            try
            {
                MySqlConnection conn = new MySqlConnection(ConStrings);
                MySqlCommand cmd = new MySqlCommand();

                if (conn != null && conn.State == ConnectionState.Closed)
                {
                    conn.Open();
                }
                cmd.Connection = conn;
                MySqlCommand cmd1 = new MySqlCommand("SP_GetTenantMailerDetails", conn);
                cmd1.CommandType = CommandType.StoredProcedure;
                MySqlDataAdapter da = new MySqlDataAdapter();
                da.SelectCommand = cmd1;
                da.Fill(ds);
                if (ds != null && ds.Tables[0] != null)
                {
                    if (ds.Tables[0] != null && ds.Tables[0].Rows.Count > 0)
                    {
                        foreach (DataRow dr in ds.Tables[0].Rows)
                        {
                            TenantMailDetailsModel obj = new TenantMailDetailsModel()
                            {
                                TenantID = Convert.ToInt32(dr["TenantID"]),
                                SMTPHost = dr["SMTPHost"] == System.DBNull.Value ? string.Empty : Convert.ToString(dr["SMTPHost"]),
                                SMTPPort = dr["SMTPPort"] == System.DBNull.Value ? 0 : Convert.ToInt32(dr["SMTPPort"]),
                                AppID = dr["AppID"] == System.DBNull.Value ? string.Empty : Convert.ToString(dr["AppID"]),
                                EmailID = dr["EmailID"] == System.DBNull.Value ? string.Empty : Convert.ToString(dr["EmailID"]),
                                TenantStatusID = dr["TenantStatusID"] == System.DBNull.Value ? 0 : Convert.ToInt32(dr["TenantStatusID"]),
                                ID = dr["ID"] == System.DBNull.Value ? 0 : Convert.ToInt32(dr["ID"]),
                                IsActive = dr["IsActive"] == System.DBNull.Value ? 0 : Convert.ToInt32(dr["IsActive"]),
                                EnabledSSL = dr["EnabledSSL"] == System.DBNull.Value ? 0 : Convert.ToInt32(dr["EnabledSSL"]),
                                EmailUserID = dr["EmailUserID"] == System.DBNull.Value ? string.Empty : Convert.ToString(dr["EmailUserID"]),
                                EmailSenderName = dr["EmailSenderName"] == System.DBNull.Value ? string.Empty : Convert.ToString(dr["EmailSenderName"]),
                                EmailSenderID = dr["EmailSenderID"] == System.DBNull.Value ? string.Empty : Convert.ToString(dr["EmailSenderID"]),
                                EmailPassword = dr["EmailPassword"] == System.DBNull.Value ? string.Empty : Convert.ToString(dr["EmailPassword"]),
                            };
                            tenantDetails.Add(obj);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                errorlogs.SendErrorToText(ex,ConStrings);
            }
            finally
            {

                if (ds != null)
                    ds.Dispose();
                conn.Close();
            }

            return tenantDetails;
        }
        #endregion

        #region Get CustomerID/TicketID from the Email body

        public int GetIDFromEmailBody(string EmailBody, string searchfor,string ConStrings)
        {
            int RetrivedID = 0; string strRetreivedID = string.Empty;
            EmailBody = EmailBody.ToLower();

            string[] KeywordList = dictKeyword[searchfor];
            int startindex = 0;
            try
            {
                for (int i = 0; i < KeywordList.Length; i++)
                {
                    if (EmailBody.Contains(KeywordList[i]))
                    {
                        startindex = EmailBody.IndexOf(KeywordList[i]) + KeywordList[i].Length;

                        for (int j = startindex; j < EmailBody.Length; j++)
                        {
                            if (char.IsDigit(EmailBody[j]))
                            {
                                strRetreivedID += EmailBody[j];
                            }
                            else
                            {
                                break;
                            }

                        }

                    }
                }

                RetrivedID = !string.IsNullOrEmpty(strRetreivedID) ? Convert.ToInt32(strRetreivedID) : 0 ;
            }
            catch (Exception ex)
            {
                errorlogs.SendErrorToText(ex,ConStrings);
            }

            return RetrivedID;
        }

        #endregion


        #region CHECK IF CUSTOMER EXISTS

        /// <summary>
        /// check if customer exists: if yes then return cust ID else create new customer and return new cust ID
        /// </summary>
        /// 
        public int IsCustomerExists(int tenantId, string EmailID,int CustomerId,string CustomerName,string ConStrings)
        {
            DataSet ds = new DataSet();
            int CustomerID = 0;
            try
            {
                MySqlConnection conn = new MySqlConnection(ConStrings);

                if (conn != null && conn.State == ConnectionState.Closed)
                {
                    conn.Open();
                }
                cmd.Connection = conn;
                MySqlCommand cmd1 = new MySqlCommand("SP_ValidateCustomerEmailID", conn);
                cmd1.CommandType = CommandType.StoredProcedure;
                cmd1.Parameters.AddWithValue("@_tenantID", tenantId);
                cmd1.Parameters.AddWithValue("@_emailId", EmailID);
                cmd1.Parameters.AddWithValue("@_customerName", string.IsNullOrEmpty(CustomerName) ? "": CustomerName);
                cmd1.Parameters.AddWithValue("@_custID", CustomerId); 
                MySqlDataAdapter da = new MySqlDataAdapter();
                da.SelectCommand = cmd1;
                da.Fill(ds);
                if (ds != null && ds.Tables[0] != null)
                {
                    if (ds.Tables[0] != null && ds.Tables[0].Rows.Count > 0)
                    {
                        CustomerID = Convert.ToInt32(ds.Tables[0].Rows[0]["CustomerID"]);
                    }

                }
            }
            catch (Exception ex)
            {
                errorlogs.SendErrorToText(ex, ConStrings);
            }
            finally
            {
                if (ds != null)
                    ds.Dispose();
                conn.Close();
            }

            return CustomerID;
        }

        #endregion


        #region CREATE TICKET BASED ON CUSTOMERID

        public int CreateTicket(int TenantId,int Customerid,int TicketId,string EmailID, string MailSubject, string MailDescription,string Attachment,string ConStrings)
        {
            int TicketID = 0; short isfalse = 0; short istrue = 1;
            int TicketCreationCount = 0;

            try
            {
                MySqlConnection conn = new MySqlConnection(ConStrings);
                if (conn != null && conn.State == ConnectionState.Closed)
                {
                    conn.Open();
                }

                cmd.Connection = conn;
                MySqlCommand cmd1 = new MySqlCommand("SP_ValidateTicketByCustomerID", conn);
                cmd1.Parameters.AddWithValue("@_TicketID", TicketId);
                cmd1.Parameters.AddWithValue("@_TenantID", TenantId);
                cmd1.Parameters.AddWithValue("@_TikcketTitle", string.IsNullOrEmpty(MailSubject)? "" : MailSubject);
                cmd1.Parameters.AddWithValue("@_TicketDescription", string.IsNullOrEmpty(MailDescription) ? "" : MailDescription);
                cmd1.Parameters.AddWithValue("@_TicketSourceID", 2); //ticketsourceID 2 for EMAIL
                cmd1.Parameters.AddWithValue("@_BrandID", null); //testing
                cmd1.Parameters.AddWithValue("@_CategoryID", null);//testing
                cmd1.Parameters.AddWithValue("@_SubCategoryID", null);//testing
                cmd1.Parameters.AddWithValue("@_PriorityID", null);//testing
                cmd1.Parameters.AddWithValue("@_CustomerID", Customerid);
                cmd1.Parameters.AddWithValue("@_OrderMasterID", null);//testing
                cmd1.Parameters.AddWithValue("@_IssueTypeID", null);//testing
                cmd1.Parameters.AddWithValue("@_ChannelOfPurchaseID", 30);//testing
                //need to change as per TicketActionType ID[QB / ETA]
                cmd1.Parameters.AddWithValue("@_AssignedID", null); //for test
                cmd1.Parameters.AddWithValue("@_TicketActionID", null);//testing

                cmd1.Parameters.AddWithValue("@_StatusID", 101); //for new ticket status : 101

                cmd1.Parameters.AddWithValue("@_TicketTemplateID", null);//testing

                cmd1.Parameters.AddWithValue("@_CreatedBy", null); //add admin ID here from UserMaster
                cmd1.Parameters.AddWithValue("@_Notes", "Ticket Creation By Console");

                cmd1.Parameters.AddWithValue("@_IsInstantEscalateToHighLevel", isfalse);
                cmd1.Parameters.AddWithValue("@_IsWantToVisitedStore", isfalse);
                cmd1.Parameters.AddWithValue("@_IsAlreadyVisitedStore", isfalse);
                cmd1.Parameters.AddWithValue("@_IsWantToAttachOrder", isfalse);
                cmd1.Parameters.AddWithValue("@_IsActive",isfalse);
                cmd1.Parameters.AddWithValue("@_OrderItemID", "");
                cmd1.Parameters.AddWithValue("@_StoreID", "");
                cmd1.Parameters.AddWithValue("@_HasAttachment", Convert.ToInt16(!string.IsNullOrEmpty(Attachment)));

                cmd1.CommandType = CommandType.StoredProcedure;

                TicketID = Convert.ToInt32(cmd1.ExecuteScalar());

                if (TicketID > 0)
                {
                    TicketCreationCount++;

                    if(!string.IsNullOrEmpty(Attachment))
                    { 
                    MySqlCommand cmdattachment = new MySqlCommand("SP_SaveAttachment", conn);
                    cmdattachment.Parameters.AddWithValue("@fileName", Attachment);
                    cmdattachment.Parameters.AddWithValue("@Ticket_ID", TicketID);
                    cmdattachment.CommandType = CommandType.StoredProcedure;
                    int _a = cmdattachment.ExecuteNonQuery();
                    }

                    if (!TicketID.Equals(TicketId) )
                    { 
                        if(!string.IsNullOrEmpty(MailDescription))
                        {
                            MySqlCommand cmdMail = new MySqlCommand("SP_SendTicketingEmail", conn);
                            cmdMail.Parameters.AddWithValue("@Tenant_ID", TenantId);
                            cmdMail.Parameters.AddWithValue("@Ticket_ID", TicketID);
                            cmdMail.Parameters.AddWithValue("@TikcketMail_Subject", MailSubject);
                            cmdMail.Parameters.AddWithValue("@TicketMail_Body", MailDescription);
                            cmdMail.Parameters.AddWithValue("@To_Email", EmailID);
                            cmdMail.Parameters.AddWithValue("@User_CC", "");
                            cmdMail.Parameters.AddWithValue("@User_BCC", "");
                            cmdMail.Parameters.AddWithValue("@Ticket_Source", 2); //ticketsourceID 2 for EMAIL
                            cmdMail.Parameters.AddWithValue("@Alert_ID", null);
                            cmdMail.Parameters.AddWithValue("@Is_Sent", istrue);
                            cmdMail.Parameters.AddWithValue("@Priority_ID", null);
                            cmdMail.Parameters.AddWithValue("@Created_By", null); //add admin ID here from UserMaster
                            cmdMail.Parameters.AddWithValue("@Has_Attachment", !string.IsNullOrEmpty(Attachment) ? Convert.ToInt16(1) : Convert.ToInt16(0));
                            cmdMail.CommandType = CommandType.StoredProcedure;
                            int a = Convert.ToInt32(cmdMail.ExecuteScalar());
                        }
                      

                    }
                }

            }
            catch (Exception ex)
            {
                errorlogs.SendErrorToText(ex,ConStrings);
            }
            finally
            {
                conn.Close();
            }

            //return TicketID;

            return TicketCreationCount;

        }

        #endregion

        
    }
}
