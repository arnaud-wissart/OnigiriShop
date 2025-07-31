namespace OnigiriShop.Data.Models
{
    public interface IProductService
    {
        Task<List<Product>> GetAllAsync(bool includeDeleted = false);
        Task<List<Product>> GetMenuProductsAsync();
        Task<Product?> GetByIdAsync(int id);
        Task<int> CreateAsync(Product p);
        Task<bool> UpdateAsync(Product p);
        Task<bool> SoftDeleteAsync(int id);
    }
}
