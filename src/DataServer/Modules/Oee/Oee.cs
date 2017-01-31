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
    [InheritedExport(typeof(IModule))]
    public class Oee : IModule
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        public string Name { get { return "Oee"; } }


        public bool GetResponse(Uri requestUri, Stream stream)
        {
            var query = new RequestQuery(requestUri);
            if (query.IsValid)
            {
                string json = null;

                // Get the requested Query/SubQuery
                switch (query.SubQuery)
                {
                    case "availability": json = Availability.Get(query, stream); break;
                    case "performance": Console.WriteLine("Performance"); break;
                    case "quality": Console.WriteLine("Quality"); break;
                    default: Console.WriteLine("OEE"); break;
                }

                if (!string.IsNullOrEmpty(json))
                {
                    // Write JSON to stream
                    var bytes = Encoding.UTF8.GetBytes(json);
                    stream.Write(bytes, 0, bytes.Length);
                }
            }

            return false;
        }

        //private List<Event> GetEvents()
        //{
        //    string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, EventsConfiguration.FILENAME);

        //    // Read the EventsConfiguration file
        //    var config = EventsConfiguration.Get(configPath);
        //    if (config != null)
        //    {
        //        return config.Events;
        //    }

        //    return null;
        //}

        //private List<EventItem> GetEvents(Event e, List<DataItemInfo> dataItemInfos, List<Sample> samples, DateTime from)
        //{
        //    var l = new List<EventItem>();

        //    if (!samples.IsNullOrEmpty())
        //    {
        //        // Create a list of SampleInfo objects with DataItem information contained
        //        var infos = SampleInfo.Create(dataItemInfos, samples);

        //        // Get a list of instance values
        //        var instance = infos.FindAll(o => o.Timestamp <= from);

        //        // Find all distinct timestamps greater than or equal to 'from'
        //        var timestamps = infos.FindAll(o => o.Timestamp > from).Select(o => o.Timestamp).Distinct().OrderBy(o => o).ToList();

        //        int i = 0;
        //        DateTime timestamp = from;

        //        do
        //        {
        //            // Evaluate Event
        //            var response = e.Evaluate(instance);
        //            if (response != null)
        //            {
        //                var item = new EventItem();
        //                item.Timestamp = timestamp;
        //                item.Name = e.Name;
        //                item.Description = e.Description;
        //                item.Value = response.Value;
        //                item.ValueDescription = response.Description;

        //                l.Add(item);
        //            }

        //            if (timestamps.Count > 0)
        //            {
        //                // Update instance values
        //                var atTimestamp = infos.FindAll(o => o.Timestamp == timestamps[i]);
        //                foreach (var sample in atTimestamp)
        //                {
        //                    var match = instance.Find(o => o.Id == sample.Id);
        //                    if (match != null) instance.Remove(match);
        //                    instance.Add(sample);
        //                }
        //            }
        //            else break;

        //            i++;

        //        } while (i < timestamps.Count - 1);
        //    }

        //    return l;
        //}

        //private List<EventItem> GetEvents(List<DataItemInfo> dataItemInfos, List<Sample> samples, DateTime from)
        //{
        //    var l = new List<EventItem>();

        //    if (!samples.IsNullOrEmpty())
        //    {
        //        string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, EventsConfiguration.FILENAME);

        //        // Read the EventsConfiguration file
        //        var config = EventsConfiguration.Get(configPath);
        //        if (config != null)
        //        {
        //            // Create a list of SampleInfo objects with DataItem information contained
        //            var infos = SampleInfo.Create(dataItemInfos, samples);

        //            // Get a list of instance values
        //            var instance = infos.FindAll(o => o.Timestamp <= from);

        //            // Find all distinct timestamps greater than or equal to 'from'
        //            var timestamps = infos.FindAll(o => o.Timestamp > from).Select(o => o.Timestamp).Distinct().OrderBy(o => o).ToList();

        //            int i = 0;
        //            DateTime timestamp = from;

        //            do
        //            {
        //                // Evaluate Events
        //                foreach (var e in config.Events)
        //                {
        //                    var response = e.Evaluate(instance);
        //                    if (response != null)
        //                    {
        //                        var item = new EventItem();
        //                        item.Timestamp = timestamp;
        //                        item.Name = e.Name;
        //                        item.Description = e.Description;
        //                        item.Value = response.Value;
        //                        item.ValueDescription = response.Description;

        //                        l.Add(item);
        //                    }
        //                }

        //                if (timestamps.Count > 0)
        //                {
        //                    // Update instance values
        //                    var atTimestamp = infos.FindAll(o => o.Timestamp == timestamps[i]);
        //                    foreach (var sample in atTimestamp)
        //                    {
        //                        var match = instance.Find(o => o.Id == sample.Id);
        //                        if (match != null) instance.Remove(match);
        //                        instance.Add(sample);
        //                    }
        //                }
        //                else break;

        //                i++;

        //            } while (i < timestamps.Count - 1);
        //        }
        //    }

        //    return l;
        //}

        ///// <summary>
        ///// Get whether or not the Event is triggered by an item that is part of a Path component
        ///// </summary>
        //private static bool ContainsPath(Event e, List<ComponentDefinition> components, List<DataItemDefinition> dataItems)
        //{
        //    foreach (var response in e.Responses)
        //    {
        //        foreach (var trigger in response.Triggers.OfType<Trigger>())
        //        {
        //            string filter = trigger.Filter;

        //            bool match = false;

        //            if (filter.Contains("Path")) return true;
        //            else
        //            {
        //                var parts = filter.Split('/');
        //                string dataType = parts[parts.Length - 1];

        //                foreach (var path in components.FindAll(o => o.Type == "Path"))
        //                {
        //                    var pathDataItems = dataItems.FindAll(o => o.ParentId == path.Id);
        //                    match = pathDataItems.Exists(o => NormalizeType(o.Type) == NormalizeType(dataType));
        //                    if (match) return true;
        //                }
        //            }
        //        }
        //    }

        //    return false;
        //}

        //private static string NormalizeType(string s)
        //{
        //    string debug = s;

        //    if (!string.IsNullOrEmpty(s))
        //    {
        //        if (s.ToUpper() != s)
        //        {
        //            // Split string by Uppercase characters
        //            var parts = Regex.Split(s, @"(?<!^)(?=[A-Z])");
        //            s = string.Join("_", parts);
        //            s = s.ToUpper();
        //        }

        //        // Return to Pascal Case
        //        s = s.Replace("_", " ");
        //        s = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(s.ToLower());

        //        s = s.Replace(" ", "");
        //    }

        //    return s;
        //}

    }
}
