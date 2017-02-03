// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using Newtonsoft.Json;
using System;
using TrakHound.Api.v2;
using TrakHound.Api.v2.Data;

namespace mod_rest_samples
{
    public class Item
    {
        [JsonProperty("timestamp")]
        [JsonConverter(typeof(UnixTimeJsonConverter))]
        public DateTime Timestamp { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("sequence")]
        public long Sequence { get; set; }

        [JsonProperty("cdata")]
        public string CDATA { get; set; }

        [JsonProperty("condition")]
        public string Condition { get; set; }

        public Item(Sample sample)
        {
            Timestamp = sample.Timestamp;
            Id = sample.Id;
            Sequence = sample.Sequence;
            CDATA = sample.CDATA;
            Condition = sample.Condition;
        }
    }
}
