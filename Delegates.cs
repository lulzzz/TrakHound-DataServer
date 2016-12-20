// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System.Collections.Generic;

namespace TrakHound.Squirrel
{
    public delegate void ContainerDefinitionsHandler(List<ContainerDefinition> definitions);

    public delegate void DataDefinitionsHandler(List<DataDefinition> definitions);

    public delegate void DataSamplesHandler(List<DataSample> samples);
}
