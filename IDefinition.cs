// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

namespace TrakHound.Sniff
{
    public interface IDefinition
    {
        string DeviceId { get; set; }

        string Id { get; set; }

        string Name { get; set; }
    }
}
