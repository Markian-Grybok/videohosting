using MediatR;
using Microsoft.AspNetCore.Mvc;
using FileService.Features.Streaming.Queries;
using FileService.Features.Streaming.Dtos;

namespace FileService.Features.Streaming
{
    [ApiController]
    [Route("api/files")]
    public class StreamingController : ControllerBase
    {
        private readonly IMediator _mediator;

        public StreamingController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> GetFileList(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            var query = new GetFileListQuery(page, pageSize);
            var result = await _mediator.Send(query, ct);
            return Ok(result);
        }

        [HttpGet("{fileId}")]
        public async Task<IActionResult> GetFileStatus(Guid fileId, CancellationToken ct = default)
        {
            var query = new GetFileStatusQuery(fileId);
            var result = await _mediator.Send(query, ct);
            
            if (result == null)
                return NotFound();
            
            return Ok(result);
        }

        [HttpGet("{fileId}/play/{quality?}")]
        public async Task<IActionResult> GetPlaybackUrl(
            Guid fileId,
            string? quality,
            CancellationToken ct = default)
        {
            try
            {
                var query = new GetPlaybackUrlQuery(fileId, quality);
                var result = await _mediator.Send(query, ct);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{fileId}/qualities")]
        public async Task<IActionResult> GetAvailableQualities(
            Guid fileId,
            CancellationToken ct = default)
        {
            try
            {
                var query = new GetAvailableQualitiesQuery(fileId);
                var result = await _mediator.Send(query, ct);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{fileId:guid}")]
        public async Task<IActionResult> DeleteFile(
            Guid fileId,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new FileService.Features.Delete.DeleteVideoCommand(fileId), cancellationToken);

            if (!result.Success)
                return StatusCode(500, new { error = result.Error });

            return NoContent(); // 204
        }
    }
}
