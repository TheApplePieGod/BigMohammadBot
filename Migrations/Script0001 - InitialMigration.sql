/****** Object:  UserDefinedFunction [dbo].[udf_GetChainBreaks]    Script Date: 1/15/2021 7:24:11 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE FUNCTION [dbo].[udf_GetChainBreaks] (@UserId int)
RETURNS @ResultTable TABLE (UserName varchar(200), [Count] int)
AS BEGIN
INSERT INTO @ResultTable
	
	select TOP(5) DiscordUserName, ChainBreaks from Users
	where (Id = @UserId or @UserId = 0)
	order by ChainBreaks desc

RETURN
END
GO
/****** Object:  UserDefinedFunction [dbo].[udf_GetChannelTotalMessages]    Script Date: 1/15/2021 7:24:11 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE FUNCTION [dbo].[udf_GetChannelTotalMessages] (@ChannelId int, @AllChannels bit, @TimeRangeBottom datetime, @TimeRangeTop datetime)
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
GO
/****** Object:  UserDefinedFunction [dbo].[udf_GetHelloMessageCount]    Script Date: 1/15/2021 7:24:11 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE FUNCTION [dbo].[udf_GetHelloMessageCount] (@Interation int, @AllIterations bit, @UserId int, @AllUsers bit)
RETURNS @ResultTable TABLE (UserId int, Iteration int, NumMessages int)
AS BEGIN
INSERT INTO @ResultTable
	
	select UserId, Iteration, COUNT(UserId) as NumMessages from Greetings
	where (Iteration = @Interation or @AllIterations = 1) and (UserId = @UserId or @AllUsers = 1)
	group by UserId, Iteration

RETURN
END
GO
/****** Object:  UserDefinedFunction [dbo].[udf_GetIterationMessageCount]    Script Date: 1/15/2021 7:24:11 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE FUNCTION [dbo].[udf_GetIterationMessageCount] (@Iteration int)
RETURNS @ResultTable TABLE (Iteration int, [Count] int)
AS BEGIN
INSERT INTO @ResultTable
	
	select TOP(5) Iteration, COUNT(Id) as [Count] from Greetings
	where (Iteration = @Iteration or @Iteration = 0)
	group by Iteration
	order by [Count] desc

RETURN
END
GO
/****** Object:  UserDefinedFunction [dbo].[udf_GetKeeperCount]    Script Date: 1/15/2021 7:24:11 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE FUNCTION [dbo].[udf_GetKeeperCount] (@UserId int)
RETURNS @ResultTable TABLE (UserName varchar(200), [Count] int)
AS BEGIN
INSERT INTO @ResultTable
	
	select TOP(5) DiscordUserName, AmountKeeper from Users
	where (Id = @UserId or @UserId = 0)
	order by AmountKeeper desc

RETURN
END
GO
/****** Object:  UserDefinedFunction [dbo].[udf_GetLastLogs]    Script Date: 1/15/2021 7:24:11 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE FUNCTION [dbo].[udf_GetLastLogs] (@NumLogs int)
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
GO
/****** Object:  UserDefinedFunction [dbo].[udf_GetUserActivity]    Script Date: 1/15/2021 7:24:11 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE FUNCTION [dbo].[udf_GetUserActivity] (@UserId int, @AllUsers bit, @TimeRangeBottom datetime, @TimeRangeTop datetime)
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
GO
/****** Object:  UserDefinedFunction [dbo].[udf_GetUserTotalMessages]    Script Date: 1/15/2021 7:24:11 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE FUNCTION [dbo].[udf_GetUserTotalMessages] (@UserId int, @AllUsers bit, @TimeRangeBottom datetime, @TimeRangeTop datetime)
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
GO
/****** Object:  UserDefinedFunction [dbo].[udf_GetUserTotalVoiceTime]    Script Date: 1/15/2021 7:24:11 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE FUNCTION [dbo].[udf_GetUserTotalVoiceTime] (@UserId int, @AllUsers bit, @TimeRangeBottom datetime, @TimeRangeTop datetime)
RETURNS @ResultTable TABLE ([UserName] VARCHAR(200), TotalSecondsInVoice int)
AS BEGIN
INSERT INTO @ResultTable
	
	select [User].DiscordUserName, SUM([Stats].TimeInChannel)
	from dbo.VoiceStatistics [Stats]
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
GO
/****** Object:  Table [dbo].[ActivityLog]    Script Date: 1/15/2021 7:24:11 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ActivityLog](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[TypeId] [int] NOT NULL,
	[Information] [varchar](100) NOT NULL,
	[CalledByUserId] [int] NOT NULL,
	[CallTime] [datetime] NOT NULL,
	[ResultText] [varchar](200) NULL,
	[Success] [bit] NOT NULL,
 CONSTRAINT [PK_CommandLog] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ActivityTypes]    Script Date: 1/15/2021 7:24:11 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ActivityTypes](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [varchar](50) NOT NULL,
 CONSTRAINT [PK_ActivityTypes] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppState]    Script Date: 1/15/2021 7:24:11 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppState](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[LastHelloUserId] [int] NULL,
	[LastHelloMessage] [datetime] NULL,
	[HelloDeleted] [bit] NOT NULL,
	[HelloChannelId] [int] NOT NULL,
	[StatisticsPeriodStart] [datetime] NULL,
	[HelloIteration] [int] NOT NULL,
	[AutoCreateNewHello] [bit] NOT NULL,
	[HelloTimerNotified] [bit] NULL,
	[KeeperUserId] [int] NOT NULL,
	[SuspendedUserId] [int] NOT NULL,
	[HelloTopic] [nvarchar](max) NOT NULL,
	[JoinMuteMinutes] [int] NOT NULL,
 CONSTRAINT [PK_AppState] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ChannelBlacklist]    Script Date: 1/15/2021 7:24:11 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ChannelBlacklist](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ChannelId] [int] NOT NULL,
 CONSTRAINT [PK_ChannelBlacklist] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Channels]    Script Date: 1/15/2021 7:24:11 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Channels](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Type] [smallint] NOT NULL,
	[DiscordChannelId] [varbinary](8) NOT NULL,
	[DiscordChannelName] [varchar](200) NOT NULL,
	[LastActive] [datetime] NOT NULL,
	[Deleted] [bit] NOT NULL,
 CONSTRAINT [PK_Channels] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ChannelTypes]    Script Date: 1/15/2021 7:24:11 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ChannelTypes](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [varchar](50) NOT NULL,
 CONSTRAINT [PK_ChannelTypes] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Greetings]    Script Date: 1/15/2021 7:24:11 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Greetings](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[UserId] [int] NOT NULL,
	[Greeting] [varchar](200) NOT NULL,
	[Iteration] [int] NOT NULL,
 CONSTRAINT [PK_Greetings] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[MessageStatistics]    Script Date: 1/15/2021 7:24:11 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MessageStatistics](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[UserId] [int] NOT NULL,
	[MessagesSent] [int] NOT NULL,
	[ChannelId] [int] NOT NULL,
	[LastSent] [datetime] NULL,
	[TimePeriod] [datetime] NULL,
 CONSTRAINT [PK_MessageStatistics] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[SupressedUsers]    Script Date: 1/15/2021 7:24:11 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SupressedUsers](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[UserId] [int] NOT NULL,
	[TimeStarted] [datetime] NULL,
	[MaxTimeSeconds] [int] NOT NULL,
 CONSTRAINT [PK_SupressedUsers] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Users]    Script Date: 1/15/2021 7:24:11 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Users](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[DiscordUserId] [varbinary](8) NULL,
	[DiscordUserName] [varchar](200) NULL,
	[ChainBreaks] [int] NOT NULL,
	[LastActive] [datetime] NULL,
	[AmountKeeper] [int] NOT NULL,
 CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[VoiceStatistics]    Script Date: 1/15/2021 7:24:11 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[VoiceStatistics](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[UserId] [int] NOT NULL,
	[TimeInChannel] [int] NOT NULL,
	[ChannelId] [int] NOT NULL,
	[LastInChannel] [datetime] NULL,
	[TimePeriod] [datetime] NULL,
 CONSTRAINT [PK_VoiceStatistics] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[AppState] ADD  DEFAULT ((1)) FOR [HelloDeleted]
GO
ALTER TABLE [dbo].[AppState] ADD  DEFAULT (getdate()) FOR [StatisticsPeriodStart]
GO
ALTER TABLE [dbo].[AppState] ADD  DEFAULT ((0)) FOR [HelloIteration]
GO
ALTER TABLE [dbo].[AppState] ADD  DEFAULT ((0)) FOR [AutoCreateNewHello]
GO
ALTER TABLE [dbo].[AppState] ADD  DEFAULT ((0)) FOR [KeeperUserId]
GO
ALTER TABLE [dbo].[AppState] ADD  DEFAULT ((0)) FOR [SuspendedUserId]
GO
ALTER TABLE [dbo].[AppState] ADD  DEFAULT ('') FOR [HelloTopic]
GO
ALTER TABLE [dbo].[AppState] ADD  DEFAULT ((30)) FOR [JoinMuteMinutes]
GO
ALTER TABLE [dbo].[Channels] ADD  DEFAULT ((0)) FOR [Deleted]
GO
ALTER TABLE [dbo].[MessageStatistics] ADD  DEFAULT ((0)) FOR [MessagesSent]
GO
ALTER TABLE [dbo].[Users] ADD  DEFAULT ((0)) FOR [AmountKeeper]
GO
ALTER TABLE [dbo].[VoiceStatistics] ADD  DEFAULT ((0)) FOR [TimeInChannel]
GO
