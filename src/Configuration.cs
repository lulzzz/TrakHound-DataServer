// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace TrakHound.DataServer
{
    [XmlRoot("DataServer")]
    public class Configuration
    {
        [XmlElement("SslCertificatePath")]
        public string SslCertificatePath { get; set; }

        [XmlElement("SslCertificatePassword")]
        public string SslCertificatePassword { get; set; }

        [XmlElement("ClientConnectionTimeout")]
        public int ClientConnectionTimeout { get; set; }

        [XmlArray("EndPoints")]
        [XmlArrayItem("EndPoint")]
        public List<string> EndPoints { get; set; }

        [XmlElement("MySql", typeof(Sql.MySqlConfiguration))]
        public object Database { get; set; }

        public Configuration()
        {
            ClientConnectionTimeout = 30000; // 30 Seconds
        }

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
                        return (Configuration)serializer.Deserialize(xmlReader);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }           

            return null;
        }
    }
}
