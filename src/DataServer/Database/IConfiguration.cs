// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

namespace TrakHound.DataServer.Sql
{
    public interface IConfiguration
    {
        string DatabaseType { get; set; }
    }
}
