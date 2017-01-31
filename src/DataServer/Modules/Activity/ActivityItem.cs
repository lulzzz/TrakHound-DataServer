// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System.Collections.Generic;
using Newtonsoft.Json;
using TrakHound.Api.v2;

namespace TrakHound.DataServer.Modules.Activity
{
    class ActivityItem
    {
        [JsonProperty("events")]
        public List<EventItem> Events { get; set; }

        [JsonProperty("paths")]
        public List<PathItem> Paths { get; set; }


        public void Add(EventItem component)
        {
            if (Events == null) Events = new List<EventItem>();
            Events.Add(component);
        }

        public void Add(List<EventItem> events)
        {
            if (!events.IsNullOrEmpty())
            {
                if (Events == null) Events = new List<EventItem>();
                Events.AddRange(events);
            }
        }

        public void Add(PathItem path)
        {
            if (Paths == null) Paths = new List<PathItem>();
            Paths.Add(path);
        }

        public void Add(List<PathItem> paths)
        {
            if (!paths.IsNullOrEmpty())
            {
                if (Paths == null) Paths = new List<PathItem>();
                Paths.AddRange(paths);
            }
        }
    }
}
