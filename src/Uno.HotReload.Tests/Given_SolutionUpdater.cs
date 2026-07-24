using System.Collections.Immutable;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Uno.HotReload.Diffing;
using Uno.HotReload.Tests.TestUtils;

namespace Uno.HotReload.Tests;

[TestClass]
public class Given_SolutionUpdater
{
	public TestContext TestContext { get; set; } = null!;

	[TestMethod]
	[Description(
		"Spec 055 R2: a change-set entry whose on-disk content is byte-identical to the document's " +
		"current text is not re-applied — a fully-reduced batch returns the original solution " +
		"instance (reference-equal), enabling the manager's NoChanges short-circuit — and the " +
		"skipped entry is surfaced through UpToDateChanges.")]
	public async Task When_DiskContentIsIdentical_Then_OriginalSolutionInstanceReturned()
	{
		var ct = TestContext.CancellationTokenSource.Token;
		using var temp = new TempDirectory();
		var path = await temp.WriteFileAsync("Model.cs", "class Model { }", ct);

		using var workspace = new AdhocWorkspace();
		var projectId = ProjectId.CreateNewId();
		var documentId = DocumentId.CreateNewId(projectId);
		var solution = workspace.CurrentSolution
			.AddProject(projectId, "TestProject", "TestProject", LanguageNames.CSharp)
			.AddDocument(documentId, "Model.cs", SourceText.From("class Model { }"), filePath: path);

		// Realize the text, as the initial compilation does on the real workspace.
		_ = await solution.GetDocument(documentId)!.GetTextAsync(ct);

		var result = await new SolutionUpdater().UpdateAsync(solution, Edits([solution.GetDocument(documentId)!]), ct);

		result.Solution.Should().BeSameAs(solution, "byte-identical content must not fork the snapshot (spec 055 R2)");
		result.UpToDateChanges.EditedDocuments.Should().ContainSingle()
			.Which.Id.Should().Be(documentId);
	}

	[TestMethod]
	[Description("Spec 055 R2: genuinely different on-disk content still forks the solution and applies the new text.")]
	public async Task When_DiskContentDiffers_Then_TextIsApplied()
	{
		var ct = TestContext.CancellationTokenSource.Token;
		using var temp = new TempDirectory();
		var path = await temp.WriteFileAsync("Model.cs", "class Model { int A; }", ct);

		using var workspace = new AdhocWorkspace();
		var projectId = ProjectId.CreateNewId();
		var documentId = DocumentId.CreateNewId(projectId);
		var solution = workspace.CurrentSolution
			.AddProject(projectId, "TestProject", "TestProject", LanguageNames.CSharp)
			.AddDocument(documentId, "Model.cs", SourceText.From("class Model { }"), filePath: path);

		// Realize the text so the update exercises the content comparison, not the unrealized-text bypass.
		_ = await solution.GetDocument(documentId)!.GetTextAsync(ct);

		var result = await new SolutionUpdater().UpdateAsync(solution, Edits([solution.GetDocument(documentId)!]), ct);

		result.Solution.Should().NotBeSameAs(solution);
		(await result.Solution.GetDocument(documentId)!.GetTextAsync(ct)).ToString().Should().Be("class Model { int A; }");
		result.UpToDateChanges.GetAllPaths().Should().BeEmpty("an applied edit is not up to date");
	}

	[TestMethod]
	[Description(
		"Spec 055 R2: the up-to-date skip applies to additional documents too — a XAML additional " +
		"document re-observed with identical bytes is the same no-op.")]
	public async Task When_AdditionalDocumentContentIsIdentical_Then_OriginalSolutionInstanceReturned()
	{
		var ct = TestContext.CancellationTokenSource.Token;
		using var temp = new TempDirectory();
		var path = await temp.WriteFileAsync("Page.xaml", "<Page />", ct);

		using var workspace = new AdhocWorkspace();
		var projectId = ProjectId.CreateNewId();
		var documentId = DocumentId.CreateNewId(projectId);
		var solution = workspace.CurrentSolution
			.AddProject(projectId, "TestProject", "TestProject", LanguageNames.CSharp)
			.AddAdditionalDocument(documentId, "Page.xaml", SourceText.From("<Page />"), filePath: path);

		// Realize the text, as the initial compilation does on the real workspace.
		_ = await solution.GetAdditionalDocument(documentId)!.GetTextAsync(ct);

		var result = await new SolutionUpdater().UpdateAsync(
			solution,
			Edits([], [solution.GetAdditionalDocument(documentId)!]),
			ct);

		result.Solution.Should().BeSameAs(solution, "byte-identical additional-document content must not fork the snapshot (spec 055 R2)");
		result.UpToDateChanges.EditedAdditionalDocuments.Should().ContainSingle()
			.Which.Id.Should().Be(documentId);
	}

	[TestMethod]
	[Description(
		"Spec 055 R2: a mixed batch (one file already up to date, one genuinely changed) forks the " +
		"solution but only re-applies the changed document; the identical one is reported up to date.")]
	public async Task When_MixedBatch_Then_OnlyChangedDocumentIsApplied()
	{
		var ct = TestContext.CancellationTokenSource.Token;
		using var temp = new TempDirectory();
		var samePath = await temp.WriteFileAsync("Same.cs", "class Same { }", ct);
		var changedPath = await temp.WriteFileAsync("Changed.cs", "class Changed { int A; }", ct);

		using var workspace = new AdhocWorkspace();
		var projectId = ProjectId.CreateNewId();
		var sameId = DocumentId.CreateNewId(projectId);
		var changedId = DocumentId.CreateNewId(projectId);
		var solution = workspace.CurrentSolution
			.AddProject(projectId, "TestProject", "TestProject", LanguageNames.CSharp)
			.AddDocument(sameId, "Same.cs", SourceText.From("class Same { }"), filePath: samePath)
			.AddDocument(changedId, "Changed.cs", SourceText.From("class Changed { }"), filePath: changedPath);

		// Realize the texts, as the initial compilation does on the real workspace.
		_ = await solution.GetDocument(sameId)!.GetTextAsync(ct);
		_ = await solution.GetDocument(changedId)!.GetTextAsync(ct);

		var result = await new SolutionUpdater().UpdateAsync(
			solution,
			Edits([solution.GetDocument(sameId)!, solution.GetDocument(changedId)!]),
			ct);

		result.Solution.Should().NotBeSameAs(solution, "the batch contains a real change");
		(await result.Solution.GetDocument(changedId)!.GetTextAsync(ct)).ToString().Should().Be("class Changed { int A; }");
		(await result.Solution.GetDocument(sameId)!.GetTextAsync(ct))
			.Should().BeSameAs(await solution.GetDocument(sameId)!.GetTextAsync(ct), "the up-to-date document's state must be left untouched");
		result.UpToDateChanges.EditedDocuments.Should().ContainSingle()
			.Which.Id.Should().Be(sameId);
	}

	[TestMethod]
	[Description(
		"Spec 055 R2 boundary: the skip only compares realized texts. A document whose text was " +
		"never read into the snapshot cannot be a re-observation, so the edit is applied (never " +
		"swallow a first edit).")]
	public async Task When_DocumentTextNotRealized_Then_EditIsApplied()
	{
		var ct = TestContext.CancellationTokenSource.Token;
		using var temp = new TempDirectory();
		var path = await temp.WriteFileAsync("Model.cs", "class Model { }", ct);

		using var workspace = new AdhocWorkspace();
		var projectId = ProjectId.CreateNewId();
		var documentId = DocumentId.CreateNewId(projectId);
		var solution = workspace.CurrentSolution
			.AddProject(projectId, "TestProject", "TestProject", LanguageNames.CSharp)
			.AddDocument(DocumentInfo.Create(
				documentId,
				"Model.cs",
				loader: TextLoader.From(TextAndVersion.Create(SourceText.From("class Model { }"), VersionStamp.Create(), path)),
				filePath: path));

		// Note: the document's text is deliberately NOT realized (no GetTextAsync before the update).
		var result = await new SolutionUpdater().UpdateAsync(solution, Edits([solution.GetDocument(documentId)!]), ct);

		result.Solution.Should().NotBeSameAs(solution, "an unrealized text cannot be proven up to date, so the edit must be applied");
		result.UpToDateChanges.GetAllPaths().Should().BeEmpty();
	}

	private static ChangeSet Edits(ImmutableArray<Document> documents, ImmutableArray<TextDocument> additionalDocuments = default)
		=> ChangeSet.Empty with
		{
			EditedDocuments = documents,
			EditedAdditionalDocuments = additionalDocuments.IsDefault ? [] : additionalDocuments,
		};
}
