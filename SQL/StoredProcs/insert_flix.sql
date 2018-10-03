IF EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_insert_flix]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[usp_insert_flix]
GO

CREATE PROCEDURE [dbo].[usp_insert_flix]
(
	@Tag			VARCHAR(20),
	@Details		VARCHAR(100),
	@FlixTitle		VARCHAR(100),
	@FlixGenre		VARCHAR(30),
	@FlixRating		VARCHAR(10)
)
AS
BEGIN
	INSERT INTO [dbo].[TrunkedModel]
	(
		Tag,
		Details,
		FlixTitle,
		FlixGenre,
		FlixRating,
		DateAdded
	)
	VALUES
	(
		@Tag,
		@Details,
		@FlixTitle,
		@FlixGenre,
		@FlixRating,
		GETUTCDATE()
	)
END

GO