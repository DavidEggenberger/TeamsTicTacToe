using Domain.ApplicationUserAggregate;
using IdentityServer4.Models;
using Infrastructure.Persistance;
using Microsoft.AspNetCore.ApiAuthorization.IdentityServer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace WebAPI
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public IWebHostEnvironment WebHostEnvironment { get; }
        public Startup(IConfiguration configuration, IWebHostEnvironment WebHostEnvironment)
        {
            Configuration = configuration;
            this.WebHostEnvironment = WebHostEnvironment;
        }
    
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = null;
            });
            services.AddRazorPages();
            services.AddSignalR();

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlServer(Configuration["AzureSQLConnection"]);
            });

            #region Authenentication Setup
            services.AddAuthentication(options =>
            {
                options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
                options.DefaultChallengeScheme = IdentityServerJwtConstants.IdentityServerJwtBearerScheme;
                options.DefaultAuthenticateScheme = "ApplicationDefinedAuthentication";
            })
               .AddIdentityServerJwt()
               .AddMicrosoftAccount(options =>
               {
                   options.ClientId = Configuration["AzureKeyVaultMicrosoftId"];
                   options.ClientSecret = Configuration["AzureKeyVaultMicrosoftSecret"];
                   options.Scope.Add("Chat.ReadWrite");
                   options.Scope.Add("Team.ReadBasic.All");
                   options.Scope.Add("offline_access");
                   options.SaveTokens = true;
                   options.Events.OnCreatingTicket += async context =>
                   {
                       using HttpClient htp = context.HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>().CreateClient("MicrosoftGraph");
                       htp.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);
                       var stringg = await htp.GetStringAsync("me/joinedTeams");
                   };
               })
               .AddPolicyScheme("ApplicationDefinedAuthentication", null, options =>
               {
                   options.ForwardDefaultSelector = (context) =>
                   {
                       if (context.Request.Path.StartsWithSegments(new PathString("/api"), StringComparison.OrdinalIgnoreCase))
                           return IdentityServerJwtConstants.IdentityServerJwtScheme;
                       else
                           return IdentityConstants.ApplicationScheme;
                   };
               })
               .AddIdentityCookies(options =>
               {
               });
            services.ConfigureApplicationCookie(config =>
            {
                config.LoginPath = "/Login";
                config.LogoutPath = "/Logout";
            });

            var identityService = services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+/ ";
                options.User.RequireUniqueEmail = false;
                options.Tokens.AuthenticatorIssuer = "JustRoll";
                options.Stores.MaxLengthForKeys = 128;
            })
                .AddDefaultTokenProviders()
                .AddEntityFrameworkStores<ApplicationDbContext>();
            identityService.AddSignInManager();
            #endregion
            #region IdentityServer Registration
            if (WebHostEnvironment.IsDevelopment())
            {
                services.AddIdentityServer()
                .AddApiAuthorization<ApplicationUser, ApplicationDbContext>(options =>
                {
                    options.Clients.Add(new Client
                    {
                        ClientId = "BlazorClient",
                        AllowedGrantTypes = GrantTypes.Code,
                        RequirePkce = true,
                        RequireClientSecret = false,
                        AllowedScopes = new List<string>
                        {
                            "openid",
                            "profile",
                            "API"
                        },
                        RedirectUris = { "https://localhost:44389/authentication/login-callback" },
                        PostLogoutRedirectUris = { "https://localhost:44389" },
                        FrontChannelLogoutUri = "https://localhost:44389"
                    });
                    options.ApiResources = new ApiResourceCollection
                    {
                        new ApiResource
                        {
                            Name = "API",
                            Scopes = new List<string> {"API"}
                        }
                    };
                    var cert = options.SigningCredential = new SigningCredentials(new SymmetricSecurityKey(Encoding.ASCII.GetBytes(Configuration["AzureKeyVaultSigningKey"])), SecurityAlgorithms.HmacSha256);
                });
            }
            if (WebHostEnvironment.IsProduction())
            {
                services.AddIdentityServer(options =>
                {

                })
                .AddApiAuthorization<ApplicationUser, ApplicationDbContext>(options =>
                {
                    options.Clients.Add(new Client
                    {
                        ClientId = "BlazorClient",
                        AllowedGrantTypes = GrantTypes.Code,
                        RequirePkce = true,
                        RequireClientSecret = false,
                        AllowedScopes = new List<string>
                        {
                            "openid",
                            "profile",
                            "API"
                        },
                        RedirectUris =
                            {
                                "https://snorkelsg.azurewebsites.net/authentication/login-callback"
                            },
                        PostLogoutRedirectUris =
                            {
                                "https://snorkelsg.azurewebsites.net"
                            },
                        FrontChannelLogoutUri = "https://snorkelsg.azurewebsites.net"
                    });
                    options.ApiResources = new ApiResourceCollection
                    {
                        new ApiResource
                        {
                            Name = "API",
                            Scopes = new List<string> {"API"}
                        }
                    };
                    var cert = options.SigningCredential = new SigningCredentials(new SymmetricSecurityKey(Encoding.ASCII.GetBytes(Configuration["AzureKeyVaultSigningKey"])), SecurityAlgorithms.HmacSha256);
                });
            }
            #endregion          
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseBlazorFrameworkFiles();

            app.UseRouting();

            app.UseIdentityServer();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapRazorPages();
                endpoints.MapFallbackToFile("/index.html");
            });
        }
    }
}
