using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RemoteControl;
using Uno.UI.RemoteControl.HotReload;
using Uno.UI.RuntimeTests.HRApp.Skia.Tests.Pages;
using Uno.UI.RuntimeTests.HRUnoLib;
using _HR = Uno.UI.RuntimeTests.Tests.HotReload.HotReloadHelper;

namespace Uno.UI.RuntimeTests.HRApp.Skia.Tests;

[TestClass]
[RunsOnUIThread]
public class Given_FileAddUpdateRemove
{
	private static readonly string _appDir = Path.GetDirectoryName(Uno.UI.RuntimeTests.Tests.HotReload.FrameworkElementExtensions.GetDebugParseContext(new BlankAppPage1()).FileName)!;
	private static readonly string _libDir = Path.GetDirectoryName(Uno.UI.RuntimeTests.Tests.HotReload.FrameworkElementExtensions.GetDebugParseContext(new BlankLibPage1()).FileName)!;

	[TestMethod]
	public async Task When_AddInApp()
	{
		var ct = new CancellationTokenSource(TimeSpan.FromSeconds(300)).Token;
		var sut = CreateType();

		Assert.IsNull(sut.Get(), "Type should not exists yet");

		/* await using ==> Causes roslyn crash */ var update = await _HR.UpdateAsync([AddXaml(sut), AddCodeBehind(sut)], ct);

		Assert.IsNotNull(sut.Get(), "Page should have been compiled and made available to the application");
	}

	[TestMethod]
	public async Task When_AddInLib()
	{
		var ct = new CancellationTokenSource(TimeSpan.FromSeconds(300)).Token;
		var sut = CreateLibType();

		Assert.IsNull(sut.Get(), "Type should not exists yet");

		/* await using ==> Causes roslyn crash */ var update = await _HR.UpdateAsync([AddXaml(sut), AddCodeBehind(sut)], ct);

		Assert.IsNotNull(sut.Get(), "Page should have been compiled and made available to the library");
	}

	[TestMethod]
	public async Task When_RemoveInApp()
	{
		var ct = new CancellationTokenSource(TimeSpan.FromSeconds(300)).Token;
		var sut = GetType<BlankAppPage2>();

		await using var update = await _HR.UpdateAsync([DeleteXaml(sut), DeleteCodeBehind(sut)], ct);

		Assert.IsNull(sut.CreateInstance(), "BlankAppPage2 should have been removed");
	}

	[TestMethod]
	public async Task When_RemoveInLib()
	{
		var ct = new CancellationTokenSource(TimeSpan.FromSeconds(300)).Token;
		var sut = GetType<BlankLibPage2>();

		await using var update = await _HR.UpdateAsync([DeleteXaml(sut), DeleteCodeBehind(sut)], ct);

		Assert.IsNull(sut.CreateInstance(), "BlankLibPage2 should have been removed");
	}

	[TestMethod]
	public async Task When_AddAndEditInApp()
	{
		var ct = new CancellationTokenSource(TimeSpan.FromSeconds(300)).Token;
		var sut = CreateType();

		Assert.IsNull(sut.Get(), "Type should not exists yet");

		// Add the page
		await using var add = await _HR.UpdateAsync([AddXaml(sut), AddCodeBehind(sut)], ct);

		Assert.IsNotNull(sut.Get(), "Page should have been compiled and made available");

		// Edit the page
		_ = await _HR.UpdateAsync([EditXaml(sut, sut.Name, $"{sut.Name}_EDITED")], ct);

		// Verify type still exists after edit
		// TODO: Instance the type and validate teh content!
		Assert.IsNotNull(sut.Get(), "Page should still exist after edit");
	}

	[TestMethod]
	public async Task When_AddAndEditInLib()
	{
		var ct = new CancellationTokenSource(TimeSpan.FromSeconds(300)).Token;
		var sut = CreateLibType();

		Assert.IsNull(sut.Get(), "Type should not exists yet");

		// Add the page
		await using var add = await _HR.UpdateAsync([AddXaml(sut), AddCodeBehind(sut)], ct);

		Assert.IsNotNull(sut.Get(), "Page should have been compiled and made available");

		// Edit the page
		_ = await _HR.UpdateAsync([EditXaml(sut, sut.Name, $"{sut.Name}_EDITED")], ct);

		// Verify type still exists after edit
		// TODO: Instance the type and validate teh content!
		Assert.IsNotNull(sut.Get(), "Page should still exist after edit");
	}

	[TestMethod]
	public async Task When_RemoveAddBackAndEditInApp()
	{
		var ct = new CancellationTokenSource(TimeSpan.FromSeconds(300)).Token;
		var sut = GetType<BlankAppPage2>();

		// Remove the page
		await using (var delete = await _HR.UpdateAsync([DeleteXaml(sut), DeleteCodeBehind(sut)], ct))
		{
			Assert.IsNull(sut.CreateInstance(), "BlankAppPage2 should have been removed");
		} // Add it back

		Assert.IsNotNull(sut.CreateInstance(), "BlankAppPage2 should have been added back");

		// Edit it
		await using var edit = await _HR.UpdateAsync([EditXaml(sut)], ct);

		// TODO: Instance the type and validate teh content!
		Assert.IsNotNull(sut.Get(), "BlankAppPage2 should still exist after edit");
	}

	[TestMethod]
	public async Task When_RemoveAddBackAndEditInLib()
	{
		var ct = new CancellationTokenSource(TimeSpan.FromSeconds(300)).Token;
		var sut = GetType<BlankLibPage2>();

		// Remove the page
		await using (var delete = await _HR.UpdateAsync([DeleteXaml(sut), DeleteCodeBehind(sut)], ct))
		{
			Assert.IsNull(sut.CreateInstance(), "BlankLibPage2 should have been removed");
		} // Add it back

		Assert.IsNotNull(sut.CreateInstance(), "BlankLibPage2 should have been added back");

		// Edit it
		await using var edit = await _HR.UpdateAsync([EditXaml(sut)], ct);

		// TODO: Instance the type and validate teh content!
		Assert.IsNotNull(sut.Get(), "BlankLibPage2 should still exist after edit");
	}

	[TestMethod]
	public async Task When_AddMultiplePagesInApp()
	{
		var ct = new CancellationTokenSource(TimeSpan.FromSeconds(300)).Token;
		var sut1 = CreateType();
		var sut2 = CreateType();

		Assert.IsNull(sut1.Get(), "First type should not exist yet");
		Assert.IsNull(sut2.Get(), "Second type should not exist yet");

		/* await using ==> Causes roslyn crash */ var update = await _HR.UpdateAsync([
			AddXaml(sut1),
			AddCodeBehind(sut1),
			AddXaml(sut2),
			AddCodeBehind(sut2)
		], ct);

		Assert.IsNotNull(sut1.Get(), "First page should have been compiled");
		Assert.IsNotNull(sut2.Get(), "Second page should have been compiled");
	}

	[TestMethod]
	public async Task When_AddAndRemoveInSameUpdate()
	{
		var ct = new CancellationTokenSource(TimeSpan.FromSeconds(300)).Token;
		var added = CreateType();
		var removed = GetType<BlankAppPage2>();

		Assert.IsNull(added.Get(), "New type should not exist yet");

		await using var update = await _HR.UpdateAsync([
			// Add new page
			AddXaml(added),
			AddCodeBehind(added),
			// Remove existing page
			DeleteXaml<BlankAppPage2>(),
			DeleteCodeBehind<BlankAppPage2>()
		], ct);

		Assert.IsNotNull(added.Get(), "New page should have been compiled");
		Assert.IsNull(removed.CreateInstance(), "BlankAppPage2 should have been removed");
	}

	private record struct DynamicType(string Namespace, string Name, string Assembly, string Directory)
	{
		public string FilePath => Path.Combine(Directory, Name);

		public Type? Get() => Type.GetType($"{Namespace}.{Name},{Assembly}");

		public object? CreateInstance()
		{
			var type = Get();
			if (type is null)
			{
				return null;
			}
			return Activator.CreateInstance(type);
		}
	}

	private static DynamicType CreateType([CallerMemberName] string @class = "TestPage")
	{
		var name = $"{@class}_{Guid.NewGuid():N}";
		return new DynamicType("Uno.UI.RuntimeTests", name, "Uno.UI.RuntimeTests.HRApp.Skia", _appDir);
	}

	private static DynamicType CreateLibType([CallerMemberName] string @class = "TestPage")
	{
		var name = $"{@class}_{Guid.NewGuid():N}";
		return new DynamicType("Uno.UI.RuntimeTests", name, "Uno.UI.RuntimeTests.HRUnoLib", _libDir);
	}

	private static DynamicType GetType<TPage>()
		where TPage : FrameworkElement, new()
		=> new(
			typeof(TPage).Namespace ?? throw new ArgumentOutOfRangeException(nameof(TPage)),
			typeof(TPage).Name,
			typeof(TPage).Assembly.GetName().Name ?? throw new ArgumentOutOfRangeException(nameof(TPage)),
			Uno.UI.RuntimeTests.Tests.HotReload.FrameworkElementExtensions.GetDebugParseContext(new TPage()).FileName[..^".xaml".Length]);

	private static FileEdit EditXaml<TPage>(string oldText, string newText)
		where TPage : FrameworkElement, new()
		=> EditXaml(GetType<TPage>(), oldText, newText);
	private static FileEdit EditXaml(DynamicType type)
		=> EditXaml(type, type.Name, $"{type.Name}_EDITED");
	private static FileEdit EditXaml(DynamicType type, string oldText, string newText)
		=> new(type.FilePath, oldText, newText, IsCreateDeleteAllowed: false);

	private static FileEdit DeleteXaml<TPage>()
		where TPage : FrameworkElement, new()
		=> DeleteXaml(GetType<TPage>());
	private static FileEdit DeleteXaml(DynamicType sut)
		=> new(
			sut.FilePath + ".xaml",
			OldText: File.ReadAllText(sut.FilePath + ".xaml.cs"), // We capture the OldText to be able to restore it in undo.
			NewText: null,
			IsCreateDeleteAllowed: true);

	private static FileEdit DeleteCodeBehind<TPage>()
		where TPage : FrameworkElement, new()
		=> DeleteCodeBehind(GetType<TPage>());
	private static FileEdit DeleteCodeBehind(DynamicType sut)
		=> new(
			sut.FilePath + ".xaml.cs",
			OldText: File.ReadAllText(sut.FilePath + ".xaml.cs"), // We capture the OldText to be able to restore it in undo.
			NewText: null,
			IsCreateDeleteAllowed: true);

	private static FileEdit AddXaml(DynamicType type)
		=> new (
			type.FilePath + ".xaml",
			OldText: null,
			NewText: $$"""
				<Page
					x:Class="{{type.Namespace}}.{{type.Name}}"
					xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
					xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
					xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
					mc:Ignorable="d"
					Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">

					<Grid>
						<TextBlock Text="This is a live test page {{type}}" />
					</Grid>
				</Page>
				""",
			IsCreateDeleteAllowed: true);

	private static FileEdit AddCodeBehind(DynamicType type)
		=> new(
			type.FilePath + ".xaml.cs",
			OldText: null,
			NewText: $$"""
				using System;
				using System.Linq;
				using Microsoft.UI.Xaml.Controls;

				namespace {{type.Namespace}};

				public sealed partial class {{type.Name}} : Page
				{
					public {{type.Name}}()
					{
						this.InitializeComponent();
					}
				}
				""",
			IsCreateDeleteAllowed: true);
}
