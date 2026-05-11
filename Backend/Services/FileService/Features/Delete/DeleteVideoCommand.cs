using MediatR;

namespace FileService.Features.Delete
{
    public record DeleteVideoCommand(Guid FileId) : IRequest<DeleteVideoResult>;
    public record DeleteVideoResult(bool Success, string? Error);
}
