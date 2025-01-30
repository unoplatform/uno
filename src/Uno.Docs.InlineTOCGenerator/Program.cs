using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Uno.Docs.InlineTOCGenerator
{
	class Program
	{
		static void Main(string[] args)
		{
			var deserializer = new DeserializerBuilder()
				.WithNamingConvention(CamelCaseNamingConvention.Instance)
				.IgnoreUnmatchedProperties()
				.Build();

			var tocPath = FindSiblingRelativePath("doc/articles/toc.yml") ?? throw new InvalidOperationException("TOC file not found");
			var tocReader = new StreamReader(tocPath);
			var topLevelItems = deserializer.Deserialize<Item[]>(tocReader);
			var topLevel = new Item { Items = topLevelItems };

			const string subdirectoryName = "includes";
			var articlesPath = Path.GetDirectoryName(tocPath) ?? throw new InvalidOperationException();
			var subdirectoryPath = Path.Combine(articlesPath, subdirectoryName);
			Directory.CreateDirectory(subdirectoryPath);

			InlineTOCGenerator.Generate(topLevel, "How-tos and tutorials", 2, subdirectoryPath);
			InlineTOCGenerator.Generate(topLevel, "WinRT features", 1, subdirectoryPath);
		}

		private static string? FindSiblingRelativePath(string pathFromSharedRoot)
		{
			const int maxHeight = 100;
			var sb = new StringBuilder();
			sb.Append(pathFromSharedRoot);
			for (int i = 0; i < maxHeight; i++)
			{
				var current = sb.ToString();
				if (File.Exists(current))
				{
					return current;
				}
				sb.Insert(0, "../");
			}

			return null;
		}

	}
}
