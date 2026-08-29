---
title: JSON serialization
description: System.Text.Json — options, source generation, polymorphism, and custom converters.
order: 30
---

`System.Text.Json` is the built-in serializer: fast, strict, UTF-8 first, and the default in ASP.NET Core.

## Options

```csharp
var options = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    NumberHandling = JsonNumberHandling.AllowReadingFromString,
    Converters = { new JsonStringEnumConverter() }
};
```

Create options **once** and reuse them. Constructing `JsonSerializerOptions` per call rebuilds the metadata cache and is dramatically slower.

Configure the framework's instance rather than serializing by hand in handlers:

```csharp
builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
```

## Source generation

```csharp
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(Order))]
[JsonSerializable(typeof(List<Order>))]
internal sealed partial class AppJsonContext : JsonSerializerContext;

var json = JsonSerializer.Serialize(order, AppJsonContext.Default.Order);
```

Faster, allocation-free at startup, and required for [trimming and Native AOT](/docs/runtime/native-aot). Wire it into ASP.NET Core with `o.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonContext.Default)`.

## Attributes

```csharp
public sealed record Order
{
    [JsonPropertyName("order_id")] public Guid Id { get; init; }
    [JsonIgnore] public string Internal { get; init; } = "";
    [JsonPropertyOrder(-1)] public string Type { get; init; } = "order";
    [JsonExtensionData] public Dictionary<string, JsonElement>? Extra { get; init; }
}
```

## Polymorphism

```csharp
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$kind")]
[JsonDerivedType(typeof(CardPayment), "card")]
[JsonDerivedType(typeof(TransferPayment), "transfer")]
public abstract record Payment;
```

Explicit discriminators only. Never enable type-name handling over untrusted input — that is a remote code execution class of bug.

## Custom converters

```csharp
public sealed class MoneyConverter : JsonConverter<Money>
{
    public override Money Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
        => Money.Parse(reader.GetString()!);

    public override void Write(Utf8JsonWriter writer, Money value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());
}
```

## Streaming

```csharp
await JsonSerializer.SerializeAsync(stream, orders, options, ct);

await foreach (var order in JsonSerializer.DeserializeAsyncEnumerable<Order>(stream, options, ct))
    ...
```

Streaming keeps a large payload out of memory in one piece — pair it with `HttpCompletionOption.ResponseHeadersRead` on the client side.

## Differences from Newtonsoft.Json

Stricter by default: no comments, no trailing commas, no implicit string-to-number, case-sensitive matching unless you opt out. Those are usually the bugs you want surfaced. `Newtonsoft.Json` remains available (`AddNewtonsoftJson`) when a legacy contract depends on its behaviour.

## Further reading

- [System.Text.Json overview](https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/overview)
