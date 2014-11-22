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

        public static void Main()
        {
            ConcurrentDictionary<int, int> dic = new ConcurrentDictionary<int, int>();
            dic.TryAdd(1, 2);
            dic.TryAdd(2, 3);
            dic.TryAdd(3, 4);
            dic.TryAdd(34, 4);
            dic.TryAdd(31, 4);
            int outTmp;

            Parallel.ForEach<int>(dic.Keys, item =>
            {
                if (dic.TryRemove(item, out outTmp))
                    Console.WriteLine(item);
            });

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
