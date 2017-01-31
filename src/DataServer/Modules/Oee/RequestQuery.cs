// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Web;

namespace TrakHound.DataServer.Modules.Oee
{
    /// <summary>
    /// Contains the query parameters for a request
    /// </summary>
    class RequestQuery
    {
        public string DeviceId { get; set; }

        public string SubQuery { get; set; }

        public DateTime From { get; set; }

        public DateTime To { get; set; }

        public int Interval { get; set; }

        private bool _isValid = false;
        public bool IsValid { get { return _isValid; } }

        public RequestQuery(Uri uri)
        {
            if (uri != null)
            {
                var segments = uri.Segments;
                if (segments.Length > 1)
                {
                    bool valid = false;
                    int i = 2;

                    // Get the requested Query and SubQuery
                    string query = segments[segments.Length - 1].ToLower().Trim('/');
                    if (query == "oee") valid = true;
                    else if (segments.Length > 2)
                    {
                        // Get SubQuery
                        if (query == "availability" || query == "performance" || query == "quality")
                        {
                            SubQuery = query;
                            valid = segments[segments.Length - i].Trim('/') == "oee";
                        }

                        i = 3;
                    }

                    if (valid)
                    {
                        // Get the Device Id as the resource owner
                        DeviceId = segments[segments.Length - i].Trim('/');
                        if (!string.IsNullOrEmpty(DeviceId))
                        {
                            // From
                            string s = HttpUtility.ParseQueryString(uri.Query).Get("from");
                            DateTime from = DateTime.MinValue;
                            DateTime.TryParse(s, out from);
                            From = from.ToUniversalTime();

                            // To
                            s = HttpUtility.ParseQueryString(uri.Query).Get("to");
                            DateTime to = DateTime.MinValue;
                            DateTime.TryParse(s, out to);
                            To = to.ToUniversalTime();

                            // Interval
                            s = HttpUtility.ParseQueryString(uri.Query).Get("interval");
                            int interval = 0;
                            int.TryParse(s, out interval);
                            Interval = interval;

                            _isValid = to > from;
                        }
                    }
                }
            }
        }
    }
}
