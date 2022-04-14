using ArchiverII.SHARED;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchiverII.CODE
{
    public class SQLtool
    {
        private Dictionary<string/*db conn string is key*/, List<TAG>> byTimeTags = new Dictionary<string, List<TAG>>();
        private Dictionary<string/*the*/, int> timers = new Dictionary<string, int>();
        private Dictionary<string/*same*/, DateTime> lastRun = new Dictionary<string, DateTime>();

        private string connStr = "";
        private SqlConnection con = null;
        private TimeHelper timeHelper = new TimeHelper();
        private HumbleLogger _log = HumbleLogger.Instance;
        private void SetConn(string newConnStr)
        {
            if(con!=null && newConnStr.Equals(connStr))
            {
                return;
            }
            else
            {
                connStr = newConnStr;
                try
                {
                    if (con != null)
                    {
                        con.Close();
                    }

                    con = new SqlConnection(connStr);
                    con.Open();
                }
                catch (Exception ex)
                {
                    _log.ErrorMessage("SQL connection exeception: " + ex.Message);
                }
            }
            
        }
        public void PrepareTables(List<TAG> watchedList)
        {
            foreach(TAG tag in watchedList)
            {
                switch(tag.type)
                {
                    case LOGINNG_TYPE.BY_TIME:
                        {
                            if(!byTimeTags.ContainsKey(tag.DBconn))
                            {
                                byTimeTags[tag.DBconn] = new List<TAG>();
                                timeTAG tt = (timeTAG)tag;
                                timers[tag.DBconn] = tt.interval;
                                lastRun[tag.DBconn] = DateTime.Now;
                            }
                            byTimeTags[tag.DBconn].Add(tag);
                        }
                        break;
                    default:break;
                }    
            }
            Dictionary<string, List<TAG>>.KeyCollection DBs = byTimeTags.Keys;
            foreach (string dbStr in DBs)
            {
                SetConn(dbStr);
                PrepareByTimeTable(byTimeTags[dbStr]);
            }
        }
        private void PrepareByTimeTable(List<TAG> tags)
        {
            string sqlType = "";
            string req = "IF NOT EXISTS (SELECT * FROM sysobjects WHERE id = object_id(N'[dbo].["+ tags[0].module+ "_ByTime]')"
                + "AND OBJECTPROPERTY(id, N'IsUserTable') = 1) CREATE TABLE [dbo].[" + tags[0].module + "_ByTime] ([id] [int] NOT NULL UNIQUE";
            foreach(TAG tag in tags)
            {
                if(tag.OPCtype.Contains("float"))
                {
                    sqlType = "[float]";
                }
                if (tag.OPCtype.Contains("int"))
                {
                    sqlType = "[int]";
                }
                req = req + ",["+tag.ID+"] "+ sqlType+" NULL";
            }
            req = req + ")";
            //Console.WriteLine(req);
            SqlCommand command = con.CreateCommand();
            command.CommandText = req;
            try
            {
                command.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                Console.WriteLine("SQL exeception: " + ex.Message + " | " + command.CommandText);
                _log.ErrorMessage("SQL exeception: " + ex.Message + " | " + command.CommandText);
            }
        }
        private string BuildInsert(List<TAG> tags)
        {
            string ret = "INSERT INTO [dbo].[" + tags[0].module + "_ByTime] ([id]";
            foreach(TAG tag in tags)
            {
                ret = ret +",["+tag.ID+"]";
            }
            ret = ret + ") VALUES("+timeHelper.ConverDT2UNIX(DateTime.Now).ToString();
            foreach(TAG tag in tags)
            {
                ret = ret + "," + tag.value;
            }
            ret = ret + ")";
            return ret;
        }
        private void InsertByTime(string dbStr)
        {
            SetConn(dbStr);
            string insert = "";
            try
            {
                insert = BuildInsert(byTimeTags[dbStr]);
                SqlCommand command = con.CreateCommand();
                command.CommandText = insert;
                command.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                Console.WriteLine("SQL exeception: " + ex.Message + " | " + insert);
                _log.ErrorMessage("SQL exeception: " + ex.Message + " | " + insert);
            }
        }
        public void RunLogging(DateTime now)
        {
            Dictionary<string, List<TAG>>.KeyCollection DBs = byTimeTags.Keys;
            foreach (string dbStr in DBs)
            {
                Console.WriteLine((now - lastRun[dbStr]).TotalSeconds.ToString());
                if ( (now - lastRun[dbStr]).TotalSeconds>timers[dbStr])
                {
                    InsertByTime(dbStr);
                    lastRun[dbStr] = DateTime.Now;
                }
            }
        }
    }
}
