#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
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

		private DefaultAssemblyResolver? _assemblyResolver;

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
				Log.LogMessage(DefaultLogMessageLevel, $"BindableTypeLinkerGenerator: Processing assembly {AssemblyPath}");

				BuildAssemblyResolver();

				var assemblyList = BuildAssemblyList();
				var bindableTypes = FindBindableTypes(assemblyList);
				var referencedTypes = FindReferencedTypes(bindableTypes);

				if (referencedTypes.Count > 0)
				{
					GenerateLinkerDescriptor(referencedTypes);
					Log.LogMessage(MessageImportance.High, $"BindableTypeLinkerGenerator: Generated descriptor with {referencedTypes.Count} types");
				}
				else
				{
					Log.LogMessage(DefaultLogMessageLevel, $"BindableTypeLinkerGenerator: No types to preserve found");
				}

				// Dispose assemblies
				foreach (var asm in assemblyList)
				{
					asm.Dispose();
				}

				return true;
			}
			catch (Exception ex)
			{
				Log.LogError($"BindableTypeLinkerGenerator failed: {ex}");
				return false;
			}
		}

		private void BuildAssemblyResolver()
		{
			if (ReferencePath != null)
			{
				var searchPaths = ReferencePath
					.Select(p => Path.GetDirectoryName(p.ItemSpec))
					.Where(p => !string.IsNullOrEmpty(p))
					.Distinct()
					.ToArray();

				_assemblyResolver = new DefaultAssemblyResolver();

				foreach (var path in searchPaths)
				{
					_assemblyResolver.AddSearchDirectory(path!);
				}
			}
		}

		private List<AssemblyDefinition> BuildAssemblyList()
		{
			var assemblies = new List<AssemblyDefinition>();
			var visitedAssemblies = new HashSet<string>();
			var assembliesToProcess = new Queue<string>();

			// Start with the main assembly
			assembliesToProcess.Enqueue(AssemblyPath);

			while (assembliesToProcess.Count > 0)
			{
				var asmPath = assembliesToProcess.Dequeue();

				if (visitedAssemblies.Contains(asmPath))
				{
					continue;
				}

				visitedAssemblies.Add(asmPath);

				var asm = ReadAssembly(asmPath);
				if (asm == null)
				{
					continue;
				}

				assemblies.Add(asm);

				// Add referenced assemblies
				foreach (var reference in asm.MainModule.AssemblyReferences)
				{
					try
					{
						var resolvedAsm = _assemblyResolver?.Resolve(reference);
						if (resolvedAsm != null && !visitedAssemblies.Contains(resolvedAsm.MainModule.FileName))
						{
							assembliesToProcess.Enqueue(resolvedAsm.MainModule.FileName);
						}
					}
					catch (Exception ex)
					{
						Log.LogMessage(MessageImportance.Low, $"Failed to resolve {reference.FullName}: {ex.Message}");
					}
				}
			}

			Log.LogMessage(DefaultLogMessageLevel, $"BindableTypeLinkerGenerator: Loaded {assemblies.Count} assemblies");
			return assemblies;
		}

		private AssemblyDefinition? ReadAssembly(string asmPath)
		{
			try
			{
				return AssemblyDefinition.ReadAssembly(asmPath, new ReaderParameters { AssemblyResolver = _assemblyResolver });
			}
			catch (Exception ex)
			{
				Log.LogMessage(MessageImportance.Low, $"Failed to read assembly {asmPath}: {ex}");
				return null;
			}
		}

		private List<TypeDefinition> FindBindableTypes(List<AssemblyDefinition> assemblies)
		{
			var bindableTypes = new List<TypeDefinition>();

			foreach (var asm in assemblies)
			{
				foreach (var type in asm.MainModule.Types)
				{
					if (HasBindableAttribute(type))
					{
						bindableTypes.Add(type);
						Log.LogMessage(DefaultLogMessageLevel, $"BindableTypeLinkerGenerator: Found bindable type {type.FullName}");
					}
				}
			}

			Log.LogMessage(DefaultLogMessageLevel, $"BindableTypeLinkerGenerator: Found {bindableTypes.Count} bindable types");
			return bindableTypes;
		}

		private bool HasBindableAttribute(TypeDefinition type)
		{
			return type.CustomAttributes.Any(attr =>
				attr.AttributeType.FullName == "Microsoft.UI.Xaml.Data.BindableAttribute" ||
				attr.AttributeType.FullName == "Uno.Extensions.Reactive.Bindings.BindableAttribute");
		}

		private Dictionary<TypeDefinition, HashSet<string>> FindReferencedTypes(List<TypeDefinition> bindableTypes)
		{
			var referencedTypes = new Dictionary<TypeDefinition, HashSet<string>>();

			foreach (var bindableType in bindableTypes)
			{
				// Iterate over all public properties
				foreach (var property in bindableType.Properties.Where(p => p.GetMethod?.IsPublic == true))
				{
					ProcessPropertyType(property.PropertyType, referencedTypes);
				}
			}

			Log.LogMessage(DefaultLogMessageLevel, $"BindableTypeLinkerGenerator: Found {referencedTypes.Count} referenced types to preserve");
			return referencedTypes;
		}

		private void ProcessPropertyType(TypeReference propertyType, Dictionary<TypeDefinition, HashSet<string>> referencedTypes)
		{
			// Resolve the type
			TypeDefinition? resolvedType = null;
			try
			{
				resolvedType = propertyType.Resolve();
			}
			catch (Exception ex)
			{
				Log.LogMessage(MessageImportance.Low, $"Failed to resolve type {propertyType.FullName}: {ex.Message}");
				return;
			}

			if (resolvedType == null)
			{
				return;
			}

			// Process the main type
			AddTypeIfNeeded(resolvedType, referencedTypes);

			// Process generic arguments (e.g., List<Entity> should also process Entity)
			if (propertyType is GenericInstanceType genericType)
			{
				foreach (var genericArg in genericType.GenericArguments)
				{
					ProcessPropertyType(genericArg, referencedTypes);
				}
			}
		}

		private void AddTypeIfNeeded(TypeDefinition resolvedType, Dictionary<TypeDefinition, HashSet<string>> referencedTypes)
		{
			// Skip if type already has Bindable attribute
			if (HasBindableAttribute(resolvedType))
			{
				Log.LogMessage(MessageImportance.Low, $"Skipping {resolvedType.FullName} - already has Bindable attribute");
				return;
			}

			// Get all public properties with getters
			var publicProperties = resolvedType.Properties
				.Where(p => p.GetMethod?.IsPublic == true)
				.Select(p => p.Name)
				.ToList();

			// Skip if type has no public properties
			if (!publicProperties.Any())
			{
				Log.LogMessage(MessageImportance.Low, $"Skipping {resolvedType.FullName} - has no public properties");
				return;
			}

			// Add or update the type with its properties
			if (!referencedTypes.ContainsKey(resolvedType))
			{
				referencedTypes[resolvedType] = new HashSet<string>();
				Log.LogMessage(DefaultLogMessageLevel, $"BindableTypeLinkerGenerator: Adding referenced type {resolvedType.FullName}");
			}

			// Add all public properties to the set
			foreach (var propName in publicProperties)
			{
				referencedTypes[resolvedType].Add(propName);
			}
		}

		private void GenerateLinkerDescriptor(Dictionary<TypeDefinition, HashSet<string>> referencedTypes)
		{
			var doc = new XmlDocument();

			var xmlDeclaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
			doc.InsertBefore(xmlDeclaration, doc.DocumentElement);

			var linkerNode = doc.CreateElement("linker");
			doc.AppendChild(linkerNode);

			// Group types by assembly
			var typesByAssembly = referencedTypes
				.GroupBy(kvp => kvp.Key.Module.Assembly.Name.Name)
				.OrderBy(g => g.Key);

			foreach (var assemblyGroup in typesByAssembly)
			{
				var assemblyNode = doc.CreateElement("assembly");
				var fullnameAttr = doc.CreateAttribute("fullname");
				fullnameAttr.Value = assemblyGroup.Key;
				assemblyNode.Attributes.Append(fullnameAttr);

				foreach (var typeEntry in assemblyGroup.OrderBy(t => t.Key.FullName))
				{
					var type = typeEntry.Key;
					var properties = typeEntry.Value;

					var typeNode = doc.CreateElement("type");

					var typeFullnameAttr = doc.CreateAttribute("fullname");
					typeFullnameAttr.Value = type.FullName;
					typeNode.Attributes.Append(typeFullnameAttr);

					var requiredAttr = doc.CreateAttribute("required");
					requiredAttr.Value = "false";
					typeNode.Attributes.Append(requiredAttr);

					// Add property elements for each public property
					foreach (var propName in properties.OrderBy(p => p))
					{
						var propertyNode = doc.CreateElement("property");
						var nameAttr = doc.CreateAttribute("name");
						nameAttr.Value = propName;
						propertyNode.Attributes.Append(nameAttr);
						typeNode.AppendChild(propertyNode);
					}

					assemblyNode.AppendChild(typeNode);
				}

				linkerNode.AppendChild(assemblyNode);
			}

			// Ensure output directory exists
			var outputDir = Path.GetDirectoryName(OutputDescriptorPath);
			if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
			{
				Directory.CreateDirectory(outputDir);
			}

			doc.Save(OutputDescriptorPath);
			Log.LogMessage(MessageImportance.High, $"BindableTypeLinkerGenerator: Saved descriptor to {OutputDescriptorPath}");
		}
	}
}
