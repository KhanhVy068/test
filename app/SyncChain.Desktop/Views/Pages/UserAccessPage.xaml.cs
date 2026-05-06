using System.Collections.ObjectModel;
using SyncChain.Desktop.Models;
using SyncChain.Desktop.Services;

namespace SyncChain.Desktop.Views.Pages;

public partial class UserAccessPage : ContentPage
{
	private string _selectedRole = "manager";

	public ObservableCollection<InternalUserItem> Users { get; } = new();

	public UserAccessPage()
	{
		InitializeComponent();
		BindingContext = this;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		ApplyPermissions();
		await LoadUsersAsync();
	}

	private async Task LoadUsersAsync()
	{
		if (!SyncChainApiClient.Instance.CanManageUsers)
			return;

		try
		{
			var users = await SyncChainApiClient.Instance.GetInternalUsersAsync();
			Users.Clear();
			foreach (var user in users)
			{
				Users.Add(user);
			}

			UpdateSummary();
		}
		catch (Exception ex)
		{
			Users.Clear();
			UpdateSummary();
			await DisplayAlert("Nguoi dung", ex.Message, "OK");
		}
	}

	private void ApplyPermissions()
	{
		var currentUser = SyncChainApiClient.Instance.CurrentUser;
		var canManage = SyncChainApiClient.Instance.CanManageUsers;

		RoleHintLabel.Text = currentUser == null
			? "Chua dang nhap."
			: $"Dang dang nhap: {currentUser.Email} - role {currentUser.Role}. Admin co toan quyen quan ly tai khoan noi bo.";
		CreatePanel.IsVisible = canManage;
		PermissionLabel.Text = canManage ? "Admin: tao/sua/khoa/reset" : "Chi admin duoc quan ly";
	}

	private void UpdateSummary()
	{
		TotalUsersLabel.Text = Users.Count.ToString();
		ManagerUsersLabel.Text = Users.Count(x => x.Role == "manager").ToString();
		StaffUsersLabel.Text = Users.Count(x => x.Role == "staff").ToString();
		LockedUsersLabel.Text = Users.Count(x => !x.IsActive).ToString();
		EmptyLabel.IsVisible = Users.Count == 0;
	}

	private async void OnRefreshClicked(object? sender, EventArgs e)
	{
		await LoadUsersAsync();
	}

	private void OnManagerRoleClicked(object? sender, EventArgs e)
	{
		_selectedRole = "manager";
		ManagerRoleButton.Style = (Style)Application.Current!.Resources["PrimaryButtonStyle"];
		StaffRoleButton.Style = (Style)Application.Current!.Resources["SecondaryButtonStyle"];
	}

	private void OnStaffRoleClicked(object? sender, EventArgs e)
	{
		_selectedRole = "staff";
		ManagerRoleButton.Style = (Style)Application.Current!.Resources["SecondaryButtonStyle"];
		StaffRoleButton.Style = (Style)Application.Current!.Resources["PrimaryButtonStyle"];
	}

	private async void OnCreateUserClicked(object? sender, EventArgs e)
	{
		var email = EmailEntry.Text?.Trim() ?? string.Empty;
		var password = PasswordEntry.Text?.Trim() ?? string.Empty;
		var username = UsernameEntry.Text?.Trim() ?? string.Empty;

		if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
		{
			await DisplayAlert("Tai khoan", "Vui long nhap email va mat khau.", "OK");
			return;
		}

		CreateUserButton.IsEnabled = false;

		try
		{
			await SyncChainApiClient.Instance.CreateInternalUserAsync(email, password, username, _selectedRole);
			EmailEntry.Text = string.Empty;
			UsernameEntry.Text = string.Empty;
			PasswordEntry.Text = "123456";
			await LoadUsersAsync();
			await DisplayAlert("Tai khoan", "Tao tai khoan thanh cong.", "OK");
		}
		catch (Exception ex)
		{
			await DisplayAlert("Khong tao duoc tai khoan", ex.Message, "OK");
		}
		finally
		{
			CreateUserButton.IsEnabled = true;
		}
	}

	private async void OnChangeRoleClicked(object? sender, EventArgs e)
	{
		if ((sender as Button)?.CommandParameter is not InternalUserItem user)
			return;

		var newRole = user.Role == "manager" ? "staff" : "manager";
		var confirmed = await DisplayAlert("Doi role", $"Chuyen {user.Email} sang {newRole}?", "Dong y", "Huy");
		if (!confirmed)
			return;

		try
		{
			await SyncChainApiClient.Instance.UpdateInternalUserAsync(user.Id, newRole, user.IsActive);
			await LoadUsersAsync();
		}
		catch (Exception ex)
		{
			await DisplayAlert("Khong doi duoc role", ex.Message, "OK");
		}
	}

	private async void OnToggleActiveClicked(object? sender, EventArgs e)
	{
		if ((sender as Button)?.CommandParameter is not InternalUserItem user)
			return;

		var newState = !user.IsActive;
		var action = newState ? "mo khoa" : "khoa";
		var confirmed = await DisplayAlert("Trang thai tai khoan", $"{action} {user.Email}?", "Dong y", "Huy");
		if (!confirmed)
			return;

		try
		{
			await SyncChainApiClient.Instance.SetInternalUserActiveAsync(user.Id, newState);
			await LoadUsersAsync();
		}
		catch (Exception ex)
		{
			await DisplayAlert("Khong cap nhat duoc tai khoan", ex.Message, "OK");
		}
	}

	private async void OnResetPasswordClicked(object? sender, EventArgs e)
	{
		if ((sender as Button)?.CommandParameter is not InternalUserItem user)
			return;

		var password = await DisplayPromptAsync("Reset mat khau", $"Mat khau moi cho {user.Email}:", "Cap nhat", "Huy", "Nhap mat khau moi", 64, Keyboard.Text, "123456");
		if (string.IsNullOrWhiteSpace(password))
			return;

		try
		{
			await SyncChainApiClient.Instance.ResetInternalUserPasswordAsync(user.Id, password.Trim());
			await DisplayAlert("Mat khau", "Da reset mat khau.", "OK");
		}
		catch (Exception ex)
		{
			await DisplayAlert("Khong reset duoc mat khau", ex.Message, "OK");
		}
	}

	private void OnLogoutClicked(object? sender, EventArgs e)
	{
		App.ShowLogin();
	}
}
