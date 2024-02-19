using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repository.Contracts;
using SharedModel.Dtos;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace AuctionApi.Controllers
{


   //[Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserRepository userRepository;
        private readonly IRoleRepository roleRepository;
        private ILogger<UserController> logger;
        private readonly IAuthRepository authRepository;
        //private readonly IHttpContextAccessor _httpContextAccessor;


        public UserController(IUserRepository _userRepository, IRoleRepository _roleRepository,
            IAuthRepository _authRepository, ILogger<UserController> _logger)
        {
            authRepository = _authRepository;
            userRepository = _userRepository;
            roleRepository = _roleRepository;
            //mapper = _mapper;
            logger = _logger;
            //_httpContextAccessor= httpContextAccessor;
        }


        // GET: UserController/Details/5
        //[Authorize(Roles = "HOD")]
        [HttpGet("GetUser/{id:int}")]
        public async Task<ActionResult<UserDto>> GetItem(int id)
        {
            var httpResponse = new HttpResponseMessage();
            try
            {
                var userDto = await this.userRepository.GetById(id);

                if (userDto == null)
                {
                    return NotFound();
                }

                return Ok(userDto);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        // GET: api/<UserController/GetUsers>
        [HttpGet("GetUsers")]
        public async Task<ActionResult> Get([FromQuery] PagingRequestDto pagingRequestDto)
        {

            try
            {
                var pagedUsers = await this.userRepository.GetAllUsers(pagingRequestDto);

                if (pagedUsers == null)
                {
                    return NotFound();
                }

                var userDtos = pagedUsers;//.ConvertToDto();

                var pagedResponse = new PagingResponse<UserDto>
                {
                    Items = userDtos.ToList(),
                    MetaData = pagedUsers.MetaData,
                };
                return Ok(pagedResponse);

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);

            }
        }

        // GET: UserController/Details/5
        [HttpGet("Get/{email}")]
        public async Task<ActionResult<UserDto>> GetByEmail(string email)
        {

            var httpResponse = new HttpResponseMessage();

            try
            {
                var userDto = await this.userRepository.GetUserByEmail(email);
                if (userDto == null)
                {
                    return NotFound();
                }
                return Ok(userDto);

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        // GET: api/<UserController/Create>
        [AllowAnonymous]
        [HttpPost("Create")]
        public async Task<ActionResult<UserDto>> PostItem([FromBody] UserToAddDto userToAddDto)
        {
            try
            {
                if (userToAddDto == null || !ModelState.IsValid)
                {
                    return BadRequest($"{nameof(UserToAddDto)} cannot be null or empty !");
                }

                //Check if same item exist in db.

                var user = await this.userRepository.GetUserByEmail(userToAddDto.Email);
                if (user != null)
                {
                    ModelState.AddModelError("email", "Customer email already in use");
                    return BadRequest(ModelState);
                }

                if (Request.HttpContext.Connection.RemoteIpAddress?.ToString() != null)
                {
                    userToAddDto.IPAddress = Request.HttpContext.Connection.RemoteIpAddress.ToString();
                }

                /* Override the role list  to ensure user with only with one role, 'User' role is created */
                userToAddDto.Roles.Clear();
                userToAddDto.Roles = new List<RoleToAddEditDto> { new RoleToAddEditDto() { Id = 1 } };

                /*userToAddDto.Roles = userToAddDto.UserTypeId == 2 ?
                      new List<RoleToAddEditDto> { new RoleToAddEditDto() { Id = 1 }, new RoleToAddEditDto() { Id = 6 } }
                    : new List<RoleToAddEditDto> { new RoleToAddEditDto() { Id = 1 } };*/

                var newUser = await this.userRepository.CreateUser(userToAddDto);

                if (newUser == null)
                {
                    throw new Exception($"Something went wrong when attempting to create object (Item: ({newUser?.Id})");
                }
                return CreatedAtAction(nameof(GetItem), new { id = newUser.Id }, newUser);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        //PATCH: UserController/UpdateUset{int id, UserToUpdateDto}       
        [HttpPatch("Update/{id:int}")]
        public async Task<ActionResult> UpdateUser(int id, [FromBody] UserToEditDto userToEditDto)
        {
            try
            {
                if (userToEditDto == null || !ModelState.IsValid)
                {
                    return BadRequest($"{nameof(UserToEditDto)} cannot be null or empty !");
                }

                if (id != userToEditDto.Id)
                    return BadRequest("Profile ID mismatch");

                if (Request.HttpContext.Connection.RemoteIpAddress?.ToString() != null)
                {
                    userToEditDto.IPAddress = Request.HttpContext.Connection.RemoteIpAddress.ToString();
                }

                userToEditDto.ModifiedDate = DateTime.UtcNow.AddHours(12);
                userToEditDto.UserTypeId = (userToEditDto.UserTypeId == 0) ? 1 : userToEditDto.UserTypeId;

                var updatedUser = await this.userRepository.UpdateUser(id, userToEditDto);

                if (updatedUser == null)
                {
                    return NotFound();
                }
                return Ok(updatedUser);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }

        }

        //Patch: UserController/UpdateUserAdmin{int id, UserToUpdateDto}

        //[Authorize(Policy = "SuperUser")]  // Using RequireAssertion method to test Auth
        [Authorize(Policy = "UserRoleManagePermission")]
        [HttpPatch("UpdateUserAndRoles/{id:int}")]
        public async Task<ActionResult> UpdateUserAndRoles(int id, [FromBody] UserToEditDto userToEditDto)
        {
            try
            {
                if (userToEditDto == null || !ModelState.IsValid)
                {
                    return BadRequest($"{nameof(UserToEditDto)} cannot be null or empty !");
                }

                if (id != userToEditDto.Id)
                    return BadRequest("Profile ID mismatch");

                if (Request.HttpContext.Connection.RemoteIpAddress?.ToString() != null)
                {
                    userToEditDto.IPAddress = Request.HttpContext.Connection.RemoteIpAddress.ToString();
                }

                userToEditDto.ModifiedDate = DateTime.UtcNow.AddHours(12);
                userToEditDto.UserTypeId = (userToEditDto.UserTypeId == 0) ? 1 : userToEditDto.UserTypeId;

                var updatedUser = await this.userRepository.UpdateUserWithRoles(id, userToEditDto);

                if (updatedUser == null)
                {
                    return NotFound();
                }

                return Ok(updatedUser);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }

        }

        //PATCH: UserController/AssignRole{int id, UserToUpdateDto}
        [Authorize(Policy = "UserRoleManagePermission")]
        [HttpPatch("AssignRole/{id:int}")]
        public async Task<ActionResult> AssignRole(int id, [FromBody] List<RoleToAssignDto> roleToAssignDtos)
        {
            try
            {

                if (roleToAssignDtos == null || !ModelState.IsValid || roleToAssignDtos.Count == 0)
                {
                    return BadRequest($"{nameof(RoleToAssignDto)} cannot be null or empty !");
                }

                var user = await userRepository.Get(id);
                if (user == null)
                {
                    return NotFound();
                }

                /* Uncomment below codes after adding roles repo ... */
                var allRoles = await roleRepository.GetAll();

                foreach (var role in roleToAssignDtos)
                {
                    var roleExist = allRoles.Any(r => r.ID == role.RoleId);
                    if (!roleExist)
                    {
                        ModelState.AddModelError("Error", $"Role with code {role.RoleId} may not exist  in database !");
                        return BadRequest(ModelState);
                    }
                    if (Request.HttpContext.Connection.RemoteIpAddress?.ToString() != null)
                        role.IPAddress = Request.HttpContext.Connection.RemoteIpAddress.ToString();
                }
                var res = await userRepository.AssignRole(user, roleToAssignDtos);

                if (res.StatusCode == 200)
                {
                    return Ok(res?.Data);

                }
                return BadRequest();

            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Violation of PRIMARY KEY constraint"))
                {
                    return BadRequest(ex.Message);
                }

                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        // Delete: UserController/Delete/5
        [Authorize(Policy = "UserDeletePermission")]
        [HttpDelete("Delete/{id:int}")]
        public async Task<ActionResult<UserDto>> DeleteItem(int id)
        {
            try
            {
                var deletedUser = await userRepository.DeleteUser(id);

                if (deletedUser == null)
                {
                    return NotFound();
                }

                //var userMapper = mapper.Map<UserDto>(deletedUser);

                return Ok(deletedUser); // or ' return NoContent();'

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        //POST:  UserController/Login{LoginDto}
        [AllowAnonymous]
        [HttpPost("Login")]
        public async Task<ActionResult> LoginUser([FromBody] LoginDto loginDto)
        {
            try
            {
                if (loginDto == null || !ModelState.IsValid)
                {
                    return BadRequest($"{nameof(LoginDto)} cannot be null or empty !");
                }

                //if (Request.HttpContext.Connection.RemoteIpAddress?.ToString() != null)
                //{
                //    loginDto.IpAddress = Request.HttpContext.Connection.RemoteIpAddress.ToString();
                //}                
                loginDto.IpAddress = ipAddress();

                var loggedInUser = await authRepository.Login(loginDto);


                if (loggedInUser.StatusCode == 404)
                {
                    return NotFound(loggedInUser);
                }

                if (loggedInUser.StatusCode == 200)
                {
                    setTokenCookie(loggedInUser.Data.RefreshToken);
                    return Ok(loggedInUser.Data);

                }
                return StatusCode(StatusCodes.Status500InternalServerError, loggedInUser);

                //throw new Exception($"Server failed to fetch record, contact system admin./n/n" + loggedInUser);

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        //POST: UserController/RefreshToken
        [AllowAnonymous]
        [HttpGet("RefreshToken")]
        public async Task<ActionResult> RefreshToken()
        {

            var refreshToken = Request.Cookies["refreshToken"];

            Console.WriteLine("refreshToken send from client is : " + refreshToken);


            try
            {
                if (string.IsNullOrEmpty(refreshToken) || !ModelState.IsValid)
                {
                    return BadRequest($"{nameof(LoginDto)} cannot be null or empty !");
                }

                var refreshTokenReq = new RefreshTokenRequestDto();

                refreshTokenReq.RefreshToken = refreshToken;
                refreshTokenReq.IpAddress = ipAddress();

                //refreshTokenRequest.IpAddress = ipAddress();

                var refreshedToken = await authRepository.RefreshToken(refreshTokenReq);

                if (refreshedToken == null)
                {
                    return Unauthorized(new { Message = "Failed to refresh token. Check Jwt and/or refresh token provided from client.", refreshedToken });
                }
                setTokenCookie(refreshedToken.RefreshToken);
                return Ok(refreshedToken);

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }

        }


        [HttpGet("Logout/{id}")]
        public async Task<IActionResult> Logout(string id)
        {
            //string rawUserId = id;//= HttpContext.User.FindFirstValue("Id");

            if (!int.TryParse(id, out int userId))
            {
                return Unauthorized();
            }
            if (userId <= 0)
                return BadRequest(new { message = "Invalid user id found !", StatusCode = 400 });

            var result = await authRepository.DeleteRefreshTokens(userId);
            if (!result)
                return StatusCode(500, new { Message = $"Failed to logout user ?, review error logs ? ", Error = result });

            logger.LogInformation("User logged out.");

            return NoContent();

        }


        // Helper methods

        private void setTokenCookie(string token)
        {
            // append cookie with refresh token to the http response
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddDays(7),
                //Path = "/",               
                IsEssential = true,
                SameSite = SameSiteMode.None, // changed to test error - 02-02-23 @9.41am
                Secure = true,
            };

            Response.Cookies.Append("refreshToken", token, cookieOptions);
            /**This 3 lines of code to test cookies are set or not ! */
            //_httpContextAccessor.HttpContext.Response.Cookies.Append("someKey", "someValue", cookieOptions);
            // Response.Cookies.Append("X-Refresh-Token", "user.RefreshToken", new CookieOptions() { HttpOnly = true, SameSite = SameSiteMode.Strict });

        }

        private string ipAddress()
        {
            // get source ip address for the current request
            if (Request.Headers.ContainsKey("X-Forwarded-For"))
                return Request.Headers["X-Forwarded-For"];
            else
                return HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();
        }

    }
}
