using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MDBFeeder.ENTITYs
{
    public enum TAGaccess
    {
        R = 100,
        RW = 101
    }
    public enum TAGtype
    {
        uint16 = 100,
        int16,
        uint32,
        int32,
        uint64,
        int64,
        float32,
        float64
    }
    public class TAG
    {
        public string OPCid { get; set; }
        public ushort hardAddr { get; set; }
        public byte devAddr { get; set; }
        public TAGaccess access { get; set; }
        public TAGtype type { get; set; }
        public byte[] rawValueDevice { get; set; }
        public byte[] rawValueOPC { get; set; }
        public DateTime rdStamp { get; set; } 
        public DateTime wdStamp { get; set; }
    }
}
