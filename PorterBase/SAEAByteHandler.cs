using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using ZdCache.Common.SizeGetter;

namespace ZdCache.PorterBase
{
    /// <summary>
    /// 用于对 SAEA 接收到的数据进行拆包处理
    /// </summary>
    internal class SAEAByteHandler
    {
        /// <summary>
        /// 对 SocketAsyncEventArgs 接收到的数据进行处理， 此方法不抛出异常，错误通过 errors 参数传出
        /// </summary>
        /// <param name="recvSAEA"></param>
        /// <param name="token"></param>
        /// <param name="getter"></param>
        /// <param name="cbArgsList">存储本次获取到的 CallBackListArg</param>
        /// <returns>表示是否接收完成</returns>
        internal static bool HandleRecv(SocketAsyncEventArgs recvSAEA, UToken token, ISizeGetter getter, List<CallBackListArg> cbArgsList, List<string> errors)
        {
            return HandleRecv(recvSAEA.BytesTransferred, token, getter, cbArgsList, errors);
        }

        /// <summary>
        /// 对接收到的数据进行拆包处理， 此方法不抛出异常，错误通过 errors 参数传出
        /// </summary>
        /// <param name="recvSAEA"></param>
        /// <param name="token"></param>
        /// <param name="sizeGetter"></param>
        /// <param name="cbArgsList">存储本次获取到的 CallBackListArg</param>
        /// <returns>表示是否接收完成</returns>
        internal static bool HandleRecv(int bytesTransferred, UToken token, ISizeGetter getter, List<CallBackListArg> cbArgsList, List<string> errors)
        {
            try
            {
                //偏移接收缓存的 offset
                token.OffSet += bytesTransferred;

                //如果是第一次获取，则需要获取其数据包总大小
                int outPacketSize;
                //读取的起始位置，如果是第一次读取一个包 （token.RecvPacketLength == -1），则需要考虑这个位置，否则不需要考虑，为0
                int realStartIndex = 0;

                if (token.RecvPacketLength == -1)
                {
                    switch (getter.GetSize(token.Buffer, token.OffSet, out realStartIndex, out outPacketSize))
                    {
                        case SizeGetResult.Ignore:
                            //如果是 ignore，则直接返回 true，忽略此包
                            return true;

                        case SizeGetResult.ParaBytesNotEnough:
                            break;

                        case SizeGetResult.NoSizeFlag: //如果没有长度标志位，则直接认为本次就读取完了
                        case SizeGetResult.Success:
                            token.RecvPacketLength = outPacketSize;
                            break;
                    }
                }

                //如果 BufferRecv 都读取满了，都不能获取到 Header， 则抛出异常
                if (token.RecvPacketLength == -1 && token.OffSet == token.Buffer.Length)
                    throw new Exception("获取数据包长度标志位所需字节数大于UserToken.BufferRecv 长度，请检查数据传输协议，或者增加 BufferRecv 长度。");

                //如果获取到了 TotalSize， 则进行判断，是否已经读取完所有数据
                if (token.RecvPacketLength > 0 && (token.Container.Length + token.OffSet - realStartIndex) >= token.RecvPacketLength)
                {
                    if (token.Container.Length + token.OffSet - realStartIndex == token.RecvPacketLength)
                    {
                        //如果相等，则正好表示一个记录
                        //保存到 token.Container 中
                        token.Container.Assign(token.Buffer, realStartIndex, token.OffSet - realStartIndex);
                        cbArgsList.Add(new CallBackListArg(token.ID, token.Container.BytesList));

                        //完成接收
                        return true;
                    }
                    else
                    {
                        //如果大于，则表示收到了不止一个包的数据，需要拆分，拆分逻辑如下：
                        // 1、先把第一个记录组合齐(显然此时 token.Container.Length 要小于 token.RecvPacketLength)
                        // 2、此次收到的数据（token.Offset）减去第一步所用去的字节数，得到剩余的字节，依次组成记录
                        // 3、如果最后的不够用于获取 size（RecvPacketLength），则改变 token 的 offset，继续接收
                        // 4、如果最后有剩余的字节（不够一个记录），则存到到 token.Container 中，以便下一次接收
                        int firstRecordNeed = token.RecvPacketLength - token.Container.Length;

                        token.Container.Assign(token.Buffer, realStartIndex, firstRecordNeed);
                        cbArgsList.Add(new CallBackListArg(token.ID, token.Container.BytesList));

                        //至此，则 container 中的数据不再需要了
                        token.Container.Reset();
                        //将 token 的 RecvPacketLength 设为 -1，因为下面的 while 循环是对新的 CachedObject 进行处理
                        token.RecvPacketLength = -1;

                        int i = firstRecordNeed + realStartIndex;

                        while (i < token.OffSet)
                        {
                            byte[] dataTemp = new byte[token.OffSet - i];
                            Array.Copy(token.Buffer, i, dataTemp, 0, dataTemp.Length);

                            switch (getter.GetSize(dataTemp, dataTemp.Length, out realStartIndex, out outPacketSize))
                            {
                                case SizeGetResult.Ignore:
                                    //如果 ignore，则返回 true，忽略
                                    return true;

                                case SizeGetResult.NoSizeFlag:
                                    //如果没有长度标志位，则剩余的数据作为一个记录
                                    byte[] noSizeDataTemp = new byte[outPacketSize - realStartIndex];
                                    Array.Copy(dataTemp, realStartIndex, noSizeDataTemp, 0, noSizeDataTemp.Length);
                                    cbArgsList.Add(new CallBackListArg(token.ID, noSizeDataTemp));
                                    //接收完成，退出循环
                                    return true;

                                case SizeGetResult.ParaBytesNotEnough:
                                    //将 dataTemp 中的数据 copy 到 buffer 中，然后继续接收
                                    Array.Copy(dataTemp, token.Buffer, dataTemp.Length);
                                    //设置 offSet
                                    token.OffSet = dataTemp.Length;
                                    //未接受完成，退出循环
                                    return false;

                                case SizeGetResult.Success:
                                    //判断字节是否够
                                    if (i + realStartIndex + outPacketSize <= token.OffSet)
                                    {
                                        byte[] innerData = new byte[outPacketSize];
                                        Array.Copy(dataTemp, realStartIndex, innerData, 0, innerData.Length);
                                        cbArgsList.Add(new CallBackListArg(token.ID, innerData));

                                        //递增i
                                        i += realStartIndex + outPacketSize;

                                        //如果相等，则表示刚好收取完
                                        if (i == token.OffSet)
                                            return true;                                        
                                    }
                                    else
                                    {
                                        //执行到此，表示字节数不够，则保存剩余数据，继续接收
                                        //设置 RecvPacketLength
                                        token.RecvPacketLength = outPacketSize;

                                        token.Container.Assign(dataTemp, realStartIndex, dataTemp.Length - realStartIndex);
                                        token.OffSet = 0;
                                        //未接收完成，退出循环
                                        return false;
                                    }
                                    break;
                            }
                        }
                    }
                }
                else
                {
                    //执行到此，表示没获取到 TotalSize 或者没读取完所有数据，需继续读取
                    //如果缓存满了，则需要保存到 token.Container 中
                    if (token.OffSet == token.Buffer.Length)
                    {
                        //注意，此处个前提：TotalSize 已经获得了，因为上面进行了抛出异常的判断
                        token.Container.Assign(token.Buffer, realStartIndex, token.Buffer.Length - realStartIndex);
                        //重置 offset
                        token.OffSet = 0;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                errors.Add(ex.Message + " " + ex.StackTrace);
                //一旦异常，则放弃此次接受
                return true;
            }
        }


        /// <summary>
        /// 对 SocketAsyncEventArgs 发送的数据进行处理， 此方法不抛出异常，错误通过 errors 参数传出
        /// </summary>
        /// <param name="recvSAEA"></param>
        /// <param name="token"></param>
        /// <returns>表示是否发送完成全部数据</returns>
        internal static bool HandleSend(SocketAsyncEventArgs sendSAEA, UToken token, List<string> errors)
        {
            try
            {
                token.OffSet += sendSAEA.BytesTransferred;
                if (token.OffSet >= token.Buffer.Length)
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                errors.Add(ex.Message + " " + ex.StackTrace);
                return true;
            }
        }
    }
}
