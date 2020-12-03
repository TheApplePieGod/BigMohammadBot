use [BigMohammadBot]
go
IF OBJECT_ID('udf_GetLastLogs') IS NOT NULL
DROP FUNCTION udf_GetLastLogs
GO
CREATE FUNCTION udf_GetLastLogs (@NumLogs int)
RETURNS @ResultTable TABLE ([TypeName] VARCHAR(50), Information VARCHAR(100), CalledByUserName VARCHAR(200), CallTime datetime, ResultText VARCHAR(200), Success bit)
AS BEGIN
INSERT INTO @ResultTable
	
	select TOP (@NumLogs) [Type].[Name], [Log].Information, [User].DiscordUserName, [Log].CallTime, [Log].ResultText, [Log].Success
	from dbo.ActivityLog [Log]
		left join dbo.Users [User] on ([Log].CalledByUserId = [User].Id)
		left join dbo.ActivityTypes [Type] on ([Log].TypeId = [Type].Id)
	order by [Log].Id desc

RETURN
END
go
select * from udf_GetLastLogs(5)