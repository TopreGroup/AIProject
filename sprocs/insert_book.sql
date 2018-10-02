USE TrunkedDevDB
GO

IF EXISTS (
    SELECT 1 
    FROM sys.objects
    WHERE object_id = OBJECT_ID(N'[dbo].[usp_book_insert]')
    AND type  in (N'P', N'PC')
    )
    DROP PROCEDURE [dbo].[usp_book_insert]
GO


CREATE PROCEDURE [dbo].[usp_book_insert]
@Tag varchar(20), @Details varchar, @ISBN varchar, 
@BookTitle varchar, @BookAuthor varchar, @BookPublisher varchar
AS
BEGIN
INSERT INTO [dbo].[TrunkedModel] (Tag, Details, ISBN, Booktitle, BookAuthor, BookPublisher)
VALUES  (@Tag, @Details, @ISBN, @BookTitle, @BookAuthor, @BookPublisher)
END

GO