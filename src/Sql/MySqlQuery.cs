using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MySql.Data.MySqlClient;

namespace TrakHound.DataServer.Sql
{
    public class MySqlQuery
    {
        private const string CONNECTION = "server={0};uid={1};pwd={2};database={3};";

        public bool ExecuteNonQuery(string query)
        {
            //MySqlHelper.ExecuteNonQuery(connectionString, query, null);

            return false;
        }

        public MySqlDataReader Execute(string query, MySqlConfiguration config)
        {
            // Create connection string
            string c = string.Format(CONNECTION, config.Server, config.User, config.Password, config.Database);

            return MySqlHelper.ExecuteReader(c, query, null);
        }
    }
}
