using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.Docs.InlineTOCGenerator
{
	class Item
	{
		public string? Name { get; set; }

		public string? TopicHref { get; set; }

		public Item[]? Items { get; set; }

		public string? Href { get; set; }

		public override string ToString()
		{
			var count = Items is { } items ? $" ({items.Length})" : "";
			return $"Item-{Name}{count}";
		}
	}
}
