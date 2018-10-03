USE TrunkedDevDB
GO

IF EXISTS (
    SELECT 1 
    FROM sys.objects
    WHERE object_id = OBJECT_ID(N'[dbo].[usp_flix_insert]')
    AND type  in (N'P', N'PC')
    )
    DROP PROCEDURE [dbo].[usp_flix_insert]
GO


CREATE PROCEDURE [dbo].[usp_flix_insert]
@Tag varchar(20), @Details varchar, @FlixTitle varchar, 
@FlixGenre varchar, @FlixRating varchar
AS
BEGIN
INSERT INTO [dbo].[TrunkedModel] (Tag, Details, FlixTitle, FlixGenre, FlixRating)
VALUES  (@Tag, @Details, @FlixTitle, @FlixGenre, @FlixRating)
END

GO