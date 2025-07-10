using Microsoft.AspNetCore.Components;
using OnigiriShop.Data;
using OnigiriShop.Data.Models;

namespace OnigiriShop.Pages
{
    public class AdminProductsBase : ComponentBase
    {
        [Inject] public ProductService ProductService { get; set; }
        protected List<Product> Products { get; set; } = new();
        protected List<Product> FilteredProducts => Products; // Ajoute plus tard la recherche
        protected Product ModalModel { get; set; } = new();
        protected Product DeleteModel { get; set; }
        protected bool ShowModal { get; set; }
        protected bool ShowDeleteConfirm { get; set; }
        protected bool IsEdit { get; set; }
        protected string ModalTitle { get; set; }
        protected string ModalError { get; set; }
        protected bool IsBusy { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await LoadProducts();
        }

        protected async Task LoadProducts()
        {
            Products = await ProductService.GetAllAsync();
            StateHasChanged();
        }

        protected void ShowAddModal()
        {
            IsEdit = false;
            ModalTitle = "Nouveau produit";
            ModalModel = new Product();
            ModalError = null;
            ShowModal = true;
        }

        protected void EditProduct(Product p)
        {
            IsEdit = true;
            ModalTitle = "Modifier le produit";
            ModalModel = new Product
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                IsOnMenu = p.IsOnMenu,
                ImagePath = p.ImagePath,
                IsDeleted = p.IsDeleted
            };
            ModalError = null;
            ShowModal = true;
        }

        protected void HideModal()
        {
            ShowModal = false;
            ModalModel = new Product();
        }

        protected async Task HandleModalValid()
        {
            IsBusy = true;
            try
            {
                if (IsEdit)
                {
                    await ProductService.UpdateAsync(ModalModel);
                }
                else
                {
                    await ProductService.CreateAsync(ModalModel);
                }
                ShowModal = false;
                await LoadProducts();
            }
            catch (Exception ex)
            {
                ModalError = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        protected void ConfirmDeleteProduct(Product p)
        {
            DeleteModel = p;
            ShowDeleteConfirm = true;
        }

        protected void CancelDelete()
        {
            ShowDeleteConfirm = false;
            DeleteModel = null;
        }

        protected async Task DeleteProductConfirmed()
        {
            if (DeleteModel != null)
            {
                await ProductService.SoftDeleteAsync(DeleteModel.Id);
                ShowDeleteConfirm = false;
                await LoadProducts();
            }
        }
    }
}
