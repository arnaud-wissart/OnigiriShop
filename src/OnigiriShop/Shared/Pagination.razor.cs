using Microsoft.AspNetCore.Components;
using OnigiriShop.Infrastructure;

namespace OnigiriShop.Shared
{
    public partial class PaginationBase : CustomComponentBase
    {
        [Parameter] public int TotalItems { get; set; }
        [Parameter] public int PageSize { get; set; } = 10;
        [Parameter] public int CurrentPage { get; set; } = 1;
        [Parameter] public EventCallback<int> OnPageChanged { get; set; }

        public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);

        protected async Task SetPage(int page)
        {
            if (page < 1) page = 1;
            if (page > TotalPages) page = TotalPages;
            if (page != CurrentPage)
                await OnPageChanged.InvokeAsync(page);
        }
    }
}
