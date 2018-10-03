CREATE TABLE TrunkedModel 
(
    ID int IDENTITY (1, 1) PRIMARY KEY,
    Tag VARCHAR (20) NOT NULL,
    Details VARCHAR(100),
    DateAdded DATETIME,
    ISBN VARCHAR(30),
    BookTitle VARCHAR(100),
    BookAuthors VARCHAR(100),
    BookGenre VARCHAR(30),
    BookPublisher VARCHAR(100),
	BookPublishDate VARCHAR(10),
    ClothingType VARCHAR(100),
    ClothingSubType VARCHAR(100),
    ClothingBrand VARCHAR(100),
    ClothingSize VARCHAR(100),
    ClothingColour VARCHAR(100),
    FlixTitle VARCHAR(100),
    FlixGenre VARCHAR(100),
    FlixRating VARCHAR(100),
    MusicTitle VARCHAR(100),
    Musician VARCHAR(100),
    MusicGenre VARCHAR(100)
);