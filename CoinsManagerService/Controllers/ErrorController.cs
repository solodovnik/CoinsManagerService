using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CoinsManagerService.Controllers
{
    [ApiController]
    public class ErrorController : ControllerBase
    {
        [ApiExplorerSettings(IgnoreApi = true)]
        [Route("/error")]
        public IActionResult Error()
        {
            return Problem(
        detail: "lalala",
        title: "tralala");
        }
    }
}
