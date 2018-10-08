IF EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_insert_dvd]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[usp_insert_dvd]
GO

CREATE PROCEDURE [dbo].[usp_insert_dvd]
(
	@Tag			VARCHAR(20),
	@Details		VARCHAR(100) = NULL,
	@DVDTitle		VARCHAR(100),
	@DVDGenre		VARCHAR(30),
	@DVDRating		VARCHAR(10)
)
AS
BEGIN
	INSERT INTO [dbo].[TrunkedInventory]
	(
		Tag,
		Details,
		DVDTitle,
		DVDGenre,
		DVDRating,
		DateAdded
	)
	VALUES
	(
		@Tag,
		@Details,
		@DVDTitle,
		@DVDGenre,
		@DVDRating,
		GETUTCDATE()
	)
END

GO