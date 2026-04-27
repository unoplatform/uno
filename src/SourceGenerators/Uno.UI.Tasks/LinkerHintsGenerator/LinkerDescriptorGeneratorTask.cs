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
	/// Unified task that generates ILLink descriptor files for Native AOT scenarios.
	/// Loads the assembly closure once and runs all registered descriptor generators against it.
	/// </summary>
	public class LinkerDescriptorGeneratorTask_v0 : Microsoft.Build.Utilities.Task
	{
		[Required]
		public string AssemblyPath { get; set; } = "";

		[Required]
		public string BindableDescriptorOutputPath { get; set; } = "";

		[Required]
		public string AttachedPropertiesDescriptorOutputPath { get; set; } = "";

		[Required]
		public Microsoft.Build.Framework.ITaskItem[]? ReferencePath { get; set; }

		public override bool Execute()
		{
			try
			{
				Log.LogMessage(LinkerDescriptorGenerator.DefaultLogMessageLevel, $"Processing assembly: {AssemblyPath}");

				using var resolver = BuildAssemblyResolver();
				var assemblyList = BuildAssemblyList(resolver);

				var bindableGenerator = new BindableTypeDescriptorGenerator(Log);
				var attachedGenerator = new AttachedPropertiesDescriptorGenerator(Log);

				bindableGenerator.Analyze(assemblyList);
				attachedGenerator.Analyze(assemblyList);

				bindableGenerator.WriteDescriptorIfResults(BindableDescriptorOutputPath);
				attachedGenerator.WriteDescriptorIfResults(AttachedPropertiesDescriptorOutputPath);

				return true;
			}
			catch (Exception ex)
			{
				Log.LogError($"LinkerDescriptorGenerator failed: {ex.Message}");
				Log.LogMessage(LinkerDescriptorGenerator.DefaultLogMessageLevel, ex.ToString());
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
			Log.LogMessage(LinkerDescriptorGenerator.DefaultLogMessageLevel, $"Resolved Root Assembly reference `{rootAssembly.FullName}`.");

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
						Log.LogMessage(LinkerDescriptorGenerator.DefaultLogMessageLevel, $"Resolved Assembly reference `{referencedAssembly.FullName}` via `{assembly.MainModule.FileName}`.");
						assembliesToProcess.Enqueue(referencedAssembly);
						assemblies.Add(referencedAssembly);
					}
				}
			}

			Log.LogMessage(LinkerDescriptorGenerator.DefaultLogMessageLevel, $"Loaded {assemblies.Count} assemblies.");
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
	}

	/// <summary>
	/// Base class for ILLink descriptor generators.
	/// Subclasses analyse a set of assemblies and emit XML preserving specific members.
	/// </summary>
	internal abstract class LinkerDescriptorGenerator
	{
		internal const MessageImportance DefaultLogMessageLevel
#if DEBUG
			= MessageImportance.High;
#else
			= MessageImportance.Low;
#endif

		protected TaskLoggingHelper Log { get; }

		protected LinkerDescriptorGenerator(TaskLoggingHelper log)
		{
			Log = log;
		}

		/// <summary>Scan <paramref name="assemblies"/> and collect results.</summary>
		public abstract void Analyze(IEnumerable<AssemblyDefinition> assemblies);

		/// <summary>Returns <see langword="true"/> when <see cref="Analyze"/> found at least one member to preserve.</summary>
		public abstract bool HasResults { get; }

		/// <summary>Write the ILLink descriptor XML to <paramref name="outputPath"/>.</summary>
		public abstract void WriteDescriptor(string outputPath);

		/// <summary>
		/// Calls <see cref="WriteDescriptor"/> when there are results to preserve;
		/// otherwise logs a diagnostic message and does nothing.
		/// </summary>
		public void WriteDescriptorIfResults(string outputPath)
		{
			if (HasResults)
			{
				WriteDescriptor(outputPath);
			}
			else
			{
				Log.LogMessage(DefaultLogMessageLevel, $"No results to preserve; skipping {outputPath}.");
			}
		}

		protected static IEnumerable<TypeDefinition> GetAllTypes(TypeDefinition type)
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

		protected static void EnsureDirectory(string outputPath)
		{
			var outputDir = Path.GetDirectoryName(outputPath);
			if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
			{
				Directory.CreateDirectory(outputDir);
			}
		}
	}

	/// <summary>
	/// Preserves public properties on types that are referenced by [Bindable]-annotated types.
	/// </summary>
	internal sealed class BindableTypeDescriptorGenerator : LinkerDescriptorGenerator
	{
		private PreservePropertyInfoMap? _typesToProperties;

		public BindableTypeDescriptorGenerator(TaskLoggingHelper log) : base(log) { }

		public override bool HasResults =>
			_typesToProperties?.GetTypeProperties().Any(kvp => kvp.Value.Count > 0) ?? false;

		public override void Analyze(IEnumerable<AssemblyDefinition> assemblies)
		{
			var bindableTypes = FindBindableTypes(assemblies);
			var typeCache = new TypeDefinitionCache();
			_typesToProperties = FindReferencedPropertyTypes(bindableTypes, typeCache);

			var typesWithProperties = _typesToProperties.GetTypeProperties().Count(kvp => kvp.Value.Count > 0);
			Log.LogMessage(DefaultLogMessageLevel, $"Found {typesWithProperties} implicitly referenced types with properties to preserve.");
		}

		public override void WriteDescriptor(string outputPath)
		{
			if (_typesToProperties == null)
			{
				return;
			}

			EnsureDirectory(outputPath);
			GenerateLinkerDescriptor(_typesToProperties, outputPath);
		}

		private List<TypeDefinition> FindBindableTypes(IEnumerable<AssemblyDefinition> assemblies)
		{
			var bindableTypes = new List<TypeDefinition>();

			foreach (var assembly in assemblies)
			{
				foreach (var type in assembly.MainModule.Types.SelectMany(GetAllTypes))
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

		private static bool HasBindableAttribute(TypeDefinition type)
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

		private static readonly HashSet<PreservePropertyInfo> EmptyPreservedPropertyInfo = [];

		private PreserveTypeDefinition? AddDeclaredPropertyTypes(TypeDefinitionCache typeCache, TypeReference typeReference, PreservePropertyInfoMap typesToProperties)
		{
			var typeDefinition = TryResolveTypeDefinition(typeCache, typeReference);
			if (typeDefinition == null)
			{
				return null;
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

			if (typesToProperties.TryGetPreserveType(typeDefinition.FullName, out var existingType))
			{
				// Already processed; return the *existing* key, so that callers can call `.AddContext()`
				return existingType;
			}

			var key = new PreserveTypeDefinition(typeDefinition.FullName, typeDefinition);

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
				typesToProperties[key] = EmptyPreservedPropertyInfo;
				return key;
			}

			var declaredProperties = new HashSet<PreservePropertyInfo>();

			foreach (var property in typeDefinition.Properties.Where(ShouldProcessPropertyType))
			{
				declaredProperties.Add(new PreservePropertyInfo(property.Name, property.PropertyType));
			}
			typesToProperties[key] = declaredProperties;

			foreach (var property in declaredProperties)
			{
				AddDeclaredPropertyTypes(typeCache, property.PropertyType, typesToProperties)
					?.AddContext($"From Property `{property.PropertyName} : {property.PropertyType.FullName}` on type `{typeDefinition.FullName}`");
			}
			return key;
		}

		private static bool ShouldProcessPropertyType(PropertyDefinition property)
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

		private void GenerateLinkerDescriptor(PreservePropertyInfoMap referencedTypes, string outputPath)
		{
			// Group types by assembly
			var typesByAssembly = referencedTypes
				.GetTypeProperties()
				.GroupBy(kvp => kvp.Key.TypeDefinition.Module.Assembly.Name.Name)
				.OrderBy(g => g.Key);

			var linkerElement = new XElement("linker",
				typesByAssembly.Select(assemblyGroup =>
					CreateAssemblyElement(assemblyGroup.Key, assemblyGroup)));

			var doc = new XDocument(
				new XDeclaration("1.0", "UTF-8", null),
				linkerElement);

			doc.Save(outputPath);
			Log.LogMessage(MessageImportance.High, $"Saved bindable descriptor to {outputPath}");

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

	/// <summary>
	/// Preserves <c>Get&lt;Name&gt;</c> and <c>Set&lt;Name&gt;</c> static methods for attached
	/// <see cref="Microsoft.UI.Xaml.DependencyProperty"/> members so that they survive trimming
	/// and Native AOT without requiring manual <c>[DynamicDependency]</c> annotations.
	/// </summary>
	internal sealed class AttachedPropertiesDescriptorGenerator : LinkerDescriptorGenerator
	{
		private const string DependencyPropertyFullName = "Microsoft.UI.Xaml.DependencyProperty";
		private const string PropertySuffix = "Property";

		private Dictionary<TypeDefinition, List<string>>? _typeMethods;

		public AttachedPropertiesDescriptorGenerator(TaskLoggingHelper log) : base(log) { }

		public override bool HasResults => (_typeMethods?.Count ?? 0) > 0;

		public override void Analyze(IEnumerable<AssemblyDefinition> assemblies)
		{
			_typeMethods = FindAttachedPropertyMethods(assemblies);
			Log.LogMessage(DefaultLogMessageLevel, $"Found {_typeMethods.Values.Sum(v => v.Count)} attached property methods to preserve in {_typeMethods.Count} types.");
		}

		public override void WriteDescriptor(string outputPath)
		{
			if (_typeMethods == null)
			{
				return;
			}

			EnsureDirectory(outputPath);
			GenerateLinkerDescriptor(_typeMethods, outputPath);
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

		private void GenerateLinkerDescriptor(Dictionary<TypeDefinition, List<string>> typeMethods, string outputPath)
		{
			var typesByAssembly = typeMethods
				.GroupBy(kvp => kvp.Key.Module.Assembly.Name.Name)
				.OrderBy(g => g.Key);

			var linkerElement = new XElement("linker",
				typesByAssembly.Select(assemblyGroup =>
					CreateAssemblyElement(assemblyGroup.Key, assemblyGroup)));

			var doc = new XDocument(
				new XDeclaration("1.0", "UTF-8", null),
				linkerElement);

			doc.Save(outputPath);
			Log.LogMessage(MessageImportance.High, $"Saved attached properties descriptor to {outputPath}");

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
