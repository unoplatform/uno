using AwesomeAssertions;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Uno.WinAppSDKSyncGenerator.Helpers;

namespace Uno.WinAppSDKSyncGenerator.Tests;

[TestClass]
public class Given_SymbolMatchingHelpers
{
	private const string WinUISource = """
		namespace Microsoft.UI.Xaml
		{
			public class SetterBase { }
			public class Brush { }

			public sealed class SetterBaseCollection
			{
				public void Add(SetterBase item) { }
				public SetterBase this[int index] { get => null; set { } }
				public SetterBase GetFirst() => null;
				public void Paint(Brush brush) { }
			}
		}
		""";

	// Uno makes SetterBase abstract and inherits the collection members from a generic base.
	private const string UnoSource = """
		namespace Microsoft.UI.Xaml
		{
			public abstract class SetterBase { }
			public class OtherBrush { }

			public class DependencyObjectCollection<T>
			{
				public void Add(T item) { }
				public T this[int index] { get => default; set { } }
				public T GetFirst() => default;
				public void Paint(OtherBrush brush) { }
			}

			public sealed class SetterBaseCollection : DependencyObjectCollection<SetterBase> { }
		}
		""";

	[TestMethod]
	public void When_Inherited_Method_Parameter_Type_Is_Abstract_In_Uno()
		=> AreMatching("Add").Should().BeTrue();

	[TestMethod]
	public void When_Inherited_Method_Return_Type_Is_Abstract_In_Uno()
		=> AreMatching("GetFirst").Should().BeTrue();

	[TestMethod]
	public void When_Inherited_Indexer_Type_Is_Abstract_In_Uno()
		=> AreMatching("this[]").Should().BeTrue();

	[TestMethod]
	public void When_Parameter_Type_Differs()
		=> AreMatching("Paint").Should().BeFalse();

	private static bool AreMatching(string memberName)
	{
		var winUIMember = GetCollectionType(WinUISource).GetMembers(memberName).Single();
		var unoMember = GetCollectionType(UnoSource).BaseType!.GetMembers(memberName).Single();

		return SymbolMatchingHelpers.AreMatching(winUIMember, unoMember);
	}

	private static INamedTypeSymbol GetCollectionType(string source)
	{
		var compilation = CSharpCompilation.Create(
			"Test",
			[CSharpSyntaxTree.ParseText(source)],
			Net100.References.All,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		return compilation.GetTypeByMetadataName("Microsoft.UI.Xaml.SetterBaseCollection")!;
	}
}
