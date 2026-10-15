using System.Text.Json.Serialization;

namespace Zitie.Desktop.Services;

/// <summary>为第三方 Jieba 的 Viterbi 模型预编译字典转换器，供其默认 JSON 加载流程使用。</summary>
[JsonSerializable(typeof(IDictionary<char, IDictionary<char, double>>))]
internal sealed partial class JiebaModelJsonContext : JsonSerializerContext;
