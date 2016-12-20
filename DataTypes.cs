// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

namespace TrakHound.Squirrel
{
    public enum DataTypes
    {
        ANY,

        /// <summary>
        /// Any Axis Position, Spindle Speed, or Feedrate data
        /// </summary>
        POSITION,

        /// <summary>
        /// Any Status data
        /// </summary>
        STATUS,

        /// <summary>
        /// Any Program data such as Program Name, Line, Block, Tool etc.
        /// </summary>
        PROGRAM,

        OTHER
    }
}
