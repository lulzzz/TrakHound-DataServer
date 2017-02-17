
-- Create the Cutting Tool table
CREATE TABLE IF NOT EXISTS `cutting_tools` (

`device_id` varchar(90) NOT NULL,
`agent_instance_id` bigint NOT NULL,
`asset_id` varchar(90) NOT NULL,
`timestamp` bigint NOT NULL,

`tool_id` varchar(90),
`serial_number` varchar(90),
`manufacturers` varchar(90),
`device_uuid` varchar(90),
`removed` varchar(90),
`description` varchar(90),

PRIMARY KEY (`device_id`, `asset_id`, `timestamp`),
);

# Create the Cutting Tool Life Cycle table
CREATE TABLE IF NOT EXISTS `cutting_tool_life_cycles` (

`device_id` varchar(90) NOT NULL,
`agent_instance_id` bigint NOT NULL,
`asset_id` varchar(90) NOT NULL,
`timestamp` bigint NOT NULL,

`cutter_status` varchar(300),

`program_tool_group` varchar(90),
`program_tool_number` int(10),

`location` int(10),
`location_type` varchar(90),
`location_positive_overlap` int(10),
`location_negative_overlap` int(10),

`process_spindle_speed_maximum` int(10),
`process_spindle_speed_minimum` int(10),
`process_spindle_speed_nominal` int(10),

`process_feedrate_maximum` int(10),
`process_feedrate_minimum` int(10),
`process_feedrate_nominal` int(10),

`recondition_count` int(10),
`recondition_count_maximum_count` int(10),

`connection_code_machine_side` varchar(90),

PRIMARY KEY (`device_id`, `asset_id`, `timestamp`),
);

# Create the Cutting Tool Tool Life table
CREATE TABLE IF NOT EXISTS `cutting_tool_tool_life` (

`device_id` varchar(90) NOT NULL,
`agent_instance_id` bigint NOT NULL,
`asset_id` varchar(90) NOT NULL,
`timestamp` bigint NOT NULL,

`tool_life` bigint,
`type` varchar(90),
`count_direction` varchar(90),
`warning` bigint,
`limit` bigint,
`initial` bigint,

PRIMARY KEY (`device_id`, `asset_id`, `timestamp`),
);