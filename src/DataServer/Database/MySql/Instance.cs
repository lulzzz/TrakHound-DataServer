using MySql.Data.MySqlClient;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TrakHound.Api.v2.Streams.Data;
using TrakHound.Api.v2;

namespace TrakHound.DataServer.Sql
{
    public partial class MySql
    {
        public List<SampleData> GetInstance(string deviceId, DateTime timestamp)
        {
            var l = new List<SampleData>();

            // Build Query string
            string query = string.Format("CALL getInstance({0}, {1})", deviceId, timestamp.ToUnixTime());

            try
            {
                // Execute Query
                if (MySqlHelper.ExecuteNonQuery(connectionString, query, null) >= 0)
                {
                    // Return the EntryIds of the items that were written successfully
                    return l.Select(o => o.EntryId).ToList();
                }
            }
            catch (MySqlException ex)
            {
                log.Error(ex);
            }

            return l;
        }
    }
}
