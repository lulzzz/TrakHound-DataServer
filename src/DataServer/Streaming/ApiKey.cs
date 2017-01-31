// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

namespace TrakHound.DataServer.Streaming
{
    class ApiKey
    {
        public string Key { get; set; }

        public string DeviceId { get; set; }

        public ApiKey(string key, string deviceId)
        {
            Key = key;
            DeviceId = deviceId;
        }
    }
}
