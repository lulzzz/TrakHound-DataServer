// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace TrakHound.DataServer
{
    /// <summary>
    /// Streaming server that accepts multiple client connections and writes Samples to a database
    /// </summary>
    class DataServer
    {
        private TcpListener listener = null;
        private ManualResetEvent stop;

        internal static Sql.SqlQueue SqlQueue;

        internal static Configuration Configuration;

        /// <summary>
        /// Flag whether SSL is used for client connections. Read Only.
        /// </summary>
        public bool UseSSL { get { return _sslCertificate != null; } }

        private X509Certificate2 _sslCertificate;
        /// <summary>
        /// The SSL Certificate to use for client connections
        /// </summary>
        public X509Certificate2 SslCertificate { get { return _sslCertificate; } }

        /// <summary>
        /// List of Allowed Endpoint IP Addresses. If specified, only addresses in this list will be allowed to connect.
        /// </summary>
        public List<IPAddress> AllowedEndPoints { get; set; }

        /// <summary>
        /// Connection Timeout in Milliseconds
        /// </summary>
        public int Timeout { get; set; }

        public DataServer()
        {
            Timeout = 30000; // 30 Seconds

            PrintHeader();
            
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "server.conf");
            var config = Configuration.Get(configPath);
            if (config != null)
            {
                Configuration = config;

                Log.Write("Configuration file read from '" + configPath + "'", this);
                Log.Write("---------------------------", this);

                // Get SSL Certificate (if set)
                if (!string.IsNullOrEmpty(config.SslCertificatePath))
                {
                    string path = config.SslCertificatePath;

                    if (File.Exists(path))
                    {
                        X509Certificate2 cert = null;

                        // Se the Certificate Password
                        if (!string.IsNullOrEmpty(config.SslCertificatePassword)) cert = new X509Certificate2(path, config.SslCertificatePassword);
                        else cert = new X509Certificate2(path);

                        if (cert != null)
                        {
                            PrintCertificateInfo(cert);
                            _sslCertificate = cert;
                        }
                    }
                }

                // Set Client Connection Timeout
                Timeout = config.ClientConnectionTimeout;

                // Load Allowed EndPoints
                if (config.EndPoints != null && config.EndPoints.Count > 0)
                {
                    AllowedEndPoints = new List<IPAddress>();

                    foreach (var endPoint in config.EndPoints)
                    {
                        IPAddress ip;
                        if (IPAddress.TryParse(endPoint, out ip)) AllowedEndPoints.Add(ip);
                    }
                }

                // Create SqlQueue
                if (config.Database != null)
                {
                    // Create MySqlQueue
                    if (config.Database.GetType() == typeof(Sql.MySqlConfiguration))
                    {
                        var sqlConfig = (Sql.MySqlConfiguration)config.Database;
                        SqlQueue = new Sql.MySqlQueue(sqlConfig.Server, sqlConfig.User, sqlConfig.Password, sqlConfig.Database);
                    }
                }
            }
        }

        public void Start()
        {
            stop = new ManualResetEvent(false);

            try
            {
                do
                {
                    ListenForConnections();

                } while (!stop.WaitOne(0, true));
            }
            catch (Exception ex)
            {
                Log.Write(ex.Message, this);
            }
        }

        private void ListenForConnections()
        {
            try
            {
                // Set the TCP port to use
                int port = UseSSL ? 443 : 80;

                // Create new TcpListener
                listener = new TcpListener(IPAddress.Any, port);

                // Start listening for client requests.
                listener.Start();

                do
                {
                    Log.Write("Waiting for a connection on port " + port + "... ", this);

                    // Perform a blocking call to accept requests.
                    var client = listener.AcceptTcpClient();

                    bool allowed = false;

                    // Check if address is allowed
                    if (AllowedEndPoints != null && AllowedEndPoints.Count > 0)
                    {
                        var ipEndpoint = client.Client.LocalEndPoint as IPEndPoint;
                        if (ipEndpoint != null)
                        {
                            allowed = AllowedEndPoints.Exists(o => o.Equals(ipEndpoint.Address));
                        }
                    }
                    else allowed = true;

                    if (!allowed)
                    {
                        Log.Write("Blocked from " + client.Client.LocalEndPoint.ToString(), this);
                        if (client != null) client.Close();
                    }
                    else
                    {
                        Log.Write("Connected to " + client.Client.LocalEndPoint.ToString(), this);

                        // Start Processing Data on separate thread and continue to listen for subsequent requests
                        ThreadPool.QueueUserWorkItem((x) =>
                        {
                            // Start new StreamConnection for client connection
                            var stream = new StreamConnection(ref client, _sslCertificate);
                            stream.Timeout = Timeout;
                            stream.Start();
                        });
                    }

                } while (!stop.WaitOne(0, true));
            }
            catch(Exception ex)
            {
                Log.Write(ex.Message, this);
            }
        }

        private void PrintHeader()
        {
            Log.Write("---------------------------", this);
            Log.Write("TrakHound DataServer : v" + Assembly.GetExecutingAssembly().GetName().Version.ToString(), this);
            Log.Write(@"Copyright 2017 TrakHound Inc., All Rights Reserved", this);
            Log.Write("---------------------------", this);
        }

        private void PrintCertificateInfo(X509Certificate2 cert)
        {
            Log.Write("SSL Certificate Information", this);
            Log.Write("---------------------------", this);
            Log.Write("Common Name : " + cert.GetNameInfo(X509NameType.SimpleName, false), this);
            Log.Write("Subject : " + cert.Subject, this);
            Log.Write("Serial Number : " + cert.SerialNumber, this);
            Log.Write("Format : " + cert.GetFormat(), this);
            Log.Write("Effective Date : " + cert.GetEffectiveDateString(), this);
            Log.Write("Expiration Date : " + cert.GetExpirationDateString(), this);
            Log.Write("---------------------------", this);
        }

    }
}
