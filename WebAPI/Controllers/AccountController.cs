using Domain.ApplicationUserAggregate;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private SignInManager<ApplicationUser> signInManager;
        private UserManager<ApplicationUser> userManager;
        public AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
        }
        public async Task<IActionResult> ExternalLoginCallback(string ReturnUrl = null)
        {
            var info = await signInManager.GetExternalLoginInfoAsync();
            var user = await userManager.FindByNameAsync(info.Principal.Identity.Name);

            if (info is not null && user is null)
            {
                ApplicationUser _user = new ApplicationUser
                {
                    UserName = info.Principal.Identity.Name,
                    //AccessToken = info.AuthenticationTokens.First(token => token.Name == "access_token").Value,
                };
                var result = await userManager.CreateAsync(_user);
                if (result.Succeeded)
                {
                    result = await userManager.AddLoginAsync(_user, info);
                    await signInManager.SignInAsync(_user, isPersistent: false, info.LoginProvider);
                    return LocalRedirect(ReturnUrl);
                }
            }

            var signInResult = await signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: false);
            return signInResult switch
            {
                Microsoft.AspNetCore.Identity.SignInResult { Succeeded: true } => LocalRedirect(ReturnUrl),
                _ => Redirect("/")
            };
        }
    }
}
