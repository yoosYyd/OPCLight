using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchiverII.SHARED
{
    public enum LOGINNG_TYPE
    {
        BY_TIME = 100,
        ON_CHANGE,
        BY_START_CHANGING,
        BY_END_CHANGING
    }
    public class TAG
    {
        public string ID { get; set; }
        public LOGINNG_TYPE type { get; set; }
        public string value { get; set; }
        public string module { get; set; }
        public string DBconn { get; set; }
        public string OPCtype { get; set; }
    }
    public class timeTAG: TAG
    {
        public int interval { get; set; }
    }
}
