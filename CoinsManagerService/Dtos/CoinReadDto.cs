
namespace CoinsManagerService.Dtos
{
    public class CoinReadDto
    {        
        public int Id { get; set; }
        public string Nominal { get; set; }
        public string Currency { get; set; }
        public string Year { get; set; }
        public int Type { get; set; }
        public string CommemorativeName { get; set; }
        public int Period { get; set; }
        public string PictPreviewPath { get; set; }
    }
}
