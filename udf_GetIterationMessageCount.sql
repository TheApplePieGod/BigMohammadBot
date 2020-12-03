use [BigMohammadBot]
go
IF OBJECT_ID('udf_GetIterationMessageCount') IS NOT NULL
DROP FUNCTION udf_GetIterationMessageCount
GO
CREATE FUNCTION udf_GetIterationMessageCount (@Iteration int)
RETURNS @ResultTable TABLE (Iteration int, [Count] int)
AS BEGIN
INSERT INTO @ResultTable
	
	select TOP(5) Iteration, COUNT(Id) as [Count] from Greetings
	where (Iteration = @Iteration or @Iteration = 0)
	group by Iteration
	order by [Count] desc

RETURN
END
go
select * from udf_GetIterationMessageCount(0)