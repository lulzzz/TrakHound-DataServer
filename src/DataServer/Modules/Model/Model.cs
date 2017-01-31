// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using MySql.Data.MySqlClient;
using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using TrakHound.Api.v2;
using TrakHound.Api.v2.Data;
using TrakHound.DataServer.Rest;

namespace TrakHound.DataServer.Modules.Model
{
    [InheritedExport(typeof(IModule))]
    public class Model : IModule
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        public string Name { get { return "Model"; } }

        
        public bool GetResponse(Uri requestUri, Stream stream)
        {
            var query = new RequestQuery(requestUri);
            if (query.IsValid)
            {
                var config = Configuration.Current;
                if (config != null)
                {
                    try
                    {
                        DeviceDefinition device = null;
                        List<ComponentDefinition> components = null;
                        List<DataItemDefinition> dataItems = null;

                        // MySQL
                        if (config.Database.GetType() == typeof(Sql.MySql.MySqlConfiguration))
                        {
                            // Get Current Agent
                            var agent = Sql.MySql.Agent.Get(query.DeviceId);
                            if (agent != null)
                            {
                                // Get Device
                                device = Sql.MySql.Device.Get(query.DeviceId, agent.InstanceId);

                                // Get Components
                                components = Sql.MySql.Components.Get(query.DeviceId, agent.InstanceId);

                                // Get Data Items
                                dataItems = Sql.MySql.DataItems.Get(query.DeviceId, agent.InstanceId);
                            }
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


                            //// Add Axes Components
                            //var component = components.Find(o => o.Type == "Axes");
                            //if (component != null)
                            //{
                            //    var axes = new ComponentItem(component);
                            //    axes.DataItems.AddRange(dataItems.FindAll(o => o.ParentId == component.Id).ConvertAll(converter));

                            //    // Linear
                            //    foreach (var c in components.FindAll(o => o.Type == "Linear"))
                            //    {
                            //        var linear = new ComponentItem(c);
                            //        linear.DataItems.AddRange(dataItems.FindAll(o => o.ParentId == c.Id).ConvertAll(converter));
                            //        axes.Components.Add(linear);                                   
                            //    }

                            //    // Rotary
                            //    foreach (var c in components.FindAll(o => o.Type == "Rotary"))
                            //    {
                            //        var rotary = new ComponentItem(c);
                            //        rotary.DataItems.AddRange(dataItems.FindAll(o => o.ParentId == c.Id).ConvertAll(converter));
                            //        axes.Components.Add(rotary);
                            //    }

                            //    deviceItem.Components.Add(axes);
                            //}

                            //// Add Controller and Path Components
                            //component = components.Find(o => o.Type == "Controller");
                            //if (component != null)
                            //{
                            //    var controller = new ComponentItem(component);
                            //    controller.DataItems.AddRange(dataItems.FindAll(o => o.ParentId == component.Id).ConvertAll(converter));

                            //    // Paths
                            //    foreach (var c in components.FindAll(o => o.Type == "Path"))
                            //    {
                            //        var path = new ComponentItem(c);
                            //        path.DataItems.AddRange(dataItems.FindAll(o => o.ParentId == c.Id).ConvertAll(converter));
                            //        controller.Components.Add(path);
                            //    }

                            //    deviceItem.Components.Add(controller);
                            //}

                            //// Add Systems Components
                            //var otherComponents = components.FindAll(o => o.Type != "Axes" &&
                            //o.Type != "Controller" &&
                            //o.Type != "Linear" &&
                            //o.Type != "Path" &&
                            //o.Type != "Rotary"
                            //);
                            //foreach (var c in otherComponents)
                            //{
                            //    var other = new ComponentItem(c);
                            //    other.DataItems.AddRange(dataItems.FindAll(o => o.ParentId == c.Id).ConvertAll(converter));
                            //    deviceItem.Components.Add(other);
                            //}

                            // Write DeviceItem JSON to stream
                            string json = Requests.ToJson(deviceItem);
                            var bytes = Encoding.UTF8.GetBytes(json);
                            stream.Write(bytes, 0, bytes.Length);
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Trace(ex);
                    }

                    return true;
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


        
        //public static List<DataItemDefinition> GetDataItemsFromMySql(string deviceId)
        //{
        //    var config = (Sql.MySql.MySqlConfiguration)Configuration.Current.Database;

        //    // Create connection string
        //    string cf = "server={0};uid={1};pwd={2};database={3};";
        //    string c = string.Format(cf, config.Server, config.User, config.Password, config.Database);

        //    string qf = "SELECT * FROM `data_items` WHERE `device_id` = '{0}'";
        //    string query = string.Format(qf, deviceId);

        //    if (!string.IsNullOrEmpty(query))
        //    {
        //        try
        //        {
        //            using (var reader = MySqlHelper.ExecuteReader(c, query, null))
        //            {
        //                var l = new List<DataItemDefinition>();

        //                while (reader.Read())
        //                {
        //                    // Create new DataItemDefinition
        //                    var obj = new DataItemDefinition();
        //                    obj.DeviceId = reader.GetString("device_id");
        //                    obj.Id = reader.GetString("id");
        //                    obj.Type = reader.GetString("type");
        //                    obj.SubType = reader.GetString("sub_type");
        //                    obj.ParentId = reader.GetString("parent_id");
        //                    l.Add(obj);
        //                }

        //                return l;
        //            }
        //        }
        //        catch (MySqlException ex)
        //        {
        //            log.Error(ex);
        //        }
        //        catch (Exception ex)
        //        {
        //            log.Error(ex);
        //        }
        //    }

        //    return null;
        //}

        //public static DeviceDefinition GetDeviceFromMySql(string deviceId)
        //{
        //    var config = (Sql.MySql.MySqlConfiguration)Configuration.Current.Database;

        //    // Create connection string
        //    string cf = "server={0};uid={1};pwd={2};database={3};";
        //    string c = string.Format(cf, config.Server, config.User, config.Password, config.Database);

        //    string qf = "SELECT * FROM `devices` WHERE `device_id` = '{0}' LIMIT 1";
        //    string query = string.Format(qf, deviceId);

        //    if (!string.IsNullOrEmpty(query))
        //    {
        //        try
        //        {
        //            using (var reader = MySqlHelper.ExecuteReader(c, query, null))
        //            {
        //                reader.Read();

        //                var device = new DeviceDefinition();
        //                device.DeviceId = reader.GetString("device_id");
        //                device.Id = reader.GetString("id");
        //                device.Name = reader.GetString("name");

        //                return device;
        //            }
        //        }
        //        catch (MySqlException ex)
        //        {
        //            log.Error(ex);
        //        }
        //        catch (Exception ex)
        //        {
        //            log.Error(ex);
        //        }
        //    }

        //    return null;
        //}

        //public static List<ComponentDefinition> GetComponentsFromMySql(string deviceId)
        //{
        //    var config = (Sql.MySql.MySqlConfiguration)Configuration.Current.Database;

        //    // Create connection string
        //    string cf = "server={0};uid={1};pwd={2};database={3};";
        //    string c = string.Format(cf, config.Server, config.User, config.Password, config.Database);

        //    string qf = "SELECT * FROM `components` WHERE `device_id` = '{0}'";
        //    string query = string.Format(qf, deviceId);

        //    if (!string.IsNullOrEmpty(query))
        //    {
        //        try
        //        {
        //            using (var reader = MySqlHelper.ExecuteReader(c, query, null))
        //            {
        //                var l = new List<ComponentDefinition>();

        //                while (reader.Read())
        //                {
        //                    // Create new ComponentDefinition
        //                    var obj = new ComponentDefinition();
        //                    obj.DeviceId = reader.GetString("device_id");
        //                    obj.Id = reader.GetString("id");
        //                    obj.Name = reader.GetString("name");
        //                    obj.Component = reader.GetString("component");
        //                    obj.ParentId = reader.GetString("parent_id");
        //                    l.Add(obj);
        //                }

        //                return l;
        //            }
        //        }
        //        catch (MySqlException ex)
        //        {
        //            log.Error(ex);
        //        }
        //        catch (Exception ex)
        //        {
        //            log.Error(ex);
        //        }
        //    }

        //    return null;
        //}

    }
}
