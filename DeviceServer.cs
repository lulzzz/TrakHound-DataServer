// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System.Collections.Generic;

namespace TrakHound.Sniff
{
    public class DeviceServer
    {
        private Configuration configuration;

        public DeviceServer(string configPath)
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
            //foreach (var dataServer in configuration.DataServers)
            //{
            //    dataServer.SendDefinitions(definitions);
            //}
        }

        private void DataDefinitionsReceived(List<DataDefinition> definitions)
        {
            //foreach (var dataServer in configuration.DataServers)
            //{
            //    dataServer.SendDefinitions(definitions);
            //}
        }

        private void DataSamplesReceived(List<DataSample> samples)
        {
            //foreach (var sample in samples) buffer.Add(sample);

            foreach (var dataServer in configuration.DataServers)
            {
                dataServer.Add(samples);
            }
        }
    }
}
