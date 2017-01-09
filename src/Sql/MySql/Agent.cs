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
        public static class Agent
        {
            private static Logger log = LogManager.GetCurrentClassLogger();

            public static AgentDefinition Get(string deviceId)
            {
                var config = (Sql.MySql.MySqlConfiguration)Configuration.Current.Database;

                // Create connection string
                string cf = "server={0};uid={1};pwd={2};database={3};";
                string c = string.Format(cf, config.Server, config.User, config.Password, config.Database);

                string qf = "SELECT * FROM `agents` WHERE `device_id` = '{0}' ORDER BY `timestamp` DESC LIMIT 1";
                string query = string.Format(qf, deviceId);

                if (!string.IsNullOrEmpty(query))
                {
                    try
                    {
                        using (var reader = MySqlHelper.ExecuteReader(c, query, null))
                        {
                            reader.Read();

                            // Create new AgentDefinition
                            //var obj = new AgentDefinition();
                            //obj.DeviceId = reader.GetString("device_id");
                            //obj.InstanceId = reader.GetInt64("instance_id");
                            //obj.Sender = reader.GetString("sender");
                            //obj.Version = reader.GetString("version");
                            //obj.BufferSize = reader.GetInt32("buffer_size");
                            //obj.TestIndicator = reader.GetString("test_indicator");

                            //return obj;

                            return Read<AgentDefinition>(reader);
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
