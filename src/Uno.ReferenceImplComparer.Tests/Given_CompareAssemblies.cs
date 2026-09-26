using AwesomeAssertions;
using Mono.Cecil;

namespace Uno.ReferenceImplComparer.Tests;

[TestClass]
public class Given_CompareAssemblies
{
	private const string Namespace = "Windows.Test";

	[TestMethod]
	public void When_Surfaces_Match_Then_No_Errors()
	{
		var reference = CreateAssembly(m => AddClass(m, "Widget", AddInterface(m, "IWidget")));
		var runtime = CreateAssembly(m => AddClass(m, "Widget", AddInterface(m, "IWidget")));

		Program.CompareAssemblies(reference, runtime, "skia").Should().BeEmpty();
	}

	[TestMethod]
	public void When_Reference_Type_Is_Missing_From_Runtime_Then_Error()
	{
		var reference = CreateAssembly(m => AddClass(m, "Widget"));
		var runtime = CreateAssembly(_ => { });

		Program.CompareAssemblies(reference, runtime, "skia").Should().ContainSingle()
			.Which.Should().Contain("Windows.Test.Widget").And.Contain("skia");
	}

	[TestMethod]
	public void When_Runtime_Has_Extra_Public_Type_Then_Error()
	{
		var reference = CreateAssembly(_ => { });
		var runtime = CreateAssembly(m => AddInterface(m, "IWidgetExtension"));

		Program.CompareAssemblies(reference, runtime, "skia").Should().ContainSingle()
			.Which.Should().Contain("Windows.Test.IWidgetExtension").And.Contain("skia");
	}

	[TestMethod]
	public void When_Runtime_Has_Extra_Internal_Type_Then_No_Errors()
	{
		var reference = CreateAssembly(_ => { });
		var runtime = CreateAssembly(m => AddInterface(m, "IWidgetExtension", isPublic: false));

		Program.CompareAssemblies(reference, runtime, "skia").Should().BeEmpty();
	}

	[TestMethod]
	public void When_Runtime_Implements_Extra_Public_Interface_Then_Error()
	{
		var reference = CreateAssembly(m =>
		{
			AddInterface(m, "IWidget");
			AddClass(m, "Widget");
		});
		var runtime = CreateAssembly(m => AddClass(m, "Widget", AddInterface(m, "IWidget")));

		Program.CompareAssemblies(reference, runtime, "skia").Should().ContainSingle()
			.Which.Should().Contain("Windows.Test.Widget").And.Contain("Windows.Test.IWidget");
	}

	[TestMethod]
	public void When_Reference_Implements_Extra_Public_Interface_Then_Error()
	{
		var reference = CreateAssembly(m => AddClass(m, "Widget", AddInterface(m, "IWidget")));
		var runtime = CreateAssembly(m =>
		{
			AddInterface(m, "IWidget");
			AddClass(m, "Widget");
		});

		Program.CompareAssemblies(reference, runtime, "skia").Should().ContainSingle()
			.Which.Should().Contain("Windows.Test.Widget").And.Contain("Windows.Test.IWidget");
	}

	[TestMethod]
	public void When_Runtime_Implements_Extra_Internal_Interface_Then_No_Errors()
	{
		var reference = CreateAssembly(m => AddClass(m, "Widget"));
		var runtime = CreateAssembly(m => AddClass(m, "Widget", AddInterface(m, "IWidgetHost", isPublic: false)));

		Program.CompareAssemblies(reference, runtime, "skia").Should().BeEmpty();
	}

	private static AssemblyDefinition CreateAssembly(Action<ModuleDefinition> populate)
	{
		var assembly = AssemblyDefinition.CreateAssembly(
			new AssemblyNameDefinition("Uno.WinRT", new Version(1, 0)),
			"Uno.WinRT",
			ModuleKind.Dll);

		populate(assembly.MainModule);

		return assembly;
	}

	private static TypeDefinition AddInterface(ModuleDefinition module, string name, bool isPublic = true)
	{
		var type = new TypeDefinition(
			Namespace,
			name,
			(isPublic ? TypeAttributes.Public : TypeAttributes.NotPublic) | TypeAttributes.Interface | TypeAttributes.Abstract);

		module.Types.Add(type);

		return type;
	}

	private static TypeDefinition AddClass(ModuleDefinition module, string name, params TypeDefinition[] interfaces)
	{
		var type = new TypeDefinition(Namespace, name, TypeAttributes.Public | TypeAttributes.Class, module.TypeSystem.Object);

		foreach (var @interface in interfaces)
		{
			type.Interfaces.Add(new InterfaceImplementation(@interface));
		}

		module.Types.Add(type);

		return type;
	}
}
