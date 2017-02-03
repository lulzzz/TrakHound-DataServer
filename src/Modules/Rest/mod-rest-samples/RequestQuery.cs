// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Web;

namespace mod_rest_samples
{
    /// <summary>
    /// Contains the query parameters for a request
    /// </summary>
    class RequestQuery
    {
        public string DeviceId { get; set; }

        public DateTime From { get; set; }

        public DateTime To { get; set; }

        public DateTime At { get; set; }

        public long Count { get; set; }

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
                    // Check if Samples is the resource that is requested
                    if (segments[segments.Length - 1].ToLower().Trim('/') == "samples")
                    {
                        // Get the Device Id as the resource owner
                        DeviceId = segments[segments.Length - 2].Trim('/');
                        if (!string.IsNullOrEmpty(DeviceId))
                        {
                            // From
                            string s = HttpUtility.ParseQueryString(uri.Query).Get("from");
                            DateTime from = DateTime.MinValue;
                            DateTime.TryParse(s, out from);
                            From = from;

                            // To
                            s = HttpUtility.ParseQueryString(uri.Query).Get("to");
                            DateTime to = DateTime.MinValue;
                            DateTime.TryParse(s, out to);
                            To = to;

                            //Count
                            s = HttpUtility.ParseQueryString(uri.Query).Get("count");
                            long count = 0;
                            long.TryParse(s, out count);
                            Count = count;

                            // At
                            s = HttpUtility.ParseQueryString(uri.Query).Get("at");
                            DateTime at = DateTime.MinValue;
                            DateTime.TryParse(s, out at);
                            At = at;

                            // Interval
                            s = HttpUtility.ParseQueryString(uri.Query).Get("interval");
                            int interval = 0;
                            int.TryParse(s, out interval);
                            Interval = interval;

                            _isValid = true;
                        }
                    }
                }
            }
        }
    }
}
