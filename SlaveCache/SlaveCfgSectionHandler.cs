using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Xml;

namespace ZdCache.SlaveCache
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
            cfg.CallBackThreadCount = int.Parse(keyValues["CallBackThreadCount"]);
            cfg.CacheExpireTM = int.Parse(keyValues["CacheExpireTM"]);
            cfg.MaxCacheSize = long.Parse(keyValues["MaxCacheSize"]) * 1024 * 1024;

            return cfg;
        }

        #endregion
    }
}
