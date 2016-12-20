// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System.Collections.Generic;

namespace TrakHound.Squirrel
{
    public class Server
    {
        private Configuration configuration;

        public Server(string configPath)
        {
            configuration = Configuration.Get(configPath);
            if (configuration != null)
            {
                // Start Devices
                foreach (var device in configuration.Devices)
                {
                    device.ContainerDefinitionsReceived += ContainerDefinitionsReceived;
                    device.DataDefinitionsReceived += DataDefinitionsReceived;
                    device.DataSamplesReceived += DataSamplesReceived;
                    device.Start();
                }

                // Start Data Servers
                foreach (var dataServer in configuration.DataServers)
                {
                    dataServer.Start();
                }
            }
        }

        private void ContainerDefinitionsReceived(List<ContainerDefinition> definitions)
        {
        }

        private void DataDefinitionsReceived(List<DataDefinition> definitions)
        {
        }

        private void DataSamplesReceived(List<DataSample> samples)
        {
            foreach (var dataServer in configuration.DataServers)
            {
                dataServer.Add(samples);
            }
        }
    }
}
