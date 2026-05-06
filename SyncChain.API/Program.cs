using Microsoft.EntityFrameworkCore;
using SyncChain.API.Data;
using SyncChain.API.Services; 
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using SyncChain.API.Models;
using System.Security.Claims;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// DI
builder.Services.AddScoped<AuthService>();

// DB
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=../database/SyncChain.db"));

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "SyncChain API", 
        Version = "v1" 
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập: Bearer {token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
});


var jwtSettings = builder.Configuration.GetSection("Jwt");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})


.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings["Key"]!)
        )
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("admin"));

    options.AddPolicy("StaffOnly", policy =>
        policy.RequireRole("staff", "manager", "admin"));

    options.AddPolicy("ProductWrite", policy =>
        policy.RequireRole("manager", "admin"));

    options.AddPolicy("OrderWrite", policy =>
        policy.RequireRole("customer", "staff", "manager", "admin"));

    options.AddPolicy("OrderManage", policy =>
        policy.RequireRole("staff", "manager", "admin"));
});

builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<OrderService>();

var app = builder.Build();


using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    try
    {
        db.Database.ExecuteSqlRaw("ALTER TABLE SanPham ADD COLUMN MoTa TEXT NOT NULL DEFAULT ''");
    }
    catch
    {
        // Column already exists in an upgraded local database.
    }
    db.Database.ExecuteSqlRaw("""
        CREATE TABLE IF NOT EXISTS GiaoDichKho (
            MaGiaoDich INTEGER NOT NULL CONSTRAINT PK_GiaoDichKho PRIMARY KEY AUTOINCREMENT,
            MaSanPham INTEGER NOT NULL,
            Loai TEXT NOT NULL,
            SoLuong INTEGER NOT NULL,
            ThoiGian TEXT NOT NULL,
            MaNguoiDung INTEGER NULL,
            GhiChu TEXT NOT NULL,
            CONSTRAINT FK_GiaoDichKho_SanPham_MaSanPham FOREIGN KEY (MaSanPham) REFERENCES SanPham (MaSanPham) ON DELETE CASCADE
        );
        """);

    var roles = new[]
    {
        new PhanQuyen { MaVaiTro = 1, TenVaiTro = "customer" },
        new PhanQuyen { MaVaiTro = 2, TenVaiTro = "staff" },
        new PhanQuyen { MaVaiTro = 3, TenVaiTro = "manager" },
        new PhanQuyen { MaVaiTro = 4, TenVaiTro = "admin" }
    };

    foreach (var role in roles)
    {
        var existingRole = db.PhanQuyen.FirstOrDefault(x => x.MaVaiTro == role.MaVaiTro);
        if (existingRole == null)
        {
            db.PhanQuyen.Add(role);
        }
        else
        {
            existingRole.TenVaiTro = role.TenVaiTro;
        }
    }

    var admin = db.NguoiDung.FirstOrDefault(x => x.Email == "admin@gmail.com");

    if (admin == null)
    {
        db.NguoiDung.Add(new NguoiDung
        {
            Email = "admin@gmail.com",
            TenDangNhap = "admin",
            MatKhauHash = BCrypt.Net.BCrypt.HashPassword("123456"),
            MaVaiTro = 4
        });
    }
    else
    {
        admin.MaVaiTro = 4; // 🔥 ép thành admin nếu đã tồn tại
    }

    db.SaveChanges();
}



app.UseSwagger();
app.UseSwaggerUI();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
