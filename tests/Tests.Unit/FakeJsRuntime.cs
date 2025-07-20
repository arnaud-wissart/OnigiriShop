using Microsoft.JSInterop;

namespace Tests.Unit;

public class FakeJsRuntime : IJSRuntime
{
    public string? StoredValue { get; private set; }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
    {
        return InvokeAsync<TValue>(identifier, CancellationToken.None, args);
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        if (identifier == "localStorage.getItem" && args != null && args.Length > 0 && args[0]?.ToString() == "anonCart")
        {
            object? result = StoredValue;
            return new ValueTask<TValue>((TValue?)(object?)result!);
        }
        if (identifier == "localStorage.setItem" && args != null && args.Length > 1 && args[0]?.ToString() == "anonCart")
        {
            StoredValue = args[1]?.ToString();
            return new ValueTask<TValue>(default(TValue)!);
        }
        if (identifier == "localStorage.removeItem" && args != null && args.Length > 0 && args[0]?.ToString() == "anonCart")
        {
            StoredValue = null;
            return new ValueTask<TValue>(default(TValue)!);
        }
        return new ValueTask<TValue>(default(TValue)!);
    }
}