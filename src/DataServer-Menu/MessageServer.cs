// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE', which is part of this source code package.

using NLog;
using System;
using System.ServiceModel;
using TrakHound.Api.v2.WCF;

namespace TrakHound.DataServer.Menu
{
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class MessageServer : IMessage
    {
        private static Logger log = LogManager.GetCurrentClassLogger();
        private static IMessageCallback callback;

        public MessageServer()
        {
            try
            {
                callback = OperationContext.Current.GetCallbackChannel<IMessageCallback>();
            }
            catch (Exception ex)
            {
                log.Error("Error during MessageServer Start");
                log.Trace(ex);
            }
        }

        public object SendData(Message data)
        {
            if (data != null && data.Id != null)
            {
                switch (data.Id.ToLower())
                {
                    case "notify":

                        var notifyIcon = SystemTrayMenu.NotifyIcon;
                        notifyIcon.BalloonTipTitle = "TrakHound DataServer";
                        notifyIcon.BalloonTipText = data.Text;
                        notifyIcon.Icon = Properties.Resources.dataserver;
                        notifyIcon.ShowBalloonTip(5000);
                        break;

                    case "status": SystemTrayMenu.SetHeader(data.Text); break;
                }
            }

            return "Data Sent Successfully!";
        }

        public static void SendCallback(Message data)
        {
            try
            {
                callback.OnCallback(data);
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }
    }
}
