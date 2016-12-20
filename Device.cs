// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using MTConnect.Clients;
using NLog;
using System.Collections.Generic;
using System.Xml.Serialization;
using MTConnectDevices = MTConnect.MTConnectDevices;
using MTConnectStreams = MTConnect.MTConnectStreams;

namespace TrakHound.Squirrel
{
    public class Device
    {
        [XmlAttribute("deviceId")]
        public string DeviceId { get; set; }

        [XmlText]
        public string AgentUrl { get; set; }

        [XmlAttribute("deviceName")]
        public string DeviceName { get; set; }

        public event ContainerDefinitionsHandler ContainerDefinitionsReceived;
        public event DataDefinitionsHandler DataDefinitionsReceived;
        public event DataSamplesHandler DataSamplesReceived;

        private static Logger logger = LogManager.GetCurrentClassLogger();

        private MTConnectClient agentClient;

        public void Start()
        {
            StartAgentClient();
        }

        public void Stop()
        {
            if (agentClient != null) agentClient.Stop();
        }

        private void StartAgentClient()
        {
            // Create a new MTConnectClient using the baseUrl
            agentClient = new MTConnectClient(AgentUrl, DeviceName);

            // Subscribe to the Event handlers to receive the MTConnect documents
            agentClient.ProbeReceived += DevicesSuccessful;
            agentClient.CurrentReceived += StreamsSuccessful;
            agentClient.SampleReceived += StreamsSuccessful;

            // Start the MTConnectClient
            agentClient.Start();
        }

        void DevicesSuccessful(MTConnectDevices.Document document)
        {
            if (document.Devices != null && document.Devices.Count > 0)
            {
                var dataDefinitions = new List<DataDefinition>();
                var containerDefinitions = new List<ContainerDefinition>();

                var device = document.Devices[0];

                // Add Device Container
                containerDefinitions.Add(new ContainerDefinition(DeviceId, device));

                // Add Path DataItems
                foreach (var item in device.DataItems)
                {
                    dataDefinitions.Add(new DataDefinition(DeviceId, item, device.Id));
                }

                // Create a ContainerDefinition for each Component
                foreach (var component in device.Components)
                {
                    // Add Component Container
                    containerDefinitions.Add(new ContainerDefinition(DeviceId, component, null));

                    // Add Path DataItems
                    foreach (var item in component.DataItems)
                    {
                        dataDefinitions.Add(new DataDefinition(DeviceId, item, component.Id));
                    }

                    // Process Axes Component
                    if (component.GetType() == typeof(MTConnectDevices.Components.Axes))
                    {
                        var axes = (MTConnectDevices.Components.Axes)component;
                        foreach (var axis in axes.Components)
                        {
                            // Add Axis Component
                            containerDefinitions.Add(new ContainerDefinition(DeviceId, axis, component.Id));

                            // Add Path DataItems
                            foreach (var item in axis.DataItems)
                            {
                                dataDefinitions.Add(new DataDefinition(DeviceId, item, axis.Id));
                            }
                        }
                    }

                    // Process Controller Component
                    if (component.GetType() == typeof(MTConnectDevices.Components.Controller))
                    {
                        var controller = (MTConnectDevices.Components.Controller)component;
                        foreach (var path in controller.Components)
                        {
                            // Add Path Component
                            containerDefinitions.Add(new ContainerDefinition(DeviceId, path, component.Id));

                            // Add Path DataItems
                            foreach (var item in path.DataItems)
                            {
                                dataDefinitions.Add(new DataDefinition(DeviceId, item, path.Id));
                            }
                        }
                    }
                }

                // Send ContainerDefinition Objects
                if (containerDefinitions.Count > 0) ContainerDefinitionsReceived?.Invoke(containerDefinitions);

                // Send DataDefinition Objects
                if (dataDefinitions.Count > 0) DataDefinitionsReceived?.Invoke(dataDefinitions);
            }
        }

        void StreamsSuccessful(MTConnectStreams.Document document)
        {
            if (document.DeviceStreams != null && document.DeviceStreams.Count > 0)
            {
                var dataSamples = new List<DataSample>();

                var deviceStream = document.DeviceStreams[0];

                foreach (var dataItem in deviceStream.DataItems)
                {
                    dataSamples.Add(new DataSample(DeviceId, dataItem));
                }

                if (dataSamples.Count > 0) DataSamplesReceived?.Invoke(dataSamples);
            }
        }
    }
}
