// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using NLog;
using System;
using System.IO;
using System.Reflection;
using System.ServiceProcess;
using TrakHound.DataServer.Streaming;
using TrakHound.DataServer.Rest;

namespace TrakHound.DataServer
{
    static class Program
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                switch (args[0])
                {
                    case "debug": Run(args); break;
                }
            }
            else
            {
                // Start as Service
                ServiceBase.Run(new DataServerService());
            }
        }

        private static void Run(string[] args)
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
                    var streamingServer = new StreamingServer(config);
                    streamingServer.Start();

                    // Start the Rest Server (Download)
                    var restServer = new RestServer(config);
                    restServer.Start();
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

            Console.ReadLine();
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
