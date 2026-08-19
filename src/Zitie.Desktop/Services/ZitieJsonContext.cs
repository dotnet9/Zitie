using System.Text.Json;
using System.Text.Json.Serialization;

namespace Zitie.Desktop.Services;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(ModuleDefinition))]
[JsonSerializable(typeof(List<TextEntry>))]
[JsonSerializable(typeof(List<PinyinCategory>))]
internal sealed partial class ZitieJsonContext : JsonSerializerContext;
