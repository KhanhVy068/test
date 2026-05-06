namespace SyncChain.API.DTOs;

public class CreateInternalUserDTO
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string Role { get; set; } = string.Empty;
}
