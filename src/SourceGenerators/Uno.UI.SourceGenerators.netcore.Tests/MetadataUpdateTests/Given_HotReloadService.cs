using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions;
using Uno.UI.RemoteControl.Host.HotReload.MetadataUpdates;
using Uno.UI.SourceGenerators.MetadataUpdates;

namespace Uno.UI.SourceGenerators.Tests.MetadataUpdateTests;

[TestClass]
public class Given_HotReloadService
{
	[TestMethod]
	public async Task When_Empty()
	{
		try
		{
			var results = await ApplyScenario(isDebugCompilation: true);

			Assert.Fail();
		}
		catch (Exception e)
		{
			Assert.IsTrue(e.Message.Contains("CS5001"));
		}
	}

	[TestMethod]
	public async Task When_Single_Code_File_With_No_Update()
	{
		var results = await ApplyScenario(isDebugCompilation: true);
		
		Assert.AreEqual(0, results[0].Diagnostics.Length);
		Assert.AreEqual(0, results[0].MetadataUpdates.Length);
	}

	[TestMethod]
	public async Task When_Single_Code_File_With_Code_Update()
	{
		var results = await ApplyScenario(isDebugCompilation: true);

		Assert.AreEqual(0, results[0].Diagnostics.Length);
		Assert.AreEqual(1, results[0].MetadataUpdates.Length);
	}

	[TestMethod]
	public async Task When_Simple_Xaml_No_Update()
	{
		var results = await ApplyScenario(isDebugCompilation: true);

		Assert.AreEqual(0, results[0].Diagnostics.Length);
		Assert.AreEqual(0, results[0].MetadataUpdates.Length);
	}

	[TestMethod]
	public async Task When_Simple_Xaml_Single_Text_Change()
	{
		var results = await ApplyScenario(isDebugCompilation: true);

		Assert.AreEqual(0, results[0].Diagnostics.Length);
		Assert.AreEqual(1, results[0].MetadataUpdates.Length);
	}

	[TestMethod]
	public async Task When_Simple_Xaml_Single_xName_Add()
	{
		var results = await ApplyScenario(isDebugCompilation: true);

		Assert.AreEqual(0, results[0].Diagnostics.Length);
		Assert.AreEqual(1, results[0].MetadataUpdates.Length);
	}

	[TestMethod]
	public async Task When_Simple_Xaml_Single_xName_Add_Twice_Release()
	{
		var results = await ApplyScenario(isDebugCompilation: false);

		Assert.AreEqual(2, results.Length);
		Assert.AreEqual(0, results[0].Diagnostics.Length);
		Assert.AreEqual(1, results[0].MetadataUpdates.Length);
		Assert.AreEqual(4, results[1].Diagnostics.Length);
		Assert.IsTrue(results[1].Diagnostics.First().GetMessage().Contains("ENC0049"));
	}

	[TestMethod]
	public async Task When_Simple_Xaml_Single_xName_Add_Twice_Debug()
	{
		var results = await ApplyScenario(isDebugCompilation: true);

		Assert.AreEqual(2, results.Length);
		Assert.AreEqual(0, results[0].Diagnostics.Length);
		Assert.AreEqual(1, results[0].MetadataUpdates.Length);
		Assert.AreEqual(0, results[1].Diagnostics.Length);
		Assert.AreEqual(1, results[1].MetadataUpdates.Length);
	}

	[TestMethod]
	public async Task When_Simple_Xaml_xBind_Event_Add()
	{
		var results = await ApplyScenario(isDebugCompilation: true);

		Assert.AreEqual(0, results[0].Diagnostics.Length);
		Assert.AreEqual(1, results[0].MetadataUpdates.Length);
	}

	[TestMethod]
	public async Task When_Simple_Xaml_Add_xBind_Simple_Property()
	{
		var results = await ApplyScenario(isDebugCompilation: true);

		Assert.AreEqual(0, results[0].Diagnostics.Length);
		Assert.AreEqual(1, results[0].MetadataUpdates.Length);
	}
	
	[TestMethod]
	public async Task When_Simple_Xaml_Add_xLoad()
	{
		var results = await ApplyScenario(isDebugCompilation: true);

		Assert.AreEqual(0, results[0].Diagnostics.Length);
		Assert.AreEqual(1, results[0].MetadataUpdates.Length);
	}

	private async Task<HotReloadWorkspace.UpdateResult[]> ApplyScenario(bool isDebugCompilation, [CallerMemberName] string? name = null)
	{
		if(name is null)
		{
			throw new InvalidOperationException($"A test scenario name must be provided.");
		}

		name = name
			.Replace("_Debug", "")
			.Replace("_Release", "");

		var scenarioFolder = Path.Combine(
			Path.GetDirectoryName(typeof(HotReloadWorkspace).Assembly.Location)!,
			"MetadataUpdateTests",
			"Scenarios",
			name);
		
		HotReloadWorkspace SUT = new(isDebugCompilation);
		List<HotReloadWorkspace.UpdateResult> results = new();

		var steps = Directory
			.GetFiles(scenarioFolder, "*.*", SearchOption.AllDirectories)
			.OrderBy(f => f)
			.GroupBy(f => Path.GetRelativePath(scenarioFolder, f).Split(Path.DirectorySeparatorChar)[0]);

		int index = 0;
		foreach (var step in steps)
		{
			foreach (var file in step)
			{
				var pathParts = Path.GetRelativePath(scenarioFolder, file).Split(Path.DirectorySeparatorChar);
				
				var fileContent = File.ReadAllText(file);

				if (Path.GetExtension(file) == ".cs")
				{
					SUT.SetSourceFile(pathParts[1], pathParts[2], fileContent);
				}
				else
				{
					SUT.SetAdditionalFile(pathParts[1], pathParts[2], fileContent);
				}
			}

			if(index++ == 0)
			{
				await SUT.Initialize(CancellationToken.None);
			}
			else
			{
				results.Add(await SUT.Update());
			}
		}
		
		return results.ToArray();
	}
}
