namespace WebAuthen.Data;

public class AuthModel
{
    public class UserEntity
    {
        public int user_id { get; set; }
        public string user_username { get; set; }
        public string user_password { get; set; }
        public string PasswordHash { get; set; }
    }
    public class cypto
    {
        public string code_enc { get; set; }

    }
}