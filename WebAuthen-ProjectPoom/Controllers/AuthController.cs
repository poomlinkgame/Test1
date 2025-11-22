
using System.Security.Claims;

using Microsoft.AspNetCore.Mvc;

using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Identity;
using WebAuthen.Data;
using WebAuthen.Models;
using Microsoft.AspNetCore.Authentication;
using WebAuthen.App_code;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace WebAuthen.Controllers;

[ApiController]
[Route("api/[Controller]")]
public class AuthController(IOpenIddictApplicationManager applicationManager, IOpenIddictTokenManager tokenManager, ApplicationDbContext dbContext,
 UserManager<ApplicationUser> userManager, AuthClass auth, DynamicConnectionService dynamic, HandleApiReturn handle, EmailService email) : ControllerBase
{
    private readonly IOpenIddictApplicationManager _applicationManager = applicationManager;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly IOpenIddictTokenManager _tokenManager = tokenManager;
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly HandleApiReturn _handle = handle;

    private readonly AuthClass _auth = auth;
    private readonly DynamicConnectionService _dynamic = dynamic;
    private readonly EmailService _email = email;

    // ******** API Function ********
    [HttpGet("version")]
    public IActionResult authVersion()
    {
        return Ok(new { message = "Api version => 1.0.0" });
    }

    [HttpPost("decrypt")]
    public IActionResult Decrypt([FromBody] Cypto request)
    {
        if (request == null || string.IsNullOrEmpty(request.code_enc))
        {
            return BadRequest("Invalid request data.");
        }
        try
        {
            var decryptedCode = _auth.Decrypt(request.code_enc);
            return Ok(new { decryptedCode });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpPost("encrypt")]
    public IActionResult Encrypt([FromBody] Cypto request)
    {
        if (request == null || string.IsNullOrEmpty(request.code_enc))
        {
            return BadRequest("Invalid request data.");
        }
        try
        {
            var encryptedCode = _auth.Encrypt(request.code_enc);
            return Ok(new { encryptedCode });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpPost("register_old")]
    public async Task<IActionResult> RegisterOld([FromBody] RegisterModelTest UserData)
    {
        ApplicationUser user = new() { UserName = UserData.UserName };
        var rs = await _userManager.CreateAsync(user, UserData.Password);


        if (rs.Succeeded)
        {
            return Ok(new { message = "User registered" });
        }

        var errors = rs.Errors.Select(e => e.Description);
        return BadRequest(new { errors });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromHeader(Name = "Authenticate")] string authenticate, [FromBody] UserRegisterDto UserData)
    {
        return await _handle.ApiAsync(async () =>
        {
            AuthenticateDto authenticateDto = DecryptAuthenticate(authenticate);

            bool result = await _auth.Register(UserData, authenticateDto.Constr);

            return result switch
            {
                false => new ReturnDto(Code: "200", Message: "Can't register"),
                true => new ReturnDto(Code: "200", Message: "User is registered")
            };
        });

    }

    [HttpPost("token"), Produces("application/json")]
    public async Task<IActionResult> Token()
    {
        var request = HttpContext.GetOpenIddictServerRequest();
        var auth = HttpContext.Request.Headers["Authenticate"].ToString();

        if (request is null)
        {
            return BadRequest("Invalid OpenIddict request.");
        }

        return request.GrantType switch
        {
            GrantTypes.ClientCredentials => await HandleClientCredetials(request),
            GrantTypes.Password or "third_party" => await HandleSelfPassword(auth, request),
            GrantTypes.RefreshToken => await HandleRefreshToken(),
            _ => Unauthorized(),
        };

    }

    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke([FromForm(Name = "access_token")] string atk)
    {
        if (string.IsNullOrEmpty(atk))
            return BadRequest();


        var token = await _tokenManager.FindByReferenceIdAsync(atk);
        // descriptor = (OpenIddictTokenDescriptor?)await _tokenManager.FindByReferenceIdAsync(token);

        if (token is null) return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        var authorizeId = await _tokenManager.GetAuthorizationIdAsync(token);

        if (authorizeId is not null)
        {
            await _tokenManager.RevokeByAuthorizationIdAsync(authorizeId);
        }

        return Ok();
    }

    [HttpGet("revoke_all")]
    [Authorize]
    public async Task<IActionResult> RevokeAll()
    {
        var subject = User.FindFirst(Claims.Subject)?.Value;

        if (subject is null) return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        await _tokenManager.RevokeBySubjectAsync(subject);


        return Ok();
    }

    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> Profile()
    {

        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;

            UserDto? user = await _auth.GetUserData(authorization.Constr, authorization.Sysstr, authorization.Id, 1);

            ReturnDto result;
            if (user is null)
            {
                result = new(Code: "404", Message: "not found", Data: user);
            }
            else
            {
                result = new(Code: "200", Data: user);
            }

            return result;
        });

    }

    [HttpPost("ValidatePassword")]
    [Authorize]
    public async Task<IActionResult> ValidatePassword([FromBody] PasswordDto body)
    {
        return await ValidatePasswordOrPin(password: body.User_Password);
    }

    [HttpPost("ValidatePin")]
    [Authorize]
    public async Task<IActionResult> ValidatePin([FromBody] PinDto body)
    {
        return await ValidatePasswordOrPin(pin: body.User_Pin);
    }

    // ละไว้ก่อนจ้าาาา
    [HttpGet("CheckUserPin")]
    [Authorize]
    public async Task<IActionResult> CheckUserPin()
    {

        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;

            bool hasPin = await _auth.UserHasPin(authorization);

            return hasPin switch
            {
                false => new ReturnDto(Code: "404", Data: hasPin),
                true => new ReturnDto(Code: "200", Data: hasPin),
            };
        });
    }

    // Is this email in use?
    [HttpPost("CheckEmail")]
    public async Task<IActionResult> CheckEmail([FromHeader(Name = "Authenticate")] string authenticate, [FromBody] EmailDto body)
    {

        return await _handle.ApiAsync(async () =>
        {
            AuthenticateDto auth = DecryptAuthenticate(authenticate);
            bool hasEmail = await _auth.EmailExists(auth, body.User_Email);

            return hasEmail switch
            {
                false => new ReturnDto(Code: "404", Data: hasEmail),
                true => new ReturnDto(Code: "200", Data: hasEmail)
            };
        });

    }

    [HttpPost("VerifyEmail")]
    public async Task<IActionResult> VerifyEmail([FromHeader(Name = "Authenticate")] string Authenticate, [FromBody] EmailDto body)
    {

        return await _handle.ApiAsync(async () =>
        {
            AuthenticateDto auth = DecryptAuthenticate(Authenticate);
            bool hasEmail = await _auth.EmailExists(auth, body.User_Email);

            if (hasEmail)
            {
                return new ReturnDto(Code: "200", Message: "Email already exists", Data: hasEmail);
            }

            string referenceToken = Guid.NewGuid().ToString("N")[..8];
            string otpCode = Utils.GenerateNumericOtp();
            var expireAt = DateTime.UtcNow.AddMinutes(5);


            // insert reference token and otp code into database and send email

            await _email.SendOtpEmailAsync(toEmail: body.User_Email, reference: referenceToken, otp: otpCode);

            return new ReturnDto(Code: "200", Message: "Email is available", Data: new { referenceToken, expireAt });
        });

    }

    // Is this phone number in use?
    [HttpPost("CheckPhoneNumber")]
    public async Task<IActionResult> CheckPhoneNumber([FromHeader(Name = "Authenticate")] string Authenticate, [FromBody] PhoneDto body)
    {

        return await _handle.ApiAsync(async () =>
        {
            AuthenticateDto auth = DecryptAuthenticate(Authenticate);
            bool hasPhoneNumber = await _auth.CheckPhoneNumber(auth, body.PhoneNumber);

            return hasPhoneNumber switch
            {
                false => new ReturnDto(Code: "404", Data: hasPhoneNumber),
                true => new ReturnDto(Code: "200", Data: hasPhoneNumber),
            };
        });

    }

    [HttpPost("ValidatePhoneNumber")]
    [Authorize]
    public async Task<IActionResult> ValidatePhoneNumber([FromBody] PhoneDto body)
    {

        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;
            AuthenticateDto authenticate = new(authorization.Constr, authorization.Sysstr);
            bool hasPhoneNumber = await _auth.CheckPhoneNumber(authenticate, body.PhoneNumber, UserwebId: authorization.Id);

            return hasPhoneNumber switch
            {
                false => new ReturnDto(Code: "404", Data: hasPhoneNumber),
                true => new ReturnDto(Code: "200", Data: hasPhoneNumber),
            };
        });

    }

    // Have this platform_authen in database ?
    [HttpPost("CheckPlatform_Authencode")]
    public async Task<IActionResult> CheckPlatformAuthencode([FromHeader(Name = "Authenticate")] string authenticate, [FromBody] AuthenCodeDto body)
    {

        return await _handle.ApiAsync(async () =>
        {
            AuthenticateDto authenticateDto = DecryptAuthenticate(authenticate);
            List<dynamic> dt = await _auth.GetDataPlatform_Authencode(authenticateDto, body.AuthenCode, PlatformName: body.PlatformName);

            return (dt.Count > 0) switch
            {
                false => new ReturnDto(Code: "404", Data: false),
                true => new ReturnDto(Code: "200", Data: true)
            };
        });

    }

    [HttpPost("SaveAuthenCode")]
    [Authorize]
    public async Task<IActionResult> SaveAuthenCode([FromBody] AuthenCodeDto body)
    {
        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;

            bool isSuccessed = await _auth.InsertPlatformAuthenCode(authorization, body);

            return isSuccessed switch
            {
                false => new ReturnDto(Code: "500", Message: "ไม่สามารถบันทึกข้อมูลได้"),
                true => new ReturnDto(Code: "200", Message: "บันทึกข้อมูลเรียบร้อย"),
            };
        });

    }

    [HttpPut("EditProfile")]
    [Authorize]
    public async Task<IActionResult> EditProfile([FromBody] UserUpdateDto body)
    {

        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;
            bool isSuccessed = await _auth.EditUserData(authorization, body);

            return isSuccessed switch
            {
                false => new ReturnDto(Code: "500", Message: "แก้ไขข้อมูลไม่สำเร็จ"),
                true => new ReturnDto(Code: "200", Message: "บันทึกข้อมูลเรียบร้อย"),
            };
        });

    }

    [HttpPatch("SaveTitleUsername")]
    [Authorize]
    public async Task<IActionResult> SaveTitleUsername([FromBody] SaveTitleUsernameDto body)
    {
        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;
            bool result = await _auth.HostnameInsertAsync(authorization, body.user_hostname);

            return result switch
            {
                true => new ReturnDto(Code: "200", Message: "บันทึกข้อมูลสำเร็จ"),
                false => new ReturnDto(Code: "500", Message: "ไม่สามารถบันทึกข้อมูลได้")
            };

        });
    }

    [HttpPost("AddSubAccount")]
    [Authorize]
    public async Task<IActionResult> SubAccount([FromBody] SubaccountDto body)
    {
        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;

            if (body.password_must_change && body.password_change_disable)
                return new ReturnDto(Code: "400", Message: "หากต้องการให้เปลี่ยนรหัสผ่าน ครั้งหน้าต้องอนุญาตให้เปลี่ยนรหัสผ่านด้วย");

            bool result = await _auth.SubAccountInsertAsync(authorization, body);

            return result switch
            {
                true => new ReturnDto(Code: "200", Message: "บันทึกข้อมูลสำเร็จ"),
                false => new ReturnDto(Code: "500", Message: "ไม่สามารถบันทึกข้อมูลได้")
            };
        });

    }

    [HttpPatch("{user_id}/toggleStatus")]
    [Authorize]
    public async Task<IActionResult> SubAccountStatusSwitch(int user_id)
    {
        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;
            bool result = await _auth.SubAccountStatusToggleAsync(authorization, user_id);

            return result switch
            {
                true => new ReturnDto(Code: "200", Message: "เปลี่ยนสถานะสำเร็จ"),
                false => new ReturnDto(Code: "500", Message: "ไม่สามารถเปลี่ยนสถานะได้")
            };
        });
    }

    [HttpPatch("UpdateSubAccount")]
    [Authorize]
    public async Task<IActionResult> UpdateSubAccount([FromBody] SubaccountUpdateDto body)
    {
        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;
            bool result = await _auth.SubAccountUpdateAsync(authorization, body);

            return result switch
            {
                true => new ReturnDto(Code: "200", Message: "อัพเดทข้อมูลสำเร็จ"),
                false => new ReturnDto(Code: "200", Message: "ไม่ได้รับอนุญาตให้เปลี่ยนรหัสผ่าน หรือไม่พบข้อมูล")
            };
        });
    }

    [HttpGet("GetSubAccounts")]
    [Authorize]
    public async Task<IActionResult> GetSubAccount()
    {
        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;
            List<dynamic> result = await _auth.SubAccountGetAsync(authorization);

            if (result.Count > 0)
            {
                return new ReturnDto(Code: "200", Message: "ดึงข้อมูลสำเร็จ", Data: result);
            }
            else
            {
                return new ReturnDto(Code: "404", Message: "ไม่พบข้อมูล");
            }
        });
    }

    [HttpDelete("{user_id}/deleteAccount")]
    [Authorize]
    public async Task<IActionResult> DeleteSubAccount(int user_id)
    {
        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;
            bool result = await _auth.SubAccountDeleteAsync(authorization, user_id);

            return result switch
            {
                true => new ReturnDto(Code: "200", Message: "ลบข้อมูลสำเร็จ"),
                false => new ReturnDto(Code: "500", Message: "ไม่สามารถลบข้อมูลได้")
            };
        });
    }



    // ******** Handle Function ********

    private AuthenticateDto DecryptAuthenticate(string Authenticate)
    {
        var jstr = _auth.Decrypt(Authenticate);

        try
        {
            JObject jobj = [];
            jobj = JsonConvert.DeserializeObject<JObject>(jstr) ?? [];
            string constr = jobj.Value<string>("coid") ?? "";
            string sysstr = jobj.Value<string>("syt") ?? "";
            return new AuthenticateDto(constr, sysstr);
        }
        catch (Exception)
        {
            throw;
        }

    }

    private async Task<IActionResult> HandleRefreshToken()
    {
        var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        if (!result.Succeeded)
        {
            return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        var principal = result.Principal!;


        var identity = new ClaimsIdentity(
            principal.Claims,
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            Claims.Name,
            Claims.Role
        );

        identity.SetDestinations(claim => claim.Type switch
      {
          Claims.Scope => Array.Empty<string>(),
          _ => [Destinations.AccessToken]
      });

        var newPrincipal = new ClaimsPrincipal(identity);

        var properties = new AuthenticationProperties();

        return SignIn(newPrincipal, properties, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private async Task<IActionResult> HandlePassword(OpenIddictRequest request)
    {
        var user = await _userManager.FindByNameAsync(request.Username!);
        if (user is null ||
            !await _userManager.CheckPasswordAsync(user, request.Password!))
        {
            return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        var identity = new ClaimsIdentity(
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            Claims.Name, Claims.Role);

        identity.AddClaim(Claims.Subject, user.Id);
        identity.AddClaim(Claims.Name, user.UserName!);

        identity.SetScopes(Scopes.OfflineAccess);

        identity.SetDestinations(claim => claim.Type switch
        {
            // Claims.Scope => Array.Empty<string>(),
            _ => [Destinations.AccessToken]
        });


        var Properties = new AuthenticationProperties();

        Properties.SetParameter("id", user.Id);
        Properties.SetParameter("username", user.UserName);
        Properties.SetParameter("expired", false);

        return SignIn(new ClaimsPrincipal(identity), Properties,
                      OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private async Task<IActionResult> HandleSelfPassword(string Authenticate, OpenIddictRequest request)
    {
        string? user_username = request.Username;
        string? password = request.Password;
        string? authen_code = request["authen_code"]?.ToString();
        string? loginType = request["login_type"]?.ToString();

        if (string.IsNullOrEmpty(Authenticate))
        {
            return BadRequest("Authenticate header is required.");
        }

        try
        {
            AuthenticateDto auth = DecryptAuthenticate(Authenticate);
            user_username ??= string.Empty;
            password ??= string.Empty;
            loginType ??= string.Empty;
            authen_code ??= string.Empty;

            UserDto? user = await _auth.ValidateUserAsync(user_username, password, loginType, auth.Constr, auth.Sysstr, authenCode: authen_code);

            if (user is null)
            {
                return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            var identity = new ClaimsIdentity(
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                Claims.Name, Claims.Role);
            var Properties = new AuthenticationProperties();

            user.Subject = $"{user.user_id}_{auth.Constr}_{auth.Sysstr}";

            identity.AddClaim(Claims.Subject, user.Subject!);
            identity.AddClaim("id", user.user_id);
            // identity.AddClaim(Claims.Name, user.user_name);
            identity.AddClaim("constr", auth.Constr);
            identity.AddClaim("sysstr", auth.Sysstr);

            identity.SetScopes(Scopes.OfflineAccess);

            Properties.SetParameter("username", user.user_name ?? "null");

            if (user is UserWebDto userWeb)
            {
                Properties.SetParameter("TwoFAuth", userWeb.two_fauth);
                Properties.SetParameter("password_expired", userWeb.password_expired);
            }

            identity.SetDestinations(claim => claim.Type switch
            {
                Claims.Scope => Array.Empty<string>(),
                _ => [Destinations.AccessToken]
            });


            return SignIn(new ClaimsPrincipal(identity), Properties,
                          OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }
        catch (Exception e)
        {
            return StatusCode(500, new ReturnDto(Code: "500", Message: e.ToString()));
        }

    }

    private async Task<IActionResult> HandleClientCredetials(OpenIddictRequest request)
    {
        if (request is null || request.ClientId is null) throw new Exception("request || clientId is null");
        var application = await _applicationManager
            .FindByClientIdAsync(request.ClientId)
            ?? throw new InvalidOperationException("The application cannot be found.");

        var identity = new ClaimsIdentity(
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            Claims.Name,
            Claims.Role);


        identity.SetClaim(Claims.Subject,
            await _applicationManager.GetClientIdAsync(application));
        identity.SetClaim(Claims.Name,
            await _applicationManager.GetDisplayNameAsync(application));
        identity.SetClaim(Claims.Role, "admin");
        identity.SetClaim("connection", "string");


        identity.SetDestinations(static claim => claim.Type switch
        {
            // Claims.Name when claim.Subject.HasScope(Scopes.Profile)
            //     => [Destinations.AccessToken, Destinations.IdentityToken],
            _ => new[] { Destinations.AccessToken }
        });


        return SignIn(
            new ClaimsPrincipal(identity),
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private async Task<IActionResult> ValidatePasswordOrPin(string password = "", string pin = "")
    {

        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;

            bool hasCredential = false;

            if (!string.IsNullOrEmpty(password))
            {
                hasCredential = await _auth.ValidateUserCredential(authorization, UserPassword: password);
            }

            if (!string.IsNullOrEmpty(pin))
            {
                hasCredential = await _auth.ValidateUserCredential(authorization, UserPin: pin);
            }

            return hasCredential switch
            {
                false => new ReturnDto(Code: "404", Data: hasCredential),
                true => new ReturnDto(Code: "200", Data: hasCredential),
            };
        });

    }

}
