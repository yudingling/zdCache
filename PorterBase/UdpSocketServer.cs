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
    public class UdpSocketServer : SocketBase
    {
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
        public UdpSocketServer(SocketServerSettings setting, ErrorTracer tracer)
            : base(setting, tracer)
        {
            try
            {
                this.serverSetting = setting;
                this.recvSAEAPool = new SAEAPool();
                this.sendSAEAPool = new SAEAPool();

                //用于保存已经连接上客户端的 SAEA 对象
                this.keepAccepttedSAEAList = new ConcurrentDictionary<int, SocketAsyncEventArgs>();

                //开始处理
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
        /// 异步数据的接收， 重载。 启动一个新的接收
        /// </summary>
        private void StartReceive()
        {
            SocketAsyncEventArgs recvSAEA = null;
            try
            {
                if (this.localSocket != null)
                {
                    //获取一个用于接收的 saea 对象
                    recvSAEA = GetRecvAndSendSAEAItem(TokenUseType.Receive);
                    UToken token = recvSAEA.UserToken as UToken;

                    //设置接收缓冲区
                    recvSAEA.SetBuffer(token.Buffer, token.OffSet, token.Buffer.Length - token.OffSet);

                    //进行异步接收
                    bool ret = this.localSocket.ReceiveFromAsync(recvSAEA);

                    //如果 ReceiveAsync 返回值为 false，表示 ReceiveAsync 操作被同步执行了，SAEA 对象的 Completed 事件将不会被执行
                    //则此时需要进行手动调用 ProcessReceive
                    if (!ret)
                        ProcessReceive(recvSAEA);
                }
            }
            catch
            {
                //close 被调用时，会将 localSocket 设置为 null，又因为异步的缘故，
                //       上面的为 this.localSocket != null 判断有时候会失效，需要处理此异常
                if (recvSAEA != null)
                    HandleBadRecv(recvSAEA);
            }
        }

        /// <summary>
        /// 异步数据的接收， 重载。 当存在数据未接收完成（接收了一部分）需要继续完成剩余部分数据接收时调用
        /// </summary>
        private void StartReceive(SocketAsyncEventArgs recvSAEA)
        {
            try
            {
                if (this.localSocket != null)
                {
                    //获取一个用于接收的 saea 对象
                    UToken token = recvSAEA.UserToken as UToken;

                    //设置接收缓冲区
                    recvSAEA.SetBuffer(token.Buffer, token.OffSet, token.Buffer.Length - token.OffSet);

                    //进行异步接收
                    bool ret = this.localSocket.ReceiveFromAsync(recvSAEA);

                    //如果 ReceiveAsync 返回值为 false，表示 ReceiveAsync 操作被同步执行了，SAEA 对象的 Completed 事件将不会被执行
                    //则此时需要进行手动调用 ProcessReceive
                    if (!ret)
                        ProcessReceive(recvSAEA);
                }
            }
            catch
            {
                //关闭socket 时，触发了 ProcessReceive 事件，但此时 socket 已不能用，对这种情况需要进行处理
                HandleBadRecv(recvSAEA);
            }
        }

        /// <summary>
        /// SAEA 接收完后，对接收的数据进行处理
        /// </summary>
        /// <param name="recSAEA"></param>
        private void ProcessReceive(SocketAsyncEventArgs recvSAEA)
        {
            //收到数据后，直接开启一个新的接收流程，减少阻塞
            StartReceive();

            UToken token = recvSAEA.UserToken as UToken;
            if (recvSAEA.SocketError != SocketError.Success)
            {
                //执行到这表示接收数据出错了
                HandleBadRecv(recvSAEA);
                //返回，结束一个 recv 过程
                return;
            }

            //保存 recAndSendSAEA 到 keepedSAEAList 中，以便下次再次使用此 SAEA 对象(比如回复数据到客户端)
            this.keepAccepttedSAEAList.TryAdd(token.ID, recvSAEA);

            //对接收到的数据进行处理
            List<CallBackListArg> cbArgList = new List<CallBackListArg>();
            List<string> errors = new List<string>();
            bool isfinishedRecv = SAEAByteHandler.HandleRecv(recvSAEA, token, this.serverSetting.MySizeGetter, cbArgList, errors);

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

            //判断此 recvSAEA 是否需要继续接收， 已接收完成，则由业务逻辑决定何时复用此 saea (dropClient 后复用)，否则继续接收
            if (!isfinishedRecv)
                StartReceive(recvSAEA);
        }

        /// <summary>
        /// 处理失败的数据接收
        /// </summary>
        /// <param name="recSAEA"></param>
        private void HandleBadRecv(SocketAsyncEventArgs recSAEA)
        {
            RemoveKeepAccepttedSAEAList((recSAEA.UserToken as UToken).ID);
        }

        private void RemoveKeepAccepttedSAEAList(int tokenID)
        {
            SocketAsyncEventArgs saeaForRemove;

            //注意这里的写法，不能使用 containsKey 的方式，防止异步导致的重入
            if (this.keepAccepttedSAEAList.TryRemove(tokenID, out saeaForRemove))
            {
                //重置 token
                (saeaForRemove.UserToken as UToken).Reset();

                //重置 RemoteEndPoint
                saeaForRemove.RemoteEndPoint = this.serverSetting.EP;

                //push 回 SAEA 池中，复用
                this.recvSAEAPool.Push(saeaForRemove);
            }
        }

        /// <summary>
        /// 处理失败的数据发送
        /// </summary>
        /// <param name="acceptSAEA"></param>
        private void HandleBadSend(SocketAsyncEventArgs sendSAEA)
        {
            //重置 token
            (sendSAEA.UserToken as UToken).Reset();
            //重置 RemoteEndPoint
            sendSAEA.RemoteEndPoint = null;
            //Push 回 SAEA 池中，复用
            this.sendSAEAPool.Push(sendSAEA);
        }


        #region 从池中获取SAEA 对象

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
        /// 创建一个新的用于接收/发送数据的 SAEA 对象
        /// </summary>
        /// <param name="pool"></param>
        /// <returns></returns>
        private SocketAsyncEventArgs CreateNewSAEAForRecvAndSend(TokenUseType useType)
        {
            SocketAsyncEventArgs recvAndSendSAEA = new SocketAsyncEventArgs();

            //如果是用于接收到话，给 RemoteEndPoint 赋值， 如果是发送，则由 send 方法去赋值
            if (useType == TokenUseType.Receive)
                recvAndSendSAEA.RemoteEndPoint = this.serverSetting.EP;

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
                case SocketAsyncOperation.ReceiveFrom:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.SendTo:
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
            bool ret = this.localSocket.SendToAsync(sendSAEA);

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
                HandleBadSend(sendSAEA);
                //返回，结束一个 send 过程
                return;
            }

            List<string> errors = new List<string>();
            if (SAEAByteHandler.HandleSend(sendSAEA, token, errors))
            {
                //执行到此，表示发送完成
                HandleBadSend(sendSAEA);
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
        /// 初始化 socketclient
        /// </summary>
        protected override void InitLocalSocket()
        {
            this.localSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            this.localSocket.SendTimeout = this.serverSetting.RecvSendTimeOut;
            this.localSocket.ReceiveTimeout = this.serverSetting.RecvSendTimeOut;

            this.localSocket.Bind(this.serverSetting.EP);

            StartReceive();
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

                //赋值 RemoteEndPoint
                sendSAEA.RemoteEndPoint = keepedSAEA.RemoteEndPoint;

                //给发送 byte 数组赋值
                (sendSAEA.UserToken as UToken).Buffer = data;

                StartSend(sendSAEA);
            }
            else
                throw new Exception("此 tokenID 对应的客户端不存在，无法向其发送数据！");
        }

        public override void DropClient(int tokenID)
        {
            RemoveKeepAccepttedSAEAList(tokenID);
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

            //释放 keepAccepttedSAEAList 中的资源
            if (this.keepAccepttedSAEAList != null)
            {
                foreach (SocketAsyncEventArgs arg in this.keepAccepttedSAEAList.Values)
                    arg.Dispose();
            }

            //释放 saea pool 资源
            if (this.sendSAEAPool != null)
                this.sendSAEAPool.Dispose();

            if (this.recvSAEAPool != null)
                this.recvSAEAPool.Dispose();
        }

        #endregion
    }
}
