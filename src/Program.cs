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
        private static Logger log = LogManager.GetCurrentClassLogger();

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
                ServiceBase.Run(new ServiceBase[]
                {
                    new Service()
                });
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
                    log.Info("Configuration file read from '" + configPath + "'");
                    log.Info("---------------------------");

                    var streamingServer = new StreamingServer(config);
                    streamingServer.Start();

                    var restServer = new RestServer(config);
                    restServer.Start();
                }
                else
                {
                    // Throw exception that no configuration file was found
                    var ex = new Exception("No Configuration File Found. Exiting TrakHound-DataServer!");
                    log.Error(ex);
                    throw ex;
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }

            Console.ReadLine();
        }

        private static void PrintHeader()
        {
            log.Info("---------------------------");
            log.Info("TrakHound DataServer : v" + Assembly.GetExecutingAssembly().GetName().Version.ToString());
            log.Info(@"Copyright 2017 TrakHound Inc., All Rights Reserved");
            log.Info("---------------------------");
        }
    }
}
