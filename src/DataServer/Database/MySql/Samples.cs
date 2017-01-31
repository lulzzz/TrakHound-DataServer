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
            public static List<Sample> Get(string[] dataItemIds, string deviceId, DateTime from, DateTime to, DateTime at, long count)
            {
                var samples = new List<Sample>();

                string COLUMNS = "`device_id`,`id`,`timestamp`,`sequence`,`cdata`,`condition`";
                string TABLENAME_ARCHIVED = "archived_samples";
                string TABLENAME_CURRENT = "current_samples";
                string INSTANCE_FORMAT = "CALL getInstance('{0}', {1})";

                string dataItemFilter = "";

                if (dataItemIds != null && dataItemIds.Length > 0)
                {
                    for (int i = 0; i < dataItemIds.Length; i++)
                    {
                        dataItemFilter += "`id`='" + dataItemIds[i] + "'";
                        if (i < dataItemIds.Length - 1) dataItemFilter += " OR ";
                    }

                    dataItemFilter = string.Format("({0}) AND ", dataItemFilter);
                }

                var queries = new List<string>();

                // Create query
                if (from > DateTime.MinValue && to > DateTime.MinValue)
                {
                    queries.Add(string.Format(INSTANCE_FORMAT, deviceId, from.ToUnixTime()));

                    string qf = "SELECT {0} FROM `{1}` WHERE {2}`device_id` = '{3}' AND `timestamp` >= '{4}' AND `timestamp` <= '{5}'";
                    queries.Add(string.Format(qf, COLUMNS, TABLENAME_ARCHIVED, dataItemFilter, deviceId, from.ToUnixTime(), to.ToUnixTime()));
                }
                else if (from > DateTime.MinValue && count > 0)
                {
                    queries.Add(string.Format(INSTANCE_FORMAT, deviceId, from.ToUnixTime()));

                    string qf = "SELECT {0} FROM `{1}` WHERE {2}`device_id` = '{3}' AND `timestamp` >= '{4}' LIMIT {5}";
                    queries.Add(string.Format(qf, COLUMNS, TABLENAME_ARCHIVED, dataItemFilter, deviceId, from.ToUnixTime(), count));
                }
                else if (to > DateTime.MinValue && count > 0)
                {
                    queries.Add(string.Format(INSTANCE_FORMAT, deviceId, to.ToUnixTime()));

                    string qf = "SELECT {0} FROM `{1}` WHERE {2}`device_id` = '{3}' AND `timestamp` <= '{4}' LIMIT {5}";
                    queries.Add(string.Format(qf, COLUMNS, TABLENAME_ARCHIVED, dataItemFilter, deviceId, to.ToUnixTime(), count));
                }
                else if (from > DateTime.MinValue)
                {
                    queries.Add(string.Format(INSTANCE_FORMAT, deviceId, from.ToUnixTime()));

                    string qf = "SELECT {0} FROM `{1}` WHERE {2}`device_id` = '{3}' AND `timestamp` <= '{4}' LIMIT 1000";
                    queries.Add(string.Format(qf, COLUMNS, TABLENAME_ARCHIVED, dataItemFilter, deviceId, from.ToUnixTime()));
                }
                else if (to > DateTime.MinValue)
                {
                    queries.Add(string.Format(INSTANCE_FORMAT, deviceId, to.ToUnixTime()));

                    string qf = "SELECT {0} FROM `{1}` WHERE {2}`device_id` = '{3}' AND `timestamp` <= '{4}' LIMIT 1000";
                    queries.Add(string.Format(qf, COLUMNS, TABLENAME_ARCHIVED, dataItemFilter, deviceId, to.ToUnixTime()));
                }
                else if (count > 0)
                {
                    string qf = "SELECT {0} FROM `{1}` WHERE {2}`device_id` = '{3}' ORDER BY `timestamp` DESC LIMIT {4}";
                    queries.Add(string.Format(qf, COLUMNS, TABLENAME_ARCHIVED, dataItemFilter, deviceId, count));
                }
                else if (at > DateTime.MinValue)
                {
                    queries.Add(string.Format(INSTANCE_FORMAT, deviceId, at.ToUnixTime()));
                }
                else
                {
                    string qf = "SELECT {0} FROM `{1}` WHERE {2}`device_id` = '{3}'";
                    queries.Add(string.Format(qf, COLUMNS, TABLENAME_CURRENT, dataItemFilter, deviceId, at.ToUnixTime()));
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
                                samples.Add(Read<Sample>(reader));
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
