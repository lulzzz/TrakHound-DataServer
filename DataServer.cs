// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System.Collections.Generic;
using System.Xml.Serialization;
using System.IO;
using System.Threading;

namespace TrakHound.Squirrel
{
    public class DataServer
    {
        private ManualResetEvent sendStop;
        private Thread sendThread;

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

                if (Buffer != null) Buffer.Url = _url;
            }
        }

        [XmlAttribute("sendInterval")]
        public int SendInterval { get; set; }

        private Buffer _buffer;
        [XmlElement("Buffer")]
        public Buffer Buffer
        {
            get { return _buffer; }
            set
            {
                _buffer = value;
                if (_buffer != null) _buffer.Url = Url;
            }
        }

        public DataServer()
        {
            SendInterval = 5000;
        }

        public void Start()
        {
            sendStop = new ManualResetEvent(false);

            sendThread = new Thread(new ThreadStart(SendWorker));
            sendThread.Start();
        }

        public void Stop()
        {
            if (sendStop != null) sendStop.Set();
        }

        public void Add(List<DataSample> samples)
        {
            foreach (var sample in samples)
            {
                var type = DataType.Get(sample.Type);
                if (DataTypes.Exists(o => o == type))
                {
                    if (Buffer != null) Buffer.Add(sample);
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

        private void SendWorker()
        {
            do
            {
                Buffer.ReadSamples(1000);

            } while (!sendStop.WaitOne(SendInterval, true));
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


        
    }
}
