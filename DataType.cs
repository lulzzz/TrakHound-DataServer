// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System.Collections.Generic;
using System.Globalization;

namespace TrakHound.Squirrel
{
    public class DataType
    {
        public static DataTypes Get(string s)
        {
            string n = Normalize(s);
            if (!string.IsNullOrEmpty(n))
            {
                if (PositionTypes.Exists(o => o.ToLower() == n.ToLower())) return DataTypes.POSITION;
                if (ProgramTypes.Exists(o => o.ToLower() == n.ToLower())) return DataTypes.PROGRAM;
                if (StatusTypes.Exists(o => o.ToLower() == n.ToLower())) return DataTypes.STATUS;
            }

            return DataTypes.OTHER;
        }

        private static List<string> PositionTypes = new List<string>()
        {
            "Position",
            "PathPosition",
            "PathFeedrate",
            "RotaryVelocity"
        };

        private static List<string> ProgramTypes = new List<string>()
        {
            "Program",
            "Block",
            "Line",
            "ToolId"
        };

        private static List<string> StatusTypes = new List<string>()
        {
            "EmergencyStop",
            "Message",
            "Availability",
            "PowerState",
            "ControllerMode",
            "Execution"
        };

        private static string Normalize(string s)
        {
            if (!string.IsNullOrEmpty(s))
            {
                s = s.Replace("_", " ");
                s = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(s.ToLower());
                return s.Replace(" ", "");
            }

            return s;
        }

    }
}
