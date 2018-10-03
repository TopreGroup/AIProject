IF EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_get_clothingSubTypes]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[usp_get_clothingSubTypes]
GO

CREATE PROCEDURE [dbo].[usp_get_clothingSubTypes]
(
	@ClothingType VARCHAR(20)
)
AS
BEGIN
	SELECT DISTINCT ClothingSubType 
	  FROM [dbo].[TrunkedModel]
	 WHERE ClothingType = @ClothingType
END

GO