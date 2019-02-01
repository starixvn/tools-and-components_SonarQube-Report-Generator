CREATE TABLE [dbo].[ProjectExclusion] (
    [Id]          INT            IDENTITY (1, 1) NOT NULL,
    [ProjectKey]  VARCHAR (100)  NOT NULL,
    [ProjectName] VARCHAR (150)  NOT NULL,
    [Exclusions]  VARCHAR (2000) NOT NULL,
    CONSTRAINT [PK_ProjectExclusion] PRIMARY KEY CLUSTERED ([Id] ASC)
);

