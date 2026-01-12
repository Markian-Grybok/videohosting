using EducationContentService.Core.EndpointsSettings;

namespace EducationContentService.Core.Features.Lessons;

public sealed class CreateEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/lessons", async (CreateHandle handler) =>
        {
            await handler.Handle();
        });
    }
}

public sealed class CreateHandle
{
    public async Task Handle()
    {
        await Task.Delay(TimeSpan.FromSeconds(2));
    }
}
