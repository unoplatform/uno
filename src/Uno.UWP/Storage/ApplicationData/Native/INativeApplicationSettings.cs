using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.Storage;

internal interface INativeApplicationSettings
{
	object this[string key] { get; set; }

	IEnumerable<string> Keys { get; }
}
