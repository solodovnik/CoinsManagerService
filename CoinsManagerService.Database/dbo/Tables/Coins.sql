CREATE TABLE [dbo].[Coins]
(
	[ID] INT NOT NULL PRIMARY KEY IDENTITY, 
    [Nominal] NVARCHAR(50) NOT NULL, 
    [Currency] NVARCHAR(MAX) NOT NULL, 
    [Year] NVARCHAR(50) NOT NULL, 
    [Type] INT NOT NULL, 
    [CommemorativeName] NVARCHAR(MAX) NULL, 
    [Period] INT NOT NULL, 
    [PictPreviewPath] NVARCHAR(MAX) NULL, 
    [CatalogId] NVARCHAR(50) NULL, 
    CONSTRAINT [FK_Coins_Types] FOREIGN KEY ([Type]) REFERENCES [CoinTypes]([ID]), 
    CONSTRAINT [FK_Coins_Periods] FOREIGN KEY ([Period]) REFERENCES [Periods]([ID]) 
)
