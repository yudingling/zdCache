using System;
using System.Threading;
using System.Runtime.Remoting.Contexts;
using ZdCache.Common;
using System.Reflection;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Management;
using Microsoft.VisualBasic.Devices;

namespace test
{
    public class TestSyncDomainWait
    {
        private static long GetMEMSize()
        {
            ComputerInfo computerInfo = new ComputerInfo();
            bool isX64 = IntPtr.Size == 8;
            if (isX64)
                return (long)(computerInfo.AvailablePhysicalMemory * 0.8);
            else
            {
                if (computerInfo.AvailablePhysicalMemory > (long)2 * 1024 * 1024 * 1024)
                    return (long)(1.4 * 1024 * 1024 * 1024);
                else
                    return (long)(computerInfo.AvailablePhysicalMemory * 0.8);
            }
        }


        public static void Main()
        {
            Console.WriteLine(GetMEMSize());
            Console.ReadLine();
        }

        static void Call(params Delegate[] cbs)
        {
        }
    }
}
