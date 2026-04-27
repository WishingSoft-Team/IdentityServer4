// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityServer.UnitTests.Common;
using IdentityServer4.Services;
using Xunit;

namespace IdentityServer.UnitTests.Services.Default
{
    public class OidcReturnUrlParserTests
    {
        private readonly OidcReturnUrlParser _subject;

        public OidcReturnUrlParserTests()
        {
            _subject = new OidcReturnUrlParser(null, null, TestLogger.Create<OidcReturnUrlParser>());
        }

        [Theory]
        [InlineData("/connect/authorize", true)]
        [InlineData("/connect/authorize?client_id=client", true)]
        [InlineData("/core/connect/authorize", true)]
        [InlineData("/core/connect/authorize/callback?client_id=client", true)]
        [InlineData("~/connect/authorize", true)]
        [InlineData("/%5c%5cevil.example/connect/authorize", false)]
        [InlineData("/%2f%2fevil.example/connect/authorize", false)]
        [InlineData("/connect/authorize#//evil.example", false)]
        [InlineData("/foo/bar", false)]
        [InlineData("https://evil.example/connect/authorize", false)]
        public void IsValidReturnUrl_validation(string returnUrl, bool expected)
        {
            var result = _subject.IsValidReturnUrl(returnUrl);

            Assert.Equal(expected, result);
        }
    }
}
