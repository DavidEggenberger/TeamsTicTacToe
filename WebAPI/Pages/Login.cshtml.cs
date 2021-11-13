using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.ApplicationUserAggregate;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebAPI.Pages
{
    public class LoginModel : PageModel
    {
        private SignInManager<ApplicationUser> SignInManager;

        [BindProperty(SupportsGet = true)]
        public string ReturnUrl { get; set; }
        public LoginModel(SignInManager<ApplicationUser> signInManager)
        {
            SignInManager = signInManager;
        }
        public ActionResult OnGet(string returnUrl)
        {
            var redirectUrl = Url.Action("ExternalLoginCallback", "Account", new { returnUrl });
            var properties = SignInManager.ConfigureExternalAuthenticationProperties("Microsoft", redirectUrl);
            return Challenge(properties, "Microsoft");
        }
    }
}
