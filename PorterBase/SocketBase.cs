using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using ZdCache.Common;
using ZdCache.PorterBase.Setting;

namespace ZdCache.PorterBase
{
    /// <summary>
    /// socket 的基类，internal 访问权限
    /// </summary>
    public abstract class SocketBase : Porter
    {
        protected Socket localSocket;

        //回调操作类
        protected CallBackHandler callBackHandler;

        public SocketBase(BaseSettings pbSettings, ErrorTracer tracer)
            : base(tracer)
        {
            this.callBackHandler = new CallBackHandler(pbSettings.CallBackThreadCount, pbSettings.OnReceive);
        }

        /// <summary>
        /// 初始化关联对象， 需要子类去实现
        /// </summary>
        protected abstract void InitLocalSocket();

        #region IPorter 成员

        /// <summary>
        /// 关闭 socket， 释放占用的资源
        /// </summary>
        public override void Close()
        {
            if (this.localSocket != null)
            {
                try
                {
                    this.localSocket.Shutdown(SocketShutdown.Both);
                }
                catch (Exception ex)
                {
                    this.TraceError(ErrorType.Other, 0, string.Format("close local socket failed：{0}", ex.Message));
                }
                finally
                {
                    this.localSocket.Close();
                    this.localSocket = null;
                }
            }

            //释放 callbackhandler。
            //   注意，此处不能将 callBackHandler 引用置为null， 因为涉及到异步的问题， 
            //   close 调用时依然有可能用到 callBackHandler 参数（比如 tcpServer 中 saea 的 receive 回调）
            if (this.callBackHandler != null)
                this.callBackHandler.Dispose();
        }

        #endregion

        #region common

        public void ShutDownClientSocket(Socket clientSocket)
        {
            if (clientSocket != null)
            {
                try
                {
                    clientSocket.Shutdown(SocketShutdown.Both);
                }
                catch
                {
                }
            }
        }

        public void CloseClientSocket(Socket clientSocket)
        {
            if (clientSocket != null)
            {
                try
                {
                    clientSocket.Close();
                }
                catch
                {
                }
            }
        }

        #endregion
    }
}
