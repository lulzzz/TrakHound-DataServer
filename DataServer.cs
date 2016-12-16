// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using System.IO;

namespace TrakHound.DeviceServer
{
    public class DataServer
    {
        [XmlArray("DataTypes")]
        [XmlArrayItem("DataType", typeof(DataTypes))]
        public List<DataTypes> DataTypes { get; set; }

        private string _url;
        [XmlAttribute("url")]
        public string Url
        {
            get { return _url; }
            set
            {
                _url = value;
                SetBufferDirectory();
            }
        }

        private string _bufferPath;
        [XmlAttribute("bufferPath")]
        public string BufferPath
        {
            get { return _bufferPath; }
            set
            {
                _bufferPath = value;
                SetBufferDirectory();
            }
        }

        private void SetBufferDirectory()
        {
            string dir = "";

            if (!string.IsNullOrEmpty(BufferPath)) dir = BufferPath;

            if (!string.IsNullOrEmpty(Url))
            {
                if (string.IsNullOrEmpty(dir)) dir = ConvertToFileName(Url);
                else dir = Path.Combine(BufferPath, ConvertToFileName(Url));
            }

            if (buffer != null) buffer.Directory = dir;
            else buffer = new Buffer(dir);
        }

        private Buffer buffer;

        public DataServer()
        {
            buffer = new Buffer();
        }

        public void Add(List<DataSample> samples)
        {
            foreach (var sample in samples)
            {
                var type = DataType.Get(sample.Type);
                if (DataTypes.Exists(o => o == type))
                {
                    buffer.Add(sample);
                }
            }
        }

        public void SendDefinitions(List<ContainerDefinition> definitions)
        {
            //foreach (var definition in definitions)
            //{
            //    if (DataTypes.Exists(o => o.ToString().ToLower() == DataType.Get(definition))
            //    {
            //        Console.WriteLine(Url + " : " + definition.Id);
            //    } 
            //}
        }

        public void SendDefinitions(List<DataDefinition> definitions)
        {
            //foreach (var definition in definitions)
            //{
            //    var type = DataType.Get(definition.Type);
            //    if (DataTypes.Exists(o => o == type))
            //    {
            //        Console.WriteLine(Url + " : " + definition.Type + " : " + definition.Id);
            //    }
            //}
        }

        public void SendSamples(List<DataSample> samples)
        {
            //foreach (var sample in samples)
            //{
            //    var type = DataType.Get(sample.Type);
            //    if (DataTypes.Exists(o => o == type))
            //    {
            //        Console.WriteLine(Url + " : " + sample.Id + " = " + sample.Value1 + " : " + sample.Value2);
            //    }
            //}


            //foreach (var sample in samples) Console.WriteLine(Url + " : " + sample.Id + " = " + sample.Value1 + " : " + sample.Value2);
        }


        private static string ConvertToFileName(string url)
        {
            List<string> urlParts = new List<string>();
            string rt = "";
            var r = new Regex(@"[a-z]+", RegexOptions.IgnoreCase);
            foreach (Match m in r.Matches(url))
            {
                urlParts.Add(m.Value);
            }
            int c = urlParts.Count;
            for (int i = 0; i < c; i++)
            {
                rt = rt + urlParts[i];
                if (i < c - 1) rt = rt + "_";
            }
            return rt;
        }
    }
}
