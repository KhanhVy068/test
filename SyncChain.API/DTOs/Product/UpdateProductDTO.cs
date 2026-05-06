namespace SyncChain.API.DTOs.Product;

public class UpdateProductDTO
{
    public string TenSanPham { get; set; } = string.Empty;
    public decimal GiaBan { get; set; }
    public int SoLuongTon { get; set; }
    public string HinhAnhUrl { get; set; } = string.Empty;
    public string MoTa { get; set; } = string.Empty;
    public string? TrangThai { get; set; }
}
