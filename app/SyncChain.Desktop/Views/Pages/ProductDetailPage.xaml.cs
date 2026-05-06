using System.Globalization;
using SyncChain.Desktop.Models;
using SyncChain.Desktop.Services;

namespace SyncChain.Desktop.Views.Pages;

[QueryProperty(nameof(ProductId), "productId")]
public partial class ProductDetailPage : ContentPage
{
	private int _productId;
	private ProductItem? _product;
	private ProductDetailData? _detail;

	public string ProductId
	{
		set
		{
			if (int.TryParse(value, out var productId))
			{
				_productId = productId;
			}
		}
	}

	public ProductDetailPage()
	{
		InitializeComponent();
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		ApplyPermissions();
		await LoadProductAsync();
	}

	private async Task LoadProductAsync()
	{
		if (_productId <= 0)
		{
			await DisplayAlert("San pham", "Khong tim thay ma san pham.", "OK");
			await Shell.Current.GoToAsync("..");
			return;
		}

		try
		{
			_detail = await SyncChainApiClient.Instance.GetProductDetailAsync(_productId);
			_product = _detail.Product;
			RenderProduct(_detail);
		}
		catch (Exception ex)
		{
			await DisplayAlert("Khong tai duoc san pham", ex.Message, "OK");
		}
	}

	private void RenderProduct(ProductDetailData detail)
	{
		var product = detail.Product;
		var soldCount = detail.SoldCount;
		var revenue = detail.Revenue;
		var consumption = product.StockQuantity <= 0 ? 1 : Math.Min(1, Math.Max(0.05, soldCount / 100d));

		CodeLabel.Text = product.Code;
		NameLabel.Text = product.Name;
		DescriptionLabel.Text = product.Description;
		PriceLabel.Text = product.Price;
		ImportPriceLabel.Text = product.ImportPrice.ToString("N0", CultureInfo.InvariantCulture) + " VND";
		StockLabel.Text = product.StockQuantity.ToString("00", CultureInfo.InvariantCulture);
		StockLabel.TextColor = product.StockQuantity <= product.LowStockThreshold ? Colors.Firebrick : Colors.Black;
		LowStockLabel.Text = product.LowStockThreshold.ToString(CultureInfo.InvariantCulture);
		SoldLabel.Text = soldCount.ToString(CultureInfo.InvariantCulture);
		SoldMetricLabel.Text = soldCount.ToString(CultureInfo.InvariantCulture);
		RevenueMetricLabel.Text = revenue.ToString("N0", CultureInfo.InvariantCulture) + " VND";
		ConsumptionProgress.Progress = consumption;
		PerformanceNoteLabel.Text = product.StockQuantity <= 0
			? "San pham da het hang va duoc chuyen sang trang thai Ngung ban."
			: product.StockQuantity <= product.LowStockThreshold
				? "San pham dang ban tot nhung ton kho thap. Nen tao phieu nhap moi."
				: "Ton kho hien tai dang on dinh.";
		InventoryStateLabel.Text = product.BadgeText;
		RenderHistory(detail.StockHistory);

		StatusLabel.Text = product.BadgeText;
		StatusLabel.TextColor = Colors.White;
		StatusBadge.BackgroundColor = product.BadgeColor;
		ImageBadge.BackgroundColor = product.BadgeColor;
		ImageBadgeLabel.Text = product.BadgeText;

		InitialsLabel.Text = product.Initials;
		ProductImage.Source = CreateImageSource(product.ImageUrl);
		InitialsLabel.IsVisible = string.IsNullOrWhiteSpace(product.ImageUrl);

		NameEntry.Text = product.Name;
		PriceEntry.Text = product.UnitPrice.ToString(CultureInfo.InvariantCulture);
		StockEntry.Text = product.StockQuantity.ToString(CultureInfo.InvariantCulture);
		ImageEntry.Text = product.ImageUrl;
		DescriptionEditor.Text = product.Description;
	}

	private void RenderProduct(ProductItem product)
	{
		RenderProduct(new ProductDetailData
		{
			Product = product,
			SoldCount = _detail?.SoldCount ?? 0,
			Revenue = _detail?.Revenue ?? 0,
			StockHistory = _detail?.StockHistory ?? Array.Empty<StockHistoryItem>()
		});
	}

	private void RenderHistory(IReadOnlyList<StockHistoryItem> history)
	{
		HistoryList.Children.Clear();

		if (history.Count == 0)
		{
			HistoryList.Children.Add(new Label
			{
				Text = "Chua co lich su nhap/xuat kho.",
				TextColor = Colors.Gray,
				FontSize = 12
			});
			return;
		}

		foreach (var item in history.Take(8))
		{
			var row = new Grid
			{
				ColumnDefinitions =
				{
					new ColumnDefinition { Width = GridLength.Star },
					new ColumnDefinition { Width = GridLength.Star },
					new ColumnDefinition { Width = GridLength.Star },
					new ColumnDefinition { Width = GridLength.Star }
				}
			};

			row.Add(new Label { Text = item.Time, FontSize = 12 }, 0);
			row.Add(new Label { Text = item.Type, FontAttributes = FontAttributes.Bold, FontSize = 12 }, 1);
			row.Add(new Label { Text = item.Quantity, FontSize = 12 }, 2);
			row.Add(new Label { Text = item.Note, FontSize = 12 }, 3);
			HistoryList.Children.Add(row);
		}
	}

	private void ApplyPermissions()
	{
		var canManage = SyncChainApiClient.Instance.CanManageProducts;
		EditToggleButton.IsVisible = canManage;
		ImportButton.IsVisible = canManage;
		ManageActions.IsVisible = canManage;
	}

	private static ImageSource? CreateImageSource(string imageUrl)
	{
		if (string.IsNullOrWhiteSpace(imageUrl))
			return null;

		try
		{
			if (File.Exists(imageUrl))
				return ImageSource.FromStream(() => File.OpenRead(imageUrl));

			if (Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri))
			{
				if (uri.IsFile && File.Exists(uri.LocalPath))
					return ImageSource.FromStream(() => File.OpenRead(uri.LocalPath));

				if (uri.Scheme is "http" or "https")
					return ImageSource.FromUri(uri);
			}
		}
		catch
		{
			return null;
		}

		return null;
	}

	private async void OnSaveClicked(object? sender, EventArgs e)
	{
		if (_product == null)
			return;

		var name = NameEntry.Text?.Trim() ?? string.Empty;
		var imageUrl = ImageEntry.Text?.Trim() ?? string.Empty;
		var description = DescriptionEditor.Text?.Trim() ?? string.Empty;

		if (string.IsNullOrWhiteSpace(name))
		{
			await DisplayAlert("San pham", "Vui long nhap ten san pham.", "OK");
			return;
		}

		if (!decimal.TryParse(PriceEntry.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var price) || price < 0)
		{
			await DisplayAlert("San pham", "Gia ban khong hop le.", "OK");
			return;
		}

		if (!int.TryParse(StockEntry.Text, out var stockQuantity) || stockQuantity < 0)
		{
			await DisplayAlert("San pham", "Ton kho khong hop le.", "OK");
			return;
		}

		SaveButton.IsEnabled = false;

		try
		{
			if (File.Exists(imageUrl))
			{
				imageUrl = await SyncChainApiClient.Instance.UploadProductImageAsync(imageUrl);
			}

			_product = await SyncChainApiClient.Instance.UpdateProductAsync(_product.Id, name, price, stockQuantity, imageUrl, description);
			_detail = await SyncChainApiClient.Instance.GetProductDetailAsync(_product.Id);
			_product = _detail.Product;
			RenderProduct(_detail);
			EditForm.IsVisible = false;
			await DisplayAlert("San pham", "Cap nhat san pham thanh cong.", "OK");
		}
		catch (Exception ex)
		{
			await DisplayAlert("Khong cap nhat duoc san pham", ex.Message, "OK");
		}
		finally
		{
			SaveButton.IsEnabled = true;
		}
	}

	private async void OnPickDetailImageClicked(object? sender, EventArgs e)
	{
		try
		{
			var file = await FilePicker.Default.PickAsync(new PickOptions
			{
				PickerTitle = "Chon hinh anh san pham",
				FileTypes = FilePickerFileType.Images
			});

			if (file == null)
				return;

			ImageEntry.Text = file.FullPath ?? file.FileName;
			ProductImage.Source = CreateImageSource(ImageEntry.Text);
			InitialsLabel.IsVisible = string.IsNullOrWhiteSpace(ImageEntry.Text);
		}
		catch (Exception ex)
		{
			await DisplayAlert("Khong chon duoc anh", ex.Message, "OK");
		}
	}

	private void OnToggleEditClicked(object? sender, EventArgs e)
	{
		EditForm.IsVisible = !EditForm.IsVisible;
	}

	private async void OnImportClicked(object? sender, EventArgs e)
	{
		if (_product == null)
			return;

		var amountText = await DisplayPromptAsync("Nhap them hang", "So luong nhap them:", "Cap nhat", "Huy", keyboard: Keyboard.Numeric);
		if (!int.TryParse(amountText, out var amount) || amount <= 0)
			return;

		try
		{
			_product = await SyncChainApiClient.Instance.ImportProductStockAsync(_product.Id, amount, "Nhap them hang tu Desktop");
			_detail = await SyncChainApiClient.Instance.GetProductDetailAsync(_product.Id);
			_product = _detail.Product;
			RenderProduct(_detail);
		}
		catch (Exception ex)
		{
			await DisplayAlert("Khong cap nhat duoc ton kho", ex.Message, "OK");
		}
	}

	private async void OnStopSellingClicked(object? sender, EventArgs e)
	{
		if (_product == null)
			return;

		var confirmed = await DisplayAlert("Ngung ban", $"Chuyen {_product.Name} sang trang thai Ngung ban?", "Dong y", "Huy");
		if (!confirmed)
			return;

		try
		{
			_product = await SyncChainApiClient.Instance.UpdateProductStatusAsync(_product.Id, "Ngung ban");
			_detail = await SyncChainApiClient.Instance.GetProductDetailAsync(_product.Id);
			_product = _detail.Product;
			RenderProduct(_detail);
		}
		catch (Exception ex)
		{
			await DisplayAlert("Khong ngung ban duoc", ex.Message, "OK");
		}
	}

	private async void OnBackClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("..");
	}
}
