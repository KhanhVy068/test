using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncChain.API.DTOs.Product;
using SyncChain.API.Services;

namespace SyncChain.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductController : ControllerBase
{
    private readonly ProductService _service;

    public ProductController(ProductService service)
    {
        _service = service;
    }

    [Authorize]
    [HttpGet]
    public IActionResult GetAll()
    {
        return Ok(_service.GetAll());
    }

    [Authorize]
    [HttpGet("{id}")]
    public IActionResult GetById(int id)
    {
        return Ok(_service.GetById(id));
    }

    [Authorize]
    [HttpGet("{id}/detail")]
    public IActionResult GetDetail(int id)
    {
        return Ok(_service.GetDetail(id));
    }

    [Authorize(Policy = "ProductWrite")]
    [HttpPost]
    public IActionResult Create(CreateProductDTO dto)
    {
        return Ok(_service.Create(dto));
    }

    [Authorize(Policy = "ProductWrite")]
    [HttpPut("{id}")]
    public IActionResult Update(int id, UpdateProductDTO dto)
    {
        return Ok(_service.Update(id, dto));
    }

    [Authorize(Policy = "ProductWrite")]
    [HttpPost("{id}/import")]
    public IActionResult ImportStock(int id, ImportStockDTO dto)
    {
        return Ok(_service.ImportStock(id, dto.SoLuong, GetUserId(), dto.GhiChu));
    }

    [Authorize(Policy = "ProductWrite")]
    [HttpPut("{id}/status")]
    public IActionResult UpdateStatus(int id, string status)
    {
        return Ok(_service.UpdateStatus(id, status));
    }

    [Authorize(Policy = "ProductWrite")]
    [HttpPost("upload-image")]
    public async Task<IActionResult> UploadImage(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("File anh khong hop le");

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
            return BadRequest("Chi ho tro file anh jpg, jpeg, png, webp, gif");

        var uploadRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "products");
        Directory.CreateDirectory(uploadRoot);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(uploadRoot, fileName);

        await using (var stream = System.IO.File.Create(filePath))
        {
            await file.CopyToAsync(stream);
        }

        return Ok(new { imageUrl = $"/uploads/products/{fileName}" });
    }

    [Authorize(Policy = "ProductWrite")]
    [HttpDelete("{id}")]
    public IActionResult Delete(int id)
    {
        _service.Delete(id);
        return Ok("Da xoa");
    }

    private int? GetUserId()
    {
        var claim = User.FindFirst("user_id")?.Value;
        return int.TryParse(claim, out var userId) ? userId : null;
    }
}
