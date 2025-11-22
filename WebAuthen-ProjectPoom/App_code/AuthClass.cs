using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;
using RepoDb;
using System.Data;
using WebAuthen.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WebAuthen.App_code;

public class AuthClass
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    private readonly ICryptoTransform encryptor;
    private readonly ICryptoTransform decryptor;
    private static readonly Encoding Utf8 = Encoding.UTF8;
    private readonly DynamicConnectionService _dynamicConnection;

    public AuthClass(IConfiguration configuration, DynamicConnectionService dynamicConnection)
    {
        string? keyBase64 = configuration["Encryption:Key"];
        string? ivBase64 = configuration["Encryption:IV"];

        if (string.IsNullOrEmpty(keyBase64) || string.IsNullOrEmpty(ivBase64))
            throw new ArgumentException("Key or IV configuration is missing.");

        _key = Convert.FromBase64String(keyBase64);
        _iv = Convert.FromBase64String(ivBase64);

        if (_key.Length != 32 || _iv.Length != 16)
            throw new ArgumentException("Invalid Key/IV length. Key must be 32 bytes (256-bit), IV must be 16 bytes.");

        // Initialize encryptor and decryptor
        using (Aes aes = Aes.Create())
        {
            aes.Key = _key;
            aes.IV = _iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            encryptor = aes.CreateEncryptor();
            decryptor = aes.CreateDecryptor();
        }

        // _connectionString = configuration.GetConnectionString("DefaultConnection")
        //                     ?? throw new ArgumentException("DefaultConnection string is missing.");
        _dynamicConnection = dynamicConnection;
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return "";

        using MemoryStream ms = new();
        using CryptoStream cs = new(ms, encryptor, CryptoStreamMode.Write);
        byte[] inputBytes = Utf8.GetBytes(plainText);
        cs.Write(inputBytes, 0, inputBytes.Length);
        cs.FlushFinalBlock();
        return Convert.ToBase64String(ms.ToArray());
    }

    public string Decrypt(string cipherBase64)
    {
        if (string.IsNullOrEmpty(cipherBase64)) return "";

        byte[] cipherBytes = Convert.FromBase64String(cipherBase64);

        using MemoryStream ms = new(cipherBytes);
        CryptoStream cs = new(ms, decryptor, CryptoStreamMode.Read);
        StreamReader reader = new(cs, Utf8);
        return reader.ReadToEnd();
    }

    public byte[] UnicodeStringToBytes(string str)
    {
        return Encoding.Unicode.GetBytes(str);
    }

    private string UnicodeBytesToString(byte[] bytes)
    {
        return Encoding.Unicode.GetString(bytes);
    }

    public async Task<bool> Register(UserRegisterDto register, string constr)
    {
        string sql = @"
        INSERT INTO tbl_sys_user_web 
        (firstname, lastname, idcard, address_id, address_text, tel, telfax, 
         user_name, user_username, user_password, user_email, user_img, user_status, 
         id_verify, email_verify, tel_verify, parent_userweb_id, user_pin, password_expire_date)
        VALUES 
        (@Firstname, @Lastname, @IdCard, @AddressId, @AddressText, @Tel, @Telfax,
         @User_Name, @User_UserName, @Password, @Email, @Img, @Status,
         0, 0, 0, @ParentUserwebId, @User_Pin, null);

        ";

        if (!string.IsNullOrEmpty(register.authen_code) && !string.IsNullOrEmpty(register.platform_name))
        {
            sql += $@"
            DECLARE @userwbId INT;
            SET @userwbId = SCOPE_IDENTITY();
            
            INSERT INTO tbl_sys_user_pac (userweb_id, authen_code,pa_id ,pac_type)
            SELECT @userwbId,@authenCode,u.pa_id,1
            FROM tbl_sys_user_platformauthen u
            WHERE platform_name = @platform_name;
             
            ";
        }

        byte[]? userImg = string.IsNullOrEmpty(register.User_Img) ? null : Convert.FromBase64String(register.User_Img);

        var parameters = new
        {
            Firstname = register.Firstname.NullIfEmpty(),
            Lastname = register.Lastname.NullIfEmpty(),
            IdCard = register.IdCard.NullIfEmpty(),
            AddressId = register.Address_Id.NullIfEmpty(),
            AddressText = register.Address_Text.NullIfEmpty(),
            Tel = register.Tel.NullIfEmpty(),
            Telfax = register.Telfax.NullIfEmpty(),
            User_Name = register.User_Name.NullIfEmpty(),
            User_UserName = register.User_Username.NullIfEmpty(),
            Password = register.User_Password.NullIfEmpty(),
            Email = register.User_Email.NullIfEmpty(),
            Img = userImg,
            Status = register.User_Status.NullIfEmpty(),
            ParentUserwebId = register.Parent_Userweb_Id.NullIfEmpty(),
            User_Pin = register.user_pin.NullIfEmpty(),
            // PasswordExpireDate = DateTime.UtcNow.AddMonths(1),
            authenCode = register.authen_code.NullIfEmpty(),
            platform_name = register.platform_name.NullIfEmpty(),
        };



        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(constr);
        using SqlConnection connection = new(connectionString);
        using IDbTransaction transaction = connection.EnsureOpen().BeginTransaction();
        try
        {
            //***** 1. แบบเดิมไม่มี query *****
            // int rowsAffected = await connection.ExecuteNonQueryAsync(sql, parameters, transaction: transaction);

            //***** 2. แบบใืช้ Method Insert ไม่ต้องเขียน sql เอง *****
            // int newId = await connection.InsertAsync<RegisterModel, int>(
            //     tableName: "tbl_sys_user_web ",
            //     entity: register,
            //     transaction: transaction
            // );

            //***** 3. ต้องการเขียน sql เองใช้ Scalar ดึงแถวแรกคอลัมน์แรก
            // int newId = connection.ExecuteScalar<int>(sql, parameters, transaction: transaction);
            int rowsAffected = connection.ExecuteNonQuery(sql, parameters, transaction: transaction);
            transaction.Commit();
            return rowsAffected > 0;
        }
        catch (Exception)
        {
            transaction.Rollback();
            return false;
        }

    }

    public async Task<UserDto?> ValidateUserAsync(string user_username, string password, string loginType, string constr, string sysstr, string authenCode = "")
    {
        var targetConnStr = await _dynamicConnection.GetConnectionStringByNameAsync(constr);
        if (targetConnStr == null)
        {
            return null;
        }


        return sysstr switch
        {
            "sysuserweb" => UserWebAsync(targetConnStr, authenCode: authenCode, user: user_username, password: password, loginType: loginType, dataSelect: 1),
            "tbluser" => UserBackendAsync(targetConnStr, user: user_username, password: password, loginType: loginType, dataSelect: 1),
            _ => throw new Exception("Not have sysstr!!"),
        };
    }

    public async Task<UserDto?> GetUserData(string constr, string sysstr, int userId = 0, int dataSelect = 0)
    {
        string? targetConnStr = await _dynamicConnection.GetConnectionStringByNameAsync(constr);
        if (string.IsNullOrEmpty(targetConnStr)) return null;

        return sysstr switch
        {
            "sysuserweb" => UserWebAsync(targetConnStr, userId, dataSelect: dataSelect),
            "tbluser" => UserBackendAsync(targetConnStr, userId, dataSelect: dataSelect),
            _ => null
        };
    }

    private UserWebDto? UserWebAsync(
        string connectionString,
        int userwebId = 0,
        string user = "",
        string password = "",
        string authenCode = "",
        string loginType = "",
        int dataSelect = 0)
    {
        string strSelect;
        string condition;
        string sql = "";

        if (loginType == "TokenApp")
        {
            JObject jobjUser = JsonConvert.DeserializeObject<JObject>(Decrypt(user)) ?? [];
            JObject jobjPass = JsonConvert.DeserializeObject<JObject>(Decrypt(password)) ?? [];

            if (jobjUser.Value<DateTime>("exp") < DateTime.Now ||
                jobjPass.Value<DateTime>("exp") < DateTime.Now)
            {
                return null;
            }

            user = jobjUser.Value<string>("user") ?? "";
            password = jobjPass.Value<string>("password") ?? "";
        }

        strSelect = dataSelect switch
        {
            0 => @"userwb.userweb_id as user_id ,userwb.userweb_id, firstname, lastname, idcard, 
            address_id, address_text, tel, user_name, user_username, user_password,
            user_email, user_img, user_status, email_verify, tel_verify , id_verify,
            parent_userweb_id, user_pin, authen_code ,p.partner_id,p.partner_name",
            1 => @"userwb.userweb_id as user_id , user_name, user_username, [userwb].[2fauth],user_hostname,
            CASE WHEN ISNULL(userwb.password_expire_date, GETDATE()) <= GETDATE() THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS password_expired,
            SUBSTRING(user_email,1,2) + '*****@' + SUBSTRING(user_email,CHARINDEX('@',user_email)+1,len(user_email)) as user_email,
            '*******' + SUBSTRING(tel,len(tel)-2,3) tel ,user_img,
            email_verify, tel_verify , id_verify,
            CASE WHEN user_pin IS NULL THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END AS haspin, 'front-end' as user_type",
            2 => "user_img",
            _ => ""
        };

        if (userwebId == 0)
        {
            condition = loginType switch
            {
                "PasswordAuth" => "WHERE user_username = @user_username AND user_password = @password AND ISNULL(userwb.flg,0) != 9 AND (password_expire_date IS NULL OR password_expire_date > GETDATE())",
                "lineAuth" => "WHERE authen_code = @authenCode AND platform_name = 'lineauthen' AND ISNULL(userwb.flg,0) != 9",
                "linePassAuth" => "WHERE authen_code = @authenCode AND user_password = @password AND platform_name = 'lineauthen' AND ISNULL(userwb.flg,0) != 9",
                "GoogleAuth" => "WHERE authen_code = @authenCode AND platform_name = 'googleauthen' AND ISNULL(userwb.flg,0) != 9",
                "GooglePassAuth" => "WHERE authen_code = @authenCode AND user_password = @password AND platform_name = 'googleauthen' AND ISNULL(userwb.flg,0) != 9",
                "FBAuth" => "WHERE authen_code = @authenCode AND platform_name = 'fbauthen' AND ISNULL(userwb.flg,0) != 9",
                "FBPassAuth" => "WHERE authen_code = @authenCode AND user_password = @password AND platform_name = 'fbauthen' AND ISNULL(userwb.flg,0) != 9",
                "TokenApp" => "WHERE user_username = @user_username AND user_password = @password AND ISNULL(userwb.flg,0) != 9",
                "AppleAuth" => "WHERE authen_code = @authenCode AND platform_name = 'appleauthen' AND ISNULL(userwb.flg,0) != 9",
                _ => ""
            };


        }
        else
        {
            condition = "WHERE userwb.userweb_id = @userweb_id AND ISNULL(userwb.flg,0) != 9";
        }

        sql += $@"
        WITH l AS (
            SELECT p.userweb_id, p.authen_code, p.pa_id, platform_name
            FROM tbl_sys_user_pac p
            INNER JOIN tbl_sys_user_platformauthen u ON u.pa_id = p.pa_id
            WHERE pac_type = 1
        )
        SELECT {strSelect}
        FROM tbl_sys_user_web userwb
        LEFT JOIN tbl_m_partner_master p ON userwb.userweb_id = p.userweb_id AND ISNULL(p.partner_flg,0) NOT IN (9)
        LEFT JOIN l ON l.userweb_id = userwb.userweb_id
        {condition};
        ";

        try
        {
            using SqlConnection connection = new(connectionString);
            using IDbTransaction transaction = connection.EnsureOpen().BeginTransaction(IsolationLevel.Serializable);

            Dictionary<string, object> parameters = [];
            if (userwebId == 0)
            {
                parameters.Add("@user_username", user);
                parameters.Add("@password", password);
                parameters.Add("@authenCode", authenCode);
            }
            else
            {
                parameters.Add("@userweb_id", userwebId);
            }

            UserWebDto? userQuery = connection.ExecuteQuery<UserWebDto>(sql, parameters, transaction: transaction).FirstOrDefault();

            transaction.Commit();

            return userQuery;
        }
        catch (Exception)
        {
            throw;
        }


    }

    private UserDto? UserBackendAsync(
        string connectionString,
        int userId = 0,
        string user = "",
        string password = "",
        string loginType = "",
        int dataSelect = 0
    )
    {
        string strSelect;
        string condition;
        string log = "";

        if (loginType == "TokenApp")
        {
            JObject jobjUser = JsonConvert.DeserializeObject<JObject>(Decrypt(user)) ?? [];
            JObject jobjPass = JsonConvert.DeserializeObject<JObject>(Decrypt(password)) ?? [];

            if (jobjUser.Value<DateTime>("exp") < DateTime.Now ||
                jobjPass.Value<DateTime>("exp") < DateTime.Now)
            {
                return null;
            }

            user = jobjUser.Value<string>("user") ?? "";
            password = jobjPass.Value<string>("password") ?? "";
            log = $"user : {user} | pass : {password} | exp1 : {jobjUser.Value<DateTime>("exp")} | exp2 : {jobjPass.Value<DateTime>("exp")}";
        }

        if (!string.IsNullOrEmpty(log))
        {
            using SqlConnection connection = new(connectionString);
            using IDbTransaction? transaction = connection.EnsureOpen().BeginTransaction();

            string logSql = @"
            INSERT INTO log_data (log_txt)
            VALUES (@log_txt)";

            connection.ExecuteNonQueryAsync(logSql, new { log_txt = log }, transaction: transaction);
            transaction.Commit();

        }

        strSelect = dataSelect switch
        {
            0 => @"
            u.user_id,
            user_code,
            user_name,
            user_username,
            user_password,
            user_flg,
            user_menu_list,
            user_com.compid_list,
            user_com.parentcomp_id
            ",
            1 => @"
            u.user_id,
            user_name,
            SUBSTRING(user_username,1,2)
              + CASE WHEN CHARINDEX('@',user_username)=0 THEN '*****'
                     ELSE '*****@' + SUBSTRING(user_username,CHARINDEX('@',user_username)+1,LEN(user_username))
                END AS user_username,
            password_expire_date,
            password_must_change,
            password_chage_disable
            'back-end' AS user_type
            ",
            _ => "",
        };

        if (userId == 0)
        {
            condition = loginType switch
            {
                "PasswordAuth" => "WHERE user_username = @username AND user_password = @password AND ISNULL(user_flg,0)!=9  AND (password_expire_date IS NULL OR password_expire_date > GETDATE())",
                "lineAuth" => "WHERE authen_code    = @username AND user_pin     = @password AND platform_name='lineauthen' AND ISNULL(user_flg,0)!=9",
                "linePassAuth" => "WHERE authen_code    = @username AND user_password = @password AND platform_name='lineauthen' AND ISNULL(user_flg,0)!=9",
                "GoogleAuth" => "WHERE authen_code    = @username AND user_pin     = @password AND platform_name='googleauthen' AND ISNULL(user_flg,0)!=9",
                "GooglePassAuth" => "WHERE authen_code    = @username AND user_password = @password AND platform_name='googleauthen' AND ISNULL(user_flg,0)!=9",
                "FBAuth" => "WHERE authen_code    = @username AND user_pin     = @password AND platform_name='fbauthen' AND ISNULL(user_flg,0)!=9",
                "FBPassAuth" => "WHERE authen_code    = @username AND user_password = @password AND platform_name='fbauthen' AND ISNULL(user_flg,0)!=9",
                "TokenApp" => "WHERE user_username  = @username AND user_password = @password AND ISNULL(user_flg,0)!=9",
                _ => ""
            };
        }
        else
        {
            condition = "WHERE u.user_id = @user_id AND ISNULL(user_flg,0)!=9";
        }


        var sql = $@"
        WITH user_com AS (
            SELECT DISTINCT ST2.user_id,
                STUFF((
                    SELECT ',' + CAST(ST1.comp_id AS nvarchar)
                    FROM tbl_sys_user_com ST1
                    WHERE ST1.user_id = ST2.user_id AND ST1.uc_flg <> 9
                    FOR XML PATH('')), 1, 1, '') AS compid_list
               
            FROM tbl_sys_user_com ST2
            INNER JOIN tbl_m_company_master C
                ON ST2.comp_id = C.comp_id AND ISNULL(comp_flg,0)<>9
        ),
        l AS (
            SELECT p.userweb_id, p.authen_code, p.pa_id, platform_name
            FROM tbl_sys_user_pac p
            INNER JOIN tbl_sys_user_platformauthen u
                ON u.pa_id = p.pa_id
            WHERE pac_type = 2
        )
        SELECT {strSelect}
        FROM tbl_sys_user u
        LEFT JOIN user_com ON user_com.user_id = u.user_id
        LEFT JOIN l ON l.userweb_id  = u.user_id
        {condition}";

        try
        {
            using SqlConnection connection = new(connectionString);
            Dictionary<string, object> parameters = new()
            {
                { "@username", user },
                { "@password", password },
                { "@user_id", userId }
            };

            UserDto? userQuery = connection.ExecuteQuery<UserDto>(sql, parameters).FirstOrDefault();

            return userQuery;
        }
        catch (Exception)
        {
            throw;
        }

    }


    public async Task<bool> UserHasPin(AuthorizationDto authorization)
    {
        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(authorization.Constr);

        string tableNameAndCondition = authorization.Sysstr switch
        {
            "tbluser" => "FROM tbl_sys_user WHERE user_id = @UserId",
            "sysuserweb" => "FROM tbl_sys_user_web WHERE userweb_id = @UserId",
            _ => "",
        };

        string sql = @$"
        SELECT TOP(1) 1
        {tableNameAndCondition}
        AND user_pin IS NOT null";

        using SqlConnection connection = new(connectionString);
        using IDbTransaction transaction = connection.EnsureOpen().BeginTransaction();

        int? result = connection.ExecuteScalar<int?>(sql, new { UserId = authorization.Id }, transaction: transaction);
        transaction.Commit();

        return result.HasValue;
    }

    public async Task<bool> ValidateUserCredential(AuthorizationDto authorization, string UserPassword = "", string UserPin = "")
    {
        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(authorization.Constr)!;

        string sysstr = authorization.Sysstr;
        int UserId = authorization.Id;

        string tableNameAndCondition = sysstr switch
        {
            "tbluser" => "tbl_sys_user WHERE user_id",
            "sysuserweb" => "tbl_sys_user_web WHERE userweb_id",
            _ => "",
        };

        string flgColumn = sysstr switch
        {
            "tbluser" => "user_flg",
            "sysuserweb" => "flg",
            _ => "",
        };

        string sql = @$"
        SELECT TOP(1) 1
        FROM {tableNameAndCondition} = @UserId
        AND ISNULL({flgColumn}, 0) <> 9";

        if (!string.IsNullOrEmpty(UserPassword))
        {
            sql += " AND user_password = @UserPassword";
        }

        if (!string.IsNullOrEmpty(UserPin))
        {
            sql += " AND user_pin = @UserPin";
        }

        var parameters = new
        {
            UserId,
            UserPassword,
            UserPin
        };

        using SqlConnection connection = new(connectionString);
        using IDbTransaction transaction = connection.EnsureOpen().BeginTransaction();

        int? result = connection.ExecuteScalar<int?>(sql, parameters, transaction: transaction);

        transaction.Commit();

        return result.HasValue;
    }

    public async Task<bool> EmailExists(AuthenticateDto authenticate, string email)
    {
        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(authenticate.Constr);

        string sql = @"
        SELECT TOP(1) 1
        FROM tbl_sys_user_web
        WHERE user_email = @email";

        try
        {
            using SqlConnection connection = new(connectionString);
            using IDbTransaction transaction = connection.EnsureOpen().BeginTransaction();
            int? result = connection.ExecuteScalar<int?>(sql, new { email }, transaction: transaction);
            transaction.Commit();

            return result.HasValue;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<bool> CheckPhoneNumber(AuthenticateDto authenticate, string PhoneNumber, string? connectionString = "", int UserwebId = 0)
    {
        string sql = @"
        SELECT TOP(1) 1
        FROM tbl_sys_user_web
        WHERE tel = @PhoneNumber";

        if (UserwebId > 0)
            sql += " AND userweb_id = @UserwebId";

        try
        {
            if (string.IsNullOrEmpty(connectionString))
                connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(authenticate.Constr);
            using SqlConnection connection = new(connectionString);
            using IDbTransaction transaction = connection.EnsureOpen().BeginTransaction();

            int? result = connection.ExecuteScalar<int?>(sql, new { PhoneNumber, UserwebId }, transaction: transaction);

            transaction.Commit();

            return result.HasValue;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<List<dynamic>> GetDataPlatform_Authencode(AuthenticateDto authenticate, string AuthenCode = "", string UserwebId = "", string PlatformName = "")
    {
        string sql;
        string condition;
        string sysstr = authenticate.Sysstr;

        if (!string.IsNullOrEmpty(AuthenCode))
        {
            condition = " AND l.authen_code = @AuthenCode";
        }
        else
        {
            condition = " AND uw.userweb_id = @UserwebId";
        }

        if (!string.IsNullOrEmpty(PlatformName))
        {
            condition += " AND platform_name = @PlatformName";
        }

        if (!string.IsNullOrEmpty(sysstr))
        {
            condition += " AND pac_type = @PacType";
        }

        if (sysstr == "tbluser")
        {
            sql = @$"
            SELECT u.user_id, l.authen_code as line_userlineid
            FROM tbl_sys_user u
            INNER JOIN tbl_sys_user_pac l ON u.user_id = l.userweb_id
            INNER JOIN tbl_sys_user_platformauthen pa ON pa.pa_id = l.pa_id
            WHERE ISNULL(user_flag,0) != 9 {condition}";
        }
        else
        {
            sql = @$"
            SELECT uw.userweb_id, l.authen_code as line_userlineid
            FROM tbl_sys_user_web uw
            INNER JOIN tbl_sys_user_pac l ON uw.userweb_id = l.userweb_id
            INNER JOIN tbl_sys_user_platformauthen pa ON pa.pa_id = l.pa_id
            WHERE ISNULL(flg,0) != 9 {condition}";
        }

        try
        {
            string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(authenticate.Constr);
            using SqlConnection connection = new(connectionString);
            using IDbTransaction transaction = connection.EnsureOpen().BeginTransaction();

            var parameters = new
            {
                AuthenCode,
                UserwebId,
                PlatformName,
                PacType = (sysstr == "tbluser") ? 2 : 1
            };

            List<dynamic> result = connection.ExecuteQuery(sql, parameters, transaction: transaction).ToList();
            transaction.Commit();

            return result;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<bool> InsertPlatformAuthenCode(AuthorizationDto authorization, AuthenCodeDto authenCodeDto)
    {
        try
        {
            string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(authorization.Constr);
            string sql = @"
            IF EXISTS (SELECT * FROM tbl_sys_user_pac p
            INNER JOIN tbl_sys_user_platformauthen u ON u.pa_id = p.pa_id
            WHERE userweb_id = @userweb_id AND platform_name = @platform_name AND pac_type = @pac_type)
                BEGIN
                    UPDATE p
                    SET authen_code = @authen_code
                    FROM tbl_sys_user_pac p
                    INNER JOIN tbl_sys_user_platformauthen u ON u.pa_id = p.pa_id
                    WHERE userweb_id = @userweb_id AND platform_name = @platform_name AND pac_type = @pac_type
                END
            ELSE
                BEGIN
                    INSERT INTO tbl_sys_user_pac (userweb_id, authen_code,pa_id ,pac_type)
                    SELECT @userweb_id, @authen_code, u.pa_id ,@pac_type
                    FROM  tbl_sys_user_platformauthen u
                    WHERE platform_name = @platform_name
                END
            ";

            using SqlConnection connection = new(connectionString);
            using IDbTransaction transaction = connection.EnsureOpen().BeginTransaction();

            var parameters = new
            {
                userweb_id = authorization.Id,
                platform_name = authenCodeDto.PlatformName,
                pac_type = authorization.Sysstr == "tbluser" ? 2 : 1,
                authen_code = authenCodeDto.AuthenCode,
            };

            int rowsAffected = connection.ExecuteNonQuery(sql, parameters, transaction: transaction);
            transaction.Commit();

            if (rowsAffected > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<bool> EditUserData(AuthorizationDto authorization, UserUpdateDto userData)
    {
        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(authorization.Constr);
        userData.UserId = authorization.Id;
        string sysstr = authorization.Sysstr;

        var setParts = new List<string>();
        Dictionary<string, object> param = [];

        void Add(string column, object? value)
        {
            if (value is null) return;

            switch (value)
            {
                case string s when string.IsNullOrWhiteSpace(s):
                    return;
                case int i when i == 0:
                    return;
            }
            setParts.Add($"{column} = @{column}");
            param.Add(column, value);
        }

        byte[]? UserImg = string.IsNullOrEmpty(userData.UserImg) ? null : Convert.FromBase64String(userData.UserImg);

        Add("firstname", userData.Firstname);
        Add("lastname", userData.Lastname);
        Add("idcard", userData.IdCard);
        Add("address_id", userData.AddressId);
        Add("address_text", userData.AddressText);
        Add("tel", userData.Tel);
        Add("telfax", userData.TelFax);
        Add("user_name", userData.UserName);
        Add("user_username", userData.UserUsername);
        Add("user_password", userData.UserPassword);
        Add("user_email", userData.UserEmail);
        Add("user_img", UserImg);
        Add("user_status", userData.UserStatus);
        Add("user_pin", userData.UserPin);
        Add("id_verify", userData.IdVerify);
        Add("email_verify", userData.EmailVerify);
        Add("tel_verify", userData.TelVerify);

        if (setParts.Count == 0) return false;

        string sql;

        if (sysstr == "sysuserweb")
        {
            sql = $@"
            UPDATE tbl_sys_user_web
            SET    {string.Join(", ", setParts)}
            WHERE  userweb_id = @userweb_id
            AND  ISNULL(flg, 0) <> 9;";

            param.Add("userweb_id", userData.UserId);

        }
        else if (sysstr == "tbluser")
        {
            sql = $@"
            UPDATE tbl_sys_user
            SET    {string.Join(", ", setParts)}
            WHERE  user_id = @user_id
            AND  ISNULL(user_flg, 0) <> 9;";

            param.Add("user_id", userData.UserId);
        }
        else
        {
            sql = "";
        }


        try
        {
            using SqlConnection connection = new(connectionString);
            using IDbTransaction transaction = connection.EnsureOpen().BeginTransaction();

            int rowsAffected = connection.ExecuteNonQuery(sql, param, transaction: transaction);
            transaction.Commit();

            if (rowsAffected > 0) return true;

            return false;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<bool> HostnameInsertAsync(AuthorizationDto authorization, String HostName)
    {
        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(authorization.Constr);

        using SqlConnection connection = new(connectionString);
        await connection.EnsureOpenAsync();
        using IDbTransaction transaction = await connection.BeginTransactionAsync();

        string sql = @"UPDATE tbl_sys_user_web
        SET user_hostname = @HostName
        WHERE userweb_id = @userweb_id";

        var parameters = new
        {
            HostName,
            userweb_id = authorization.Id
        };

        int rowsAffected = await connection.ExecuteNonQueryAsync(sql, parameters, transaction: transaction);
        transaction.Commit();
        return rowsAffected > 0;
    }

    public async Task<bool> SubAccountInsertAsync(AuthorizationDto authorization, SubaccountDto subAccount)
    {
        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(authorization.Constr);

        string sql = @"
        INSERT INTO tbl_sys_user
        (user_name, user_username, user_password, created_by, created_date,userweb_id,user_flg,
        password_must_change,password_change_disable,password_expire_date)
        SELECT @UserName, @UserUsername, @UserPassword, @CreatedBy, @CreatedDate, @userweb_id, 0,
        @password_must_change, @password_change_disable,@password_expire_date
        WHERE NOT EXISTS (
            SELECT 1 FROM tbl_sys_user 
            WHERE user_username = @UserUsername AND userweb_id = @userweb_id AND ISNULL(user_flg,0) <> 9
        )
        ";

        DateTime? password_expire_date = null;

        if (!subAccount.password_never_expire)
        {
            password_expire_date = DateTime.UtcNow.AddMonths(6);
        }

        var parameters = new
        {
            UserName = subAccount.user_name,
            UserUsername = subAccount.user_username,
            UserPassword = subAccount.user_password,
            CreatedBy = subAccount.created_by,
            CreatedDate = DateTime.Now,
            userweb_id = authorization.Id,
            subAccount.password_must_change,
            subAccount.password_change_disable,
            password_expire_date
        };

        using SqlConnection connection = new(connectionString);
        await connection.EnsureOpenAsync();
        using IDbTransaction transaction = await connection.BeginTransactionAsync();

        int rowsAffected = await connection.ExecuteNonQueryAsync(sql, parameters, transaction: transaction);
        transaction.Commit();
        return rowsAffected > 0;
    }

    public async Task<List<dynamic>> SubAccountGetAsync(AuthorizationDto authorization)
    {
        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(authorization.Constr);

        using SqlConnection connection = new(connectionString);
        await connection.EnsureOpenAsync();
        using IDbTransaction transaction = await connection.BeginTransactionAsync();

        string sql = @"
        SELECT user_id, user_name, user_username, user_password, created_by ,user_flg ,password_must_change, password_change_disable ,password_expire_date
        FROM tbl_sys_user
        WHERE userweb_id = @userweb_id AND ISNULL(user_flg,0) <> 9
        ";

        var parameters = new
        {
            userweb_id = authorization.Id,
        };

        var result = await connection.ExecuteQueryAsync<dynamic>(sql, parameters, transaction: transaction);
        transaction.Commit();

        return result.ToList();
    }

    public async Task<bool> SubAccountUpdateAsync(AuthorizationDto authorization, SubaccountUpdateDto subAccount)
    {
        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(authorization.Constr);

        string sql = @"
        UPDATE tbl_sys_user
        SET user_name = @UserName, user_password = @UserPassword, update_by = @UpdateBy, update_date = @UpdateDate
        WHERE user_id = @UserId AND password_change_disable = 0 AND ISNULL(user_flg,0) <> 9
        ";

        var parameters = new
        {
            UserName = subAccount.user_name,
            UserPassword = subAccount.user_password,
            UpdateBy = subAccount.update_by,
            UpdateDate = DateTime.Now,
            UserId = subAccount.user_id
        };

        using SqlConnection connection = new(connectionString);
        await connection.EnsureOpenAsync();
        using IDbTransaction transaction = await connection.BeginTransactionAsync();

        int rowsAffected = await connection.ExecuteNonQueryAsync(sql, parameters, transaction: transaction);
        transaction.Commit();
        return rowsAffected > 0;
    }

    public async Task<bool> SubAccountDeleteAsync(AuthorizationDto authorization, int userId)
    {
        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(authorization.Constr);
        using SqlConnection connection = new(connectionString);
        await connection.EnsureOpenAsync();
        using IDbTransaction transaction = await connection.BeginTransactionAsync();

        string sql = @"
        UPDATE tbl_sys_user
        SET user_flg = 9, update_date = @update_date,
            update_by = @update_by
        WHERE user_id = @userId and userweb_id = @userweb_id AND ISNULL(user_flg, 0) <> 9
        ";

        var parameters = new
        {
            userId,
            userweb_id = authorization.Id,
            update_by = authorization.Id.ToString(),
            update_date = DateTime.Now
        };

        int rowsAffected = await connection.ExecuteNonQueryAsync(sql, parameters, transaction: transaction);
        transaction.Commit();
        return rowsAffected > 0;
    }

    public async Task<bool> SubAccountStatusToggleAsync(AuthorizationDto authorization, int userId)
    {
        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(authorization.Constr);
        using SqlConnection connection = new(connectionString);
        await connection.EnsureOpenAsync();
        using IDbTransaction transaction = await connection.BeginTransactionAsync();

        string sql = @"
        UPDATE tbl_sys_user
        SET user_flg = 1 - ISNULL(user_flg, 0),
            update_by = @update_by, update_date = @update_date
        WHERE user_id = @user_id AND userweb_id = @userweb_id AND ISNULL(user_flg, 0) <> 9
        ";

        var parameters = new
        {
            user_id = userId,
            userweb_id = authorization.Id,
            update_by = authorization.Id,
            update_date = DateTime.Now
        };

        int rowsAffected = await connection.ExecuteNonQueryAsync(sql, parameters, transaction: transaction);
        transaction.Commit();
        return rowsAffected > 0;
    }



}
