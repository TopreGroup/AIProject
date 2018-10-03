IF EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_get_clothingTypes]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[usp_get_clothingTypes]
GO

CREATE PROCEDURE [dbo].[usp_get_clothingTypes]
AS
BEGIN
	SELECT DISTINCT ClothingType FROM [dbo].[TrunkedInventory]
END

GO