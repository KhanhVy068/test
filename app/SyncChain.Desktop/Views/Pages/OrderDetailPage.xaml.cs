using System.Collections.ObjectModel;
using SyncChain.Desktop.Models;
using SyncChain.Desktop.Services;

namespace SyncChain.Desktop.Views.Pages;

[QueryProperty(nameof(OrderId), "orderId")]
public partial class OrderDetailPage : ContentPage
{
	private int _orderId;
	private string _status = "pending";

	public ObservableCollection<OrderDetailLineItem> Lines { get; } = new();

	public string OrderId
	{
		set
		{
			if (int.TryParse(value, out var orderId))
			{
				_orderId = orderId;
			}
		}
	}

	public OrderDetailPage()
	{
		InitializeComponent();
		BindingContext = this;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await LoadOrderAsync();
	}

	private async Task LoadOrderAsync()
	{
		if (_orderId <= 0)
		{
			await DisplayAlert("Don hang", "Khong tim thay ma don hang.", "OK");
			await Shell.Current.GoToAsync("..");
			return;
		}

		try
		{
			var orders = await SyncChainApiClient.Instance.GetOrdersAsync();
			var order = orders.FirstOrDefault(x => x.Id == _orderId);

			if (order != null)
			{
				_status = order.Status;
				OrderTitleLabel.Text = $"DON HANG {order.Code}";
				OrderSubtitleLabel.Text = $"Tao luc {order.CreatedAt}";
				OrderCodeLabel.Text = order.Code;
				TotalLabel.Text = order.Total;
				StatusLabel.Text = order.Status;
				StatusBadge.BackgroundColor = order.StatusColor;
				StatusPicker.SelectedItem = order.Status;
			}

			var details = await SyncChainApiClient.Instance.GetOrderDetailsAsync(_orderId);
			Lines.Clear();

			foreach (var detail in details)
			{
				Lines.Add(detail);
			}

			LineCountLabel.Text = $"{Lines.Count} san pham";
			EmptyLinesLabel.IsVisible = Lines.Count == 0;

			var canManage = SyncChainApiClient.Instance.CanManageOrders;
			StatusPicker.IsVisible = canManage;
			UpdateStatusButton.IsVisible = canManage;
		}
		catch (Exception ex)
		{
			await DisplayAlert("Khong tai duoc chi tiet don", ex.Message, "OK");
		}
	}

	private async void OnUpdateStatusClicked(object? sender, EventArgs e)
	{
		if (StatusPicker.SelectedItem is not string status)
		{
			await DisplayAlert("Trang thai", "Vui long chon trang thai.", "OK");
			return;
		}

		if (status == _status)
		{
			await DisplayAlert("Trang thai", "Don hang dang o trang thai nay.", "OK");
			return;
		}

		UpdateStatusButton.IsEnabled = false;

		try
		{
			await SyncChainApiClient.Instance.UpdateOrderStatusAsync(_orderId, status);
			_status = status;
			await DisplayAlert("Trang thai", "Cap nhat trang thai thanh cong.", "OK");
			await LoadOrderAsync();
		}
		catch (Exception ex)
		{
			await DisplayAlert("Khong cap nhat duoc trang thai", ex.Message, "OK");
		}
		finally
		{
			UpdateStatusButton.IsEnabled = true;
		}
	}

	private async void OnBackClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("..");
	}
}
