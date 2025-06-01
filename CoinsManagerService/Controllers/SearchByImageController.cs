using CoinsManagerService.Dtos;
using CoinsManagerService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Threading.Tasks;

namespace CoinsManagerService.Controllers
{
    [Authorize(Roles = "Api.ReadWrite")]
    [ApiController]
    [Route("api")]
    public class SearchByImageController : Controller
    {
        private readonly ICoinSearchService _coinSearchService;

        public SearchByImageController(ICoinSearchService coinSearchService)
        {
            _coinSearchService = coinSearchService;
        }

        [HttpPost("search-by-image")]
        [SwaggerResponse(200)]
        [SwaggerResponse(401)]
        [SwaggerResponse(404)]
        [SwaggerResponse(500)]
        public async Task<ActionResult> SearchByImage([FromForm] ImageSearchRequest request)
        {
            if (request.Obverse == null || request.Reverse == null)
                return BadRequest("Both images are required."); 
            
            var match = await _coinSearchService.FindMatchAsync(request.Obverse, request.Reverse);

            if (match == null)
                return NotFound(new { message = "Coin not found" });

            return Ok(match);
        }
    }
}
