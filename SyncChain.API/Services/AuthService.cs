using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SyncChain.API.Data;
using SyncChain.API.DTOs;
using SyncChain.API.Models;

namespace SyncChain.API.Services;

public class AuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public string Register(RegisterDTO dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var password = dto.Password.Trim();

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            throw new Exception("Thieu email hoac mat khau");

        if (_db.NguoiDung.Any(x => x.Email == email))
            throw new Exception("Email da ton tai");

        if (password.Length < 6)
            throw new Exception("Mat khau phai >= 6 ky tu");

        var role = _db.PhanQuyen.FirstOrDefault(x => x.TenVaiTro == "customer");
        if (role == null)
            throw new Exception("Chua co role customer trong DB");

        var user = new NguoiDung
        {
            Email = email,
            TenDangNhap = email,
            MatKhauHash = BCrypt.Net.BCrypt.HashPassword(password),
            MaVaiTro = role.MaVaiTro,
            IsActive = true
        };

        _db.NguoiDung.Add(user);
        _db.SaveChanges();

        return "Dang ky thanh cong";
    }

    public object Login(LoginDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            throw new Exception("Thieu email hoac mat khau");

        var email = dto.Email.Trim().ToLowerInvariant();
        var password = dto.Password.Trim();

        var user = _db.NguoiDung.FirstOrDefault(x => x.Email == email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.MatKhauHash))
        {
            Console.WriteLine($"LOGIN FAIL: {dto.Email}");
            throw new Exception("Sai thong tin dang nhap");
        }

        if (!user.IsActive)
        {
            Console.WriteLine($"LOGIN BLOCKED: {dto.Email}");
            throw new Exception("Tai khoan bi khoa");
        }

        var roleName = user.MaVaiTro switch
        {
            1 => "customer",
            2 => "staff",
            3 => "manager",
            4 => "admin",
            _ => "unknown"
        };

        var jwtSettings = _config.GetSection("Jwt");

        var claims = new[]
        {
            new Claim("user_id", user.MaNguoiDung.ToString()),
            new Claim(ClaimTypes.Role, roleName)
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings["Key"]!)
        );

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.Now.AddHours(2),
            signingCredentials: creds
        );

        Console.WriteLine($"LOGIN SUCCESS: {user.Email}");

        return new
        {
            token = new JwtSecurityTokenHandler().WriteToken(token),
            user = new
            {
                user.MaNguoiDung,
                user.Email,
                role = roleName
            }
        };
    }
}
