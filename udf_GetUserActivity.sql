use [BigMohammadBot]
go
IF OBJECT_ID('udf_GetUserActivity') IS NOT NULL
DROP FUNCTION udf_GetUserActivity
GO
CREATE FUNCTION udf_GetUserActivity (@UserId int, @AllUsers bit, @TimeRangeBottom datetime, @TimeRangeTop datetime)
RETURNS @ResultTable TABLE ([UserName] VARCHAR(200), TotalMessages int, TotalSecondsInVoice int)
AS BEGIN
INSERT INTO @ResultTable
	
	select [User].DiscordUserName, SUM([Stats].MessagesSent), 0
	from dbo.MessageStatistics [Stats]
		left join dbo.Users [User] on ([Stats].UserId = [User].Id)
		left join dbo.Channels Channel on ([Stats].ChannelId = Channel.Id)
	where ([User].Id = @UserId or @AllUsers = 1)
		and Channel.Deleted = 0
		and [Stats].ChannelId not in (select ChannelId from ChannelBlacklist)
		and (@TimeRangeBottom = '' or [Stats].TimePeriod >= @TimeRangeBottom)
		and (@TimeRangeTop = '' or [Stats].TimePeriod <= @TimeRangeTop)
	group by [User].DiscordUserName

INSERT INTO @ResultTable

	select [User].DiscordUserName, 0, SUM([Stats].TimeInChannel)
	from dbo.VoiceStatistics [Stats]
		left join dbo.Users [User] on ([Stats].UserId = [User].Id)
		left join dbo.Channels Channel on ([Stats].ChannelId = Channel.Id)
	where ([User].Id = @UserId or @AllUsers = 1)
		and Channel.Deleted = 0
		and [Stats].ChannelId not in (select ChannelId from ChannelBlacklist)
		and (@TimeRangeBottom = '' or [Stats].TimePeriod >= @TimeRangeBottom)
		and (@TimeRangeTop = '' or [Stats].TimePeriod <= @TimeRangeTop)
	group by [User].DiscordUserName

RETURN
END
go
select UserName, sum(TotalMessages) as TotalMessages, sum(TotalSecondsInVoice) as TotalSecondsInVoice from udf_GetUserActivity(1,1, '', '')
group by UserName