CREATE TABLE [dbo].[GlobalSuppression] (
    [Id]          INT            IDENTITY (1, 1) NOT NULL,
    [ProjectKey]  VARCHAR (100)  NOT NULL,
    [ProjectName] VARCHAR (150)  NOT NULL,
    [RuleName]    VARCHAR (500)  NOT NULL,
    [Scope]       VARCHAR (50)   NOT NULL,
    [Target]      VARCHAR (1000) NULL,
    CONSTRAINT [PK_GlobalSuppression_1] PRIMARY KEY CLUSTERED ([Id] ASC)
);

