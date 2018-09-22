CREATE TABLE TrunkedModel (
    ID int IDENTITY (1, 1) PRIMARY KEY,
    Tag VARCHAR (20) NOT NULL,
    Details VARCHAR NOT NULL,
    ISBN int,
    Author VARCHAR,
    Genre VARCHAR,
    DateAdded TIMESTAMP
);