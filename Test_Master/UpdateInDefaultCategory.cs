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
    class UpdateInDefaultCategory
    {
        private int setCount = 0;
        private AsyncCall call;

        public UpdateInDefaultCategory(int count, Master master)
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
                UserInfo ui = new UserInfo() { Name = "DefaultCategory", age = 100, Prov = "This is a test of an object blah blah es, serialization does not seem to slow things down so much.  The gzip compression is ", Data = new byte[rd.Next(200, 9058)] };
                List<byte[]> data = Function.GetBytesFromSerializableObj(ui, ConstParams.BufferBlockSize, ref size);

                while (this.setCount-- > 0)
                {
                    try
                    {
                        ui.Name = "DefaultCategory" + this.setCount;
                        ICacheDataType key = new CacheSerializableObject(ui.Name, ui, data, size);
                        if (master.Update(key))
                            Logger.WriteLog("UpdateInDefaultCategory", string.Format("UpdateInDefaultCategory: category {0}  key: {1}  DataLength: {2}", key.Category, ui.Name, ui.Data.Length));
                        else
                            Logger.WriteLog("UpdateInDefaultCategory", "UpdateInDefaultCategory: null");
                    }
                    catch (Exception err)
                    {
                        Logger.WriteLog("UpdateInDefaultCategory", "error UpdateInDefaultCategory[Outter]:" + err.Message);
                        failCount++;
                    }
                }
                sp.Stop();

                Console.WriteLine("UpdateInDefaultCategory finished, failed:" + failCount + "  time(ms):" + sp.ElapsedMilliseconds);
                Logger.WriteLog("UpdateInDefaultCategory finished, failed:" + failCount + "  time(ms):" + sp.ElapsedMilliseconds);

            }
            catch (Exception ex)
            {
                Console.WriteLine("err:" + ex.Message + ex.StackTrace);
            }
        }
    }
}
