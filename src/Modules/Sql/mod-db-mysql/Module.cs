// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using MySql.Data.MySqlClient;
using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using TrakHound.Api.v2;
using TrakHound.Api.v2.Data;
using TrakHound.Api.v2.Streams;
using TrakHound.Api.v2.Streams.Data;

namespace mod_db_mysql
{
    [InheritedExport(typeof(IDatabaseModule))]
    public class Module : IDatabaseModule
    {
        private const string CONNECTION_FORMAT = "server={0};uid={1};pwd={2};database={3};";

        private static Logger logger = LogManager.GetCurrentClassLogger();
        private static string connectionString;

        private Configuration configuration;

        /// <summary>
        /// Gets the name of the Database. This corresponds to the node name in the 'server.config' file
        /// </summary>
        public string Name { get { return "MySql"; } }

        
        public bool Initialize(string databaseConfigurationPath)
        {
            var config = Configuration.Get(databaseConfigurationPath);
            if (config != null)
            {
                connectionString = string.Format(CONNECTION_FORMAT, config.Server, config.User, config.Password, config.Database);
                configuration = config;
                return true;
            }

            return false;
        }

        #region "Read"

        /// <summary>
        /// Read the most current AgentDefintion from the database
        /// </summary>
        public AgentDefinition ReadAgent(string deviceId) { return null; }

        /// <summary>
        /// Read the ComponentDefinitions for the specified Agent Instance Id from the database
        /// </summary>
        public List<ComponentDefinition> ReadComponents(string deviceId, long agentInstanceId) { return null; }

        /// <summary>
        /// Read the DataItemDefinitions for the specified Agent Instance Id from the database
        /// </summary>
        public List<DataItemDefinition> ReadDataItems(string deviceId, long agentInstanceId) { return null; }

        /// <summary>
        /// Read the DeviceDefintion for the specified Agent Instance Id from the database
        /// </summary>
        public DeviceDefinition ReadDevice(string deviceId, long agentInstanceId) { return null; }

        /// <summary>
        /// Read Samples from the database
        /// </summary>
        public List<Sample> ReadSamples(string[] dataItemIds, string deviceId, DateTime from, DateTime to, DateTime at, long count) { return null; }

        #endregion

        #region "Write"

        /// <summary>
        /// Write AgentDefintions to the database
        /// </summary>
        public bool Write(List<AgentDefinitionData> definitions)
        {
            if (!definitions.IsNullOrEmpty())
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
                    return MySqlHelper.ExecuteNonQuery(connectionString, query, null) >= 0;
                }
                catch (MySqlException ex) { logger.Error(ex); }
                catch (Exception ex) { logger.Error(ex); }
            }

            return false;
        }

        /// <summary>
        /// Write ComponentDefintions to the database
        /// </summary>
        public bool Write(List<ComponentDefinitionData> definitions)
        {
            if (!definitions.IsNullOrEmpty())
            {
                string COLUMNS = "`device_id`,`agent_instance_id`, `id`, `uuid`, `name`, `native_name`, `sample_interval`, `sample_rate`, `type`,`parent_id`";
                string QUERY_FORMAT = "INSERT IGNORE INTO `components` ({0}) VALUES {1}";
                string VALUE_FORMAT = "('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}')";

                // Build VALUES string
                var v = new string[definitions.Count];
                for (var i = 0; i < definitions.Count; i++)
                {
                    var d = definitions[i];
                    v[i] = string.Format(VALUE_FORMAT, d.DeviceId, d.AgentInstanceId, d.Id, d.Uuid, d.Name, d.NativeName, d.SampleInterval, d.SampleRate, d.Type, d.ParentId);
                }
                string values = string.Join(",", v);

                // Build Query string
                string query = string.Format(QUERY_FORMAT, COLUMNS, values);

                try
                {
                    // Execute Query
                    return MySqlHelper.ExecuteNonQuery(connectionString, query, null) >= 0;
                }
                catch (MySqlException ex) { logger.Error(ex); }
                catch (Exception ex) { logger.Error(ex); }
            }

            return false;
        }

        /// <summary>
        /// Write DeviceDefintions to the database
        /// </summary>
        public bool Write(List<DeviceDefinitionData> definitions)
        {
            if (!definitions.IsNullOrEmpty())
            {
                string COLUMNS = "`device_id`, `agent_instance_id`, `id`, `uuid`, `name`, `native_name`, `sample_interval`, `sample_rate`, `iso_841_class`";
                string QUERY_FORMAT = "INSERT IGNORE INTO `devices` ({0}) VALUES {1}";
                string VALUE_FORMAT = "('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}')";

                // Build VALUES string
                var v = new string[definitions.Count];
                for (var i = 0; i < definitions.Count; i++)
                {
                    var d = definitions[i];
                    v[i] = string.Format(VALUE_FORMAT, d.DeviceId, d.AgentInstanceId, d.Id, d.Uuid, d.Name, d.NativeName, d.SampleInterval, d.SampleRate, d.Iso841Class);
                }
                string values = string.Join(",", v);

                // Build Query string
                string query = string.Format(QUERY_FORMAT, COLUMNS, values);

                try
                {
                    // Execute Query
                    return MySqlHelper.ExecuteNonQuery(connectionString, query, null) >= 0;
                }
                catch (MySqlException ex) { logger.Error(ex); }
                catch (Exception ex) { logger.Error(ex); }
            }

            return false;
        }

        /// <summary>
        /// Write DataItemDefinitions to the database
        /// </summary>
        public bool Write(List<DataItemDefinitionData> definitions)
        {
            if (!definitions.IsNullOrEmpty())
            {
                string COLUMNS = "`device_id`,`agent_instance_id`, `id`, `name`, `category`, `type`, `sub_type`, `statistic`, `units`,`native_units`,`native_scale`,`coordinate_system`,`sample_rate`,`representation`,`significant_digits`,`parent_id`";
                string QUERY_FORMAT = "INSERT IGNORE INTO `data_items` ({0}) VALUES {1}";
                string VALUE_FORMAT = "('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}','{12}','{13}','{14}','{15}')";

                // Build VALUES string
                var v = new string[definitions.Count];
                for (var i = 0; i < definitions.Count; i++)
                {
                    var d = definitions[i];
                    v[i] = string.Format(VALUE_FORMAT, d.DeviceId, d.AgentInstanceId, d.Id, d.Name, d.Category, d.Type, d.SubType, d.Statistic, d.Units, d.NativeUnits, d.NativeScale, d.CoordinateSystem, d.SampleRate, d.Representation, d.SignificantDigits, d.ParentId);
                }
                string values = string.Join(",", v);

                // Build Query string
                string query = string.Format(QUERY_FORMAT, COLUMNS, values);

                try
                {
                    // Execute Query
                    return MySqlHelper.ExecuteNonQuery(connectionString, query, null) >= 0;
                }
                catch (MySqlException ex) { logger.Error(ex); }
                catch (Exception ex) { logger.Error(ex); }
            }

            return false;
        }

        /// <summary>
        /// Write Samples to the database
        /// </summary>
        public bool Write(List<SampleData> samples)
        {
            if (!samples.IsNullOrEmpty())
            {
                string COLUMNS = "`device_id`, `id`, `timestamp`, `agent_instance_id`, `sequence`, `cdata`, `condition`";
                string QUERY_FORMAT_ARCHIVED = "INSERT IGNORE INTO `archived_samples` ({0}) VALUES {1}";
                string QUERY_FORMAT_CURRENT = "INSERT IGNORE INTO `current_samples` ({0}) VALUES {1} ON DUPLICATE KEY UPDATE {2}";
                string VALUE_FORMAT = "('{0}','{1}','{2}','{3}','{4}','{5}','{6}')";
                string UPDATE_FORMAT = "`timestamp`='{0}',`agent_instance_id`='{1}',`sequence`='{2}',`cdata`='{3}',`condition`='{4}'";

                // Build Archived VALUES string
                string values = "";
                var archived = samples.FindAll(o => o.StreamDataType == StreamDataType.ARCHIVED_SAMPLE);
                for (var i = 0; i < archived.Count; i++)
                {
                    var sample = archived[i];

                    values += string.Format(VALUE_FORMAT,
                        sample.DeviceId,
                        sample.Id,
                        sample.Timestamp.ToUnixTime(),
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
                        string currentValues = string.Format(VALUE_FORMAT, sample.DeviceId, sample.Id, sample.Timestamp.ToUnixTime(), sample.AgentInstanceId, sample.Sequence, sample.CDATA, sample.Condition);
                        string currentUpdate = string.Format(UPDATE_FORMAT, sample.Timestamp.ToUnixTime(), sample.AgentInstanceId, sample.Sequence, sample.CDATA, sample.Condition);
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
                    return MySqlHelper.ExecuteNonQuery(connectionString, query, null) > 0;
                }
                catch (MySqlException ex) { logger.Error(ex); }
                catch (Exception ex) { logger.Error(ex); }
            }

            return false;
        }

        #endregion
    }
}
