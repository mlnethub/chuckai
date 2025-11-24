-- Create database
CREATE DATABASE ChuckNorrisJokes;
GO

-- Use the new database
USE ChuckNorrisJokes;
GO

-- Create table with CreatedAt and UpdatedAt columns
CREATE TABLE dbo.ChuckNorrisJokes (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Joke NVARCHAR(500) NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT(GETDATE()),
    UpdatedAt DATETIME NULL
);
GO

-- Insert random jokes (CreatedAt will default to current date/time, UpdatedAt left NULL)
INSERT INTO dbo.ChuckNorrisJokes (Joke) VALUES
(N'Chuck Norris counted to infinity. Twice.'),
(N'Chuck Norris can slam a revolving door.'),
(N'Chuck Norris doesn''t read books. He stares them down until he gets the information he wants.'),
(N'Chuck Norris can divide by zero.'),
(N'Chuck Norris can hear sign language.'),
(N'Chuck Norris beat the sun in a staring contest.'),
(N'Chuck Norris can unscramble an egg.'),
(N'Chuck Norris can kill two stones with one bird.'),
(N'Chuck Norris can do a wheelie on a unicycle.'),
(N'Chuck Norris makes onions cry.');
GO