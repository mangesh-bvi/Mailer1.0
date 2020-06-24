using MailConsole;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace MailerConsole
{
    public class ErrorLogs
    {
        public int UserID { get; set; }
        public int TenantID { get; set; }
        public string ControllerName { get; set; }
        public string ActionName { get; set; }
        public string Exceptions { get; set; }
        public string MessageException { get; set; }
        public string IPAddress { get; set; }

        public static MySqlConnection conn = null;


        static ErrorLogs()
        {
            MySettingsConfigMoal mysettingsconfigmoal = new MySettingsConfigMoal();
            Program obj = new Program();
            mysettingsconfigmoal = obj.GetConfigDetails();

            conn = new MySqlConnection(mysettingsconfigmoal.Connectionstring);

        }

        #region Exception

        public void SendErrorToText(Exception ex)
        {
            String ErrorlineNo, Errormsg, extype, ErrorLocation;

            var line = Environment.NewLine + Environment.NewLine;

            ErrorlineNo = ex.StackTrace.Substring(ex.StackTrace.Length - 7, 7);
            Errormsg = ex.GetType().Name.ToString();
            extype = ex.GetType().ToString();
            ErrorLocation = ex.Message.ToString();

            try
            {


                ErrorLogs errorLogs = new ErrorLogs
                {
                    ActionName = "Ticketing Job",
                    ControllerName = "Ticketing Job",
                    TenantID = 0,
                    UserID = 0,
                    Exceptions = ex.StackTrace,
                    MessageException = ex.Message,
                    IPAddress = ""
                };

                InsertErrorLog(errorLogs);

            }
            catch (Exception e)
            {
                e.ToString();
            }
        }

        public void FileText(string Text)
        {

            var line = Environment.NewLine + Environment.NewLine;

            try
            {
                ErrorLogs errorLogs = new ErrorLogs
                {
                    ActionName = "Ticketing Job",
                    ControllerName = "Ticketing Job Steps",
                    TenantID = 0,
                    UserID = 0,
                    Exceptions = Text,
                    MessageException = "",
                    IPAddress = ""
                };

                //if (mysettingsconfigmoal.IsWriteLog == "1")
                {
                    InsertErrorLog(errorLogs);
                }
            }
            catch (Exception e)
            {
                e.ToString();
            }
        }

        public int InsertErrorLog(ErrorLogs errorLog)
        {
            int Success = 0;
            try
            {

                conn.Open();
                MySqlCommand cmd = new MySqlCommand("SP_ErrorLog", conn);
                cmd.Connection = conn;
                cmd.Parameters.AddWithValue("@User_ID", errorLog.UserID);
                cmd.Parameters.AddWithValue("@Tenant_ID", errorLog.TenantID);
                cmd.Parameters.AddWithValue("@Controller_Name", errorLog.ControllerName);
                cmd.Parameters.AddWithValue("@Action_Name", errorLog.ActionName);
                cmd.Parameters.AddWithValue("@_Exceptions", errorLog.Exceptions);
                cmd.Parameters.AddWithValue("@_MessageException", errorLog.MessageException);
                cmd.Parameters.AddWithValue("@_IPAddress", errorLog.IPAddress);
                cmd.CommandType = CommandType.StoredProcedure;
                Success = Convert.ToInt32(cmd.ExecuteNonQuery());
                conn.Close();

            }
            catch (Exception ex)
            {

                throw ex;
            }
            finally
            {
                if (conn != null)
                {
                    conn.Close();
                }
            }
            return Success;
        }

        #endregion

    }
}
