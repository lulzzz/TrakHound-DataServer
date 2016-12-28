// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TrakHound.Api.v2;

namespace TrakHound.DataServer.Sql
{
    public class SqlQueue
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        private object _lock = new object();

        public class DataSample : Samples.Sample
        {
            public DataSample(Samples.Sample sample)
            {
                Uuid = Guid.NewGuid().ToString();
                DeviceId = sample.DeviceId;
                Id = sample.Id;
                Timestamp = sample.Timestamp;
                Value1 = sample.Value1;
                Value2 = sample.Value2;
            }

            public string Uuid { get; set; }
        }

        private List<DataSample> queue = new List<DataSample>();

        private ManualResetEvent stop;
        private Thread thread;

        public int Interval { get; set; }

        public int MaxSamplePerQuery { get; set; }


        public SqlQueue()
        {
            Interval = 1000;
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

        public void Add(List<Samples.Sample> samples)
        {
            if (samples != null && samples.Count > 0)
            {
                foreach (var sample in samples)
                {
                    lock (_lock) queue.Add(new DataSample(sample));
                }
            }
        }

        private void Worker()
        {
            do
            {
                List<DataSample> samples = null;

                lock (_lock) samples = queue.OrderBy(o => o.Timestamp).Take(MaxSamplePerQuery).ToList();

                if (samples != null && samples.Count > 0)
                {
                    if (WriteSql(samples))
                    {
                        log.Info(samples.Count + " Samples Written to Database succesfully");

                        // Remove written samples
                        foreach (var sample in samples)
                        {
                            lock (_lock) queue.RemoveAll(o => o.Uuid == sample.Uuid);
                        }
                    }
                }

            } while (!stop.WaitOne(Interval, true));
        }

        public virtual bool WriteSql(List<DataSample> samples)
        {
            return true;
        }
    }

}
