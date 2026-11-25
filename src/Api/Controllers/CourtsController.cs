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
    private readonly DeleteCourtImage _deleteCourtImage;
    private readonly GetImages _getCourtImages;
    private readonly GetCourts _getCourts;

    public CourtsController(
        CreateCourt createCourt, 
        GetAvailability getAvailability, 
        UploadCourtImage uploadCourtImage, 
        GetImages getCourtImages, 
        DeleteCourtImage deleteCourtImage,
        GetCourts getCourts)
    {
        _createCourt = createCourt;
        _getAvailability = getAvailability;
        _uploadCourtImage = uploadCourtImage;
        _deleteCourtImage = deleteCourtImage;
        _getCourtImages = getCourtImages;
        _getCourts = getCourts;
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPost]
    public async Task<IActionResult> Create(CreateCourtRequest request)
    {
        var id = await _createCourt.ExecuteAsync(new CreateCourtCommand(request.Name));

        return Created($"/api/courts/{id}", new { id });
    }

    [AllowAnonymous]
    [HttpGet()]
    public async Task<IActionResult> GetCourts()
    {
        var courts = await _getCourts.ExecuteAsync();

        return Ok(courts);
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

    [Authorize(Roles = Roles.Admin)]
    [HttpDelete("{id:guid}/images/{imageId:guid}")]
    public async Task<IActionResult> DeleteImage(Guid id, Guid imageId)
    {

        var command = new DeleteCourtImageCommand(imageId ,id);

        await _deleteCourtImage.ExecuteAsync(command);

        return NoContent();
    }

    [AllowAnonymous]
    [HttpGet("{id:guid}/images")]
    public async Task<IActionResult> GetCourtImages(Guid id)
    {
        var query = new GetCourtImagesQuery(id);
        var images = await _getCourtImages.ExecuteAsync(query);

        return Ok(images);
    }
}