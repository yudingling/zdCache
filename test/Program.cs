using System;
using System.Threading;
using System.Runtime.Remoting.Contexts;
using ZdCache.Common;
using System.Reflection;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace test
{
    delegate void CallA(string a);
    delegate void CallB(int a);

    public class TestSyncDomainWait
    {
        static void done1(string a)
        {
            Console.WriteLine(a);
        }

        static void done2(int a)
        {
            Console.WriteLine(a);
        }

        static SemaphoreSlim seamphore = new SemaphoreSlim(0, 2);

        public static void Main()
        {
            Task.Factory.StartNew(() => {
                try
                {
                    Console.WriteLine("A Start;");
                    seamphore.Wait(Timeout.Infinite);
                    Console.WriteLine("A finished;");
                }
                catch
                {
                    Console.WriteLine("A error");
                }
            });

            Task.Factory.StartNew(() =>
            {
                try
                {
                    Console.WriteLine("B Start;");
                    seamphore.Wait(1000);
                    Console.WriteLine("B finished;");
                }
                catch(Exception ex)
                {
                    Console.WriteLine("B error" + ex.Message + ex.StackTrace);
                }
            });

            SleepHelper.Sleep(100);
            seamphore.Release();
            Console.WriteLine("Release 1;");


            SleepHelper.Sleep(100);
            seamphore.Release();
            Console.WriteLine("Release 2;");

            SleepHelper.Sleep(100);
            Console.WriteLine("Current:" + seamphore.CurrentCount);

            seamphore.Release();
            Console.WriteLine("Current:" + seamphore.CurrentCount);
            seamphore.Release();
            Console.WriteLine("Current:" + seamphore.CurrentCount);

            Console.WriteLine("Wait xx:" + seamphore.Wait(0));
            Console.WriteLine("Wait xx:" + seamphore.Wait(0));
            Console.WriteLine("Wait xx:" + seamphore.Wait(0));
            Console.WriteLine("Wait xx:" + seamphore.Wait(0));

            Console.WriteLine("Current:" + seamphore.CurrentCount);


            Console.ReadLine();
            return;

            //CallB b = delegate(int a) { Console.WriteLine(a); };
            //CallB b = (int a) => { Console.WriteLine(a); };
            string str = "fuck you";
            Call(new CallA(done1), (CallB)((int a) => { Console.WriteLine(a); Console.WriteLine(str); }));

            Console.ReadLine();
        }

        static void Call(params Delegate[] cbs)
        {
            Console.WriteLine(cbs[1].GetType().Name);
            (cbs[0] as CallA)("1");
            (cbs[1] as CallB)(0);
        }
    }
}
