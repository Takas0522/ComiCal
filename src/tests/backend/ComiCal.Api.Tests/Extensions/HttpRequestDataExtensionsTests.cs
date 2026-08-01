using ComiCal.Api.Extensions;
using ComiCal.Api.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using NSubstitute;
using System.Security.Claims;
using Xunit;

namespace ComiCal.Api.Tests.Extensions;

public sealed class HttpRequestDataExtensionsTests
{
    [Fact]
    public void GetQueryParam_WhenValueContainsLiteralPlus_PreservesPlus()
    {
        var request = new TestHttpRequestData(new Uri("https://example.test/api?q=C++"));

        var result = request.GetQueryParam("q");

        Assert.Equal("C++", result);
    }

    [Fact]
    public void GetQueryParam_WhenQIsUrlEncoded_ReturnsJsonThatKeywordParserCanParse()
    {
        var request = new TestHttpRequestData(new Uri("https://example.test/api?q=%5B%22C%2B%2B%22%5D"));

        var result = KeywordQueryParser.Parse(request.GetQueryParam("q"));

        Assert.True(result.IsValid);
        Assert.Equal(["C++"], result.Keywords);
    }

    private sealed class TestHttpRequestData(Uri url)
        : HttpRequestData(Substitute.For<FunctionContext>())
    {
        public override Stream Body => Stream.Null;

        public override HttpHeadersCollection Headers { get; } = [];

        public override IReadOnlyCollection<IHttpCookie> Cookies => [];

        public override Uri Url { get; } = url;

        public override IEnumerable<ClaimsIdentity> Identities => [];

        public override string Method => "GET";

        public override HttpResponseData CreateResponse() => throw new NotSupportedException();
    }
}
