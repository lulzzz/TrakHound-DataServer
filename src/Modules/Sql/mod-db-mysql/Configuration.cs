// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using NLog;
using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace mod_db_mysql
{
    [XmlRoot("MySql")]
    public class Configuration
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        [XmlElement("Server")]
        public string Server { get; set; }

        [XmlElement("User")]
        public string User { get; set; }

        [XmlElement("Password")]
        public string Password { get; set; }

        [XmlElement("Database")]
        public string Database { get; set; }


        public static Configuration Get(string path)
        {
            if (File.Exists(path))
            {
                try
                {
                    var serializer = new XmlSerializer(typeof(Configuration));
                    using (var fileReader = new FileStream(path, FileMode.Open))
                    using (var xmlReader = XmlReader.Create(fileReader))
                    {
                        var config = (Configuration)serializer.Deserialize(xmlReader);

                        return config;
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex);
                }
            }

            return null;
        }
    }
}
