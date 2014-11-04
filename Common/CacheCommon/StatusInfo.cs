using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ZdCache.Common.CacheCommon
{
    [Serializable]
    public class StatusInfo
    {
        private string _machineName;
        private string _ip;
        private long _availMem;
        private long _usedMem;
        private long _cachedMem;
        private double _cpuUseRateIn1Minutes;
        private DateTime _reportTM;
        private double _hitRate;

        public StatusInfo()
        {
            this._machineName = "{None}";
            this._ip = "{None}";
            this._availMem = 1073741824; //默认1G
            this._usedMem = 209715200; //使用200M
            this._cpuUseRateIn1Minutes = 0.05;
            this._reportTM = DateTime.Now;
        }

        public StatusInfo(Guid registerID)
        {
            this.SlaveRegisterID = registerID;

            this._machineName = "{None}";
            this._ip = "{None}";
            this._availMem = 1073741824; //默认1G
            this._usedMem = 209715200; //使用200M
            this._cpuUseRateIn1Minutes = 0.05;
            this._reportTM = DateTime.Now;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="availMemery">电脑可用内存大小(byte)</param>
        /// <param name="usedMemery">已使用内存(byte)</param>
        /// <param name="cpuUseRateIn1Min">1分钟内 cpu 平均占用率(byte)</param>
        public StatusInfo(string machineName, string ip, long availMemery, long usedMemery, double cpuUseRateIn1Min)
        {
            this._machineName = machineName;
            this._ip = ip;
            this._availMem = availMemery;
            this._usedMem = usedMemery;
            this._cpuUseRateIn1Minutes = cpuUseRateIn1Min;
            this._reportTM = DateTime.Now;
        }

        /// <summary>
        /// slave 的注册 ID
        /// </summary>
        public Guid SlaveRegisterID { get; set; }

        /// <summary>
        /// 电脑名称
        /// </summary>
        public string MachineName { get { return this._machineName; } }

        /// <summary>
        /// ip 地址(内网)
        /// </summary>
        public string IP { get { return this._ip; } }

        /// <summary>
        /// 电脑可用内存大小(byte)
        /// </summary>
        public long AvailMem { get { return this._availMem; } }

        /// <summary>
        /// 已使用内存 (byte)
        /// </summary>
        public long UsedMem { get { return this._usedMem; } }

        /// <summary>
        /// 缓存大小(byte)
        /// </summary>
        public long CachedMem
        {
            get { return this._cachedMem; }
            set { this._cachedMem = value; }
        }

        /// <summary>
        /// 1分钟内 cpu 平均占用率 (百分比，值为 0-100)
        /// </summary>
        public double CpuUseRateIn1Minutes { get { return this._cpuUseRateIn1Minutes; } }

        /// <summary>
        /// 内存占用率
        /// </summary>
        public decimal MemUseRate { get { return this._usedMem / this._availMem; } }

        /// <summary>
        /// 剩余内存(byte)
        /// </summary>
        public long FreeMem { get { return this._availMem - this._usedMem; } }

        /// <summary>
        /// 此状态对应的时刻
        /// </summary>
        public DateTime ReportTM { get { return this._reportTM; } }

        /// <summary>
        /// 缓存命中率
        /// </summary>
        public double HitRate { get { return this._hitRate; } set { this._hitRate = value; } }

        /// <summary>
        /// 增加使用内存, Interlocked 增加
        /// </summary>
        public void IncCachedMem(long mem)
        {
            Interlocked.Add(ref this._usedMem, mem);
            Interlocked.Add(ref this._cachedMem, mem);

            this._reportTM = DateTime.Now;
        }
    }
}
