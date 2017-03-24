// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using MySql.Data.MySqlClient;
using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text.RegularExpressions;
using TrakHound.Api.v2;
using TrakHound.Api.v2.Data;
using TrakHound.Api.v2.Streams;
using TrakHound.Api.v2.Streams.Data;

namespace mod_db_mysql
{
    [InheritedExport(typeof(IDatabaseModule))]
    public class Module : IDatabaseModule
    {
        private const string CONNECTION_FORMAT = "server={0};uid={1};pwd={2};database={3};default command timeout=10;";

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

        public void Close() { }

        #region "Read"

        private static T Read<T>(string query)
        {
            if (!string.IsNullOrEmpty(query))
            {
                try
                {
                    using (var reader = MySqlHelper.ExecuteReader(connectionString, query))
                    {
                        reader.Read();
                        return Read<T>(reader);
                    }
                }
                catch (MySqlException ex)
                {
                    logger.Error(ex);
                }
                catch (Exception ex)
                {
                    logger.Error(ex);
                }
            }

            return default(T);
        }

        private static List<T> ReadList<T>(string query)
        {
            if (!string.IsNullOrEmpty(query))
            {
                try
                {
                    var list = new List<T>();

                    using (var reader = MySqlHelper.ExecuteReader(connectionString, query))
                    {
                        while(reader.Read())
                        {
                            list.Add(Read<T>(reader));
                        }
                    }

                    return list;
                }
                catch (MySqlException ex)
                {
                    logger.Error(ex);
                }
                catch (Exception ex)
                {
                    logger.Error(ex);
                }
            }

            return null;
        }

        private static T Read<T>(MySqlDataReader reader)
        {
            var obj = (T)Activator.CreateInstance(typeof(T));

            // Get object's properties
            var properties = typeof(T).GetProperties().ToList();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                var column = reader.GetName(i);
                var value = reader.GetValue(i);

                var property = properties.Find(o => PropertyToColumn(o.Name) == column);
                if (property != null && value != null)
                {
                    object val = default(T);

                    if (property.PropertyType == typeof(string))
                    {
                        string s = value.ToString();
                        if (!string.IsNullOrEmpty(s)) val = s;
                    }
                    else if (property.PropertyType == typeof(DateTime))
                    {
                        long ms = (long)value;
                        val = UnixTimeExtensions.EpochTime.AddMilliseconds(ms);
                    }
                    else
                    {
                        val = Convert.ChangeType(value, property.PropertyType);
                    }

                    property.SetValue(obj, val, null);
                }
            }

            return obj;
        }

        private static string PropertyToColumn(string propertyName)
        {
            if (propertyName != propertyName.ToUpper())
            {
                // Split string by Uppercase characters
                var parts = Regex.Split(propertyName, @"(?<!^)(?=[A-Z])");
                string s = string.Join("_", parts);
                return s.ToLower();
            }
            else return propertyName.ToLower();
        }

        /// <summary>
        /// Read the most current AgentDefintion from the database
        /// </summary>
        public AgentDefinition ReadAgent(string deviceId)
        {
            string qf = "SELECT * FROM `agents` WHERE `device_id` = '{0}' ORDER BY `timestamp` DESC LIMIT 1";
            string query = string.Format(qf, deviceId);

            return Read<AgentDefinition>(query);
        }

        /// <summary>
        /// Read the ComponentDefinitions for the specified Agent Instance Id from the database
        /// </summary>
        public List<ComponentDefinition> ReadComponents(string deviceId, long agentInstanceId)
        {
            string qf = "SELECT * FROM `components` WHERE `device_id` = '{0}' AND `agent_instance_id` = {1}";
            string query = string.Format(qf, deviceId, agentInstanceId);

            return ReadList<ComponentDefinition>(query);
        }
   
        /// <summary>
        /// Read all of the Connections available from the DataServer
        /// </summary>
        public List<ConnectionDefinition> ReadConnections()
        {
            string query = "SELECT * FROM `connections`";

            return ReadList<ConnectionDefinition>(query);
        }

        /// <summary>
        /// Read the most ConnectionDefintion from the database
        /// </summary>
        public ConnectionDefinition ReadConnection(string deviceId)
        {
            string qf = "SELECT * FROM `connections` WHERE `device_id` = '{0}' LIMIT 1";
            string query = string.Format(qf, deviceId);

            return Read<ConnectionDefinition>(query);
        }

        /// <summary>
        /// Read the DataItemDefinitions for the specified Agent Instance Id from the database
        /// </summary>
        public List<DataItemDefinition> ReadDataItems(string deviceId, long agentInstanceId)
        {
            string qf = "SELECT * FROM `data_items` WHERE `device_id` = '{0}' AND `agent_instance_id` = {1}";
            string query = string.Format(qf, deviceId, agentInstanceId);

            return ReadList<DataItemDefinition>(query);
        }

        /// <summary>
        /// Read the DeviceDefintion for the specified Agent Instance Id from the database
        /// </summary>
        public DeviceDefinition ReadDevice(string deviceId, long agentInstanceId)
        {
            string qf = "SELECT * FROM `devices` WHERE `device_id` = '{0}' AND `agent_instance_id` = {1} LIMIT 1";
            string query = string.Format(qf, deviceId, agentInstanceId);

            return Read<DeviceDefinition>(query);
        }

        /// <summary>
        /// Read Samples from the database
        /// </summary>
        public List<Sample> ReadSamples(string[] dataItemIds, string deviceId, DateTime from, DateTime to, DateTime at, long count)
        {
            var samples = new List<Sample>();

            string COLUMNS = "*";
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
            else if (from > DateTime.MinValue)
            {
                queries.Add(string.Format(INSTANCE_FORMAT, deviceId, from.ToUnixTime()));

                string qf = "SELECT {0} FROM `{1}` WHERE {2}`device_id` = '{3}' AND `timestamp` >= '{4}' LIMIT {5}";
                queries.Add(string.Format(qf, COLUMNS, TABLENAME_ARCHIVED, dataItemFilter, deviceId, from.ToUnixTime(), count == 0 ? 1000 : count));
            }
            else if (to > DateTime.MinValue)
            {
                queries.Add(string.Format(INSTANCE_FORMAT, deviceId, to.ToUnixTime()));

                string qf = "SELECT {0} FROM `{1}` WHERE {2}`device_id` = '{3}' AND `timestamp` <= '{4}' LIMIT {5}";
                queries.Add(string.Format(qf, COLUMNS, TABLENAME_ARCHIVED, dataItemFilter, deviceId, to.ToUnixTime(), count == 0 ? 1000 : count));
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

            foreach (var query in queries) samples.AddRange(ReadList<Sample>(query));

            if (!dataItemIds.IsNullOrEmpty()) samples = samples.FindAll(o => dataItemIds.ToList().Exists(x => x == o.Id));

            return samples;
        }

        /// <summary>
        /// Read the Status from the database
        /// </summary>
        public Status ReadStatus(string deviceId)
        {
            string qf = "SELECT * FROM `status` WHERE `device_id` = '{0}' LIMIT 1";
            string query = string.Format(qf, deviceId);

            return Read<Status>(query);
        }

        #endregion

        #region "Write"

        private bool Write(string query)
        {
            if (!string.IsNullOrEmpty(query))
            {
                try
                {
                    // Create a new SqlConnection using the connectionString
                    using (var connection = new MySqlConnection(connectionString))
                    {
                        // Open the connection
                        connection.Open();

                        using (MySqlCommand command = new MySqlCommand(query, connection))
                        {
                            // Execute the query
                            return command.ExecuteNonQuery() >= 0;
                        }
                    }
                }
                catch (NullReferenceException ex) { logger.Debug(ex); }
                catch (MySqlException ex) { logger.Warn(ex); }
                catch (Exception ex) { logger.Error(ex); }
            }

            return false;
        }

        private string EscapeString(string s)
        {
            if (!string.IsNullOrEmpty(s)) return MySqlHelper.EscapeString(s);

            return s;
        }

        /// <summary>
        /// Write ConnectionDefintions to the database
        /// </summary>
        public bool Write(List<ConnectionDefinitionData> definitions)
        {
            if (!definitions.IsNullOrEmpty())
            {
                string COLUMNS = "`device_id`, `address`, `port`, `physical_address`";
                string QUERY_FORMAT = "INSERT INTO `connections` ({0}) VALUES {1} ON DUPLICATE KEY UPDATE `address`=VALUES(`address`), `port`=VALUES(`port`), `physical_address`=VALUES(`physical_address`)";
                string VALUE_FORMAT = "('{0}','{1}',{2},'{3}')";

                // Build VALUES string
                var v = new string[definitions.Count];
                for (var i = 0; i < definitions.Count; i++)
                {
                    var d = definitions[i];
                    v[i] = string.Format(VALUE_FORMAT,
                        EscapeString(d.DeviceId),
                        EscapeString(d.Address), 
                        d.Port,
                        EscapeString(d.PhysicalAddress));
                }
                string values = string.Join(",", v);

                // Build Query string
                string query = string.Format(QUERY_FORMAT, COLUMNS, values);

                return Write(query);
            }

            return false;
        }

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
                    v[i] = string.Format(VALUE_FORMAT,
                        EscapeString(d.DeviceId), 
                        d.InstanceId,
                        EscapeString(d.Sender),
                        EscapeString(d.Version),
                        d.BufferSize,
                        EscapeString(d.TestIndicator), 
                        d.Timestamp.ToUnixTime());
                }
                string values = string.Join(",", v);

                // Build Query string
                string query = string.Format(QUERY_FORMAT, COLUMNS, values);

                return Write(query);
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
                    v[i] = string.Format(VALUE_FORMAT,
                        EscapeString(d.DeviceId), 
                        d.AgentInstanceId,
                        EscapeString(d.Id),
                        EscapeString(d.Uuid),
                        EscapeString(d.Name),
                        EscapeString(d.NativeName),
                        d.SampleInterval,
                        d.SampleRate,
                        EscapeString(d.Type),
                        EscapeString(d.ParentId));
                }
                string values = string.Join(",", v);

                // Build Query string
                string query = string.Format(QUERY_FORMAT, COLUMNS, values);

                return Write(query);
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
                string COLUMNS = "`device_id`, `agent_instance_id`, `id`, `uuid`, `name`, `native_name`, `sample_interval`, `sample_rate`, `iso_841_class`, `manufacturer`, `model`, `serial_number`, `station`, `description`";
                string QUERY_FORMAT = "INSERT IGNORE INTO `devices` ({0}) VALUES {1}";
                string VALUE_FORMAT = "('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}','{12}','{13}')";

                // Build VALUES string
                var v = new string[definitions.Count];
                for (var i = 0; i < definitions.Count; i++)
                {
                    var d = definitions[i];
                    v[i] = string.Format(VALUE_FORMAT,
                        EscapeString(d.DeviceId),
                        d.AgentInstanceId,
                        EscapeString(d.Id),
                        EscapeString(d.Uuid),
                        EscapeString(d.Name),
                        EscapeString(d.NativeName), 
                        d.SampleInterval,
                        d.SampleRate,
                        EscapeString(d.Iso841Class),
                        EscapeString(d.Manufacturer),
                        EscapeString(d.Model),
                        EscapeString(d.SerialNumber),
                        EscapeString(d.Station),
                        EscapeString(d.Description));
                }
                string values = string.Join(",", v);

                // Build Query string
                string query = string.Format(QUERY_FORMAT, COLUMNS, values);

                return Write(query);
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
                    v[i] = string.Format(VALUE_FORMAT,
                        EscapeString(d.DeviceId), 
                        d.AgentInstanceId, 
                        d.Id,
                        EscapeString(d.Name),
                        EscapeString(d.Category),
                        EscapeString(d.Type),
                        EscapeString(d.SubType),
                        EscapeString(d.Statistic),
                        EscapeString(d.Units),
                        EscapeString(d.NativeUnits),
                        EscapeString(d.NativeScale),
                        EscapeString(d.CoordinateSystem),
                        d.SampleRate,
                        EscapeString(d.Representation),
                        d.SignificantDigits,
                        EscapeString(d.ParentId));
                }
                string values = string.Join(",", v);

                // Build Query string
                string query = string.Format(QUERY_FORMAT, COLUMNS, values);

                return Write(query);
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
                string VALUE_FORMAT = "('{0}','{1}',{2},{3},{4},'{5}','{6}')";
                string UPDATE_FORMAT = "`timestamp`={0},`agent_instance_id`={1},`sequence`={2},`cdata`='{3}',`condition`='{4}'";

                // Build Archived VALUES string
                string values = "";
                var archived = samples.FindAll(o => o.StreamDataType == StreamDataType.ARCHIVED_SAMPLE);
                for (var i = 0; i < archived.Count; i++)
                {
                    var sample = archived[i];

                    values += string.Format(VALUE_FORMAT,
                        EscapeString(sample.DeviceId),
                        EscapeString(sample.Id),
                        sample.Timestamp.ToUnixTime(),
                        sample.AgentInstanceId,
                        sample.Sequence,
                        EscapeString(sample.CDATA),
                        EscapeString(sample.Condition)
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
                        string currentValues = string.Format(VALUE_FORMAT,
                            EscapeString(sample.DeviceId),
                            EscapeString(sample.Id), 
                            sample.Timestamp.ToUnixTime(), 
                            sample.AgentInstanceId, 
                            sample.Sequence,
                            EscapeString(sample.CDATA),
                            EscapeString(sample.Condition));

                        string currentUpdate = string.Format(UPDATE_FORMAT, 
                            sample.Timestamp.ToUnixTime(), 
                            sample.AgentInstanceId, 
                            sample.Sequence,
                            EscapeString(sample.CDATA),
                            EscapeString(sample.Condition));

                        currentQueries.Add(string.Format(QUERY_FORMAT_CURRENT, COLUMNS, currentValues, currentUpdate));
                    }
                }

                var queries = new List<string>();

                // Add Archived Query
                if (archived.Count > 0) queries.Add(string.Format(QUERY_FORMAT_ARCHIVED, COLUMNS, values));

                // Add Current Queries
                queries.AddRange(currentQueries);

                string query = string.Join(";", queries);

                return Write(query);
            }

            return false;
        }

        /// <summary>
        /// Write StatusData to the database
        /// </summary>
        public bool Write(List<StatusData> definitions)
        {
            if (!definitions.IsNullOrEmpty())
            {
                string COLUMNS = "`device_id`, `timestamp`, `connected`, `available`";
                string QUERY_FORMAT = "INSERT INTO `status` ({0}) VALUES {1} ON DUPLICATE KEY UPDATE `timestamp`=VALUES(`timestamp`), `connected`=VALUES(`connected`), `available`=VALUES(`available`)";
                string VALUE_FORMAT = "('{0}',{1},{2},{3})";

                // Build VALUES string
                var v = new string[definitions.Count];
                for (var i = 0; i < definitions.Count; i++)
                {
                    var d = definitions[i];
                    v[i] = string.Format(VALUE_FORMAT,
                        EscapeString(d.DeviceId),
                        d.Timestamp.ToUnixTime(), 
                        d.Connected ? 1 : 0,
                        d.Available ? 1 : 0);
                }
                string values = string.Join(",", v);

                // Build Query string
                string query = string.Format(QUERY_FORMAT, COLUMNS, values);

                return Write(query);
            }

            return false;
        }

        #endregion
    }
}
