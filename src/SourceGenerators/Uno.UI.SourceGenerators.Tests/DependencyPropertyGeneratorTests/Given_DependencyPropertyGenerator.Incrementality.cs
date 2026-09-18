using Microsoft.CodeAnalysis;
using static Uno.UI.SourceGenerators.Tests.DependencyPropertyGeneratorTests.DependencyPropertyGeneratorHarness;

namespace Uno.UI.SourceGenerators.Tests.DependencyPropertyGeneratorTests;

public partial class Given_DependencyPropertyGenerator
{
	private const string FirstTypeSource = """
		using Microsoft.UI.Xaml;
		using TestHelpers;
		using Uno.UI.Xaml;

		namespace Mynamespace
		{
			public enum MyEnum { First, Second }

			public partial class First : TestDependencyObject
			{
				[GeneratedDependencyProperty(DefaultValue = MyEnum.Second, ChangedCallback = true)]
				public partial MyEnum MyValue { get; set; }

				private void OnMyValueChanged(MyEnum oldValue, MyEnum newValue) { }
			}
		}
		""";

	private const string SecondTypeSource = """
		using TestHelpers;
		using Uno.UI.Xaml;

		namespace Mynamespace
		{
			public partial class Second : TestDependencyObject
			{
				[GeneratedDependencyProperty(DefaultValue = 1)]
				public partial int Count { get; set; }
			}
		}
		""";

	[TestMethod]
	public async Task When_Only_Trivia_Changes_Outputs_Are_Cached()
	{
		var run = await RunWithBoxesAsync(FirstTypeSource, SecondTypeSource);
		run.ShouldSucceed();

		var rerun = run.RerunWithReplacedSource(FirstTypeSource, FirstTypeSource.Replace("private void OnMyValueChanged", "// A comment\n\t\tprivate void OnMyValueChanged"));

		rerun.StepReasons("Results").Should().OnlyContain(r => r == IncrementalStepRunReason.Unchanged || r == IncrementalStepRunReason.Cached);
		rerun.StepReasons("Boxes").Should().OnlyContain(r => r == IncrementalStepRunReason.Unchanged || r == IncrementalStepRunReason.Cached);
		rerun.SourceOutputReasons.Should().NotBeEmpty().And.OnlyContain(r => r == IncrementalStepRunReason.Cached);
	}

	[TestMethod]
	public async Task When_One_Type_Changes_Other_Types_Are_Not_Regenerated()
	{
		var run = await RunWithBoxesAsync(FirstTypeSource, SecondTypeSource);
		run.ShouldSucceed();

		var rerun = run.RerunWithReplacedSource(SecondTypeSource, SecondTypeSource.Replace("DefaultValue = 1", "DefaultValue = 2"));
		rerun.ShouldSucceed().ShouldContain("Mynamespace.Second.g.cs", "defaultValue: 2,");

		var typeSteps = rerun.RunResult.TrackedSteps["Types"]
			.SelectMany(step => step.Outputs)
			.Select(output => (Type: GetContainingTypeMetadataName(output.Value), output.Reason))
			.ToArray();

		typeSteps.Should().Contain(("Mynamespace.First", IncrementalStepRunReason.Unchanged));
		typeSteps.Should().Contain(("Mynamespace.Second", IncrementalStepRunReason.Modified));
	}

	// The pipeline models are internal to the generator assembly.
	private static string? GetContainingTypeMetadataName(object typeInfo)
	{
		var containingType = typeInfo.GetType().GetProperty("ContainingType")!.GetValue(typeInfo)!;
		return (string?)containingType.GetType().GetProperty("MetadataName")!.GetValue(containingType);
	}
}
