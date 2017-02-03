// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Text;
using System.Threading;
using TrakHound.Api.v2;

namespace mod_rest_samples
{
    [InheritedExport(typeof(IRestModule))]
    public class Samples : IRestModule
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        public string Name { get { return "Samples"; } }

        public bool GetResponse(Uri requestUri, Stream stream)
        {
            var query = new RequestQuery(requestUri);
            if (query.IsValid)
            {
                try
                {
                    var sent = new List<Item>();

                    while (stream != null)
                    {
                        // Read Samples from Database
                        var samples = Database.ReadSamples(null, query.DeviceId, query.From, query.To, query.At, query.Count);
                        if (!samples.IsNullOrEmpty())
                        {
                            foreach (var sample in samples)
                            {
                                bool write = true;

                                // Only write to output stream if new
                                var x = sent.Find(o => o.Id == sample.Id);
                                if (x != null)
                                {
                                    if (sample.Timestamp > x.Timestamp)
                                    {
                                        sent.Remove(x);
                                        sent.Add(new Item(sample));
                                    }
                                    else write = false;
                                }
                                else sent.Add(new Item(sample));

                                if (write)
                                {
                                    string json = Requests.ToJson(sample);
                                    json += Environment.NewLine;
                                    var bytes = Encoding.UTF8.GetBytes(json);
                                    stream.Write(bytes, 0, bytes.Length);
                                }
                            }
                        }

                        if (query.Interval <= 0) break;
                        else Thread.Sleep(query.Interval);
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    log.Info("Samples Stream Closed");
                    log.Trace(ex);
                }
            }

            return false;
        }
    }
}
