DROP procedure IF EXISTS `getInstance`;

DELIMITER $$
CREATE PROCEDURE `getInstance`(IN deviceId varchar(90), IN fromTS BIGINT)
BEGIN

DECLARE n INT DEFAULT 0;
DECLARE i INT DEFAULT 1;

-- Get Agent Instance Id at Timestamp
IF (fromTS > 0) THEN 
	SELECT `instance_id` FROM `agents` WHERE `device_id` = deviceId AND `timestamp` <= fromTS ORDER BY `timestamp` LIMIT 1 INTO @agentId;
ELSE 
	SELECT `instance_id` FROM `agents` WHERE `device_id` = deviceId ORDER BY `timestamp` LIMIT 1 INTO @agentId;
END IF;


DROP TABLE IF EXISTS `tmpIds`;
CREATE TEMPORARY TABLE `tmpIds` (`x` int auto_increment, `id` varchar(90), PRIMARY KEY (`x`)) ENGINE=MEMORY;
INSERT INTO `tmpIds` (`id`) SELECT `id` FROM `data_items` WHERE `device_id` = deviceId AND `agent_instance_id` = @agentId;

DROP TABLE IF EXISTS `tmpInstance`;
CREATE TEMPORARY TABLE `tmpInstance` ENGINE=MEMORY (SELECT * FROM `archived_samples` LIMIT 0);

-- Get the number of Ids
SELECT COUNT(*) FROM `tmpIds` INTO n;

WHILE i <= n DO

	-- Set @id as the next Id
	SELECT `id` FROM `tmpIds` WHERE `x` = i INTO @id;

	-- Get latest values from `archived_samples` table
	IF (fromTS > 0) THEN 
		SELECT `timestamp`, `sequence`, `cdata`, `condition` FROM `archived_samples` WHERE (`device_id` = deviceId AND `id` = @id AND `timestamp` <= fromTS) ORDER BY `timestamp` DESC LIMIT 1 INTO @ts, @seq, @cdata, @cond;
	ELSE 
		SELECT `timestamp`, `sequence`, `cdata`, `condition` FROM `archived_samples` WHERE (`device_id` = deviceId AND `id` = @id) ORDER BY `timestamp` DESC LIMIT 1 INTO @ts, @seq, @cdata, @cond;
    END IF;

    IF (SELECT FOUND_ROWS() > 0) THEN
		
        -- Insert into temp table
        INSERT INTO `tmpInstance` (`agent_instance_id`, `device_id`, `id`, `timestamp`, `sequence`, `cdata`, `condition`) VALUES (@agentId, deviceId, @id, @ts, @seq, @cdata, @cond);
        
    END IF;
    
    -- Increment index
	SET i = i + 1;
    
END WHILE;

-- Return value table
SELECT * FROM `tmpInstance`;

-- Clean up
DROP TABLE IF EXISTS `tmpInstance`;
DROP TABLE IF EXISTS `tmpIds`;

END$$

DELIMITER ;

