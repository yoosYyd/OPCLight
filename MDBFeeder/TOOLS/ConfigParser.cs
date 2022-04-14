using MDBFeeder.ENTITYs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MDBFeeder.TOOLS
{
    public class ConfigParser
    {
        private string configPath = ""/*"C:\\Users\\user\\source\\repos\\OPCLight\\Release\\mdbTest.json"*/;
        private string moduleName = "!!!test!!!";
        private string OPCuser = "";
        private string OPCpass = "";
        
        //dprivate string SQLconnStr = "";
        private List<ModbusDeviceDescriptor> devices = new List<ModbusDeviceDescriptor>();
        public ConfigParser()
        {
            configPath = Directory.GetParent(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName)) + "\\config.json";
            moduleName = AppDomain.CurrentDomain.BaseDirectory + "MDBFeeder.exe";
            Console.WriteLine(configPath);
            Console.WriteLine(moduleName);
            Parse();
        }
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
                    if(el.Value.GetProperty("accesslvl").GetInt16()==0)
                    {
                        OPCuser = el.Name;
                        OPCpass = el.Value.GetProperty("pass").GetString();
                        break;
                    }
                }
            }
            //Console.WriteLine(OPCuser + ":" + OPCpass);
            string _moduleName = "";
            if (jd.RootElement.TryGetProperty("FEEDERS", out feeders))
            {
                foreach (var el in feeders.EnumerateObject())
                {
                    _moduleName = el.Value.GetProperty("SETTINGS").GetProperty("ModuleLocation").GetString();
                    if (_moduleName.Equals(moduleName))
                    {
                        List<TAG> mdbTags = new List<TAG>();
                        ModbusConfig mdbConfig = null;
                        //SQLconnStr = PureficateDBString(el.Value.GetProperty("SETTINGS").GetProperty("LOGGING").GetProperty("DBconnString").GetString());
                        //Console.WriteLine(el.Value.GetProperty("SETTINGS").GetProperty("HARDWARE").GetProperty("TRANSPORT").ToString());
                        switch (el.Value.GetProperty("SETTINGS").GetProperty("HARDWARE").GetProperty("TRANSPORT").ToString())
                        {
                            case "SERIALviaTCP":
                                {
                                    ModbusConfigSerialViaTCP config = new ModbusConfigSerialViaTCP();
                                    config.cfgType = ConfigType.MDBSerialViaTCP;
                                    config.server =
                                        el.Value.GetProperty("SETTINGS").GetProperty("HARDWARE").GetProperty("REMOTE_ADDR").GetString();
                                    config.port =
                                        el.Value.GetProperty("SETTINGS").GetProperty("HARDWARE").GetProperty("REMOTE_PORT").GetInt32();
                                    config.timeout =
                                        el.Value.GetProperty("SETTINGS").GetProperty("HARDWARE").GetProperty("REQ_DELAY_MS").GetInt32();
                                    mdbConfig = (ModbusConfig)config;
                                }
                                break;
                            case "SERIAL":
                                {
                                    ModbusConfigSerial config = new ModbusConfigSerial();
                                    config.cfgType = ConfigType.MDBSerial;
                                    config.settings = new MDBLib.HEADERS.SerialSettings();
                                    config.settings.PortName =
                                        el.Value.GetProperty("SETTINGS").GetProperty("HARDWARE").GetProperty("PORT_NAME").GetString();
                                    config.settings.BaudRate =
                                        el.Value.GetProperty("SETTINGS").GetProperty("HARDWARE").GetProperty("BAUD_RATE").GetInt32();
                                    config.settings.DataBits =
                                        el.Value.GetProperty("SETTINGS").GetProperty("HARDWARE").GetProperty("DATA_BITS").GetInt32();
                                    config.settings.Parity =(System.IO.Ports.Parity)
                                        el.Value.GetProperty("SETTINGS").GetProperty("HARDWARE").GetProperty("PARITY").GetInt32();
                                    config.settings.stopBit = (System.IO.Ports.StopBits)
                                        el.Value.GetProperty("SETTINGS").GetProperty("HARDWARE").GetProperty("STOP_BIT").GetInt32();
                                    config.settings.ReadTimeout = 
                                        el.Value.GetProperty("SETTINGS").GetProperty("HARDWARE").GetProperty("R_TIMEOUT").GetInt32();
                                    config.settings.WriteTimeout =
                                        el.Value.GetProperty("SETTINGS").GetProperty("HARDWARE").GetProperty("W_TIMEOUT").GetInt32();
                                    mdbConfig = (ModbusConfig)config;
                                }
                                break;
                            case "TCP":
                                {
                                    ModbusConfigTCP config = new ModbusConfigTCP();
                                    config.cfgType = ConfigType.MDBTCP;
                                    config.server =
                                        el.Value.GetProperty("SETTINGS").GetProperty("HARDWARE").GetProperty("REMOTE_ADDR").GetString();
                                    config.port =
                                        el.Value.GetProperty("SETTINGS").GetProperty("HARDWARE").GetProperty("REMOTE_PORT").GetInt32();
                                    config.timeout =
                                        el.Value.GetProperty("SETTINGS").GetProperty("HARDWARE").GetProperty("REQ_DELAY_MS").GetInt32();
                                    mdbConfig = (ModbusConfig)config;
                                }
                                break;
                        }
                        foreach (var tags in el.Value.GetProperty("TAGS").EnumerateObject())
                        {
                            foreach (var tag in tags.Value.EnumerateObject())
                            {
                                //Console.WriteLine(el.Name+"."+tags.Name+"."+tag.Name);
                                TAG _tag = new TAG();
                                _tag.rdStamp = DateTime.Now;
                                _tag.wdStamp = _tag.rdStamp;
                                _tag.rawValueDevice = null;
                                _tag.rawValueOPC = null;
                                _tag.OPCid = el.Name + "." + tags.Name + "." + tag.Name;
                                _tag.hardAddr = (ushort)tag.Value.GetProperty("hwAdr").GetInt32();
                                _tag.devAddr = (byte)tag.Value.GetProperty("DEV_ADDR").GetInt32();
                                _tag.access = tag.Value.GetProperty("Acesses").GetString().Equals("RW") ? TAGaccess.RW: TAGaccess.R;
                                switch(tag.Value.GetProperty("type").GetString())
                                {
                                    case "uint16": { _tag.type = TAGtype.uint16; } break;
                                    case "int16": { _tag.type = TAGtype.int16; } break;
                                    case "uint32": { _tag.type = TAGtype.uint32; } break;
                                    case "int32": { _tag.type = TAGtype.int32; } break;
                                    case "uint64": { _tag.type = TAGtype.uint64; } break;
                                    case "int64": { _tag.type = TAGtype.int64; } break;
                                    case "float32": { _tag.type = TAGtype.float32; } break;
                                    case "float64": { _tag.type = TAGtype.float64; } break;
                                }
                                mdbTags.Add(_tag);
                            }
                        }
                        ModbusDeviceDescriptor dev = new ModbusDeviceDescriptor();
                        dev.config = mdbConfig;
                        dev.tags = mdbTags;
                        devices.Add(dev);
                    }
                }
            }
        }
        public void GetOPCParams(out string user,out string pass)
        {
            user = this.OPCuser;
            pass = this.OPCpass;
        }
        public List<ModbusDeviceDescriptor> GetModbusDevices()
        {
            return devices;
        }
    }
}
