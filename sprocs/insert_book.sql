USE TrunkedDevDB
GO

IF EXISTS (
    SELECT 1 
    FROM sys.procedures)
    WHERE Name = 'usp_book_insert'
    )
    DROP PROCEDURE dbo.usp_book_insert
GO


CREATE PROCEDURE [dbo].[usp_book_insert]
@Tag varchar(20), @Details varchar, @ISBN varchar, 
@BookTitle varchar, @BookAuthor varchar, @BookPublisher varchar
AS
BEGIN
INSERT INTO [dbo].[TrunkedModel] (Tag, Details, ISBN, Booktitle, BookAuthor, BookPublisher)
VALUES  (@Tag, @Details, @ISBN, @Booktitle, @BookAuthor, @BookPublisher)
END

GO