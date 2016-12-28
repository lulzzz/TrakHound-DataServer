// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using NLog;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using TrakHound.DataServer.Data;

namespace TrakHound.DataServer.Rest
{
    public class RestServer
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        private HttpListener listener;
        private Thread thread;
        private ManualResetEvent stop;

        private List<IModule> modules;

        public List<string> Prefixes { get; set; }

        public RestServer(Configuration config)
        {
            Prefixes = config.Prefixes;
            modules = DataProcessor.LoadModules();
        }

        public void Start()
        {
            if (Prefixes != null && Prefixes.Count > 0)
            {
                stop = new ManualResetEvent(false);

                thread = new Thread(new ThreadStart(Worker));
                thread.Start();
            }
            else
            {
                var ex = new Exception("No URL Prefixes are defined!");
                log.Error(ex);
                throw ex;
            }
        }

        public void Stop()
        {
            if (stop != null) stop.Set();
        }

        private void Worker()
        {
            do
            {
                try
                {
                    // (Access Denied - Exception)
                    // Must grant permissions to use URL (for each Prefix) in Windows using the command below
                    // CMD: netsh http add urlacl url = "http://localhost/" user = everyone

                    // (Service Unavailable - HTTP Status)
                    // Multiple urls are configured using netsh that point to the same place
 
                    listener = new HttpListener();

                    // Add Prefixes
                    foreach (var prefix in Prefixes)
                    {
                        listener.Prefixes.Add(prefix);
                    }
                    
                    // Start Listener
                    listener.Start();

                    foreach (var prefix in Prefixes) log.Info("Rest Server : Listening at " + prefix + "..");

                    while (listener.IsListening && !stop.WaitOne(0, true))
                    {
                        var context = listener.GetContext();

                        ThreadPool.QueueUserWorkItem((o) =>
                        {
                            try
                            {
                                log.Info("Connected to : " + context.Request.LocalEndPoint.ToString());

                                var requestUri = context.Request.Url;

                                string response = DataProcessor.Get(requestUri, modules);
                                if (!string.IsNullOrEmpty(response))
                                {
                                    log.Info(response);

                                    var b = Encoding.UTF8.GetBytes(response);
                                    context.Response.OutputStream.Write(b, 0, b.Length);
                                }
                                else
                                {
                                    context.Response.StatusCode = 400;
                                }
                            }
                            catch (Exception ex)
                            {
                                context.Response.StatusCode = 500;
                                log.Error(ex);
                            }
                            finally
                            {
                                context.Response.KeepAlive = false;
                                context.Response.OutputStream.Close();
                                context.Response.Close();
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    log.Error(ex);
                }
            } while (!stop.WaitOne(1000, true));
        }

    }
}
