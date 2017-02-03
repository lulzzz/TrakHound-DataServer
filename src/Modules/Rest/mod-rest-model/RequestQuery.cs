// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;

namespace mod_rest_model
{
    /// <summary>
    /// Contains the query parameters for a request
    /// </summary>
    class RequestQuery
    {
        public string DeviceId { get; set; }

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
                    if (segments[segments.Length - 1].ToLower().Trim('/') == "model")
                    {
                        // Get the Device Id as the resource owner
                        DeviceId = segments[segments.Length - 2].Trim('/');
                        _isValid = !string.IsNullOrEmpty(DeviceId);
                    }
                }
            }
        }
    }
}
