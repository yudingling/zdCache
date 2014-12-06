using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZdCache.Common;
using ZdCache.MasterCache;
using System.Diagnostics;
using ZdCache.Common.CacheCommon;
using ZdCache.Common.CDataType;

namespace Test_Master
{
    class DelInCategory1
    {
        private int setCount = 0;
        private AsyncCall call;

        public DelInCategory1(int count, Master master)
        {
            this.setCount = count;
            call = new AsyncCall(new AsyncMethod(this.Test), new AsyncArgs() { Args = master, ArgsAbort = null },
                false, null);
        }

        public void Start()
        {
            this.call.Start();
        }

        void Test(AsyncArgs arg)
        {
            try
            {
                Master master = arg.Args as Master;
                Random rd = new Random();

                UserInfo ui = new UserInfo() { Name = "Category1" };

                Stopwatch sp = new Stopwatch();
                sp.Start();
                long failCount = 0;

                int tempCount = this.setCount;

                while (tempCount-- > 0)
                {
                    try
                    {
                        ui.Name = "Category1" + rd.Next(1, this.setCount); 
                        ICacheDataType key = new CacheSerializableObject(ui.Name, ui);

                        if (master.Delete(key))
                            Logger.WriteLog("DelInCategory1", string.Format("DelInCategory1: category {0}  key: {1} ", key.Category, ui.Name));
                        else
                            Logger.WriteLog("DelInCategory1", "DelInCategory1: null");
                    }
                    catch (Exception err)
                    {
                        Logger.WriteLog("DelInCategory1", "error DelInCategory1:" + err.Message);
                        failCount++;
                    }
                }
                sp.Stop();

                Console.WriteLine("DelInCategory1 finished, failed:" + failCount + "  time(ms):" + sp.ElapsedMilliseconds);
                Logger.WriteLog("DelInCategory1 finished, failed:" + failCount + "  time(ms):" + sp.ElapsedMilliseconds);

            }
            catch (Exception ex)
            {
                Console.WriteLine("err:" + ex.Message + ex.StackTrace);
            }
        }
    }
}
