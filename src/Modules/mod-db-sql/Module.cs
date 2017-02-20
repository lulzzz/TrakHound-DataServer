// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using TrakHound.Api.v2;
using TrakHound.Api.v2.Data;
using TrakHound.Api.v2.Streams.Data;

namespace mod_db_sql
{
    [InheritedExport(typeof(IDatabaseModule))]
    public class Module : IDatabaseModule
    {
        private const string CONNECTION_FORMAT = "Data Source={0};User ID={1};Password={2};Initial Catalog={3}";

        private static Logger logger = LogManager.GetCurrentClassLogger();
        private static string connectionString;

        private Configuration configuration;

        /// <summary>
        /// Gets the name of the Database. This corresponds to the node name in the 'server.config' file
        /// </summary>
        public string Name { get { return "Sql"; } }


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
                    // Create a new SqlConnection using the connectionString
                    using (var connection = new SqlConnection(connectionString))
                    {
                        // Open the connection
                        connection.Open();

                        using (var command = new SqlCommand(query, connection))
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            reader.Read();
                            return Read<T>(reader);
                        }
                    }
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

                    // Create a new SqlConnection using the connectionString
                    using (var connection = new SqlConnection(connectionString))
                    {
                        // Open the connection
                        connection.Open();

                        using (var command = new SqlCommand(query, connection))
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                list.Add(Read<T>(reader));
                            }
                        }
                    }

                    return list;
                }
                catch (Exception ex)
                {
                    logger.Error(ex);
                }
            }

            return null;
        }

        private static T Read<T>(SqlDataReader reader)
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
            string qf = "SELECT TOP 1 * FROM [agents] WHERE [device_id] = '{0}' ORDER BY [timestamp] DESC";
            string query = string.Format(qf, deviceId);

            return Read<AgentDefinition>(query);
        }

        /// <summary>
        /// Read the ComponentDefinitions for the specified Agent Instance Id from the database
        /// </summary>
        public List<ComponentDefinition> ReadComponents(string deviceId, long agentInstanceId)
        {
            string qf = "SELECT * FROM [components] WHERE [device_id] = '{0}' AND [agent_instance_id] = {1}";
            string query = string.Format(qf, deviceId, agentInstanceId);

            return ReadList<ComponentDefinition>(query);
        }

        /// <summary>
        /// Read all of the Connections available from the DataServer
        /// </summary>
        public List<ConnectionDefinition> ReadConnections()
        {
            string query = "SELECT * FROM [connections]";

            return ReadList<ConnectionDefinition>(query);
        }

        /// <summary>
        /// Read the most ConnectionDefintion from the database
        /// </summary>
        public ConnectionDefinition ReadConnection(string deviceId)
        {
            string qf = "SELECT TOP 1 * FROM [connections] WHERE [device_id] = '{0}'";
            string query = string.Format(qf, deviceId);

            return Read<ConnectionDefinition>(query);
        }

        /// <summary>
        /// Read the DataItemDefinitions for the specified Agent Instance Id from the database
        /// </summary>
        public List<DataItemDefinition> ReadDataItems(string deviceId, long agentInstanceId)
        {
            string qf = "SELECT * FROM [data_items] WHERE [device_id] = '{0}' AND [agent_instance_id] = {1}";
            string query = string.Format(qf, deviceId, agentInstanceId);

            return ReadList<DataItemDefinition>(query);
        }

        /// <summary>
        /// Read the DeviceDefintion for the specified Agent Instance Id from the database
        /// </summary>
        public DeviceDefinition ReadDevice(string deviceId, long agentInstanceId)
        {
            string qf = "SELECT TOP 1 * FROM [devices] WHERE [device_id] = '{0}' AND [agent_instance_id]={1}";
            string query = string.Format(qf, deviceId, agentInstanceId);

            return Read<DeviceDefinition>(query);
        }

        /// <summary>
        /// Read Samples from the database
        /// </summary>
        public List<Sample> ReadSamples(string[] dataItemIds, string deviceId, DateTime from, DateTime to, DateTime at, long count)
        {
            var samples = new List<Sample>();

            //string COLUMNS = "[device_id],[id],[timestamp],[sequence],[cdata],[condition]";
            string COLUMNS = "*";
            string TABLENAME_ARCHIVED = "archived_samples";
            string TABLENAME_CURRENT = "current_samples";
            string INSTANCE_FORMAT = "CALL getInstance('{0}', {1})";

            string dataItemFilter = "";

            if (dataItemIds != null && dataItemIds.Length > 0)
            {
                for (int i = 0; i < dataItemIds.Length; i++)
                {
                    dataItemFilter += "[id]='" + dataItemIds[i] + "'";
                    if (i < dataItemIds.Length - 1) dataItemFilter += " OR ";
                }

                dataItemFilter = string.Format("({0}) AND ", dataItemFilter);
            }

            var queries = new List<string>();

            // Create query
            if (from > DateTime.MinValue && to > DateTime.MinValue)
            {
                queries.Add(string.Format(INSTANCE_FORMAT, deviceId, from.ToUnixTime()));

                string qf = "SELECT {0} FROM [{1}] WHERE {2}[device_id] = '{3}' AND [timestamp] >= '{4}' AND [timestamp] <= '{5}'";
                queries.Add(string.Format(qf, COLUMNS, TABLENAME_ARCHIVED, dataItemFilter, deviceId, from.ToUnixTime(), to.ToUnixTime()));
            }
            else if (from > DateTime.MinValue && count > 0)
            {
                queries.Add(string.Format(INSTANCE_FORMAT, deviceId, from.ToUnixTime()));

                string qf = "SELECT TOP {5} {0} FROM [{1}] WHERE {2}[device_id] = '{3}' AND [timestamp] >= '{4}'";
                queries.Add(string.Format(qf, COLUMNS, TABLENAME_ARCHIVED, dataItemFilter, deviceId, from.ToUnixTime(), count));
            }
            else if (to > DateTime.MinValue && count > 0)
            {
                queries.Add(string.Format(INSTANCE_FORMAT, deviceId, to.ToUnixTime()));

                string qf = "SELECT TOP {5} {0} FROM [{1}] WHERE {2}[device_id] = '{3}' AND [timestamp] <= '{4}'";
                queries.Add(string.Format(qf, COLUMNS, TABLENAME_ARCHIVED, dataItemFilter, deviceId, to.ToUnixTime(), count));
            }
            else if (from > DateTime.MinValue)
            {
                queries.Add(string.Format(INSTANCE_FORMAT, deviceId, from.ToUnixTime()));

                string qf = "SELECT TOP 1000 {0} FROM [{1}] WHERE {2}[device_id] = '{3}' AND [timestamp] <= '{4}'";
                queries.Add(string.Format(qf, COLUMNS, TABLENAME_ARCHIVED, dataItemFilter, deviceId, from.ToUnixTime()));
            }
            else if (to > DateTime.MinValue)
            {
                queries.Add(string.Format(INSTANCE_FORMAT, deviceId, to.ToUnixTime()));

                string qf = "SELECT TOP 1000 {0} FROM [{1}] WHERE {2}[device_id] = '{3}' AND [timestamp] <= '{4}'";
                queries.Add(string.Format(qf, COLUMNS, TABLENAME_ARCHIVED, dataItemFilter, deviceId, to.ToUnixTime()));
            }
            else if (count > 0)
            {
                string qf = "SELECT TOP {4} {0} FROM [{1}] WHERE {2}[device_id] = '{3}' ORDER BY [timestamp] DESC";
                queries.Add(string.Format(qf, COLUMNS, TABLENAME_ARCHIVED, dataItemFilter, deviceId, count));
            }
            else if (at > DateTime.MinValue)
            {
                queries.Add(string.Format(INSTANCE_FORMAT, deviceId, at.ToUnixTime()));
            }
            else
            {
                string qf = "SELECT {0} FROM [{1}] WHERE {2}[device_id] = '{3}'";
                queries.Add(string.Format(qf, COLUMNS, TABLENAME_CURRENT, dataItemFilter, deviceId, at.ToUnixTime()));
            }

            foreach (var query in queries) samples.AddRange(ReadList<Sample>(query));

            return samples;
        }

        #endregion

        #region "Write"

        private bool Write(string query)
        {
            try
            {
                // Create a new SqlConnection using the connectionString
                using (var connection = new SqlConnection(connectionString))
                {
                    // Open the connection
                    connection.Open();

                    using (var command = new SqlCommand(query, connection))
                    {
                        return command.ExecuteNonQuery() >= 0;
                    }           
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }

            return false;
        }

        /// <summary>
        /// Write ConnectionDefintions to the database
        /// </summary>
        public bool Write(List<ConnectionDefinitionData> definitions)
        {
            if (!definitions.IsNullOrEmpty())
            {
                string query = "";

                string COLUMNS = "[device_id], [address], [port], [physical_address]";

                string QUERY_FORMAT = "IF NOT EXISTS(SELECT * FROM [connections] WHERE {0}) BEGIN {1} END ELSE BEGIN {2} END";
                string WHERE_FORMAT = "[device_id]='{0}'";
                string INSERT_FORMAT = "INSERT INTO [connections] ({0}) VALUES {1}";
                string VALUE_FORMAT = "('{0}','{1}',{2},'{3}')";
                string UPDATE_FORMAT = "UPDATE [connections] SET [address]='{0}', [port]={1}, [physical_address]='{2}'";

                // Build VALUES string
                var v = new string[definitions.Count];
                for (var i = 0; i < definitions.Count; i++)
                {
                    var d = definitions[i];

                    var where = string.Format(WHERE_FORMAT, d.DeviceId);
                    var values = string.Format(VALUE_FORMAT, d.DeviceId, d.Address, d.Port, d.PhysicalAddress);
                    var insert = string.Format(INSERT_FORMAT, COLUMNS, values);
                    var update = string.Format(UPDATE_FORMAT, d.Address, d.Port, d.PhysicalAddress);

                    // Build Query string
                    query += string.Format(QUERY_FORMAT, where, insert, update) + Environment.NewLine;
                }

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
                string query = "";

                string COLUMNS = "[device_id], [instance_id], [sender], [version], [buffer_size], [test_indicator], [timestamp]";

                string QUERY_FORMAT = "IF NOT EXISTS(SELECT * FROM [agents] WHERE {0}) BEGIN {1} END ELSE BEGIN {2} END";
                string WHERE_FORMAT = "[device_id]='{0}' AND [instance_id]={1}";
                string INSERT_FORMAT = "INSERT INTO [agents] ({0}) VALUES {1}";
                string VALUE_FORMAT = "('{0}',{1},'{2}','{3}','{4}','{5}',{6})";
                string UPDATE_FORMAT = "UPDATE [agents] SET [sender]='{0}', [version]='{1}', [buffer_size]='{2}', [test_indicator]='{3}', [timestamp]={4}";

                // Build VALUES string
                var v = new string[definitions.Count];
                for (var i = 0; i < definitions.Count; i++)
                {
                    var d = definitions[i];

                    var where = string.Format(WHERE_FORMAT, d.DeviceId, d.InstanceId);
                    var values = string.Format(VALUE_FORMAT, d.DeviceId, d.InstanceId, d.Sender, d.Version, d.BufferSize, d.TestIndicator, d.Timestamp.ToUnixTime());
                    var insert = string.Format(INSERT_FORMAT, COLUMNS, values);
                    var update = string.Format(UPDATE_FORMAT, d.Sender, d.BufferSize, d.BufferSize, d.TestIndicator, d.Timestamp.ToUnixTime());

                    // Build Query string
                    query += string.Format(QUERY_FORMAT, where, insert, update) + Environment.NewLine;
                }

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
                string query = "";

                string COLUMNS = "[device_id], [agent_instance_id], [id], [uuid], [name], [native_name], [sample_interval], [sample_rate], [type], [parent_id]";

                string QUERY_FORMAT = "IF NOT EXISTS(SELECT * FROM [components] WHERE {0}) BEGIN {1} END ELSE BEGIN {2} END";
                string WHERE_FORMAT = "[device_id]='{0}' AND [agent_instance_id]={1} AND [id]='{2}'";
                string INSERT_FORMAT = "INSERT INTO [components] ({0}) VALUES {1}";
                string VALUE_FORMAT = "('{0}',{1},'{2}','{3}','{4}','{5}',{6},{7},'{8}','{9}')";
                string UPDATE_FORMAT = "UPDATE [components] SET [uuid]='{0}', [name]='{1}', [native_name]='{2}', [sample_interval]={3}, [sample_rate]={4}, [type]='{5}', [parent_id]='{6}' WHERE {7}";

                for (var i = 0; i < definitions.Count; i++)
                {
                    var d = definitions[i];

                    var where = string.Format(WHERE_FORMAT, d.DeviceId, d.AgentInstanceId, d.Id);
                    var values = string.Format(VALUE_FORMAT, d.DeviceId, d.AgentInstanceId, d.Id, d.Uuid, d.Name, d.NativeName, d.SampleInterval, d.SampleRate, d.Type, d.ParentId);
                    var insert = string.Format(INSERT_FORMAT, COLUMNS, values);
                    var update = string.Format(UPDATE_FORMAT, d.Uuid, d.Name, d.NativeName, d.SampleInterval, d.SampleRate, d.Type, d.ParentId, where);

                    // Build Query string
                    query += string.Format(QUERY_FORMAT, where, insert, update) + Environment.NewLine;
                }

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
                string query = "";

                string COLUMNS = "[device_id], [agent_instance_id], [id], [uuid], [name], [native_name], [sample_interval], [sample_rate], [iso_841_class]";

                string QUERY_FORMAT = "IF NOT EXISTS(SELECT * FROM [devices] WHERE {0}) BEGIN {1} END ELSE BEGIN {2} END";
                string WHERE_FORMAT = "[device_id]='{0}' AND [agent_instance_id]={1} AND [id]='{2}'";
                string INSERT_FORMAT = "INSERT INTO [devices] ({0}) VALUES {1}";
                string VALUE_FORMAT = "('{0}',{1},'{2}','{3}','{4}','{5}',{6},{7},'{8}')";
                string UPDATE_FORMAT = "UPDATE [devices] SET [uuid]='{0}', [name]='{1}', [native_name]='{2}', [sample_interval]={3}, [sample_rate]={4}, [iso_841_class]='{5}' WHERE {6}";

                // Build VALUES string
                var v = new string[definitions.Count];
                for (var i = 0; i < definitions.Count; i++)
                {
                    var d = definitions[i];

                    var where = string.Format(WHERE_FORMAT, d.DeviceId, d.AgentInstanceId, d.Id);
                    var values = string.Format(VALUE_FORMAT, d.DeviceId, d.AgentInstanceId, d.Id, d.Uuid, d.Name, d.NativeName, d.SampleInterval, d.SampleRate, d.Iso841Class);
                    var insert = string.Format(INSERT_FORMAT, COLUMNS, values);
                    var update = string.Format(UPDATE_FORMAT, d.Uuid, d.Name, d.NativeName, d.SampleInterval, d.SampleRate, d.Iso841Class, where);

                    // Build Query string
                    query += string.Format(QUERY_FORMAT, where, insert, update) + Environment.NewLine;
                }

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
                string query = "";

                string COLUMNS = "[device_id], [agent_instance_id], [id], [name], [category], [type], [sub_type], [statistic], [units], [native_units], [native_scale], [coordinate_system], [sample_rate], [representation], [significant_digits], [parent_id]";

                string QUERY_FORMAT = "IF NOT EXISTS(SELECT * FROM [data_items] WHERE {0}) BEGIN {1} END ELSE BEGIN {2} END";
                string WHERE_FORMAT = "[device_id]='{0}' AND [agent_instance_id]={1} AND [id]='{2}'";
                string INSERT_FORMAT = "INSERT INTO [data_items] ({0}) VALUES {1}";
                string VALUE_FORMAT = "('{0}',{1},'{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}',{12},'{13}',{14},'{15}')";
                string UPDATE_FORMAT = "UPDATE [data_items] SET [name]='{0}', [category]='{1}', [type]='{2}', [sub_type]='{3}', [statistic]='{4}', [units]='{5}', [native_units]='{6}', [native_scale]='{7}', [coordinate_system]='{8}', [sample_rate]={9}, [representation]='{10}', [significant_digits]={11}, [parent_id]='{12}' WHERE {13}";

                // Build VALUES string
                var v = new string[definitions.Count];
                for (var i = 0; i < definitions.Count; i++)
                {
                    var d = definitions[i];

                    var where = string.Format(WHERE_FORMAT, d.DeviceId, d.AgentInstanceId, d.Id);
                    var values = string.Format(VALUE_FORMAT, d.DeviceId, d.AgentInstanceId, d.Id, d.Name, d.Category, d.Type, d.SubType, d.Statistic, d.Units, d.NativeUnits, d.NativeScale, d.CoordinateSystem, d.SampleRate, d.Representation, d.SignificantDigits, d.ParentId);
                    var insert = string.Format(INSERT_FORMAT, COLUMNS, values);
                    var update = string.Format(UPDATE_FORMAT, d.Name, d.Category, d.Type, d.SubType, d.Statistic, d.Units, d.NativeUnits, d.NativeScale, d.CoordinateSystem, d.SampleRate, d.Representation, d.SignificantDigits, d.ParentId, where);

                    // Build Query string
                    query += string.Format(QUERY_FORMAT, where, insert, update) + Environment.NewLine;
                }

                return Write(query);
            }
            
            return false;
        }

        /// <summary>
        /// Write Samples to the database
        /// </summary>
        public bool Write(List<SampleData> samples)
        {
            var queries = new List<string>();

            queries.AddRange(CreateArchivedSamplesQuery(samples));
            queries.AddRange(CreateCurrentSamplesQuery(samples));

            if (!queries.IsNullOrEmpty()) {

                return Write(string.Join(";", queries));
            }

            return false;
        }

        private List<string> CreateArchivedSamplesQuery(List<SampleData> samples)
        {
            var queries = new List<string>();

            if (!samples.IsNullOrEmpty())
            {
                string COLUMNS = "[device_id], [id], [timestamp], [agent_instance_id], [sequence], [cdata], [condition]";

                string QUERY_FORMAT = "IF NOT EXISTS(SELECT * FROM [archived_samples] WHERE {0}) BEGIN {1} END";
                string WHERE_FORMAT = "[device_id]='{0}' AND [id]='{1}' AND [timestamp]={2}";
                string INSERT_FORMAT = "INSERT INTO [archived_samples] ({0}) VALUES {1}";
                string VALUE_FORMAT = "('{0}','{1}',{2},{3},{4},'{5}','{6}')";

                var v = new string[samples.Count];
                for (var i = 0; i < samples.Count; i++)
                {
                    var s = samples[i];

                    var where = string.Format(WHERE_FORMAT, s.DeviceId, s.Id, s.Timestamp.ToUnixTime());
                    var values = string.Format(VALUE_FORMAT, s.DeviceId, s.Id, s.Timestamp.ToUnixTime(), s.AgentInstanceId, s.Sequence, s.CDATA, s.Condition);
                    var insert = string.Format(INSERT_FORMAT, COLUMNS, values);

                    queries.Add(string.Format(QUERY_FORMAT, where, insert));
                }
            }

            return queries;
        }

        private List<string> CreateCurrentSamplesQuery(List<SampleData> samples)
        {
            var queries = new List<string>();

            if (!samples.IsNullOrEmpty())
            {
                string COLUMNS = "[device_id], [id], [timestamp], [agent_instance_id], [sequence], [cdata], [condition]";

                string QUERY_FORMAT = "IF NOT EXISTS(SELECT * FROM [current_samples] WHERE {0}) BEGIN {1} END ELSE BEGIN {2} END";
                string WHERE_FORMAT = "[device_id]='{0}' AND [id]='{1}'";
                string INSERT_FORMAT = "INSERT INTO [current_samples] ({0}) VALUES {1}";
                string VALUE_FORMAT = "('{0}','{1}',{2},{3},{4},'{5}','{6}')";
                string UPDATE_FORMAT = "UPDATE [current_samples] SET [timestamp]={0}, [agent_instance_id]={1}, [sequence]={2}, [cdata]='{3}', [condition]='{4}' WHERE {5}";

                var ids = samples.Select(o => o.Id).Distinct();
                foreach (var id in ids)
                {
                    var sample = samples.OrderBy(o => o.Timestamp).ToList().First(o => o.Id == id);
                    if (sample != null)
                    {
                        string where = string.Format(WHERE_FORMAT, sample.DeviceId, sample.Id);
                        string values = string.Format(VALUE_FORMAT, sample.DeviceId, sample.Id, sample.Timestamp.ToUnixTime(), sample.AgentInstanceId, sample.Sequence, sample.CDATA, sample.Condition);
                        string insert = string.Format(INSERT_FORMAT, COLUMNS, values);
                        string update = string.Format(UPDATE_FORMAT, sample.Timestamp.ToUnixTime(), sample.AgentInstanceId, sample.Sequence, sample.CDATA, sample.Condition, where);
                        queries.Add(string.Format(QUERY_FORMAT, where, insert, update));
                    }
                }
            }

            return queries;
        }

        #endregion
    }
}
