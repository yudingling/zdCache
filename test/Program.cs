using System;
using System.Threading;
using System.Runtime.Remoting.Contexts;
using ZdCache.Common;
using System.Reflection;

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

        public static void Main()
        {
            //CallB b = delegate(int a) { Console.WriteLine(a); };
            //CallB b = (int a) => { Console.WriteLine(a); };
            Call(new CallA(done1), (CallB)((int a)=>{ Console.WriteLine(a); }));

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
