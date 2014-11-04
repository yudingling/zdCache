﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZdCache.PorterBase;
using ZdCache.PorterBase.Setting;
using ZdCache.Common;
using ZdCache.Common.ActionModels;
using System.Collections.Concurrent;
using ZdCache.Common.CacheCommon;

namespace ZdCache.SlaveCache
{
    public delegate void DataFromBinding(string bindingID, CallArgsModel model);
    public delegate void SendErrorFromBinding(string bindingID, Exception ex);

    public sealed class ClientBinding : BasePorter
    {
        //唯一id
        private string id = "";

        private bool running = true;

        private DataFromBinding onData;
        private SendErrorFromBinding onSendError;
        private PackageDataContainer packageContainer;

        public ClientBinding(string bindingId, SocketClientSettings setting, ErrorTracer tracer,
            DataFromBinding dataFromBinding, SendErrorFromBinding sendErrorFromBinding)
            : base(setting, tracer)
        {
            this.id = bindingId;

            this.onData = dataFromBinding;
            this.onSendError = sendErrorFromBinding;

            this.packageContainer = new PackageDataContainer();
        }

        protected override void DataReceived(List<byte[]> data)
        {
            //此处 GetCachedObject 是不需要 generateRealObj
            CallArgsModel args = DataArrangement.GetCallObject(this.packageContainer, data, false);
            if (args != null && this.running && this.onData != null)
                this.onData(this.id, args);
        }

        protected override void SendErrorOccured(Exception ex)
        {
            if (this.onSendError != null)
                this.onSendError(this.id, ex);

            base.SendErrorOccured(ex);
        }

        /// <summary>
        /// 释放资源，阻塞
        /// </summary>
        public override void Close()
        {
            this.running = false;

            if (this.packageContainer != null)
                this.packageContainer.Dispose();

            base.Close();
        }

        /// <summary>
        /// binding 的唯一标识
        /// </summary>
        public string ID { get { return this.id; } }

    }
}