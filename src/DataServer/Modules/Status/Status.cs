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
using System.Threading;
using TrakHound.Api.v2;
using TrakHound.Api.v2.Data;
using TrakHound.DataServer.Rest;

namespace TrakHound.DataServer.Modules.Status
{
    [InheritedExport(typeof(IModule))]
    public class Status : IModule
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        public string Name { get { return "Status"; } }

        
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
                        while (stream != null)
                        {
                            DeviceDefinition device;
                            List<ComponentDefinition> components;
                            List<DataItemDefinition> dataItems;
                            List<Sample> samples;

                            // MySQL
                            if (config.Database.GetType() == typeof(Sql.MySql.MySqlConfiguration))
                            {
                                // Get Device
                                device = GetDeviceFromMySql(query.DeviceId);

                                // Get Components
                                components = GetComponentsFromMySql(query.DeviceId);

                                // Get Data Items
                                dataItems = GetDataItemsFromMySql(query.DeviceId);

                                // Get Samples
                                samples = Sql.MySql.Samples.Get(query.DeviceId, query.From, query.To, query.At, query.Count);
                            }
                            else break;

                            if (!dataItems.IsNullOrEmpty() && !samples.IsNullOrEmpty())
                            {
                                // Get the initial timestamp
                                DateTime timestamp;
                                if (query.From > DateTime.MinValue) timestamp = query.From;
                                else if (query.At > DateTime.MinValue) timestamp = query.At;
                                else timestamp = samples.Select(o => o.Timestamp).OrderByDescending(o => o).First();

                                // Create a list of DataItemInfos (DataItems with Parent Component info)
                                var infos = DataItemInfo.CreateList(dataItems, components);

                                // Get Device Component
                                if (device != null)
                                {
                                    var deviceItem = new DeviceItem();

                                    // Add all of the root Device infos
                                    var deviceDataItems = infos.FindAll(o => o.Parents.Exists(x => x.Id == device.Id));
                                    foreach (var dataItem in deviceDataItems)
                                    {
                                        var sample = samples.Find(o => o.Id == dataItem.Id);
                                        if (sample != null) deviceItem.Data.Add(new StatusItem(sample, dataItem));
                                    }

                                    // Get Controller Component
                                    var controller = components.Find(o => o.Component == "Controller");
                                    if (controller != null)
                                    {
                                        var controllerItem = new ControllerItem();

                                        // Get Root DataItems
                                        var controllerDataItems = infos.FindAll(o => o.Parents.Exists(x => x.Id == controller.Id));
                                        foreach (var dataItem in controllerDataItems)
                                        {
                                            var sample = samples.Find(o => o.Id == dataItem.Id);
                                            if (sample != null) controllerItem.Data.Add(new StatusItem(sample, dataItem));
                                        }

                                        // Get Path Components
                                        var paths = components.FindAll(o => o.Component == "Path");
                                        foreach (var path in paths)
                                        {
                                            // Create new PathItem
                                            var pathItem = new PathItem();
                                            pathItem.Id = path.Id;

                                            // Find all DataItemInfo descendants of this Path
                                            var pathDataItems = infos.FindAll(o => o.Parents.Exists(x => x.Id == path.Id));
                                            foreach (var dataItem in pathDataItems)
                                            {
                                                var sample = samples.Find(o => o.Id == dataItem.Id);
                                                if (sample != null) pathItem.Data.Add(new StatusItem(sample, dataItem));
                                            }

                                            controllerItem.Paths.Add(pathItem);
                                        }

                                        deviceItem.Controller = controllerItem;
                                    }

                                    // Write DeviceItem JSON to stream
                                    string json = Requests.ToJson(deviceItem);
                                    var bytes = Encoding.UTF8.GetBytes(json);
                                    stream.Write(bytes, 0, bytes.Length);
                                }
                            }

                            if (query.Interval <= 0) break;
                            else Thread.Sleep(query.Interval);
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Info("Status Stream Closed");
                        log.Trace(ex);
                    }

                    return true;
                }
            }

            return false;
        }

        
        public static List<DataItemDefinition> GetDataItemsFromMySql(string deviceId)
        {
            var config = (Sql.MySql.MySqlConfiguration)Configuration.Current.Database;

            // Create connection string
            string cf = "server={0};uid={1};pwd={2};database={3};";
            string c = string.Format(cf, config.Server, config.User, config.Password, config.Database);

            string qf = "SELECT * FROM `data_items` WHERE `device_id` = '{0}'";
            string query = string.Format(qf, deviceId);

            if (!string.IsNullOrEmpty(query))
            {
                try
                {
                    using (var reader = MySqlHelper.ExecuteReader(c, query, null))
                    {
                        var l = new List<DataItemDefinition>();

                        while (reader.Read())
                        {
                            // Create new DataItemDefinition
                            var obj = new DataItemDefinition();
                            obj.DeviceId = reader.GetString("device_id");
                            obj.Id = reader.GetString("id");
                            obj.Type = reader.GetString("type");
                            obj.SubType = reader.GetString("sub_type");
                            obj.ParentId = reader.GetString("parent_id");
                            l.Add(obj);
                        }

                        return l;
                    }
                }
                catch (MySqlException ex)
                {
                    log.Error(ex);
                }
                catch (Exception ex)
                {
                    log.Error(ex);
                }
            }

            return null;
        }

        public static DeviceDefinition GetDeviceFromMySql(string deviceId)
        {
            var config = (Sql.MySql.MySqlConfiguration)Configuration.Current.Database;

            // Create connection string
            string cf = "server={0};uid={1};pwd={2};database={3};";
            string c = string.Format(cf, config.Server, config.User, config.Password, config.Database);

            string qf = "SELECT * FROM `devices` WHERE `device_id` = '{0}' LIMIT 1";
            string query = string.Format(qf, deviceId);

            if (!string.IsNullOrEmpty(query))
            {
                try
                {
                    using (var reader = MySqlHelper.ExecuteReader(c, query, null))
                    {
                        reader.Read();

                        var device = new DeviceDefinition();
                        device.DeviceId = reader.GetString("device_id");
                        device.Id = reader.GetString("id");
                        device.Name = reader.GetString("name");

                        return device;
                    }
                }
                catch (MySqlException ex)
                {
                    log.Error(ex);
                }
                catch (Exception ex)
                {
                    log.Error(ex);
                }
            }

            return null;
        }

        public static List<ComponentDefinition> GetComponentsFromMySql(string deviceId)
        {
            var config = (Sql.MySql.MySqlConfiguration)Configuration.Current.Database;

            // Create connection string
            string cf = "server={0};uid={1};pwd={2};database={3};";
            string c = string.Format(cf, config.Server, config.User, config.Password, config.Database);

            string qf = "SELECT * FROM `components` WHERE `device_id` = '{0}'";
            string query = string.Format(qf, deviceId);

            if (!string.IsNullOrEmpty(query))
            {
                try
                {
                    using (var reader = MySqlHelper.ExecuteReader(c, query, null))
                    {
                        var l = new List<ComponentDefinition>();

                        while (reader.Read())
                        {
                            // Create new ComponentDefinition
                            var obj = new ComponentDefinition();
                            obj.DeviceId = reader.GetString("device_id");
                            obj.Id = reader.GetString("id");
                            obj.Name = reader.GetString("name");
                            obj.Component = reader.GetString("component");
                            obj.ParentId = reader.GetString("parent_id");
                            l.Add(obj);
                        }

                        return l;
                    }
                }
                catch (MySqlException ex)
                {
                    log.Error(ex);
                }
                catch (Exception ex)
                {
                    log.Error(ex);
                }
            }

            return null;
        }

    }
}
