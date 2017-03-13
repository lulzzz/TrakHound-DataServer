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

        /// <summary>
        /// Gets the Count of the underlying queue
        /// </summary>
        public int Count
        {
            get
            {
                lock (_lock)
                {
                    if (queue != null) return queue.Count;
                }

                return -1;
            }
        }


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
                    var sentIds = new List<string>();

                    // Write ConnectionDefintions to Database
                    var connections = streamData.OfType<ConnectionDefinitionData>().ToList();
                    if (Database.Write(connections)) sentIds.AddRange(GetSentDataIds(connections.ToList<IStreamData>(), "Connections"));

                    // Write AgentDefintions to Database
                    var agents = streamData.OfType<AgentDefinitionData>().ToList();
                    if (Database.Write(agents)) sentIds.AddRange(GetSentDataIds(agents.ToList<IStreamData>(), "Agents"));

                    // Write ComponentDefinitions to Database
                    var components = streamData.OfType<ComponentDefinitionData>().ToList();
                    if (Database.Write(components)) sentIds.AddRange(GetSentDataIds(components.ToList<IStreamData>(), "Components"));

                    // Write DataItems to Database
                    var dataItems = streamData.OfType<DataItemDefinitionData>().ToList();
                    if (Database.Write(dataItems)) sentIds.AddRange(GetSentDataIds(dataItems.ToList<IStreamData>(), "DataItems"));

                    // Write DeviceDefinitions to Database
                    var devices = streamData.OfType<DeviceDefinitionData>().ToList();
                    if (Database.Write(devices)) sentIds.AddRange(GetSentDataIds(devices.ToList<IStreamData>(), "Devices"));

                    // Write Samples to Database
                    var samples = streamData.OfType<SampleData>().ToList();
                    if (Database.Write(samples)) sentIds.AddRange(GetSentDataIds(samples.ToList<IStreamData>(), "Samples"));

                    // Write Statuses to Database
                    var statuses = streamData.OfType<StatusData>().ToList();
                    if (statuses != null && statuses.Count > 0)
                    {
                        var newStatuses = new List<StatusData>();

                        var deviceIds = statuses.Select(o => o.DeviceId).Distinct();
                        foreach (var deviceId in deviceIds)
                        {
                            var status = statuses.FindAll(o => o.DeviceId == deviceId).OrderByDescending(o => o.Timestamp).First();
                            newStatuses.Add(status);
                        }

                        if (Database.Write(newStatuses)) sentIds.AddRange(GetSentDataIds(newStatuses.ToList<IStreamData>(), "Status"));
                    }


                    if (sentIds.Count > 0)
                    {
                        // Remove written samples
                        foreach (var id in sentIds)
                        {
                            lock (_lock) queue.RemoveAll(o => o.EntryId == id);
                        }
                    }
                }

            } while (!stop.WaitOne(Interval, true));
        }

        private List<string> GetSentDataIds(List<IStreamData> sentData, string tag)
        {
            var sent = sentData.Select(o => o.EntryId).ToList();
            if (sent.Count > 0)
            {
                log.Info(sent.Count + " " + tag + " Written to Database successfully");
                return sent;
            }

            return new List<string>();
        }
    }

}
