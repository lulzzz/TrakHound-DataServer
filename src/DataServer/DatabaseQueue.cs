// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using NLog;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TrakHound.Api.v2;
using TrakHound.Api.v2.Streams;
using TrakHound.Api.v2.Streams.Data;

namespace TrakHound.DataServer
{
    /// <summary>
    /// Handles writing data to the configured SQL database
    /// </summary>
    public class DatabaseQueue
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        private object _lock = new object();

        private List<IStreamData> queue = new List<IStreamData>();

        private ManualResetEvent stop;
        private Thread thread;

        /// <summary>
        /// Gets or Sets the interval at which the queue is read and queries are executed
        /// </summary>
        public int Interval { get; set; }

        /// <summary>
        /// Gets or Sets the maximum number of queries to read from the queue at a time
        /// </summary>
        public int MaxSamplePerQuery { get; set; }


        public DatabaseQueue()
        {
            Interval = 200;
            MaxSamplePerQuery = 2000;

            Start();
        }

        public void Start()
        {
            stop = new ManualResetEvent(false);

            thread = new Thread(new ThreadStart(Worker));
            thread.Start();
        }

        public void Stop()
        {
            if (stop != null) stop.Set();
        }

        public void Add(IStreamData data)
        {
            if (data != null)
            {
                lock (_lock) queue.Add(data);
            }
        }

        public void Add(List<IStreamData> data)
        {
            if (data != null && data.Count > 0)
            {
                foreach (var item in data)
                {
                    lock (_lock) queue.Add(item);
                }
            }
        }

        private void Worker()
        {
            do
            {
                List<IStreamData> streamData = null;

                lock (_lock) streamData = queue.Take(MaxSamplePerQuery).ToList();

                if (streamData != null && streamData.Count > 0)
                {
                    var sentItems = new List<string>();

                    // Write ConnectionDefintions to Database
                    var connections = streamData.OfType<ConnectionDefinitionData>().ToList();
                    if (Database.Write(connections)) sentItems.AddRange(connections.Select(o => o.EntryId));

                    // Write AgentDefintions to Database
                    var agents = streamData.OfType<AgentDefinitionData>().ToList();
                    if (Database.Write(agents)) sentItems.AddRange(agents.Select(o => o.EntryId));

                    // Write ComponentDefinitions to Database
                    var components = streamData.OfType<ComponentDefinitionData>().ToList();
                    if (Database.Write(components)) sentItems.AddRange(components.Select(o => o.EntryId));

                    // Write DataItems to Database
                    var dataItems = streamData.OfType<DataItemDefinitionData>().ToList();
                    if (Database.Write(dataItems)) sentItems.AddRange(dataItems.Select(o => o.EntryId));

                    // Write DeviceDefinitions to Database
                    var devices = streamData.OfType<DeviceDefinitionData>().ToList();
                    if (Database.Write(devices)) sentItems.AddRange(devices.Select(o => o.EntryId));

                    // Write Samples to Database
                    var samples = streamData.OfType<SampleData>().ToList();
                    if (Database.Write(samples)) sentItems.AddRange(samples.Select(o => o.EntryId));

                    if (sentItems.Count > 0)
                    {
                        log.Info(streamData.Count + " Items Written to Database successfully");

                        // Remove written samples
                        foreach (var item in streamData)
                        {
                            lock (_lock) queue.RemoveAll(o => o.EntryId == item.EntryId);
                        }
                    }
                }

            } while (!stop.WaitOne(Interval, true));
        }
    }

}
