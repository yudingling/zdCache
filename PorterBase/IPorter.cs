using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZdCache.PorterBase
{
    /// <summary>
    /// Porter 使用的基接口， 对外使用此接口
    /// </summary>
    public interface IPorter
    {
        /// <summary>
        /// 发送数据，一般由 Client 端调用，向 Server 发送
        /// </summary>
        /// <param name="data"></param>
        void Send(byte[] data);

        /// <summary>
        /// 发送数据，一般由 Server 端调用，向已连接的 Client 发送
        /// </summary>
        /// <param name="tokeID"></param>
        /// <param name="data"></param>
        void Send(int tokenID, byte[] data);

        /// <summary>
        /// 关闭 Server/Client
        /// </summary>
        void Close();

        /// <summary>
        /// 输出错误
        /// </summary>
        void TraceError(string msg);
    }
}
