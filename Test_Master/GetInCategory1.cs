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
    class GetInCategory1
    {
        private int setCount = 0;
        private AsyncCall call;

        public GetInCategory1(int count, Master master)
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

                long size = 0;
                UserInfo ui = new UserInfo() { Name = "Category1" };
                List<byte[]> data = Function.GetBytesFromSerializableObj(ui, ConstParams.BufferBlockSize, ref size);

                Stopwatch sp = new Stopwatch();
                sp.Start();
                long failCount = 0;

                int tempCount = this.setCount;

                while (tempCount-- > 0)
                {
                    try
                    {
                        ui.Name = "Category1" + rd.Next(1, this.setCount);
                        ICacheDataType key = new CacheSerializableObject(1, ui.Name, ui, data, size);

                        ICacheDataType cached = master.Get(key);
                        if (cached != null)
                        {
                            UserInfo temp = cached.RealObj as UserInfo;
                            Logger.WriteLog("GetInCategory1", string.Format("GetInCategory1: category {0}  key: {1}  DataLength: {2}", cached.Category, temp.Name, temp.Data.Length));
                        }
                        else
                            Logger.WriteLog("GetInCategory1", "GetInCategory1: null");
                    }
                    catch (Exception err)
                    {
                        Logger.WriteLog("GetInCategory1", "error GetInCategory1:" + err.Message);
                        failCount++;
                    }
                }
                sp.Stop();

                Console.WriteLine("GetInCategory1 finished, failed:" + failCount + "  time(ms):" + sp.ElapsedMilliseconds);
                Logger.WriteLog("GetInCategory1 finished, failed:" + failCount + "  time(ms):" + sp.ElapsedMilliseconds);

            }
            catch (Exception ex)
            {
                Console.WriteLine("err:" + ex.Message + ex.StackTrace);
            }
        }
    }
}
