using System.ComponentModel.DataAnnotations;

namespace SyncChain.API.Models;

public class GiaoDichKho
{
    [Key]
    public int MaGiaoDich { get; set; }

    public int MaSanPham { get; set; }

    public string Loai { get; set; } = string.Empty;

    public int SoLuong { get; set; }

    public DateTime ThoiGian { get; set; } = DateTime.Now;

    public int? MaNguoiDung { get; set; }

    public string GhiChu { get; set; } = string.Empty;

    public SanPham SanPham { get; set; } = null!;
}
