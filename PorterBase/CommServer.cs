using System;
using System.Collections.Generic;
using System.Text;
using System.IO.Ports;
using ZdCache.Common;
using ZdCache.Common.SizeGetter;
using ZdCache.PorterBase.Setting;

namespace ZdCache.PorterBase
{
    public class CommServer : Porter
    {
        private CommServerSettings commSetting;
        private SerialPort sp;

        //回调处理器
        private CallBackHandler callBackHandler;

        /// <summary>
        /// CommServer 构造函数
        /// </summary>
        /// <param name="comPort">端口号</param>
        /// <param name="baudRate">波特率</param>
        /// <param name="parity">校验位</param>
        /// <param name="dataBits">数据位</param>
        /// <param name="stopBits">停止位</param>
        public CommServer(CommServerSettings setting, ErrorTracer tracer)
            : base(tracer)
        {
            this.sp = new SerialPort(setting.Port, setting.BaudRate, setting.CommParity, setting.DataBits, setting.CommStopBits);
            this.sp.ReceivedBytesThreshold = 10;
            this.sp.ReadTimeout = setting.RecvSendTimeOut;
            this.sp.WriteTimeout = setting.RecvSendTimeOut;
            this.sp.RtsEnable = true;//必须为true 这样串口才能接收到数据
            this.sp.DataReceived += new SerialDataReceivedEventHandler(this.sp_DataReceived);
            this.callBackHandler = new CallBackHandler(setting.CallBackThreadCount, setting.OnReceive);
            this.commSetting = setting;
        }

        /// <summary>
        /// 打开设备
        /// </summary>
        public void Active()
        {
            if (!this.sp.IsOpen)
            {
                try
                {
                    this.sp.Open();
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }

        /// <summary>
        /// 数据接收事件。 
        ///    注意，此方法需要整个 try catch， 因为 close 被调用时， sp 被设置为 null 了，而接收是异步的，可能导致异常，需要处理。 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void sp_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                UToken token = new UToken(TokenUseType.Receive);
                List<CallBackListArg> cbArgsList = new List<CallBackListArg>();

                List<string> errors = new List<string>();
                try
                {
                    //将 ReceivedBytesThreshold 值提高，保证数据没接收完成前不新触发 datareceived 事件
                    this.sp.ReceivedBytesThreshold = this.sp.ReadBufferSize;
                    while (true)
                    {
                        int bytesTransferred = this.sp.Read(token.Buffer, token.OffSet, token.Buffer.Length - token.OffSet);

                        if (SAEAByteHandler.HandleRecv(bytesTransferred, token, this.commSetting.MySizeGetter, cbArgsList, errors))
                            break;

                        //未读取完，睡眠10毫秒, 继续读取
                        SleepHelper.Sleep(10);
                    }
                }
                catch (Exception ex)
                {
                    //这里要进行异常处理，因为 datareceived 是被动触发的，不然抛出，线程没有地方可处理，服务就挂了
                    this.TraceError(ErrorType.Receive, token.ID, "comm receive error:" + ex.Message);
                }
                finally
                {
                    //放到 finally 中，避免 while 循环中某个时刻读取超时，而导致之前已经获取到的记录丢失
                    foreach (CallBackListArg item in cbArgsList)
                        this.callBackHandler.Push(item);

                    if (errors.Count > 0)
                    {
                        foreach (string msg in errors)
                            this.TraceError(ErrorType.Receive, token.ID, msg);
                    }

                    //ClearInBuffer();

                    //注意此处 sleepHelper 的使用方式，对于能自行结束的方法，应该调用 RemoveResetEvent
                    SleepHelper.RemoveResetEvent();
                    this.sp.ReceivedBytesThreshold = 1;
                }
            }
            catch
            {
            }
        }

        private void ClearOutBuffer()
        {
            try
            {
                //清空发送缓冲区
                this.sp.DiscardOutBuffer();
            }
            catch
            {
            }
        }

        private void ClearInBuffer()
        {
            try
            {
                //清空接收缓冲区
                this.sp.DiscardInBuffer();
            }
            catch
            {
            }
        }

        #region IPorter 成员

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="data"></param>
        public override void Send(byte[] data)
        {
            //打开设备
            Active();

            //注销事件关联，为发送做准备
            this.sp.DataReceived -= this.sp_DataReceived;
            try
            {
                //ClearOutBuffer();
                this.sp.Write(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                this.sp.DataReceived += this.sp_DataReceived;
            }
        }

        /// <summary>
        /// 关闭 Comm 设备
        /// </summary>
        public override void Close()
        {
            try
            {
                if (this.sp != null)
                {
                    this.sp.Close();
                    this.sp = null;
                }
            }
            catch (Exception ex)
            {
                this.TraceError(ErrorType.Other, 0, string.Format("close Comm failed：{0}", ex.Message));
            }

            if (this.callBackHandler != null)
                this.callBackHandler.Dispose();
        }

        #endregion
    }
}