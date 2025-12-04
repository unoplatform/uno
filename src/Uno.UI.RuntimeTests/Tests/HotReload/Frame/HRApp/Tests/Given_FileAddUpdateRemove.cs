using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
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
		var sut = CreatePageType();

		Assert.IsNull(Type.GetType(sut), "Type should not exists yet");

		/* await using ==> Causes roslyn crash */ var update = await _HR.UpdateAsync([CreateBlankPageXaml(sut), CreateBlankPageCodeBehind(sut)], ct);

		Assert.IsNotNull(Type.GetType(sut), "Page should have been compiled and made available to the application");
	}

	private static string CreatePageType([CallerMemberName] string @class = "TestPage")
		=> $"Uno.UI.RuntimeTests.{@class}_{Guid.NewGuid():N}";

	private static FileEdit CreateBlankPageXaml(string type, string? dir = null)
		=> new (
			Path.Combine(dir ?? _appDir, $"{type}.xaml"),
			OldText: null,
			NewText: $$"""
				<Page
					x:Class="{{type}}"
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

	private static FileEdit CreateBlankPageCodeBehind(string type, string? dir = null)
		=> new(
			Path.Combine(dir ?? _appDir, $"{type}.xaml.cs"),
			OldText: null,
			NewText: $$"""
				using System;
				using System.Linq;
				using Microsoft.UI.Xaml.Controls;

				namespace {{type[..type.LastIndexOf('.')]}};

				public sealed partial class {{type[(type.LastIndexOf('.') + 1) ..]}} : Page
				{
					public {{type[(type.LastIndexOf('.') + 1) ..]}}()
					{
						this.InitializeComponent();
					}
				}
				""",
			IsCreateDeleteAllowed: true);
}
