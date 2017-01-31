// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using Newtonsoft.Json;
using System.Collections.Generic;
using TrakHound.Api.v2;

namespace TrakHound.DataServer.Modules.Status
{
    class DeviceItem
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("data")]
        public List<StatusItem> Data { get; set; }

        [JsonProperty("controller")]
        public ControllerItem Controller { get; set; }

        public DeviceItem()
        {
            Data = new List<StatusItem>();
        }

    }
}
