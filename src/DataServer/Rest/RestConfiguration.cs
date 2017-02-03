// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System.Collections.Generic;
using System.Xml.Serialization;

namespace TrakHound.DataServer
{
    public class RestConfiguration
    {
        [XmlAttribute("port")]
        public int Port { get; set; }

        [XmlArray("Prefixes")]
        [XmlArrayItem("Prefix")]
        public List<string> Prefixes { get; set; }


        public RestConfiguration()
        {
            Port = 80;
        }
    }
}
