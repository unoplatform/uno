#nullable enable

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Windows.Storage;

[JsonSerializable(typeof(Dictionary<string, string?>))]
internal partial class DataTypeSerializerContext : JsonSerializerContext
{
}
