// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using MTConnect;
using MTConnect.MTConnectStreams;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;

namespace TrakHound.DeviceServer
{
    public class DataSample
    {
        [JsonIgnore]
        public string Uuid { get; set; }

        [JsonProperty("device_id")]
        public string DeviceId { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("value_1")]
        public string Value1 { get; set; }

        [JsonProperty("value_2")]
        public string Value2 { get; set; }

        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonIgnore]
        public string Type { get; set; }

        public DataSample() { }

        public DataSample(string deviceId, DataItem dataItem)
        {
            Uuid = Guid.NewGuid().ToString();
            DeviceId = deviceId;

            Id = dataItem.DataItemId;
            Type = dataItem.Type;
            Timestamp = dataItem.Timestamp;

            if (dataItem.Category == DataItemCategory.CONDITION) Value2 = ((Condition)dataItem).ConditionValue.ToString();

            Value1 = dataItem.CDATA;
        }

        public string ToCsv()
        {
            string f = "{0},{1},{2},{3},{4}";
            return string.Format(f, DeviceId, Id, Value1, Value2, Timestamp.ToString("o"));
        }
    }
}
