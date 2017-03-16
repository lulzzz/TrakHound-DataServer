// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using NLog;
using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace TrakHound.DataServer.Streaming
{
    /// <summary>
    /// Handles streams for new connections and adds received JSON data to SQL queue
    /// </summary>
    class StreamConnection
    {
        // Response Codes
        public const int SUCCESS = 200;
        public const int BAD_REQUEST_ERROR = 400;
        public const int AUTHENTICATION_ERROR = 401;

        private static Logger logger = LogManager.GetCurrentClassLogger();

        private Stream stream = null;
        private StreamReader streamReader;
        private StreamWriter streamWriter;
        private ManualResetEvent stop;

        private TcpClient _client;
        /// <summary>
        /// The TcpClient Connection used for streaming data
        /// </summary>
        public TcpClient Client { get { return _client; } }

        private IPEndPoint _endPoint;
        /// <summary>
        /// Gets the Client Connection's initial RemoteEndPoint
        /// </summary>
        public IPEndPoint EndPoint { get { return _endPoint; } }

        /// <summary>
        /// Flag whether SSL is used for client connections. Read Only.
        /// </summary>
        public bool UseSSL { get { return _sslCertificate != null; } }

        /// <summary>
        /// Connection Timeout in Milliseconds
        /// </summary>
        public int Timeout { get; set; }

        private X509Certificate2 _sslCertificate;
        /// <summary>
        /// The SSL Certificate to use for client connections
        /// </summary>
        public X509Certificate2 SslCertificate { get { return _sslCertificate; } }

        public StreamConnection(ref TcpClient client, X509Certificate2 sslCertificate)
        {
            _endPoint = ((IPEndPoint)client.Client.LocalEndPoint);
            _client = client;
            _sslCertificate = sslCertificate;

            ServicePointManager.ServerCertificateValidationCallback += new RemoteCertificateValidationCallback((sender, certificate, chain, policyErrors) => { return true; });
        }

        public void Start()
        {
            stop = new ManualResetEvent(false);

            GetStream();
            if (streamReader == null)
            {
                logger.Warn(EndPoint.ToString() + " : No Stream Found");
                Stop();
            }
            else ReadStream();
        }

        public void Stop()
        {
            stop.Set();
            if (stream != null) stream.Close();
            if (_client != null) _client.Close();
        }

        
        private static bool RemoteCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            //Return true if the server certificate is ok
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;

            bool acceptCertificate = true;
            string msg = "The server could not be validated for the following reason(s):\r\n";

            //The server did not present a certificate
            if ((sslPolicyErrors &
                 SslPolicyErrors.RemoteCertificateNotAvailable) == SslPolicyErrors.RemoteCertificateNotAvailable)
            {
                msg = msg + "\r\n    -The server did not present a certificate.\r\n";
                acceptCertificate = false;
            }
            else
            {
                //The certificate does not match the server name
                if ((sslPolicyErrors &
                     SslPolicyErrors.RemoteCertificateNameMismatch) == SslPolicyErrors.RemoteCertificateNameMismatch)
                {
                    msg = msg + "\r\n    -The certificate name does not match the authenticated name.\r\n";
                    acceptCertificate = false;
                }

                //There is some other problem with the certificate
                if ((sslPolicyErrors &
                     SslPolicyErrors.RemoteCertificateChainErrors) == SslPolicyErrors.RemoteCertificateChainErrors)
                {
                    foreach (X509ChainStatus item in chain.ChainStatus)
                    {
                        if (item.Status != X509ChainStatusFlags.RevocationStatusUnknown &&
                            item.Status != X509ChainStatusFlags.OfflineRevocation)
                            break;

                        if (item.Status != X509ChainStatusFlags.NoError)
                        {
                            msg = msg + "\r\n    -" + item.StatusInformation;
                            acceptCertificate = false;
                        }
                    }
                }
            }

            //If Validation failed, present message box
            if (acceptCertificate == false)
            {
                msg = msg + "\r\nDo you wish to override the security check?";
                //          if (MessageBox.Show(msg, "Security Alert: Server could not be validated",
                //                       MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1) == DialogResult.Yes)
                acceptCertificate = true;
            }

            return acceptCertificate;
        }

        private void GetStream()
        {
            try
            {
                if (UseSSL)
                {
                    // Create new SSL Stream from client's NetworkStream
                    var sslStream = new SslStream(_client.GetStream(), false);
                    sslStream.AuthenticateAsServer(_sslCertificate, false, System.Security.Authentication.SslProtocols.Default, false);
                    stream = sslStream;
                }
                else
                {
                    stream = _client.GetStream();
                }

                streamReader = new StreamReader(stream);
                streamWriter = new StreamWriter(stream);
            }
            catch (System.Security.Authentication.AuthenticationException ex)
            {
                logger.Error(ex, EndPoint.ToString() + " : Authentication failed - closing the connection.");
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
        }

        private void ReadStream()
        {
            try
            {
                // Create & Start Timeout timer
                var timeoutTimer = new System.Timers.Timer();
                timeoutTimer.Interval = Timeout;
                timeoutTimer.Elapsed += TimeoutTimer_Elapsed;
                timeoutTimer.Enabled = true;

                while (!streamReader.EndOfStream)
                {
                    // Reset Timeout timer
                    timeoutTimer.Stop();
                    timeoutTimer.Start();

                    int response = BAD_REQUEST_ERROR;

                    // Read the next IStreamData object
                    string json = streamReader.ReadLine();

                    logger.Trace(json);

                    // Parse JSON string to IStreamData
                    var streamData = Json.StreamData.Read(json);
                    if (streamData != null)
                    {
                        if (StreamingServer.AddToQueue(streamData)) response = SUCCESS;
                        else response = AUTHENTICATION_ERROR;
                    }

                    // Write Response Code
                    streamWriter.WriteLine(response);
                    streamWriter.Flush();
                }

                logger.Info("End of Stream");
            }
            catch (IOException ex)
            {
                logger.Info(EndPoint.ToString() + " : Connection Interrupted");
                logger.Trace(ex);
            }
            catch (Exception ex)
            {
                logger.Trace(ex);
            }
            finally
            {
                if (stream != null) stream.Close();
                logger.Info(EndPoint.ToString() + " : Stream Closed");
            }
        }

        private void TimeoutTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            var timer = (System.Timers.Timer)sender;
            timer.Enabled = false;

            Stop();
        }
    }
}
