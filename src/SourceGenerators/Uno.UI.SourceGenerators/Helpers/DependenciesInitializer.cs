using System;
using System.Linq;
using System.IO;
using System.Reflection;
using Uno.Extensions;
using Uno.Extensions.Specialized;
using Uno.Roslyn;
using System.Diagnostics;
using Microsoft.CodeAnalysis;

#if NETFRAMEWORK
using Uno.SourceGeneration;
#endif

namespace Uno.UI.SourceGenerators
{
	internal class DependenciesInitializer
	{
		internal static void Init()
		{
#if NETSTANDARD2_0
			// Kept until dependencies can be added automatically and this is not needed anymore
			//
			var baseAnalyzer = typeof(DependenciesInitializer).Assembly.Location;

			if (baseAnalyzer != null)
			{
				var basePath = Path.GetDirectoryName(baseAnalyzer);

				var files = Directory
					.EnumerateFiles(basePath, "*.dll")
					.Where(f => Path.GetFileName(f).StartsWith("Uno."));

				foreach (var file in files)
				{
					Assembly.LoadFrom(file);
				}
			}
#endif
		}
	}
}
