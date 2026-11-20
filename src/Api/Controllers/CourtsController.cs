using Application.Auth.Constants;
using Application.Courts.UseCases;
using Application.Reservations.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;
public sealed record CreateCourtRequest(string Name);
public sealed record UploadCourtImageRequest(IFormFile File);

[ApiController]
[Route("api/courts")]
public class CourtsController : ControllerBase
{
    private readonly CreateCourt _createCourt;
    private readonly GetAvailability _getAvailability;
    private readonly UploadCourtImage _uploadCourtImage;

    public CourtsController(CreateCourt createCourt, GetAvailability getAvailability, UploadCourtImage uploadCourtImage)
    {
        _createCourt = createCourt;
        _getAvailability = getAvailability;
        _uploadCourtImage = uploadCourtImage;
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPost]
    public async Task<IActionResult> Create(CreateCourtRequest request)
    {
        var id = await _createCourt.ExecuteAsync(new CreateCourtCommand(request.Name));

        return Created($"/api/courts/{id}", new { id });
    }

    [AllowAnonymous]
    [HttpGet("{id:guid}/availability")]
    public async Task<IActionResult> GetCourtAvailability(Guid id, [FromQuery] DateOnly? date)
    {
        var query = new GetAvailabilityQuery(id,date);
        var availability = await _getAvailability.ExecuteAsync(query);

        return Ok(availability);
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPost("{id:guid}/images")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> UploadImage(Guid id, [FromForm] UploadCourtImageRequest request)
    {
        await using var stream = request.File.OpenReadStream();

        var command = new UploadCourtImageCommand(id, stream, request.File.ContentType, request.File.Length);

        var imageId = await _uploadCourtImage.ExecuteAsync(command);

        return Created($"/api/courts/{id}/images/{imageId}", new {id = imageId});
    }
}