namespace WebAuthen.Models;

public record Cypto(string code_enc);

public record ReturnDto(string Code, string? Message = null, object? Headers = null, object? Data = null);

// Data interface for Authorization decrypted
public record AuthorizationDto(int Id, string Constr, string Sysstr);

// Data interface for Authenticate decrypted
public record AuthenticateDto(string Constr, string Sysstr);

