using ArchiverII.SHARED;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ArchiverII.CODE
{
    public class ConfigLoader
    {
        private string configPath = ""/*"C:\\Users\\user\\source\\repos\\OPCLight\\Release\\config.json"*/;
        private string OPCuser = "";
        private string OPCpass = "";

        private List<TAG> subscribeList = new List<TAG>();

        private string PureficateDBString(string input)
        {
            string ret = input.Replace("'", "");
            string[] splitArr = ret.Split(";");
            ret = "";
            foreach (string str in splitArr)
            {
                //Console.WriteLine(str);
                if (str.Contains("Data Source=") || str.Contains("User ID=") || str.Contains("Password=") ||
                    str.Contains("Initial Catalog="))
                {
                    ret = ret + str + ";";
                }
            }
            return ret;
        }
        public ConfigLoader()
        {
            configPath = Directory.GetParent(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName)) + "\\config.json";
            Console.WriteLine(configPath);
            Parse();
        }
        public void GetOPCcredentials(out string user,out string pass)
        {
            user = OPCuser;
            pass = OPCpass;
        }
        public List<TAG> GetListOnWatch()
        {
            return subscribeList;
        }
        private void Parse()
        {
            string json = File.ReadAllText(configPath);
            JsonDocument jd = JsonDocument.Parse(json);
            JsonElement users;
            JsonElement feeders;
            if (jd.RootElement.TryGetProperty("USERS", out users))
            {
                foreach (var el in users.EnumerateObject())
                {
                    if (el.Value.GetProperty("accesslvl").GetInt16() == 1)
                    {
                        OPCuser = el.Name;
                        OPCpass = el.Value.GetProperty("pass").GetString();
                        break;
                    }
                }
            }
            Console.WriteLine(OPCuser + ":" + OPCpass);
            if (jd.RootElement.TryGetProperty("FEEDERS", out feeders))
            {
                foreach (var el in feeders.EnumerateObject())
                {
                    if (el.Value.GetProperty("SETTINGS").GetProperty("LOGGING").GetProperty("IsEnabled").GetBoolean())
                    {
                        //Console.WriteLine(el.Name);
                        foreach(var group in el.Value.GetProperty("TAGS").EnumerateObject())
                        {
                            //Console.WriteLine(group.Name);
                            foreach(var tag in group.Value.EnumerateObject())
                            {
                                //Console.WriteLine(tag.Name);
                                if(tag.Value.GetProperty("logging").GetBoolean())
                                {
                                    TAG ptr;
                                    switch(tag.Value.GetProperty("loggingType").GetString())
                                    {
                                        case "bytime":
                                            {
                                                timeTAG tt = new timeTAG();
                                                tt.type = LOGINNG_TYPE.BY_TIME;
                                                tt.interval = el.Value.GetProperty("SETTINGS").GetProperty("LOGGING").
                                                    GetProperty("ArcIntervalSeconds").GetInt32();
                                                tt.ID = el.Name + "." + group.Name + "." + tag.Name;//OPCid
                                                tt.module = el.Name;
                                                tt.value = "";
                                                tt.DBconn = PureficateDBString(el.Value.GetProperty("SETTINGS").GetProperty("LOGGING").
                                                    GetProperty("DBconnString").GetString());
                                                tt.OPCtype = tag.Value.GetProperty("type").GetString();
                                                ptr = (TAG)tt;
                                                subscribeList.Add(ptr);
                                            }
                                            break;
                                        default:break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        //
    }
}
