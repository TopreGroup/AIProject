USE TrunkedDevDB
GO

IF EXISTS (
    SELECT 1 
    FROM sys.objects
    WHERE object_id = OBJECT_ID(N'[dbo].[usp_music_insert]')
    AND type  in (N'P', N'PC')
    )
    DROP PROCEDURE [dbo].[usp_music_insert]
GO


CREATE PROCEDURE [dbo].[usp_music_insert]
@Tag varchar(20), @Details varchar, @MusicTitle varchar, 
@Musician varchar, @MusicGenre varchar
AS
BEGIN
INSERT INTO [dbo].[TrunkedModel] (Tag, Details, MusicTitle, Musician, MusicGenre)
VALUES  (@Tag, @Details, @MusicTitle, @Musician, @MusicGenre)
END

GO