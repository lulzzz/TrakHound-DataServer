// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using TrakHound.Api.v2;
using TrakHound.Api.v2.Data;

namespace TrakHound.DataServer.Sql
{
    public partial class MySql
    {
        public static class Samples
        {
            public static List<Sample> Get(string deviceId, DateTime from, DateTime to, DateTime at, long count)
            {
                var samples = new List<Sample>();

                string COLUMNS = "`device_id`,`id`,`timestamp`,`sequence`,`cdata`,`condition`";
                string TABLENAME_ARCHIVED = "archived_samples";
                string TABLENAME_CURRENT = "current_samples";
                string INSTANCE_FORMAT = "CALL getInstance('{0}', {1})";

                var queries = new List<string>();

                // Create query
                if (from > DateTime.MinValue && to > DateTime.MinValue)
                {
                    queries.Add(string.Format(INSTANCE_FORMAT, deviceId, from.ToUnixTime()));

                    string qf = "SELECT {0} FROM `{1}` WHERE `device_id` = '{2}' AND `timestamp` >= '{3}' AND `timestamp` <= '{4}'";
                    queries.Add(string.Format(qf, COLUMNS, TABLENAME_ARCHIVED, deviceId, from.ToUnixTime(), to.ToUnixTime()));
                }
                else if (from > DateTime.MinValue && count > 0)
                {
                    queries.Add(string.Format(INSTANCE_FORMAT, deviceId, from.ToUnixTime()));

                    string qf = "SELECT {0} FROM `{1}` WHERE `device_id` = '{0}' AND `timestamp` >= '{1}' LIMIT {2}";
                    queries.Add(string.Format(qf, COLUMNS, TABLENAME_ARCHIVED, deviceId, from.ToUnixTime(), count));
                }
                else if (to > DateTime.MinValue && count > 0)
                {
                    queries.Add(string.Format(INSTANCE_FORMAT, deviceId, to.ToUnixTime()));

                    string qf = "SELECT {0} FROM `{1}` WHERE `device_id` = '{0}' AND `timestamp` <= '{1}' LIMIT {2}";
                    queries.Add(string.Format(qf, COLUMNS, TABLENAME_ARCHIVED, deviceId, to.ToUnixTime(), count));
                }
                else if (from > DateTime.MinValue)
                {
                    queries.Add(string.Format(INSTANCE_FORMAT, deviceId, from.ToUnixTime()));

                    string qf = "SELECT {0} FROM `{1}` WHERE `device_id` = '{0}' AND `timestamp` <= '{1}' LIMIT 1000";
                    queries.Add(string.Format(qf, COLUMNS, TABLENAME_ARCHIVED, deviceId, from.ToUnixTime()));
                }
                else if (to > DateTime.MinValue)
                {
                    queries.Add(string.Format(INSTANCE_FORMAT, deviceId, to.ToUnixTime()));

                    string qf = "SELECT {0} FROM `{1}` WHERE `device_id` = '{0}' AND `timestamp` <= '{1}' LIMIT 1000";
                    queries.Add(string.Format(qf, COLUMNS, TABLENAME_ARCHIVED, deviceId, to.ToUnixTime()));
                }
                else if (count > 0)
                {
                    string qf = "SELECT {0} FROM `{1}` WHERE `device_id` = '{0}' ORDER BY `timestamp` DESC LIMIT {1}";
                    queries.Add(string.Format(qf, COLUMNS, TABLENAME_ARCHIVED, deviceId, count));
                }
                else if (at > DateTime.MinValue)
                {
                    queries.Add(string.Format(INSTANCE_FORMAT, deviceId, at.ToUnixTime()));
                }
                else
                {
                    string qf = "SELECT {0} FROM `{1}` WHERE `device_id` = '{2}'";
                    queries.Add(string.Format(qf, COLUMNS, TABLENAME_CURRENT, deviceId, at.ToUnixTime()));
                }

                foreach (var query in queries) samples.AddRange(Get(query));

                return samples;
            }

            private static List<Sample> Get(string query)
            {
                var samples = new List<Sample>();

                if (!string.IsNullOrEmpty(query))
                {
                    try
                    {
                        using (var reader = MySqlHelper.ExecuteReader(connectionString, query, null))
                        {
                            while (reader.Read())
                            {
                                // Create new Item object
                                var sample = new Sample();
                                sample.DeviceId = reader.GetString("device_id");
                                sample.Id = reader.GetString("id");
                                sample.Sequence = reader.GetInt64("sequence");

                                // Calculate DateTime from unix milliseconds
                                sample.Timestamp = UnixTimeExtensions.EpochTime.AddMilliseconds(reader.GetInt64("timestamp"));

                                // CDATA
                                string s = reader.GetString("cdata");
                                if (!string.IsNullOrEmpty(s)) sample.CDATA = s;

                                // Condition
                                s = reader.GetString("condition");
                                if (!string.IsNullOrEmpty(s)) sample.Condition = s;

                                samples.Add(sample);
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

                return samples;
            }
        }
    }
}
