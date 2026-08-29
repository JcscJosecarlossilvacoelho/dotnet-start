---
title: Interop with native code
description: Calling C libraries from .NET with source-generated P/Invoke, and the marshalling rules that matter.
order: 60
---

.NET calls native code through **P/Invoke**. Since .NET 7 the recommended form is `[LibraryImport]`, which generates the marshalling code at compile time — it is faster than the old `[DllImport]` and works under [Native AOT](/docs/runtime/native-aot).

## A minimal binding

```csharp
internal static partial class Native
{
    [LibraryImport("libz", EntryPoint = "compressBound")]
    internal static partial nuint CompressBound(nuint sourceLength);

    [LibraryImport("libc", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int puts(string message);
}
```

The class and method must be `partial`; the generator supplies the body.

## Type mapping

| C | C# |
| --- | --- |
| `int` / `unsigned int` | `int` / `uint` |
| `long` (LP64) | `long` |
| `size_t` | `nuint` |
| `void*`, opaque handle | `nint`, or a `SafeHandle` subclass |
| `char*` (UTF-8) | `string` with `StringMarshalling.Utf8` |
| `struct` by value | A `struct` with matching `[StructLayout]` |
| callback | A `[UnmanagedFunctionPointer]` delegate or `delegate* unmanaged<...>` |

Blittable types (no reference fields, no layout translation) cross the boundary with zero copying. Keep interop structs blittable when you can.

## Owning native resources

Wrap every handle in a `SafeHandle`. It ties the lifetime to the GC, prevents the handle being collected mid-call, and gives you a correct finalizer for free:

```csharp
internal sealed class ArchiveHandle() : SafeHandleZeroOrMinusOneIsInvalid(ownsHandle: true)
{
    protected override bool ReleaseHandle() => Native.CloseArchive(handle) == 0;
}
```

## Rules that prevent crashes

- A managed delegate passed to native code must be kept alive by managed code for as long as native code may call it. Store it in a field.
- Pin buffers with `fixed` only for the duration of the call; long pins fragment the heap.
- Native code must not throw into managed frames — check return codes and translate them into exceptions on the managed side.
- Set `SetLastError = true` only when you will read `Marshal.GetLastWin32Error()`; it costs a call.

## Loading the right library

Names differ per platform (`libz.so.1`, `libz.dylib`, `zlib1.dll`). Register a resolver once:

```csharp
NativeLibrary.SetDllImportResolver(typeof(Native).Assembly, (name, assembly, path) =>
    name == "libz" && OperatingSystem.IsMacOS()
        ? NativeLibrary.Load("libz.dylib", assembly, path)
        : IntPtr.Zero);
```

## Further reading

- [Source-generated P/Invoke](https://learn.microsoft.com/dotnet/standard/native-interop/pinvoke-source-generation)
- [Native interoperability](https://learn.microsoft.com/dotnet/standard/native-interop/)
