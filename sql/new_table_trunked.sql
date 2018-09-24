CREATE TABLE TrunkedModel (
    ID int IDENTITY (1, 1) PRIMARY KEY,
    Tag VARCHAR (20) NOT NULL,
    Details VARCHAR NOT NULL,
    DateAdded DATETIME,
    ISBN VARCHAR,
    BookTitle VARCHAR,
    BookAuthor VARCHAR,
    BookGenre VARCHAR,
    BookPublisher VARCHAR,
    ClothingType VARCHAR,
    ClothingSubType VARCHAR,
    ClothingBrand VARCHAR,
    ClothingSize VARCHAR,
    ClothingColour VARCHAR,
    FlixTitle VARCHAR,
    FlixGenre VARCHAR,
    FlixRating VARCHAR,
    MusicTitle VARCHAR,
    Musician VARCHAR,
    MusicGenre VARCHAR
);