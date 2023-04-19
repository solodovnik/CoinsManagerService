CREATE TABLE [dbo].[Countries]
(
	[ID] INT NOT NULL PRIMARY KEY IDENTITY, 
    [Country] NVARCHAR(MAX) NOT NULL, 
    [Continent] INT NOT NULL, 
    CONSTRAINT [FK_Countries_Continents] FOREIGN KEY ([Continent]) REFERENCES [Continents]([ID])
)
