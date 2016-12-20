// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.IO;
using System.ServiceProcess;

namespace TrakHound.Squirrel
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            Init(args);
        }

        private static void Init(string[] args)
        {
            if (args.Length > 0)
            {
                switch (args[0])
                {
                    case "debug": Debug(args); break;
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

        private static void Debug(string[] args)
        {
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configuration-Example.xml");

            var deviceServer = new Server(configPath);

            Console.ReadLine();
        }

    }
}
