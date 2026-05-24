namespace FileService.Features.Streaming.Dtos
{
    public record AvailableQualitiesDto(Guid FileId, List<string> Qualities);
}
