using System.Collections.Immutable;
using System.Text;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Uno.HotReload.Tests.TestUtils;

namespace Uno.HotReload.Tests.Microsoft;

[TestClass]
public sealed class Given_WatchHotReloadService_RudeEdit
{
	public TestContext TestContext { get; set; } = null!;

	private static SourceText App(string baseList, string value)
		=> SourceText.From($$"""
			public interface IMarker { }
			public class C {{baseList}}
			{
				public static string M() => "{{value}}";
			}
			""", Encoding.UTF8);

	[TestMethod]
	[Description(
		"Issue #25378: Roslyn 5.x reports a blocking rude edit as a Ready update with the project to " +
		"rebuild, and committing it adopts the rejected text and discards the project's emit baseline. " +
		"The rejected edit must stay rejected, and reverting it must bring the session back to the " +
		"last applied update.")]
	public async Task When_RudeEditIsRejected_Then_ItIsNotCommitted()
	{
		var ct = TestContext.CancellationTokenSource.Token;
		using var temp = new TempDirectory();
		using var harness = await EnCHarness.CreateAsync(temp, ct, App("", "v0"));

		async Task<(ImmutableArray<Update> updates, ImmutableArray<Diagnostic> errors)> Emit(string baseList, string value)
		{
			var (updates, diagnostics) = await harness.Watch.EmitSolutionUpdateAsync(
				harness.Solution.WithDocumentText(harness.DocumentId, App(baseList, value)),
				ct);

			return (updates, [.. diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)]);
		}

		var applied = await Emit("", "v1");
		applied.errors.Should().BeEmpty();
		applied.updates.Should().HaveCount(1);

		var rude = await Emit(": IMarker", "v1");
		rude.updates.Should().BeEmpty();
		rude.errors.Should().ContainSingle(d => d.Id == "ENC0014");

		var stillRude = await Emit(": IMarker", "v2");
		stillRude.updates.Should().BeEmpty("the rejected base-list change is still in the source");
		stillRude.errors.Should().ContainSingle(d => d.Id == "ENC0014");

		var reverted = await Emit("", "v2");
		reverted.errors.Should().BeEmpty("the revert returns to the last applied update, not to the rejected one");
		reverted.updates.Should().HaveCount(1);
	}
}
