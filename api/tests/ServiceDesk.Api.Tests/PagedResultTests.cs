using ServiceDesk.Api.Contracts;

namespace ServiceDesk.Api.Tests;

public class PagedResultTests
{
    [Theory]
    [InlineData(41, 20, 3)]
    [InlineData(40, 20, 2)]
    [InlineData(0, 20, 0)]
    public void TotalPages_is_rounded_up(int totalCount, int pageSize, int expectedPages)
    {
        var result = new PagedResult<int>([], 1, pageSize, totalCount);
        Assert.Equal(expectedPages, result.TotalPages);
    }
}