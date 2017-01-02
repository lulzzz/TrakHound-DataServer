// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using MySql.Data.MySqlClient;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using TrakHound.Api.v2.Streams.Data;
using TrakHound.Api.v2;

namespace TrakHound.DataServer.Sql
{
    public class MySqlQueue : SqlQueue
    {
        private readonly static DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static Logger log = LogManager.GetCurrentClassLogger();

        string CONNECTION = "server={0};uid={1};pwd={2};database={3};";

        private string connectionString;

        public MySqlQueue(string server, string user, string password, string database)
        {
            connectionString = string.Format(CONNECTION, server, user, password, database);

            log.Info("MySql Database Configuration");
            log.Info("---------------------------");
            log.Info("Server = " + server);
            log.Info("User = " + user);
            log.Info("database = " + database);
            log.Info("---------------------------");
        }

        public override IEnumerable<string> WriteSql(List<AgentDefinitionData> definitions)
        {
            if (definitions != null && definitions.Count > 0)
            {
                string COLUMNS = "`device_id`, `instance_id`, `sender`, `version`, `buffer_size`, `test_indicator`, `timestamp`";
                string QUERY_FORMAT = "INSERT IGNORE INTO `agents` ({0}) VALUES {1}";
                string VALUE_FORMAT = "('{0}','{1}','{2}','{3}','{4}','{5}','{6}')";

                // Build VALUES string
                var v = new string[definitions.Count];
                for (var i = 0; i < definitions.Count; i++)
                {
                    var d = definitions[i];
                    v[i] = string.Format(VALUE_FORMAT, d.DeviceId, d.InstanceId, d.Sender, d.Version, d.BufferSize, d.TestIndicator, d.Timestamp.ToUnixTime());
                }
                string values = string.Join(",", v);

                // Build Query string
                string query = string.Format(QUERY_FORMAT, COLUMNS, values);

                try
                {
                    // Execute Query
                    if (MySqlHelper.ExecuteNonQuery(connectionString, query, null) >= 0)
                    {
                        // Return the EntryIds of the items that were written successfully
                        return definitions.Select(o => o.EntryId).ToList();
                    }
                }
                catch (MySqlException ex)
                {
                    log.Error(ex);
                }
            }

            return Enumerable.Empty<string>();
        }

        public override IEnumerable<string> WriteSql(List<DeviceDefinitionData> definitions)
        {
            if (definitions != null && definitions.Count > 0)
            {
                string COLUMNS = "`device_id`, `agent_instance_id`, `id`, `uuid`, `name`, `native_name`, `sample_interval`, `sample_rate`, `iso_841_class`, `timestamp`";
                string QUERY_FORMAT = "INSERT IGNORE INTO `devices` ({0}) VALUES {1}";
                string VALUE_FORMAT = "('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}')";

                // Build VALUES string
                var v = new string[definitions.Count];
                for (var i = 0; i < definitions.Count; i++)
                {
                    var d = definitions[i];
                    v[i] = string.Format(VALUE_FORMAT, d.DeviceId, d.AgentInstanceId, d.Id, d.Uuid, d.Name, d.NativeName, d.SampleInterval, d.SampleRate, d.Iso841Class, d.Timestamp.ToUnixTime());
                }
                string values = string.Join(",", v);

                // Build Query string
                string query = string.Format(QUERY_FORMAT, COLUMNS, values);

                try
                {
                    // Execute Query
                    if (MySqlHelper.ExecuteNonQuery(connectionString, query, null) >= 0)
                    {
                        // Return the EntryIds of the items that were written successfully
                        return definitions.Select(o => o.EntryId).ToList();
                    }
                }
                catch (MySqlException ex)
                {
                    log.Error(ex);
                }
            }

            return Enumerable.Empty<string>();
        }

        public override IEnumerable<string> WriteSql(List<ComponentDefinitionData> definitions)
        {
            if (definitions != null && definitions.Count > 0)
            {
                string COLUMNS = "`device_id`,`agent_instance_id`, `id`, `uuid`, `name`, `native_name`, `sample_interval`, `sample_rate`, `component`,`parent_id`, `timestamp`";
                string QUERY_FORMAT = "INSERT IGNORE INTO `components` ({0}) VALUES {1}";
                string VALUE_FORMAT = "('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}')";

                // Build VALUES string
                var v = new string[definitions.Count];
                for (var i = 0; i < definitions.Count; i++)
                {
                    var d = definitions[i];
                    v[i] = string.Format(VALUE_FORMAT, d.DeviceId, d.AgentInstanceId, d.Id, d.Uuid, d.Name, d.NativeName, d.SampleInterval, d.SampleRate, d.Component, d.ParentId, d.Timestamp.ToUnixTime());
                }
                string values = string.Join(",", v);

                // Build Query string
                string query = string.Format(QUERY_FORMAT, COLUMNS, values);

                try
                {
                    // Execute Query
                    if (MySqlHelper.ExecuteNonQuery(connectionString, query, null) >= 0)
                    {
                        // Return the EntryIds of the items that were written successfully
                        return definitions.Select(o => o.EntryId).ToList();
                    }
                }
                catch (MySqlException ex)
                {
                    log.Error(ex);
                }
            }

            return Enumerable.Empty<string>();
        }

        public override IEnumerable<string> WriteSql(List<DataItemDefinitionData> definitions)
        {
            if (definitions != null && definitions.Count > 0)
            {
                string COLUMNS = "`device_id`,`agent_instance_id`, `id`, `name`, `category`, `type`, `sub_type`, `statistic`, `units`,`native_units`,`native_scale`,`coordinate_system`,`sample_rate`,`representation`,`significant_digits`,`parent_id`, `timestamp`";
                string QUERY_FORMAT = "INSERT IGNORE INTO `data_items` ({0}) VALUES {1}";
                string VALUE_FORMAT = "('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}','{12}','{13}','{14}','{15}','{16}')";

                // Build VALUES string
                var v = new string[definitions.Count];
                for (var i = 0; i < definitions.Count; i++)
                {
                    var d = definitions[i];
                    v[i] = string.Format(VALUE_FORMAT, d.DeviceId, d.AgentInstanceId, d.Id, d.Name, d.Catergory, d.Type, d.SubType, d.Statistic, d.Units, d.NativeUnits, d.NativeScale, d.CoordinateSystem, d.SampleRate, d.Representation, d.SignificantDigits, d.ParentId, d.Timestamp.ToUnixTime());
                }
                string values = string.Join(",", v);

                // Build Query string
                string query = string.Format(QUERY_FORMAT, COLUMNS, values);

                try
                {
                    // Execute Query
                    if (MySqlHelper.ExecuteNonQuery(connectionString, query, null) >= 0)
                    {
                        // Return the EntryIds of the items that were written successfully
                        return definitions.Select(o => o.EntryId).ToList();
                    }
                }
                catch (MySqlException ex)
                {
                    log.Error(ex);
                }
            }

            return Enumerable.Empty<string>();
        }

        public override IEnumerable<string> WriteSql(List<SampleData> samples)
        {  
            if (samples != null && samples.Count > 0)
            {
                string COLUMNS = "`device_id`, `id`, `timestamp`, `agent_instance_id`, `sequence`, `cdata`, `condition`";
                string QUERY_FORMAT_ARCHIVED = "INSERT IGNORE INTO `archived_samples` ({0}) VALUES {1}";
                string QUERY_FORMAT_CURRENT = "INSERT IGNORE INTO `current_samples` ({0}) VALUES {1} ON DUPLICATE KEY UPDATE {2}";
                string VALUE_FORMAT = "('{0}','{1}','{2}','{3}','{4}','{5}','{6}')";
                string UPDATE_FORMAT = "`timestamp`='{0}',`agent_instance_id`='{1}',`sequence`='{2}',`cdata`='{3}',`condition`='{4}'";

                // Build Archived VALUES string
                string values = "";
                var archived = samples.FindAll(o => o.StreamDataType == Api.v2.Streams.StreamDataType.ARCHIVED_SAMPLE);
                for (var i = 0; i < archived.Count; i++)
                {
                    var sample = archived[i];

                    values += string.Format(VALUE_FORMAT,
                        sample.DeviceId,
                        sample.Id,
                        ToUnixTime(sample.Timestamp),
                        sample.AgentInstanceId,
                        sample.Sequence,
                        sample.CDATA,
                        sample.Condition
                        );

                    if (i < archived.Count - 1) values += ",";
                }

                // Build Current Queries
                var currentQueries = new List<string>();
                var ids = samples.Select(o => o.Id).Distinct();
                foreach (var id in ids)
                {
                    var sample = samples.OrderBy(o => o.Timestamp).ToList().First(o => o.Id == id);
                    if (sample != null)
                    {
                        string currentValues = string.Format(VALUE_FORMAT, sample.DeviceId, sample.Id, ToUnixTime(sample.Timestamp), sample.AgentInstanceId, sample.Sequence, sample.CDATA, sample.Condition);
                        string currentUpdate = string.Format(UPDATE_FORMAT, ToUnixTime(sample.Timestamp), sample.AgentInstanceId, sample.Sequence, sample.CDATA, sample.Condition);
                        currentQueries.Add(string.Format(QUERY_FORMAT_CURRENT, COLUMNS, currentValues, currentUpdate));
                    }
                }

                var queries = new List<string>();

                // Add Archived Query
                if (archived.Count > 0) queries.Add(string.Format(QUERY_FORMAT_ARCHIVED, COLUMNS, values));

                // Add Current Queries
                queries.AddRange(currentQueries);

                string query = string.Join(";", queries);

                try
                {
                    // Execute Samples Query
                    if (MySqlHelper.ExecuteNonQuery(connectionString, query, null) > 0)
                    {
                        // Return the EntryIds of the items that were written successfully
                        return samples.Select(o => o.EntryId).ToList();
                    }
                }
                catch (MySqlException ex)
                {
                    log.Error(ex);
                }
            }    
            
            return Enumerable.Empty<string>();
        }

        public static long ToUnixTime(DateTime date)
        {
            return Convert.ToInt64((date.ToUniversalTime() - epoch).TotalMilliseconds);
        }

    }
}
