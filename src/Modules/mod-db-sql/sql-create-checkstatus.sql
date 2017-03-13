CREATE PROCEDURE [checkStatus]
AS

DECLARE @n int = 0;
DECLARE @i int = 1;

IF OBJECT_ID('tempdb..##[#tmp_status]') IS NOT NULL DROP TABLE ##tmp_status;
CREATE TABLE [#tmp_status] ([x] int IDENTITY(1,1), [device_id] varchar(90), PRIMARY KEY ([x]));
INSERT INTO [#tmp_status] ([device_id]) SELECT [device_id] FROM [status] WHERE ([connected] = 0 OR [available] = 0) AND [timestamp] < (DATEDIFF(SECOND, '19700101', GETUTCDATE()) - 90000);

SELECT @n = COUNT(*) FROM [#tmp_status];

WHILE @i <= @n
BEGIN

	DECLARE @id varchar(90);

	-- Set @id as the next Id
	SELECT @id = [device_id] FROM [#tmp_status] WHERE [x] = @i;

	UPDATE [device_id] SET [timestamp]=DATEDIFF(SECOND, '19700101', GETUTCDATE()), [connected] = 0, [available] = 0 WHERE [device_id] = @id;

    -- Increment index
	SET @i = @i + 1;
END

-- Clean up
IF OBJECT_ID('tempdb..##[#tmp_status]') IS NOT NULL DROP TABLE ##tmp_status;

GO