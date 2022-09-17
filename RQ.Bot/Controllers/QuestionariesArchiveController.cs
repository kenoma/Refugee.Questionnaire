using System.ComponentModel.DataAnnotations;
using Bot.Repo;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

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
        private readonly TelegramBotClient _botClient;

        /// <inheritdoc />
        public QuestionariesArchiveController(IRepository repo, ILogger<QuestionariesArchiveController> logger,
            TelegramBotClient botClient)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _botClient = botClient ?? throw new ArgumentNullException(nameof(botClient));
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
        public IActionResult GetRecords([FromHeader(Name = "X-Volunteer-Token")] string token)
        {
            _logger.LogTrace("Someone requested {Method} with {Token}", nameof(GetRecords), token);

            if (!_repo.IsKnownToken(token))
            {
                return Unauthorized();
            }

            var records = _repo
                .GetAllRequests()
                .OrderBy(z => z.TimeStamp);

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
        public IActionResult GetUsers([FromHeader(Name = "X-Volunteer-Token")] string token)
        {
            _logger.LogTrace("Someone requested {Method} with {Token}", nameof(GetUsers), token);

            if (!_repo.IsKnownToken(token))
            {
                return Unauthorized();
            }

            var records = _repo.GetAllUsers()
                .OrderBy(z => z.Created);

            return Ok(records);
        }

        /// <summary>
        ///     Возвращает записи из таблицы от даты
        /// </summary>
        /// <returns></returns>
        /// <response code="200">Список анкет</response>
        /// <response code="401">Не передан токен для доступа</response>
        [HttpGet("refrequestDt")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult GetRecordsDate([FromHeader(Name = "X-Volunteer-Token")] string token,
            [Required(ErrorMessage = "Установите дату")]
            DateTime? dt)
        {
            _logger.LogTrace("Someone requested {Method} with {Token}", nameof(GetRecordsDate), token);

            if (!_repo.IsKnownToken(token))
            {
                return Unauthorized();
            }

            if (dt == null)
                return BadRequest("Установите дату");

            var records = _repo.GetRequestsDt(dt.Value);

            return Ok(records);
        }

        /// <summary>
        ///     Возвращает записи из архива от даты
        /// </summary>
        /// <returns></returns>
        /// <response code="200">Список анкет</response>
        /// <response code="401">Не передан токен для доступа</response>
        [HttpGet("refrequestDtArch")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult GetRecordsDateArch([FromHeader(Name = "X-Volunteer-Token")] string token,
            [Required(ErrorMessage = "Установите дату")]
            DateTime? dt)
        {
            _logger.LogTrace("Someone requested {Method} with {Token}", nameof(GetRecordsDateArch), token);

            if (!_repo.IsKnownToken(token))
            {
                return Unauthorized();
            }

            if (dt == null)
                return BadRequest("Установите дату");

            var records = _repo.GetRequestsDtArch(dt.Value);

            return Ok(records);
        }

        /// <summary>
        ///     Возвращает записи пользователей из хранилища бота по дате
        /// </summary>
        /// <returns></returns>
        /// <response code="200">Список пользователей</response>
        /// <response code="401">Не передан токен для доступа</response>
        [HttpGet("usersdt")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult GetUsersDate([FromHeader(Name = "X-Volunteer-Token")] string token,
            [Required(ErrorMessage = "Установите дату")]
            DateTime? dt)
        {
            _logger.LogTrace("Someone requested {Method} with {Token}", nameof(GetUsersDate), token);

            if (!_repo.IsKnownToken(token))
            {
                return Unauthorized();
            }

            if (dt == null)
                return BadRequest("Установите дату");

            var records = _repo.GetUsersDt(dt.Value);

            return Ok(records);
        }

        /// <summary>
        ///     Отправляет сообщение пользователю
        /// </summary>
        /// <returns></returns>
        /// <response code="200">Сообщение отправлено</response>
        /// <response code="401">Не передан токен для доступа</response>
        /// <response code="400">Произошла ошибка</response>
        [HttpPost("sendmtu")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SendMessageToUser([FromHeader(Name = "X-Volunteer-Token")] string token,
            [Required(ErrorMessage = "Укажите пользоватея")]
            long userId,
            [Required(ErrorMessage = "Укажите текст сообщения"), FromBody]
            string message)
        {
            _logger.LogTrace("Someone requested {Method} with {Token}", nameof(SendMessageToUser), token);

            if (!_repo.IsKnownToken(token))
            {
                return Unauthorized();
            }

            try
            {
                var chat = await _botClient.GetChatAsync(userId);
                
                await _botClient.SendTextMessageAsync(
                    chatId: chat.Id,
                    parseMode: ParseMode.Html,
                    text: message,
                    disableWebPagePreview: false
                );
            }
            catch (Exception e)
            {
                _logger.LogWarning("Failed to send message to {UserId} : {Reason}", userId, e.Message);
                return BadRequest();
            }

            return Ok();
        }
    }
}