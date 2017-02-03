// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System.Xml.Serialization;

namespace TrakHound.DataServer
{
    public class StreamingConfiguration
    {
        [XmlAttribute("port")]
        public int Port { get; set; }

        [XmlAttribute("clientTimeout")]
        public int ClientTimeout { get; set; }

        [XmlAttribute("authenticationUrl")]
        public string AuthenticationUrl { get; set; }


        public StreamingConfiguration()
        {
            Port = 8472;
            ClientTimeout = 30000; // 30 Seconds
        }
    }
}
