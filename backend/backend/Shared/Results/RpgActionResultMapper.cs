using backend.Contracts.Rpg.Common;
using Microsoft.AspNetCore.Mvc;

namespace backend.Shared.Results;

public static class RpgActionResultMapper
{
    public static ActionResult ToActionResult<T>(this ControllerBase controller, RpgResult<T> result)
        => result.Status switch
        {
            RpgResultStatus.Ok => controller.Ok(result.Value),
            RpgResultStatus.BadRequest => controller.BadRequest(new MessageResponse { Message = result.Message ?? "Bad request." }),
            RpgResultStatus.NotFound => controller.NotFound(new MessageResponse { Message = result.Message ?? "Not found." }),
            RpgResultStatus.Conflict => controller.Conflict(new MessageResponse { Message = result.Message ?? "Conflict." }),
            RpgResultStatus.ServiceUnavailable => controller.StatusCode(StatusCodes.Status503ServiceUnavailable, result.Value is null
                ? new MessageResponse { Message = result.Message ?? "Service unavailable." }
                : result.Value),
            _ => controller.StatusCode(StatusCodes.Status500InternalServerError)
        };
}
