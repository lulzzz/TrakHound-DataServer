// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System.Xml.Serialization;

namespace TrakHound.DataServer
{
    public class EndPointRange
    {
        [XmlArray("Allow")]
        [XmlArrayItem("EndPoint")]
        public string[] Allowed { get; set; }

        [XmlArray("Deny")]
        [XmlArrayItem("EndPoint")]
        public string[] Denied { get; set; }
    }
}
