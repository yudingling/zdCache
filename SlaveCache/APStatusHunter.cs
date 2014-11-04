using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using ZdCache.Common;
using Microsoft.VisualBasic.Devices;
using System.Diagnostics;
using ZdCache.Common.CDataType;
using ZdCache.Common.ActionModels;
using ZdCache.Common.CacheCommon;

namespace ZdCache.SlaveCache
{
    /// <summary>
    /// status 数据回调
    /// </summary>
    /// <param name="info"></param>
    internal delegate void StatusDataArrived(StatusInfo info);

    /// <summary>
    /// 获取 slave 的状态信息类
    /// </summary>
    internal class APStatusHunter : IDisposable
    {
        private StatusDataArrived handleStatusData;

        private ConcurrentQueue<double> cpuRateQueue = new ConcurrentQueue<double>();
        //1分钟cpu 使用率
        private double cpuRateIn1Minute = 0;

        private ComputerInfo computerInfo = new ComputerInfo();

        AsyncCall call, cpuUseRateCall;

        /// <summary>
        /// 构造函数
        /// </summary>
        public APStatusHunter(StatusDataArrived dataArrived)
        {
            this.handleStatusData = dataArrived;
            this.call = new AsyncCall(new AsyncMethod(StatusGetter), null, true, null);

            //获取 cpu 使用率的线程
            this.cpuUseRateCall = new AsyncCall(new AsyncMethod(CpuUseRateGetter), null, true, null);
        }

        /// <summary>
        /// 获取电脑状态
        /// </summary>
        /// <param name="arg"></param>
        private void StatusGetter(AsyncArgs arg)
        {
            while (true)
            {
                try
                {
                    StatusInfo info = GetStatusInfo();
                    if (this.handleStatusData != null)
                        this.handleStatusData(info);
                }
                catch (Exception ex)
                {
                    Logger.WriteLog(LogMsgType.Error, "[APStatusHunter.StatusGetter] 异常:" + ex.Message + " " + ex.StackTrace);
                }

                //间隔 interval
                SleepHelper.Sleep(ConstParams.SlaveStatusInterval);
            }
        }

        /// <summary>
        /// 2s 获取一次 cpu 使用率
        /// </summary>
        /// <param name="arg"></param>
        private void CpuUseRateGetter(AsyncArgs arg)
        {
            PerformanceCounter cpuInfo = null;

            while (true)
            {
                try
                {
                    if (cpuInfo == null)
                    {
                        cpuInfo = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                        //第一次调用 NextValue 是一直返回0 的，这个值得过滤掉
                        cpuInfo.NextValue();
                    }

                    this.cpuRateQueue.Enqueue(Math.Round(cpuInfo.NextValue(), 2));

                    //2s 一个，则1分钟差不多存储30个数据
                    double value;
                    while (this.cpuRateQueue.Count > 30)
                    {
                        this.cpuRateQueue.TryDequeue(out value);
                    }

                    //计算平均值
                    double sum = 0, num = 0;
                    foreach (double item in this.cpuRateQueue)
                    {
                        sum += item;
                        num++;
                    }
                    if (num > 0)
                        this.cpuRateIn1Minute = Math.Round(sum / num, 2);
                }
                catch
                {
                }

                SleepHelper.Sleep(2000);
            }
        }

        private StatusInfo GetStatusInfo()
        {
            ulong totalMem = computerInfo.TotalPhysicalMemory;
            ulong remainMem = computerInfo.AvailablePhysicalMemory;

            return new StatusInfo(
                Environment.MachineName,
                GetIpV4(),
                (long)(totalMem),
                (long)((totalMem - remainMem)),
                this.cpuRateIn1Minute);
        }

        private string GetIpV4()
        {
            System.Net.IPAddress[] addrs = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList;
            if (addrs != null && addrs.Length > 0)
            {
                foreach (System.Net.IPAddress adr in addrs)
                {
                    if (adr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        return adr.ToString();
                    }
                }
            }

            return "{None}";
        }

        #region IDisposable 成员

        public void Dispose()
        {
            if (this.call != null)
                this.call.Stop();

            if (this.cpuUseRateCall != null)
                this.cpuUseRateCall.Stop();
        }

        #endregion
    }
}
