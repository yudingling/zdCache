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
    public class SocketClientSettings : BaseSettings
    {
        //绑定的IP
        private string ip;
        //绑定的端口
        private int port;

        private ProtocolType ptType;

        private IPEndPoint ep;

        /// <summary>
        /// 构造函数
        /// </summary>
        public SocketClientSettings(string remoteIP, int remotePort, ProtocolType remotePtType, int recvAndSendTimeOut, ISizeGetter packetSizeGetter, int callBackThreadCount)
        {
            this.ip = remoteIP;
            this.port = remotePort;
            this.ptType = remotePtType;
            this.RecvSendTimeOut = recvAndSendTimeOut;
            this.MySizeGetter = packetSizeGetter;
            this.CallBackThreadCount = callBackThreadCount;

            this.ep = new IPEndPoint(IPAddress.Parse(remoteIP), this.port);
        }

        #region readonly properities

        public string IP { get { return this.ip; } }

        public int Port { get { return this.port; } }

        public ProtocolType PTType { get { return this.ptType; } }

        public EndPoint EP { get { return this.ep; } }

        #endregion

    }
}
