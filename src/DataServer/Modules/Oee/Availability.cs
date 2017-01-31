// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using TrakHound.Api.v2;
using TrakHound.Api.v2.Data;
using TrakHound.Api.v2.Events;
using TrakHound.DataServer.Rest;

namespace TrakHound.DataServer.Modules.Oee
{
    class Availability
    {
        private const string EVENT_NAME = "Status";
        private const string EVENT_VALUE = "Active";

        private static Logger log = LogManager.GetCurrentClassLogger();

        public static string Get(RequestQuery query, Stream stream)
        {
            var config = Configuration.Current;
            if (config != null)
            {
                var e = GetEvent(EVENT_NAME);
                if (e != null)
                {
                    List<ComponentDefinition> components = null;
                    List<DataItemDefinition> dataItems = null;
                    List<Sample> samples = null;
                    List<DataItemInfo> dataItemInfos = null;

                    // MySQL
                    if (config.Database.GetType() == typeof(Sql.MySql.MySqlConfiguration))
                    {
                        // Get Current Agent
                        var agent = Sql.MySql.Agent.Get(query.DeviceId);
                        if (agent != null)
                        {
                            // Get Components
                            components = Sql.MySql.Components.Get(query.DeviceId, agent.InstanceId);

                            // Get Data Items
                            dataItems = Sql.MySql.DataItems.Get(query.DeviceId, agent.InstanceId);

                            // Create a list of DataItemInfos (DataItems with Parent Component info)
                            dataItemInfos = DataItemInfo.CreateList(dataItems, components);
                        }
                    }

                    if (!dataItems.IsNullOrEmpty() && !components.IsNullOrEmpty())
                    {
                        var dataItemIds = GetEventIds(e, dataItems, components);

                        // Get Samples
                        samples = Sql.MySql.Samples.Get(dataItemIds, query.DeviceId, query.From, query.To, DateTime.MinValue, 0);

                        //foreach (var sample in samples)
                        //{
                        //    Console.WriteLine(sample.Id);
                        //}

                        Console.WriteLine("Samples : " + samples.Count);

                        // Get Samples for Time Range
                        var infos = SampleInfo.Create(dataItemInfos, samples);

                        GetAvailableTime(query, infos);


                    }
                }
            }


            //    try
            //    {
            //        while (stream != null)
            //        {
            //            List<ComponentDefinition> components = null;
            //            List<DataItemDefinition> dataItems = null;
            //            List<Sample> samples = null;
            //            List<DataItemInfo> dataItemInfos = null;

            //            // MySQL
            //            if (config.Database.GetType() == typeof(Sql.MySql.MySqlConfiguration))
            //            {
            //                // Get Current Agent
            //                var agent = Sql.MySql.Agent.Get(query.DeviceId);
            //                if (agent != null)
            //                {
            //                    // Get Components
            //                    components = Sql.MySql.Components.Get(query.DeviceId, agent.InstanceId);

            //                    // Get Data Items
            //                    dataItems = Sql.MySql.DataItems.Get(query.DeviceId, agent.InstanceId);

            //                    // Get Samples
            //                    samples = Sql.MySql.Samples.Get(query.DeviceId, query.From, query.To, DateTime.MinValue, 0);

            //                    // Create a list of DataItemInfos (DataItems with Parent Component info)
            //                    dataItemInfos = DataItemInfo.CreateList(dataItems, components);
            //                }
            //            }
            //            else break;

            //            if (!dataItemInfos.IsNullOrEmpty() && !samples.IsNullOrEmpty())
            //            {
            //                string json = null;

            //                // Get the requested Query/SubQuery
            //                switch (query.SubQuery)
            //                {
            //                    case "availability": json = Availability.Get(query, dataItemInfos, samples); break;
            //                    case "performance": Console.WriteLine("Performance"); break;
            //                    case "quality": Console.WriteLine("Quality"); break;
            //                    default: Console.WriteLine("OEE"); break;
            //                }

            //                if (!string.IsNullOrEmpty(json))
            //                {
            //                    // Write JSON to stream
            //                    var bytes = Encoding.UTF8.GetBytes(json);
            //                    stream.Write(bytes, 0, bytes.Length);
            //                }



            //            }










            //            //var activityItem = new ActivityItem();
            //            //var pathItems = new List<PathItem>();

            //            //List<ComponentDefinition> components = null;
            //            //List<DataItemDefinition> dataItems = null;
            //            //List<Sample> samples = null;

            //            //// MySQL
            //            //if (config.Database.GetType() == typeof(Sql.MySql.MySqlConfiguration))
            //            //{
            //            //    // Get Current Agent
            //            //    var agent = Sql.MySql.Agent.Get(query.DeviceId);
            //            //    if (agent != null)
            //            //    {
            //            //        // Get Components
            //            //        components = Sql.MySql.Components.Get(query.DeviceId, agent.InstanceId);

            //            //        // Get Data Items
            //            //        dataItems = Sql.MySql.DataItems.Get(query.DeviceId, agent.InstanceId);

            //            //        // Get Samples
            //            //        samples = Sql.MySql.Samples.Get(query.DeviceId, query.From, query.To, query.At, query.Count);
            //            //    }
            //            //}
            //            //else break;

            //            //if (!dataItems.IsNullOrEmpty() && !samples.IsNullOrEmpty())
            //            //{
            //            //    var events = GetEvents();
            //            //    if (events != null)
            //            //    {
            //            //        // Get the initial timestamp
            //            //        DateTime timestamp;
            //            //        if (query.From > DateTime.MinValue) timestamp = query.From;
            //            //        else if (query.At > DateTime.MinValue) timestamp = query.At;
            //            //        else timestamp = samples.Select(o => o.Timestamp).OrderByDescending(o => o).First();

            //            //        // Create a list of DataItemInfos (DataItems with Parent Component info)
            //            //        var dataItemInfos = DataItemInfo.CreateList(dataItems, components);

            //            //        // Get Path Components
            //            //        var paths = components.FindAll(o => o.Type == "Path");

            //            //        foreach (var e in events)
            //            //        {
            //            //            // Check if Event relies on Path and there are multiple paths
            //            //            if (ContainsPath(e, components, dataItems) && paths.Count > 1)
            //            //            {
            //            //                foreach (var path in paths)
            //            //                {
            //            //                    // Find all DataItemInfo descendants of this Path
            //            //                    var pathInfos = dataItemInfos.FindAll(o => !o.Parents.Exists(x => x.Type == "Path") || o.ParentId == path.Id);

            //            //                    var pathItem = pathItems.Find(o => o.Id == path.Id);
            //            //                    if (pathItem == null)
            //            //                    {
            //            //                        // Create new PathItem
            //            //                        pathItem = new PathItem();
            //            //                        pathItem.Id = path.Id;
            //            //                        pathItem.Name = path.Name;
            //            //                        pathItems.Add(pathItem);
            //            //                    }

            //            //                    // Get a list of EventItems for the Path
            //            //                    pathItem.Events.AddRange(GetEvents(e, pathInfos, samples, timestamp));
            //            //                }
            //            //            }
            //            //            else
            //            //            {
            //            //                activityItem.Add(GetEvents(e, dataItemInfos, samples, timestamp));
            //            //            }
            //            //        }

            //            //        activityItem.Add(pathItems);

            //            //        // Write JSON to stream
            //            //        string json = Requests.ToJson(activityItem);
            //            //        var bytes = Encoding.UTF8.GetBytes(json);
            //            //        stream.Write(bytes, 0, bytes.Length);
            //            //    }
            //            //}

            //            if (query.Interval <= 0) break;
            //            else Thread.Sleep(query.Interval);
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        log.Info("Activity Stream Closed");
            //        log.Trace(ex);
            //    }

            //    return true;
            //}



            //// Get Samples for Time Range
            //var infos = SampleInfo.Create(dataItemInfos, samples);

            //var available = TimeSpan.Zero;

            //// Evaluate Event for each Timestamp
            //var events = EvaluateEvent(query.From, infos);
            //for (int i = 0; i < events.Count; i++)
            //{
            //    var e = events[i];
            //    if (e.Value == EVENT_VALUE)
            //    {
            //        available += e.Timestamp - query.From;

            //        if (i == events.Count - 1) available += query.To - e.Timestamp;
            //    }              
            //}

            //Console.WriteLine(available.ToString());

            


            // Get Time for "Available" event response

            // Calculate Availability













            //var e = GetEvent(EVENT_NAME);
            //if (e != null && !dataItemInfos.IsNullOrEmpty() && !samples.IsNullOrEmpty())
            //{
            //    // Create a list of SampleInfo objects with DataItem information contained
            //    var infos = SampleInfo.Create(dataItemInfos, samples);

            //    // Get a list of instance values
            //    var instance = infos.FindAll(o => o.Timestamp <= query.From);

            //    // Find all distinct timestamps greater than or equal to 'from'
            //    var timestamps = infos.FindAll(o => o.Timestamp > query.From).Select(o => o.Timestamp).Distinct().OrderBy(o => o).ToList();

            //    e.Evaluate(infos);
            //}

            return null;
        }

        private static TimeSpan GetAvailableTime(RequestQuery query, List<SampleInfo> infos)
        {
            var available = TimeSpan.Zero;

            // Evaluate Event for each Timestamp
            var events = EvaluateEvent(query.From, infos);

            Console.WriteLine("Events : " + events.Count);

            DateTime last1 = query.From;
            DateTime last2 = query.From;

            EventItem current = events[0];
            EventItem previous = events[0];

            var total = TimeSpan.Zero;

            for (int i = 0; i < events.Count; i++)
            {
                current = events[i];

                //last2 = e.Timestamp;

                last2 = current.Timestamp;

                Console.WriteLine(previous.Value + " - " + current.Value + " : " + last1.ToString("o") + " - " + last2.ToString("o") + " :: " + (last2 - last1).ToString());

                total += last2 - last1;

                if (previous.Value == EVENT_VALUE)
                {
                    //available += e.Timestamp - query.From;
                    available += last2 - last1;

                    //if (i == events.Count - 1)
                    //{
                    //    //available += query.To - current.Timestamp;
                    //    //total += query.To - current.Timestamp;
                    //}
                }

                last1 = last2;
                previous = current;         
            }

            last2 = query.To;

            Console.WriteLine(previous.Value + " - " + current.Value + " : " + last1.ToString("o") + " - " + last2.ToString("o") + " :: " + (last2 - last1).ToString());

            available += last2 - last1;
            total += last2 - last1;

            Console.WriteLine(available.ToString() + " of " + total.ToString());

            return available;
        }

        private static List<EventItem> EvaluateEvent(DateTime from, List<SampleInfo> samples)
        {
            var e = GetEvent(EVENT_NAME);

            // Get a list of instance values
            var instance = samples.FindAll(o => o.Timestamp <= from);

            // Find all distinct timestamps greater than or equal to 'from'
            var timestamps = samples.FindAll(o => o.Timestamp > from).Select(o => o.Timestamp).Distinct().OrderBy(o => o).ToList();

            int i = 0;
            DateTime timestamp = from;

            var events = new List<EventItem>();

            do
            {
                // Evaluate Event
                var response = e.Evaluate(instance);
                if (response != null)
                {
                    var item = new EventItem();
                    item.Timestamp = timestamp;
                    item.Name = e.Name;
                    item.Description = e.Description;
                    item.Value = response.Value;
                    item.ValueDescription = response.Description;

                    events.Add(item);
                }

                if (timestamps.Count > 0)
                {
                    timestamp = timestamps[i];

                    // Update instance values
                    var atTimestamp = samples.FindAll(o => o.Timestamp == timestamps[i]);
                    foreach (var sample in atTimestamp)
                    {
                        var match = instance.Find(o => o.Id == sample.Id);
                        if (match != null) instance.Remove(match);
                        instance.Add(sample);
                    }
                }
                else break;

                i++;

            } while (i < timestamps.Count - 1);

            return events;
        }

        private static Event GetEvent(string eventName)
        {
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, EventsConfiguration.FILENAME);

            // Read the EventsConfiguration file
            var config = EventsConfiguration.Get(configPath);
            if (config != null)
            {
                var e = config.Events.Find(o => o.Name.ToLower() == eventName.ToLower());
                if (e != null) return e;
            }

            return null;
        }

        private static string[] GetEventIds(Event e, List<DataItemDefinition> dataItems, List<ComponentDefinition> components)
        {
            var ids = new List<string>();

            foreach (var response in e.Responses)
            {
                foreach (var trigger in response.Triggers.OfType<Trigger>())
                {
                    foreach (var id in GetFilterIds(trigger.Filter, dataItems, components))
                    {
                        if (!ids.Exists(o => o == id)) ids.Add(id);
                    }
                }
            }

            return ids.ToArray();
        }

        private static string[] GetFilterIds(string filter, List<DataItemDefinition> dataItems, List<ComponentDefinition> components)
        {
            var ids = new List<string>();

            foreach (var dataItem in dataItems)
            {
                var dataFilter = new DataFilter(filter, dataItem, components);
                if (dataFilter.IsMatch() && !ids.Exists(o => o == dataItem.Id))
                {
                    ids.Add(dataItem.Id);
                }
            }

            return ids.ToArray();
        }

    }
}
