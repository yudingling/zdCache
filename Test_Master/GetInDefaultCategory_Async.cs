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
    class GetInDefaultCategory_Async
    {
        private int setCount = 0;
        private AsyncCall call;

        public GetInDefaultCategory_Async(int count, Master master)
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
                UserInfo ui = new UserInfo() { Name = "DefaultCategory" };
                List<byte[]> data = Function.GetBytesFromSerializableObj(ui, ConstParams.BufferBlockSize, ref size);

                Stopwatch sp = new Stopwatch();
                sp.Start();
                long failCount = 0;

                int tempCount = this.setCount;

                while (tempCount-- > 0)
                {
                    try
                    {
                        ui.Name = "DefaultCategory" + rd.Next(1, this.setCount);
                        ICacheDataType key = new CacheSerializableObject(ui.Name, ui, data, size);

                        master.GetAsync(key).Then((SuccessInMaster)((obj) =>
                        {
                            UserInfo temp = obj[0].RealObj as UserInfo;
                            if (obj != null)
                                Logger.WriteLog("GetInDefaultCategory", string.Format("GetInDefaultCategory: category {0}  key: {1}  DataLength: {2}", obj[0].Category, temp.Name, temp.Data.Length));
                            else
                                Logger.WriteLog("GetInDefaultCategory", "GetInDefaultCategory: null");
                        }), (FailInMaster)((err) =>
                        {
                            Logger.WriteLog("GetInDefaultCategory", "error GetInDefaultCategory:" + err.Message);
                            failCount++;
                        })).Then((SuccessInMaster)((obj) =>
                        {
                            Logger.WriteLog("xxxxx", "success 2");
                        }));
                    }
                    catch (Exception err)
                    {
                        Logger.WriteLog("GetInDefaultCategory", "error GetInDefaultCategory[Outter]:" + err.Message + " " + err.StackTrace);
                        failCount++;
                    }
                }
                sp.Stop();

                Console.WriteLine("GetInDefaultCategory finished, failed:" + failCount + "  time(ms):" + sp.ElapsedMilliseconds);
                Logger.WriteLog("GetInDefaultCategory finished, failed:" + failCount + "  time(ms):" + sp.ElapsedMilliseconds);

            }
            catch (Exception ex)
            {
                Console.WriteLine("err:" + ex.Message + ex.StackTrace);
            }
        }
    }
}
