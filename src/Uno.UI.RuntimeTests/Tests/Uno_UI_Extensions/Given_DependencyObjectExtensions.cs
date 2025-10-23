﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.Extensions;
using DependencyObjectExtensions = Uno.UI.Extensions.DependencyObjectExtensions;

namespace Uno.UI.RuntimeTests.Tests.Uno_UI_Extensions
{
	[TestClass]
#if RUNTIME_NATIVE_AOT
	[Ignore(".BeEquivalentTo() unsupported under NativeAOT; see: https://github.com/AwesomeAssertions/AwesomeAssertions/issues/290")]
#endif  // RUNTIME_NATIVE_AOT
	public class Given_DependencyObjectExtensions
	{
		[TestMethod]
		[RunsOnUIThread]
		public Task When_GetAllChildren_NoMax_Current_Branch()
			=> TestGetAllChildren(
				childLevelLimit: null,
				includeCurrent: true,
				mode: TreeEnumerationMode.Branch,
				"elt 1",
				"elt 1.1",
				"elt 1.1.1",
				"elt 1.1.1.1",
				"elt 1.1.1.2",
				"elt 1.1.1.2.1",
				"elt 1.1.1.2.2",
				"elt 1.1.2",
				"elt 1.1.2.1",
				"elt 1.1.2.2"
			);

		[TestMethod]
		[RunsOnUIThread]
		public Task When_GetAllChildren_NoMax_Current_Layer()
			=> TestGetAllChildren(
				childLevelLimit: null,
				includeCurrent: true,
				mode: TreeEnumerationMode.Layer,
				"elt 1",
				"elt 1.1",
				"elt 1.1.1",
				"elt 1.1.2",
				"elt 1.1.1.1",
				"elt 1.1.1.2",
				"elt 1.1.2.1",
				"elt 1.1.2.2",
				"elt 1.1.1.2.1",
				"elt 1.1.1.2.2"
			);

		[TestMethod]
		[RunsOnUIThread]
		public Task When_GetAllChildren_NoMax_NoCurrent_Branch()
			=> TestGetAllChildren(
				childLevelLimit: null,
				includeCurrent: false,
				mode: TreeEnumerationMode.Branch,
				"elt 1.1",
				"elt 1.1.1",
				"elt 1.1.1.1",
				"elt 1.1.1.2",
				"elt 1.1.1.2.1",
				"elt 1.1.1.2.2",
				"elt 1.1.2",
				"elt 1.1.2.1",
				"elt 1.1.2.2"
			);

		[TestMethod]
		[RunsOnUIThread]
		public Task When_GetAllChildren_NoMax_NoCurrent_Layer()
			=> TestGetAllChildren(
				childLevelLimit: null,
				includeCurrent: false,
				mode: TreeEnumerationMode.Layer,
				"elt 1.1",
				"elt 1.1.1",
				"elt 1.1.2",
				"elt 1.1.1.1",
				"elt 1.1.1.2",
				"elt 1.1.2.1",
				"elt 1.1.2.2",
				"elt 1.1.1.2.1",
				"elt 1.1.1.2.2"
			);

		[TestMethod]
		[RunsOnUIThread]
		public Task When_GetAllChildren_Max3_Current_Branch()
			=> TestGetAllChildren(
				childLevelLimit: 3,
				includeCurrent: true,
				mode: TreeEnumerationMode.Branch,
				"elt 1",
				"elt 1.1",
				"elt 1.1.1",
				"elt 1.1.1.1",
				"elt 1.1.1.2",
				"elt 1.1.2",
				"elt 1.1.2.1",
				"elt 1.1.2.2"
			);

		[TestMethod]
		[RunsOnUIThread]
		public Task When_GetAllChildren_Max3_Current_Layer()
			=> TestGetAllChildren(
				childLevelLimit: 3,
				includeCurrent: true,
				mode: TreeEnumerationMode.Layer,
				"elt 1",
				"elt 1.1",
				"elt 1.1.1",
				"elt 1.1.2",
				"elt 1.1.1.1",
				"elt 1.1.1.2",
				"elt 1.1.2.1",
				"elt 1.1.2.2"
			);

		[TestMethod]
		[RunsOnUIThread]
		public Task When_GetAllChildren_Max3_NoCurrent_Branch()
			=> TestGetAllChildren(
				childLevelLimit: 3,
				includeCurrent: false,
				mode: TreeEnumerationMode.Branch,
				"elt 1.1",
				"elt 1.1.1",
				"elt 1.1.1.1",
				"elt 1.1.1.2",
				"elt 1.1.2",
				"elt 1.1.2.1",
				"elt 1.1.2.2"
			);

		[TestMethod]
		[RunsOnUIThread]
		public Task When_GetAllChildren_Max3_NoCurrent_Layer()
			=> TestGetAllChildren(
				childLevelLimit: 3,
				includeCurrent: false,
				mode: TreeEnumerationMode.Layer,
				"elt 1.1",
				"elt 1.1.1",
				"elt 1.1.2",
				"elt 1.1.1.1",
				"elt 1.1.1.2",
				"elt 1.1.2.1",
				"elt 1.1.2.2"
			);

		[TestMethod]
		[RunsOnUIThread]
		public Task When_GetAllChildren_Max0_Current_Branch()
			=> TestGetAllChildren(
				childLevelLimit: 0,
				includeCurrent: true,
				mode: TreeEnumerationMode.Branch,
				"elt 1"
			);

		[TestMethod]
		[RunsOnUIThread]
		public Task When_GetAllChildren_Max0_Current_Layer()
			=> TestGetAllChildren(
				childLevelLimit: 0,
				includeCurrent: true,
				mode: TreeEnumerationMode.Layer,
				"elt 1"
			);

		[TestMethod]
		[RunsOnUIThread]
		public Task When_GetAllChildren_Max0_NoCurrent_Branch()
			=> TestGetAllChildren(
				childLevelLimit: 0,
				includeCurrent: false,
				mode: TreeEnumerationMode.Branch
			);

		[TestMethod]
		[RunsOnUIThread]
		public Task When_GetAllChildren_Max0_NoCurrent_Layer()
			=> TestGetAllChildren(
				childLevelLimit: 0,
				includeCurrent: false,
				mode: TreeEnumerationMode.Layer
			);

		private async Task TestGetAllChildren(
			int? childLevelLimit,
			bool includeCurrent,
			TreeEnumerationMode mode,
			params string[] expected)
		{
			var tree = BuildTestTree();

			TestServices.WindowHelper.WindowContent = tree;
			await TestServices.WindowHelper.WaitForIdle();

			Validate(expected, tree.GetAllChildren(childLevelLimit, includeCurrent, mode));
		}

		private UIElement BuildTestTree()
		{
			return new Border
			{
				Name = "elt 1",
				Child = new StackPanel
				{
					Name = "elt 1.1",
					Children =
					{
						new StackPanel
						{
							Name = "elt 1.1.1",
							Children =
							{
								new Border {Name = "elt 1.1.1.1"},
								new StackPanel
								{
									Name = "elt 1.1.1.2",
									Children =
									{
										new Border {Name = "elt 1.1.1.2.1"},
										new Border {Name = "elt 1.1.1.2.2"}
									}
								},
							}
						},
						new StackPanel
						{
							Name = "elt 1.1.2",
							Children =
							{
								new Border {Name = "elt 1.1.2.1"},
								new Border {Name = "elt 1.1.2.2"}
							}
						}
					}
				}
			};
		}

		private void Validate(string[] expected, IEnumerable<DependencyObject> actual)
		{
			var tree = actual.Cast<FrameworkElement>().ToArray();

			Console.WriteLine("Tree: " + Dump(tree));

			tree.Select(elt => elt.Name).Should().BeEquivalentTo(expected);
		}

		private string Dump(IEnumerable<FrameworkElement> tree)
			=> tree.Aggregate(string.Empty, (r, elt) => $"{r}\r\n{elt.Name}");
	}
}
