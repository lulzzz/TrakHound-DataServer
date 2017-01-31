// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using Newtonsoft.Json;
using System;
using TrakHound.Api.v2.Data;

namespace TrakHound.DataServer.Modules.Status
{
    class StatusItem
    {
        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }


        public StatusItem(Sample sample, DataItemDefinition dataItem)
        {
            Timestamp = sample.Timestamp;
            Id = sample.Id;
            Type = dataItem.Type;
            Value = sample.CDATA;
            Message = sample.Condition;
        }
    }
}
