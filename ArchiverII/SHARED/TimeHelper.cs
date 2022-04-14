using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchiverII.SHARED
{
    public class TimeHelper
    {
        public long GetPrevDayStartUnix()
        {
            DateTime dayDT = new DateTime(DateTime.Now.Year, DateTime.Now.Month,
                DateTime.Now.Day, 0, 0, 0, DateTime.Now.Kind);
            DateTime unixDT = new DateTime(1970, 1, 1, 0, 0, 0, DateTime.Now.Kind);
            return System.Convert.ToInt64((dayDT - unixDT).TotalSeconds);
        }
        public long ConvertPLCDT2Unix(UInt32 plcDT)
        {
            byte[] dtBuff = BitConverter.GetBytes(plcDT);
            Console.WriteLine(dtBuff[0]);
            Console.WriteLine(dtBuff[1]);
            Console.WriteLine(dtBuff[2]);
            Console.WriteLine(dtBuff[3]);
            DateTime plc = new DateTime(dtBuff[3] + 2000, dtBuff[2], dtBuff[1], dtBuff[0], 0, 0);
            DateTime unixDT = new DateTime(1970, 1, 1, 0, 0, 0, plc.Kind);
            return System.Convert.ToInt64((plc - unixDT).TotalSeconds);
        }
        public long ConverDT2UNIX(DateTime inputDT)
        {
            DateTime unixDT = new DateTime(1970, 1, 1, 0, 0, 0, inputDT.Kind);
            return System.Convert.ToInt64((inputDT - unixDT).TotalSeconds);
        }
        public uint ConvertUNIX2PLC(long unixDT)
        {
            uint ret = 0;
            DateTime start = new DateTime(1970, 1, 1, 0, 0, 0, DateTime.Now.Kind);
            start = start.AddSeconds(unixDT);
            byte[] dtBuff = new byte[4];
            dtBuff[0] = (byte)(start.Hour);
            dtBuff[1] = (byte)start.Day;
            dtBuff[2] = (byte)start.Month;
            dtBuff[3] = (byte)(start.Year - 2000);
            ret = BitConverter.ToUInt32(dtBuff);
            return ret;
        }
        public DateTime Unix2DT(long udt)
        {
            DateTime ret = new DateTime(1970, 1, 1, 0, 0, 0, DateTime.Now.Kind);
            ret = ret.AddSeconds(udt);
            return ret;
        }
    }
}
