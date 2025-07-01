using Azure;

namespace ImageService.Tests.Services;

public class TestAsyncPageable<T> : AsyncPageable<T>
{
    private readonly IAsyncEnumerable<T> _items;

    public TestAsyncPageable(IAsyncEnumerable<T> items)
    {
        _items = items;
    }

    public override async IAsyncEnumerable<Page<T>> AsPages(string continuationToken = null, int? pageSizeHint = null)
    {
        var list = new List<T>();
        await foreach (var item in _items)
        {
            list.Add(item);
        }
        yield return Page<T>.FromValues(list, null, null);
    }

    public override IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return _items.GetAsyncEnumerator(cancellationToken);
    }

    public static AsyncPageable<T> Create<T>(IEnumerable<T> items)
    {
        async IAsyncEnumerable<T> GetAsyncEnumerable()
        {
            foreach (var item in items)
            {
                yield return item;
                await Task.Yield();
            }
        }

        return new TestAsyncPageable<T>(GetAsyncEnumerable());
    }
}