use [BigMohammadBot]
go
IF OBJECT_ID('udf_GetChainBreaks') IS NOT NULL
DROP FUNCTION udf_GetChainBreaks
GO
CREATE FUNCTION udf_GetChainBreaks (@UserId int)
RETURNS @ResultTable TABLE (UserName varchar(200), [Count] int)
AS BEGIN
INSERT INTO @ResultTable
	
	select TOP(5) DiscordUserName, ChainBreaks from Users
	where (Id = @UserId or @UserId = 0)
	order by ChainBreaks desc

RETURN
END
go
select * from udf_GetChainBreaks(0)