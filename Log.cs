// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;

namespace TrakHound.DataServer
{
    public class Log
    {
        public static void Write(string line, object sender)
        {
            Console.WriteLine(line);
        }
    }
}
