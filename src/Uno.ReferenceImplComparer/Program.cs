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

			var referenceTargetFrameworks = new[] {
				"net9.0",
				"net10.0"
			};

			foreach (var targetFramework in referenceTargetFrameworks)
			{
				foreach (var assembly in Directory.GetFiles(Path.Combine(filePath, "lib", targetFramework), "*.dll"))
				{
					var referenceAssemblyDefinition = ReadAssemblyDefinition(assembly);

					foreach (var runtimeAssembly in Directory.GetFiles(Path.Combine(filePath, "uno-runtime", targetFramework), Path.GetFileName(assembly), SearchOption.AllDirectories))
					{
						var identifier = $"{Path.GetFileName(runtimeAssembly)}/{Path.GetFileName(Path.GetDirectoryName(runtimeAssembly))}";
						Console.WriteLine($"Validating {identifier} ({runtimeAssembly})");

						var runtimeAssemblyDefinition = ReadAssemblyDefinition(runtimeAssembly);

						hasErrors |= CompareAssemblies(referenceAssemblyDefinition, runtimeAssemblyDefinition, identifier);
					}
				}
			}

			return hasErrors ? 1 : 0;
		}

		private static bool IsAccessible(MethodDefinition method)
		{
			// https://github.com/jbevain/cecil/blob/56d4409b8a0165830565c6e3f96f41bead2c418b/Mono.Cecil/MethodAttributes.cs#L22-L24
			return method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly;
		}

		private static bool IsAccessible(FieldDefinition field)
		{
			// https://github.com/jbevain/cecil/blob/56d4409b8a0165830565c6e3f96f41bead2c418b/Mono.Cecil/FieldAttributes.cs#L22-L24
			return field.IsPublic || field.IsFamily || field.IsFamilyOrAssembly;
		}

		private static bool IsAccessible(TypeDefinition type)
		{
			// https://github.com/jbevain/cecil/blob/56d4409b8a0165830565c6e3f96f41bead2c418b/Mono.Cecil/TypeAttributes.cs#L21-L26
			return type.IsPublic ||
				((type.IsNestedPublic || type.IsNestedFamily || type.IsNestedFamilyOrAssembly) && IsAccessible(type.DeclaringType));
		}

		private static bool IsAccessible(PropertyDefinition property)
		{
			return (property.GetMethod is not null && IsAccessible(property.GetMethod)) ||
				(property.SetMethod is not null && IsAccessible(property.SetMethod));
		}

		private static bool IsAccessible(EventDefinition @event)
		{
			return (@event.AddMethod is not null && IsAccessible(@event.AddMethod)) ||
				(@event.RemoveMethod is not null && IsAccessible(@event.RemoveMethod));
		}

		private static bool CompareAssemblies(AssemblyDefinition referenceAssembly, AssemblyDefinition runtimeAssembly, string identifier)
		{
			var hasError = false;
			var referenceTypes = referenceAssembly.MainModule.GetTypes();
			var runtimeTypes = runtimeAssembly.MainModule.GetTypes().ToDictionary(t => t.FullName);

			foreach (var referenceType in referenceTypes.Where(IsAccessible))
			{
				if (referenceType.FullName == "Microsoft.UI.Xaml.Documents.TextElement")
				{
					Console.WriteLine("Skipping Microsoft.UI.Xaml.Documents.TextElement comparison");
					continue;
				}

				if (runtimeTypes.TryGetValue(referenceType.FullName, out var runtimeType))
				{
					if (referenceType.BaseType?.FullName != runtimeType.BaseType?.FullName)
					{
						Console.Error.WriteLine($"Error: {referenceType.FullName} base type is different {referenceType.BaseType?.FullName} in reference, {runtimeType.BaseType?.FullName} in {identifier}");
						hasError = true;
					}

					hasError |= CompareMembers(referenceType.Methods.Where(IsAccessible), runtimeType.Methods.Where(IsAccessible), identifier);
					hasError |= CompareMembers(referenceType.Properties.Where(IsAccessible), runtimeType.Properties.Where(IsAccessible), identifier);
					hasError |= CompareMembers(referenceType.Fields.Where(IsAccessible), runtimeType.Fields.Where(IsAccessible), identifier);
					hasError |= CompareMembers(referenceType.Events.Where(IsAccessible), runtimeType.Events.Where(IsAccessible), identifier);
				}
				else
				{
					Console.Error.WriteLine($"Error: The type {referenceType} is missing from ");
					hasError = true;
				}
			}

			return hasError;
		}

		private static bool CompareMembers(IEnumerable<MemberReference> referenceMembers, IEnumerable<MemberReference> runtimeMembers, string identifier)
		{
			var hasError = false;
			var runtimeMembersLookup = runtimeMembers.ToDictionary(m => m.ToString());
			var referenceMembersLookup = referenceMembers.ToDictionary(m => m.ToString());

			foreach (var referenceMember in referenceMembers)
			{
				if (!runtimeMembersLookup.ContainsKey(referenceMember.ToString()))
				{
					Console.Error.WriteLine($"Error: The member {referenceMember} cannot be found in {identifier}");
					hasError = true;
				}
			}

			foreach (var runtimeMember in runtimeMembers)
			{
				// It's not very necessary for runtime members to exist in reference members.
				// But there are cases where it's good to know that info.
				// 1. If the method is overrides. This is very problematic because in app code that is compiled against reference binaries, a base.XYZ call could refer to the wrong method.
				// 2. Sometimes there is intent to introduce Skia-specific or Wasm-specific API. Doing so
				//    under `#if __SKIA__` or `#if __WASM__` isn't enough because you can't call it when compiling against the reference binaries.

				if (runtimeMember is MethodReference { Name: "Finalize" } finalizer && finalizer.Resolve().IsVirtual)
				{
					// Assumption is that it's okay for finalizers to be in runtime API but not reference API.
					continue;
				}
				else if (runtimeMember is MethodReference @operator && @operator.Name is "op_Implicit" or "op_Explicit")
				{
					// Operators must be declared publicly.
					continue;
				}

				if (!referenceMembersLookup.ContainsKey(runtimeMember.ToString()))
				{
					Console.Error.WriteLine($"Error: The member {runtimeMember} cannot be found in reference API");
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
