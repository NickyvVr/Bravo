﻿namespace Sqlbi.Bravo.Controllers
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Sqlbi.Bravo.Infrastructure.Services.PowerBI;
    using Sqlbi.Bravo.Models;
    using Sqlbi.Bravo.Services;
    using System.Net.Mime;
    using System.Threading.Tasks;

    /// <summary>
    /// Authentication controller
    /// </summary>
    /// <response code="400">Status400BadRequest - See the "instance" and "detail" properties to identify the specific occurrence of the problem</response>
    [Route("auth/[action]")]
    [ApiController]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    public class AuthenticationController : ControllerBase
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly IPBICloudService _pbicloudService;

        public AuthenticationController(IAuthenticationService authenticationService, IPBICloudService pbicloudService)
        {
            _authenticationService = authenticationService;
            _pbicloudService = pbicloudService;
        }

        /// <summary>
        /// Attempts to authenticate and acquire an access token for the account to access the PowerBI cloud services
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        [HttpGet]
        [ActionName("powerbi/SignIn")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BravoAccount))]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> PowerBISignIn(string? upn)
        {
            await _authenticationService.PBICloudSignInAsync(userPrincipalName: upn);
            return Ok(_authenticationService.Account);
        }

        /// <summary>
        /// Clear the token cache for all the accounts
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        [HttpGet]
        [ActionName("powerbi/SignOut")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> PowerBISignOut()
        {
            await _authenticationService.PBICloudSignOutAsync();
            return Ok();
        }

        /// <summary>
        /// Returns information about the currently logged in user
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        /// <response code="401">Status401Unauthorized - Sign-in required</response>
        [HttpGet]
        [ActionName("GetUser")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BravoAccount))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> GetAccount()
        {
            if (await _authenticationService.IsPBICloudSignInRequiredAsync())
                return Unauthorized();

            return Ok(_authenticationService.Account);
        }

        /// <summary>
        /// Returns the account profile picture as base64 encoded image [data:image/jpeg;base64,...]
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        /// <response code="404">Status404NotFound - Current account has no profile picture</response>
        /// <response code="401">Status401Unauthorized - Sign-in required</response>
        [HttpGet]
        [ActionName("GetUserAvatar")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> GetAccountAvatar()
        {
            if (await _authenticationService.IsPBICloudSignInRequiredAsync())
                return Unauthorized();

            var avatar = await _pbicloudService.GetAccountAvatarAsync();
            if (avatar is null)
                return NotFound();

            return Ok(avatar);
        }
    }
}
