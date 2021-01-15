use [BigMohammadBot]
go
IF OBJECT_ID('udf_GetUserTotalMessages') IS NOT NULL
DROP FUNCTION udf_GetUserTotalMessages
GO
CREATE FUNCTION udf_GetUserTotalMessages (@UserId int, @AllUsers bit, @TimeRangeBottom datetime, @TimeRangeTop datetime)
RETURNS @ResultTable TABLE ([UserName] VARCHAR(200), TotalMessages int)
AS BEGIN

INSERT INTO @ResultTable
	
	select [User].DiscordUserName, SUM([Stats].MessagesSent)
	from dbo.MessageStatistics [Stats]
		left join dbo.Users [User] on ([Stats].UserId = [User].Id)
		left join dbo.Channels Channel on ([Stats].ChannelId = Channel.Id)
	where ([User].Id = @UserId or @AllUsers = 1)
		and Channel.Deleted = 0
		and [Stats].ChannelId not in (select ChannelId from  ChannelBlacklist)
		and (@TimeRangeBottom = '' or [Stats].TimePeriod >= @TimeRangeBottom)
		and (@TimeRangeTop = '' or [Stats].TimePeriod <= @TimeRangeTop)
	group by [User].DiscordUserName

RETURN
END
go
select * from udf_GetUserTotalMessages(1,1,'','')