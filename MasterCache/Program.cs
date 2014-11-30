using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using ZdCache.Common;
using ZdCache.Common.CacheCommon;
using ZdCache.Common.CDataType;
using System.Diagnostics;

namespace ZdCache.MasterCache
{
    [Serializable]
    class UserInfo
    {
        public string Name { get; set; }
        public int age { get; set; }
        public string Prov { get; set; }
        public byte[] Data { get; set; }
    }
    [Serializable]
    class CarInfo
    {
        public string CarName { get; set; }
        public string CarBrand { get; set; }
        public byte[] InfoSek { get; set; }
    }
    [Serializable]
    class HomeInfo
    {
        public string HomeId { get; set; }
        public string Address { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Master master = new Master(int.Parse(ConfigurationManager.AppSettings["port"].ToString().Trim()),
                int.Parse(ConfigurationManager.AppSettings["RevcAndSendTimeout"].ToString().Trim()));

            AsyncCall call1 = new AsyncCall(new AsyncMethod(Test1), new AsyncArgs() { Args = master, ArgsAbort = null }, true, null);
            //AsyncCall call2 = new AsyncCall(new AsyncMethod(Test2), new AsyncArgs() { Args = master, ArgsAbort = null }, true, null);

            AsyncCall call5 = new AsyncCall(new AsyncMethod(Test5), new AsyncArgs() { Args = master, ArgsAbort = null }, true, null);
            //AsyncCall call6 = new AsyncCall(new AsyncMethod(Test6), new AsyncArgs() { Args = master, ArgsAbort = null }, true, null);
            
            //AsyncCall call9 = new AsyncCall(new AsyncMethod(Test9), new AsyncArgs() { Args = master, ArgsAbort = null }, true, null);
            //AsyncCall call10 = new AsyncCall(new AsyncMethod(Test10), new AsyncArgs() { Args = master, ArgsAbort = null }, true, null);

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

        //set in default category 0
        static long i = 1;
        static void Test1(AsyncArgs arg)
        {
            try
            {
                SleepHelper.Sleep(10000);
                Master master = arg.Args as Master;
                Random rd = new Random();
                long failCount = 0;
                Stopwatch sp = new Stopwatch();
                sp.Start();

                long size = 0;
                UserInfo ui = new UserInfo() { Name = "左丹test1_x", age = 26, Prov = "This is a test of an object blah blah es, serialization does not seem to slow things down so much.  The gzip compression is ", Data = new byte[rd.Next(200, 9058)] };
                List<byte[]> data = Function.GetBytesFromSerializableObj(ui, ConstParams.BufferBlockSize, ref size);


                while (true)
                {
                    try
                    {
                        ui.Name = "左丹test1_" + i++;
                        ICacheDataType key = new CacheSerializableObject(ui.Name, ui, data, size);
                        if (!master.Set(key))
                            failCount++;
                    }
                    catch
                    {
                        failCount++;
                    }

                    if (i > 10000)
                    {
                        sp.Stop();
                        break;
                    }
                }

                Console.WriteLine("左丹test1_  set 10000 finished, failed:" + failCount + "  time(ms):" + sp.ElapsedMilliseconds);
                Logger.WriteLog("左丹test1_  set 10000 finished, failed:" + failCount + "  time(ms):" + sp.ElapsedMilliseconds);

            }
            catch (Exception ex)
            {
                Console.WriteLine("err:" + ex.Message + ex.StackTrace);
            }
        }

        //set in default category 1
        static long i2 = 1;
        static void Test2(AsyncArgs arg)
        {
            SleepHelper.Sleep(10000);

            Master master = arg.Args as Master;
            Random rd = new Random();
            long failCount = 0;

            Stopwatch sp = new Stopwatch();
            sp.Start();

            long size = 0;
            CarInfo ui = new CarInfo() { CarName = "Car_test2_", CarBrand = "奔驰，宝马", InfoSek = new byte[rd.Next(200, 9058)] };
            List<byte[]> data = Function.GetBytesFromSerializableObj(ui, ConstParams.BufferBlockSize, ref size);

            while (true)
            {
                try
                {
                    ui.CarName = "Car_test2_" + i2++;
                    ICacheDataType cache = new CacheSerializableObject(1, ui.CarName, ui, data, size);
                    if (!master.Set(cache))
                        failCount++;
                }
                catch (Exception ex)
                {
                    failCount++;
                }

                if (i2 > 10000)
                {
                    sp.Stop();
                    break;
                }
            }

            Console.WriteLine("Car_test2_  set 10000 finished, failed:" + failCount + "  time(ms):" + sp.ElapsedMilliseconds);
            Logger.WriteLog("Car_test2_  set 10000 finished, failed:" + failCount + "  time(ms):" + sp.ElapsedMilliseconds);
        }
        
        //get in category 0
        static void Test5(AsyncArgs arg)
        {
            SleepHelper.Sleep(10000);

            Master master = arg.Args as Master;
            Random rd = new Random();

            long size = 0;
            UserInfo ui = new UserInfo() { Name = "左丹test1_" };
            List<byte[]> data = Function.GetBytesFromSerializableObj(ui, ConstParams.BufferBlockSize, ref size);

            Stopwatch sp = new Stopwatch();
            sp.Start();
            long failCount = 0;

            int roundTime = 0;

            while (roundTime++ <= 10000)
            {
                try
                {
                    if (i < 200)
                    {
                        roundTime--;
                        continue;
                    }

                    ui.Name = "左丹test1_" + rd.Next(1, (int)(i-199));
                    ICacheDataType key = new CacheSerializableObject(ui.Name, ui, data, size);

                    ICacheDataType cached = master.Get(key);

                    //if (cached != null)
                    //    Logger.WriteLog("get_category0", string.Format("get UserInfo: category {0}  key: {1}  Prov: {2}", key.Category, ui.Name, (cached.RealObj as UserInfo).Prov));
                    //else
                    //{
                    //    failCount++;
                    //    Logger.WriteLog("get_category0", string.Format("get UserInfo: category {0}  key: {1}  failed************", key.Category, ui.Name));
                    //}

                    master.GetAsync(key).Then((SuccessInMaster)((obj) =>
                    {
                        UserInfo temp = obj[0].RealObj as UserInfo;
                        if (obj != null)
                            Logger.WriteLog("get_category0", string.Format("get UserInfo: category {0}  key: {1}  DataLength: {2}", obj[0].Category, temp.Name, temp.Data.Length));
                        else
                        {
                            failCount++;
                            Logger.WriteLog("get_category0", "get UserInfo: failed************");
                        }
                    }), (FailInMaster)((err) =>
                    {
                        Logger.WriteLog("get_category0", "error in get userInfo:" + err.Message);
                        failCount++;
                    })).Then((SuccessInMaster)((obj) =>
                    {
                        Logger.WriteLog("xxxxx", "success 2");
                    }));
                }
                catch (Exception ex)
                {
                    Logger.WriteLog("fuck:" + ex.Message +  ex.StackTrace);
                }
            }

            sp.Stop();

            Console.WriteLine("左丹test1_  get 10000 finished, failed:" + failCount + "  time(ms):" + sp.ElapsedMilliseconds);
            Logger.WriteLog("左丹test1_  get 10000 finished, failed:" + failCount + "  time(ms):" + sp.ElapsedMilliseconds);
        }

        //get in category 1
        static void Test6(AsyncArgs arg)
        {
            SleepHelper.Sleep(10000);

            Master master = arg.Args as Master;
            Random rd = new Random();

            long size = 0;
            CarInfo ui = new CarInfo() { CarName = "Car_test2_" };
            List<byte[]> data = Function.GetBytesFromSerializableObj(ui, ConstParams.BufferBlockSize, ref size);

            Stopwatch sp = new Stopwatch();
            sp.Start();
            long failCount = 0;

            int roundTime = 0;

            while (roundTime++ <= 10000)
            {
                if (i2 < 200)
                {
                    roundTime--;
                    continue;
                }

                try
                {
                    ui.CarName = "Car_test2_" + rd.Next(1, (int)(i2-199));
                    ICacheDataType key = new CacheSerializableObject(1, ui.CarName, ui, data, size);

                    ICacheDataType cached = master.Get(key);

                    if (cached != null)
                        Logger.WriteLog("get_category1", string.Format("get CarInfo: category {0}  key: {1}  book length: {2} &&&&&&&&&&&", key.Category, ui.CarName, (cached.RealObj as CarInfo).InfoSek.Length));
                    else
                    {
                        failCount++;
                        Logger.WriteLog("get_category1", string.Format("get CarInfo: category {0}  key: {1}  failed ^^^^^^^^^^^^^^^", key.Category, ui.CarName));
                    }
                }
                catch (Exception ex)
                {
                    failCount++;
                    Logger.WriteLog("get_category1", "error in get CarInfo:" + ex.Message);
                }
            }

            sp.Stop();

            Console.WriteLine("Car_test2_  get 10000 finished, failed:" + failCount + "  time(ms):" + sp.ElapsedMilliseconds);
            Logger.WriteLog("Car_test2_  get 10000 finished, failed:" + failCount + "  time(ms):" + sp.ElapsedMilliseconds);
        }

        //delete
        static void Test9(AsyncArgs arg)
        {
            SleepHelper.Sleep(10000);

            Master master = arg.Args as Master;
            Random rd = new Random();
            while (true)
            {
                try
                {
                    UserInfo ui = new UserInfo() { Name = "左丹test1_" + rd.Next(1, (int)i) };
                    ICacheDataType key = new CacheSerializableObject(ui.Name, ui);

                    if (master.Delete(key))
                        Logger.WriteLog("delete", string.Format("delete UserInfo: category {0}  key: {1} ", key.Category, ui.Name));
                    else
                        Logger.WriteLog("delete", string.Format("delete UserInfo: category {0}  key: {1}  failed************", key.Category, ui.Name));
                }
                catch (Exception ex)
                {
                    Logger.WriteLog("delete", "error in delete userInfo:" + ex.Message);
                }

                SleepHelper.Sleep(100);
            }
        }

        //update
        static void Test10(AsyncArgs arg)
        {
            SleepHelper.Sleep(10000);

            Master master = arg.Args as Master;
            Random rd = new Random();
            while (true)
            {
                try
                {
                    UserInfo ui = new UserInfo()
                    {
                        Name = "左丹test1_" + rd.Next(1, (int)i),
                        age = 34,
                        Prov = "changed provinced" + i
                    };
                    ICacheDataType key = new CacheSerializableObject(ui.Name, ui);

                    if (master.Update(key))
                        Logger.WriteLog("update", string.Format("update UserInfo: category {0}  key: {1}  prov:{2}",
                            key.Category, ui.Name, ui.Prov));
                    else
                        Logger.WriteLog("update", string.Format("update UserInfo: category {0}  key: {1}  failed************", key.Category, ui.Name));
                }
                catch (Exception ex)
                {
                    Logger.WriteLog("update", "error in update userInfo:" + ex.Message);
                }

                SleepHelper.Sleep(1500);
            }
        }
    }
}
