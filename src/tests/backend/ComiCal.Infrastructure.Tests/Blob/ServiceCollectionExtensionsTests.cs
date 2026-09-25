using ComiCal.Infrastructure.Blob;
using Xunit;

namespace ComiCal.Infrastructure.Tests.Blob;

public sealed class ServiceCollectionExtensionsTests
{
    [Theory]
    [InlineData("UseDevelopmentStorage=true")]
    [InlineData("DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=test;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;")]
    [InlineData("http://127.0.0.1:10000/devstoreaccount1")]
    public void RequiresTokenCredential_LocalStorage_ReturnsFalse(string storageAccountUri)
    {
        Assert.False(ServiceCollectionExtensions.RequiresTokenCredential(storageAccountUri));
    }

    [Fact]
    public void RequiresTokenCredential_AzureHttpsEndpoint_ReturnsTrue()
    {
        Assert.True(ServiceCollectionExtensions.RequiresTokenCredential(
            "https://cmcldevjpest.blob.core.windows.net"));
    }
}
