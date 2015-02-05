using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Xml;
using ZdCache.SlaveCache;
using ZdCache.Common;
using Microsoft.VisualBasic.Devices;

namespace Test_Slave
{
    internal class SlaveCfgSectionHandler : IConfigurationSectionHandler
    {
        #region IConfigurationSectionHandler 成员

        public object Create(object parent, object configContext, System.Xml.XmlNode section)
        {
            if (section == null)
                throw new ArgumentNullException("section");

            SlaveCfg cfg = new SlaveCfg();

            XmlNodeList masterNodes = section.SelectNodes("masters/add");
            foreach (XmlNode node in masterNodes)
            {
                SlaveCfg.IpAndPort ipInfo = new SlaveCfg.IpAndPort() { IP = node.Attributes["ip"].Value.Trim(), Port = int.Parse(node.Attributes["port"].Value.Trim()) };
                cfg.Masters.Add(ipInfo.ID, ipInfo);
            }

            Dictionary<string, string> keyValues = new Dictionary<string, string>();
            XmlNodeList keyValueNodes = section.SelectNodes("add");
            foreach (XmlNode node in keyValueNodes)
            {
                keyValues.Add(node.Attributes["key"].Value.Trim(), node.Attributes["value"].Value.Trim());
            }

            cfg.RecvAndSendTimeout = int.Parse(keyValues["RevcAndSendTimeout"]);
            cfg.CallBackThreadCount = Function.GetCpuCoreCount();
            cfg.CacheExpireTM = int.Parse(keyValues["CacheExpireTM"]);
            cfg.MaxCacheSize = GetMaxCacheSize();

            return cfg;
        }

        /// <summary>
        /// 获取允许的最大缓存大小。
        /// X86 下，32位进程最大2GB（3GB，设置boot.ini 或者win7下使用bcdedit，同时开启IMAGE_FILE_LARGE_ADDRESS_AWARE）
        /// X64 下，32为进程最大2GB（4GB，开启IMAGE_FILE_LARGE_ADDRESS_AWARE）。 64位进程最大8TB（具体值和操作系统有关）
        /// 上述是理想情况，实际因为GAC以及其他的开销，2GB模式下，最大能申请到 1.4-1.6GB的空间
        /// 当缓存接近这个最大值时，会主动触发缓存的过期策略
        /// </summary>
        /// <returns></returns>
        private long GetMaxCacheSize()
        {
            ComputerInfo computerInfo = new ComputerInfo();
            bool isX64 = IntPtr.Size == 8;
            if (isX64 || computerInfo.AvailablePhysicalMemory <= (long)2 * 1024 * 1024 * 1024)
                return (long)(computerInfo.AvailablePhysicalMemory * 0.8);
            else
                return (long)(1.4 * 1024 * 1024 * 1024);
        }

        #endregion
    }
}
