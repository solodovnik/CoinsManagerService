using Microsoft.AspNetCore.Http;
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
        public int? Type { get; set; }
         
        public string CommemorativeName { get; set; }

        [Required]
        public int? Period { get; set; }

        [Required]
        public string CatalogId { get; set; }

        [Required]
        public IFormFile ObverseImage { get; set; }

        [Required]
        public IFormFile ReverseImage { get; set; }
    }
}
