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
		private static object _gate = new object();

		internal static void Init()
		{
#if NETSTANDARD2_0
			lock (_gate)
			{
				// Kept until dependencies can be added automatically and this is not needed anymore
				//
				var baseAnalyzer = typeof(DependenciesInitializer).Assembly.Location;

				if (baseAnalyzer != null)
				{
					var basePath = Path.GetDirectoryName(baseAnalyzer);

					var files = from file in Directory.EnumerateFiles(basePath, "*.dll")
								let fileName = Path.GetFileName(file)
								where fileName.StartsWith("Uno.")

								// Starting from net 6.0.200, avoids "System.IO.FileLoadException: Assembly with same name is already loaded"
								where !fileName.Equals(Path.GetFileName(baseAnalyzer), StringComparison.OrdinalIgnoreCase)
								select file;

					foreach (var file in files)
					{
						try
						{
							Assembly.LoadFrom(file);
						}
						catch (Exception e)
						{
							throw new Exception($"Failed to load {file}: {e}");
						}
					}
				}
			}
#endif
		}
	}
}
