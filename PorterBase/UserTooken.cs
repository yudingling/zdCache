using System;
using System.Threading;
using System.Net.Sockets;

namespace ZdCache.PorterBase
{
    /// <summary>
    /// 标志 Token 的用途
    /// </summary>
    internal enum TokenUseType { Receive, Send }

    internal class UToken
    {
        private TokenUseType tokenType;

        private int id;  //唯一标识一个 SocketAsyncEventArgs 对象

        internal byte[] Buffer;

        internal int RecvPacketLength; //接收数据包的总长度(接收)

        internal int OffSet; //接收/发送缓存区的偏移

        internal DataContainer Container;  //数据容器，用于存储某次完整的接收数据 (Buffer 的大小可能是不够的) (接收)

        internal UToken(TokenUseType type)
        {
            this.tokenType = type;

            this.id = IDGetter.GetID();
            this.RecvPacketLength = -1;
            this.OffSet = 0;

            //当用于接收时，初始化4KB 接收缓冲区。 当用于发送时， 不初始化缓冲区，由发送时进行赋值
            if (type == TokenUseType.Receive)
            {
                this.Buffer = new byte[4 * 1024];
                Container = new DataContainer();
            }
        }

        /// <summary>
        /// 重置 usertoken ，以便用于新的接收/发送
        /// </summary>
        internal void Reset()
        {
            this.OffSet = 0;

            if (this.tokenType == TokenUseType.Receive)
            {
                this.RecvPacketLength = -1;
                this.Container.Reset();
            }
            else
            {
                this.Buffer = null;
            }
        }

        internal int ID
        {
            get { return this.id; }
        }
    }
}
