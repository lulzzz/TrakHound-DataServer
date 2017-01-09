// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using MySql.Data.MySqlClient;
using NLog;
using System;
using System.Collections.Generic;
using TrakHound.Api.v2.Data;

namespace TrakHound.DataServer.Sql
{
    public partial class MySql
    {
        public static class Components
        {
            private static Logger log = LogManager.GetCurrentClassLogger();

            public static List<ComponentDefinition> Get(string deviceId, long agentInstanceId)
            {
                var config = (Sql.MySql.MySqlConfiguration)Configuration.Current.Database;

                // Create connection string
                string cf = "server={0};uid={1};pwd={2};database={3};";
                string c = string.Format(cf, config.Server, config.User, config.Password, config.Database);

                string qf = "SELECT * FROM `components` WHERE `device_id` = '{0}' AND `agent_instance_id` = '{1}'";
                string query = string.Format(qf, deviceId, agentInstanceId);

                if (!string.IsNullOrEmpty(query))
                {
                    try
                    {
                        using (var reader = MySqlHelper.ExecuteReader(c, query, null))
                        {
                            var l = new List<ComponentDefinition>();

                            while (reader.Read())
                            {
                                l.Add(Read<ComponentDefinition>(reader));

                                //// Create new DataItemDefinition
                                //var obj = new ComponentDefinition();
                                //obj.DeviceId = reader.GetString("device_id");
                                //obj.Id = reader.GetString("id");
                                //obj.Uuid = reader.GetString("uuid");
                                //obj.ParentId = reader.GetString("parent_id");
                                //obj.AgentInstanceId = reader.GetString("agent_instance_id");
                                //obj.Type = reader.GetString("type");
                                //obj.NativeName = reader.GetString("native_name");
                                //obj.SampleInterval = reader.GetString("sample_interval");
                                //obj.SampleRate = reader.GetString("sample_rate");

                                //l.Add(obj);
                            }

                            return l;
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
