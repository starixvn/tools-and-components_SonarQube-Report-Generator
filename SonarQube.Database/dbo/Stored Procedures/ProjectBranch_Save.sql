CREATE PROCEDURE ProjectBranch_Save
	@ProjectKey [varchar](100),
    @Branch [varchar](200),
    @Commit [varchar](50),
    @Author [varchar](50)    
AS
BEGIN	
	SET NOCOUNT ON;

    IF EXISTS(
        SELECT TOP 1 1 FROM dbo.ProjectBranch AS b WHERE b.ProjectKey = @ProjectKey
    )BEGIN
        UPDATE dbo.ProjectBranch
        SET           
            Branch = @Branch, -- varchar
            [Commit] = @Commit, -- varchar
            Author = @Author, -- varchar
            ScanTime = GETDATE()
        WHERE ProjectKey = @ProjectKey
    END
    ELSE 
    BEGIN
        INSERT INTO dbo.ProjectBranch
        (
            ProjectKey,
            Branch,
            [Commit],
            Author,
            ScanTime
        )
        VALUES
        (
            @ProjectKey, -- ProjectKey - varchar
            @Branch, -- Branch - varchar
            @Commit, -- Commit - varchar
            @Author, -- Author - varchar
            GETDATE() -- ScanTime - datetime
        )
    END
END