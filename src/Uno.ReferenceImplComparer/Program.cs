#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Transactions;
using Mono.Cecil;
using Mono.Collections.Generic;

namespace Uno.ReferenceImplComparer
{
	class Program
	{
		static int Main(string[] args)
		{
			var hasErrors = false;
			var filePath = UnpackArchive(args[0]);

			Console.WriteLine($"Validating package {args[0]}");

			foreach(var assembly in Directory.GetFiles(Path.Combine(filePath, "lib", "netstandard2.0"), "*.dll"))
			{
				var referenceAssemblyDefinition = ReadAssemblyDefinition(assembly);

				foreach(var runtimeAssembly in Directory.GetFiles(Path.Combine(filePath, "uno-runtime"), Path.GetFileName(assembly), SearchOption.AllDirectories))
				{
					var identifier = $"{Path.GetFileName(runtimeAssembly)}/{Path.GetFileName(Path.GetDirectoryName(runtimeAssembly))}";
					Console.WriteLine($"Validating {identifier}");

					var runtimeAssemblyDefinition = ReadAssemblyDefinition(runtimeAssembly);

					hasErrors |= CompareAssemblies(referenceAssemblyDefinition, runtimeAssemblyDefinition, identifier);
				}
			}

			return hasErrors ? 1 : 0;
		}

		private static bool CompareAssemblies(AssemblyDefinition referenceAssemby, AssemblyDefinition runtimeAssembly, string identifier)
		{
			var hasError = false;
			var referenceTypes = referenceAssemby.MainModule.GetTypes();
			var runtimeTypes = runtimeAssembly.MainModule.GetTypes().ToDictionary(t => t.FullName);

			foreach(var referenceType in referenceTypes.Where(t => t.IsPublic))
			{
				if(referenceType.FullName == "Windows.UI.Xaml.Documents.TextElement")
				{
					Console.WriteLine("Skipping Windows.UI.Xaml.Documents.TextElement comparison");
					continue;
				}

				if(runtimeTypes.TryGetValue(referenceType.FullName, out var runtimeType))
				{
					if(referenceType.BaseType?.FullName != runtimeType.BaseType?.FullName)
					{
						Console.WriteLine($"{referenceType.FullName} base type is different {referenceType.BaseType?.FullName} in reference, {runtimeType.BaseType?.FullName} in {identifier}");
						hasError = true;
					}

					hasError |= CompareMembers(referenceType.Methods.Where(m => m.IsPublic), runtimeType.Methods, identifier);
					hasError |= CompareMembers(referenceType.Properties.Where(m => m.GetMethod?.IsPublic ?? false), runtimeType.Properties, identifier);
					hasError |= CompareMembers(referenceType.Fields.Where(m => m.IsPublic), runtimeType.Fields, identifier);
					hasError |= CompareMembers(referenceType.Events.Where(m => m.AddMethod?.IsPublic ?? false), runtimeType.Events, identifier);
				}
				else
				{
					hasError = true;
					Console.WriteLine($"The type {referenceType} is missing from ");
				}
			}

			return hasError;
		}

		private static bool CompareMembers(IEnumerable<MemberReference> referenceMembers, IEnumerable<MemberReference> runtimeMembers, string identifier)
		{
			var hasError = false;
			var runtimeMembersLookup = runtimeMembers.ToDictionary(m => m.ToString());

			foreach(var referenceMember in referenceMembers)
			{
				if (!runtimeMembersLookup.ContainsKey(referenceMember.ToString()))
				{
					Console.WriteLine($"The member {referenceMember} cannot be found in {identifier}");
					hasError = true;
				}
			}

			return hasError;
		}

		private static AssemblyDefinition ReadAssemblyDefinition(string assemblyPath)
			=> AssemblyDefinition.ReadAssembly(assemblyPath, new ReaderParameters() { AssemblyResolver = new DefaultAssemblyResolver() });

		private static string UnpackArchive(string packagePath)
		{
			var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString().Trim('{', '}'));

			Directory.CreateDirectory(path);

			Console.WriteLine($"Extracting {packagePath} -> {path}");
			using (var file = File.OpenRead(packagePath))
			{
				using (var archive = new ZipArchive(file, ZipArchiveMode.Read))
				{
					archive.ExtractToDirectory(path);
				}
			}

			if (Directory.GetFiles(path, "*.nuspec", SearchOption.AllDirectories).FirstOrDefault() is string nuspecFile)
			{
				return path;
			}
			else
			{
				throw new InvalidOperationException($"Unable to find nuspec file in {packagePath}");
			}
		}

	}
}
