namespace DotnetSexy.Docs;

/// <summary>Shared open/close state for the documentation search palette (⌘K).</summary>
public sealed class DocsSearchState
{
    public bool IsOpen { get; private set; }

    public event Action? Changed;

    public void Open()
    {
        if (IsOpen) return;
        IsOpen = true;
        Changed?.Invoke();
    }

    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;
        Changed?.Invoke();
    }

    public void Toggle()
    {
        IsOpen = !IsOpen;
        Changed?.Invoke();
    }
}
