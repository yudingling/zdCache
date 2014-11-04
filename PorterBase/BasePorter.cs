using System;
using System.Net.Sockets;
using System.Collections.Generic;
using ZdCache.Common;
using ZdCache.PorterBase.Setting;

namespace ZdCache.PorterBase
{
    public class BasePorter
    {
        private object lockObj = new object();

        private IPorter porter;

        /// <summary>
        /// 无参构造函数， 用于延迟初始 Porter
        /// </summary>
        public BasePorter()
        {
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="setting"></param>
        public BasePorter(BaseSettings setting, ErrorTracer tracer)
        {
            InitPorter(setting, tracer);
        }

        protected void InitPorter(BaseSettings setting, ErrorTracer tracer)
        {
            //只允许初始化一次
            lock (lockObj)
            {
                if (this.porter == null)
                {
                    //设置接收事件
                    setting.OnReceive = this.PorterDataReceived;

                    if (setting is CommServerSettings)
                    {
                        this.porter = new CommServer(setting as CommServerSettings, tracer);
                    }
                    else if (setting is SocketServerSettings)
                    {
                        SocketServerSettings settingSocket = setting as SocketServerSettings;
                        this.porter = settingSocket.PTType == ProtocolType.Tcp ?
                            (IPorter)new TcpSocketServer(settingSocket, tracer) : (IPorter)new UdpSocketServer(settingSocket, tracer);
                    }
                    else if (setting is SocketClientSettings)
                    {
                        this.porter = new SocketClient(setting as SocketClientSettings, tracer);
                    }
                    else
                        throw new Exception(string.Format("Porter 不支持此种类型[{0}]的配置", setting.GetType().Name));
                }
                else
                    throw new Exception("Porter 已经被初始化了，禁止初始多次");
            }
        }

        /// <summary>
        /// 接收数据的事件， 给 IPorter 用。 是个多线程回调，此方法不能抛出异常
        /// </summary>
        /// <param name="tokenID"></param>
        /// <param name="data"></param>
        private void PorterDataReceived(int tokenID, List<byte[]> data)
        {
            try
            {
                if (data.Count == 0 || data[0].Length == 0)
                    throw new Exception("接收到的字节数为0！");

                //调用逻辑处理
                if (this.porter is SocketClient || this.porter is CommServer)
                    DataReceived(data);
                else
                    DataReceived(tokenID, data);
            }
            catch (Exception ex)
            {
                this.porter.TraceError("接收/处理数据出错：" + ex.Message + " " + ex.StackTrace);
            }
        }


        /// <summary>
        /// 接收事件的业务逻辑回调(server 用)
        /// </summary>
        /// <param name="tokenID"></param>
        /// <param name="data"></param>
        protected virtual void DataReceived(int tokenID, List<byte[]> data) { }

        /// <summary>
        /// 接收事件的业务逻辑回调(client 用)
        /// </summary>
        /// <param name="allBytes"></param>
        protected virtual void DataReceived(List<byte[]> data) { }

        /// <summary>
        /// 发送数据异常时的回调 (client 用)
        /// </summary>
        /// <param name="ex"></param>
        protected virtual void SendErrorOccured(Exception ex){}

        /// <summary>
        /// 发送数据异常时的回调 (server 用)
        /// </summary>
        /// <param name="tokenID"></param>
        /// <param name="ex"></param>
        protected virtual void SendErrorOccured(int tokenID, Exception ex){}

        #region 公开 IPorter 接口成员

        /// <summary>
        /// 发送数据，一般由 Client 端调用，向 Server 发送
        /// </summary>
        /// <param name="data"></param>
        public void Send(byte[] data)
        {
            try
            {
                this.porter.Send(data);
            }
            catch (Exception ex)
            {
                this.porter.TraceError(string.Format("send error: {0} {1}", ex.Message, ex.StackTrace));
                this.SendErrorOccured(ex);
            }
        }

        /// <summary>
        /// 发送数据，一般由 Server 端调用，向已连接的 Client 发送
        /// </summary>
        /// <param name="tokeID"></param>
        /// <param name="data"></param>
        public void Send(int tokenID, byte[] data)
        {
            try
            {
                this.porter.Send(tokenID, data);
            }
            catch (Exception ex)
            {
                this.porter.TraceError(string.Format("tokenId[{0}] send error: {1} {2}", tokenID, ex.Message, ex.StackTrace));
                this.SendErrorOccured(tokenID, ex);
            }
        }

        /// <summary>
        /// 关闭 Server/Client
        /// </summary>
        public virtual void Close()
        {
            this.porter.Close();
        }

        #endregion
    }
}
