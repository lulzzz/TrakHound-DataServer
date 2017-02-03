// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using Newtonsoft.Json;
using System.Collections.Generic;
using TrakHound.Api.v2;
using TrakHound.Api.v2.Data;

namespace mod_rest_model
{
    class ComponentItem : Component
    {
        [JsonProperty("components", Order = 5)]
        public List<ComponentItem> Components { get; set; }

        [JsonProperty("data_items", Order = 4)]
        public List<DataItem> DataItems { get; set; }

        [JsonIgnore]
        public string ParentId { get; set; }


        public ComponentItem(ComponentDefinition component)
        {
            Type = component.Type;
            Id = component.Id;
            Uuid = component.Uuid;
            Name = component.Name;
            NativeName = component.NativeName;
            SampleInterval = component.SampleInterval;
            SampleRate = component.SampleRate;

            ParentId = component.ParentId;
        }

        public void Add(ComponentItem component)
        {
            if (Components == null) Components = new List<ComponentItem>();
            Components.Add(component);
        }

        public void Add(List<ComponentItem> components)
        {
            if (!components.IsNullOrEmpty())
            {
                if (Components == null) Components = new List<ComponentItem>();
                Components.AddRange(components);
            }
        }

        public void Add(DataItem dataItem)
        {
            if (DataItems == null) DataItems = new List<DataItem>();
            DataItems.Add(dataItem);
        }

        public void Add(List<DataItem> dataItems)
        {
            if (!dataItems.IsNullOrEmpty())
            {
                if (DataItems == null) DataItems = new List<DataItem>();
                DataItems.AddRange(dataItems);
            }
        }
    }
}
