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
		private const MessageImportance DefaultLogMessageLevel
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

				var typesWithProperties = typesToProperties.Count(kvp => kvp.Value.Count > 0);
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

		private DefaultAssemblyResolver BuildAssemblyResolver()
		{
			var resolver = new DefaultAssemblyResolver();

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

		private HashSet<AssemblyDefinition> BuildAssemblyList(DefaultAssemblyResolver resolver)
		{
			var rootAssembly = resolver.Resolve(AssemblyNameReference.Parse(Path.GetFileNameWithoutExtension(AssemblyPath)));
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

		private AssemblyDefinition? TryResolveAssembly(DefaultAssemblyResolver resolver, AssemblyNameReference assemblyName)
		{
			try
			{
				return resolver.Resolve(assemblyName);
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

		private Dictionary<PreserveTypeDefinition, HashSet<PreservePropertyInfo>> FindReferencedPropertyTypes(IEnumerable<TypeDefinition> bindableTypes, TypeDefinitionCache typeCache)
		{
			var typesToProperties = new Dictionary<PreserveTypeDefinition, HashSet<PreservePropertyInfo>>();

			foreach (var bindableType in bindableTypes)
			{
				if (!bindableType.HasProperties)
				{
					continue;
				}

				// Iterate over all public properties
				foreach (var property in bindableType.Properties.Where(ShouldProcessPropertyType))
				{
					AddDeclaredPropertyTypes(typeCache, property.PropertyType, typesToProperties);
				}
			}

			Log.LogMessage(DefaultLogMessageLevel, $"Found {typesToProperties.Count} implicitly referenced types.");
			return typesToProperties;
		}

		static readonly HashSet<PreservePropertyInfo> EmptyPreservedPropertyInfo = [];

		private void AddDeclaredPropertyTypes(TypeDefinitionCache typeCache, TypeReference typeReference, Dictionary<PreserveTypeDefinition, HashSet<PreservePropertyInfo>> typesToProperties)
		{
			var typeDefinition = TryResolveTypeDefinition(typeCache, typeReference);
			if (typeDefinition == null)
			{
				return;
			}

			var key = new PreserveTypeDefinition(typeDefinition.FullName, typeDefinition);

			if (typesToProperties.ContainsKey(key) ||
					HasBindableAttribute(typeDefinition))
			{
				return;
			}

			if (!typeDefinition.HasProperties)
			{
				typesToProperties[key] = EmptyPreservedPropertyInfo;
				return;
			}

			var declaredProperties = new HashSet<PreservePropertyInfo>();

			foreach (var property in typeDefinition.Properties.Where(ShouldProcessPropertyType))
			{
				declaredProperties.Add(new PreservePropertyInfo(property.Name, property.PropertyType));
			}
			typesToProperties[key] = declaredProperties;

			foreach (var property in declaredProperties)
			{
				AddDeclaredPropertyTypes(typeCache, property.PropertyType, typesToProperties);
			}

			// Process generic arguments (e.g., List<Entity> should also process Entity)
			if (typeReference is GenericInstanceType genericType)
			{
				foreach (var genericArg in genericType.GenericArguments)
				{
					AddDeclaredPropertyTypes(typeCache, genericArg, typesToProperties);
				}
			}
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

		private void GenerateLinkerDescriptor(Dictionary<PreserveTypeDefinition, HashSet<PreservePropertyInfo>> referencedTypes)
		{
			// Group types by assembly
			var typesByAssembly = referencedTypes
				.GroupBy(kvp => kvp.Key.TypeDefinition.Module.Assembly.Name.Name)
				.OrderBy(g => g.Key);

			var linkerElement = new XElement("linker",
				typesByAssembly
					.Select(assemblyGroup => CreateAssemblyElement(assemblyGroup.Key, assemblyGroup))
					.Where(element => element != null));

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
					types.OrderBy(t => t.Key.FullName)
						.Select(typeEntry => CreateTypeElement(typeEntry.Key.FullName, typeEntry.Value))
						.Where(element => element != null));
			}

			static XElement? CreateTypeElement(string typeFullName, HashSet<PreservePropertyInfo> properties)
			{
				if (properties.Count == 0)
				{
					return null; // Skip types with no public properties
				}

				return new XElement("type",
					new XAttribute("fullname", typeFullName),
					new XAttribute("required", "false"),
					properties.OrderBy(p => p.PropertyName)
						.Select(p =>
							new XElement("property",
								new XAttribute("name", p.PropertyName),
								new XComment($" {p.PropertyType.FullName ?? "<<unresolved>>"} {p.PropertyName} {{get;}} "))));
			}
		}
	}

	internal sealed class PreserveTypeDefinition : IEquatable<PreserveTypeDefinition>
	{
		public string FullName { get; }
		// Note: This property should not be mutated after construction as it's used as a dictionary key
		public TypeDefinition TypeDefinition { get; set; }

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
}
