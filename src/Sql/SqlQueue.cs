// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using NLog;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TrakHound.Api.v2.Streams.Data;
using TrakHound.Api.v2.Streams;

namespace TrakHound.DataServer.Sql
{
    public class SqlQueue
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        private object _lock = new object();

        private List<IStreamData> queue = new List<IStreamData>();

        private ManualResetEvent stop;
        private Thread thread;

        public int Interval { get; set; }

        public int MaxSamplePerQuery { get; set; }


        public SqlQueue()
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

                    // Write SQL
                    sentItems.AddRange(WriteSql(streamData.OfType<AgentDefinitionData>().ToList()));
                    sentItems.AddRange(WriteSql(streamData.OfType<DeviceDefinitionData>().ToList()));
                    sentItems.AddRange(WriteSql(streamData.OfType<ComponentDefinitionData>().ToList()));
                    sentItems.AddRange(WriteSql(streamData.OfType<DataItemDefinitionData>().ToList()));
                    sentItems.AddRange(WriteSql(streamData.OfType<SampleData>().ToList()));

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

        public virtual IEnumerable<string> WriteSql(List<AgentDefinitionData> definitions) { return Enumerable.Empty<string>(); }

        public virtual IEnumerable<string> WriteSql(List<ComponentDefinitionData> definitions) { return Enumerable.Empty<string>(); }

        public virtual IEnumerable<string> WriteSql(List<DataItemDefinitionData> definitions) { return Enumerable.Empty<string>(); }

        public virtual IEnumerable<string> WriteSql(List<DeviceDefinitionData> definitions) { return Enumerable.Empty<string>(); }

        public virtual IEnumerable<string> WriteSql(List<SampleData> samples) { return Enumerable.Empty<string>(); }
    }

}
