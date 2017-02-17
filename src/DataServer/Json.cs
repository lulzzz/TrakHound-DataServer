// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using TrakHound.Api.v2.Streams;
using TrakHound.Api.v2.Streams.Data;

namespace TrakHound.DataServer
{
    static class Json
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        public static IStreamData ReadStreamData(string json)
        {
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    return JsonConvert.DeserializeObject<IStreamData>(json, new JsonStreamDataConverter());
                }
                catch (JsonException ex) { log.Trace(ex); }
                catch (Exception ex) { log.Trace(ex); }
            }

            return null;
        }

        public static IEnumerable<IStreamData> ReadStreamDataList(string json)
        {
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    return JsonConvert.DeserializeObject<List<IStreamData>>(json, new JsonStreamDataConverter());
                }
                catch (JsonException ex) { log.Trace(ex); }
                catch (Exception ex) { log.Trace(ex); }
            }

            return Enumerable.Empty<IStreamData>();
        }
    }


    public class JsonStreamDataConverter : CustomCreationConverter<IStreamData>
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        public override IStreamData Create(Type objectType)
        {
            throw new NotImplementedException();
        }

        public IStreamData Create(Type objectType, JObject jObject)
        {
            var type = (string)jObject.Property("stream_data_type");
            switch (type)
            {
                case "1": return new ConnectionDefinitionData();
                case "2": return new AgentDefinitionData();
                case "3": return new DeviceDefinitionData();
                case "4": return new ComponentDefinitionData();
                case "5": return new DataItemDefinitionData();
                case "6": return new SampleData(StreamDataType.ARCHIVED_SAMPLE);
                case "7": return new SampleData(StreamDataType.CURRENT_SAMPLE);
            }

            throw new Exception("Stream Data Type not supported");
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            string value = "";

            try
            {
                var val = reader.Value;
                if (val != null) value = val.ToString();

                // Load JObject from stream
                JObject jObject = JObject.Load(reader);

                // Create target object based on JObject
                var target = Create(objectType, jObject);

                if (target != null)
                {
                    // Populate the object properties
                    serializer.Populate(jObject.CreateReader(), target);

                    return target;
                }
            }
            catch (Exception ex)
            {
                log.Trace(ex);
            }

            return null;
        }
    }
}
