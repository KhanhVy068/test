namespace SyncChain.API.DTOs.Admin;

public class UpdateInternalUserDTO
{
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
