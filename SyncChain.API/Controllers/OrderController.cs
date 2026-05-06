using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SyncChain.API.Data;
using SyncChain.API.DTOs;
using SyncChain.API.Services;

namespace SyncChain.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrderController : ControllerBase
{
    private readonly OrderService _service;
    private readonly AppDbContext _db;

    public OrderController(OrderService service, AppDbContext db)
    {
        _service = service;
        _db = db;
    }

    [Authorize(Policy = "OrderWrite")]
    [HttpPost]
    public IActionResult CreateOrder(CreateOrderDTO dto)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized("Token khong hop le");

        var result = _service.CreateOrder(userId.Value, dto);

        return Ok(result);
    }

    [Authorize]
    [HttpGet]
    public IActionResult GetOrders()
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized();

        var role = GetRole();

        if (IsInternalRole(role))
        {
            return Ok(_db.DonHang
                .OrderByDescending(x => x.NgayTao)
                .Select(x => new
                {
                    x.MaDonHang,
                    x.MaNguoiDung,
                    x.TongTien,
                    x.NgayTao,
                    x.TrangThai
                })
                .ToList());
        }

        var orders = _db.DonHang
            .Where(x => x.MaNguoiDung == userId.Value)
            .OrderByDescending(x => x.NgayTao)
            .Select(x => new
            {
                x.MaDonHang,
                x.MaNguoiDung,
                x.TongTien,
                x.NgayTao,
                x.TrangThai
            })
            .ToList();

        return Ok(orders);
    }

    [Authorize]
    [HttpGet("{id}")]
    public IActionResult GetOrderDetail(int id)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized();

        var order = _db.DonHang.Find(id);
        if (order == null)
            return NotFound();

        if (!IsInternalRole(GetRole()) && order.MaNguoiDung != userId.Value)
            return Forbid();

        var details = _db.ChiTietDonHang
            .Include(x => x.SanPham)
            .Where(x => x.MaDonHang == id)
            .Select(x => new
            {
                x.MaSanPham,
                x.SoLuong,
                x.DonGia,
                SanPham = new
                {
                    x.SanPham.MaSanPham,
                    x.SanPham.TenSanPham,
                    x.SanPham.GiaBan,
                    x.SanPham.SoLuongTon,
                    x.SanPham.MucTonThap,
                    x.SanPham.TrangThai
                }
            })
            .ToList();

        return Ok(details);
    }

    [Authorize(Policy = "OrderManage")]
    [HttpGet("full")]
    public IActionResult GetFullOrders()
    {
        var orders = _db.DonHang
            .OrderByDescending(o => o.NgayTao)
            .Select(x => new
            {
                x.MaDonHang,
                x.MaNguoiDung,
                x.TongTien,
                x.NgayTao,
                x.TrangThai
            })
            .ToList();

        return Ok(orders);
    }

    [Authorize(Policy = "OrderManage")]
    [HttpPut("{id}/status")]
    public IActionResult UpdateStatus(int id, string status)
    {
        var order = _db.DonHang.Find(id);

        if (order == null)
            return NotFound();

        var validStatus = new[] { "pending", "processing", "done", "cancel" };

        if (!validStatus.Contains(status))
            return BadRequest("Trang thai khong hop le");

        if (order.TrangThai == "done")
            return BadRequest("Don da hoan thanh");

        order.TrangThai = status;
        _db.SaveChanges();

        return Ok("Cap nhat thanh cong");
    }

    private int? GetUserId()
    {
        var claim = User.FindFirst("user_id")?.Value;
        return int.TryParse(claim, out var userId) ? userId : null;
    }

    private string GetRole()
    {
        return User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
    }

    private static bool IsInternalRole(string role)
    {
        return role is "admin" or "manager" or "staff";
    }
}
