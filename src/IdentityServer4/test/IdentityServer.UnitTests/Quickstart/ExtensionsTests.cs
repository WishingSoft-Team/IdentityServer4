// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServerHost.Quickstart.UI;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace IdentityServer.UnitTests.Quickstart
{
    public class ExtensionsTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void IsValidReturnUrl_should_reject_null_or_whitespace(string returnUrl)
        {
            var controller = CreateController(isLocalUrl: false);
            var interaction = new StubInteractionService(isValidReturnUrl: true);

            var result = controller.IsValidReturnUrl(interaction, returnUrl);

            result.Should().BeFalse();
        }

        [Fact]
        public void RedirectToSafeReturnUrl_should_redirect_home_for_invalid_returnUrl()
        {
            var controller = CreateController(isLocalUrl: false);
            var interaction = new StubInteractionService(isValidReturnUrl: false);

            var result = controller.RedirectToSafeReturnUrl(interaction, "/%2f%2fevil.example");

            var redirect = result.Should().BeOfType<RedirectResult>().Subject;
            redirect.Url.Should().Be("~/");
        }

        [Fact]
        public void RedirectToSafeReturnUrl_should_allow_valid_local_returnUrl()
        {
            var controller = CreateController(isLocalUrl: true);
            var interaction = new StubInteractionService(isValidReturnUrl: false);

            var result = controller.RedirectToSafeReturnUrl(interaction, "/local/page");

            var redirect = result.Should().BeOfType<RedirectResult>().Subject;
            redirect.Url.Should().Be("/local/page");
        }

        [Fact]
        public void RedirectToSafeReturnUrl_should_allow_valid_oidc_returnUrl_for_native_clients()
        {
            var controller = CreateController(isLocalUrl: false);
            var interaction = new StubInteractionService(isValidReturnUrl: true);

            var result = controller.RedirectToSafeReturnUrl(interaction, "/connect/authorize?client_id=client", useLoadingPageForNativeClient: true);

            var view = result.Should().BeOfType<ViewResult>().Subject;
            view.ViewName.Should().Be("Redirect");
            view.Model.Should().BeOfType<RedirectViewModel>().Which.RedirectUrl.Should().Be("/connect/authorize?client_id=client");
            controller.HttpContext.Response.StatusCode.Should().Be(200);
            controller.HttpContext.Response.Headers["Location"].ToString().Should().BeEmpty();
        }

        private static Controller CreateController(bool isLocalUrl)
        {
            var controller = new TestController();
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            controller.Url = new StubUrlHelper(isLocalUrl);
            controller.ViewData = new Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary(
                new EmptyModelMetadataProvider(),
                new ModelStateDictionary());

            return controller;
        }

        private class TestController : Controller
        {
        }

        private class StubUrlHelper : IUrlHelper
        {
            private readonly bool _isLocalUrl;

            public StubUrlHelper(bool isLocalUrl)
            {
                _isLocalUrl = isLocalUrl;
            }

            public ActionContext ActionContext { get; } = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());

            public string Action(UrlActionContext actionContext) => throw new NotSupportedException();

            public string Content(string contentPath) => contentPath;

            public bool IsLocalUrl(string url) => _isLocalUrl;

            public string Link(string routeName, object values) => throw new NotSupportedException();

            public string RouteUrl(UrlRouteContext routeContext) => throw new NotSupportedException();
        }

        private class StubInteractionService : IIdentityServerInteractionService
        {
            private readonly bool _isValidReturnUrl;

            public StubInteractionService(bool isValidReturnUrl)
            {
                _isValidReturnUrl = isValidReturnUrl;
            }

            public Task<AuthorizationRequest> GetAuthorizationContextAsync(string returnUrl) => throw new NotSupportedException();

            public bool IsValidReturnUrl(string returnUrl) => _isValidReturnUrl;

            public Task<ErrorMessage> GetErrorContextAsync(string errorId) => throw new NotSupportedException();

            public Task<LogoutRequest> GetLogoutContextAsync(string logoutId) => throw new NotSupportedException();

            public Task<string> CreateLogoutContextAsync() => throw new NotSupportedException();

            public Task GrantConsentAsync(AuthorizationRequest request, ConsentResponse consent, string subject = null) => throw new NotSupportedException();

            public Task DenyAuthorizationAsync(AuthorizationRequest request, AuthorizationError error, string errorDescription = null) => throw new NotSupportedException();

            public Task<IEnumerable<Grant>> GetAllUserGrantsAsync() => throw new NotSupportedException();

            public Task RevokeUserConsentAsync(string clientId) => throw new NotSupportedException();

            public Task RevokeTokensForCurrentSessionAsync() => throw new NotSupportedException();
        }
    }
}