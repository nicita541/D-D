using backend.Infrastructure.Auth;
using backend.Modules.AccountCharacters.Application;
using backend.Modules.AccountCharacters.Contracts;
using backend.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Modules.AccountCharacters.Api;

[Authorize]
[ApiController]
[Route("api/characters")]
public sealed class AccountCharactersController : ControllerBase
{
    private readonly IAccountCharacterService _characters;
    private readonly ICurrentUserService _currentUser;

    public AccountCharactersController(IAccountCharacterService characters, ICurrentUserService currentUser)
    {
        _characters = characters;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult> List(CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        return Ok(await _characters.GetCharactersAsync(current.AccountId, cancellationToken));
    }

    [HttpPost("generate")]
    public async Task<ActionResult> Generate([FromBody] GenerateAccountCharacterRequest? request, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        var character = await _characters.GenerateCharacterAsync(current.AccountId, request ?? new GenerateAccountCharacterRequest(), cancellationToken);
        return character.HasValue
            ? Ok(character.Value)
            : BadRequest(new MessageResponse { Message = "Не удалось создать героя." });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        var character = await _characters.GetCharacterAsync(current.AccountId, id, cancellationToken);
        return character.HasValue ? Ok(character.Value) : NotFound(new MessageResponse { Message = "Герой не найден." });
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Update(Guid id, [FromBody] UpdateAccountCharacterRequest? request, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        var character = await _characters.UpdateCharacterAsync(current.AccountId, id, request ?? new UpdateAccountCharacterRequest(), cancellationToken);
        return character.HasValue ? Ok(character.Value) : NotFound(new MessageResponse { Message = "Герой не найден." });
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        var deleted = await _characters.DeleteCharacterAsync(current.AccountId, id, cancellationToken);
        return deleted
            ? Ok(new MessageResponse { Message = "Герой удалён." })
            : NotFound(new MessageResponse { Message = "Герой не найден." });
    }
}
