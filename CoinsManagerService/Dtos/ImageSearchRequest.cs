using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace CoinsManagerService.Dtos
{
    public class ImageSearchRequest
    {
        [Required]
        public IFormFile Obverse { get; set; }

        [Required]
        public IFormFile Reverse { get; set; }
        [Required]
        public int topCount { get; set; }
    }
}
