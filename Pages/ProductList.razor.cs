using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using OnigiriShop.Data;
using Blazored.Toast.Services;
using OnigiriShop.Data.Models;

namespace OnigiriShop.Pages
{
    public class ProductListBase : ComponentBase
    {
        [Inject] protected ProductService ProductService { get; set; }
        [Inject] protected IWebHostEnvironment Environment { get; set; }
        [Inject] protected IToastService ToastService { get; set; }

        protected List<Product> Products { get; set; }
        protected bool IsLoading { get; set; }
        protected bool IsSaving { get; set; }

        // Edition
        protected bool EditModalOpen { get; set; }
        protected Product EditingProduct { get; set; }
        protected EditContext EditContext { get; set; }
        protected IBrowserFile UploadedImage { get; set; }
        protected string UploadedImagePreview { get; set; }
        protected string ImageUploadError { get; set; }

        // Suppression
        protected bool DeleteModalOpen { get; set; }
        protected int DeleteProductId { get; set; }
        protected string DeleteProductName { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await LoadProductsAsync();
        }

        protected async Task LoadProductsAsync()
        {
            IsLoading = true;
            Products = await ProductService.GetAllAsync();
            IsLoading = false;
        }

        protected async void OpenEditModal(int productId)
        {
            var original = await ProductService.GetByIdAsync(productId);
            if (original == null) return;

            EditingProduct = new Product
            {
                Id = original.Id,
                Name = original.Name,
                Description = original.Description,
                Price = original.Price,
                IsOnMenu = original.IsOnMenu,
                ImagePath = original.ImagePath
            };
            EditContext = new EditContext(EditingProduct);
            UploadedImage = null;
            UploadedImagePreview = null;
            ImageUploadError = null;
            EditModalOpen = true;
            StateHasChanged();
        }

        protected async Task HandleImageUpload(InputFileChangeEventArgs e)
        {
            ImageUploadError = null;
            UploadedImagePreview = null;
            UploadedImage = null;

            var file = e.File;
            if (file == null) return;
            if (!file.ContentType.StartsWith("image/"))
            {
                ImageUploadError = "Le fichier sélectionné n'est pas une image.";
                return;
            }
            if (file.Size > 2 * 1024 * 1024)
            {
                ImageUploadError = "Image trop volumineuse (max 2 Mo).";
                return;
            }
            // Preview
            var buffer = new byte[file.Size];
            await file.OpenReadStream(maxAllowedSize: 2 * 1024 * 1024).ReadAsync(buffer);
            UploadedImagePreview = $"data:{file.ContentType};base64,{Convert.ToBase64String(buffer)}";
            UploadedImage = file;
        }

        protected async Task HandleValidSubmit()
        {
            if (!EditContext.Validate()) return;
            IsSaving = true;

            // Gestion upload
            if (UploadedImage != null)
            {
                var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(UploadedImage.Name)}";
                var imageDir = Path.Combine(Environment.WebRootPath, "images", "products");
                if (!Directory.Exists(imageDir))
                    Directory.CreateDirectory(imageDir);
                var filePath = Path.Combine(imageDir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await UploadedImage.OpenReadStream(maxAllowedSize: 2 * 1024 * 1024).CopyToAsync(stream);
                }
                // Chemin relatif web
                EditingProduct.ImagePath = $"/images/products/{fileName}";
            }

            var ok = await ProductService.UpdateAsync(EditingProduct);
            if (ok)
            {
                ToastService.ShowSuccess("Produit modifié.");
                // Mise à jour directe de la liste
                var idx = Products.FindIndex(p => p.Id == EditingProduct.Id);
                if (idx != -1) Products[idx] = EditingProduct;
                CloseEditModal();
            }
            else
            {
                ToastService.ShowError("Erreur lors de la sauvegarde.");
            }
            IsSaving = false;
        }

        protected void CloseEditModal()
        {
            EditModalOpen = false;
            EditingProduct = null;
            EditContext = null;
            UploadedImage = null;
            UploadedImagePreview = null;
            ImageUploadError = null;
            StateHasChanged();
        }

        // Suppression
        protected void OpenDeleteModal(int productId)
        {
            var product = Products.FirstOrDefault(p => p.Id == productId);
            if (product == null) return;
            DeleteProductId = product.Id;
            DeleteProductName = product.Name;
            DeleteModalOpen = true;
        }

        protected async Task ConfirmDelete()
        {
            IsSaving = true;
            var ok = await ProductService.SoftDeleteAsync(DeleteProductId);
            if (ok)
            {
                ToastService.ShowSuccess("Produit supprimé.");
                Products = Products.Where(p => p.Id != DeleteProductId).ToList();
            }
            else
            {
                ToastService.ShowError("Erreur lors de la suppression.");
            }
            CloseDeleteModal();
            IsSaving = false;
        }

        protected void CloseDeleteModal()
        {
            DeleteModalOpen = false;
            DeleteProductId = 0;
            DeleteProductName = null;
            StateHasChanged();
        }
    }
}