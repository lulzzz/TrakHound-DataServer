// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using Newtonsoft.Json;
using MySql.Data.MySqlClient;
using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Text;
using System.Threading;
using System.Web;
using TrakHound.Api.v2;
using TrakHound.Api.v2.Data;
using TrakHound.DataServer.Rest;

namespace TrakHound.DataServer.Modules
{
    [InheritedExport(typeof(IModule))]
    public class Samples : IModule
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        private const string COLUMNS = "`device_id`,`id`,`timestamp`,`sequence`,`cdata`,`condition`";
        private const string TABLENAME_ARCHIVED = "archived_samples";
        private const string TABLENAME_CURRENT = "current_samples";

        private class SampleData
        {
            [JsonProperty("timestamp")]
            [JsonConverter(typeof(UnixTimeJsonConverter))]
            public DateTime Timestamp { get; set; }

            [JsonProperty("id")]
            public string Id { get; set; }

            [JsonProperty("sequence")]
            public long Sequence { get; set; }

            [JsonProperty("cdata")]
            public string CDATA { get; set; }

            [JsonProperty("condition")]
            public string Condition { get; set; }
        }


        public string Name { get { return "Samples"; } }

        public bool GetResponse(Uri requestUri, Stream stream)
        {
            var segments = requestUri.Segments;
            if (segments.Length > 1)
            {
                // Check if Samples is the resource that is requested
                if (segments[segments.Length - 1].ToLower().Trim('/') == "samples")
                {
                    // Get the Device Id as the resource owner
                    string deviceId = segments[segments.Length - 2].Trim('/');
                    if (!string.IsNullOrEmpty(deviceId))
                    {
                        // From
                        string s = HttpUtility.ParseQueryString(requestUri.Query).Get("from");
                        DateTime from = DateTime.MinValue;
                        DateTime.TryParse(s, out from);

                        // To
                        s = HttpUtility.ParseQueryString(requestUri.Query).Get("to");
                        DateTime to = DateTime.MinValue;
                        DateTime.TryParse(s, out to);

                        // Count
                        s = HttpUtility.ParseQueryString(requestUri.Query).Get("count");
                        long count = 0;
                        long.TryParse(s, out count);

                        // At
                        s = HttpUtility.ParseQueryString(requestUri.Query).Get("at");
                        DateTime at = DateTime.MinValue;
                        DateTime.TryParse(s, out at);

                        // Interval
                        s = HttpUtility.ParseQueryString(requestUri.Query).Get("interval");
                        int interval = 0;
                        int.TryParse(s, out interval);

                        var config = Configuration.Current;
                        if (config != null)
                        {
                            try
                            {
                                var sent = new List<SampleData>();

                                while (true)
                                {
                                    // MySQL
                                    if (config.Database.GetType() == typeof(Sql.MySqlConfiguration))
                                    {
                                        var samples = GetFromMySql(deviceId, from, to, count, at);
                                        if (samples != null && samples.Count > 0)
                                        {
                                            foreach (var sample in samples)
                                            {
                                                bool write = true;

                                                // Only write to output stream if new
                                                var current = sent.Find(o => o.Id == sample.Id);
                                                if (current != null)
                                                {
                                                    if (sample.Timestamp > current.Timestamp)
                                                    {
                                                        sent.Remove(current);
                                                        sent.Add(sample);
                                                    }
                                                    else write = false;
                                                }
                                                else sent.Add(sample);

                                                if (write)
                                                {
                                                    string json = Requests.ToJson(sample);
                                                    var bytes = Encoding.UTF8.GetBytes(json);
                                                    stream.Write(bytes, 0, bytes.Length);
                                                }
                                            }
                                        }
                                    }
                                    else break;

                                    if (interval <= 0) break;
                                    else Thread.Sleep(interval);
                                }
                            }
                            catch (Exception ex)
                            {
                                log.Info("Samples Stream Closed");
                                log.Trace(ex);
                            }

                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private List<SampleData> GetFromMySql(string deviceId, DateTime from, DateTime to, long count, DateTime at)
        {
            var config = (Sql.MySqlConfiguration)Configuration.Current.Database;

            // Create connection string
            string cf = "server={0};uid={1};pwd={2};database={3};";
            string c = string.Format(cf, config.Server, config.User, config.Password, config.Database);

            string query = "";

            // Create query
            if (from > DateTime.MinValue && to > DateTime.MinValue)
            {
                string qf = "SELECT {0} FROM `{1}` WHERE `device_id` = '{2}' AND `timestamp` >= '{3}' AND `timestamp` <= '{4}'";
                query = string.Format(qf, COLUMNS, TABLENAME_ARCHIVED, deviceId, from.ToUnixTime(), to.ToUnixTime());
            }
            else if (from > DateTime.MinValue && count > 0)
            {
                string qf = "SELECT {0} FROM `{1}` WHERE `device_id` = '{0}' AND `timestamp` >= '{1}' LIMIT {2}";
                query = string.Format(qf, COLUMNS, TABLENAME_ARCHIVED, deviceId, from.ToUnixTime(), count);
            }
            else if (to > DateTime.MinValue && count > 0)
            {
                string qf = "SELECT {0} FROM `{1}` WHERE `device_id` = '{0}' AND `timestamp` <= '{1}' LIMIT {2}";
                query = string.Format(qf, COLUMNS, TABLENAME_ARCHIVED, deviceId, to.ToUnixTime(), count);
            }
            else if (from > DateTime.MinValue)
            {
                string qf = "SELECT {0} FROM `{1}` WHERE `device_id` = '{0}' AND `timestamp` <= '{1}' LIMIT 1000";
                query = string.Format(qf, COLUMNS, TABLENAME_ARCHIVED, deviceId, from.ToUnixTime());
            }
            else if (to > DateTime.MinValue)
            {
                string qf = "SELECT {0} FROM `{1}` WHERE `device_id` = '{0}' AND `timestamp` <= '{1}' LIMIT 1000";
                query = string.Format(qf, COLUMNS, TABLENAME_ARCHIVED, deviceId, to.ToUnixTime());
            }
            else if (count > 0)
            {
                string qf = "SELECT {0} FROM `{1}` WHERE `device_id` = '{0}' ORDER BY `timestamp` DESC LIMIT {1}";
                query = string.Format(qf, COLUMNS, TABLENAME_ARCHIVED, deviceId, count);
            }
            else if (at > DateTime.MinValue)
            {
                string qf = "SELECT {0} FROM `{1}` WHERE `device_id` = '{0}' AND `timestamp` = '{1}'";
                query = string.Format(qf, COLUMNS, TABLENAME_ARCHIVED, deviceId, at.ToUnixTime());
            }
            else
            {
                string qf = "SELECT {0} FROM `{1}` WHERE `device_id` = '{2}'";
                query = string.Format(qf, COLUMNS, TABLENAME_CURRENT, deviceId, at.ToUnixTime());
            }

            if (!string.IsNullOrEmpty(query))
            {
                try
                {
                    using (var reader = MySqlHelper.ExecuteReader(c, query, null))
                    {
                        var samples = new List<SampleData>();

                        while (reader.Read())
                        {
                            var s = reader.GetString("timestamp");
                            long ms;
                            if (long.TryParse(s, out ms))
                            {
                                s = reader.GetString("sequence");
                                long sequence;
                                if (long.TryParse(s, out sequence))
                                {
                                    // Calculate DateTime from unix milliseconds
                                    var ts = UnixTimeExtensions.EpochTime.AddMilliseconds(ms);

                                    // Create new SampleData object
                                    var sample = new SampleData();
                                    sample.Id = reader.GetString("id");
                                    sample.Timestamp = ts;
                                    sample.Sequence = sequence;

                                    // CDATA
                                    s = reader.GetString("cdata");
                                    if (!string.IsNullOrEmpty(s)) sample.CDATA = s;

                                    // Condition
                                    s = reader.GetString("condition");
                                    if (!string.IsNullOrEmpty(s)) sample.Condition = s;

                                    samples.Add(sample);
                                }
                            }
                        }

                        return samples;
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
