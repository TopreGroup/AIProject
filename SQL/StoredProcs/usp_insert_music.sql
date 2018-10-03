IF EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_insert_music]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[usp_insert_music]
GO

CREATE PROCEDURE [dbo].[usp_insert_music]
(
	@Tag			VARCHAR(20), 
	@Details		VARCHAR(100) = NULL, 
	@MusicTitle		VARCHAR(100), 
	@Musician		VARCHAR(100), 
	@MusicGenre		VARCHAR(30)
)
AS
BEGIN
	INSERT INTO [dbo].[TrunkedInventory] 
	(
		Tag, 
		Details, 
		MusicTitle, 
		Musician, 
		MusicGenre,
		DateAdded
	)
	VALUES
	(
		@Tag, 
		@Details, 
		@MusicTitle, 
		@Musician, 
		@MusicGenre,
		GETUTCDATE()
	)
END

GO