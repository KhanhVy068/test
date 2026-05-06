using System.Collections.ObjectModel;
using System.Globalization;
using SyncChain.Desktop.Models;
using SyncChain.Desktop.Services;

namespace SyncChain.Desktop.Views.Pages;

public partial class ProductsPage : ContentPage
{
	private readonly List<ProductItem> _allProducts = new();

	public ObservableCollection<ProductItem> Products { get; } = new();

	public bool CanManageProducts => SyncChainApiClient.Instance.CanManageProducts;

	public ProductsPage()
	{
		InitializeComponent();
		BindingContext = this;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		ApplyPermissions();
		await LoadProductsAsync();
	}

	private async Task LoadProductsAsync()
	{
		try
		{
			var products = await SyncChainApiClient.Instance.GetProductsAsync();
			_allProducts.Clear();
			_allProducts.AddRange(products);
			ApplyFilter();
			UpdateSummary();
		}
		catch (Exception ex)
		{
			Products.Clear();
			_allProducts.Clear();
			UpdateSummary();
			await DisplayAlert("Khong tai duoc san pham", ex.Message, "OK");
		}
	}

	private void ApplyPermissions()
	{
		var canManage = SyncChainApiClient.Instance.CanManageProducts;
		OnPropertyChanged(nameof(CanManageProducts));
		ShowCreateFormButton.IsVisible = canManage;
		PermissionLabel.Text = canManage
			? "Ban co quyen them/sua/xoa san pham"
			: "Chi admin hoac manager duoc them/sua/xoa";
	}

	private void ApplyFilter()
	{
		var keyword = SearchEntry.Text?.Trim() ?? string.Empty;
		var filtered = string.IsNullOrWhiteSpace(keyword)
			? _allProducts
			: _allProducts
				.Where(x => x.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase)
					|| x.Code.Contains(keyword, StringComparison.OrdinalIgnoreCase))
				.ToList();

		Products.Clear();
		foreach (var product in filtered)
		{
			Products.Add(product);
		}

		EmptyLabel.IsVisible = Products.Count == 0;
	}

	private void UpdateSummary()
	{
		TotalProductsLabel.Text = _allProducts.Count.ToString(CultureInfo.InvariantCulture);
		ActiveProductsLabel.Text = _allProducts.Count(x => x.StockQuantity > 0).ToString(CultureInfo.InvariantCulture);
		LowStockProductsLabel.Text = _allProducts.Count(x => x.StockQuantity > 0 && x.StockQuantity <= 10).ToString(CultureInfo.InvariantCulture);
		OutOfStockProductsLabel.Text = _allProducts.Count(x => x.StockQuantity <= 0).ToString(CultureInfo.InvariantCulture);
		EmptyLabel.IsVisible = Products.Count == 0;
	}

	private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
	{
		ApplyFilter();
	}

	private async void OnRefreshClicked(object? sender, EventArgs e)
	{
		await LoadProductsAsync();
	}

	private async void OnShowCreateFormClicked(object? sender, EventArgs e)
	{
		if (!EnsureCanManageProducts())
			return;

		await Shell.Current.GoToAsync(nameof(CreateProductPage));
	}

	private async void OnEditProductClicked(object? sender, EventArgs e)
	{
		if (!EnsureCanManageProducts())
			return;

		if ((sender as Button)?.CommandParameter is not ProductItem product)
			return;

		await Shell.Current.GoToAsync($"{nameof(ProductDetailPage)}?productId={product.Id}");
	}

	private async void OnDeleteProductClicked(object? sender, EventArgs e)
	{
		if (!EnsureCanManageProducts())
			return;

		if ((sender as Button)?.CommandParameter is not ProductItem product)
			return;

		var confirmed = await DisplayAlert("Xoa san pham", $"Xoa {product.Name}?", "Xoa", "Huy");
		if (!confirmed)
			return;

		try
		{
			await SyncChainApiClient.Instance.DeleteProductAsync(product.Id);
			await LoadProductsAsync();
			await DisplayAlert("Xoa san pham", "Da xoa san pham.", "OK");
		}
		catch (Exception ex)
		{
			await DisplayAlert("Khong xoa duoc san pham", ex.Message, "OK");
		}
	}

	private async void OnOpenDetailClicked(object? sender, EventArgs e)
	{
		if ((sender as Button)?.CommandParameter is not ProductItem product)
			return;

		await Shell.Current.GoToAsync($"{nameof(ProductDetailPage)}?productId={product.Id}");
	}

	private bool EnsureCanManageProducts()
	{
		if (SyncChainApiClient.Instance.CanManageProducts)
			return true;

		DisplayAlert("Khong co quyen", "Chi admin hoac manager duoc quan ly san pham.", "OK");
		return false;
	}

}
