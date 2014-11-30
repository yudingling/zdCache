using System;
using System.Collections.Concurrent;
using System.Text;
using System.Net.Sockets;

namespace ZdCache.PorterBase
{
    internal class SAEAPool : IDisposable
    {
        private ConcurrentStack<SocketAsyncEventArgs> pool;

        internal SAEAPool()
        {
            this.pool = new ConcurrentStack<SocketAsyncEventArgs>();
        }

        internal int Count
        {
            get { return this.pool.Count; }
        }

        internal SocketAsyncEventArgs Pop()
        {
            SocketAsyncEventArgs temp;
            if (this.pool.TryPop(out temp))
                return temp;
            else
                throw new Exception("SocketAsyncEventArgsPool 中 Item 不足!");
        }

        internal void Push(SocketAsyncEventArgs item)
        {
            this.pool.Push(item);
        }

        #region IDisposable 成员

        /// <summary>
        /// 释放非托管的资源
        /// </summary>
        public void Dispose()
        {
            SocketAsyncEventArgs temp;
            while (this.pool.TryPop(out temp))
                temp.Dispose();
        }

        #endregion
    }
}
