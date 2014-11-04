using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;

namespace ZdCache.PorterBase
{
    internal class SAEAPool
    {
        private Stack<SocketAsyncEventArgs> pool;

        internal SAEAPool(int capacity)
        {
            this.pool = new Stack<SocketAsyncEventArgs>(capacity);
        }

        internal int Count
        {
            get { return this.pool.Count; }
        }

        internal SocketAsyncEventArgs Pop()
        {
            lock (this.pool)
            {
                if (this.pool.Count > 0)
                    return this.pool.Pop();
                else
                    throw new Exception("SocketAsyncEventArgsPool 中 Item 不足!");
            }
        }

        internal void Push(SocketAsyncEventArgs item)
        {
            lock (this.pool)
            {
                this.pool.Push(item);
            }
        }
    }
}
