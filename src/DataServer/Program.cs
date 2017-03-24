// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using NLog;
using System;
using System.Configuration.Install;
using System.IO;
using System.Reflection;
using System.ServiceProcess;
using System.Timers;
using TrakHound.Api.v2;
using TrakHound.DataServer.Streaming;

namespace TrakHound.DataServer
{
    static class Program
    {
        private const int MENU_UPDATE_INTERVAL = 2000;

        private static Logger logger = LogManager.GetCurrentClassLogger();
        private static StreamingServer server;
        private static ServiceBase service;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        private static void Main(string[] args)
        {
            Init(args);
        }

        private static void Init(string[] args)
        {
            if (args.Length > 0)
            {
                string runMode = args[0];

                switch (runMode)
                {
                    // Debug (Run as console application)
                    case "debug":

                        Start();
                        Console.ReadLine();
                        Stop();
                        Console.ReadLine();
                        break;

                    // Install the Service
                    case "install":

                        InstallService();
                        break;

                    // Uninstall the Service
                    case "uninstall":

                        UninstallService();
                        break;
                }
            }
            else
            {
                StartService();
            }
        }

        public static void StartService()
        {
            if (service == null) service = new DataServerService();
            ServiceBase.Run(service);
        }

        public static void StopService()
        {
            if (service != null) service.Stop();
        }

        public static void Start()
        {
            var currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += CurrentDomain_UnhandledException;

            try
            {
                PrintHeader();

                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Configuration.FILENAME);
                string defaultPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Configuration.DEFAULT_FILENAME);
                if (!File.Exists(configPath) && File.Exists(defaultPath))
                {
                    File.Copy(defaultPath, configPath);
                }
                var config = Configuration.Get(configPath);
                if (config != null)
                {
                    logger.Info("Configuration file read from '" + configPath + "'");
                    logger.Info("---------------------------");

                    // Initialize the Database Configuration
                    if (!Database.Initialize(config.DatabaseConfigurationPath))
                    {
                        // Throw exception that no configuration file was found
                        var ex = new Exception("No Database Configuration File Found. Exiting TrakHound-DataServer!");
                        logger.Error(ex);
                        throw ex;
                    }

                    // Start the Sreaming Server
                    server = new StreamingServer(config);
                    server.Start();
                }
                else
                {
                    // Throw exception that no configuration file was found
                    var ex = new Exception("No Configuration File Found. Exiting TrakHound-DataServer!");
                    logger.Fatal(ex);
                    throw ex;
                }
            }
            catch (Exception ex)
            {
                logger.Fatal(ex);
            }
        }

        public static void Stop()
        {
            if (server != null) server.Stop();
        }

        private static void InstallService()
        {
            ManagedInstallerClass.InstallHelper(new string[] { Assembly.GetExecutingAssembly().Location });
        }

        private static void UninstallService()
        {
            ManagedInstallerClass.InstallHelper(new string[] { "/u", Assembly.GetExecutingAssembly().Location });
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            logger.Fatal(e.ExceptionObject);
        }

        private static void PrintHeader()
        {
            logger.Info("---------------------------");
            logger.Info("TrakHound DataServer : v" + Assembly.GetExecutingAssembly().GetName().Version.ToString());
            logger.Info(@"Copyright 2017 TrakHound Inc., All Rights Reserved");
            logger.Info("---------------------------");
        }
    }
}
