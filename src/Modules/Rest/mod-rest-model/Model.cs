// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Text;
using TrakHound.Api.v2;
using TrakHound.Api.v2.Data;
using Json = TrakHound.Api.v2.Json;

namespace mod_rest_model
{
    [InheritedExport(typeof(IRestModule))]
    public class Model : IRestModule
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        public string Name { get { return "Model"; } }


        public bool GetResponse(Uri requestUri, Stream stream)
        {
            var query = new RequestQuery(requestUri);
            if (query.IsValid)
            {
                try
                {
                    DeviceDefinition device = null;
                    List<ComponentDefinition> components = null;
                    List<DataItemDefinition> dataItems = null;

                    // Get Current Agent
                    var agent = Database.ReadAgent(query.DeviceId);
                    if (agent != null)
                    {
                        // Get Device
                        device = Database.ReadDevice(query.DeviceId, agent.InstanceId);

                        // Get Components
                        components = Database.ReadComponents(query.DeviceId, agent.InstanceId);

                        // Get Data Items
                        dataItems = Database.ReadDataItems(query.DeviceId, agent.InstanceId);
                    }

                    if (device != null && !components.IsNullOrEmpty() && !dataItems.IsNullOrEmpty())
                    {
                        // Use a converter to actually convert Defintions to DataItem objects.
                        // This is because we don't want to output the Defintion properties to JSON (as they are redundant)
                        var converter = new Converter<DataItemDefinition, DataItem>(ConvertDataItem);

                        // Create new Item
                        var deviceItem = new DeviceItem(device);
                        deviceItem.Add(dataItems.FindAll(o => o.ParentId == deviceItem.Id).ConvertAll(converter));

                        var componentItems = new List<ComponentItem>();

                        foreach (var component in components)
                        {
                            var item = new ComponentItem(component);
                            item.Add(dataItems.FindAll(o => o.ParentId == component.Id).ConvertAll(converter));

                            // Add any child components
                            foreach (var child in componentItems.FindAll(o => o.ParentId == component.Id))
                            {
                                componentItems.Remove(child);
                                item.Add(child);
                            }

                            // Add to parent component
                            var parent = componentItems.Find(o => o.Id == component.ParentId);
                            if (parent != null) parent.Add(item);
                            else componentItems.Add(item);
                        }

                        deviceItem.Add(componentItems);

                        // Write DeviceItem JSON to stream
                        string json = Json.Convert.ToJson(deviceItem, true);
                        var bytes = Encoding.UTF8.GetBytes(json);
                        stream.Write(bytes, 0, bytes.Length);
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    log.Trace(ex);
                }
            }

            return false;
        }

        private static DataItem ConvertDataItem(DataItemDefinition definition)
        {
            var dataItem = new DataItem();
            foreach (var property in typeof(DataItem).GetProperties())
            {
                var value = property.GetValue(definition, null);
                property.SetValue(dataItem, value, null);
            }

            return dataItem;
        }
    }
}
