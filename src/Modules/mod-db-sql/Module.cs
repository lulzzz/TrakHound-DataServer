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
using TrakHound.Api.v2.Streams;
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

            if (!dataItemIds.IsNullOrEmpty()) samples = samples.FindAll(o => dataItemIds.ToList().Exists(x => x == o.Id));

            return samples;
        }

        /// <summary>
        /// Read the Status from the database
        /// </summary>
        public Status ReadStatus(string deviceId)
        {
            string qf = "SELECT TOP 1 * FROM [status] WHERE [device_id] = '{0}'";
            string query = string.Format(qf, deviceId);

            return Read<Status>(query);
        }

        #endregion

        #region "Write"

        private bool Write(SqlCommand command)
        {
            try
            {
                // Create a new SqlConnection using the connectionString
                using (var connection = new SqlConnection(connectionString))
                {
                    // Open the connection
                    connection.Open();
                    command.Connection = connection;
                    return command.ExecuteNonQuery() >= 0;
                }
            }
            catch (SqlException ex) { logger.Warn(ex); }
            catch (Exception ex) { logger.Error(ex); }

            return false;
        }


        /// <summary>
        /// Write ConnectionDefintions to the database
        /// </summary>
        public bool Write(List<ConnectionDefinitionData> definitions)
        {
            if (!definitions.IsNullOrEmpty())
            {
                string COLUMNS = "[device_id], [address], [port], [physical_address]";
                string VALUES = "(@deviceId, @address, @port, @physicalAddress)";
                string UPDATE = "[address]=@address, [port]=@port, [physical_address]=@physicalAddress";
                string WHERE = "[device_id]=@deviceId";

                string QUERY_FORMAT = "IF NOT EXISTS(SELECT * FROM [connections] WHERE {0}) BEGIN {1} END ELSE BEGIN {2} END";
                string INSERT_FORMAT = "INSERT INTO [connections] ({0}) VALUES {1}";
                string UPDATE_FORMAT = "UPDATE [connections] SET {0} WHERE {1}";

                string insert = string.Format(INSERT_FORMAT, COLUMNS, VALUES);
                string update = string.Format(UPDATE_FORMAT, UPDATE, WHERE);
                string query = string.Format(QUERY_FORMAT, WHERE, insert, update);

                bool success = false;

                for (var i = 0; i < definitions.Count; i++)
                {
                    var d = definitions[i];

                    using (var command = new SqlCommand(query))
                    {
                        command.Parameters.AddWithValue("@deviceId", d.DeviceId ?? Convert.DBNull);
                        command.Parameters.AddWithValue("@address", d.Address ?? Convert.DBNull);
                        command.Parameters.AddWithValue("@port", d.Port);
                        command.Parameters.AddWithValue("@physicalAddress", d.PhysicalAddress ?? Convert.DBNull);

                        success = Write(command);
                    }

                    if (!success) break;
                }

                return success;
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
                string COLUMNS = "[device_id], [instance_id], [sender], [version], [buffer_size], [test_indicator], [timestamp]";
                string VALUES = "(@deviceId, @instanceId, @sender, @version, @bufferSize, @testIndicator, @timestamp)";
                string UPDATE = "[instance_id]=@instanceId, [sender]=@sender, [version]=@version, [buffer_size]=@bufferSize, [test_indicator]=@testIndicator, [timestamp]=@timestamp";
                string WHERE = "[device_id]=@deviceId";

                string QUERY_FORMAT = "IF NOT EXISTS(SELECT * FROM [agents] WHERE {0}) BEGIN {1} END ELSE BEGIN {2} END";
                string INSERT_FORMAT = "INSERT INTO [agents] ({0}) VALUES {1}";
                string UPDATE_FORMAT = "UPDATE [agents] SET {0} WHERE {1}";

                var insert = string.Format(INSERT_FORMAT, COLUMNS, VALUES);
                var update = string.Format(UPDATE_FORMAT, UPDATE, WHERE);
                var query = string.Format(QUERY_FORMAT, WHERE, insert, update);

                bool success = false;

                for (var i = 0; i < definitions.Count; i++)
                {
                    var d = definitions[i];

                    using (var command = new SqlCommand(query))
                    {
                        command.Parameters.AddWithValue("@deviceId", d.DeviceId ?? Convert.DBNull);
                        command.Parameters.AddWithValue("@instanceId", d.InstanceId);
                        command.Parameters.AddWithValue("@sender", d.Sender ?? Convert.DBNull);
                        command.Parameters.AddWithValue("@version", d.Version ?? Convert.DBNull);
                        command.Parameters.AddWithValue("@bufferSize", d.BufferSize);
                        command.Parameters.AddWithValue("@testIndicator", d.TestIndicator ?? Convert.DBNull);
                        command.Parameters.AddWithValue("@timestamp", d.Timestamp.ToUnixTime());

                        success = Write(command);
                    }

                    if (!success) break;
                }

                return success;
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
                string COLUMNS = "[device_id], [agent_instance_id], [id], [uuid], [name], [native_name], [sample_interval], [sample_rate], [type], [parent_id]";
                string VALUES = "(@deviceId, @agentInstanceId, @id, @uuid, @name, @nativeName, @sampleInterval, @sampleRate, @type, @parentId)";
                string UPDATE = "[uuid]=@uuid, [name]=@name, [native_name]=@nativeName, [sample_interval]=@sampleInterval, [sample_rate]=@sampleRate, [type]=@type, [parent_id]=@parentId";
                string WHERE = "[device_id]=@deviceId AND [agent_instance_id]=@agentInstanceId AND [id]=@id";

                string QUERY_FORMAT = "IF NOT EXISTS(SELECT * FROM [components] WHERE {0}) BEGIN {1} END ELSE BEGIN {2} END";
                string INSERT_FORMAT = "INSERT INTO [components] ({0}) VALUES {1}";
                string UPDATE_FORMAT = "UPDATE [components] SET {0} WHERE {1}";

                string insert = string.Format(INSERT_FORMAT, COLUMNS, VALUES);
                string update = string.Format(UPDATE_FORMAT, UPDATE, WHERE);
                string query = string.Format(QUERY_FORMAT, WHERE, insert, update);

                bool success = false;

                for (var i = 0; i < definitions.Count; i++)
                {
                    var d = definitions[i];

                    using (var command = new SqlCommand(query))
                    {
                        command.Parameters.AddWithValue("@deviceId", d.DeviceId ?? Convert.DBNull);
                        command.Parameters.AddWithValue("@agentInstanceId", d.AgentInstanceId);
                        command.Parameters.AddWithValue("@id", d.Id ?? Convert.DBNull);
                        command.Parameters.AddWithValue("@uuid", d.Uuid ?? Convert.DBNull);
                        command.Parameters.AddWithValue("@name", d.Name ?? Convert.DBNull);
                        command.Parameters.AddWithValue("@nativeName", d.NativeName ?? Convert.DBNull);
                        command.Parameters.AddWithValue("@sampleInterval", d.SampleInterval);
                        command.Parameters.AddWithValue("@sampleRate", d.SampleRate);
                        command.Parameters.AddWithValue("@type", d.Type ?? Convert.DBNull);
                        command.Parameters.AddWithValue("@parentId", d.ParentId ?? Convert.DBNull);

                        success = Write(command);
                    }

                    if (!success) break;
                }

                return success;
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
                string COLUMNS = "[device_id], [agent_instance_id], [id], [uuid], [name], [native_name], [sample_interval], [sample_rate], [iso_841_class], [manufacturer], [model], [serial_number], [station], [description]";
                string VALUES = "(@deviceId, @agentInstanceId, @id, @uuid, @name, @nativeName, @sampleInterval, @sampleRate, @iso841Class, @manufacturer, @model, @serialNumber, @station, @description)";
                string UPDATE = "[uuid]=@uuid, [name]=@name, [native_name]=@nativeName, [sample_interval]=@sampleInterval, [sample_rate]=@sampleRate, [iso_841_class]=@iso841Class, [manufacturer]=@manufacturer, [model]=@model, [serial_number]=@serialNumber, [station]=@station, [description]=@description";
                string WHERE = "[device_id]=@deviceId AND [agent_instance_id]=@agentInstanceId AND [id]=@id";

                string QUERY_FORMAT = "IF NOT EXISTS(SELECT * FROM [devices] WHERE {0}) BEGIN {1} END ELSE BEGIN {2} END";
                string INSERT_FORMAT = "INSERT INTO [devices] ({0}) VALUES {1}";
                string UPDATE_FORMAT = "UPDATE [devices] SET {0} WHERE {1}";

                string insert = string.Format(INSERT_FORMAT, COLUMNS, VALUES);
                string update = string.Format(UPDATE_FORMAT, UPDATE, WHERE);
                string query = string.Format(QUERY_FORMAT, WHERE, insert, update);

                bool success = false;

                for (var i = 0; i < definitions.Count; i++)
                {
                    var d = definitions[i];

                    using (var command = new SqlCommand(query))
                    {
                        command.Parameters.AddWithValue("@deviceId", d.DeviceId ?? Convert.DBNull);
                        command.Parameters.AddWithValue("@agentInstanceId", d.AgentInstanceId);
                        command.Parameters.AddWithValue("@id", d.Id ?? Convert.DBNull);
                        command.Parameters.AddWithValue("@uuid", d.Uuid ?? Convert.DBNull);
                        command.Parameters.AddWithValue("@name", d.Name ?? Convert.DBNull);
                        command.Parameters.AddWithValue("@nativeName", d.NativeName ?? Convert.DBNull);
                        command.Parameters.AddWithValue("@sampleInterval", d.SampleInterval);
                        command.Parameters.AddWithValue("@sampleRate", d.SampleRate);
                        command.Parameters.AddWithValue("@iso841Class", d.Iso841Class ?? Convert.DBNull);
                        command.Parameters.AddWithValue("@manufacturer", d.Manufacturer ?? Convert.DBNull);
                        command.Parameters.AddWithValue("@model", d.Model ?? Convert.DBNull);
                        command.Parameters.AddWithValue("@serialNumber", d.SerialNumber ?? Convert.DBNull);
                        command.Parameters.AddWithValue("@station", d.Station ?? Convert.DBNull);
                        command.Parameters.AddWithValue("@description", d.Description ?? Convert.DBNull);

                        success = Write(command);
                    }

                    if (!success) break;
                }

                return success;
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
                string COLUMNS = "[device_id], [agent_instance_id], [id], [name], [category], [type], [sub_type], [statistic], [units], [native_units], [native_scale], [coordinate_system], [sample_rate], [representation], [significant_digits], [parent_id]";
                string VALUES = "(@deviceId, @agentInstanceId, @id, @name, @category, @type, @subType, @statistic, @units, @nativeUnits, @nativeScale, @coordinateSystem, @sampleRate, @representation, @significantDigits, @parentId)";
                string UPDATE = "[name]=@name, [category]=@category, [type]=@type, [sub_type]=@subType, [statistic]=@statistic, [units]=@units, [native_units]=@nativeUnits, [native_scale]=@nativeScale, [coordinate_system]=@coordinateSystem, [sample_rate]=@sampleRate, [representation]=@representation, [significant_digits]=@significantDigits, [parent_id]=@parentId";
                string WHERE = "[device_id]=@deviceId AND [agent_instance_id]=@agentInstanceId AND [id]=@id";

                string QUERY_FORMAT = "IF NOT EXISTS(SELECT * FROM [data_items] WHERE {0}) BEGIN {1} END ELSE BEGIN {2} END";
                string INSERT_FORMAT = "INSERT INTO [data_items] ({0}) VALUES {1}";
                string UPDATE_FORMAT = "UPDATE [data_items] SET {0} WHERE {1}";

                string insert = string.Format(INSERT_FORMAT, COLUMNS, VALUES);
                string update = string.Format(UPDATE_FORMAT, UPDATE, WHERE);
                string query = string.Format(QUERY_FORMAT, WHERE, insert, update);

                bool success = false;

                for (var i = 0; i < definitions.Count; i++)
                {
                    var d = definitions[i];

                    using (var command = new SqlCommand(query))
                    {
                        command.Parameters.AddWithValue("@deviceId", d.DeviceId ?? Convert.DBNull);
                        command.Parameters.AddWithValue("@agentInstanceId", d.AgentInstanceId);
                        command.Parameters.AddWithValue("@id", d.Id ?? Convert.DBNull);
                        command.Parameters.AddWithValue("@name", d.Name ?? Convert.DBNull);
                        command.Parameters.AddWithValue("@category", d.Category ?? Convert.DBNull);
                        command.Parameters.AddWithValue("@type", d.Type ?? Convert.DBNull);
                        command.Parameters.AddWithValue("@subType", d.SubType ?? Convert.DBNull);
                        command.Parameters.AddWithValue("@statistic", d.Statistic ?? Convert.DBNull);
                        command.Parameters.AddWithValue("@units", d.Units ?? Convert.DBNull);
                        command.Parameters.AddWithValue("@nativeUnits", d.NativeUnits ?? Convert.DBNull);
                        command.Parameters.AddWithValue("@nativeScale", d.NativeScale ?? Convert.DBNull);
                        command.Parameters.AddWithValue("@coordinateSystem", d.CoordinateSystem ?? Convert.DBNull);
                        command.Parameters.AddWithValue("@sampleRate", d.SampleRate);
                        command.Parameters.AddWithValue("@representation", d.Representation ?? Convert.DBNull);
                        command.Parameters.AddWithValue("@significantDigits", d.SignificantDigits);
                        command.Parameters.AddWithValue("@parentId", d.ParentId ?? Convert.DBNull);

                        success = Write(command);
                    }

                    if (!success) break;
                }

                return success;
            }
            
            return false;
        }

        /// <summary>
        /// Write Samples to the database
        /// </summary>
        public bool Write(List<SampleData> samples)
        {
            var queries = new List<string>();

            bool success = true;

            success = WriteArchivedSamples(samples);
            if (success) success = WriteCurrentSamples(samples);

            return success;
        }

        private bool WriteArchivedSamples(List<SampleData> samples)
        {
            if (!samples.IsNullOrEmpty())
            {
                string COLUMNS = "[device_id], [id], [timestamp], [agent_instance_id], [sequence], [cdata], [condition]";
                string VALUES = "(@deviceId, @id, @timestamp, @agentInstanceId, @sequence, @cdata, @condition)";
                string WHERE = "[device_id]=@deviceId AND [id]=@id AND [timestamp]=@timestamp";

                string QUERY_FORMAT = "IF NOT EXISTS(SELECT * FROM [archived_samples] WHERE {0}) BEGIN {1} END";
                string INSERT_FORMAT = "INSERT INTO [archived_samples] ({0}) VALUES {1}";

                string insert = string.Format(INSERT_FORMAT, COLUMNS, VALUES);
                string query = string.Format(QUERY_FORMAT, WHERE, insert);

                bool success = true;

                var archived = samples.FindAll(o => o.StreamDataType == StreamDataType.ARCHIVED_SAMPLE);
                for (var i = 0; i < archived.Count; i++)
                {
                    var s = archived[i];

                    using (var command = new SqlCommand(query))
                    {
                        command.Parameters.AddWithValue("@deviceId", s.DeviceId ?? Convert.DBNull);
                        command.Parameters.AddWithValue("@id", s.Id ?? Convert.DBNull);
                        command.Parameters.AddWithValue("@timestamp", s.Timestamp.ToUnixTime());
                        command.Parameters.AddWithValue("@agentInstanceId", s.AgentInstanceId);
                        command.Parameters.AddWithValue("@sequence", s.Sequence);
                        command.Parameters.AddWithValue("@cdata", s.CDATA ?? Convert.DBNull);
                        command.Parameters.AddWithValue("@condition", s.Condition ?? Convert.DBNull);

                        success = Write(command);
                    }

                    if (!success) break;
                }

                return success;
            }
            else
            {
                return true;
            }
        }

        private bool WriteCurrentSamples(List<SampleData> samples)
        {
            if (!samples.IsNullOrEmpty())
            {
                string COLUMNS = "[device_id], [id], [timestamp], [agent_instance_id], [sequence], [cdata], [condition]";
                string VALUES = "(@deviceId, @id, @timestamp, @agentInstanceId, @sequence, @cdata, @condition)";
                string UPDATE = "[timestamp]=@timestamp, [agent_instance_id]=@agentInstanceId, [sequence]=@sequence, [cdata]=@cdata, [condition]=@condition";
                string WHERE = "[device_id]=@deviceId AND [id]=@id";

                string QUERY_FORMAT = "IF NOT EXISTS(SELECT * FROM [current_samples] WHERE {0}) BEGIN {1} END ELSE BEGIN {2} END";
                string INSERT_FORMAT = "INSERT INTO [current_samples] ({0}) VALUES {1}";
                string UPDATE_FORMAT = "UPDATE [current_samples] SET {0} WHERE {1}";

                string insert = string.Format(INSERT_FORMAT, COLUMNS, VALUES);
                string update = string.Format(UPDATE_FORMAT, UPDATE, WHERE);
                string query = string.Format(QUERY_FORMAT, WHERE, insert, update);

                bool success = true;

                var ids = samples.Select(o => o.Id).Distinct();
                foreach (var id in ids)
                {
                    var sample = samples.OrderBy(o => o.Timestamp).ToList().First(o => o.Id == id);
                    if (sample != null)
                    {
                        using (var command = new SqlCommand(query))
                        {
                            command.Parameters.AddWithValue("@deviceId", sample.DeviceId ?? Convert.DBNull);
                            command.Parameters.AddWithValue("@id", sample.Id ?? Convert.DBNull);
                            command.Parameters.AddWithValue("@timestamp", sample.Timestamp.ToUnixTime());
                            command.Parameters.AddWithValue("@agentInstanceId", sample.AgentInstanceId);
                            command.Parameters.AddWithValue("@sequence", sample.Sequence);
                            command.Parameters.AddWithValue("@cdata", sample.CDATA ?? Convert.DBNull);
                            command.Parameters.AddWithValue("@condition", sample.Condition ?? Convert.DBNull);

                            success = Write(command);
                        }
                    }

                    if (!success) break;
                }

                return success;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Write StatusData to the database
        /// </summary>
        public bool Write(List<StatusData> statuses)
        {
            if (!statuses.IsNullOrEmpty())
            {
                string COLUMNS = "[device_id], [timestamp], [connected], [available]";
                string VALUES = "(@deviceId, @timestamp, @connected, @available)";
                string UPDATE = "[timestamp]=@timestamp, [connected]=@connected, [available]=@available";
                string WHERE = "[device_id]=@deviceId";

                string QUERY_FORMAT = "IF NOT EXISTS(SELECT * FROM [status] WHERE {0}) BEGIN {1} END ELSE BEGIN {2} END";
                string INSERT_FORMAT = "INSERT INTO [status] ({0}) VALUES {1}";
                string UPDATE_FORMAT = "UPDATE [status] SET {0} WHERE {1}";

                string insert = string.Format(INSERT_FORMAT, COLUMNS, VALUES);
                string update = string.Format(UPDATE_FORMAT, UPDATE, WHERE);
                string query = string.Format(QUERY_FORMAT, WHERE, insert, update);

                bool success = false;

                for (var i = 0; i < statuses.Count; i++)
                {
                    var s = statuses[i];

                    using (var command = new SqlCommand(query))
                    {
                        command.Parameters.AddWithValue("@deviceId", s.DeviceId ?? Convert.DBNull);
                        command.Parameters.AddWithValue("@timestamp", s.Timestamp.ToUnixTime());
                        command.Parameters.AddWithValue("@connected", s.Connected ? 1 : 0);
                        command.Parameters.AddWithValue("@available", s.Available ? 1 : 0);

                        success = Write(command);
                    }

                    if (!success) break;
                }

                return success;
            }

            return false;
        }

        #endregion
    }
}
