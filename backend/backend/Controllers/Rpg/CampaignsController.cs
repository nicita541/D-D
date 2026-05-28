using backend.Contracts.Rpg.Campaigns;
using backend.Contracts.Rpg.Common;
using backend.Services.Rpg;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers.Rpg;

[ApiController]
[Route("api/campaigns")]
public sealed class CampaignsController : ControllerBase
{
    private readonly ICampaignService _campaigns;

    public CampaignsController(ICampaignService campaigns)
    {
        _campaigns = campaigns;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> GetCampaigns(CancellationToken cancellationToken)
    {
        return Ok(await _campaigns.GetCampaignTemplatesAsync(cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetCampaign(Guid id, CancellationToken cancellationToken)
    {
        var campaign = await _campaigns.GetCampaignTemplateAsync(id, cancellationToken);
        return campaign.HasValue ? Ok(campaign.Value) : NotFound(new MessageResponse { Message = "Кампания не найдена" });
    }

    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(OperationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OperationResponse>> CreateCampaign([FromBody] CreateCampaignTemplateRequest request, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new MessageResponse { Message = "Нужно указать название кампании: название" });
        }

        var id = await _campaigns.CreateCampaignTemplateAsync(request, cancellationToken);
        var response = new OperationResponse { Id = id, Message = "Кампания создана" };
        return CreatedAtAction(nameof(GetCampaign), new { id }, response);
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(OperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OperationResponse>> DeleteCampaign(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _campaigns.DeleteCampaignTemplateAsync(id, cancellationToken);
        return deleted
            ? Ok(new OperationResponse { Id = id, Message = "Кампания удалена" })
            : NotFound(new MessageResponse { Message = "Кампания не найдена" });
    }
}
