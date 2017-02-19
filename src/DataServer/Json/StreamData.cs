// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using TrakHound.Api.v2.Streams;

namespace TrakHound.DataServer.Json
{
    public static class StreamData
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        public static IStreamData Read(string json)
        {
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    return JsonConvert.DeserializeObject<IStreamData>(json, new Api.v2.Json.StreamDataConverter());
                }
                catch (JsonException ex) { log.Trace(ex); }
                catch (Exception ex) { log.Trace(ex); }
            }

            return null;
        }

        public static IEnumerable<IStreamData> ReadList(string json)
        {
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    return JsonConvert.DeserializeObject<List<IStreamData>>(json, new Api.v2.Json.StreamDataConverter());
                }
                catch (JsonException ex) { log.Trace(ex); }
                catch (Exception ex) { log.Trace(ex); }
            }

            return Enumerable.Empty<IStreamData>();
        }
    }
}
