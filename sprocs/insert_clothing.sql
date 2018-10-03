USE TrunkedDevDB
GO

IF EXISTS (
    SELECT 1 
    FROM sys.objects
    WHERE object_id = OBJECT_ID(N'[dbo].[usp_clothing_insert]')
    AND type  in (N'P', N'PC')
    )
    DROP PROCEDURE [dbo].[usp_clothing_insert]
GO


CREATE PROCEDURE [dbo].[usp_clothing_insert]
@Tag varchar(20), @Details varchar, @ClothingType varchar, 
@ClothingSubType varchar, @ClothingBrand varchar, @ClothingSize varchar, @ClothingColour varchar
AS
BEGIN
INSERT INTO [dbo].[TrunkedModel] (Tag, Details, ClothingType, ClothingSubType, ClothingBrand, ClothingSize, ClothingColour)
VALUES  (@Tag, @Details, @ClothingType, @ClothingSubType, @ClothingBrand, @ClothingSize, @ClothingColour)
END

GO