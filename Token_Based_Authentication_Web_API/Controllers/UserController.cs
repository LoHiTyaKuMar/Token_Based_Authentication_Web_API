using System; using System.Linq; using System.Net; using System.Net.Http; using System.Web.Http; using Token_Based_Authentication_Web_API.DatabaseModel;

namespace Token_Based_Authentication_Web_API.Controllers
{
    [Authorize]
    [RoutePrefix("api/User")]
    public class UserController : ApiController
    {
        /// <summary>
        /// This method updates the user email to a provided email
        /// </summary>
        /// <param name="fromEmail"></param>
        /// <param name="toEmail"></param>
        /// <returns> Sucessful or Unsuccessful Message </returns>
        [HttpPost]
        [Route("UpdateEmail")]
        public IHttpActionResult UpdateEmail(string fromEmail, string toEmail)
        {
            if (ModelState.IsValid && !string.IsNullOrEmpty(fromEmail) && !string.IsNullOrEmpty(toEmail))
            {
                // Requests database to get user details for provided email
                using (Token_Based_Authentication_Web_APIEntities _entities = new Token_Based_Authentication_Web_APIEntities())
                {
                    AspNetUser user = _entities.AspNetUsers.Where(x => x.Email == fromEmail).FirstOrDefault();

                    if (user == null)
                    {
                        // If user not found return with error
                        return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.NotFound, "User not found!"));
                    }

                    // If user found then replace the oldemail with the new email
                    try
                    {
                        user.Email = toEmail;
                        user.UserName = toEmail;
                        _entities.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        // If any exception happens then return the Server error with exception
                        return InternalServerError(ex);
                    }
                }

                // Successful Message
                return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.OK, "Email updated successfully!"));
            }

            // If we got this far, something failed, redisplay form
            return BadRequest(ModelState);
        }

        /// <summary>
        /// This method deletes the user belongs to the provided email
        /// </summary>
        /// <param name="userEmail"></param>
        /// <returns> Sucessful or Unsuccessful Message </returns>
        [HttpPost]
        [Route("DeleteUser")]
        public IHttpActionResult DeleteUser(string userEmail)
        {
            if (ModelState.IsValid && !string.IsNullOrEmpty(userEmail))
            {
                // Requests database to get user details for provided email
                using (Token_Based_Authentication_Web_APIEntities _entities = new Token_Based_Authentication_Web_APIEntities())
                {
                    AspNetUser user = _entities.AspNetUsers.Where(x => x.Email == userEmail).FirstOrDefault();

                    if (user == null)
                    { 
                        // If user not found return with error
                        return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.NotFound, "User not found!"));
                    }

                    // If user found delete that user
                    try
                    {
                        _entities.AspNetUsers.Remove(user);
                        _entities.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        // If any exception happens then return the Server error with exception
                        return InternalServerError(ex);
                    }
                }

                // Successful Message
                return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.OK, "User deleted successfully!"));
            }

            // If we got this far, something failed, redisplay form
            return BadRequest(ModelState);
        }

        /// <summary>
        /// This method is to check the token validation and to check if the provided username is valid anonymous or authentic user of the application
        /// </summary>
        /// <param name="userEmail"></param>
        /// <returns> Authentic User or Anonymous User message </returns>
        [Route("GetUserDetails")]
        public IHttpActionResult GetUserDetails(string userEmail)
        {
            if (ModelState.IsValid && !string.IsNullOrEmpty(userEmail))
            {
                // Requests database to get user details for provided email and checks it's authnticity
                using (Token_Based_Authentication_Web_APIEntities _entities = new Token_Based_Authentication_Web_APIEntities())
                {
                    AspNetUser user = _entities.AspNetUsers.Where(x => x.Email == userEmail).FirstOrDefault();

                    if (user == null)
                    {
                        // If user not found return with error
                        return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.NotFound, "User not found!"));
                    }

                    // If user found return the authentic user message
                    return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.OK, "User exist with provided email"));
                } 
            }

            // If we got this far, something failed, redisplay form
            return BadRequest(ModelState);
        }
    }
}
