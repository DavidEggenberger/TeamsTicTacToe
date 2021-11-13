using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.ApplicationUserAggregate
{
    public class ApplicationUser : IdentityUser
    {
        public string RefreshToken { get; set; }
        public string AccessToken { get; set; }
    }
}
