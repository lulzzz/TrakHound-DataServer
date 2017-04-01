# Create the Samples table
CREATE TABLE IF NOT EXISTS `archived_samples` (

`device_id` varchar(90) NOT NULL,
`id` varchar(90) NOT NULL,
`timestamp` bigint NOT NULL,
`agent_instance_id` bigint NOT NULL,
`sequence` bigint NOT NULL,
`cdata` varchar(1000),
`condition` varchar(1000),

PRIMARY KEY (`device_id`, `id`, `timestamp`),
INDEX (`device_id`),
INDEX (`timestamp`),
INDEX (`id`)
);

# Create the Current table
CREATE TABLE IF NOT EXISTS `current_samples` (

`device_id` varchar(90) NOT NULL,
`id` varchar(90) NOT NULL,
`timestamp` bigint NOT NULL,
`agent_instance_id` bigint NOT NULL,
`sequence` bigint NOT NULL,
`cdata` varchar(1000),
`condition` varchar(1000),

PRIMARY KEY (`device_id`, `id`),
INDEX (`device_id`),
INDEX (`id`)
);

# Create the Connections table
CREATE TABLE IF NOT EXISTS `connections` (

`device_id` varchar(90) NOT NULL,
`address` varchar(90) NOT NULL,
`port` int(10),
`physical_address` varchar(90),

PRIMARY KEY (`device_id`),
INDEX (`device_id`)
);

# Create the Agents table
CREATE TABLE IF NOT EXISTS `agents` (

`device_id` varchar(90) NOT NULL,
`instance_id` bigint NOT NULL,
`sender` varchar(90),
`version` varchar(90),
`buffer_size` varchar(90),
`test_indicator` varchar(90),
`timestamp` bigint NOT NULL,

PRIMARY KEY (`device_id`, `instance_id`),
INDEX (`device_id`)
);

# Create the Assets table
CREATE TABLE IF NOT EXISTS `assets` (

`device_id` varchar(90) NOT NULL,
`id` varchar(90) NOT NULL,
`timestamp` bigint NOT NULL,
`agent_instance_id` bigint,
`type` varchar(90) NOT NULL,
`xml` blob NOT NULL,

PRIMARY KEY (`device_id`, `id`, `timestamp`)
);

# Create the Devices table
CREATE TABLE IF NOT EXISTS `devices` (

`device_id` varchar(90) NOT NULL,
`agent_instance_id` bigint NOT NULL,
`id` varchar(90) NOT NULL,
`uuid` varchar(90),
`name` varchar(90),
`native_name` varchar(90),
`sample_interval` double,
`sample_rate` double,
`iso_841_class` varchar(90),
`manufacturer` varchar(180),
`model` varchar(180),
`serial_number` varchar(180),
`station` varchar(180),
`description` varchar(500),

PRIMARY KEY (`device_id`, `agent_instance_id`),
INDEX (`device_id`)
);

# Create the Components table
CREATE TABLE IF NOT EXISTS `components` (

`device_id` varchar(90) NOT NULL,
`agent_instance_id` bigint NOT NULL,
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
`agent_instance_id` bigint NOT NULL,
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

# Create the Status table
CREATE TABLE IF NOT EXISTS `status` (

`device_id` varchar(90) NOT NULL,
`timestamp` bigint NOT NULL,
`connected` int(1) DEFAULT 0 NOT NULL,
`available` int(1) DEFAULT 0 NOT NULL,

PRIMARY KEY (`device_id`)
);

DROP procedure IF EXISTS `checkStatus`;

DELIMITER $$
CREATE PROCEDURE `checkStatus`()
BEGIN

DECLARE n INT DEFAULT 0;
DECLARE i INT DEFAULT 1;

DROP TABLE IF EXISTS `tmp_status`;
CREATE TEMPORARY TABLE `tmp_status` (`x` int auto_increment, `device_id` varchar(90), PRIMARY KEY (`x`)) ENGINE=MEMORY;
INSERT INTO `tmp_status` (`device_id`) SELECT `device_id` FROM `status` WHERE (`connected` = 1 OR `available` = 1) AND `timestamp` < ((UNIX_TIMESTAMP() * 1000) - 90000);

-- Get the number of Ids
SELECT COUNT(*) FROM `tmp_status` INTO n;

WHILE i <= n DO

	-- Set @id as the next Id
	SELECT `device_id` FROM `tmp_status` WHERE `x` = i INTO @id;
    
    UPDATE `status` SET `timestamp`=(UNIX_TIMESTAMP() * 1000), `connected`=0, `available`=0 WHERE `device_id`=@id;

    -- Increment index
	SET i = i + 1;
    
END WHILE;

-- Clean up
DROP TABLE IF EXISTS `tmp_status`;

END$$

DELIMITER ;

DROP EVENT IF EXISTS `check_status`;
CREATE EVENT `check_status`ON SCHEDULE EVERY 30 SECOND DO call checkStatus();




