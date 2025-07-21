using Microsoft.JSInterop;

namespace Tests.Unit;

public class FakeJsRuntime : IJSRuntime
{
    public string? StoredValue { get; private set; }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        if (identifier == "localStorage.getItem" && args != null && args.Length > 0 && args[0]?.ToString() == "anonCart")
        {
            object? result = StoredValue;
            if (result is null)
                return ValueTask.FromResult<TValue>(default!);
            return ValueTask.FromResult((TValue)result);
        }
        if (identifier == "localStorage.setItem" && args != null && args.Length > 1 && args[0]?.ToString() == "anonCart")
        {
            StoredValue = args[1]?.ToString();
            return ValueTask.FromResult<TValue>(default!);
        }
        if (identifier == "localStorage.removeItem" && args != null && args.Length > 0 && args[0]?.ToString() == "anonCart")
        {
            StoredValue = null;
            return ValueTask.FromResult<TValue>(default!);
        }
        return ValueTask.FromResult<TValue>(default!);
    }
}