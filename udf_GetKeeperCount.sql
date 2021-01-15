use [BigMohammadBot]
go
IF OBJECT_ID('udf_GetKeeperCount') IS NOT NULL
DROP FUNCTION udf_GetKeeperCount
GO
CREATE FUNCTION udf_GetKeeperCount (@UserId int)
RETURNS @ResultTable TABLE (UserName varchar(200), [Count] int)
AS BEGIN
INSERT INTO @ResultTable
	
	select TOP(5) DiscordUserName, AmountKeeper from Users
	where (Id = @UserId or @UserId = 0)
	order by AmountKeeper desc

RETURN
END
go
select * from udf_GetKeeperCount(0)