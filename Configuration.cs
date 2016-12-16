// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace TrakHound.DeviceServer
{
    [XmlRoot("DeviceServer")]
    public class Configuration
    {
        [XmlArray("Devices")]
        [XmlArrayItem("Device", typeof(Device))]
        public List<Device> Devices { get; set; }

        [XmlArray("DataServers")]
        [XmlArrayItem("DataServer")]
        public List<DataServer> DataServers { get; set; }

        public Configuration()
        {
            Devices = new List<Device>();
            DataServers = new List<DataServer>();
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
