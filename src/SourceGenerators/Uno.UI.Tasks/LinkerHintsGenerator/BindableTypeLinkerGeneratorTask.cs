#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

using Mono.Cecil;

namespace Uno.UI.Tasks.LinkerHintsGenerator
{
	/// <summary>
	/// Task used to generate linker descriptor files for types referenced by [Bindable] types,
	/// to preserve their public properties for Native AOT scenarios.
	/// </summary>
	public class BindableTypeLinkerGeneratorTask_v0 : Microsoft.Build.Utilities.Task
	{
		internal const MessageImportance DefaultLogMessageLevel
#if DEBUG
			= MessageImportance.High;
#else
			= MessageImportance.Low;
#endif

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
				var bindableTypes = FindBindableTypes(assemblyList);
				var typeCache = new TypeDefinitionCache();
				var typesToProperties = FindReferencedPropertyTypes(bindableTypes, typeCache);

				var typesWithProperties = typesToProperties.GetTypeProperties().Count(kvp => kvp.Value.Count > 0);
				if (typesWithProperties > 0)
				{
					GenerateLinkerDescriptor(typesToProperties);
					Log.LogMessage(DefaultLogMessageLevel, $"Generated descriptor with {typesWithProperties} types.");
				}
				else
				{
					Log.LogMessage(DefaultLogMessageLevel, $"No types to preserve found.");
				}

				return true;
			}
			catch (Exception ex)
			{
				Log.LogError($"BindableTypeLinkerGenerator failed: {ex.Message}");
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

		private List<TypeDefinition> FindBindableTypes(IEnumerable<AssemblyDefinition> assemblies)
		{
			var bindableTypes = new List<TypeDefinition>();

			foreach (var assembly in assemblies)
			{
				foreach (var type in assembly.MainModule.Types.SelectMany(t => GetAllTypes(t)))
				{
					if (HasBindableAttribute(type))
					{
						bindableTypes.Add(type);
						Log.LogMessage(DefaultLogMessageLevel, $"Found bindable type: {type.FullName}");
					}
				}
			}

			Log.LogMessage(DefaultLogMessageLevel, $"Found {bindableTypes.Count} bindable types.");
			return bindableTypes;
		}

		private IEnumerable<TypeDefinition> GetAllTypes(TypeDefinition type)
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

		private bool HasBindableAttribute(TypeDefinition type)
		{
			return type.CustomAttributes.Any(attr =>
				attr.AttributeType.FullName == "BindableAttribute" ||
				attr.AttributeType.FullName.EndsWith(".BindableAttribute", StringComparison.Ordinal));
		}

		private PreservePropertyInfoMap FindReferencedPropertyTypes(IEnumerable<TypeDefinition> bindableTypes, TypeDefinitionCache typeCache)
		{
			var typesToProperties = new PreservePropertyInfoMap();

			foreach (var bindableType in bindableTypes)
			{
				if (!bindableType.HasProperties)
				{
					continue;
				}

				// Iterate over all public properties
				foreach (var property in bindableType.Properties.Where(ShouldProcessPropertyType))
				{
					AddDeclaredPropertyTypes(typeCache, property.PropertyType, typesToProperties)
						?.AddContext($"From Property `{property.Name} : {property.PropertyType.FullName}` on bindable type `{bindableType.FullName}`");
				}
			}

			Log.LogMessage(DefaultLogMessageLevel, $"Found {typesToProperties.GetTypeProperties().Count(kvp => kvp.Value.Count > 0)} implicitly referenced types.");
			return typesToProperties;
		}

		static readonly HashSet<PreservePropertyInfo> EmptyPreservedPropertyInfo = [];

		private PreserveTypeDefinition? AddDeclaredPropertyTypes(TypeDefinitionCache typeCache, TypeReference typeReference, PreservePropertyInfoMap typesToProperties)
		{
			var typeDefinition = TryResolveTypeDefinition(typeCache, typeReference);
			if (typeDefinition == null)
			{
				return null;
			}

			bool haveKey = typesToProperties.TryGetPreserveType(typeDefinition.FullName, out var key);

			if (!haveKey)
			{
				key = new PreserveTypeDefinition(typeDefinition.FullName, typeDefinition);
				typesToProperties[key] = EmptyPreservedPropertyInfo;
			}

			// Process generic arguments (e.g., List<Entity> should also process Entity)
			if (typeReference is GenericInstanceType genericType)
			{
				foreach (var genericArg in genericType.GenericArguments)
				{
					AddDeclaredPropertyTypes(typeCache, genericArg, typesToProperties)
						?.AddContext($"Generic type parameter from {typeReference.FullName}");
				}
			}

			if (haveKey)
			{
				// Already processed; return the *existing* key, so that callers can call `.AddContext()`
				return key;
			}

			if (HasBindableAttribute(typeDefinition))
			{
				return null;
			}

			if (typeDefinition.BaseType != null)
			{
				AddDeclaredPropertyTypes(typeCache, typeDefinition.BaseType, typesToProperties)
					?.AddContext($"Base type of {typeDefinition.FullName}");
			}

			if (!typeDefinition.HasProperties)
			{
				return key;
			}

			var declaredProperties = new HashSet<PreservePropertyInfo>();

			foreach (var property in typeDefinition.Properties.Where(ShouldProcessPropertyType))
			{
				declaredProperties.Add(new PreservePropertyInfo(property.Name, property.PropertyType));
			}
			typesToProperties[key!] = declaredProperties;

			foreach (var property in declaredProperties)
			{
				AddDeclaredPropertyTypes(typeCache, property.PropertyType, typesToProperties)
					?.AddContext($"From Property `{property.PropertyName} : {property.PropertyType.FullName}` on type `{typeDefinition.FullName}`");
			}
			return key;
		}

		static bool ShouldProcessPropertyType(PropertyDefinition property)
		{
			var get = property.GetMethod;
			if (get == null)
			{
				return false;
			}
			return !get.IsStatic && get.IsPublic;
		}

		private TypeDefinition? TryResolveTypeDefinition(TypeDefinitionCache typeCache, TypeReference typeReference)
		{
			try
			{
				return typeCache.Resolve(typeReference);
			}
			catch (Exception ex)
			{
				Log.LogWarning($"Failed to resolve type {typeReference.FullName}: {ex.Message}");
				Log.LogMessage(MessageImportance.Low, ex.ToString());
				return null;
			}
		}

		private void GenerateLinkerDescriptor(PreservePropertyInfoMap referencedTypes)
		{
			// Group types by assembly
			var typesByAssembly = referencedTypes
				.GetTypeProperties()
				.GroupBy(kvp => kvp.Key.TypeDefinition.Module.Assembly.Name.Name)
				.OrderBy(g => g.Key);

			var linkerElement = new XElement("linker",
				typesByAssembly.Select(assemblyGroup =>
					CreateAssemblyElement(assemblyGroup.Key, assemblyGroup)));

			// Ensure output directory exists
			var outputDir = Path.GetDirectoryName(OutputDescriptorPath);
			if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
			{
				Directory.CreateDirectory(outputDir);
			}

			var doc = new XDocument(
				new XDeclaration("1.0", "UTF-8", null),
				linkerElement);

			doc.Save(OutputDescriptorPath);
			Log.LogMessage(MessageImportance.High, $"Saved descriptor to {OutputDescriptorPath}");

			static XElement? CreateAssemblyElement(string assemblyName, IEnumerable<KeyValuePair<PreserveTypeDefinition, HashSet<PreservePropertyInfo>>> types)
			{
				if (!types.Any() || types.All(t => t.Value.Count == 0))
				{
					return null; // Skip assemblies with no types
				}

				return new XElement("assembly",
					new XAttribute("fullname", assemblyName),
					types.OrderBy(t => t.Key.FullName).Select(typeEntry =>
						CreateTypeElement(typeEntry.Key, typeEntry.Value)));
			}

			static IEnumerable<XNode> CreateTypeElement(PreserveTypeDefinition preserveTypeDefinition, HashSet<PreservePropertyInfo> properties)
			{
				if (properties.Count == 0)
				{
					yield break; // Skip types with no public properties
				}

				yield return new XElement("type",
					new XAttribute("fullname", preserveTypeDefinition.FullName),
					new XAttribute("required", "false"),
					OrderedDistinct(preserveTypeDefinition.Context).Select(c => new XComment(c)),
					properties.OrderBy(p => p.PropertyName)
						.Select(p =>
							new XElement("property",
								new XAttribute("name", p.PropertyName),
								new XComment($" {p.PropertyType.FullName} {p.PropertyName} {{get;}} "))));
			}
		}

		// *Implementation-wise*, `Enumerable.Distinct()` preserves order.
		// However, that isn't documented behavior, so "appease" Copilot by implementing our version
		// in which the order is preserved.
		private static IEnumerable<T> OrderedDistinct<T>(IEnumerable<T> sequence)
		{
			var seen = new HashSet<T>();
			foreach (var item in sequence)
			{
				if (seen.Add(item))
				{
					yield return item;
				}
			}
		}
	}

	internal sealed class PreserveTypeDefinition : IEquatable<PreserveTypeDefinition>
	{
		public string FullName { get; }
		public TypeDefinition TypeDefinition { get; }
		public List<string> Context { get; } = new List<string>();

		public PreserveTypeDefinition(string fullName, TypeDefinition typeDefinition)
		{
			FullName = fullName;
			TypeDefinition = typeDefinition;
		}

		public override bool Equals(object? obj)
		{
			return Equals(obj as PreserveTypeDefinition);
		}

		public bool Equals(PreserveTypeDefinition? other)
		{
			return other != null &&
				   FullName == other.FullName;
		}

		public override int GetHashCode()
		{
			return FullName.GetHashCode();
		}

		public void AddContext(string context)
		{
			Context.Add(context);
		}
	}

	internal sealed class PreservePropertyInfo : IEquatable<PreservePropertyInfo>
	{
		public string PropertyName { get; }
		public TypeReference PropertyType { get; }

		public PreservePropertyInfo(string propertyName, TypeReference propertyType)
		{
			PropertyName = propertyName;
			PropertyType = propertyType;
		}

		public override bool Equals(object? obj)
		{
			return Equals(obj as PreservePropertyInfo);
		}

		public bool Equals(PreservePropertyInfo? other)
		{
			return other != null &&
				   PropertyName == other.PropertyName &&
				   PropertyType.FullName == other.PropertyType.FullName;
		}

		public override int GetHashCode()
		{
			return PropertyName.GetHashCode() ^ PropertyType.FullName.GetHashCode();
		}
	}

	internal class TypeDefinitionCache : IMetadataResolver
	{
		readonly Dictionary<TypeReference, TypeDefinition> types = new Dictionary<TypeReference, TypeDefinition>();
		readonly Dictionary<FieldReference, FieldDefinition> fields = new Dictionary<FieldReference, FieldDefinition>();
		readonly Dictionary<MethodReference, MethodDefinition> methods = new Dictionary<MethodReference, MethodDefinition>();

		public virtual TypeDefinition Resolve(TypeReference typeReference)
		{
			if (types.TryGetValue(typeReference, out var typeDefinition))
				return typeDefinition;
			return types[typeReference] = typeReference.Resolve();
		}

		public virtual FieldDefinition Resolve(FieldReference field)
		{
			if (fields.TryGetValue(field, out var fieldDefinition))
				return fieldDefinition;
			return fields[field] = field.Resolve();
		}

		public virtual MethodDefinition Resolve(MethodReference method)
		{
			if (methods.TryGetValue(method, out var methodDefinition))
				return methodDefinition;
			return methods[method] = method.Resolve();
		}
	}

	internal class PreservePropertyInfoMap
	{
		private Dictionary<string, PreserveTypeDefinition> typeMap = new();
		private Dictionary<PreserveTypeDefinition, HashSet<PreservePropertyInfo>> propertiesMap = new();

		public bool TryGetPreserveType(string fullName, out PreserveTypeDefinition? preserveTypeDefinition)
		{
			return typeMap.TryGetValue(fullName, out preserveTypeDefinition!);
		}

		public HashSet<PreservePropertyInfo>? this[PreserveTypeDefinition preserveType]
		{
			get
			{
				if (propertiesMap.TryGetValue(preserveType, out var properties))
				{
					return properties;
				}
				return null;
			}
			set
			{
				if (value == null)
				{
					propertiesMap.Remove(preserveType);
					return;
				}
				typeMap[preserveType.FullName] = preserveType;
				propertiesMap[preserveType] = value;
			}
		}

		public IEnumerable<KeyValuePair<PreserveTypeDefinition, HashSet<PreservePropertyInfo>>> GetTypeProperties()
		{
			return propertiesMap;
		}
	}

	internal class ReferencePathAssemblyResolver : BaseAssemblyResolver
	{
		private Dictionary<string, string> referencePaths;
		private TaskLoggingHelper log;
		private Dictionary<string, AssemblyDefinition> cache = new();

		public ReferencePathAssemblyResolver(Dictionary<string, string> referencePaths, TaskLoggingHelper log)
		{
			this.referencePaths = referencePaths;
			this.log = log;
		}

		public override AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
		{
			if (cache.TryGetValue(name.FullName, out var assembly))
			{
				return assembly;
			}

			assembly = base.Resolve(name, parameters);
			cache[name.FullName] = assembly;

			return assembly;
		}

		protected override AssemblyDefinition SearchDirectory(AssemblyNameReference name, IEnumerable<string> directories, ReaderParameters parameters)
		{
			// Modified from: https://github.com/jbevain/cecil/blob/882ca5eedda1e62eb41bd5869aeb15d8f1538e51/Mono.Cecil/BaseAssemblyResolver.cs#L210-L227
			var extensions = name.IsWindowsRuntime
				? new[] { ".winmd", ".dll" }
				: new[] { ".dll", ".exe" };
			foreach (var extension in extensions)
			{
				var fileName = name.Name + extension;
				if (referencePaths.TryGetValue(fileName, out var path))
				{
					return ReadAssembly(path, name.Name, parameters);
				}
			}

			log.LogMessage(MessageImportance.High, $"Assembly name `{name.FullName}` was not in @(ReferencePath); falling back to arbitrary directories…");
			return base.SearchDirectory(name, directories, parameters);
		}

		private AssemblyDefinition ReadAssembly(string path, string assemblyName, ReaderParameters parameters)
		{
			return AssemblyDefinition.ReadAssembly(path, parameters);
		}

		protected override void Dispose(bool disposing)
		{
			// Copied from: https://github.com/jbevain/cecil/blob/882ca5eedda1e62eb41bd5869aeb15d8f1538e51/Mono.Cecil/DefaultAssemblyResolver.cs#L53-L58
			foreach (var assembly in cache.Values)
			{
				assembly.Dispose();
			}

			cache.Clear();

			base.Dispose(disposing);
		}
	}
}
