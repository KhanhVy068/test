using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using SyncChain.Desktop.Models;

namespace SyncChain.Desktop.Services;

public sealed class SyncChainApiClient
{
	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
	private static readonly Uri ApiBaseAddress = new("http://localhost:5292/");
	private readonly HttpClient _httpClient;

	public static SyncChainApiClient Instance { get; } = new();

	public string? Token { get; private set; }
	public ApiUser? CurrentUser { get; private set; }

	private SyncChainApiClient()
	{
		_httpClient = new HttpClient
		{
			BaseAddress = ApiBaseAddress
		};
	}

	public async Task<ApiUser> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
	{
		var response = await _httpClient.PostAsJsonAsync("api/Auth/login", new
		{
			Email = email,
			Password = password
		}, JsonOptions, cancellationToken);

		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException(await ReadErrorAsync(response, cancellationToken));
		}

		var result = await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions, cancellationToken)
			?? throw new InvalidOperationException("Backend khong tra ve du lieu dang nhap.");

		Token = result.Token;
		CurrentUser = result.User;
		_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);

		return CurrentUser;
	}

	public async Task RegisterAsync(string email, string password, CancellationToken cancellationToken = default)
	{
		var response = await _httpClient.PostAsJsonAsync("api/Auth/register", new
		{
			Email = email,
			Password = password
		}, JsonOptions, cancellationToken);

		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException(await ReadErrorAsync(response, cancellationToken));
		}
	}

	public async Task<IReadOnlyList<ProductItem>> GetProductsAsync(CancellationToken cancellationToken = default)
	{
		var response = await _httpClient.GetAsync("api/Product", cancellationToken);
		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException(await ReadErrorAsync(response, cancellationToken));
		}

		var products = await response.Content.ReadFromJsonAsync<List<ApiProduct>>(JsonOptions, cancellationToken)
			?? new List<ApiProduct>();

		return products.Select(MapProduct).ToList();
	}

	public async Task<ProductItem> GetProductAsync(int productId, CancellationToken cancellationToken = default)
	{
		var response = await _httpClient.GetAsync($"api/Product/{productId}", cancellationToken);
		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException(await ReadErrorAsync(response, cancellationToken));
		}

		var product = await response.Content.ReadFromJsonAsync<ApiProduct>(JsonOptions, cancellationToken)
			?? throw new InvalidOperationException("Backend khong tra ve san pham.");

		return MapProduct(product);
	}

	public async Task<ProductDetailData> GetProductDetailAsync(int productId, CancellationToken cancellationToken = default)
	{
		var response = await _httpClient.GetAsync($"api/Product/{productId}/detail", cancellationToken);
		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException(await ReadErrorAsync(response, cancellationToken));
		}

		var detail = await response.Content.ReadFromJsonAsync<ApiProductDetail>(JsonOptions, cancellationToken)
			?? throw new InvalidOperationException("Backend khong tra ve chi tiet san pham.");

		return new ProductDetailData
		{
			Product = MapProduct(detail.Product),
			SoldCount = detail.SoldCount,
			Revenue = detail.Revenue,
			StockHistory = detail.StockHistory
				.OrderByDescending(x => x.ThoiGian)
				.Select(MapStockHistory)
				.ToList()
		};
	}

	public async Task<ProductItem> CreateProductAsync(string name, decimal price, int stockQuantity, string imageUrl, string description, CancellationToken cancellationToken = default)
	{
		var response = await _httpClient.PostAsJsonAsync("api/Product", new
		{
			TenSanPham = name,
			GiaBan = price,
			SoLuongTon = stockQuantity,
			HinhAnhUrl = ToStoredImageUrl(imageUrl),
			MoTa = description
		}, JsonOptions, cancellationToken);

		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException(await ReadErrorAsync(response, cancellationToken));
		}

		var product = await response.Content.ReadFromJsonAsync<ApiProduct>(JsonOptions, cancellationToken)
			?? throw new InvalidOperationException("Backend khong tra ve san pham vua tao.");

		return MapProduct(product);
	}

	public async Task<string> UploadProductImageAsync(string filePath, CancellationToken cancellationToken = default)
	{
		if (!File.Exists(filePath))
			return filePath;

		await using var stream = File.OpenRead(filePath);
		using var content = new MultipartFormDataContent();
		using var fileContent = new StreamContent(stream);
		fileContent.Headers.ContentType = new MediaTypeHeaderValue(GetImageContentType(filePath));
		content.Add(fileContent, "file", Path.GetFileName(filePath));

		var response = await _httpClient.PostAsync("api/Product/upload-image", content, cancellationToken);
		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException(await ReadErrorAsync(response, cancellationToken));
		}

		var result = await response.Content.ReadFromJsonAsync<UploadImageResponse>(JsonOptions, cancellationToken)
			?? throw new InvalidOperationException("Backend khong tra ve duong dan anh.");

		return result.ImageUrl;
	}

	public async Task<ProductItem> UpdateProductAsync(int productId, string name, decimal price, int stockQuantity, string imageUrl, string description, CancellationToken cancellationToken = default)
	{
		var response = await _httpClient.PutAsJsonAsync($"api/Product/{productId}", new
		{
			TenSanPham = name,
			GiaBan = price,
			SoLuongTon = stockQuantity,
			HinhAnhUrl = ToStoredImageUrl(imageUrl),
			MoTa = description
		}, JsonOptions, cancellationToken);

		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException(await ReadErrorAsync(response, cancellationToken));
		}

		var product = await response.Content.ReadFromJsonAsync<ApiProduct>(JsonOptions, cancellationToken)
			?? throw new InvalidOperationException("Backend khong tra ve san pham vua cap nhat.");

		return MapProduct(product);
	}

	public async Task DeleteProductAsync(int productId, CancellationToken cancellationToken = default)
	{
		var response = await _httpClient.DeleteAsync($"api/Product/{productId}", cancellationToken);
		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException(await ReadErrorAsync(response, cancellationToken));
		}
	}

	public async Task<ProductItem> ImportProductStockAsync(int productId, int quantity, string note, CancellationToken cancellationToken = default)
	{
		var response = await _httpClient.PostAsJsonAsync($"api/Product/{productId}/import", new
		{
			SoLuong = quantity,
			GhiChu = note
		}, JsonOptions, cancellationToken);

		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException(await ReadErrorAsync(response, cancellationToken));
		}

		var product = await response.Content.ReadFromJsonAsync<ApiProduct>(JsonOptions, cancellationToken)
			?? throw new InvalidOperationException("Backend khong tra ve san pham.");

		return MapProduct(product);
	}

	public async Task<ProductItem> UpdateProductStatusAsync(int productId, string status, CancellationToken cancellationToken = default)
	{
		var response = await _httpClient.PutAsync($"api/Product/{productId}/status?status={Uri.EscapeDataString(status)}", null, cancellationToken);
		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException(await ReadErrorAsync(response, cancellationToken));
		}

		var product = await response.Content.ReadFromJsonAsync<ApiProduct>(JsonOptions, cancellationToken)
			?? throw new InvalidOperationException("Backend khong tra ve san pham.");

		return MapProduct(product);
	}

	public async Task<IReadOnlyList<OrderItem>> GetOrdersAsync(CancellationToken cancellationToken = default)
	{
		var endpoint = IsInternalUser ? "api/Order/full" : "api/Order";
		var response = await _httpClient.GetAsync(endpoint, cancellationToken);
		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException(await ReadErrorAsync(response, cancellationToken));
		}

		var orders = await response.Content.ReadFromJsonAsync<List<ApiOrder>>(JsonOptions, cancellationToken)
			?? new List<ApiOrder>();

		return orders.Select(MapOrder).ToList();
	}

	public async Task<IReadOnlyList<OrderDetailLineItem>> GetOrderDetailsAsync(int orderId, CancellationToken cancellationToken = default)
	{
		var response = await _httpClient.GetAsync($"api/Order/{orderId}", cancellationToken);
		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException(await ReadErrorAsync(response, cancellationToken));
		}

		var details = await response.Content.ReadFromJsonAsync<List<ApiOrderDetail>>(JsonOptions, cancellationToken)
			?? new List<ApiOrderDetail>();

		return details.Select(MapOrderDetail).ToList();
	}

	public async Task UpdateOrderStatusAsync(int orderId, string status, CancellationToken cancellationToken = default)
	{
		var response = await _httpClient.PutAsync($"api/Order/{orderId}/status?status={Uri.EscapeDataString(status)}", null, cancellationToken);
		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException(await ReadErrorAsync(response, cancellationToken));
		}
	}

	public async Task<CreateOrderResult> CreateOrderAsync(IEnumerable<CreateOrderLineRequest> items, CancellationToken cancellationToken = default)
	{
		var response = await _httpClient.PostAsJsonAsync("api/Order", new
		{
			Items = items
		}, JsonOptions, cancellationToken);

		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException(await ReadErrorAsync(response, cancellationToken));
		}

		return await response.Content.ReadFromJsonAsync<CreateOrderResult>(JsonOptions, cancellationToken)
			?? new CreateOrderResult();
	}

	public async Task<DashboardSnapshot> GetDashboardAsync(CancellationToken cancellationToken = default)
	{
		var response = await _httpClient.GetAsync("api/Report/dashboard", cancellationToken);
		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException(await ReadErrorAsync(response, cancellationToken));
		}

		var dashboard = await response.Content.ReadFromJsonAsync<ApiDashboard>(JsonOptions, cancellationToken)
			?? throw new InvalidOperationException("Backend khong tra ve dashboard.");

		return MapDashboard(dashboard);
	}

	public async Task<IReadOnlyList<InternalUserItem>> GetInternalUsersAsync(CancellationToken cancellationToken = default)
	{
		var response = await _httpClient.GetAsync("api/admin/users", cancellationToken);
		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException(await ReadErrorAsync(response, cancellationToken));
		}

		var users = await response.Content.ReadFromJsonAsync<List<ApiInternalUser>>(JsonOptions, cancellationToken)
			?? new List<ApiInternalUser>();

		return users.Select(MapInternalUser).ToList();
	}

	public async Task CreateInternalUserAsync(string email, string password, string username, string role, CancellationToken cancellationToken = default)
	{
		var response = await _httpClient.PostAsJsonAsync("api/admin/create-user", new
		{
			Email = email,
			Password = password,
			Username = username,
			Role = role
		}, JsonOptions, cancellationToken);

		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException(await ReadErrorAsync(response, cancellationToken));
		}
	}

	public async Task UpdateInternalUserAsync(int userId, string role, bool isActive, CancellationToken cancellationToken = default)
	{
		var response = await _httpClient.PutAsJsonAsync($"api/admin/users/{userId}", new
		{
			Role = role,
			IsActive = isActive
		}, JsonOptions, cancellationToken);

		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException(await ReadErrorAsync(response, cancellationToken));
		}
	}

	public async Task SetInternalUserActiveAsync(int userId, bool isActive, CancellationToken cancellationToken = default)
	{
		var response = await _httpClient.PutAsync($"api/admin/users/{userId}/active?isActive={isActive}", null, cancellationToken);
		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException(await ReadErrorAsync(response, cancellationToken));
		}
	}

	public async Task ResetInternalUserPasswordAsync(int userId, string password, CancellationToken cancellationToken = default)
	{
		var response = await _httpClient.PutAsJsonAsync($"api/admin/users/{userId}/password", new
		{
			Password = password
		}, JsonOptions, cancellationToken);

		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException(await ReadErrorAsync(response, cancellationToken));
		}
	}

	public bool IsInternalUser => CurrentUser?.Role is "admin" or "manager" or "staff";

	public bool CanManageOrders => IsInternalUser;

	public bool CanManageProducts => CurrentUser?.Role is "admin" or "manager";

	public bool CanManageUsers => CurrentUser?.Role is "admin";

	private static ProductItem MapProduct(ApiProduct product)
	{
		var initials = string.Concat(product.TenSanPham
			.Split(' ', StringSplitOptions.RemoveEmptyEntries)
			.Take(2)
			.Select(x => char.ToUpperInvariant(x[0])));

		if (string.IsNullOrWhiteSpace(initials))
		{
			initials = "SP";
		}

		var isLowStock = product.SoLuongTon > 0 && product.SoLuongTon <= product.MucTonThap;
		var isOutOfStock = product.SoLuongTon <= 0;
		var isActive = product.TrangThai.Contains("Hoat", StringComparison.OrdinalIgnoreCase)
			|| product.TrangThai.Contains("Hoạt", StringComparison.OrdinalIgnoreCase);
		var importPrice = Math.Round(product.GiaBan * 0.7m, 0);

		return new ProductItem
		{
			Id = product.MaSanPham,
			Code = $"SP-{product.MaSanPham:0000}",
			Name = product.TenSanPham,
			Description = string.IsNullOrWhiteSpace(product.MoTa) ? "Chua co mo ta san pham." : product.MoTa,
			Status = product.TrangThai,
			ImportPrice = importPrice,
			LowStockThreshold = product.MucTonThap,
			UnitPrice = product.GiaBan,
			StockQuantity = product.SoLuongTon,
			ImageUrl = NormalizeImageUrl(product.HinhAnhUrl),
			Price = product.GiaBan.ToString("N0", CultureInfo.InvariantCulture) + " VND",
			Stock = product.SoLuongTon.ToString(CultureInfo.InvariantCulture),
			BadgeText = isOutOfStock ? "Ngung ban" : isLowStock ? "Sap het hang" : isActive ? "Dang ban" : "Ngung ban",
			BadgeColor = isOutOfStock ? Colors.Firebrick : isLowStock ? Colors.Orange : isActive ? Colors.SeaGreen : Colors.Firebrick,
			Initials = initials
		};
	}

	private static OrderItem MapOrder(ApiOrder order)
	{
		var statusColor = order.TrangThai switch
		{
			"done" => Colors.SeaGreen,
			"processing" => Colors.RoyalBlue,
			"cancel" => Colors.Firebrick,
			_ => Colors.Orange
		};

		return new OrderItem
		{
			Id = order.MaDonHang,
			Code = $"DH-{order.MaDonHang:0000}",
			Customer = $"Nguoi dung #{order.MaNguoiDung}",
			Email = CurrentUserLabel(order.MaNguoiDung),
			CreatedAt = order.NgayTao.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture),
			Total = order.TongTien.ToString("N0", CultureInfo.InvariantCulture) + " VND",
			Status = order.TrangThai,
			StatusColor = statusColor,
			Initials = "ND"
		};
	}

	private static OrderDetailLineItem MapOrderDetail(ApiOrderDetail detail)
	{
		var productName = detail.SanPham?.TenSanPham ?? $"San pham #{detail.MaSanPham}";
		var lineTotal = detail.DonGia * detail.SoLuong;
		var initials = string.Concat(productName
			.Split(' ', StringSplitOptions.RemoveEmptyEntries)
			.Take(2)
			.Select(x => char.ToUpperInvariant(x[0])));

		if (string.IsNullOrWhiteSpace(initials))
		{
			initials = "SP";
		}

		return new OrderDetailLineItem
		{
			ProductId = detail.MaSanPham,
			Name = productName,
			Variant = $"Ma SP: {detail.MaSanPham}",
			Quantity = detail.SoLuong.ToString(CultureInfo.InvariantCulture),
			UnitPrice = detail.DonGia.ToString("N0", CultureInfo.InvariantCulture) + " VND",
			LineTotal = lineTotal.ToString("N0", CultureInfo.InvariantCulture) + " VND",
			Initials = initials
		};
	}

	private static StockHistoryItem MapStockHistory(ApiStockHistory item)
	{
		return new StockHistoryItem
		{
			Time = item.ThoiGian.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture),
			Type = item.Loai,
			Quantity = item.SoLuong > 0
				? "+" + item.SoLuong.ToString(CultureInfo.InvariantCulture)
				: item.SoLuong.ToString(CultureInfo.InvariantCulture),
			Actor = item.MaNguoiDung.HasValue ? $"User #{item.MaNguoiDung.Value}" : "He thong",
			Note = item.GhiChu
		};
	}

	private static DashboardSnapshot MapDashboard(ApiDashboard dashboard)
	{
		var activePercent = dashboard.TotalProducts == 0
			? 0
			: (int)Math.Round(dashboard.ActiveProducts * 100d / dashboard.TotalProducts);

		return new DashboardSnapshot
		{
			Stats =
			[
				new() { Title = "Tong san pham", Value = dashboard.TotalProducts.ToString(CultureInfo.InvariantCulture), Subtitle = $"{dashboard.ActiveProducts} dang ban", Icon = "SP", Accent = Colors.RoyalBlue },
				new() { Title = "Tong don hang", Value = dashboard.TotalOrders.ToString(CultureInfo.InvariantCulture), Subtitle = $"{dashboard.PendingOrders} cho xu ly", Icon = "DH", Accent = Colors.SeaGreen },
				new() { Title = "Doanh thu", Value = dashboard.TotalRevenue.ToString("N0", CultureInfo.InvariantCulture) + " VND", Subtitle = "Tong doanh thu", Icon = "$", Accent = Colors.DarkCyan },
				new() { Title = "Hom nay", Value = dashboard.TodayRevenue.ToString("N0", CultureInfo.InvariantCulture) + " VND", Subtitle = "Doanh thu ngay", Icon = "D", Accent = Colors.MediumPurple },
				new() { Title = "Canh bao kho", Value = dashboard.LowStockProducts.ToString(CultureInfo.InvariantCulture), Subtitle = $"{dashboard.OutOfStockProducts} ngung ban", Icon = "!", Accent = Colors.OrangeRed }
			],
			LowStockAlerts = dashboard.LowStock.Select(x => new AlertItem
			{
				Name = x.TenSanPham,
				Code = $"SP-{x.MaSanPham:0000}",
				StockText = $"Con {x.SoLuongTon} / muc {x.MucTonThap}",
				Accent = x.SoLuongTon <= 0 ? Colors.Firebrick : Colors.Orange
			}).ToList(),
			Activities = dashboard.RecentActivities.Select(x => new ActivityItem
			{
				Title = x.Title,
				Time = x.Time.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture),
				Icon = x.Type == "stock" ? "K" : "D",
				Accent = x.Type == "stock" ? Colors.RoyalBlue : Colors.SeaGreen
			}).ToList(),
			OrderTrend = dashboard.Trend.Select(x => new OrderTrendItem
			{
				Label = x.Label,
				Orders = x.TotalOrders.ToString(CultureInfo.InvariantCulture),
				Completed = x.CompletedOrders.ToString(CultureInfo.InvariantCulture),
				Processing = x.ProcessingOrders.ToString(CultureInfo.InvariantCulture),
				Revenue = x.Revenue.ToString("N0", CultureInfo.InvariantCulture) + " VND"
			}).ToList(),
			TopProducts = dashboard.TopProducts.Select(x => new TopProductItem
			{
				Code = $"SP-{x.MaSanPham:0000}",
				Name = x.TenSanPham,
				Quantity = x.SoLuongBan.ToString(CultureInfo.InvariantCulture),
				Revenue = x.DoanhThu.ToString("N0", CultureInfo.InvariantCulture) + " VND",
				Accent = Colors.RoyalBlue
			}).ToList(),
			InventoryPercent = activePercent.ToString(CultureInfo.InvariantCulture) + "%",
			InventorySubtitle = $"{dashboard.ActiveProducts}/{dashboard.TotalProducts} san pham dang ban"
		};
	}

	private static InternalUserItem MapInternalUser(ApiInternalUser user)
	{
		return new InternalUserItem
		{
			Id = user.MaNguoiDung,
			Username = user.TenDangNhap,
			Email = user.Email,
			Role = user.Role,
			IsActive = user.IsActive
		};
	}

	private static string NormalizeImageUrl(string imageUrl)
	{
		if (string.IsNullOrWhiteSpace(imageUrl))
			return string.Empty;

		if (imageUrl.StartsWith("/", StringComparison.Ordinal))
			return new Uri(ApiBaseAddress, imageUrl.TrimStart('/')).ToString();

		return imageUrl;
	}

	private static string ToStoredImageUrl(string imageUrl)
	{
		if (string.IsNullOrWhiteSpace(imageUrl))
			return string.Empty;

		if (Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri) && uri.Host == ApiBaseAddress.Host && uri.Port == ApiBaseAddress.Port)
			return uri.AbsolutePath;

		return imageUrl;
	}

	private static string GetImageContentType(string filePath)
	{
		return Path.GetExtension(filePath).ToLowerInvariant() switch
		{
			".png" => "image/png",
			".webp" => "image/webp",
			".gif" => "image/gif",
			_ => "image/jpeg"
		};
	}

	private static string CurrentUserLabel(int userId)
	{
		return $"user-{userId}@syncchain.local";
	}

	private static async Task<string> ReadErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
	{
		var message = await response.Content.ReadAsStringAsync(cancellationToken);
		return string.IsNullOrWhiteSpace(message)
			? $"Backend tra ve loi {(int)response.StatusCode}."
			: message.Trim('"');
	}

	private sealed class LoginResponse
	{
		public string Token { get; set; } = string.Empty;
		public ApiUser User { get; set; } = new();
	}

	private sealed class UploadImageResponse
	{
		public string ImageUrl { get; set; } = string.Empty;
	}

	private sealed class ApiProductDetail
	{
		public ApiProduct Product { get; set; } = new();
		public int SoldCount { get; set; }
		public decimal Revenue { get; set; }
		public List<ApiStockHistory> StockHistory { get; set; } = new();
	}

	private sealed class ApiStockHistory
	{
		public DateTime ThoiGian { get; set; }
		public string Loai { get; set; } = string.Empty;
		public int SoLuong { get; set; }
		public int? MaNguoiDung { get; set; }
		public string GhiChu { get; set; } = string.Empty;
	}

	private sealed class ApiDashboard
	{
		public int TotalProducts { get; set; }
		public int ActiveProducts { get; set; }
		public int LowStockProducts { get; set; }
		public int OutOfStockProducts { get; set; }
		public int TotalOrders { get; set; }
		public int PendingOrders { get; set; }
		public int CompletedOrders { get; set; }
		public int CancelledOrders { get; set; }
		public decimal TotalRevenue { get; set; }
		public decimal TodayRevenue { get; set; }
		public List<ApiOrderTrend> Trend { get; set; } = new();
		public List<ApiTopProduct> TopProducts { get; set; } = new();
		public List<ApiLowStockProduct> LowStock { get; set; } = new();
		public List<ApiRecentActivity> RecentActivities { get; set; } = new();
	}

	private sealed class ApiOrderTrend
	{
		public string Label { get; set; } = string.Empty;
		public int TotalOrders { get; set; }
		public int CompletedOrders { get; set; }
		public int ProcessingOrders { get; set; }
		public decimal Revenue { get; set; }
	}

	private sealed class ApiTopProduct
	{
		public int MaSanPham { get; set; }
		public string TenSanPham { get; set; } = string.Empty;
		public int SoLuongBan { get; set; }
		public decimal DoanhThu { get; set; }
	}

	private sealed class ApiLowStockProduct
	{
		public int MaSanPham { get; set; }
		public string TenSanPham { get; set; } = string.Empty;
		public int SoLuongTon { get; set; }
		public int MucTonThap { get; set; }
	}

	private sealed class ApiRecentActivity
	{
		public string Title { get; set; } = string.Empty;
		public DateTime Time { get; set; }
		public string Type { get; set; } = string.Empty;
	}

	private sealed class ApiInternalUser
	{
		public int MaNguoiDung { get; set; }
		public string TenDangNhap { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;
		public bool IsActive { get; set; }
		public string Role { get; set; } = string.Empty;
	}

	public sealed class ApiUser
	{
		public int MaNguoiDung { get; set; }
		public string Email { get; set; } = string.Empty;
		public string Role { get; set; } = string.Empty;
	}

	public sealed class CreateOrderLineRequest
	{
		public int MaSanPham { get; set; }
		public int SoLuong { get; set; }
	}

	public sealed class CreateOrderResult
	{
		public string Message { get; set; } = string.Empty;
		public int MaDonHang { get; set; }
		public decimal TongTien { get; set; }
	}

	private sealed class ApiProduct
	{
		public int MaSanPham { get; set; }
		public string TenSanPham { get; set; } = string.Empty;
		public decimal GiaBan { get; set; }
		public int SoLuongTon { get; set; }
		public int MucTonThap { get; set; }
		public string TrangThai { get; set; } = string.Empty;
		public string HinhAnhUrl { get; set; } = string.Empty;
		public string MoTa { get; set; } = string.Empty;
	}

	private sealed class ApiOrder
	{
		public int MaDonHang { get; set; }
		public int MaNguoiDung { get; set; }
		public decimal TongTien { get; set; }
		public DateTime NgayTao { get; set; }
		public string TrangThai { get; set; } = string.Empty;
	}

	private sealed class ApiOrderDetail
	{
		public int MaSanPham { get; set; }
		public int SoLuong { get; set; }
		public decimal DonGia { get; set; }
		public ApiProduct? SanPham { get; set; }
	}
}
