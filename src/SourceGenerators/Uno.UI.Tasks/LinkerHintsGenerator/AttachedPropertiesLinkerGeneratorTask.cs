#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

using Mono.Cecil;

namespace Uno.UI.Tasks.LinkerHintsGenerator
{
	/// <summary>
	/// Task used to generate linker descriptor files for attached property Get/Set methods,
	/// to preserve them for Native AOT scenarios.
	/// </summary>
	/// <remarks>
	/// For each static field or property of type <c>Microsoft.UI.Xaml.DependencyProperty</c>
	/// whose name ends with the <c>Property</c> suffix, the task looks for corresponding
	/// <c>Get&lt;Name&gt;</c> and <c>Set&lt;Name&gt;</c> static methods on the same type
	/// and emits an ILLink descriptor entry to preserve them.
	/// </remarks>
	public class AttachedPropertiesLinkerGeneratorTask_v0 : Microsoft.Build.Utilities.Task
	{
		internal const MessageImportance DefaultLogMessageLevel
#if DEBUG
			= MessageImportance.High;
#else
			= MessageImportance.Low;
#endif

		private const string DependencyPropertyFullName = "Microsoft.UI.Xaml.DependencyProperty";
		private const string PropertySuffix = "Property";

		[Required]
		public string AssemblyPath { get; set; } = "";

		[Required]
		public string OutputDescriptorPath { get; set; } = "";

		[Required]
		public Microsoft.Build.Framework.ITaskItem[]? ReferencePath { get; set; }

		public override bool Execute()
		{
			try
			{
				Log.LogMessage(DefaultLogMessageLevel, $"Processing assembly: {AssemblyPath}");

				using var resolver = BuildAssemblyResolver();

				var assemblyList = BuildAssemblyList(resolver);
				var typeMethods = FindAttachedPropertyMethods(assemblyList);

				if (typeMethods.Count > 0)
				{
					GenerateLinkerDescriptor(typeMethods);
					Log.LogMessage(DefaultLogMessageLevel, $"Generated attached properties descriptor with {typeMethods.Count} types.");
				}
				else
				{
					Log.LogMessage(DefaultLogMessageLevel, $"No attached property methods to preserve found.");
				}

				return true;
			}
			catch (Exception ex)
			{
				Log.LogError($"AttachedPropertiesLinkerGenerator failed: {ex.Message}");
				Log.LogMessage(DefaultLogMessageLevel, ex.ToString());
				return false;
			}
		}

		private ReferencePathAssemblyResolver BuildAssemblyResolver()
		{
			var resolver = new ReferencePathAssemblyResolver(BuildReferencePaths(), Log);

			var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			var assemblyPathDirectory = GetSearchDirectory(AssemblyPath);
			if (assemblyPathDirectory != null)
			{
				resolver.AddSearchDirectory(assemblyPathDirectory);
				seen.Add(assemblyPathDirectory);
			}

			foreach (var reference in ReferencePath ?? [])
			{
				var path = GetSearchDirectory(reference.ItemSpec);
				if (path == null || seen.Contains(path))
				{
					continue;
				}
				resolver.AddSearchDirectory(path);
				seen.Add(path);
			}

			return resolver;

			static string? GetSearchDirectory(string path)
			{
				var directory = Path.GetDirectoryName(path);
				if (string.IsNullOrEmpty(directory))
				{
					return null;
				}
				return directory;
			}
		}

		private Dictionary<string, string> BuildReferencePaths()
		{
			var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

			AddPath(dict, AssemblyPath);

			foreach (var item in ReferencePath ?? [])
			{
				AddPath(dict, item.ItemSpec);
			}

			return dict;

			static void AddPath(Dictionary<string, string> dict, string path)
			{
				var fileName = Path.GetFileName(path);
				if (!dict.ContainsKey(fileName))
				{
					dict[fileName] = path;
				}
			}
		}

		private HashSet<AssemblyDefinition> BuildAssemblyList(IAssemblyResolver resolver)
		{
			var readerParameters = new ReaderParameters
			{
				AssemblyResolver = resolver,
			};
			var rootAssembly = resolver.Resolve(AssemblyNameReference.Parse(Path.GetFileNameWithoutExtension(AssemblyPath)), readerParameters);
			Log.LogMessage(DefaultLogMessageLevel, $"Resolved Root Assembly reference `{rootAssembly.FullName}`.");

			var assemblies = new HashSet<AssemblyDefinition>()
			{
				rootAssembly,
			};

			var assembliesToProcess = new Queue<AssemblyDefinition>();
			assembliesToProcess.Enqueue(rootAssembly);

			while (assembliesToProcess.Count > 0)
			{
				var assembly = assembliesToProcess.Dequeue();
				foreach (var reference in assembly.MainModule.AssemblyReferences)
				{
					var referencedAssembly = TryResolveAssembly(resolver, reference);
					if (referencedAssembly != null && !assemblies.Contains(referencedAssembly))
					{
						Log.LogMessage(DefaultLogMessageLevel, $"Resolved Assembly reference `{referencedAssembly.FullName}` via `{assembly.MainModule.FileName}`.");
						assembliesToProcess.Enqueue(referencedAssembly);
						assemblies.Add(referencedAssembly);
					}
				}
			}

			Log.LogMessage(DefaultLogMessageLevel, $"Loaded {assemblies.Count} assemblies.");
			return assemblies;
		}

		private AssemblyDefinition? TryResolveAssembly(IAssemblyResolver resolver, AssemblyNameReference assemblyName)
		{
			var readerParameters = new ReaderParameters
			{
				AssemblyResolver = resolver,
			};

			try
			{
				return resolver.Resolve(assemblyName, readerParameters);
			}
			catch (Exception ex)
			{
				Log.LogWarning($"Failed to resolve assembly `{assemblyName.FullName}`: {ex.Message}");
				Log.LogMessage(MessageImportance.Low, ex.ToString());
				return null;
			}
		}

		private Dictionary<TypeDefinition, List<string>> FindAttachedPropertyMethods(IEnumerable<AssemblyDefinition> assemblies)
		{
			var result = new Dictionary<TypeDefinition, List<string>>();

			foreach (var assembly in assemblies)
			{
				foreach (var type in assembly.MainModule.Types.SelectMany(GetAllTypes))
				{
					var methods = FindGetSetMethodsForType(type);
					if (methods.Count > 0)
					{
						result[type] = methods;
					}
				}
			}

			Log.LogMessage(DefaultLogMessageLevel, $"Found {result.Values.Sum(v => v.Count)} attached property methods to preserve in {result.Count} types.");
			return result;
		}

		private List<string> FindGetSetMethodsForType(TypeDefinition type)
		{
			var methods = new List<string>();

			// Collect all DependencyProperty member names (from static fields and static properties)
			var dpNames = new HashSet<string>(StringComparer.Ordinal);

			foreach (var field in type.Fields)
			{
				if (field.IsStatic &&
					field.FieldType.FullName == DependencyPropertyFullName &&
					field.Name.EndsWith(PropertySuffix, StringComparison.Ordinal))
				{
					var dpName = field.Name.Substring(0, field.Name.Length - PropertySuffix.Length);
					if (dpName.Length > 0)
					{
						dpNames.Add(dpName);
					}
				}
			}

			foreach (var property in type.Properties)
			{
				var getter = property.GetMethod;
				if (getter != null &&
					getter.IsStatic &&
					property.PropertyType.FullName == DependencyPropertyFullName &&
					property.Name.EndsWith(PropertySuffix, StringComparison.Ordinal))
				{
					var dpName = property.Name.Substring(0, property.Name.Length - PropertySuffix.Length);
					if (dpName.Length > 0)
					{
						dpNames.Add(dpName);
					}
				}
			}

			if (dpNames.Count == 0)
			{
				return methods;
			}

			// For each DependencyProperty name, look for Get<Name> and Set<Name> static methods
			var methodLookup = new HashSet<string>(
				type.Methods.Where(m => m.IsStatic).Select(m => m.Name),
				StringComparer.Ordinal);

			foreach (var dpName in dpNames)
			{
				var getName = "Get" + dpName;
				var setName = "Set" + dpName;

				if (methodLookup.Contains(getName))
				{
					Log.LogMessage(DefaultLogMessageLevel, $"Found attached property accessor: {type.FullName}.{getName}");
					methods.Add(getName);
				}

				if (methodLookup.Contains(setName))
				{
					Log.LogMessage(DefaultLogMessageLevel, $"Found attached property accessor: {type.FullName}.{setName}");
					methods.Add(setName);
				}
			}

			return methods;
		}

		private static IEnumerable<TypeDefinition> GetAllTypes(TypeDefinition type)
		{
			yield return type;
			foreach (var nested in type.NestedTypes)
			{
				foreach (var t in GetAllTypes(nested))
				{
					yield return t;
				}
			}
		}

		private void GenerateLinkerDescriptor(Dictionary<TypeDefinition, List<string>> typeMethods)
		{
			var typesByAssembly = typeMethods
				.GroupBy(kvp => kvp.Key.Module.Assembly.Name.Name)
				.OrderBy(g => g.Key);

			var linkerElement = new XElement("linker",
				typesByAssembly.Select(assemblyGroup =>
					CreateAssemblyElement(assemblyGroup.Key, assemblyGroup)));

			var outputDir = Path.GetDirectoryName(OutputDescriptorPath);
			if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
			{
				Directory.CreateDirectory(outputDir);
			}

			var doc = new XDocument(
				new XDeclaration("1.0", "UTF-8", null),
				linkerElement);

			doc.Save(OutputDescriptorPath);
			Log.LogMessage(MessageImportance.High, $"Saved attached properties descriptor to {OutputDescriptorPath}");

			static XElement? CreateAssemblyElement(string assemblyName, IEnumerable<KeyValuePair<TypeDefinition, List<string>>> types)
			{
				var typeElements = types
					.OrderBy(t => t.Key.FullName)
					.Select(typeEntry => CreateTypeElement(typeEntry.Key, typeEntry.Value))
					.Where(e => e != null)
					.ToList();

				if (typeElements.Count == 0)
				{
					return null;
				}

				return new XElement("assembly",
					new XAttribute("fullname", assemblyName),
					typeElements);
			}

			static XElement? CreateTypeElement(TypeDefinition typeDefinition, List<string> methodNames)
			{
				if (methodNames.Count == 0)
				{
					return null;
				}

				return new XElement("type",
					new XAttribute("fullname", typeDefinition.FullName),
					new XAttribute("required", "false"),
					methodNames.OrderBy(n => n).Select(name =>
						new XElement("method",
							new XAttribute("name", name))));
			}
		}
	}
}
