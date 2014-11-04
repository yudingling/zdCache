using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZdCache.SlaveCache
{
    public class SlaveCfg
    {
        public class IpAndPort
        {
            public string IP { get; set; }
            public int Port { get; set; }

            public string ID { get { return this.IP + "_" + Port; } }
        }


        private Dictionary<String, IpAndPort> mastersList = new Dictionary<String, IpAndPort>();

        /// <summary>
        /// master info
        /// </summary>
        public Dictionary<String, IpAndPort> Masters { get { return this.mastersList; } }

        /// <summary>
        /// socket timeout(ms)
        /// </summary>
        public int RecvAndSendTimeout { get; set; }

        /// <summary>
        /// callback thread count
        /// </summary>
        public int CallBackThreadCount { get; set; }

        /// <summary>
        /// cache expire time(ms)
        /// </summary>
        public int CacheExpireTM { get; set; }

        /// <summary>
        /// max cached buffer size(byte)
        /// </summary>
        public long MaxCacheSize { get; set; }
    }
}
