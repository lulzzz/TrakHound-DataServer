-- Create the Samples table
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'archived_samples')
BEGIN
   CREATE TABLE [archived_samples] (

	[device_id] varchar(90) NOT NULL,
	[id] varchar(90) NOT NULL,
	[timestamp] bigint NOT NULL,
	[agent_instance_id] bigint NOT NULL,
	[sequence] bigint NOT NULL,
	[cdata] varchar(1000),
	[condition] varchar(1000),

	PRIMARY KEY ([device_id], [id], [timestamp]),
	);
END

-- Create the Current table
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'current_samples')
BEGIN
	CREATE TABLE [current_samples] (

	[device_id] varchar(90) NOT NULL,
	[id] varchar(90) NOT NULL,
	[timestamp] bigint NOT NULL,
	[agent_instance_id] bigint NOT NULL,
	[sequence] bigint NOT NULL,
	[cdata] varchar(1000),
	[condition] varchar(1000),

	PRIMARY KEY ([device_id], [id]),
	);
END

-- Create the Connections table
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'connections')
BEGIN
	CREATE TABLE [connections] (

	[device_id] varchar(90) NOT NULL,
	[address] varchar(90) NOT NULL,
	[port] int,
	[physical_address] varchar(90),

	PRIMARY KEY ([device_id]),
	);
END

-- Create the Agents table
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'agents')
BEGIN
	CREATE TABLE [agents] (

	[device_id] varchar(90) NOT NULL,
	[instance_id] bigint NOT NULL,
	[sender] varchar(90),
	[version] varchar(90),
	[buffer_size] varchar(90),
	[test_indicator] varchar(90),
	[timestamp] bigint NOT NULL,

	PRIMARY KEY ([device_id], [instance_id]),
	);
END

-- Create the Assets table
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'assets')
BEGIN
	CREATE TABLE assets (

	[device_id] varchar(90) NOT NULL,
	[id] varchar(90),
	[timestamp] bigint NOT NULL,
	[agent_instance_id] bigint NOT NULL,
	[type] varchar(90) NOT NULL,
	[xml] blob NOT NULL,

	PRIMARY KEY ([device_id], [id], [timestamp]),
	);
END

-- Create the Devices table
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'devices')
BEGIN
	CREATE TABLE [devices] (

	[device_id] varchar(90) NOT NULL,
	[agent_instance_id] bigint NOT NULL,
	[id] varchar(90) NOT NULL,
	[uuid] varchar(90),
	[name] varchar(90),
	[native_name] varchar(90),
	[sample_interval] float,
	[sample_rate] float,
	[iso_841_class] varchar(90),
	[manufacturer] varchar(180),
	[model] varchar(180),
	[serial_number] varchar(180),
	[station] varchar(180),
	[description] varchar(500),

	PRIMARY KEY ([device_id], [agent_instance_id]),
	);
END

-- Create the Components table
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'components')
BEGIN
	CREATE TABLE [components] (

	[device_id] varchar(90) NOT NULL,
	[agent_instance_id] bigint NOT NULL,
	[id] varchar(90) NOT NULL,
	[uuid] varchar(90),
	[name] varchar(90),
	[native_name] varchar(90),
	[sample_interval] float,
	[sample_rate] float,
	[type] varchar(90),
	[parent_id] varchar(90) NOT NULL,

	PRIMARY KEY ([device_id], [agent_instance_id], [id]),
	);
END

-- Create the DataItems table
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'data_items')
BEGIN
	CREATE TABLE [data_items] (

	[device_id] varchar(90) NOT NULL,
	[agent_instance_id] bigint NOT NULL,
	[id] varchar(90) NOT NULL,
	[name] varchar(90),
	[category] varchar(90),
	[type] varchar(90),
	[sub_type] varchar(90),
	[statistic] varchar(90),
	[units] varchar(90),
	[native_units] varchar(90),
	[native_scale] varchar(90),
	[coordinate_system] varchar(90),
	[sample_rate] varchar(90),
	[representation] varchar(90),
	[significant_digits] varchar(90),
	[parent_id] varchar(90) NOT NULL,

	PRIMARY KEY ([device_id], [agent_instance_id], [id]),
	);
END

-- Create the Status table
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'status')
BEGIN
	CREATE TABLE [status] (

	[device_id] varchar(90) NOT NULL,
	[timestamp] bigint NOT NULL,
	[connected] bit,
	[available] bit,

	PRIMARY KEY ([device_id]),
	);
END
