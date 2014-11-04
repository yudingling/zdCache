using System;
using ZdCache.Common.SizeGetter;
using System.Net;

namespace ZdCache.PorterBase.Setting
{
    /// <summary>
    /// 基础配置类
    /// </summary>
    public class BaseSettings
    {
        /// <summary>
        /// 接收/发送超时时间
        /// </summary>
        public int RecvSendTimeOut { get; set; }

        /// <summary>
        /// 接收数据的回调
        /// </summary>
        public PorterReceive OnReceive { get; set; }

        /// <summary>
        /// 数据包长度获取器
        /// </summary>
        public ISizeGetter MySizeGetter { get; set; }

        /// <summary>
        /// 回调操作类的线程数
        /// </summary>
        public int CallBackThreadCount { get; set; }
    }
}
