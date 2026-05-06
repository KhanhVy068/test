using System.Globalization;
using SyncChain.Desktop.Services;

namespace SyncChain.Desktop.Views.Pages;

public partial class CreateProductPage : ContentPage
{
	public CreateProductPage()
	{
		InitializeComponent();
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();

		if (!SyncChainApiClient.Instance.CanManageProducts)
		{
			await DisplayAlert("Khong co quyen", "Chi admin hoac manager duoc them san pham.", "OK");
			await Shell.Current.GoToAsync("..");
		}
	}

	private async void OnSaveClicked(object? sender, EventArgs e)
	{
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

			await SyncChainApiClient.Instance.CreateProductAsync(name, price, stockQuantity, imageUrl, description);
			await DisplayAlert("San pham", "Them san pham thanh cong.", "OK");
			await Shell.Current.GoToAsync("..");
		}
		catch (Exception ex)
		{
			await DisplayAlert("Khong luu duoc san pham", ex.Message, "OK");
		}
		finally
		{
			SaveButton.IsEnabled = true;
		}
	}

	private async void OnPickImageClicked(object? sender, EventArgs e)
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
			PreviewImage.Source = CreateImageSource(ImageEntry.Text);
			PreviewLabel.IsVisible = string.IsNullOrWhiteSpace(ImageEntry.Text);
		}
		catch (Exception ex)
		{
			await DisplayAlert("Khong chon duoc anh", ex.Message, "OK");
		}
	}

	private static ImageSource? CreateImageSource(string imageUrl)
	{
		if (string.IsNullOrWhiteSpace(imageUrl))
			return null;

		if (File.Exists(imageUrl))
			return ImageSource.FromStream(() => File.OpenRead(imageUrl));

		return Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https"
			? ImageSource.FromUri(uri)
			: null;
	}

	private async void OnBackClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("..");
	}
}
