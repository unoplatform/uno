using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Mono.Cecil;
using Mono.Options;

string? assemblyPath = null;
List<string> resourceNames = new();
List<string> resourceNamesToExclude = new();

var options = new OptionSet();
options.Add("a=", "path to assembly", a => assemblyPath = a);
options.Add("r=", "include resource name", resourceNames.Add);
options.Add("x=", "exclude resource name", resourceNamesToExclude.Add);

try
{
	options.Parse(args);
}
catch (OptionException e)
{
	Console.WriteLine(e.Message);

	return -1;
}

if (!File.Exists(assemblyPath))
{
	Console.WriteLine("Assembly doesn't exist.");

	return -1;
}

if (resourceNames.Concat(resourceNamesToExclude).Any(string.IsNullOrEmpty))
{
	Console.WriteLine("Invalid resource name.");

	return -1;
}

using var assembly = AssemblyDefinition.ReadAssembly(assemblyPath, new ReaderParameters { InMemory = true });

var embeddedResources = assembly.MainModule.Resources.OfType<EmbeddedResource>().Select(r => r.Name).ToHashSet();

foreach (var resource in resourceNames)
{
	if (!embeddedResources.Contains(resource))
	{
		Console.WriteLine($"Assembly {Path.GetFileName(assemblyPath)} doesn't contain {resource}.");

		return -1;
	}
}

foreach (var resource in resourceNamesToExclude)
{
	if (embeddedResources.Contains(resource))
	{
		Console.WriteLine($"Assembly {Path.GetFileName(assemblyPath)} contains {resource}.");

		return -1;
	}
}

Console.WriteLine("Success.");

return 0;
