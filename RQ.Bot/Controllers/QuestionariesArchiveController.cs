using Microsoft.AspNetCore.Mvc;

namespace RQ.Bot.Controllers
{
    /// <inheritdoc />
    [ApiController]
    [Route("api")]
    [Produces("application/json")]
    public class QuestionariesArchiveController : Controller
    {
        private readonly ILogger<QuestionariesArchiveController> _logger;

        /// <inheritdoc />
        public QuestionariesArchiveController(ILogger<QuestionariesArchiveController> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
            _logger.LogTrace("Someone requested {Method}", nameof(GetRecords));
            if (!(HttpContext?.Request?.Headers?.TryGetValue("X-Gitlab-Token", out var token) ?? false))
            {
                return BadRequest();
            }
            
            return Ok();
        }
    }
}
