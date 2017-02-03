# Create the Samples table
CREATE TABLE IF NOT EXISTS `archived_samples` (

`device_id` varchar(90) NOT NULL,
`id` varchar(90) NOT NULL,
`timestamp` BIGINT NOT NULL,
`agent_instance_id` varchar(90) NOT NULL,
`sequence` BIGINT NOT NULL,
`cdata` varchar(1000) NOT NULL,
`condition` varchar(1000) NOT NULL,

PRIMARY KEY (`device_id`, `id`, `timestamp`),
INDEX (`device_id`),
INDEX (`timestamp`),
INDEX (`id`)
);

# Create the Current table
CREATE TABLE IF NOT EXISTS `current_samples` (

`device_id` varchar(90) NOT NULL,
`id` varchar(90) NOT NULL,
`timestamp` BIGINT NOT NULL,
`agent_instance_id` varchar(90) NOT NULL,
`sequence` BIGINT NOT NULL,
`cdata` varchar(1000) NOT NULL,
`condition` varchar(1000) NOT NULL,

PRIMARY KEY (`device_id`, `id`),
INDEX (`device_id`),
INDEX (`id`)
);

# Create the Agents table
CREATE TABLE IF NOT EXISTS `agents` (

`device_id` varchar(90) NOT NULL,
`instance_id` varchar(90) NOT NULL,
`sender` varchar(90),
`version` varchar(90),
`buffer_size` varchar(90),
`test_indicator` varchar(90),
`timestamp` BIGINT NOT NULL,

PRIMARY KEY (`device_id`, `instance_id`),
INDEX (`device_id`)
);

# Create the Devices table
CREATE TABLE IF NOT EXISTS `devices` (

`device_id` varchar(90) NOT NULL,
`agent_instance_id` varchar(90) NOT NULL,
`id` varchar(90) NOT NULL,
`uuid` varchar(90),
`name` varchar(90),
`native_name` varchar(90),
`sample_interval` varchar(90),
`sample_rate` varchar(90),
`iso_841_class` varchar(90),

PRIMARY KEY (`device_id`, `agent_instance_id`),
INDEX (`device_id`)
);

# Create the Components table
CREATE TABLE IF NOT EXISTS `components` (

`device_id` varchar(90) NOT NULL,
`agent_instance_id` varchar(90) NOT NULL,
`id` varchar(90) NOT NULL,
`uuid` varchar(90),
`name` varchar(90),
`native_name` varchar(90),
`sample_interval` varchar(90),
`sample_rate` varchar(90),
`type` varchar(90),
`parent_id` varchar(90) NOT NULL,

PRIMARY KEY (`device_id`, `agent_instance_id`, `id`),
INDEX (`device_id`)
);

# Create the DataItems table
CREATE TABLE IF NOT EXISTS `data_items` (

`device_id` varchar(90) NOT NULL,
`agent_instance_id` varchar(90) NOT NULL,
`id` varchar(90) NOT NULL,
`name` varchar(90),
`category` varchar(90),
`type` varchar(90),
`sub_type` varchar(90),
`statistic` varchar(90),
`units` varchar(90),
`native_units` varchar(90),
`native_scale` varchar(90),
`coordinate_system` varchar(90),
`sample_rate` varchar(90),
`representation` varchar(90),
`significant_digits` varchar(90),
`parent_id` varchar(90) NOT NULL,

PRIMARY KEY (`device_id`, `agent_instance_id`, `id`),
INDEX (`device_id`)
);
