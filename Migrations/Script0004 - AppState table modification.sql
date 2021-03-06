ALTER TABLE AppState
ADD ResponseChannelId varbinary(8) null, -- Will be the default channel if none specified
	HelloCategoryId varbinary(8) null,	-- Will be the default category if none specified
	ChainBreakerRoleId varbinary(8) null, -- Feature will not be disabled if the role id is null
	ChainKeeperRoleId varbinary(8) null, -- Feature will not be disabled if the role id is null
	SuppressedRoleId varbinary(8) null, -- Feature will be disabled if the role id is null
	SuspendedRoleId varbinary(8) null, -- Feature will be disabled if the role is is null
	JoinAutoRoleId varbinary(8) null, -- Feature will be disabled if the role id is null
	EnableHelloChain bit not null DEFAULT (1),
	EnableStatisticsTracking bit not null DEFAULT (1),
	EnableMeCommand bit not null DEFAULT (1),
	EnableEmotes bit not null DEFAULT (1);