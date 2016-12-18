// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Serialization;
using CsvHelper;

namespace TrakHound.DeviceServer
{
    public class Buffer
    {
        public const string FILENAME_CONTAINER_DEFINITION = "container_definitions";
        public const string FILENAME_DATA_DEFINITION = "data_definitions";
        public const string FILENAME_SAMPLES = "samples";

        private const int BUFFER_FILE_PADDING = 100;

        [XmlText]
        public string Directory { get; set; }

        [XmlAttribute("maxFileSize")]
        public long MaxFileSize { get; set; }

        public string Url { get; set; }

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

        private void Init()
        {
            MaxFileSize = 1048576 * 100; // 100 MB

            ContainerDefinitions = new List<ContainerDefinition>();
            DataDefinitions = new List<DataDefinition>();
            DataSamples = new List<DataSample>();

            writeStop = new ManualResetEvent(false);
            writeThread = new Thread(new ThreadStart(WriteWorker));
            writeThread.Start();
        }

        public void Close()
        {
            if (writeStop != null) writeStop.Set();
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
            string dir = AppDomain.CurrentDomain.BaseDirectory;
            if (!string.IsNullOrEmpty(Directory)) dir = Directory;

            if (!string.IsNullOrEmpty(Url)) dir = Path.Combine(dir, ConvertToFileName(Url));

            return dir;
        }

        public void WriteCsv()
        {
            // List of Samples that were written to file
            var storedSamples = new List<string>();

            List<DataSample> samples;
            lock (_lock) samples = DataSamples.ToList();
            if (samples != null && samples.Count > 0)
            {
                try
                {
                    do
                    {
                        string path = GetSamplesPath();

                        // Start Append FileStream
                        using (var fileStream = new FileStream(path, FileMode.Append))
                        {
                            foreach (var sample in samples)
                            {
                                string s = sample.ToCsv() + Environment.NewLine;
                                var bytes = System.Text.Encoding.ASCII.GetBytes(s);
                                fileStream.Write(bytes, 0, bytes.Length);
                                storedSamples.Add(sample.Uuid);

                                // Check file size limit
                                if (fileStream.Length >= (MaxFileSize - BUFFER_FILE_PADDING)) break;
                            }
                        }
                    } while (storedSamples.Count < samples.Count);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Buffer.WriteCsv() : " + ex.Message);
                }
               
                // Remove from List
                lock (_lock)
                {
                    int count = DataSamples.Count;
                    DataSamples.RemoveAll(o => storedSamples.Contains(o.Uuid));
                }
            }
        }

        private string GetSamplesPath()
        {
            // Get the Parent Directory
            string dir = GetDirectory();
            System.IO.Directory.CreateDirectory(dir);

            string filename = Path.ChangeExtension(FILENAME_SAMPLES, "csv");
            string path = Path.Combine(dir, filename);

            // Increment Filename until Size is ok
            int i = 1;
            while (!IsFileOk(path))
            {
                filename = Path.ChangeExtension(FILENAME_SAMPLES + "_" + i, "csv");
                path = Path.Combine(dir, filename);
                i++;
            }

            return path;
        }

        private bool IsFileOk(string path)
        {
            if (!File.Exists(path)) return true;
            else
            {
                try
                {
                    var fileInfo = new FileInfo(path);
                    if (fileInfo != null)
                    {
                        return fileInfo.Length < (MaxFileSize - BUFFER_FILE_PADDING);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            return false;
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


        public List<DataSample> ReadSamples(int maxRecords)
        {
            int recordsRead = 0;
            int records = 0;

            var sampleBuffers = System.IO.Directory.GetFiles(GetDirectory(), "samples*");
            if (sampleBuffers != null)
            {
                foreach (var sampleBuffer in sampleBuffers)
                {
                    var samples = ReadSamples(sampleBuffer, 5000);
                }
            }

            return null;
        }

        private List<DataSample> ReadSamples(string path, int maxRecords)
        {
            if (File.Exists(path))
            {
                int readRecords = 0;
                bool overwrite = false;

                var tempFile = Path.GetTempFileName();

                try
                {
                    using (var f = new FileStream(path, FileMode.Open, FileAccess.ReadWrite))
                    using (var reader = new StreamReader(f))
                    {
                        // Read records from file
                        while (!reader.EndOfStream && readRecords < maxRecords)
                        {
                            var line = reader.ReadLine();
                            readRecords++;

                            var sample = DataSample.FromCsv(line);
                            if (sample != null)
                            {
                                //Console.WriteLine(sample.ToCsv());
                            }
                        }

                        Console.WriteLine("readRecords = " + readRecords + " : maxRecords = " + maxRecords);

                        // Write unread records back to file
                        if (!reader.EndOfStream)
                        {
                            using (var writer = new StreamWriter(tempFile))
                            {
                                writer.Write(reader.ReadToEnd());
                            }

                            overwrite = true;
                        }
                    }

                    File.Delete(path);
                    if (overwrite) File.Move(tempFile, path);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Buffer.ReadSamples() : " + ex.Message);
                }
            }

            return null;
        }

    }
}
