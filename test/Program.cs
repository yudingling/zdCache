using System;
using System.Threading;
using System.Runtime.Remoting.Contexts;
using ZdCache.Common;

namespace test
{
    internal enum MREType { ReSet, Set, WaitOne }

    /// <summary>
    /// Call 对应的 ManualResetEvent 控制类。 之所以单独出来，是为了保证同一个上下文
    /// </summary>
    [Synchronization(true)]
    internal class MREContext : ContextBoundObject
    {
        //控制 call 的超时
        private ManualResetEvent manualRE = new ManualResetEvent(false);

        private bool Reset()
        {
            try
            {
                this.manualRE.Reset();
            }
            catch
            {
            }
            return true;
        }

        private bool Set(int allCallCount, int returnedCallCount)
        {
            try
            {
                Console.WriteLine("begin Set:" + returnedCallCount);
                SleepHelper.Sleep(1000);
                    
                Console.WriteLine("end Set:" + returnedCallCount);
                this.manualRE.Set();
            }
            catch
            {
            }
            return true;
        }

        private bool WaitOne(int allCallCount, int returnedCallCount, int millisecondsTimeout)
        {
            try
            {
                Console.WriteLine("begin waitOne:" + returnedCallCount);

                //waitOut 后，注意，必须退出同步上下文
                this.manualRE.WaitOne(millisecondsTimeout, true);

                Console.WriteLine("end waitOne:" + returnedCallCount);

                return false;
                
            }
            catch
            {
                return true;
            }
        }

        /// <summary>
        /// 执行 ManualResetEvent 的操作
        /// </summary>
        /// <param name="allCallCount"></param>
        /// <param name="returnedCallCount"></param>
        /// <param name="millisecondsTimeout">只在 type 为 MREType.WaitOne 时才生效</param>
        /// <param name="type"></param>
        /// <returns></returns>
        public bool Done(int allCallCount, int returnedCallCount, int millisecondsTimeout, MREType type)
        {
            switch (type)
            {
                case MREType.ReSet:
                    return this.Reset();
                case MREType.Set:
                    return this.Set(allCallCount, returnedCallCount);
                case MREType.WaitOne:
                    return this.WaitOne(allCallCount, returnedCallCount, millisecondsTimeout);

                default:
                    return true;
            }
        }
    }

    public class SyncingClass
    {
        MREContext context = new MREContext();

        

        public void Done()
        {
            SleepHelper.Sleep(1);
            Console.WriteLine("cao waitone");
            this.context.Done(10, 2, 50000, MREType.WaitOne);
        }

        public void Done2()
        {
            SleepHelper.Sleep(20);
            Console.WriteLine("cao set");
            this.context.Done(10, 1, 0, MREType.Set);
        }
    }

    public class TestSyncDomainWait
    {
        public static void Main()
        {
            SyncingClass cs= new SyncingClass();
            AsyncCall call2 = new AsyncCall(func2, new AsyncArgs() { Args = cs, ArgsAbort = null }, true, null);
            AsyncCall call1 = new AsyncCall(func1, new AsyncArgs() { Args = cs, ArgsAbort = null }, true, null);
            
            Console.ReadLine();
        }

        public static void func1(AsyncArgs parm)
        {
            ((SyncingClass)parm.Args).Done();
        }

        public static void func2(AsyncArgs parm)
        {
            ((SyncingClass)parm.Args).Done2();
        }
    }
}
