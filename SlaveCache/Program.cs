using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using ZdCache.Common;

namespace ZdCache.SlaveCache
{
    class Program
    {
        static void Main(string[] args)
        {
            SlaveCfg cfg = ConfigurationManager.GetSection("slaveCfg") as SlaveCfg;
            Slave slave = new Slave(cfg);
            Console.ReadLine();
        }
    }
}
