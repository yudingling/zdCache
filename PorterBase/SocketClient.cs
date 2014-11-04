using System;
using System.Collections.Generic;
using System.Threading;
using System.Text;
using System.Net.Sockets;
using System.Net;
using ZdCache.Common;
using ZdCache.PorterBase.Setting;

namespace ZdCache.PorterBase
{
    public class SocketClient : SocketBase
    {
        //数据接收 SAEA 对象
        private SocketAsyncEventArgs recvSAEA;
        //数据发送 SAEA 池
        private SAEAPool sendSAEAPool;

        //client 配置类
        private SocketClientSettings clientSetting;

        //控制连接超时
        private static ManualResetEvent timeOutObject = new ManualResetEvent(false); 

        public SocketClient(SocketClientSettings setting, ErrorTracer tracer)
            : base(setting, tracer)
        {
            this.recvSAEA = CreateNewSAEAForRecvAndSend(TokenUseType.Receive);
            //用于处理数据接收/发送是一个相对耗时的过程，所以并行存在的数量是比较多的，capacity 放大些
            this.sendSAEAPool = new SAEAPool(1000);

            this.clientSetting = setting;

            InitLocalSocket();
        }

        #region connect  async

        /// <summary>
        /// 进行远程连接，带超时控制
        /// </summary>
        private void SkForSendConnect()
        {
            timeOutObject.Reset(); //设置为无信号，则 waitone 方法将中断当前线程
            this.localSocket.BeginConnect(this.clientSetting.EP, new AsyncCallback(SkForSendConnectCallBack), this.localSocket);
            //waitone 将阻止当前线程，直到 timeOutObject 对象被 set 或者 超时时间到了
            if (timeOutObject.WaitOne(this.clientSetting.RecvSendTimeOut, false))
            {
                if (!this.localSocket.Connected)
                    throw new Exception("连接远程服务器失败！");
                else
                {
                    //开始接收
                    StartReceive();
                }
            }
            else
            {
                throw new Exception("远程服务器连接超时！");
            }
        }

        private void SkForSendConnectCallBack(IAsyncResult ar)
        {
            try
            {
                Socket sk = ar.AsyncState as Socket;
                sk.EndConnect(ar);
            }
            catch
            {
            }
            finally
            {
                //设置为有信号， waitone 返回true
                timeOutObject.Set();
            }
        }

        #endregion

        #region recive

        /// <summary>
        /// 异步数据的接收
        /// </summary>
        private void StartReceive()
        {
            try
            {
                UToken token = this.recvSAEA.UserToken as UToken;

                //设置接收缓冲区
                recvSAEA.SetBuffer(token.Buffer, token.OffSet, token.Buffer.Length - token.OffSet);

                //进行异步接收
                bool ret = this.localSocket.ReceiveAsync(recvSAEA);

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

            if (SAEAByteHandler.HandleRecv(recSAEA, token, this.clientSetting.MySizeGetter, cbArgList, errors))
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
                    this.TraceError(msg);
            }

            //继续读取 (读取未读取完的数据，或者开启一个新的读取)
            StartReceive();
        }

        /// <summary>
        /// 处理失败的数据接收
        /// </summary>
        /// <param name="recSAEA"></param>
        private void HandleBadRecv(SocketAsyncEventArgs recSAEA)
        {
            UToken token = recSAEA.UserToken as UToken;

            //重置 token
            token.Reset();

            //重新连接
            ReConnect();
        }

        /// <summary>
        /// 创建一个新的用于接收/发送数据的 SAEA 对象
        /// </summary>
        private SocketAsyncEventArgs CreateNewSAEAForRecvAndSend(TokenUseType useType)
        {
            SocketAsyncEventArgs recvAndSendSAEA = new SocketAsyncEventArgs();
            recvAndSendSAEA.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
            //给其附加上 UserTooken，用于存储接收/发送的数据等信息
            recvAndSendSAEA.UserToken = new UToken(useType);
            return recvAndSendSAEA;
        }

        #endregion

        #region send

        /// <summary>
        /// 异步发送数据
        /// </summary>
        private void StartSend(SocketAsyncEventArgs sendSAEA)
        {
            //设置发送缓冲区
            UToken token = sendSAEA.UserToken as UToken;

            sendSAEA.SetBuffer(token.Buffer, token.OffSet, token.Buffer.Length - token.OffSet);

            //进行异步发送
            bool ret = this.localSocket.SendAsync(sendSAEA);

            //如果 SendAsync 返回值为 false，表示 SendAsync 操作被同步执行了，SAEA 对象的 Completed 事件将不会被执行
            //则此时需要进行手动调用 ProcessSend
            if (!ret)
                ProcessSend(sendSAEA);
        }

        /// <summary>
        /// SAEA 发送完后，对数据进行处理
        /// </summary>
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
                    this.TraceError(msg);
            }
        }

        /// <summary>
        /// 处理失败的数据发送
        /// </summary>
        private void HandleBadSend(SocketAsyncEventArgs sendSAEA, bool isCloseSocket)
        {
            //重置 token
            UToken token = sendSAEA.UserToken as UToken;

            if (isCloseSocket)
            {
                ShutDownClientSocket(this.localSocket);
                CloseClientSocket(this.localSocket);

                //重新连接
                ReConnect();
            }

            token.Reset();

            //Push 回 SAEA 池中，复用
            this.sendSAEAPool.Push(sendSAEA);
        }

        /// <summary>
        /// 获取一个用户接收/发送数据的 SAEA 对象， 不抛出异常
        /// </summary>
        private SocketAsyncEventArgs GetSendSAEAItem()
        {
            SocketAsyncEventArgs item;
            try
            {
                item = this.sendSAEAPool.Pop();
            }
            catch
            {
                item = CreateNewSAEAForRecvAndSend(TokenUseType.Send);
            }
            return item;
        }

        #endregion

        #region common

        /// <summary>
        /// send/recv 完成事件
        /// </summary>
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
                    //此处不能抛出异常，否则服务停止了
                    //throw new ArgumentException("the last operation on socket is not a receive or send!");
                    return;
            }
        }

        /// <summary>
        /// 重新连接server， 注意，此方法不能抛出异常，因为是在 saea 回调线程中调用的
        /// </summary>
        private void ReConnect()
        {
            try
            {
                CloseClientSocket(this.localSocket);
                InitLocalSocket();
            }
            catch
            {
            }
        }

        #endregion

        #region override socketbase 

        /// <summary>
        /// 初始化 socket client
        /// </summary>
        protected override void InitLocalSocket()
        {
            SocketType skType = this.clientSetting.PTType == ProtocolType.Tcp ? SocketType.Stream : SocketType.Dgram;
            this.localSocket = new Socket(AddressFamily.InterNetwork, skType, this.clientSetting.PTType);

            this.localSocket.SendTimeout = this.clientSetting.RecvSendTimeOut;
            this.localSocket.ReceiveTimeout = this.clientSetting.RecvSendTimeOut;
            //只有tcp 的时候才支持下面的 linger 设定
            if (this.clientSetting.PTType == ProtocolType.Tcp)
            {
                LingerOption lo = new LingerOption(true, 0);
                this.localSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, lo);
                this.localSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            }

            //异步连接，超时控制
            SkForSendConnect();
        }

        public override void Send(byte[] data)
        {
            if (this.localSocket != null)
            {
                if (this.localSocket.Connected)
                {
                    //注意，SocketAsyncEventArgs 对象在处于接收状态时，是不能用于发送的，所以此处需要一个新的 SocketAsyncEventArgs 来进行发送
                    SocketAsyncEventArgs sendSAEA = GetSendSAEAItem();

                    //给发送 byte 数组赋值
                    (sendSAEA.UserToken as UToken).Buffer = data;

                    StartSend(sendSAEA);
                }
                else
                    throw new Exception("socket 没有链接上远程服务器，发送失败!");
            }
            else
                throw new Exception("socket 为空，发送失败！");
        }

        /**
         * 奇怪的问题，如果设置 SocketOptionName.ReuseAddress，想重用套接字，并且设置了 LingerOption(false,0)，先 Disconnect(true) 再 connect， 如果连接频率过快，会出现如下的错误：
         *    “通常每个套接字地址(协议/网络地址/端口)只允许使用一次”
         * 大概过了30秒后，就不报错了，能连接上；
         * 
         * 查找原因，tcp 有2个很重要的连接状态：
         *   1、CLOSE_WAIT   
         *      对方主动关闭连接（disconnect 或 close 操作）或者网络异常导致连接中断，这时我方的状态会变成CLOSE_WAIT 此时我方要调用close()来使得连接正确关闭
         *   2、TIME_WAIT
         *      我方主动（disconnect 或 close 操作）断开连接，收到对方确认后状态变为TIME_WAIT。TCP协议规定TIME_WAIT状态会一直持续2MSL(即两倍的分段最大生存期)
         *      （此时间可通过 HKLM\System\CurrentControlSet\Services\Tcpip\Parameters\TCPTimedWaitDelay 来调整）
         *      以此来确保旧的连接状态不会对新连接产生影响。 处于TIME_WAIT状态的连接占用的资源不会被内核释放，所以作为服务器，在可能的情况下，尽量不要主动断开连接，
         *      以减少TIME_WAIT状态造成的资源浪费。
         *      
         * msdn的说法是：如果想避免主动断开连接方进入 Time_WAIT 状态，可使用TCP 的 SO_REUSEADDR 选项， 网上的说法是即使用这个选项，windows 下 Connect 也会失败，
         *               要解决windows 平台下的这个问题，还需要设置 SO_LINGER 选项，
         *               SO_LINGER 结构体的两个成员l_onoff 和 l_linger，不同的组合表达不同的方式：   
         *                     1、 l_onoff = 0; l_linger忽略
         *                         close()立刻返回，底层会将未发送完的数据发送完成后再释放资源，即优雅退出。
         *                     2. l_onoff != 0; l_linger = 0;
         *                         close()立刻返回，但不会发送未发送完成的数据，而是通过一个REST包强制的关闭socket描述符，即强制退出。
         *                     3. l_onoff != 0; l_linger > 0;
         *                         close()不会立刻返回，内核会延迟一段时间（l_linger的值），将剩余的数据发送完，
         *                         如果在指定时间内发送完成，则进入time_wait，如果超时了则直接退出，不进入time_wait。
         *              选择第2种组合，l_onoff != 0; l_linger = 0，则 TCP 就不会进入 TIME_WAIT 状态。
         *              如你所见，这样做虽然解决了问题，但是并不安全。通过以上方式设置 SO_LINGER状态，等同于设置 SO_DONTLINGER 状态，
         *              当tcp发生断开连接时的意外， 例如网线断开，linux上的TCP实现会依然认为该连接有效，而windows则会在一定时间后返回错误信息，
         *              这似乎可以通过设置SO_KEEPALIVE选项来解决，不过不知道这个选项是否对于所有平台都有效。
         * 
         * 但要注意的是，linger 选项只对 close 方法断开的连接有效，对 disconnect 无效，即：
         *     linger off，close和disconnect都会进入time_wait。  
         *     linger on ，disconnect仍然会直接进入time_wait， 而 Close 的行为则由 LINGER 决定。
         * 
         * 同时也在此处说明下非阻塞socket 与 linger 的关系：
         *     linger off，close时与阻塞的行为一致。
         *     linger on，如果超时>0，则会立即报错，因为此时的close会是阻塞行为。
         *     linger on，超时=0，和阻塞socket的行为一致
         * 
         * 
         * 有了以上即可知，采用 Disconnect 的方式去实现无间隔重用是不可行的，主动调用方一定会进入 time_wait ，不管是否设置 linger。
         * 
         * 要想无间隔连接，只能是通过 close，然后重新建立 socket 并连接，系统会随机申请一个新的端口来连接（没有指定固定端口），
         * 不用去管原来的那个端口是否进入了 time_wait 限制, 如果指定了固定的端口，则通过设置 LINGER 一样可以实现无间隔连接。 
         * 
         * */

        #endregion

    }
}
