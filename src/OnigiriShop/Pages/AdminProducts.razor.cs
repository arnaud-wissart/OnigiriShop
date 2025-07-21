using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using OnigiriShop.Data.Models;
using OnigiriShop.Infrastructure;
using OnigiriShop.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace OnigiriShop.Pages
{
    public class AdminProductsBase : CustomComponentBase
    {
        [Inject] public IWebHostEnvironment WebHostEnv { get; set; } = default!;
        [Inject] public ProductService ProductService { get; set; } = default!;
        protected List<Product> Products { get; set; } = [];
        protected List<Product> FilteredProducts => Products;
        protected Product ModalModel { get; set; } = new();
        protected Product? DeleteModel { get; set; }
        protected bool ShowModal { get; set; }
        protected bool ShowDeleteConfirm { get; set; }
        protected bool IsEdit { get; set; }
        protected string? ModalTitle { get; set; }
        protected string? ModalError { get; set; }
        protected bool IsBusy { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

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

        protected async Task EditProduct(Product p)
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
            await JS.InvokeVoidAsync("closeAllTooltips");
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

        protected async Task ConfirmDeleteProduct(Product p)
        {
            DeleteModel = p;
            await JS.InvokeVoidAsync("closeAllTooltips");
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

        protected IBrowserFile? UploadedImage { get; set; }
        protected string GetProductImage(Product p)
        {
            if (!string.IsNullOrWhiteSpace(p.ImagePath))
                return p.ImagePath;
            return "data:image/svg+xml;utf8," + Uri.EscapeDataString(
                @"<svg width='48' height='48' xmlns='http://www.w3.org/2000/svg'>
            <rect width='100%' height='100%' fill='#e5e5e5'/>
            <text x='50%' y='50%' text-anchor='middle' dy='.3em' font-size='10' fill='#888'>Aucune image</text>
        </svg>");
        }

        protected async Task OnImageSelected(InputFileChangeEventArgs e)
        {
            if (e.FileCount == 0)
                return;

            UploadedImage = e.File;
            var ext = Path.GetExtension(UploadedImage.Name).ToLowerInvariant();
            if (ext != ".jpg" && ext != ".jpeg" && ext != ".png" && ext != ".webp")
            {
                ModalError = "Format d’image non supporté.";
                StateHasChanged();
                return;
            }

            var fileName = $"product_{Guid.NewGuid()}.jpg";
            var relPath = $"images/products/{fileName}";
            var absPath = Path.Combine(WebHostEnv.WebRootPath, "images", "products", fileName);

            var directoryName = Path.GetDirectoryName(absPath);

            if (!string.IsNullOrEmpty(directoryName))
                Directory.CreateDirectory(directoryName); 

            using (var image = await Image.LoadAsync(UploadedImage.OpenReadStream(5_000_000))) // 5 Mo max
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(400, 400),
                    Mode = ResizeMode.Max
                }));
                await image.SaveAsJpegAsync(absPath, new JpegEncoder { Quality = 70 });
            }

            ModalModel.ImagePath = "/" + relPath.Replace("\\", "/");
            StateHasChanged();
        }
    }
}