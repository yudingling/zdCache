using System;
using System.IO.Ports;
using ZdCache.Common.SizeGetter;

namespace ZdCache.PorterBase.Setting
{
    /// <summary>
    /// Comm 的配置参数类
    /// </summary>
    public class CommServerSettings : BaseSettings
    {
        //comm 端口
        private string port;
        //波特率
        private int rate;
        //校验位
        private Parity pi;
        //数据位
        private int db;
        //停止位
        private StopBits sb;

        /// <summary>
        /// 构造函数。 注意，comm 的读取/发送超时时间很奇怪，比如当为 5000 时，经常超时，实际写数据或读数据时间肯定小于5秒，不知为何。 位避免这样的问题，可将其设置为稍大些，比如 30000
        /// </summary>
        /// <param name="comPort">comm 端口</param>
        /// <param name="baudRate">波特率</param>
        /// <param name="parity">奇偶校验位</param>
        /// <param name="dataBits">数据位</param>
        /// <param name="stopBits">停止位</param>
        /// <param name="recvAndSendTimeOut">接收/发送超时时间</param>
        /// <param name="packetSizeGetter">数据包长度获取器</param>
        /// <param name="callBackThreadCount">接收到数据后，执行回调操作的线程数，此一般根据CPU核心个数而定，4核则4个线程</param>
        public CommServerSettings(string comPort, int baudRate, Parity parity, int dataBits, StopBits stopBits, int recvAndSendTimeOut, ISizeGetter packetSizeGetter, int callBackThreadCount)
        {
            this.port = comPort;
            this.rate = baudRate;
            this.pi = parity;
            this.db = dataBits;
            this.sb = stopBits;
            this.RecvSendTimeOut = recvAndSendTimeOut;
            this.MySizeGetter = packetSizeGetter;
            this.CallBackThreadCount = callBackThreadCount;
        }

        #region readonly properities

        public string Port { get { return this.port; } }

        public int BaudRate { get { return this.rate; } }

        public Parity CommParity { get { return this.pi; } }

        public int DataBits { get { return this.db; } }

        public StopBits CommStopBits { get { return this.sb; } }

        #endregion

    }
}
