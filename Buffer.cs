// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace TrakHound.DeviceServer
{
    public class Buffer
    {
        public const string CONTAINER_DEFINITION_CSV = "container_definitions.csv";
        public const string DATA_DEFINITION_CSV = "data_definitions.csv";
        public const string SAMPLES_CSV = "samples.csv";

        public string Directory { get; set; }

        public List<ContainerDefinition> ContainerDefinitions { get; set; }
        public List<DataDefinition> DataDefinitions { get; set; }
        public List<DataSample> DataSamples { get; set; }

        private Thread writeThread;
        private ManualResetEvent writeStop;

        private object _lock = new object();

        public Buffer()
        {
            Init();
        }

        public Buffer(string directory)
        {
            Init();
            Directory = directory; 
        }

        public void Close()
        {
            if (writeStop != null) writeStop.Set();
        }

        private void Init()
        {
            ContainerDefinitions = new List<ContainerDefinition>();
            DataDefinitions = new List<DataDefinition>();
            DataSamples = new List<DataSample>();

            writeStop = new ManualResetEvent(false);
            writeThread = new Thread(new ThreadStart(WriteWorker));
            writeThread.Start();
        }

        public void Add(DataSample sample)
        {
            lock(_lock)
            {
                DataSamples.Add(sample);
            }        
        }

        private void WriteWorker()
        {
            while (!writeStop.WaitOne(2000, true))
            {
                WriteCsv();
            }
        }

        private string GetDirectory()
        {
            if (!string.IsNullOrEmpty(Directory)) return Directory;
            else return AppDomain.CurrentDomain.BaseDirectory;
        }

        public void WriteCsv()
        {
            //lock (_lock) Console.WriteLine(DataSamples.Count);

            // List of Samples that were written to file
            var storedSamples = new List<string>();

            List<DataSample> samples;
            lock (_lock) samples = DataSamples.ToList();
            if (samples != null && samples.Count > 0)
            {
                string dir = GetDirectory();
                System.IO.Directory.CreateDirectory(dir);

                // Get full path of Samples file
                string path = Path.Combine(dir, SAMPLES_CSV);

                // Start Append FileStream
                using (var fileStream = new FileStream(path, FileMode.Append))
                {
                    foreach (var sample in samples)
                    {
                        string s = sample.ToCsv() + Environment.NewLine;
                        var bytes = System.Text.Encoding.ASCII.GetBytes(s);
                        fileStream.Write(bytes, 0, bytes.Length);
                        storedSamples.Add(sample.Uuid);
                    }
                }

                // Remove from List
                lock (_lock)
                {
                    int count = DataSamples.Count;
                    DataSamples.RemoveAll(o => storedSamples.Contains(o.Uuid));
                    //Console.WriteLine(count - DataSamples.Count + " Removed");
                }
            }
        }

        public string Read()
        {
            return null;
        }

    }
}
