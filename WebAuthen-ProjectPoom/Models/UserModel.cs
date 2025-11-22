using System.Text.Json.Serialization;
using RepoDb.Attributes;

namespace WebAuthen.Models;

// Everythings about user authentication and data transferation

public record UserDto
{
    [JsonIgnore]
    public int user_id { get; init; }
    [JsonIgnore]
    public string? Subject { get; set; }
    public required string user_name { get; init; }
    public required string user_username { get; init; }
    public required string user_email { get; init; }
    public required string tel { get; init; }
    public required byte[] user_img { get; init; }
    public string? user_hostname { get; init; }
    public bool? email_verify { get; init; }
    public bool? tel_verify { get; init; }
    public bool? id_verify { get; init; }
    public required string user_type { get; init; }
}


public record UserWebDto : UserDto
{
    [Map("2fauth")]
    public required bool two_fauth { get; init; }

    public required bool password_expired { get; init; }
}

public class UserRegisterDto
{
    public string? Firstname { get; set; }
    public string? Lastname { get; set; }
    public string? IdCard { get; set; }
    public string? Address_Id { get; set; }
    public string? Address_Text { get; set; }
    public string? Tel { get; set; }
    public string? Telfax { get; set; }
    public string? User_Name { get; set; }
    public string? User_Username { get; set; }
    public string? User_Password { get; set; }
    public string? User_Email { get; set; }
    public string? User_Img { get; set; }
    public string? User_Status { get; set; }
    public string? Parent_Userweb_Id { get; set; }
    public string? user_pin { get; set; }
    public string? authen_code { get; set; }
    public string? platform_name { get; set; }
}

public record UserUpdateDto
{
    public int UserId { get; set; } = 0;
    public string? Firstname { get; set; } = "";
    public string? Lastname { get; set; } = "";
    public string? IdCard { get; set; } = "";
    public int? AddressId { get; set; } = 0;
    public string? AddressText { get; set; } = "";
    public string? Tel { get; set; } = "";
    public string? TelFax { get; set; } = "";
    public string? UserName { get; set; } = "";
    public string? UserUsername { get; set; } = "";
    public string? UserPassword { get; set; } = "";
    public string? UserEmail { get; set; } = "";
    public string? UserImg { get; set; } = "";
    public string? UserStatus { get; set; } = "";
    public string? UserPin { get; set; } = "";
    public bool? IdVerify { get; set; } = null;
    public bool? EmailVerify { get; set; } = null;
    public bool? TelVerify { get; set; } = null;
};

public record PasswordDto(string User_Password);

public record PinDto(string User_Pin);

public record EmailDto(string User_Email);

public record PhoneDto(string PhoneNumber);

public record AuthenCodeDto
{
    [Map("authen_code")]
    [JsonPropertyName("authen_code")]
    public required string AuthenCode { get; set; }
    [Map("platform_name")]
    [JsonPropertyName("platform_name")]
    public required string PlatformName { get; set; }
};


public record SaveTitleUsernameDto
{
    public required string user_hostname { get; set; }
}

public record SubaccountDto
{
    public required string user_name { get; set; }
    public required string user_username { get; set; }
    public required string user_password { get; set; }
    public required string created_by { get; set; }
    public required bool password_must_change { get; set; }
    public required bool password_change_disable { get; set; }
    public required bool password_never_expire { get; set; }
}

public record SubaccountUpdateDto
{
    public required string user_name { get; set; }
    public required string user_password { get; set; }
    public string? update_by { get; set; }
    public int? user_id { get; set; }
}

