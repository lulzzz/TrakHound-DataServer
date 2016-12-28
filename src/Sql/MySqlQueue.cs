// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using MySql.Data.MySqlClient;
using NLog;
using System;
using System.Collections.Generic;

namespace TrakHound.DataServer.Sql
{
    public class MySqlQueue : SqlQueue
    {
        private readonly static DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static Logger log = LogManager.GetCurrentClassLogger();

        private const string COLUMNS = "`device_id`, `id`, `timestamp`, `value_1`, `value_2`";
        private const string QUERY_FORMAT = "INSERT IGNORE INTO `device_samples` ({0}) VALUES {1}";
        private const string VALUE_FORMAT = "('{0}','{1}','{2}','{3}','{4}')";

        private string connectionString;

        private MySqlConnection connection;

        public MySqlQueue(string server, string user, string password, string database)
        {
            string f = "server={0};uid={1};pwd={2};database={3};";
            connectionString = string.Format(f, server, user, password, database);

            log.Info("MySql Database Configuration");
            log.Info("---------------------------");
            log.Info("Server = " + server);
            log.Info("User = " + user);
            log.Info("database = " + database);
            log.Info("---------------------------");
        }

        private bool IsConnected()
        {
            if (connection != null && connection.State == System.Data.ConnectionState.Open)
            {
                try
                {
                    return connection.Ping();
                }
                catch (Exception ex)
                {
                    log.Error(ex);
                }
            }

            return false;
        }

        private void Connect()
        {
            try
            {
                connection = new MySqlConnection();
                connection.ConnectionString = connectionString;
                connection.Open();
            }
            catch (MySqlException ex)
            {
                log.Error(ex);
            }
        }

        private void Disconnect()
        {
            if (connection != null && connection.State != System.Data.ConnectionState.Closed)
            {
                try
                {
                    connection.Close();
                    connection.Dispose();
                }
                catch (MySqlException ex)
                {
                    log.Error(ex);
                }
            }
        }

        public override bool WriteSql(List<DataSample> samples)
        {
            // Build VALUES string
            string values = "";
            for (var i = 0; i < samples.Count; i++)
            {
                var sample = samples[i];

                values += string.Format(VALUE_FORMAT,
                    sample.DeviceId,
                    sample.Id,
                    ToUnixTime(sample.Timestamp),
                    sample.Value1,
                    sample.Value2
                    );

                if (i < samples.Count - 1) values += ",";
            }

            // Build Query
            string query = string.Format(QUERY_FORMAT, COLUMNS, values);

            try
            {
                // Execute Query
                return MySqlHelper.ExecuteNonQuery(connectionString, query, null) > 0;
            }
            catch (MySqlException ex)
            {
                log.Error(ex);
            }

            return false;
        }

        public static long ToUnixTime(DateTime date)
        {
            return Convert.ToInt64((date.ToUniversalTime() - epoch).TotalMilliseconds);
        }

    }
}
