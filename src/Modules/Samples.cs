// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.ComponentModel.Composition;
using System.Web;
using TrakHound.DataServer.Data;

namespace TrakHound.DataServer.Modules
{
    [InheritedExport(typeof(IModule))]
    public class Samples : IModule
    {
        public string Name { get { return "Samples"; } }

        public string GetResponse(Uri requestUri)
        {
            Console.WriteLine(requestUri.ToString());

            var from = HttpUtility.ParseQueryString(requestUri.Query).Get("from");

            return from;
        }

    }
}
