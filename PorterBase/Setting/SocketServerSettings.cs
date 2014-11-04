using System;
using System.Net.Sockets;
using System.Net;
using ZdCache.Common.SizeGetter;
using ZdCache.Common;

namespace ZdCache.PorterBase.Setting
{
    /// <summary>
    /// socket 的配置参数类
    /// </summary>
    public class SocketServerSettings : BaseSettings
    {
        //绑定的端口
        private int port;

        private ProtocolType ptType;

        private IPEndPoint ep;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="bindPort">绑定的端口</param>
        /// <param name="bindPtType">socket 通信协议类型</param>
        /// <param name="recvAndSendTimeOut">发送/接收超时时间</param>
        /// <param name="packetSizeGetter">数据包长度获取器</param>
        /// <param name="callBackThreadCount">接收到数据后，执行回调操作的线程数，此一般根据CPU核心个数而定，4核则4个线程</param>
        public SocketServerSettings(int bindPort, ProtocolType bindPtType, int recvAndSendTimeOut, ISizeGetter packetSizeGetter, int callBackThreadCount)
        {
            this.port = bindPort;
            this.ptType = bindPtType;
            this.RecvSendTimeOut = recvAndSendTimeOut;
            this.MySizeGetter = packetSizeGetter;
            this.CallBackThreadCount = callBackThreadCount;

            this.ep = new IPEndPoint(IPAddress.Any, port);
        }

        #region readonly properities

        public int Port { get { return this.port; } }

        public ProtocolType PTType { get { return this.ptType; } }

        public EndPoint EP { get { return this.ep; } }

        #endregion

    }
}
