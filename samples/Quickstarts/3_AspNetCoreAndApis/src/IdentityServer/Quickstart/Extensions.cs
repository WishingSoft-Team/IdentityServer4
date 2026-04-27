using System;
using IdentityServer4.Models;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdentityServerHost.Quickstart.UI
{
    public static class Extensions
    {
        /// <summary>
        /// Checks if the redirect URI is for a native client.
        /// </summary>
        /// <returns></returns>
        public static bool IsNativeClient(this AuthorizationRequest context)
        {
            return !context.RedirectUri.StartsWith("https", StringComparison.Ordinal)
               && !context.RedirectUri.StartsWith("http", StringComparison.Ordinal);
        }

        public static IActionResult LoadingPage(this Controller controller, string viewName, string redirectUri)
        {
            controller.HttpContext.Response.StatusCode = 200;
            controller.HttpContext.Response.Headers["Location"] = "";
            
            return controller.View(viewName, new RedirectViewModel { RedirectUrl = redirectUri });
        }

        public static bool IsValidReturnUrl(this Controller controller, IIdentityServerInteractionService interaction, string returnUrl)
        {
            if (string.IsNullOrWhiteSpace(returnUrl))
            {
                return false;
            }

            return controller.Url.IsLocalUrl(returnUrl) || interaction.IsValidReturnUrl(returnUrl);
        }

        public static IActionResult RedirectToSafeReturnUrl(this Controller controller, IIdentityServerInteractionService interaction, string returnUrl, bool useLoadingPageForNativeClient = false)
        {
            if (!controller.IsValidReturnUrl(interaction, returnUrl))
            {
                return controller.Redirect("~/");
            }

            if (useLoadingPageForNativeClient)
            {
                return controller.LoadingPage("Redirect", returnUrl);
            }

            return controller.Redirect(returnUrl);
        }
    }
}
