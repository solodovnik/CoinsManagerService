CREATE TABLE [dbo].[Periods]
(
	[ID] INT NOT NULL PRIMARY KEY IDENTITY, 
    [Period] NVARCHAR(MAX) NOT NULL, 
    [Country] INT NOT NULL, 
    CONSTRAINT [FK_Periods_Countries] FOREIGN KEY ([Country]) REFERENCES [Countries]([ID])
)
