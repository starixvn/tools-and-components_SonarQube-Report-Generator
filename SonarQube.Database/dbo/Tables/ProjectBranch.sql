CREATE TABLE [dbo].[ProjectBranch] (
    [ProjectKey] VARCHAR (100) NOT NULL,
    [Branch]     VARCHAR (200) NOT NULL,
    [Commit]     VARCHAR (50)  NOT NULL,
    [Author]     VARCHAR (50)  NOT NULL,
    [ScanTime]   DATETIME      NOT NULL,
    CONSTRAINT [PK_ProjectBranch] PRIMARY KEY CLUSTERED ([ProjectKey] ASC)
);

