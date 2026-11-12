using System.Xml.Linq;
using AwesomeAssertions;
using Microsoft.Build.Framework;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Uno.UI.Tasks.LinkerHintsGenerator;

namespace Uno.UI.Tasks.Tests;

[TestClass]
public class Given_LinkerDescriptorGeneratorTask
{
	// Android runs ILLink before ILC: an optional entry lets ILLink drop an attached property that is only reached by
	// name (a binding path), so the binding resolves nothing at runtime.
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24591")]
	public void When_Attached_Property_Found_Then_Its_Type_Is_Required()
	{
		var folder = Directory.CreateTempSubdirectory("uno-linker-descriptor-");
		try
		{
			var assemblyPath = Path.Combine(folder.FullName, "AttachedLib.dll");
			WriteAssemblyWithAttachedProperty(assemblyPath);

			var attachedDescriptor = Path.Combine(folder.FullName, "Uno.AttachedProperties.Descriptor.xml");
			var engine = new RecordingBuildEngine();
			var task = new LinkerDescriptorGeneratorTask_v0
			{
				BuildEngine = engine,
				AssemblyPath = assemblyPath,
				BindableDescriptorOutputPath = Path.Combine(folder.FullName, "Uno.Bindable.Descriptor.xml"),
				AttachedPropertiesDescriptorOutputPath = attachedDescriptor,
				ReferencePath = Array.Empty<ITaskItem>(),
			};

			task.Execute().Should().BeTrue(string.Join(Environment.NewLine, engine.Errors.Select(e => e.Message)));

			var type = XDocument.Load(attachedDescriptor).Descendants("type").Single(t => (string?)t.Attribute("fullname") == "AttachedLib.Attached");
			type.Attribute("required").Should().BeNull("an optional entry lets ILLink remove a type only reached by name");
			type.Elements("method").Select(m => (string?)m.Attribute("name")).Should().BeEquivalentTo("GetFoo", "SetFoo");
		}
		finally
		{
			folder.Delete(recursive: true);
		}
	}

	private static void WriteAssemblyWithAttachedProperty(string path)
	{
		using var assembly = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("AttachedLib", new Version(1, 0)), "AttachedLib", ModuleKind.Dll);
		var module = assembly.MainModule;

		var dependencyProperty = new TypeDefinition("Microsoft.UI.Xaml", "DependencyProperty", TypeAttributes.Public | TypeAttributes.Class, module.TypeSystem.Object);
		module.Types.Add(dependencyProperty);

		var attached = new TypeDefinition("AttachedLib", "Attached", TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.Class, module.TypeSystem.Object);
		attached.Fields.Add(new FieldDefinition("FooProperty", FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.InitOnly, dependencyProperty));
		attached.Methods.Add(CreateStaticMethod("GetFoo", module.TypeSystem.String, module.TypeSystem.Object));
		attached.Methods.Add(CreateStaticMethod("SetFoo", module.TypeSystem.Void, module.TypeSystem.Object, module.TypeSystem.String));
		module.Types.Add(attached);

		assembly.Write(path);
	}

	private static MethodDefinition CreateStaticMethod(string name, TypeReference returnType, params TypeReference[] parameters)
	{
		var method = new MethodDefinition(name, MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig, returnType);
		foreach (var parameter in parameters)
		{
			method.Parameters.Add(new ParameterDefinition(parameter));
		}

		var il = method.Body.GetILProcessor();
		if (returnType.MetadataType != MetadataType.Void)
		{
			il.Emit(OpCodes.Ldnull);
		}

		il.Emit(OpCodes.Ret);
		return method;
	}
}
