using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using SyncChain.Desktop.Models;
using SyncChain.Desktop.Services;

namespace SyncChain.Desktop.Views.Pages;

public partial class CreateOrderPage : ContentPage
{
	public ObservableCollection<ProductItem> Products { get; } = new();
	public ObservableCollection<CreateOrderLine> Lines { get; } = new();
	public IReadOnlyList<PaymentOption> Payments => DemoData.Payments;

	public CreateOrderPage()
	{
		InitializeComponent();
		BindingContext = this;
		UpdateTotals();
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await LoadProductsAsync();
	}

	private async Task LoadProductsAsync()
	{
		try
		{
			var products = await SyncChainApiClient.Instance.GetProductsAsync();
			Products.Clear();

			foreach (var product in products.Where(x => x.StockQuantity > 0))
			{
				Products.Add(product);
			}
		}
		catch (Exception ex)
		{
			await DisplayAlert("Khong tai duoc san pham", ex.Message, "OK");
		}
	}

	private async void OnAddProductClicked(object? sender, EventArgs e)
	{
		if (ProductPicker.SelectedItem is not ProductItem product)
		{
			await DisplayAlert("Tao don hang", "Vui long chon san pham.", "OK");
			return;
		}

		if (!int.TryParse(QuantityEntry.Text, out var quantity) || quantity <= 0)
		{
			await DisplayAlert("Tao don hang", "So luong phai lon hon 0.", "OK");
			return;
		}

		var existingLine = Lines.FirstOrDefault(x => x.ProductId == product.Id);
		var currentQuantity = existingLine?.Quantity ?? 0;

		if (currentQuantity + quantity > product.StockQuantity)
		{
			await DisplayAlert("Tao don hang", $"San pham chi con {product.StockQuantity} trong kho.", "OK");
			return;
		}

		if (existingLine == null)
		{
			Lines.Add(new CreateOrderLine(product, quantity));
		}
		else
		{
			existingLine.Quantity += quantity;
		}

		QuantityEntry.Text = "1";
		UpdateTotals();
	}

	private void OnDecreaseQuantityClicked(object? sender, EventArgs e)
	{
		if ((sender as Button)?.CommandParameter is not CreateOrderLine line)
			return;

		if (line.Quantity <= 1)
		{
			Lines.Remove(line);
		}
		else
		{
			line.Quantity--;
		}

		UpdateTotals();
	}

	private async void OnIncreaseQuantityClicked(object? sender, EventArgs e)
	{
		if ((sender as Button)?.CommandParameter is not CreateOrderLine line)
			return;

		if (line.Quantity >= line.StockQuantity)
		{
			await DisplayAlert("Tao don hang", $"San pham chi con {line.StockQuantity} trong kho.", "OK");
			return;
		}

		line.Quantity++;
		UpdateTotals();
	}

	private void OnRemoveLineClicked(object? sender, EventArgs e)
	{
		if ((sender as Button)?.CommandParameter is CreateOrderLine line)
		{
			Lines.Remove(line);
			UpdateTotals();
		}
	}

	private void OnResetClicked(object? sender, EventArgs e)
	{
		ProductPicker.SelectedItem = null;
		QuantityEntry.Text = "1";
		Lines.Clear();
		UpdateTotals();
	}

	private async void OnCreateOrderClicked(object? sender, EventArgs e)
	{
		if (Lines.Count == 0)
		{
			await DisplayAlert("Tao don hang", "Vui long them it nhat mot san pham.", "OK");
			return;
		}

		CreateOrderButton.IsEnabled = false;

		try
		{
			var result = await SyncChainApiClient.Instance.CreateOrderAsync(Lines.Select(x => new SyncChainApiClient.CreateOrderLineRequest
			{
				MaSanPham = x.ProductId,
				SoLuong = x.Quantity
			}));

			await DisplayAlert("Tao don hang", $"Tao don #{result.MaDonHang} thanh cong. Tong tien: {FormatMoney(result.TongTien)}", "OK");
			Lines.Clear();
			UpdateTotals();
			await Shell.Current.GoToAsync("//orders");
		}
		catch (Exception ex)
		{
			await DisplayAlert("Khong tao duoc don hang", ex.Message, "OK");
		}
		finally
		{
			CreateOrderButton.IsEnabled = true;
		}
	}

	private async void OnBackClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("//orders");
	}

	private void UpdateTotals()
	{
		var subtotal = Lines.Sum(x => x.LineTotal);
		SubtotalLabel.Text = FormatMoney(subtotal);
		TotalLabel.Text = FormatMoney(subtotal);
		CreateOrderButton.IsEnabled = Lines.Count > 0;
	}

	private static string FormatMoney(decimal value)
	{
		return value.ToString("N0", CultureInfo.InvariantCulture) + " VND";
	}
}

public sealed class CreateOrderLine : INotifyPropertyChanged
{
	private int _quantity;

	public CreateOrderLine(ProductItem product, int quantity)
	{
		ProductId = product.Id;
		Name = product.Name;
		Initials = product.Initials;
		UnitPrice = product.UnitPrice;
		StockQuantity = product.StockQuantity;
		_quantity = quantity;
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	public int ProductId { get; }
	public string Name { get; }
	public string Initials { get; }
	public decimal UnitPrice { get; }
	public int StockQuantity { get; }
	public string Variant => $"Ton kho: {StockQuantity}";
	public string Price => UnitPrice.ToString("N0", CultureInfo.InvariantCulture) + " VND";
	public decimal LineTotal => UnitPrice * Quantity;
	public string LineTotalText => LineTotal.ToString("N0", CultureInfo.InvariantCulture) + " VND";

	public int Quantity
	{
		get => _quantity;
		set
		{
			if (_quantity == value)
				return;

			_quantity = value;
			OnPropertyChanged();
			OnPropertyChanged(nameof(LineTotal));
			OnPropertyChanged(nameof(LineTotalText));
		}
	}

	private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
