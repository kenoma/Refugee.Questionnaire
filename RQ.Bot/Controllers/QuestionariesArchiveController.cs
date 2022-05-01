using Bot.Repo;
using Microsoft.AspNetCore.Mvc;

namespace RQ.Bot.Controllers
{
    /// <inheritdoc />
    [ApiController]
    [Route("api")]
    [Produces("application/json")]
    public class QuestionariesArchiveController : Controller
    {
        private readonly IRepository _repo;
        private readonly ILogger<QuestionariesArchiveController> _logger;

        /// <inheritdoc />
        public QuestionariesArchiveController(IRepository repo, ILogger<QuestionariesArchiveController> logger)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        ///     Возвращает все записи из хранилища бота
        /// </summary>
        /// <returns></returns>
        /// <response code="200">Список анкет</response>
        /// <response code="401">Не передан токен для доступа</response>
        [HttpGet("refrequest")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult GetRecords([FromHeader(Name = "X-Volunteer-Token")]string token)
        {
            _logger.LogTrace("Someone requested {Method} with {Token}", nameof(GetRecords), token);
            
            if (!_repo.IsKnownToken(token))
            {
                return Unauthorized();
            }
            
            var records = _repo.GetAllRequest();

            return Ok(records);
        }
        
        /// <summary>
        ///     Возвращает все записи пользователей из хранилища бота
        /// </summary>
        /// <returns></returns>
        /// <response code="200">Список пользователей</response>
        /// <response code="401">Не передан токен для доступа</response>
        [HttpGet("users")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult GetUsers([FromHeader(Name = "X-Volunteer-Token")]string token)
        {
            _logger.LogTrace("Someone requested {Method} with {Token}", nameof(GetRecords), token);
            
            if (!_repo.IsKnownToken(token))
            {
                return Unauthorized();
            }
            
            var records = _repo.GetAllUsers();

            return Ok(records);
        }
    }
}
