using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Linq;
using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

// TODO: uppdatera namespacet om ditt projekt heter något annat än "CityInfo.API"
namespace CityInfo.API.Controllers
{
    [Route("api/account")]
    public class AccountController : Controller
    {

        private readonly UserManager<IdentityUser> _userManager;
        private RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private ILogger<AccountController> _logger;
        private IHttpContextAccessor _httpContextAccessor;

        public AccountController(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            SignInManager<IdentityUser> signInManager,
            ILogger<AccountController> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(string username, string password)
        {
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(username, password, false, false); //, lockoutOnFailure: false
                if (result.Succeeded)
                {
                    _logger.LogInformation($"User {username} logged in.");
                    return Ok();
                }

                var errorMessage = "Invalid login attempt";

                if (result.RequiresTwoFactor)
                {
                    errorMessage = "Two factor required";
                }
                if (result.IsLockedOut)
                {
                    errorMessage = "User account locked out.";
                }
                if (result.IsNotAllowed)
                {
                    errorMessage = "User is not allowed to log in";
                }

                _logger.LogWarning(errorMessage);

                return BadRequest(errorMessage);

            }

            _logger.LogWarning("Model state is invald");

            return BadRequest();
        }

        [HttpPost("logout")]
        public async Task<IActionResult> LogOut()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            return Ok();
        }

        [HttpPost("createaccounts")]
        public async Task<IActionResult> CreateAccounts()
        {
            const string superPassword = "hemligt1A#";

            var admin = new IdentityUser { UserName = "admin", Email = "admin@cityinfo.se" };
            await RecreateUser(admin, superPassword);
            await AddUserToRole(admin, "Administrator");

            var cm34 = new IdentityUser { UserName = "traveler", Email = "traveler@cityinfo.se" };
            await RecreateUser(cm34, superPassword);
            await AddUserToRole(cm34, "Traveler");
            await UserVisitedCities(cm34, new[] { 2, 3 });

            var cm = new IdentityUser { UserName = "citymanager", Email = "citymanager@cityinfo.se" };
            await RecreateUser(cm, superPassword);
            await AddUserToRole(cm, "CityManager");

            var ex = new IdentityUser { UserName = "explorer", Email = "explorer@cityinfo.se" };
            await RecreateUser(ex, superPassword);
            await AddUserToRole(ex, "Explorer");

            return Ok();

        }

        [HttpGet("whoami")]
        public IActionResult WhoAmI()
        {
            var user = _httpContextAccessor.HttpContext.User;
            var name = user.FindFirstValue(ClaimTypes.Name);
            var roleList = user.FindAll(ClaimTypes.Role).Select(r => r.Value);
            var roleListString = string.Join(",", roleList);

            var claimOutput = new List<string>();

            foreach (var claim in user.Claims)
            {
                claimOutput.Add($"Type: {claim.Type} Value:{claim.Value}");
            }

            var claimOutputString = string.Join("\n", claimOutput);

            var result = $"You are user {name} with roles {roleListString}.\n{claimOutputString}";

            return Ok(result);
        }

        private async Task RecreateUser(IdentityUser user, string password)
        {
            var existingUser = await _userManager.FindByNameAsync(user.UserName);

            if (existingUser != null)
            {
                await _userManager.DeleteAsync(existingUser);
                _logger.LogInformation("User {0} deleted", user.UserName);
            }

            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                ThrowErrors(result.Errors);
            }

            _logger.LogInformation("User {0} created", user.UserName);
        }

        private async Task EnsureRoleExist(string role)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole { Name = role });
            }
        }

        private async Task AddUserToRole(IdentityUser user, string role)
        {
            await EnsureRoleExist(role);

            var result = await _userManager.AddToRoleAsync(user, role);

            if (!result.Succeeded)
            {
                ThrowErrors(result.Errors);
            }

            _logger.LogInformation("User {0} added to role {1}", user.UserName, role);
        }

        private async Task UserVisitedCities(IdentityUser user, int[] citiesToControl)
        {
            var citiesAsString = string.Join(",", citiesToControl);
            Claim claim = new Claim("VisitedCities", citiesAsString); // TODO: lägg till issuer
            var result = await _userManager.AddClaimAsync(user, claim);

            if (!result.Succeeded)
            {
                ThrowErrors(result.Errors);
            }

            _logger.LogInformation("User {0} added to claim VisitedCities:{1}", user.UserName, citiesAsString);

        }

        private string ErrorListToString(IdentityResult result)
        {
            if (!result.Errors.Any())
            {
                return "No errors";
            }
            return string.Join("\n", result.Errors.Select(err => err.Description));
        }

        private void ThrowErrors(IEnumerable<IdentityError> errors)
        {
            throw new Exception(string.Join(",", errors.Select(err => err.Description)));
        }
    }
}
