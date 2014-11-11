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
        /// 关闭 client 端，一般由 server 调用，进行主动的资源释放 （比如在短连接情况下【udp或者tcp，但实际业务存在需要主动断开】，完成业务的回复后，就需要断开，以便重用相关资源）
        /// </summary>
        /// <param name="tokenID"></param>
        void DropClient(int tokenID);

        /// <summary>
        /// 输出错误
        /// </summary>
        void TraceError(ErrorType errorType, int tokenID, string msg);
    }
}
