using ContentService.API.Requests;
using ContentService.Application.Commands.Lesson.CreateLesson;
using ContentService.Application.Commands.Lesson.DeleteLessonl;
using ContentService.Application.Commands.Lesson.UpdateLesson;
using ContentService.Application.Queries.Lesson.GetAllLessons;
using ContentService.Application.Queries.Lesson.GetLessonById;
using ContentService.Application.Responses.Lesson;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ContentService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LessonsController : ControllerBase
{
    private readonly IMediator _mediator;

    public LessonsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<LessonResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetAllLessonsQuery(), cancellationToken);

        if (result.IsFailed)
            return BadRequest(result.Errors.Select(e => e.Message));

        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(LessonResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetLessonByIdQuery(id), cancellationToken);

        if (result.IsFailed)
            return NotFound(result.Errors.Select(e => e.Message));

        return Ok(result.Value);
    }

    [HttpPost]
    [ProducesResponseType(typeof(LessonResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateLessonCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailed)
            return BadRequest(result.Errors.Select(e => e.Message));

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Value.Id },
            result.Value);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(LessonResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateLessonRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateLessonCommand(id, request.Title, request.Description);
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailed)
            return NotFound(result.Errors.Select(e => e.Message));

        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteLessonCommand(id), cancellationToken);

        if (result.IsFailed)
            return NotFound(result.Errors.Select(e => e.Message));

        return NoContent();
    }
}