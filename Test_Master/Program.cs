using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZdCache.MasterCache;
using ZdCache.Common;
using ZdCache.Common.CacheCommon;
using System.Diagnostics;
using ZdCache.Common.CDataType;

namespace Test_Master
{
    [Serializable]
    class UserInfo
    {
        public string Name { get; set; }
        public int age { get; set; }
        public string Prov { get; set; }
        public byte[] Data { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            int count = 10000;
            Master master = new Master(12998, 2000);

            SetInDefaultCategory set1 = new SetInDefaultCategory(count, master);
            //SetInCategory1 set2 = new SetInCategory1(count, master);

            GetInDefaultCategory_Async get1_Async = new GetInDefaultCategory_Async(count, master);
            //GetInCategory1 get2 = new GetInCategory1(count, master);

            Console.ReadLine();
            set1.Start();
            //set2.Start();
            Console.ReadLine();
            get1_Async.Start();
            //get2.Start();

            while (true)
            {
                Console.WriteLine("---------------------------");
                foreach (StatusInfo status in master.SlaveStatus)
                {
                    Console.WriteLine(string.Format("Slave:{0} {1} \n\r\t availMem:{2}  usedMem:{3} cachedMem:{4} cpuUseRate:{5} hitRate:{6}",
                        status.IP, status.MachineName,
                        status.AvailMem / 1024 / 1024, status.UsedMem / 1024 / 1024, status.CachedMem / 1024 / 1204,
                        status.CpuUseRateIn1Minutes, status.HitRate));
                }
                Console.WriteLine("---------------------------");
                Console.WriteLine("");

                SleepHelper.Sleep(3000);
            }
        }
    }
}
