// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using TrakHound.Api.v2;
using TrakHound.Api.v2.Devices;
using TrakHound.Api.v2.Streams;

namespace TrakHound.DataServer.Streaming
{
    /// <summary>
    /// Streaming server that accepts multiple client connections and writes Samples to a database
    /// </summary>
    class StreamingServer
    {
        private const int AUTHENTICATION_INTERVAL = 10000;

        private static Logger log = LogManager.GetCurrentClassLogger();
        private static object _lock = new object();

        private TcpListener listener = null;
        private Thread thread;
        private ManualResetEvent stop;

        internal static DatabaseQueue Queue = new DatabaseQueue();

        internal static Configuration Configuration;

        private System.Timers.Timer authenticationTimer;
        internal static List<ApiKey> AuthenticatedKeys = new List<ApiKey>();
        internal static List<ApiKey> UnauthenticatedKeys = new List<ApiKey>();


        /// <summary>
        /// Flag whether SSL is used for client connections. Read Only.
        /// </summary>
        public bool UseSSL { get { return _sslCertificate != null; } }

        private int _port;
        /// <summary>
        /// Gets the Port that the server listens on. Read Only.
        /// </summary>
        public int Port { get { return _port; } }

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

        public StreamingServer(Configuration config)
        {
            Timeout = 30000; // 30 Seconds

            LoadConfiguration(config);
        }

        public void LoadConfiguration(Configuration config)
        {
            Configuration = config;

            // Get SSL Certificate (if set)
            if (!string.IsNullOrEmpty(config.SslCertificatePath))
            {
                string path = config.SslCertificatePath;

                if (File.Exists(path))
                {
                    X509Certificate2 cert = null;

                    // Set the Certificate Password
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

            // Set Port
            _port = config.StreamingPort;

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
        }

        public void Start()
        {
            log.Info("Streaming Server Started..");

            stop = new ManualResetEvent(false);

            // Create authentication timer
            if (!string.IsNullOrEmpty(Configuration.AuthenticationUrl))
            {
                authenticationTimer = new System.Timers.Timer();
                authenticationTimer.Interval = AUTHENTICATION_INTERVAL;
                authenticationTimer.Elapsed += AuthenticationTimer_Elapsed;
                authenticationTimer.Start();
            }

            // Create new thread for listening for streaming connections
            thread = new Thread(new ThreadStart(Worker));
            thread.Start();
        }

        private void Worker()
        {
            do
            {
                ListenForConnections();

            } while (!stop.WaitOne(5000, true));
        }

        private void ListenForConnections()
        {
            try
            {
                // Create new TcpListener
                listener = new TcpListener(IPAddress.Any, _port);

                // Start listening for client requests.
                listener.Start();

                do
                {
                    log.Info("Waiting for a connection on port " + _port + "... ");

                    try
                    {
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
                            log.Info("Blocked from " + client.Client.LocalEndPoint.ToString());
                            if (client != null) client.Close();
                        }
                        else
                        {
                            log.Info("Connected to " + client.Client.LocalEndPoint.ToString());

                            // Start Processing Data on separate thread and continue to listen for subsequent requests
                            ThreadPool.QueueUserWorkItem((x) =>
                            {
                                // Start new StreamConnection for client connection
                                var stream = new StreamConnection(ref client, _sslCertificate);
                                stream.Timeout = Timeout;
                                stream.Start();
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Trace(ex);
                    }

                } while (!stop.WaitOne(0, true));
            }
            catch (SocketException ex)
            {
                log.Error(ex);
                log.Error("ListenForConnections() : STOPPED!");
            }
            catch (Exception ex)
            {
                log.Error(ex);
                log.Error("ListenForConnections() : STOPPED!");
            }
        }

        public static bool AddToQueue(IStreamData data)
        {
            if (ValidateApiKey(data))
            {
                Queue.Add(data);
                return true;
            }

            return false;
        }

        public static bool AddToQueue(List<IStreamData> data)
        {
            foreach (var streamData in data)
            {
                if (ValidateApiKey(streamData))
                {
                    Queue.Add(streamData);
                    return true;
                }
            }

            return false;         
        }

        #region "Api Key Authentication"

        private static bool ValidateApiKey(IStreamData data)
        {
            List<ApiKey> authenticatedKeys;
            List<ApiKey> unauthenticatedKeys;

            lock (_lock)
            {
                authenticatedKeys = AuthenticatedKeys.ToList();
                unauthenticatedKeys = UnauthenticatedKeys.ToList();
            }

            if (authenticatedKeys != null && unauthenticatedKeys != null)
            {
                // Check if already in AuthenticatedKey list
                var key = authenticatedKeys.Find(o => o.Key == data.ApiKey && o.DeviceId == data.DeviceId);
                if (key != null) return true;
                else
                {
                    // Check if already in UnauthenticatedKey list
                    key = unauthenticatedKeys.Find(o => o.Key == data.ApiKey && o.DeviceId == data.DeviceId);
                    if (key == null)
                    {
                        // Create new Device
                        bool success = Device.Create(data.ApiKey, data.DeviceId, Configuration.AuthenticationUrl);
                        if (success)
                        {
                            // Add to Authenticated list
                            lock (_lock) AuthenticatedKeys.Add(new ApiKey(data.ApiKey, data.DeviceId));
                            return true;
                        }
                        else
                        {
                            // Add to Unauthenticated list
                            lock (_lock) UnauthenticatedKeys.Add(new ApiKey(data.ApiKey, data.DeviceId));
                        }
                    }
                }
            }

            return false;
        }

        private void AuthenticationTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            List<ApiKey> authenticatedKeys = null;
            List<ApiKey> unauthenticatedKeys = null;

            lock (_lock)
            {
                authenticatedKeys = AuthenticatedKeys.ToList();
                unauthenticatedKeys = UnauthenticatedKeys.ToList();
            }

            if (authenticatedKeys != null && unauthenticatedKeys != null)
            {
                // Check to make sure each Authenticated key is still valid
                foreach (var key in authenticatedKeys)
                {
                    // Create new Device
                    bool success = Device.Create(key.Key, key.DeviceId, Configuration.AuthenticationUrl);
                    if (!success)
                    {
                        lock (_lock)
                        {
                            // Remove from Authenticated list
                            int i = AuthenticatedKeys.ToList().FindIndex(o => o.Key == key.Key && o.DeviceId == key.DeviceId);
                            if (i >= 0) AuthenticatedKeys.RemoveAt(i);

                            // Add to Unauthenticated list
                            UnauthenticatedKeys.Add(new ApiKey(key.Key, key.DeviceId));
                        }
                    }
                }

                // Check each Unauthenticated key if now valid
                foreach (var key in unauthenticatedKeys)
                {
                    // Create new Device
                    bool success = Device.Create(key.Key, key.DeviceId, Configuration.AuthenticationUrl);
                    if (success)
                    {
                        lock (_lock)
                        {
                            // Remove from Unauthenticated list
                            int i = UnauthenticatedKeys.ToList().FindIndex(o => o.Key == key.Key && o.DeviceId == key.DeviceId);
                            if (i >= 0) UnauthenticatedKeys.RemoveAt(i);

                            // Add to Authenticated list
                            AuthenticatedKeys.Add(new ApiKey(key.Key, key.DeviceId));
                        }
                    }
                }
            }
        }

        #endregion

        private void PrintCertificateInfo(X509Certificate2 cert)
        {
            log.Info("SSL Certificate Information");
            log.Info("---------------------------");
            log.Info("Common Name : " + cert.GetNameInfo(X509NameType.SimpleName, false));
            log.Info("Subject : " + cert.Subject);
            log.Info("Serial Number : " + cert.SerialNumber);
            log.Info("Format : " + cert.GetFormat());
            log.Info("Effective Date : " + cert.GetEffectiveDateString());
            log.Info("Expiration Date : " + cert.GetExpirationDateString());
            log.Info("---------------------------");
        }

    }
}
