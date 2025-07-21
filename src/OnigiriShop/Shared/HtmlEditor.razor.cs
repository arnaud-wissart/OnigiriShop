using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace OnigiriShop.Shared
{
    public class HtmlEditorBase : ComponentBase, IAsyncDisposable
    {
        [Inject] public IJSRuntime JS { get; set; } = default!;

        private DotNetObjectReference<HtmlEditorBase>? objRef;

        [Parameter] public string Value { get; set; } = string.Empty;
        [Parameter] public EventCallback<string> ValueChanged { get; set; }

        protected string EditorId { get; } = "quill-" + Guid.NewGuid().ToString("N");

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                objRef = DotNetObjectReference.Create(this);
                await JS.InvokeVoidAsync("initHtmlEditor", EditorId, objRef, Value ?? string.Empty);
            }
        }

        [JSInvokable]
        public async Task OnHtmlChanged(string html)
        {
            Value = html;
            await ValueChanged.InvokeAsync(html);
            StateHasChanged();
        }

        public async ValueTask DisposeAsync()
        {
            await JS.InvokeVoidAsync("disposeHtmlEditor", EditorId);
            objRef?.Dispose();
        }
    }
}
