using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using OnigiriShop.Data.Models;
using OnigiriShop.Infrastructure;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace OnigiriShop.Pages
{
    public class AdminProductsBase : CustomComponentBase
    {
        [Inject] public IProductService ProductService { get; set; } = default!;
        [Inject] public ICategoryService CategoryService { get; set; } = default!;
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
        protected List<Category> Categories { get; set; } = [];

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

            await LoadProducts();
            Categories = await CategoryService.GetAllAsync();
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
            ModalModel = new Product
            {
                CategoryId = Categories.FirstOrDefault()?.Id ?? 1
            };
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
                CategoryId = p.CategoryId,
                IsOnMenu = p.IsOnMenu,
                ImageBase64 = p.ImageBase64,
                IsDeleted = p.IsDeleted
            };
            ModalError = null;
            await JS.InvokeVoidAsync("closeAllTooltips");
            ShowModal = true;
        }

        protected void HideModal()
        {
            ShowModal = false;
            ModalModel = new Product { CategoryId = Categories.FirstOrDefault()?.Id ?? 1 };
        }

        protected async Task HandleModalValid()
        {
            IsBusy = true;
            await HandleAsync(async () =>
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
            }, "Erreur lors de l'enregistrement du produit");
            IsBusy = false;
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
            if (!string.IsNullOrWhiteSpace(p.ImageBase64))
                return p.ImageBase64.StartsWith("data:image") ? p.ImageBase64 : $"data:image/jpeg;base64,{p.ImageBase64}";
            
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

            using var image = await Image.LoadAsync(UploadedImage.OpenReadStream(5_000_000)); // 5 Mo max

            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(800, 800),
                Mode = ResizeMode.Max
            }));
            await using var ms = new MemoryStream();
            await image.SaveAsJpegAsync(ms, new JpegEncoder { Quality = 80 });
            ModalModel.ImageBase64 = "data:image/jpeg;base64," + Convert.ToBase64String(ms.ToArray());
            StateHasChanged();
        }

        // ----- Gestion des catégories -----
        protected Category CategoryModalModel { get; set; } = new();
        protected Category? DeleteCategoryModel { get; set; }
        protected bool ShowCategoryModal { get; set; }
        protected bool ShowCategoryDeleteConfirm { get; set; }
        protected bool CategoryIsEdit { get; set; }
        protected string? CategoryModalTitle { get; set; }
        protected bool CategoryIsBusy { get; set; }

        protected void ShowAddCategory()
        {
            CategoryIsEdit = false;
            CategoryModalTitle = "Nouvelle catégorie";
            CategoryModalModel = new Category();
            ShowCategoryModal = true;
        }

        protected void EditCategory(Category c)
        {
            CategoryIsEdit = true;
            CategoryModalTitle = "Modifier la catégorie";
            CategoryModalModel = new Category { Id = c.Id, Name = c.Name };
            ShowCategoryModal = true;
        }

        protected void HideCategoryModal()
        {
            ShowCategoryModal = false;
            CategoryModalModel = new Category();
        }

        protected async Task SaveCategoryAsync()
        {
            CategoryIsBusy = true;
            if (CategoryIsEdit)
                await CategoryService.UpdateAsync(CategoryModalModel);
            else
                await CategoryService.CreateAsync(CategoryModalModel);
            CategoryIsBusy = false;
            ShowCategoryModal = false;
            Categories = await CategoryService.GetAllAsync();
        }

        protected void ConfirmDeleteCategory(Category c)
        {
            DeleteCategoryModel = c;
            ShowCategoryDeleteConfirm = true;
        }

        protected async Task DeleteCategoryConfirmed()
        {
            if (DeleteCategoryModel != null)
            {
                await CategoryService.DeleteAsync(DeleteCategoryModel.Id);
                ShowCategoryDeleteConfirm = false;
                Categories = await CategoryService.GetAllAsync();
            }
        }
    }
}
