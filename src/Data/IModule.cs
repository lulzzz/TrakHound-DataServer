// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;

namespace TrakHound.DataServer.Data
{
    public interface IModule
    {
        /// <summary>
        /// Gets the name of the Module
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Processes the requested Uri and returns the requested data
        /// </summary>
        string GetResponse(Uri requestUri);
    }
}
