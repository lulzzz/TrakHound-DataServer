// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE', which is part of this source code package.

using NLog;
using System;
using System.Security.Permissions;
using System.Threading;
using System.Windows.Forms;
using WCF = TrakHound.Api.v2.WCF;

namespace TrakHound.DataServer.Menu
{
    static class Program
    {
        private const int MESSAGE_SERVER_RETRY_INTERVAL = 2000;

        private static Logger log = LogManager.GetCurrentClassLogger();
        private static SystemTrayMenu menu;
        private static ManualResetEvent stop;

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
                while (!started && !stop.WaitOne(MESSAGE_SERVER_RETRY_INTERVAL, true))
                {
                    try
                    {
                        var MessageServer = WCF.Server.Create<MessageServer>("trakhound-dataserver-menu");
                        started = true;
                    }
                    catch (Exception ex)
                    {
                        log.Trace(ex);
                    }
                }
            }));
        }

        public static void Exit()
        {
            if (stop != null) stop.Set();
            if (menu != null) menu.Exit();
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