using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using PaperSite.Application.DTOs.Common;
using PaperSite.Infrastructure.Services;

var checks = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception(name);
    checks++;
}
var source = new AsyncQuery<int>(Enumerable.Range(1, 23));
var query = source.OrderBy(x => x);
var first = await query.ToPageAsync(new PaginationRequest(), x => x);
Check(first.Items.SequenceEqual(Enumerable.Range(1, 10)), "First page");
Check(first.TotalCount == 23 && first.TotalPages == 3 && first.HasNextPage && !first.HasPreviousPage, "First metadata");
var json = System.Text.Json.JsonSerializer.Serialize(first, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
using var jsonDocument = System.Text.Json.JsonDocument.Parse(json);
Check(jsonDocument.RootElement.EnumerateObject().Select(x => x.Name).OrderBy(x => x).SequenceEqual(new[] { "items", "pagenumber", "pagesize", "totalcount", "totalpages", "haspreviouspage", "hasnextpage" }.OrderBy(x => x)), "Exact lowercase pagination JSON contract");
Check(jsonDocument.RootElement.GetProperty("pagenumber").GetInt32() == 1 && jsonDocument.RootElement.GetProperty("items").GetArrayLength() == 10, "Lowercase serialized values");
var second = await query.ToPageAsync(new PaginationRequest { PageNumber = 2 }, x => x);
Check(second.Items.SequenceEqual(Enumerable.Range(11, 10)), "Second page");
var last = await query.ToPageAsync(new PaginationRequest { PageNumber = 3 }, x => x);
Check(last.Items.SequenceEqual(Enumerable.Range(21, 3)) && !last.HasNextPage && last.HasPreviousPage, "Partial last page");
var beyond = await query.ToPageAsync(new PaginationRequest { PageNumber = int.MaxValue, PageSize = 100 }, x => x);
Check(beyond.Items.Count == 0 && beyond.TotalCount == 23 && beyond.PageNumber == int.MaxValue, "Overflow-safe page");
foreach (var size in new[] { -1, 0, 101, int.MaxValue })
{
    var normalized = await query.ToPageAsync(new PaginationRequest { PageNumber = -1, PageSize = size }, x => x);
    Check(normalized.PageNumber == 1 && normalized.PageSize == 10 && normalized.Items.Count == 10, "Invalid inputs");
}
var empty = await new AsyncQuery<int>(Array.Empty<int>()).OrderBy(x => x).ToPageAsync(new PaginationRequest(), x => x);
Check(empty.TotalPages == 0 && empty.Items.Count == 0 && !empty.HasNextPage && !empty.HasPreviousPage, "Empty data");
var filtered = await ((IQueryable<int>)source).Where(x => x % 2 == 0).OrderByDescending(x => x).ToPageAsync(new PaginationRequest { PageSize = 3 }, x => x.ToString());
Check(filtered.TotalCount == 11 && filtered.Items.SequenceEqual(new[] { "22", "20", "18" }), "Filter count and sorted mapping");
using var cts = new CancellationTokenSource();
cts.Cancel();
try { await query.ToPageAsync(new PaginationRequest(), x => x, cts.Token); throw new Exception("Cancellation ignored"); }
catch (OperationCanceledException) { checks++; }
Console.WriteLine($"Passed {checks} pagination checks. Uses an async query double; does not exercise a live database.");

sealed class AsyncQuery<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IOrderedQueryable<T>
{
    public AsyncQuery(IEnumerable<T> values) : base(values) { }
    public AsyncQuery(Expression expression) : base(expression) { }
    IQueryProvider IQueryable.Provider => new AsyncProvider(this);
    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) => new AsyncEnumerator<T>(this.AsEnumerable().GetEnumerator(), cancellationToken);
}
sealed class AsyncEnumerator<T>(IEnumerator<T> inner, CancellationToken token) : IAsyncEnumerator<T>
{
    public T Current => inner.Current;
    public ValueTask DisposeAsync() { inner.Dispose(); return ValueTask.CompletedTask; }
    public ValueTask<bool> MoveNextAsync() { token.ThrowIfCancellationRequested(); return ValueTask.FromResult(inner.MoveNext()); }
}
sealed class AsyncProvider(IQueryProvider inner) : IAsyncQueryProvider
{
    public IQueryable CreateQuery(Expression expression) => throw new NotSupportedException();
    public IQueryable<T> CreateQuery<T>(Expression expression) => new AsyncQuery<T>(expression);
    public object? Execute(Expression expression) => inner.Execute(expression);
    public T Execute<T>(Expression expression) => inner.Execute<T>(expression);
    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = inner.Execute(expression);
        return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))!
            .MakeGenericMethod(typeof(TResult).GetGenericArguments()[0]).Invoke(null, new[] { result })!;
    }
}
