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
    class SetInCategory1
    {
        private int setCount = 0;
        private AsyncCall call;

        public SetInCategory1(int count, Master master)
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
                long failCount = 0;

                Stopwatch sp = new Stopwatch();
                sp.Start();

                long size = 0;
                UserInfo ui = new UserInfo() { Name = "Category1", age = 27, Prov = "This is a test of an object blah blah es, serialization does not seem to slow things down so much.  The gzip compression is ", Data = new byte[rd.Next(200, 9058)] };
                List<byte[]> data = Function.GetBytesFromSerializableObj(ui, ConstParams.BufferBlockSize, ref size);

                while (this.setCount-- > 0)
                {
                    try
                    {
                        ui.Name = "Category1" + this.setCount;
                        ICacheDataType cache = new CacheSerializableObject(1, ui.Name, ui, data, size);
                        master.Set(cache);
                    }
                    catch(Exception err)
                    {
                        Logger.WriteLog("SetInCategory1", "error SetInCategory1[Outter]:" + err.Message);
                        failCount++;
                    }
                }
                sp.Stop();

                Console.WriteLine("SetInCategory1 finished, failed:" + failCount + "  time(ms):" + sp.ElapsedMilliseconds);
                Logger.WriteLog("SetInCategory1 finished, failed:" + failCount + "  time(ms):" + sp.ElapsedMilliseconds);

            }
            catch (Exception ex)
            {
                Console.WriteLine("SetInCategory1 err:" + ex.Message + ex.StackTrace);
            }
        }
    }
}
