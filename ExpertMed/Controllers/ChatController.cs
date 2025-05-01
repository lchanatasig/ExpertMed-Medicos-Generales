using System.Threading.Tasks;
using ExpertMed.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using static ExpertMed.Services.ChatGPTService;

namespace ExpertMed.Controllers
{
    public class ChatController : Controller
    {
        private readonly ILogger<ChatController> _logger;
        private readonly ChatGPTService _chatService = new ChatGPTService();

        [HttpPost]
        public async Task<IActionResult> Ask([FromBody] AskRequest request)
        {
            if (string.IsNullOrEmpty(request?.Pregunta))
                return BadRequest("La pregunta no puede estar vacía.");

            var respuesta = await _chatService.GetChatGPTResponse(request.Pregunta);
            return Ok(new { respuesta });
        }

        public ChatController(ILogger<ChatController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}
