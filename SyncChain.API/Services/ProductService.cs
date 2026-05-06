using SyncChain.API.Data;
using SyncChain.API.Models;
using SyncChain.API.DTOs.Product;
using Microsoft.EntityFrameworkCore;

namespace SyncChain.API.Services;

public class ProductService
{
    private readonly AppDbContext _db;

    public ProductService(AppDbContext db)
    {
        _db = db;
    }

    public List<SanPham> GetAll()
    {
        return _db.SanPham.ToList();
    }

    public SanPham GetById(int id)
    {
        var sp = _db.SanPham.Find(id);
        if (sp == null) throw new Exception("Khong tim thay san pham");

        return sp;
    }

    public SanPham Create(CreateProductDTO dto)
    {
        var sp = new SanPham
        {
            TenSanPham = dto.TenSanPham,
            GiaBan = dto.GiaBan,
            SoLuongTon = dto.SoLuongTon,
            HinhAnhUrl = dto.HinhAnhUrl,
            MoTa = dto.MoTa,
            TrangThai = BuildStatus(dto.SoLuongTon)
        };

        _db.SanPham.Add(sp);
        _db.SaveChanges();

        return sp;
    }

    public SanPham Update(int id, UpdateProductDTO dto)
    {
        var sp = _db.SanPham.Find(id);
        if (sp == null) throw new Exception("Không tìm thấy sản phẩm");

        sp.TenSanPham = dto.TenSanPham;
        sp.GiaBan = dto.GiaBan;
        sp.SoLuongTon = dto.SoLuongTon;
        sp.HinhAnhUrl = dto.HinhAnhUrl;
        sp.MoTa = dto.MoTa;
        sp.TrangThai = BuildStatus(dto.SoLuongTon, dto.TrangThai ?? sp.TrangThai);

        _db.SaveChanges();

        return sp;
    }

    public void Delete(int id)
    {
        var sp = _db.SanPham.Find(id);
        if (sp == null) throw new Exception("Không tìm thấy sản phẩm");

        _db.SanPham.Remove(sp);
        _db.SaveChanges();
    }

    public SanPham ImportStock(int id, int quantity, int? userId, string note)
    {
        if (quantity <= 0)
            throw new InvalidOperationException("So luong nhap phai lon hon 0");

        var sp = _db.SanPham.Find(id);
        if (sp == null) throw new Exception("Khong tim thay san pham");

        sp.SoLuongTon += quantity;
        if (sp.SoLuongTon > 0 && sp.TrangThai == "Ngung ban")
            sp.TrangThai = "Hoat dong";

        _db.GiaoDichKho.Add(new GiaoDichKho
        {
            MaSanPham = id,
            Loai = "Nhap kho",
            SoLuong = quantity,
            MaNguoiDung = userId,
            GhiChu = string.IsNullOrWhiteSpace(note) ? "Nhap them hang" : note
        });

        _db.SaveChanges();
        return sp;
    }

    public SanPham UpdateStatus(int id, string status)
    {
        var sp = _db.SanPham.Find(id);
        if (sp == null) throw new Exception("Khong tim thay san pham");

        if (status != "Hoat dong" && status != "Ngung ban")
            throw new InvalidOperationException("Trang thai san pham khong hop le");

        if (sp.SoLuongTon <= 0)
            status = "Ngung ban";

        sp.TrangThai = status;
        _db.SaveChanges();
        return sp;
    }

    public object GetDetail(int id)
    {
        var sp = GetById(id);
        var soldLines = _db.ChiTietDonHang
            .Include(x => x.DonHang)
            .Where(x => x.MaSanPham == id && x.DonHang != null && x.DonHang.TrangThai != "cancel");
        var soldCount = soldLines.Sum(x => (int?)x.SoLuong) ?? 0;
        var revenue = soldLines.Sum(x => (decimal?)(x.SoLuong * x.DonGia)) ?? 0;

        var stockHistory = _db.GiaoDichKho
            .Where(x => x.MaSanPham == id)
            .OrderByDescending(x => x.ThoiGian)
            .Take(20)
            .Select(x => new
            {
                x.ThoiGian,
                x.Loai,
                x.SoLuong,
                x.MaNguoiDung,
                x.GhiChu
            })
            .ToList();

        var salesHistory = _db.ChiTietDonHang
            .Include(x => x.DonHang)
            .Where(x => x.MaSanPham == id && x.DonHang != null && x.DonHang.TrangThai != "cancel")
            .OrderByDescending(x => x.DonHang!.NgayTao)
            .Take(20)
            .Select(x => new
            {
                ThoiGian = x.DonHang!.NgayTao,
                Loai = "Xuat kho",
                SoLuong = -x.SoLuong,
                MaNguoiDung = (int?)x.DonHang.MaNguoiDung,
                GhiChu = $"Don hang #{x.MaDonHang}"
            })
            .ToList();

        return new
        {
            Product = sp,
            SoldCount = soldCount,
            Revenue = revenue,
            StockHistory = stockHistory.Concat(salesHistory)
                .OrderByDescending(x => x.ThoiGian)
                .Take(20)
                .ToList()
        };
    }

    private static string BuildStatus(int stockQuantity, string? requestedStatus = null)
    {
        if (stockQuantity <= 0)
            return "Ngung ban";

        return requestedStatus == "Ngung ban" ? "Ngung ban" : "Hoat dong";
    }
}
