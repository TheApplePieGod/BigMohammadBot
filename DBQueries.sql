use [BigMohammadBot]

--TRUNCATE TABLE Channels
--TRUNCATE TABLE VoiceStatistics
--TRUNCATE TABLE MessageStatistics
--UPDATE AppState
--SET HelloChannelId = 0
--WHERE Id = 1;

--TRUNCATE TABLE Users
--TRUNCATE TABLE Greetings
--TRUNCATE TABLE SupressedUsers
--TRUNCATE TABLE ActivityLog
--TRUNCATE TABLE Channels
--TRUNCATE TABLE VoiceStatistics
--TRUNCATE TABLE MessageStatistics
--UPDATE AppState
--SET HelloChannelId = 0, LastHelloUserId = 0
--WHERE Id = 1;

EXEC sp_spaceused N'ActivityLog';

select * from AppState

select * from Channels

select * from MessageStatistics

select * from VoiceStatistics

select * from Users

select * from Greetings
where Iteration = 11

select * from ChannelBlacklist

select * from SupressedUsers

select * from ActivityLog
select * from udf_GetLastLogs(20)

select [User].DiscordUserName as [Name], COUNT(UserId) as Num from Greetings
left join Users [User] on (UserId = [User].Id)
where Iteration = 17
group by [User].DiscordUserName
order by Num desc
