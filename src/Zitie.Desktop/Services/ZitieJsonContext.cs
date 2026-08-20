using System.Text.Json;
using System.Text.Json.Serialization;

namespace Zitie.Desktop.Services;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web, WriteIndented = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(ModuleDefinition))]
[JsonSerializable(typeof(List<TextEntry>))]
[JsonSerializable(typeof(List<PinyinCategory>))]
[JsonSerializable(typeof(SheetDocument))]
internal sealed partial class ZitieJsonContext : JsonSerializerContext;
