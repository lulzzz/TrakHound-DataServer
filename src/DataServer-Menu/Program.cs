// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE', which is part of this source code package.

using NLog;
using System;
using System.Security.Permissions;
using System.Threading;
using System.Windows.Forms;
using Messaging = TrakHound.Api.v2.Messaging;

namespace TrakHound.DataServer.Menu
{
    static class Program
    {
        private const int MESSAGE_SERVER_RETRY_INTERVAL = 2000;

        private static Logger log = LogManager.GetCurrentClassLogger();
        private static SystemTrayMenu menu;
        private static ManualResetEvent stop;
        private static Messaging.Server messageServer;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            StartMessageServer();
            StartMenu();
        }

        private static void StartMenu()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.AddMessageFilter(new ReadMessageFilter());

            menu = new SystemTrayMenu();
            Application.Run(menu);
        }

        private static void StartMessageServer()
        {
            stop = new ManualResetEvent(false);

            ThreadPool.QueueUserWorkItem(new WaitCallback((o) =>
            {
                bool started = false;
                do
                {
                    try
                    {
                        messageServer = new Messaging.Server("trakhound-dataserver-menu");
                        messageServer.MessageReceived += MessageServer_MessageReceived;
                        messageServer.Start();
                        started = true;
                    }
                    catch (Exception ex)
                    {
                        log.Trace(ex);
                    }
                } while (!started && !stop.WaitOne(MESSAGE_SERVER_RETRY_INTERVAL, true));
            }));
        }

        private static void MessageServer_MessageReceived(Messaging.Message message)
        {
            if (message != null && !string.IsNullOrEmpty(message.Id))
            {
                switch (message.Id.ToLower())
                {
                    case "notify":

                        var notifyIcon = SystemTrayMenu.NotifyIcon;
                        notifyIcon.BalloonTipTitle = "TrakHound DataServer";
                        notifyIcon.BalloonTipText = message.Text;
                        notifyIcon.Icon = Properties.Resources.dataserver;
                        notifyIcon.ShowBalloonTip(5000);
                        break;

                    case "status": SystemTrayMenu.SetHeader(message.Text); break;
                }
            }
        }

        public static void Exit()
        {
            if (stop != null) stop.Set();
            if (menu != null) menu.Exit();
            if (messageServer != null) messageServer.Stop();
            Application.Exit();
        }

        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        private class ReadMessageFilter : IMessageFilter
        {
            public bool PreFilterMessage(ref Message m)
            {
                if (m.Msg == /*WM_CLOSE*/ 0x10)
                {
                    Exit();
                    return true;
                }

                return false;
            }
        }
    }
}