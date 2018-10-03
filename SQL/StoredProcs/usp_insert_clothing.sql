IF EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_insert_clothing]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[usp_insert_clothing]
GO

CREATE PROCEDURE [dbo].[usp_insert_clothing]
(
	@Tag				VARCHAR(20), 
	@Details			VARCHAR(100) = NULL, 
	@ClothingType		VARCHAR(100), 
	@ClothingSubType	VARCHAR(100), 
	@ClothingBrand		VARCHAR(100), 
	@ClothingSize		VARCHAR(10), 
	@ClothingColour		VARCHAR(100)
)
AS
BEGIN
	INSERT INTO [dbo].[TrunkedInventory]
	(
		Tag, 
		Details, 
		ClothingType, 
		ClothingSubType, 
		ClothingBrand, 
		ClothingSize, 
		ClothingColour,
		DateAdded
	)
	VALUES
	(
		@Tag, 
		@Details, 
		@ClothingType, 
		@ClothingSubType, 
		@ClothingBrand, 
		@ClothingSize, 
		@ClothingColour,
		GETUTCDATE()
	)
END

GO