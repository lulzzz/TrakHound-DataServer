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
        public static class DataItems
        {
            private static Logger log = LogManager.GetCurrentClassLogger();

            public static List<DataItemDefinition> Get(string deviceId, long agentInstanceId)
            {
                var config = (MySqlConfiguration)Configuration.Current.Database;

                // Create connection string
                string cf = "server={0};uid={1};pwd={2};database={3};";
                string c = string.Format(cf, config.Server, config.User, config.Password, config.Database);

                string qf = "SELECT * FROM `data_items` WHERE `device_id` = '{0}' AND `agent_instance_id` = '{1}'";
                string query = string.Format(qf, deviceId, agentInstanceId);

                if (!string.IsNullOrEmpty(query))
                {
                    try
                    {
                        using (var reader = MySqlHelper.ExecuteReader(c, query, null))
                        {
                            var l = new List<DataItemDefinition>();

                            while (reader.Read())
                            {
                                l.Add(Read<DataItemDefinition>(reader));


                                // Create new DataItemDefinition
                                //var obj = new DataItemDefinition();
                                //obj.DeviceId = reader.GetString("device_id");
                                //obj.Id = reader.GetString("id");
                                //obj.Type = reader.GetString("type");
                                //obj.SubType = reader.GetString("sub_type");
                                //obj.ParentId = reader.GetString("parent_id");
                                //obj.AgentInstanceId = reader.GetString("agent_instance_id");
                                //obj.Category = reader.GetString("category");
                                //obj.CoordinateSystem = reader.GetString("coordinate_system");
                                //obj.NativeScale = reader.GetString("native_scale");
                                //obj.NativeUnits = reader.GetString("native_units");
                                //obj.Representation = reader.GetString("representation");
                                //obj.SampleRate = reader.GetString("sample_rate");
                                //obj.SignificantDigits = reader.GetString("significant_digits");
                                //obj.Statistic = reader.GetString("statistic");
                                //obj.SubType = reader.GetString("sub_type");
                                //obj.Units = reader.GetString("units");
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
