using MediatR;
using FileService.Features.Streaming.Dtos;

namespace FileService.Features.Streaming.Queries
{
    public record GetAvailableQualitiesQuery(Guid FileId) : IRequest<AvailableQualitiesDto>;
}
