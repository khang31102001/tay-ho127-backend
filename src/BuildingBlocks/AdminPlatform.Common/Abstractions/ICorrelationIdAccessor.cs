using AdminPlatform.Common.Web;
using Microsoft.AspNetCore.Http;

namespace AdminPlatform.Common.Abstractions;

public interface ICorrelationIdAccessor
{
    string? CorrelationId { get; }
}

internal sealed class HttpCorrelationIdAccessor : ICorrelationIdAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCorrelationIdAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? CorrelationId =>
        _httpContextAccessor.HttpContext?.Items.TryGetValue(CorrelationIdMiddleware.ItemsKey, out var value) == true
            ? value as string
            : null;
}
