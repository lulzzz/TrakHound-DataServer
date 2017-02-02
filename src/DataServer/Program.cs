// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using NLog;
using System;
using System.Configuration.Install;
using System.IO;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;
using TrakHound.DataServer.Streaming;
using TrakHound.DataServer.Rest;

namespace TrakHound.DataServer
{
    static class Program
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private static ManualResetEvent stop;
        private static StreamingServer streamingServer;
        private static RestServer restServer;

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

                        if (args.Length > 1)
                        {
                            string serverMode = args[1];

                            switch (serverMode)
                            {
                                case "streaming": Start(true, false); break;
                                case "rest": Start(false, true); break;
                                default: Start(); break;
                            }
                        }
                        else Start();

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
                // Start as Service
                ServiceBase.Run(new DataServerService());
            }
        }

        public static void Start(bool startStreaming = true, bool startRest = true)
        {
            try
            {
                PrintHeader();

                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Configuration.FILENAME);
                var config = Configuration.Get(configPath);
                if (config != null)
                {
                    logger.Info("Configuration file read from '" + configPath + "'");
                    logger.Info("---------------------------");

                    // Initialize the Database Configuration
                    if (!Database.Initialize(config))
                    {
                        // Throw exception that no configuration file was found
                        var ex = new Exception("No Database Configuration File Found. Exiting TrakHound-DataServer!");
                        logger.Error(ex);
                        throw ex;
                    }

                    // Start the Sreaming Server (Upload)
                    if (startStreaming)
                    {
                        streamingServer = new StreamingServer(config);
                        streamingServer.Start();
                    }

                    // Start the Rest Server (Download)
                    if (startRest)
                    {
                        restServer = new RestServer(config);
                        restServer.Start();
                    }
                }
                else
                {
                    // Throw exception that no configuration file was found
                    var ex = new Exception("No Configuration File Found. Exiting TrakHound-DataServer!");
                    logger.Error(ex);
                    throw ex;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
        }

        public static void Stop()
        {
            if (stop != null) stop.Set();
        }

        private static void InstallService()
        {
            ManagedInstallerClass.InstallHelper(new string[] { Assembly.GetExecutingAssembly().Location });
        }

        private static void UninstallService()
        {
            ManagedInstallerClass.InstallHelper(new string[] { "/u", Assembly.GetExecutingAssembly().Location });
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
