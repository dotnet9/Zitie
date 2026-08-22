using System.Text.Json;
using System.Text.Json.Serialization;

namespace Zitie.Desktop.Services;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web,
    WriteIndented = true,
    UseStringEnumConverter = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(SheetDocument))]
[JsonSerializable(typeof(ModuleDefaults))]
internal sealed partial class ZitieJsonContext : JsonSerializerContext;
