using System;
using System.Collections.Generic;
using ZdCache.Common.ActionModels;
using ZdCache.Common.CDataType;

namespace ZdCache.Common.CacheCommon
{
    /// <summary>
    /// 数据协议处理
    /// 协议：PackageHeader[整个数据流大小(4) +  + 包总数(4) + 包序号(4) + AcCallID(16 guid)] + 用户数据
    /// 
    /// 第一个包的用户数据至少包含如下： 
    ///     ActionKind(1) + ActionResult(1) + DataType(1) + argCategory(1) + argID的长度(4) + argID(byte array)
    /// 后续包的用户数据依次包含：
    ///     arg数据(byte[], 如果长度允许的话，也有可能 arg 数据都合并到第一个包中)
    /// 
    /// </summary>
    public class DataArrangement
    {
        /// <summary>
        /// 获取发送的数据列表
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public static List<byte[]> GetCallBytes(CallArgsModel arg)
        {
            string typeNM = arg.AcArgs.GetType().FullName;
            if (CacheDataTypeList.TypeList.ContainsKey(typeNM))
            {
                List<byte[]> allBytes = new List<byte[]>();

                byte[] bytes_arcID = Function.GetBytesFromStr(arg.AcArgs.ID);
                byte[] bytes_arcIDLength = Function.GetBytesFormInt32(bytes_arcID.Length);

                //包头长度
                int headerLen = ConstParams.Protocol_PackageLength +
                    ConstParams.Protocol_PackageCount +
                    ConstParams.Protocol_PackageOrder +
                    ConstParams.Protocol_ActionCallIDLength;

                //第一个包长度
                int firstPackageLen = headerLen +
                    ConstParams.Protocol_ActionKindLength +
                    ConstParams.Protocol_ActionResultLength +
                    ConstParams.Protocol_DataTypeLength +
                    ConstParams.Protocol_CategoryLength +
                    ConstParams.Protocol_ArgIDLength +
                    bytes_arcID.Length;

                bool isMerged = false;
                if (arg.AcArgs.BytesData == null || arg.AcArgs.BytesData.Count == 0)
                    isMerged = true;
                else if (arg.AcArgs.BytesData.Count == 1
                    && firstPackageLen + arg.AcArgs.BytesData[0].Length <= ConstParams.BufferBlockSize)
                {
                    //如果 arg.AcArgs.BytesData.Count>1，则说明不用合并到第一个包中了，肯定长度超出
                    firstPackageLen += arg.AcArgs.BytesData[0].Length;
                    isMerged = true;
                }

                byte[] firstPacBytes = new byte[firstPackageLen];

                byte[] bytes_acCallID = arg.AcCallID.ToByteArray();
                //包的数量
                int packageCount = isMerged ? 1 : (1 + arg.AcArgs.BytesData.Count);
                byte[] bytes_packageCount = Function.GetBytesFormInt32(packageCount);
                //包序号
                int packageOrder = 1;

                //组装数据
                int pos = 0;
                Array.Copy(Function.GetBytesFormInt32(firstPackageLen), firstPacBytes, ConstParams.Protocol_PackageLength);
                pos += ConstParams.Protocol_PackageLength;

                Array.Copy(bytes_packageCount, 0, firstPacBytes, pos, ConstParams.Protocol_PackageCount);
                pos += ConstParams.Protocol_PackageCount;

                Array.Copy(Function.GetBytesFormInt32(packageOrder), 0, firstPacBytes, pos, ConstParams.Protocol_PackageOrder);
                pos += ConstParams.Protocol_PackageOrder;

                Array.Copy(bytes_acCallID, 0, firstPacBytes, pos, ConstParams.Protocol_ActionCallIDLength);
                pos += ConstParams.Protocol_ActionCallIDLength;

                firstPacBytes[pos] = (byte)arg.AcKind;
                pos += ConstParams.Protocol_ActionKindLength;

                firstPacBytes[pos] = (byte)arg.AcResult;
                pos += ConstParams.Protocol_ActionResultLength;

                firstPacBytes[pos] = CacheDataTypeList.TypeList[typeNM];
                pos += ConstParams.Protocol_DataTypeLength;

                firstPacBytes[pos] = arg.AcArgs.Category;
                pos += ConstParams.Protocol_CategoryLength;

                Array.Copy(bytes_arcIDLength, 0, firstPacBytes, pos, ConstParams.Protocol_ArgIDLength);
                pos += ConstParams.Protocol_ArgIDLength;

                Array.Copy(bytes_arcID, 0, firstPacBytes, pos, bytes_arcID.Length);
                pos += bytes_arcID.Length;

                if (isMerged)
                {
                    if (arg.AcArgs.BytesData != null)
                        Array.Copy(arg.AcArgs.BytesData[0], 0, firstPacBytes, pos, arg.AcArgs.BytesData[0].Length);

                    allBytes.Add(firstPacBytes);
                }
                else
                {
                    allBytes.Add(firstPacBytes);

                    foreach (byte[] contentData in arg.AcArgs.BytesData)
                    {
                        pos = 0;
                        int contentPackageLen = headerLen + contentData.Length;
                        byte[] contentPacBytes = new byte[contentPackageLen];

                        Array.Copy(Function.GetBytesFormInt32(contentPackageLen), contentPacBytes, ConstParams.Protocol_PackageLength);
                        pos += ConstParams.Protocol_PackageLength;

                        Array.Copy(bytes_packageCount, 0, contentPacBytes, pos, ConstParams.Protocol_PackageCount);
                        pos += ConstParams.Protocol_PackageCount;

                        //packageOrder 递增
                        Array.Copy(Function.GetBytesFormInt32(++packageOrder), 0, contentPacBytes, pos, ConstParams.Protocol_PackageOrder);
                        pos += ConstParams.Protocol_PackageOrder;

                        Array.Copy(bytes_acCallID, 0, contentPacBytes, pos, ConstParams.Protocol_ActionCallIDLength);
                        pos += ConstParams.Protocol_ActionCallIDLength;

                        Array.Copy(contentData, 0, contentPacBytes, pos, contentData.Length);

                        allBytes.Add(contentPacBytes);
                    }
                }

                return allBytes;
            }
            else
                throw new Exception("要缓存的数据类型无法识别(CacheDataTypeList 中不存在或非继承于CacheDataType)");
        }

        /// <summary>
        /// 从收到的字节数组获取 CachedObject
        /// </summary>
        /// <param name="container">数据合并容器</param>
        /// <param name="dataList"></param>
        /// <param name="generateRealObj">是否创建 BaseCacheDataType.RealObj，对于 slave 而言，只需要存储其 byte[] 数组，无需进行转换为 RealObj，加快速度</param>
        /// <returns></returns>
        public static CallArgsModel GetCallObject(PackageDataContainer packageContainer, List<byte[]> dataList, bool generateRealObj)
        {
            if (dataList.Count <= 0)
                throw new Exception("字节数组为空，无法解析为 CallArgsModel");

            //存储主体的数据
            List<byte[]> cachedContent = null;

            //从包总数开始读起
            int listStartIndex = 0, startPos = ConstParams.Protocol_PackageLength;

            byte[] packageCountBytes = GetBytesFromPos(dataList, ref listStartIndex, ref startPos, ConstParams.Protocol_PackageCount);
            byte[] packageOrderBytes = GetBytesFromPos(dataList, ref listStartIndex, ref startPos, ConstParams.Protocol_PackageOrder);
            byte[] acCallIDBytes = GetBytesFromPos(dataList, ref listStartIndex, ref startPos, ConstParams.Protocol_ActionCallIDLength);

            int packageCount = Function.GetInt32FromBytes(packageCountBytes, 0);
            int packageOrder = Function.GetInt32FromBytes(packageOrderBytes, 0);
            Guid callID = new Guid(acCallIDBytes);

            if (packageOrder == 1)
            {
                //firstPackage
                byte[] actionKindBytes = GetBytesFromPos(dataList, ref listStartIndex, ref startPos, ConstParams.Protocol_ActionKindLength);
                byte[] actionResultBytes = GetBytesFromPos(dataList, ref listStartIndex, ref startPos, ConstParams.Protocol_ActionResultLength);
                byte[] dataTypeBytes = GetBytesFromPos(dataList, ref listStartIndex, ref startPos, ConstParams.Protocol_DataTypeLength);
                byte[] argCategoryBytes = GetBytesFromPos(dataList, ref listStartIndex, ref startPos, ConstParams.Protocol_CategoryLength);
                byte[] argIDLengthBytes = GetBytesFromPos(dataList, ref listStartIndex, ref startPos, ConstParams.Protocol_ArgIDLength);

                byte[] argIDContentBytes = GetBytesFromPos(dataList, ref listStartIndex, ref startPos,
                    Function.GetInt32FromBytes(argIDLengthBytes, 0));

                //ActionKind (1个字节)
                ActionKind acKind = (ActionKind)actionKindBytes[0];
                //ActionResult (1个字节)
                ActionResult acResult = (ActionResult)actionResultBytes[0];
                //datatype (1个字节)
                byte dataType = dataTypeBytes[0];
                //argCategory (1个字节)
                byte acCategory = argCategoryBytes[0];
                //argID
                string id = Function.GetStrFromBytes(argIDContentBytes, 0, argIDContentBytes.Length);

                string dataTypeName;
                if (!CacheDataTypeList.TypeListReversed.TryGetValue(dataType, out dataTypeName))
                    throw new Exception("返回的数据类型无法识别(CacheDataTypeList 中不存在或非继承于CacheDataType)");

                if (packageCount == 1)
                {
                    cachedContent = GetBytesFromPos(dataList, ref listStartIndex, ref startPos);
                    return GenerateCallArgsModel(dataTypeName, acCategory, id, cachedContent, generateRealObj,
                        callID, acKind, acResult);
                }
                else
                {
                    packageContainer.AddBuiltData(callID, packageCount, packageOrder,
                        acKind, acResult, dataTypeName, acCategory, id);
                }
            }
            else
            {
                List<byte[]> content = GetBytesFromPos(dataList, ref listStartIndex, ref startPos);
                packageContainer.AddBuiltData(callID, packageCount, packageOrder, content);
            }

            //判断所有拆分包是否完成接收
            if (packageContainer.IsAllPackectBuilt(callID))
            {
                //清除
                PackageDataArg packageArg = packageContainer.ClearBuiltData(callID);
                if (packageArg != null)
                {
                    cachedContent = new List<byte[]>();
                    List<byte[]> dataInOrder;

                    //主体数据，在 PackageDataArg 中是从  packageOrder = 2 开始; 结束于 packageArg.PackageCount
                    for (int order = 2; order <= packageArg.PackageCount; order++)
                    {
                        dataInOrder = packageArg.GetPackage(order);
                        if (dataInOrder != null)
                            cachedContent.AddRange(dataInOrder);
                    }

                    return GenerateCallArgsModel(packageArg.DataTypeName, packageArg.Category, packageArg.ArgID, cachedContent,
                        generateRealObj, packageArg.CallID, packageArg.AcKind, packageArg.AcResult);
                }
            }

            return null;
        }


        #region private

        private static CallArgsModel GenerateCallArgsModel(string dataTypeName, byte acCategory, string id, List<byte[]> cachedContent, bool generateRealObj,
            Guid callID, ActionKind acKind, ActionResult acResult)
        {
            if (cachedContent != null && cachedContent.Count > 0)
            {
                BaseCacheDataType cacheObj = (BaseCacheDataType)Activator.CreateInstance(Type.GetType(dataTypeName),
                    new object[] { acCategory, id, cachedContent, generateRealObj });
                return new CallArgsModel(callID, acKind, acResult, cacheObj);
            }
            else
                throw new Exception("cachedContent 为空，无法生成 CallArgsModel！");
        }

        /// <summary>
        /// 从 dataList 中截取出固定长度的字节数组
        /// </summary>
        private static byte[] GetBytesFromPos(List<byte[]> dataList, ref int listStartIndex, ref int startPos, int length)
        {
            byte[] retBytes = new byte[length];
            int readLength = 0, tempLen = 0;

            for (; listStartIndex < dataList.Count && readLength < length; listStartIndex++)
            {
                //判断是否能够读取完全
                if (startPos + length - readLength <= dataList[listStartIndex].Length)
                {
                    Array.Copy(dataList[listStartIndex], startPos, retBytes, readLength, length - readLength);
                    tempLen = length - readLength;
                    readLength += tempLen;
                    startPos += tempLen;
                    break;
                }
                else
                {
                    //读取到最后
                    Array.Copy(dataList[listStartIndex], startPos, retBytes, readLength, dataList[listStartIndex].Length - startPos);
                    readLength += dataList[listStartIndex].Length - startPos;
                    startPos = 0;
                }
            }

            return retBytes;
        }

        /// <summary>
        /// 从 dataList 中截取出固定长度的字节数组 (最后一个，因数据可能会比较大，不能放到一个 byte 数组中)
        /// </summary>
        private static List<byte[]> GetBytesFromPos(List<byte[]> dataList, ref int listStartIndex, ref int startPos)
        {
            List<byte[]> retbytes = new List<byte[]>();

            if (startPos > 0)
            {
                byte[] firstBytes = new byte[dataList[listStartIndex].Length - startPos];
                Array.Copy(dataList[listStartIndex], startPos, firstBytes, 0, firstBytes.Length);
                retbytes.Add(firstBytes);
                listStartIndex++;
            }

            for (; listStartIndex < dataList.Count; listStartIndex++)
            {
                retbytes.Add(dataList[listStartIndex]);
            }

            return retbytes;
        }

        #endregion
    }
}
