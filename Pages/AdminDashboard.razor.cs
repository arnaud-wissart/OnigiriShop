namespace OnigiriShop.Pages
{
    public class AdminDashboardBase : AdminPageBase
    {
        protected void GotoUsers()
        {
            Nav.NavigateTo("/admin/users");
        }

        protected void GotoCatalog()
        {
            Nav.NavigateTo("/admin/catalog");
        }
    }
}
