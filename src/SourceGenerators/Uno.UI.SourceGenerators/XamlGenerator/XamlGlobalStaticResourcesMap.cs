using Uno.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI.SourceGenerators.XamlGenerator
{
    internal class XamlGlobalStaticResourcesMap
	{
		private readonly Dictionary<string, List<StaticResourceDefinition>> _map = new Dictionary<string, List<StaticResourceDefinition>>();

		public XamlGlobalStaticResourcesMap()
		{
		}

		internal StaticResourceDefinition FindResource(string resourceKey)
		{
			var list = GetListForKey(resourceKey);

			return list.OrderBy(k => k.Precedence).FirstOrDefault();
		}

		internal void Add(string staticResourceKey, string ns, ResourcePrecedence precedence)
		{
			var list = GetListForKey(staticResourceKey);

			list.Add(new StaticResourceDefinition(staticResourceKey, ns, precedence));
		}

		private List<StaticResourceDefinition> GetListForKey(string staticResourceKey)
		{
			return _map.FindOrCreate(staticResourceKey, () => new List<StaticResourceDefinition>());
		}

		public enum ResourcePrecedence : int
		{
			Local = 0,
			Library,
			System,
		}

		public class StaticResourceDefinition
		{
			public StaticResourceDefinition(string staticResourceKey, string ns, ResourcePrecedence precedence)
			{
				Name = staticResourceKey;
				Namespace = ns;
				Precedence = precedence;
			}

			public string Name { get; }

			public string Namespace { get; }

			public ResourcePrecedence Precedence { get; }
		}
	}
}
