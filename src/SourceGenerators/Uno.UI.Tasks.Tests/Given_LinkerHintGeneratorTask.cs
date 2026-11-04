using AwesomeAssertions;
using Mono.Cecil;
using Uno.UI.Tasks.LinkerHintsGenerator;

namespace Uno.UI.Tasks.Tests;

[TestClass]
public class Given_LinkerHintGeneratorTask
{
	[TestMethod]
	public void When_Concrete_Type_Kept_Without_Constructor_Then_Not_Creatable()
	{
		var type = CreateType("KeptForTypeCheck", TypeAttributes.Public | TypeAttributes.Class, withConstructor: false);

		LinkerHintGeneratorTask_v0.CanBeCreated(type).Should().BeFalse("a TypeDef kept only by an `is` check must not enable its bindable metadata");
	}

	[TestMethod]
	public void When_Concrete_Type_Keeps_Constructor_Then_Creatable()
	{
		var type = CreateType("Instantiated", TypeAttributes.Public | TypeAttributes.Class, withConstructor: true);

		LinkerHintGeneratorTask_v0.CanBeCreated(type).Should().BeTrue();
	}

	[TestMethod]
	public void When_Type_Is_Abstract_Then_Creatable()
	{
		var abstractType = CreateType("AbstractBase", TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Abstract, withConstructor: false);
		var staticType = CreateType("StaticHelper", TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Abstract | TypeAttributes.Sealed, withConstructor: false);

		LinkerHintGeneratorTask_v0.CanBeCreated(abstractType).Should().BeTrue("derived types' hints rely on their base staying available");
		LinkerHintGeneratorTask_v0.CanBeCreated(staticType).Should().BeTrue();
	}

	[TestMethod]
	public void When_Only_Static_Constructor_Kept_Then_Not_Creatable()
	{
		var type = CreateType("StaticFieldsOnly", TypeAttributes.Public | TypeAttributes.Class, withConstructor: false);
		var module = type.Module;
		type.Methods.Add(new MethodDefinition(".cctor", MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.HideBySig, module.TypeSystem.Void));

		LinkerHintGeneratorTask_v0.CanBeCreated(type).Should().BeFalse("reading a static DependencyProperty keeps the type initializer, not an instance");
	}

	private static TypeDefinition CreateType(string name, TypeAttributes attributes, bool withConstructor)
	{
		var assembly = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("HintLib", new Version(1, 0)), "HintLib", ModuleKind.Dll);
		var module = assembly.MainModule;
		var type = new TypeDefinition("HintLib", name, attributes, module.TypeSystem.Object);
		if (withConstructor)
		{
			type.Methods.Add(new MethodDefinition(".ctor", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.HideBySig, module.TypeSystem.Void));
		}
		module.Types.Add(type);
		return type;
	}
}
