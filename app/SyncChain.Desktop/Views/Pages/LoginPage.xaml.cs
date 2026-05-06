using SyncChain.Desktop.Services;

namespace SyncChain.Desktop.Views.Pages;

public partial class LoginPage : ContentPage
{
	public LoginPage()
	{
		InitializeComponent();
	}

	private async void OnLoginClicked(object? sender, EventArgs e)
	{
		var email = EmailEntry.Text?.Trim() ?? string.Empty;
		var password = PasswordEntry.Text ?? string.Empty;

		if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
		{
			await DisplayAlert("Dang nhap", "Vui long nhap email va mat khau.", "OK");
			return;
		}

		LoginButton.IsEnabled = false;

		try
		{
			await SyncChainApiClient.Instance.LoginAsync(email, password);
			App.ShowShell();
		}
		catch (Exception ex)
		{
			await DisplayAlert("Khong the dang nhap", ex.Message, "OK");
		}
		finally
		{
			LoginButton.IsEnabled = true;
		}
	}

	private async void OnRegisterClicked(object? sender, EventArgs e)
	{
		await Navigation.PushAsync(new RegisterPage());
	}
}
