// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using MySql.Data.MySqlClient;
using NLog;
using System;
using TrakHound.Api.v2.Data;

namespace TrakHound.DataServer.Sql
{
    public partial class MySql
    {
        public static class Device
        {
            private static Logger log = LogManager.GetCurrentClassLogger();

            public static DeviceDefinition Get(string deviceId, long agentInstanceId)
            {
                var config = (Sql.MySql.MySqlConfiguration)Configuration.Current.Database;

                // Create connection string
                string cf = "server={0};uid={1};pwd={2};database={3};";
                string c = string.Format(cf, config.Server, config.User, config.Password, config.Database);

                string qf = "SELECT * FROM `devices` WHERE `device_id` = '{0}' AND `agent_instance_id` = '{1}' LIMIT 1";
                string query = string.Format(qf, deviceId, agentInstanceId);

                if (!string.IsNullOrEmpty(query))
                {
                    try
                    {
                        using (var reader = MySqlHelper.ExecuteReader(c, query, null))
                        {
                            reader.Read();
                            return Read<DeviceDefinition>(reader);
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
}
