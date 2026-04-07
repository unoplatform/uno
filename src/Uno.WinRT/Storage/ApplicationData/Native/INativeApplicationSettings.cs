using System.Collections.Generic;

namespace Uno.Storage;

internal interface INativeApplicationSettings
{
	object this[string key] { get; set; }

	IEnumerable<string> Keys { get; }
}
