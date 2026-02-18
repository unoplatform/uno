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

		private Dictionary<string, TypeDefinition> FindReferencedTypes(List<TypeDefinition> bindableTypes)
		{
			var referencedTypes = new Dictionary<string, TypeDefinition>();

			foreach (var bindableType in bindableTypes)
			{
				// Iterate over all public properties
				foreach (var property in bindableType.Properties.Where(p => p.GetMethod?.IsPublic == true))
				{
					var propertyType = property.PropertyType;

					// Resolve the type
					TypeDefinition? resolvedType = null;
					try
					{
						resolvedType = propertyType.Resolve();
					}
					catch (Exception ex)
					{
						Log.LogMessage(MessageImportance.Low, $"Failed to resolve type {propertyType.FullName}: {ex.Message}");
						continue;
					}

					if (resolvedType == null)
					{
						continue;
					}

					// Skip if type already has Bindable attribute
					if (HasBindableAttribute(resolvedType))
					{
						Log.LogMessage(MessageImportance.Low, $"Skipping {resolvedType.FullName} - already has Bindable attribute");
						continue;
					}

					// Skip if type has no properties
					if (!resolvedType.Properties.Any())
					{
						Log.LogMessage(MessageImportance.Low, $"Skipping {resolvedType.FullName} - has no properties");
						continue;
					}

					// Add to referenced types
					if (!referencedTypes.ContainsKey(resolvedType.FullName))
					{
						referencedTypes[resolvedType.FullName] = resolvedType;
						Log.LogMessage(DefaultLogMessageLevel, $"BindableTypeLinkerGenerator: Adding referenced type {resolvedType.FullName}");
					}
				}
			}

			Log.LogMessage(DefaultLogMessageLevel, $"BindableTypeLinkerGenerator: Found {referencedTypes.Count} referenced types to preserve");
			return referencedTypes;
		}

		private void GenerateLinkerDescriptor(Dictionary<string, TypeDefinition> referencedTypes)
		{
			var doc = new XmlDocument();

			var xmlDeclaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
			doc.InsertBefore(xmlDeclaration, doc.DocumentElement);

			var linkerNode = doc.CreateElement("linker");
			doc.AppendChild(linkerNode);

			// Group types by assembly
			var typesByAssembly = referencedTypes.Values
				.GroupBy(t => t.Module.Assembly.Name.Name)
				.OrderBy(g => g.Key);

			foreach (var assemblyGroup in typesByAssembly)
			{
				var assemblyNode = doc.CreateElement("assembly");
				var fullnameAttr = doc.CreateAttribute("fullname");
				fullnameAttr.Value = assemblyGroup.Key;
				assemblyNode.Attributes.Append(fullnameAttr);

				foreach (var type in assemblyGroup.OrderBy(t => t.FullName))
				{
					var typeNode = doc.CreateElement("type");

					var typeFullnameAttr = doc.CreateAttribute("fullname");
					typeFullnameAttr.Value = type.FullName;
					typeNode.Attributes.Append(typeFullnameAttr);

					var preserveAttr = doc.CreateAttribute("preserve");
					preserveAttr.Value = "methods";
					typeNode.Attributes.Append(preserveAttr);

					var requiredAttr = doc.CreateAttribute("required");
					requiredAttr.Value = "false";
					typeNode.Attributes.Append(requiredAttr);

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
