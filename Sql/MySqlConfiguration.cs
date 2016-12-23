// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System.Xml;
using System.Xml.Serialization;

namespace TrakHound.DataServer.Sql
{
    public class MySqlConfiguration
    {
        [XmlAttribute("server")]
        public string Server { get; set; }

        [XmlAttribute("user")]
        public string User { get; set; }

        [XmlAttribute("password")]
        public string Password { get; set; }

        [XmlAttribute("database")]
        public string Database { get; set; }
    }
}
