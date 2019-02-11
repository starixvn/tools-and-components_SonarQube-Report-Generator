CREATE TABLE [dbo].[SonarQubeAnalysisExclusion] (
    [Id]         INT            IDENTITY (1, 1) NOT NULL,
    [ProjectKey] VARCHAR (100)  NOT NULL,
    [Key]        VARCHAR (50)   NOT NULL,
    [Values]     VARCHAR (2000) NOT NULL,
    CONSTRAINT [PK_SonarQubeAnalysisExclusion] PRIMARY KEY CLUSTERED ([Id] ASC)
);

