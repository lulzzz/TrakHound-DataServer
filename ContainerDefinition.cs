// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using MTConnectDevices = MTConnect.MTConnectDevices;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace TrakHound.DeviceServer
{
    public class ContainerDefinition
    {
        [JsonProperty("device_id")]
        public string DeviceId { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }


        [JsonProperty("container_type")]
        public ContainerType ContainerType { get; set; }

        [JsonProperty("parent_id")]
        public string ParentId { get; set; }

        public ContainerDefinition() { }

        public ContainerDefinition(string deviceId, MTConnectDevices.Device device)
        {
            DeviceId = deviceId;

            Id = device.Id;
            Name = device.Name;
            ContainerType = ContainerType.DEVICE;
        }

        public ContainerDefinition(string deviceId, MTConnectDevices.IComponent component, string parentId)
        {
            DeviceId = deviceId;

            Id = component.Id;
            Name = component.Name;
            Type = component.GetType().Name;
            ContainerType = ContainerType.COMPONENT;
            ParentId = parentId;
        }

    }
}
