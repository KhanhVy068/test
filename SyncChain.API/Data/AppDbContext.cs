using Microsoft.EntityFrameworkCore;
using SyncChain.API.Models;

namespace SyncChain.API.Data;
public class AppDbContext : DbContext
{
    public DbSet<NguoiDung> NguoiDung { get; set; }
    public DbSet<PhanQuyen> PhanQuyen { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<SanPham> SanPham { get; set; }

    public DbSet<DonHang> DonHang { get; set; }
    public DbSet<ChiTietDonHang> ChiTietDonHang { get; set; }
    public DbSet<GiaoDichKho> GiaoDichKho { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChiTietDonHang>()
            .HasOne(c => c.DonHang)
            .WithMany(o => o.ChiTietDonHang)
            .HasForeignKey(c => c.MaDonHang);

        modelBuilder.Entity<ChiTietDonHang>()
            .HasOne(c => c.SanPham)
            .WithMany()
            .HasForeignKey(c => c.MaSanPham);

        modelBuilder.Entity<GiaoDichKho>()
            .HasOne(x => x.SanPham)
            .WithMany()
            .HasForeignKey(x => x.MaSanPham);
    }
}
