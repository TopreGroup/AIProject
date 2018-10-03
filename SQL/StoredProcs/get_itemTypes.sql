IF EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_get_itemTypes]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[usp_get_itemTypes]
GO

CREATE PROCEDURE [dbo].[usp_get_itemTypes]
AS
BEGIN
	SELECT DISTINCT Tag FROM [dbo].[TrunkedModel]
END

GO