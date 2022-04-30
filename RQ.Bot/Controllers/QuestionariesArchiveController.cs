using System.IO;
using System.Threading.Tasks;
using CvLab.TelegramBot.Misc;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

using Serilog;

using Telegram.Bot.Types;

namespace CvLab.TelegramBot.Controllers
{
    /// <inheritdoc />
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class QuestionariesArchiveController : Controller
    {
        
        /// <inheritdoc />
        public QuestionariesArchiveController()
        {
        }

        /// <summary>
        ///     Возвращает все записи из хранилища бота
        /// </summary>
        /// <returns></returns>
        /// <response code="200">Список анкет воз</response>
        /// <response code="401">Не передан токен для доступа</response>
        [HttpGet("recs")]//HttpHeader("X-Gitlab-Event", "Push Hook")
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult GetRecords()
        {
            if (!(HttpContext?.Request?.Headers?.TryGetValue("X-Gitlab-Token", out var token) ?? false))
            {
                return BadRequest();
            }
            
            return Ok();
        }
    }
}
