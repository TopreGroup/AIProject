IF EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_insert_book]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[usp_insert_book]
GO

CREATE PROCEDURE [dbo].[usp_insert_book]
(
	@Tag				VARCHAR(20), 
	@Details			VARCHAR(100) = NULL, 
	@ISBN				VARCHAR(30), 
	@BookTitle			VARCHAR(100), 
	@BookAuthors		VARCHAR(100), 
	@BookGenre			VARCHAR(30),
	@BookPublisher		VARCHAR(100),
	@BookPublishDate	VARCHAR(10)
)
AS
BEGIN
	INSERT INTO [dbo].[TrunkedInventory] 
	(
		Tag, 
		Details, 
		ISBN, 
		BookTitle, 
		BookAuthors, 
		BookGenre, 
		BookPublisher, 
		BookPublishDate,
		DateAdded
	)
	VALUES
	(
		@Tag, 
		@Details, 
		@ISBN, 
		@BookTitle, 
		@BookAuthors, 
		@BookGenre, 
		@BookPublisher, 
		@BookPublishDate, 
		GETUTCDATE()
	)
END

GO

