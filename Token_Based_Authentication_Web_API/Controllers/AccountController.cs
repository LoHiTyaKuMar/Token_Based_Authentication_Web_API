using System; using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http; using System.Security.Claims; using System.Security.Cryptography; using System.Threading.Tasks;
using System.Web;
using System.Web.Helpers;
using System.Web.Http; using Microsoft.AspNet.Identity; using Microsoft.AspNet.Identity.EntityFramework; using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security; using Microsoft.Owin.Security.Cookies; using Microsoft.Owin.Security.OAuth;
using Token_Based_Authentication_Web_API.DatabaseModel;
using Token_Based_Authentication_Web_API.Models;
using Token_Based_Authentication_Web_API.Providers; using Token_Based_Authentication_Web_API.Results;

namespace Token_Based_Authentication_Web_API.Controllers
{
    [Authorize]
    [RoutePrefix("api/Account")]
    public class AccountController : ApiController
    {
        private ApplicationUserManager _userManager;
        public ISecureDataFormat<AuthenticationTicket> AccessTokenFormat { get; private set; }

        public AccountController() {  }

        public AccountController(ApplicationUserManager userManager, ISecureDataFormat<AuthenticationTicket> accessTokenFormat) 
        { UserManager = userManager; AccessTokenFormat = accessTokenFormat; }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? Request.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        /// <summary>
        /// This Method Registers an anonymous user using an email and passowrd
        /// </summary>
        /// <param name="model"></param>
        /// <returns> Sucessful or Unsuccessful Message </returns>
        // POST api/Account/Register
        [AllowAnonymous]
        [Route("Register")]
        public async Task<IHttpActionResult> Register(RegisterBindingModel model)
        {
            if (!ModelState.IsValid && model == null)
            {
                return BadRequest(ModelState);
            }

            var user = new ApplicationUser() { UserName = model.Email, Email = model.Email };

            IdentityResult result = await UserManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }

            return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.OK, "User created successfully!"));
        } 

        /// <summary>
        /// This Method generates a ResetPassword code for User, using which a user can reset the password
        /// </summary>
        /// <param name="model"></param>
        /// <returns> Sucessful or Unsuccessful Message </returns>
        [HttpPost]
        [Route("ForgotPassword")]
        public async Task<IHttpActionResult> ForgotPassword(ForgotPasswordModel model)
        {
            if (ModelState.IsValid && model != null)
            {
                // Requests database for to get user details for provided email address
                using (Token_Based_Authentication_Web_APIEntities _entities = new Token_Based_Authentication_Web_APIEntities())
                {
                    AspNetUser user = _entities.AspNetUsers.Where(x => x.Email == model.Email).FirstOrDefault();

                    // If user not found return with error
                    if (user == null)
                    {
                        return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.NotFound, "User not found!")); 
                    }

                    // Password Reset Token Generation
                    string code = await UserManager.GeneratePasswordResetTokenAsync(user.Id); 
                    // URL with password Token for resetting Password
                    string routeUrl = $"{HttpContext.Current.Request.Url.Scheme}://{Request.GetOwinContext().Request.Host.Value}/api/Account/ResetPassword?resetCode={code}"; 

                    // Saving Password in database for checking on the time of resetting password
                    user.ResetPasswordCode = code;
                    _entities.SaveChanges();

                    return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.OK, $"A password reset code is generated. Please user the following link to reset password. Link : { routeUrl }"));
                }
            }

            // If we got this far, something failed, redisplay form
            return BadRequest(ModelState);
        }

        /// <summary>
        /// This Method Reset the User Password with a new Provided User Password
        /// </summary>
        /// <param name="resetCode"></param>
        /// <param name="newPassword"></param>
        /// <returns> Sucessful or Unsuccessful Message </returns>
        [HttpPost]
        [Route("ResetPassword")]
        public IHttpActionResult ResetPassword(string resetCode, string newPassword)
        {
            if (ModelState.IsValid && !string.IsNullOrEmpty(resetCode) && !string.IsNullOrEmpty(newPassword))
            {
                // Requests database for to get user details for provided email address
                using (Token_Based_Authentication_Web_APIEntities _entities = new Token_Based_Authentication_Web_APIEntities())
                {
                    var user = _entities.AspNetUsers.Where(a => a.ResetPasswordCode == resetCode).FirstOrDefault();

                    // If user not found return with error
                    if (user == null)
                    {
                        return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.NotFound, "User not found!")); 
                    }

                    // If user found then reset the password with new provided password
                    if (user != null)
                    {
                        user.PasswordHash = Crypto.Hash(newPassword);
                        user.ResetPasswordCode = string.Empty;
                        _entities.SaveChanges();

                        return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.OK, "Password reset successful!"));
                    }
                } 
            }

            // If we got this far, something failed, redisplay form
            return BadRequest(ModelState);
        }

        /// <summary>
        /// Disposes the database connection rightafter the manipulation of data processing
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && _userManager != null)
            {
                _userManager.Dispose();
                _userManager = null;
            }

            base.Dispose(disposing);
        }

        #region Helpers

        private IAuthenticationManager Authentication
        {
            get { return Request.GetOwinContext().Authentication; }
        }

        private IHttpActionResult GetErrorResult(IdentityResult result)
        {
            if (result == null)
            {
                return InternalServerError();
            }

            if (!result.Succeeded)
            {
                if (result.Errors != null)
                {
                    foreach (string error in result.Errors)
                    {
                        ModelState.AddModelError("", error);
                    }
                }

                if (ModelState.IsValid)
                {
                    // No ModelState errors are available to send, so just return an empty BadRequest.
                    return BadRequest();
                }

                return BadRequest(ModelState);
            }

            return null;
        }

        private class ExternalLoginData
        {
            public string LoginProvider { get; set; }
            public string ProviderKey { get; set; }
            public string UserName { get; set; }

            public IList<Claim> GetClaims()
            {
                IList<Claim> claims = new List<Claim>();
                claims.Add(new Claim(ClaimTypes.NameIdentifier, ProviderKey, null, LoginProvider));

                if (UserName != null)
                {
                    claims.Add(new Claim(ClaimTypes.Name, UserName, null, LoginProvider));
                }

                return claims;
            }

            public static ExternalLoginData FromIdentity(ClaimsIdentity identity)
            {
                if (identity == null)
                {
                    return null;
                }

                Claim providerKeyClaim = identity.FindFirst(ClaimTypes.NameIdentifier);

                if (providerKeyClaim == null || String.IsNullOrEmpty(providerKeyClaim.Issuer)
                    || String.IsNullOrEmpty(providerKeyClaim.Value))
                {
                    return null;
                }

                if (providerKeyClaim.Issuer == ClaimsIdentity.DefaultIssuer)
                {
                    return null;
                }

                return new ExternalLoginData
                {
                    LoginProvider = providerKeyClaim.Issuer,
                    ProviderKey = providerKeyClaim.Value,
                    UserName = identity.FindFirstValue(ClaimTypes.Name)
                };
            }
        }

        private static class RandomOAuthStateGenerator
        {
            private static RandomNumberGenerator _random = new RNGCryptoServiceProvider();

            public static string Generate(int strengthInBits)
            {
                const int bitsPerByte = 8;

                if (strengthInBits % bitsPerByte != 0)
                {
                    throw new ArgumentException("strengthInBits must be evenly divisible by 8.", "strengthInBits");
                }

                int strengthInBytes = strengthInBits / bitsPerByte;

                byte[] data = new byte[strengthInBytes];
                _random.GetBytes(data);
                return HttpServerUtility.UrlTokenEncode(data);
            }
        }

        #endregion
    }
}
