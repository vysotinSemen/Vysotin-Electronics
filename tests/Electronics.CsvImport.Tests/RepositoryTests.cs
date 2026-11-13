namespace Electronics.CsvImport.Tests;

using Electronics.CsvImport.Models;
using Electronics.CsvImport.Services;

public sealed class RepositoryTests
{
    private static Product Item(string sku = "VS-001") => new() { Sku = sku, Name = "Учебный товар" };

    [Fact]
    public void NewRepository_IsEmpty()
    {
        var repository = new InMemoryProductRepository();
        Assert.Empty(repository.GetAll());
        Assert.Equal(0, repository.Count);
        Assert.Null(repository.GetBySku("missing"));
    }

    [Fact]
    public void TryAdd_StoresProductAndNormalizesSku()
    {
        var repository = new InMemoryProductRepository();
        Assert.True(repository.TryAdd(Item("  Vs-001  ")));
        Assert.Equal("Vs-001", Assert.Single(repository.GetAll()).Sku);
        Assert.Equal("Учебный товар", repository.GetBySku(" vs-001 ")?.Name);
        Assert.Equal(1, repository.Count);
    }

    [Fact]
    public void Duplicate_DoesNotReplaceFirstProduct()
    {
        var repository = new InMemoryProductRepository();
        repository.TryAdd(Item());
        Assert.False(repository.TryAdd(Item(" vs-001 ") with { Name = "Другой" }));
        Assert.Equal("Учебный товар", repository.GetBySku("VS-001")?.Name);
        Assert.Equal(1, repository.Count);
    }

    [Fact]
    public void GetAll_IsReadOnlySnapshot()
    {
        var repository = new InMemoryProductRepository();
        repository.TryAdd(Item());
        var snapshot = repository.GetAll();
        repository.TryAdd(Item("VS-002"));
        Assert.Single(snapshot);
        Assert.Equal(2, repository.Count);
        Assert.Throws<NotSupportedException>(() => ((IList<Product>)snapshot).Add(Item()));
    }

    [Fact]
    public void ConcurrentSameSku_IsAddedExactlyOnce()
    {
        var repository = new InMemoryProductRepository();
        var successes = 0;
        Parallel.For(0, 100, i =>
        {
            if (repository.TryAdd(Item(i % 2 == 0 ? " VS-001 " : "vs-001")))
                Interlocked.Increment(ref successes);
        });
        Assert.Equal(1, successes);
        Assert.Equal(1, repository.Count);
    }

    [Fact]
    public void ConcurrentDistinctSkus_AreAllStored()
    {
        var repository = new InMemoryProductRepository();
        Parallel.For(0, 100, i => repository.TryAdd(Item($"VS-{i}")));
        Assert.Equal(100, repository.Count);
    }

    [Fact]
    public void InvalidProduct_IsRejectedAtRepositoryBoundary()
    {
        var repository = new InMemoryProductRepository();
        Assert.Throws<ArgumentNullException>(() => repository.TryAdd(null!));
        Assert.Throws<ArgumentException>(() => repository.TryAdd(Item(" ")));
        Assert.Null(repository.GetBySku(" "));
    }
}
