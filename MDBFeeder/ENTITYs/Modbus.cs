using MDBFeeder.TOOLS;
using MDBLib;
using MDBLib.HEADERS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Workstation.ServiceModel.Ua;

namespace MDBFeeder.ENTITYs
{
    public enum ConfigType
    {
        MDBSerial = 101,
        MDBTCP,
        MDBSerialViaTCP
    }
    public class ModbusConfig
    {
        public ConfigType cfgType { get; set; }
    }
    public class ModbusConfigSerial: ModbusConfig
    {
        public SerialSettings settings { get; set; }
    }
    public class ModbusConfigTCP: ModbusConfig
    {
        public string server { get; set; }
        public int port { get; set; }
        public int timeout { get; set; }
    }
    public class ModbusConfigSerialViaTCP: ModbusConfigTCP { }
    public class ModbusDeviceDescriptor
    {
        public ModbusConfig config { get; set; }
        public List<TAG> tags { get; set; }
    }
    public class ModbusOPCFeeder
    {
        private ModbusDeviceDescriptor device;
        private Commutation commutation = null;
        private OPCUAtool opc;
        private int pollDelay = 0;
        private byte[] TagGetRAWData(TAG tag,Modbus mdb)
        {
            switch(tag.type)
            {
                case TAGtype.uint16: 
                    {
                        ushort val = 0;
                        if(!mdb.GetUINT16(tag.hardAddr, out val, tag.devAddr))
                        {
                            HumbleLogger.Instance.AddMultipleErrors(mdb.GetErrorsList());
                        }
                        return BitConverter.GetBytes(val);
                    }
                case TAGtype.int16: 
                    {
                        short val = unchecked((short)0xDEAD);
                        if(!mdb.GetINT16(tag.hardAddr, out val, tag.devAddr))
                        {
                            HumbleLogger.Instance.AddMultipleErrors(mdb.GetErrorsList());
                        }
                        return BitConverter.GetBytes(val);
                    }
                case TAGtype.uint32: 
                    {
                        uint val = 0;
                        if(!mdb.GetUINT32(tag.hardAddr, out val, tag.devAddr))
                        {
                            HumbleLogger.Instance.AddMultipleErrors(mdb.GetErrorsList());
                        }
                        return BitConverter.GetBytes(val);
                    }
                case TAGtype.int32: 
                    {
                        int val = unchecked((int)0xDEADFFFF);
                        if(!mdb.GetINT32(tag.hardAddr, out val, tag.devAddr))
                        {
                            HumbleLogger.Instance.AddMultipleErrors(mdb.GetErrorsList());
                        }
                        return BitConverter.GetBytes(val);
                    }
                case TAGtype.uint64:
                    {
                        ulong val = 0;
                        if(!mdb.GetUINT64(tag.hardAddr, out val, tag.devAddr))
                        {
                            HumbleLogger.Instance.AddMultipleErrors(mdb.GetErrorsList());
                        }
                        return BitConverter.GetBytes(val);
                    }
                case TAGtype.int64:
                    {
                        long val = 0;
                        if(mdb.GetINT64(tag.hardAddr, out val, tag.devAddr))
                        {
                            HumbleLogger.Instance.AddMultipleErrors(mdb.GetErrorsList());
                        }
                        return BitConverter.GetBytes(val);
                    }
                case TAGtype.float32: 
                    {
                        float val = 0;
                        if(!mdb.GetFLOAT(tag.hardAddr, out val, tag.devAddr))
                        {
                            HumbleLogger.Instance.AddMultipleErrors(mdb.GetErrorsList());
                        }
                        return BitConverter.GetBytes(val);
                    }
                case TAGtype.float64: 
                    {
                        double val = 0;
                        if(!mdb.GetDOUBLE(tag.hardAddr, out val, tag.devAddr))
                        {
                            HumbleLogger.Instance.AddMultipleErrors(mdb.GetErrorsList());
                        }
                        return BitConverter.GetBytes(val);
                    }
                default:
                    return null;
            }
        }
        private void StartCommunication()
        {
            switch(device.config.cfgType)
            {
                case ENTITYs.ConfigType.MDBSerial:
                    {
                        ModbusConfigSerial config = (ModbusConfigSerial)device.config;
                        commutation = new Commutation(config.settings);
                    }
                    break;
                case ENTITYs.ConfigType.MDBTCP:
                    {
                        ModbusConfigTCP config = (ModbusConfigTCP)device.config;
                        pollDelay = config.timeout;
                        commutation = new Commutation(config.server, config.port,config.timeout, false);
                    }
                    break;
                case ENTITYs.ConfigType.MDBSerialViaTCP:
                    {
                        ModbusConfigSerialViaTCP config = (ModbusConfigSerialViaTCP)device.config;
                        pollDelay = config.timeout;
                        commutation = new Commutation(config.server, config.port, config.timeout, true);
                    }
                    break;
            }
        }
        public ModbusOPCFeeder(ModbusDeviceDescriptor device,string OPCuser,string opcPassword)
        {
            this.device = device;
            opc = new OPCUAtool(OPCuser, opcPassword);
            opc.Connect();
            StartCommunication();
        }
        public void Update()
        {
            if(commutation!=null)
            {
                Modbus mdb = new Modbus(commutation);
                foreach (TAG tag in device.tags)
                {
                    tag.rawValueDevice = TagGetRAWData(tag, mdb);
                    Console.WriteLine(tag.OPCid);
                    //Console.WriteLine(BitConverter.ToSingle(tag.rawValueDevice));
                    bool needUpdDev = false;
                    if(opc.IsConnected())
                    {
                        Thread.Sleep(pollDelay);
                        switch (tag.type)
                        {
                            case TAGtype.uint16:
                                {
                                    ushort opcVal = (ushort)opc.ReadTAG(tag.OPCid);
                                    if (tag.rawValueOPC == null)
                                    {
                                        tag.rawValueOPC = BitConverter.GetBytes(opcVal);
                                        tag.wdStamp = DateTime.Now;
                                        //needUpdDev = true;
                                    }
                                    else
                                    {
                                        if (BitConverter.ToUInt16(tag.rawValueOPC) != opcVal)
                                        {
                                            tag.rawValueOPC = BitConverter.GetBytes(opcVal);
                                            tag.wdStamp = DateTime.Now;
                                            needUpdDev = true;
                                        }
                                    }
                                    if (needUpdDev && tag.access == TAGaccess.RW)
                                    {
                                        Console.WriteLine("OPC " + opcVal.ToString());
                                        Console.WriteLine("RAW " + BitConverter.ToUInt16(tag.rawValueOPC).ToString());
                                        if (!mdb.SetUINT16(tag.hardAddr, opcVal, tag.devAddr))
                                        {
                                            List<string> errors = mdb.GetErrorsList();
                                            HumbleLogger.Instance.ErrorMessage(errors[errors.Count-1]);
                                        }
                                    }
                                    else
                                    {
                                        opc.WriteTAG(tag.OPCid, new DataValue(BitConverter.ToUInt16(tag.rawValueDevice)));
                                    }
                                }
                                break;
                            case TAGtype.int16:
                                {
                                    short opcVal = (short)opc.ReadTAG(tag.OPCid);
                                    if (tag.rawValueOPC == null)
                                    {
                                        tag.rawValueOPC = BitConverter.GetBytes(opcVal);
                                        tag.wdStamp = DateTime.Now;
                                        //needUpdDev = true;
                                    }
                                    else
                                    {
                                        if (BitConverter.ToInt16(tag.rawValueOPC) != opcVal)
                                        {
                                            tag.rawValueOPC = BitConverter.GetBytes(opcVal);
                                            tag.wdStamp = DateTime.Now;
                                            needUpdDev = true;
                                        }
                                    }
                                    if (needUpdDev && tag.access == TAGaccess.RW)
                                    {
                                        if(!mdb.SetINT16(tag.hardAddr, opcVal, tag.devAddr))
                                        {
                                            List<string> errors = mdb.GetErrorsList();
                                            HumbleLogger.Instance.ErrorMessage(errors[errors.Count - 1]);
                                        }
                                    }
                                    else
                                    {
                                        opc.WriteTAG(tag.OPCid, new DataValue(BitConverter.ToInt16(tag.rawValueDevice)));
                                    }
                                }
                                break;
                            case TAGtype.int32:
                                {
                                    int opcVal = (int)opc.ReadTAG(tag.OPCid);
                                    if (tag.rawValueOPC == null)
                                    {
                                        tag.rawValueOPC = BitConverter.GetBytes(opcVal);
                                        tag.wdStamp = DateTime.Now;
                                        //needUpdDev = true;
                                    }
                                    else
                                    {
                                        if (BitConverter.ToInt32(tag.rawValueOPC) != opcVal)
                                        {
                                            tag.rawValueOPC = BitConverter.GetBytes(opcVal);
                                            tag.wdStamp = DateTime.Now;
                                            needUpdDev = true;
                                        }
                                    }
                                    if (needUpdDev && tag.access == TAGaccess.RW)
                                    {
                                        if(!mdb.SetINT32(tag.hardAddr, opcVal, tag.devAddr))
                                        {
                                            List<string> errors = mdb.GetErrorsList();
                                            HumbleLogger.Instance.ErrorMessage(errors[errors.Count - 1]);
                                        }
                                    }
                                    else
                                    {
                                        opc.WriteTAG(tag.OPCid, new DataValue(BitConverter.ToInt32(tag.rawValueDevice)));
                                    }
                                }
                                break;
                            case TAGtype.uint32:
                                {
                                    uint opcVal = (uint)opc.ReadTAG(tag.OPCid);
                                    if (tag.rawValueOPC == null)
                                    {
                                        tag.rawValueOPC = BitConverter.GetBytes(opcVal);
                                        tag.wdStamp = DateTime.Now;
                                        //needUpdDev = true;
                                    }
                                    else
                                    {
                                        if (BitConverter.ToUInt32(tag.rawValueOPC) != opcVal)
                                        {
                                            tag.rawValueOPC = BitConverter.GetBytes(opcVal);
                                            tag.wdStamp = DateTime.Now;
                                            needUpdDev = true;
                                        }
                                    }
                                    if (needUpdDev && tag.access == TAGaccess.RW)
                                    {
                                        if(!mdb.SetUINT32(tag.hardAddr, opcVal, tag.devAddr))
                                        {
                                            List<string> errors = mdb.GetErrorsList();
                                            HumbleLogger.Instance.ErrorMessage(errors[errors.Count - 1]);
                                        }
                                    }
                                    else
                                    {
                                        opc.WriteTAG(tag.OPCid, new DataValue(BitConverter.ToUInt32(tag.rawValueDevice)));
                                    }
                                }
                                break;
                            case TAGtype.int64:
                                {
                                    long opcVal = (long)opc.ReadTAG(tag.OPCid);
                                    if (tag.rawValueOPC == null)
                                    {
                                        tag.rawValueOPC = BitConverter.GetBytes(opcVal);
                                        tag.wdStamp = DateTime.Now;
                                        //needUpdDev = true;
                                    }
                                    else
                                    {
                                        if (BitConverter.ToInt64(tag.rawValueOPC) != opcVal)
                                        {
                                            tag.rawValueOPC = BitConverter.GetBytes(opcVal);
                                            tag.wdStamp = DateTime.Now;
                                            needUpdDev = true;
                                        }
                                    }
                                    if (needUpdDev && tag.access == TAGaccess.RW)
                                    {
                                        if(!mdb.SetINT64(tag.hardAddr, opcVal, tag.devAddr))
                                        {
                                            List<string> errors = mdb.GetErrorsList();
                                            HumbleLogger.Instance.ErrorMessage(errors[errors.Count - 1]);
                                        }
                                    }
                                    else
                                    {
                                        opc.WriteTAG(tag.OPCid, new DataValue(BitConverter.ToInt64(tag.rawValueDevice)));
                                    }
                                }
                                break;
                            case TAGtype.float32:
                                {
                                    float opcVal = (float)opc.ReadTAG(tag.OPCid);
                                    if (tag.rawValueOPC == null)
                                    {
                                        tag.rawValueOPC = BitConverter.GetBytes(opcVal);
                                        tag.wdStamp = DateTime.Now;
                                        //needUpdDev = true;
                                    }
                                    else
                                    {
                                        if (BitConverter.ToSingle(tag.rawValueOPC) != opcVal)
                                        {
                                            tag.rawValueOPC = BitConverter.GetBytes(opcVal);
                                            tag.wdStamp = DateTime.Now;
                                            needUpdDev = true;
                                        }
                                    }
                                    if (needUpdDev && tag.access == TAGaccess.RW)
                                    {
                                        if(mdb.SetFLOAT(tag.hardAddr, opcVal, tag.devAddr))
                                        {
                                            List<string> errors = mdb.GetErrorsList();
                                            HumbleLogger.Instance.ErrorMessage(errors[errors.Count - 1]);
                                        }
                                    }
                                    else
                                    {
                                        opc.WriteTAG(tag.OPCid, new DataValue(BitConverter.ToSingle(tag.rawValueDevice)));
                                    }
                                }
                                break;
                            case TAGtype.float64:
                                {
                                    double opcVal = (double)opc.ReadTAG(tag.OPCid);
                                    if (tag.rawValueOPC == null)
                                    {
                                        tag.rawValueOPC = BitConverter.GetBytes(opcVal);
                                        tag.wdStamp = DateTime.Now;
                                        //needUpdDev = true;
                                    }
                                    else
                                    {
                                        if (BitConverter.ToDouble(tag.rawValueOPC) != opcVal)
                                        {
                                            tag.rawValueOPC = BitConverter.GetBytes(opcVal);
                                            tag.wdStamp = DateTime.Now;
                                            needUpdDev = true;
                                        }
                                    }
                                    if (needUpdDev && tag.access == TAGaccess.RW)
                                    {
                                        if(!mdb.SetDOUBLE(tag.hardAddr, opcVal, tag.devAddr))
                                        {
                                            List<string> errors = mdb.GetErrorsList();
                                            HumbleLogger.Instance.ErrorMessage(errors[errors.Count - 1]);
                                        }
                                    }
                                    else
                                    {
                                        opc.WriteTAG(tag.OPCid, new DataValue(BitConverter.ToDouble(tag.rawValueDevice)));
                                    }
                                }
                                break;
                        }
                    }   
                }
            }
        }
    }
}
