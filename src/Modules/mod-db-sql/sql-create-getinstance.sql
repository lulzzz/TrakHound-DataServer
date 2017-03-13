CREATE PROCEDURE [getInstance] @deviceId varchar(90), @fromTS bigint = 9223372036854775807
AS

DECLARE @n int = 0;
DECLARE @i int = 1;
DECLARE @agentId varchar(90) = null;

-- Get Agent Instance Id at Timestamp
IF (@fromTS > 0)
	SELECT TOP 1 @agentId = [instance_id] FROM [agents] WHERE [device_id] = @deviceId AND [timestamp] <= @fromTS ORDER BY [timestamp];
ELSE 
	SELECT TOP 1 @agentId = [instance_id] FROM [agents] WHERE [device_id] = @deviceId ORDER BY [timestamp];

IF OBJECT_ID('tempdb..##[#tmpIds]') IS NOT NULL DROP TABLE ##tmpIds;
CREATE TABLE [#tmpIds] ([x] int IDENTITY(1,1), [id] varchar(90), PRIMARY KEY ([x]));
INSERT INTO [#tmpIds] ([id]) SELECT [id] FROM [data_items] WHERE [device_id] = @deviceId AND [agent_instance_id] = @agentId;

IF OBJECT_ID('tempdb..##[#tmpInstance]') IS NOT NULL DROP TABLE ##tmpInstance;
SELECT TOP 0 * INTO [#tmpInstance] FROM [archived_samples];

SELECT @n = COUNT(*) FROM [#tmpIds];

WHILE @i <= @n
BEGIN

	DECLARE @id varchar(90);

	-- Set @id as the next Id
	SELECT @id = [id] FROM [#tmpIds] WHERE [x] = @i;

	-- Get latest values from `archived_samples` table
	SELECT TOP 1 * INTO [#tmpFound] FROM [archived_samples] WHERE ([device_id] = @deviceId AND [id] = @id AND [timestamp] <= @fromTS) ORDER BY [timestamp] DESC;

	DECLARE @count int;
	SELECT @count = COUNT(*) FROM #tmpFound;
    IF (@count > 0)
	BEGIN

		DECLARE @ts bigint;
		DECLARE @seq bigint;
		DECLARE @cdata varchar(300);
		DECLARE @cond varchar(300);

		SELECT @ts = [timestamp] FROM [#tmpFound];
		SELECT @seq = [sequence] FROM [#tmpFound];
		SELECT @cdata = [cdata] FROM [#tmpFound];
		SELECT @cond = [condition] FROM [#tmpFound];
		
        -- Insert into temp table
        INSERT INTO [#tmpInstance] ([agent_instance_id], [device_id], [id], [timestamp], [sequence], [cdata], [condition]) VALUES (@agentId, @deviceId, @id, @ts, @seq, @cdata, @cond);  
    END

	IF OBJECT_ID('tempdb..##[#tmpFound]') IS NOT NULL DROP TABLE ##tmpFound;

    -- Increment index
	SET @i = @i + 1;
END

-- Return value table
SELECT * FROM [#tmpInstance];

-- Clean up
IF OBJECT_ID('tempdb..##[#tmpInstance]') IS NOT NULL DROP TABLE ##tmpInstance;
IF OBJECT_ID('tempdb..##[#tmpIds]') IS NOT NULL DROP TABLE ##tmpIds;

GO

