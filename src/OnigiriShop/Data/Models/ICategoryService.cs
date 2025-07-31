namespace OnigiriShop.Data.Models
{
    public interface ICategoryService
    {
        Task<List<Category>> GetAllAsync();
        Task<int> CreateAsync(Category c);
        Task<bool> UpdateAsync(Category c);
        Task<bool> DeleteAsync(int id);
    }
}
