IF EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_insert_other]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[usp_insert_other]
GO

CREATE PROCEDURE [dbo].[usp_insert_other]
(
	@Tag			VARCHAR(20), 
	@Details		VARCHAR(100), 
	@Description	VARCHAR(100)
)
AS
BEGIN
	INSERT INTO [dbo].[TrunkedInventory] 
	(
		Tag,
		Details,
		Description,
		DateAdded
	)
	VALUES
	(
		@Tag,
		@Details,
		@Description,
		GETUTCDATE()
	)
END

GO