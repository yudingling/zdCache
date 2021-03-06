﻿using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;
using ZdCache.Common;
using System.Threading;
using System.Net.Sockets;
using ZdCache.PorterBase.Setting;

namespace ZdCache.PorterBase
{
    /// <summary>
    /// Socket Server with TCP Protocol
    /// 1、window 下的 IOCP 实现 （SocketAsyncEventArgs）
    /// 2、3层独立 accept, send/receive , callback
    /// </summary>
    public class TcpSocketServer : SocketBase
    {
        //客户端接收的 SAEA 池
        private SAEAPool acceptSAEAPool;
        //数据接收/发送的 SAEA 池
        private SAEAPool recvSAEAPool;
        private SAEAPool sendSAEAPool;

        //存储保持的 SAEA 对象列表 (此处必须是线程安全字典)
        //因为接收完后，有时候还需要反馈，所以需要保存对客户端的连接，不能直接关闭了
        private ConcurrentDictionary<int, SocketAsyncEventArgs> keepAccepttedSAEAList;

        //server 配置类
        private SocketServerSettings serverSetting;

        /// <summary>
        /// 构造函数
        /// </summary>
        public TcpSocketServer(SocketServerSettings setting, ErrorTracer tracer)
            : base(setting, tracer)
        {
            try
            {
                this.serverSetting = setting;

                this.acceptSAEAPool = new SAEAPool();
                this.recvSAEAPool = new SAEAPool();
                this.sendSAEAPool = new SAEAPool();

                //用于保存已经连接上客户端的 SAEA 对象
                this.keepAccepttedSAEAList = new ConcurrentDictionary<int, SocketAsyncEventArgs>();

                //开始侦听
                InitLocalSocket();
            }
            catch
            {
                //存在有限资源的分配（SocketBase 中的 callBackHandler 分配了线程、创建了 saeaPool...），如果异常，需要释放资源
                this.Close();
                throw;
            }
        }

        /// <summary>
        /// 异步接收客户端的连接
        /// </summary>
        private void StartAccept()
        {
            SocketAsyncEventArgs acceptSAEA = null;
            try
            {
                if (this.localSocket != null)
                {
                    acceptSAEA = GetAcceptSAEAItem();
                    bool ret = this.localSocket.AcceptAsync(acceptSAEA);
                    //如果 AcceptAsync 返回值为 false，表示 AcceptAsync 操作被同步执行了，SAEA 对象的 Completed 事件将不会被执行
                    //则此时需要进行手动调用ProcessAccpet
                    if (!ret)
                        ProcessAccpet(acceptSAEA);
                }
            }
            catch
            {
                //close 被调用时，会将 localSocket 设置为 null，又因为异步的缘故，
                //       上面的为 this.localSocket != null 判断有时候会失效，需要处理此异常
                if (acceptSAEA != null)
                    HandleBadAccept(acceptSAEA);
            }
        }

        /// <summary>
        /// 完成 accept 后，对 accept 的 saea 对象进行处理
        /// </summary>
        /// <param name="acceptSAEA"></param>
        private void ProcessAccpet(SocketAsyncEventArgs acceptSAEA)
        {
            if (acceptSAEA.SocketError != SocketError.Success)
            {
                //执行到此表示accept出错了，则需要再次进行接收，进行循环
                StartAccept();
                //进行失败处理
                HandleBadAccept(acceptSAEA);
                //返回，结束一个 accept 过程
                return;
            }
            //执行到这，表示已经接收成功了，需要开启一个新的接收
            StartAccept();

            //从接收/发送 SAEA 池中获取一个 SAEA 对象，进行接收数据的操作
            SocketAsyncEventArgs recAndSendSAEA = GetRecvAndSendSAEAItem(TokenUseType.Receive);
            recAndSendSAEA.AcceptSocket = acceptSAEA.AcceptSocket;

            //push 回 acceptSAEAPool
            acceptSAEA.AcceptSocket = null;
            this.acceptSAEAPool.Push(acceptSAEA);

            StartReceive(recAndSendSAEA);
        }

        /// <summary>
        /// 异步数据的接收
        /// </summary>
        private void StartReceive(SocketAsyncEventArgs recvSAEA)
        {
            try
            {
                UToken token = recvSAEA.UserToken as UToken;

                //保存 recAndSendSAEA 到 keepedSAEAList 中，以便下次再次使用此 SAEA 对象(比如回复数据到客户端)
                this.keepAccepttedSAEAList.TryAdd(token.ID, recvSAEA);

                //设置接收缓冲区
                recvSAEA.SetBuffer(token.Buffer, token.OffSet, token.Buffer.Length - token.OffSet);

                //进行异步接收
                bool ret = recvSAEA.AcceptSocket.ReceiveAsync(recvSAEA);

                //如果 ReceiveAsync 返回值为 false，表示 ReceiveAsync 操作被同步执行了，SAEA 对象的 Completed 事件将不会被执行
                //则此时需要进行手动调用 ProcessReceive
                if (!ret)
                    ProcessReceive(recvSAEA);
            }
            catch
            {
                //关闭客户端的连接的时候，触发了 ProcessReceive 事件，但 SocketError 却是 success， 
                //然后再到此方法的 recvSAEA.AcceptSocket.ReceiveAsync 时就报错，对这种情况需要进行处理
                HandleBadRecv(recvSAEA);
            }
        }

        /// <summary>
        /// SAEA 接收完后，对接收的数据进行处理
        /// </summary>
        /// <param name="recSAEA"></param>
        private void ProcessReceive(SocketAsyncEventArgs recSAEA)
        {
            UToken token = recSAEA.UserToken as UToken;

            if (recSAEA.SocketError != SocketError.Success)
            {
                //执行到这表示接收数据出错了
                HandleBadRecv(recSAEA);
                //返回，结束一个 recv 过程
                return;
            }

            //对接收到的数据进行处理
            List<CallBackListArg> cbArgList = new List<CallBackListArg>();
            List<string> errors = new List<string>();
            if (SAEAByteHandler.HandleRecv(recSAEA, token, this.serverSetting.MySizeGetter, cbArgList, errors))
            {
                //接收完成，重置 token
                token.Reset();
            }

            if (cbArgList.Count > 0)
            {
                foreach (CallBackListArg item in cbArgList)
                    this.callBackHandler.Push(item);
            }

            if (errors.Count > 0)
            {
                foreach (string msg in errors)
                    this.TraceError(ErrorType.Receive, token.ID, msg);
            }

            //继续读取 (读取未读取完的数据，或者开启一个新的读取)
            StartReceive(recSAEA);
        }

        /// <summary>
        /// 处理失败的 accept
        /// </summary>
        /// <param name="acceptSAEA"></param>
        private void HandleBadAccept(SocketAsyncEventArgs acceptSAEA)
        {
            CloseClientSocket(acceptSAEA.AcceptSocket);
            acceptSAEA.AcceptSocket = null;
            //Push 回 SAEA 池中，重复
            this.acceptSAEAPool.Push(acceptSAEA);
        }

        /// <summary>
        /// 处理失败的数据接收
        /// </summary>
        /// <param name="recSAEA"></param>
        private void HandleBadRecv(SocketAsyncEventArgs recSAEA)
        {
            UToken token = recSAEA.UserToken as UToken;

            SocketAsyncEventArgs outValue;
            //注意这里的写法，不能使用 containsKey 的方式，因为异步
            if (this.keepAccepttedSAEAList.TryRemove(token.ID, out outValue))
            {
                //重置 token
                token.Reset();
                //关闭客户端
                if (recSAEA.AcceptSocket != null)
                {
                    ShutDownClientSocket(recSAEA.AcceptSocket);
                    CloseClientSocket(recSAEA.AcceptSocket);
                    recSAEA.AcceptSocket = null;
                }

                //push 回 SAEA 池中，复用
                this.recvSAEAPool.Push(outValue);
            }
        }

        /// <summary>
        /// 处理失败的数据发送
        /// </summary>
        /// <param name="acceptSAEA"></param>
        private void HandleBadSend(SocketAsyncEventArgs sendSAEA, bool isCloseSocket)
        {
            //重置 token
            (sendSAEA.UserToken as UToken).Reset();

            if (sendSAEA.AcceptSocket != null && isCloseSocket)
            {
                ShutDownClientSocket(sendSAEA.AcceptSocket);
                CloseClientSocket(sendSAEA.AcceptSocket);
                sendSAEA.AcceptSocket = null;
            }

            //Push 回 SAEA 池中，复用
            this.sendSAEAPool.Push(sendSAEA);
        }


        #region 从池中获取SAEA 对象

        /// <summary>
        /// 获取一个接收客户端的 SAEA 对象
        /// </summary>
        /// <returns></returns>
        private SocketAsyncEventArgs GetAcceptSAEAItem()
        {
            SocketAsyncEventArgs item;
            try
            {
                item = this.acceptSAEAPool.Pop();
            }
            catch
            {
                item = CreateNewSAEAForAccept();
            }
            return item;
        }

        /// <summary>
        /// 获取一个用户接收/发送数据的 SAEA 对象
        /// </summary>
        /// <returns></returns>
        private SocketAsyncEventArgs GetRecvAndSendSAEAItem(TokenUseType useType)
        {
            SocketAsyncEventArgs item;
            try
            {
                item = useType == TokenUseType.Receive ? this.recvSAEAPool.Pop() : this.sendSAEAPool.Pop();
            }
            catch
            {
                item = CreateNewSAEAForRecvAndSend(useType);
            }
            return item;
        }

        #endregion

        #region 创建 SAEA 对象

        /// <summary>
        /// 创建一个新的用于接收客户端socket 的 SAEA 对象
        /// </summary>
        /// <param name="pool"></param>
        /// <returns></returns>
        private SocketAsyncEventArgs CreateNewSAEAForAccept()
        {
            SocketAsyncEventArgs acceptSAEA = new SocketAsyncEventArgs();
            acceptSAEA.Completed += new EventHandler<SocketAsyncEventArgs>(Accept_Completed);
            return acceptSAEA;
        }

        /// <summary>
        /// accept socket 完成事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Accept_Completed(object sender, SocketAsyncEventArgs e)
        {
            ProcessAccpet(e);
        }


        /// <summary>
        /// 创建一个新的用于接收/发送数据的 SAEA 对象
        /// </summary>
        /// <param name="pool"></param>
        /// <returns></returns>
        private SocketAsyncEventArgs CreateNewSAEAForRecvAndSend(TokenUseType useType)
        {
            SocketAsyncEventArgs recvAndSendSAEA = new SocketAsyncEventArgs();
            recvAndSendSAEA.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
            //给其附加上 UserTooken，用于存储接收/发送的数据等信息
            recvAndSendSAEA.UserToken = new UToken(useType);
            return recvAndSendSAEA;
        }

        /// <summary>
        /// send/recv 完成事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSend(e);
                    break;
                default:
                    return;
            }
        }

        #endregion

        /// <summary>
        /// 异步发送数据
        /// </summary>
        /// <param name="sendSAEA"></param>
        private void StartSend(SocketAsyncEventArgs sendSAEA)
        {
            //设置发送缓冲区
            UToken token = sendSAEA.UserToken as UToken;
            sendSAEA.SetBuffer(token.Buffer, token.OffSet, token.Buffer.Length - token.OffSet);

            //进行异步发送
            bool ret = sendSAEA.AcceptSocket.SendAsync(sendSAEA);

            //如果 SendAsync 返回值为 false，表示 SendAsync 操作被同步执行了，SAEA 对象的 Completed 事件将不会被执行
            //则此时需要进行手动调用 ProcessSend
            if (!ret)
                ProcessSend(sendSAEA);
        }

        /// <summary>
        /// SAEA 发送完后，对数据进行处理
        /// </summary>
        /// <param name="sendSAEA"></param>
        private void ProcessSend(SocketAsyncEventArgs sendSAEA)
        {
            UToken token = sendSAEA.UserToken as UToken;
            if (sendSAEA.SocketError != SocketError.Success)
            {
                //执行到这表示发送数据出错了
                //回收 SAEA，并且关闭 socket
                HandleBadSend(sendSAEA, true);
                //返回，结束一个 send 过程
                return;
            }

            List<string> errors = new List<string>();
            if (SAEAByteHandler.HandleSend(sendSAEA, token, errors))
            {
                //执行到此，表示发送完成
                //把用于发送的 SAEA 对象放回池中， 但不关闭 socket，因为接收的 SAEA 还在执行
                HandleBadSend(sendSAEA, false);
            }
            else
            {
                //未发送完成，继续发送
                StartSend(sendSAEA);
            }

            if (errors.Count > 0)
            {
                foreach (string msg in errors)
                    this.TraceError(ErrorType.Send, token.ID, msg);
            }
        }

        #region override socketbase

        /// <summary>
        /// 初始化 socket server
        /// </summary>
        protected override void InitLocalSocket()
        {
            this.localSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.localSocket.SendTimeout = this.serverSetting.RecvSendTimeOut;
            this.localSocket.ReceiveTimeout = this.serverSetting.RecvSendTimeOut;

            LingerOption lo = new LingerOption(true, 0);
            this.localSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, lo);
            this.localSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            //能接受的最大连接数，这个和操作系统的设置有关， win7 不支持，使用的话就报错
            //socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.MaxConnections, 3000); 

            this.localSocket.Bind(this.serverSetting.EP);

            //挂起的连接数最大为10。 也就是说，如果连接达到了最大连接数SocketOptionName.MaxConnections，此时再有连接过来，
            //则将进入挂起队列，这个数值即挂起队列的长度 
            this.localSocket.Listen(10);

            StartAccept();
        }

        /// <summary>
        /// 给连接的 client 发送数据
        /// </summary>
        /// <param name="tokenID">唯一标识 SocketAsyncEventArgs 对象</param>
        /// <param name="data"></param>
        public override void Send(int tokenID, byte[] data)
        {
            //找出 tokenid 对应的 SAEA 对象
            SocketAsyncEventArgs keepedSAEA;
            if (this.keepAccepttedSAEAList.TryGetValue(tokenID, out keepedSAEA))
            {
                //注意，SocketAsyncEventArgs 对象在处于接收状态时，是不能用于发送的，所以此处需要一个新的 SocketAsyncEventArgs 来进行发送
                SocketAsyncEventArgs sendSAEA = GetRecvAndSendSAEAItem(TokenUseType.Send);

                //将保存的 socket 给新的 SAEA 对象赋值 
                sendSAEA.AcceptSocket = keepedSAEA.AcceptSocket;

                //给发送 byte 数组赋值
                (sendSAEA.UserToken as UToken).Buffer = data;

                StartSend(sendSAEA);
            }
            else
                throw new Exception("此 tokenID 对应的客户端不存在，无法向其发送数据！");
        }

        /// <summary>
        /// 释放资源，注意此方法的实现： 1、顺序是固定。先关闭本地socket，再清除已接收的saea，最后移除 saea pool 中的资源
        ///                            2、不能将清除引用（比如 saea pool 置为null）。因为异步的关系，可能 close 在调用的过程中，上面的 receive 等方法
        ///                               还在执行，如果置为 null，将引发异常
        /// </summary>
        public override void Close()
        {
            //关闭本地 socket
            base.Close();

            //释放已接收的 saea 资源
            if (this.keepAccepttedSAEAList != null)
            {
                foreach (SocketAsyncEventArgs arg in this.keepAccepttedSAEAList.Values)
                    arg.Dispose();
            }

            //释放 saea pool 资源
            if (this.acceptSAEAPool != null)
                this.acceptSAEAPool.Dispose();

            if (this.sendSAEAPool != null)
                this.sendSAEAPool.Dispose();

            if (this.recvSAEAPool != null)
                this.recvSAEAPool.Dispose();
        }

        /// <summary>
        /// tcp 的 dropClient 不做处理。
        ///  因为异步的关系，可能该连接还存在很多 SAEA 处于发送中，如果断开了，则将发送失败，这与逻辑相违背。
        ///  故对于 tcp 来说，断开操作只由 client 端进行。 server 端不处理。
        /// </summary>
        /// <param name="tokenID"></param>
        public override void DropClient(int tokenID)
        {
        }

        #endregion
    }
}
