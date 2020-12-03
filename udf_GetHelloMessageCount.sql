use [BigMohammadBot]
go
IF OBJECT_ID('udf_GetHelloMessageCount') IS NOT NULL
DROP FUNCTION udf_GetHelloMessageCount
GO
CREATE FUNCTION udf_GetHelloMessageCount (@Interation int, @AllIterations bit, @UserId int, @AllUsers bit)
RETURNS @ResultTable TABLE (UserId int, Iteration int, NumMessages int)
AS BEGIN
INSERT INTO @ResultTable
	
	select UserId, Iteration, COUNT(UserId) as NumMessages from Greetings
	where (Iteration = @Interation or @AllIterations = 1) and (UserId = @UserId or @AllUsers = 1)
	group by UserId, Iteration

RETURN
END
go
select * from udf_GetHelloMessageCount(11, 0, 1, 1)
order by NumMessages desc