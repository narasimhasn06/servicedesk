using Microsoft.EntityFrameworkCore;
using ServiceDesk.Api.Contracts;

namespace ServiceDesk.Api.Infrastructure;

public static class QueryableExtensions
{
    public const int MaxPageSize = 100;

    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);   // nobody can ask for a million rows

        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new PagedResult<T>(items, page, pageSize, total);
    }
}