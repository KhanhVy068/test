namespace SyncChain.Desktop;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		Routing.RegisterRoute(nameof(Views.Pages.RegisterPage), typeof(Views.Pages.RegisterPage));
		Routing.RegisterRoute(nameof(Views.Pages.CreateProductPage), typeof(Views.Pages.CreateProductPage));
		Routing.RegisterRoute(nameof(Views.Pages.ProductDetailPage), typeof(Views.Pages.ProductDetailPage));
		Routing.RegisterRoute(nameof(Views.Pages.OrderDetailPage), typeof(Views.Pages.OrderDetailPage));
		ApplyRoleNavigation();
	}

	private void ApplyRoleNavigation()
	{
		var api = Services.SyncChainApiClient.Instance;
		var role = api.CurrentUser?.Role ?? "guest";
		RoleLabel.Text = $"{api.CurrentUser?.Email ?? "guest"} - {role.ToUpperInvariant()}";

		DashboardItem.IsVisible = api.IsInternalUser;
		ProductsItem.IsVisible = true;
		OrdersItem.IsVisible = true;
		CreateOrderItem.IsVisible = role is "customer" or "staff" or "manager" or "admin";
		ImportsItem.IsVisible = api.CanManageProducts;
		LogsItem.IsVisible = api.IsInternalUser;
		ChatItem.IsVisible = api.IsInternalUser;
		AccessItem.IsVisible = api.CanManageUsers;
	}
}
