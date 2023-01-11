using System.ComponentModel.DataAnnotations;

namespace CoinsManagerService.Dtos
{
    public class CoinCreateDto
    {
        [Required]
        public string Nominal { get; set; }

        [Required]
        public string Currency { get; set; }

        [Required]
        public string Year { get; set; }

        [Required]
        public int Type { get; set; }

        [Required] 
        public string CommemorativeName { get; set; }

        [Required]
        public int Period { get; set; }

        [Required]
        public string PictPreviewPath { get; set; }
    }
}
