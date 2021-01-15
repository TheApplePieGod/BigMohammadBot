use [BigMohammadBot]
go
IF OBJECT_ID('udf_GetChannelTotalMessages') IS NOT NULL
DROP FUNCTION udf_GetChannelTotalMessages
GO
CREATE FUNCTION udf_GetChannelTotalMessages (@ChannelId int, @AllChannels bit, @TimeRangeBottom datetime, @TimeRangeTop datetime)
RETURNS @ResultTable TABLE ([ChannelName] VARCHAR(200), TotalMessages int)
AS BEGIN
INSERT INTO @ResultTable
	
	select [Channel].DiscordChannelName, SUM([Stats].MessagesSent)
	from dbo.MessageStatistics [Stats]
		left join dbo.Channels [Channel] on ([Stats].ChannelId = [Channel].Id)
	where ([Channel].Id = @ChannelId or @AllChannels = 1)
		and [Channel].Deleted = 0
		and [Channel].Id not in (select ChannelId from  ChannelBlacklist)
		and (@TimeRangeBottom = '' or [Stats].TimePeriod >= @TimeRangeBottom)
		and (@TimeRangeTop = '' or [Stats].TimePeriod <= @TimeRangeTop)
	group by [Channel].DiscordChannelName

RETURN
END
go
select * from udf_GetChannelTotalMessages(1,1,'','')