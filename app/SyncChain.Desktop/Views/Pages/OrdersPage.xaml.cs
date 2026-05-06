using System.Collections.ObjectModel;
using SyncChain.Desktop.Models;
using SyncChain.Desktop.Services;

namespace SyncChain.Desktop.Views.Pages;

public partial class OrdersPage : ContentPage
{
	public ObservableCollection<OrderItem> Orders { get; } = new();

	public OrdersPage()
	{
		InitializeComponent();
		BindingContext = this;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await LoadOrdersAsync();
	}

	private async Task LoadOrdersAsync()
	{
		try
		{
			var orders = await SyncChainApiClient.Instance.GetOrdersAsync();
			Orders.Clear();

			foreach (var order in orders)
			{
				Orders.Add(order);
			}

			UpdateSummary();
			ScopeLabel.Text = SyncChainApiClient.Instance.IsInternalUser
				? "Dang xem tat ca don hang"
				: "Dang xem don hang cua ban";
		}
		catch (Exception ex)
		{
			Orders.Clear();
			UpdateSummary();
			await DisplayAlert("Khong tai duoc don hang", ex.Message, "OK");
		}
	}

	private void UpdateSummary()
	{
		TotalOrdersLabel.Text = Orders.Count.ToString();
		PendingOrdersLabel.Text = Orders.Count(x => x.Status == "pending").ToString();
		DoneOrdersLabel.Text = Orders.Count(x => x.Status == "done").ToString();
		CancelOrdersLabel.Text = Orders.Count(x => x.Status == "cancel").ToString();
		EmptyLabel.IsVisible = Orders.Count == 0;
	}

	private async void OnRefreshClicked(object? sender, EventArgs e)
	{
		await LoadOrdersAsync();
	}

	private async void OnCreateOrderClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("//create-order");
	}

	private async void OnOpenDetailClicked(object? sender, EventArgs e)
	{
		if ((sender as Button)?.CommandParameter is not OrderItem order)
			return;

		await Shell.Current.GoToAsync($"{nameof(OrderDetailPage)}?orderId={order.Id}");
	}
}
