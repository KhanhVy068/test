using SyncChain.Desktop.Services;

namespace SyncChain.Desktop.Views.Pages;

public partial class RegisterPage : ContentPage
{
	public RegisterPage()
	{
		InitializeComponent();
	}

	private async void OnRegisterClicked(object? sender, EventArgs e)
	{
		var email = EmailEntry.Text?.Trim() ?? string.Empty;
		var password = PasswordEntry.Text ?? string.Empty;
		var confirmPassword = ConfirmPasswordEntry.Text ?? string.Empty;

		if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
		{
			await DisplayAlert("Dang ky", "Vui long nhap email va mat khau.", "OK");
			return;
		}

		if (password != confirmPassword)
		{
			await DisplayAlert("Dang ky", "Mat khau xac nhan khong khop.", "OK");
			return;
		}

		RegisterButton.IsEnabled = false;

		try
		{
			await SyncChainApiClient.Instance.RegisterAsync(email, password);
			await DisplayAlert("Dang ky", "Tao tai khoan thanh cong. Hay dang nhap de vao he thong.", "OK");
			await Navigation.PopAsync();
		}
		catch (Exception ex)
		{
			await DisplayAlert("Khong the dang ky", ex.Message, "OK");
		}
		finally
		{
			RegisterButton.IsEnabled = true;
		}
	}

	private async void OnBackClicked(object? sender, EventArgs e)
	{
		await Navigation.PopAsync();
	}
}
