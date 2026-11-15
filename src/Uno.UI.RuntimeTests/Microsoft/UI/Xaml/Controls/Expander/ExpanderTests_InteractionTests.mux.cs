// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference controls/dev/Expander/InteractionTests/ExpanderTests.cs, commit fee1e8acc

using System;
using Common;
using Microsoft.UI.Xaml.Tests.MUXControls.InteractionTests.Infra;
using Microsoft.UI.Xaml.Tests.MUXControls.InteractionTests.Common;
using System.Collections.Generic;
using System.Threading.Tasks;
using Uno.UI.RuntimeTests;
using Windows.Foundation.Metadata;

using Microsoft.Windows.Apps.Test.Automation;
using Microsoft.Windows.Apps.Test.Foundation;
using Microsoft.Windows.Apps.Test.Foundation.Controls;
using Microsoft.Windows.Apps.Test.Foundation.Patterns;
using Microsoft.Windows.Apps.Test.Foundation.Waiters;
using MUXTestInfra.Shared.Infra;

// Upstream TAEF conventions, kept for line-by-line fidelity.
#pragma warning disable MSTEST0007 // [TestProperty] on [ClassInitialize]
#pragma warning disable MSTEST0029 // public TestCleanup() without [TestCleanup]

namespace Microsoft.UI.Xaml.Tests.MUXControls.InteractionTests
{
	public class Expander : UIObject, IExpandCollapse
	{
		public Expander(UIObject uiObject)
			: base(uiObject)
		{
			this.Initialize();
		}

		private void Initialize()
		{
			_expandCollapsePattern = new ExpandCollapseImplementation(this);
		}

		public async Task ExpandAndWait()
		{
			using (var waiter = GetExpandedWaiter())
			{
				Expand();
				await waiter.Wait();
			}

			await Wait.ForIdle();
		}

		public async Task CollapseAndWait()
		{
			using (var waiter = GetCollapsedWaiter())
			{
				Collapse();
				await waiter.Wait();
			}

			await Wait.ForIdle();
		}

		public void Expand()
		{
			_expandCollapsePattern.Expand();
		}

		public void Collapse()
		{
			_expandCollapsePattern.Collapse();
		}

		public UIEventWaiter GetExpandedWaiter()
		{
			return _expandCollapsePattern.GetExpandedWaiter();
		}

		public UIEventWaiter GetCollapsedWaiter()
		{
			return _expandCollapsePattern.GetCollapsedWaiter();
		}

		public ExpandCollapseState ExpandCollapseState
		{
			get { return _expandCollapsePattern.ExpandCollapseState; }
		}

		new public static IFactory<Expander> Factory
		{
			get
			{
				if (null == Expander._factory)
				{
					Expander._factory = new ExpanderFactory();
				}
				return Expander._factory;
			}
		}

		private IExpandCollapse _expandCollapsePattern;
		private static IFactory<Expander> _factory = null;
		private class ExpanderFactory : IFactory<Expander>
		{
			public Expander Create(UIObject element)
			{
				return new Expander(element);
			}
		}
	}

	[TestClass]
	[RunsOnUIThread]
	public class ExpanderTests
	{
		[ClassInitialize]
		[TestProperty("RunAs", "User")]
		[TestProperty("Classification", "Integration")]
		[TestProperty("Platform", "Any")]
		[TestProperty("MUXControlsTestSuite", "SuiteB")]
		public static void ClassInitialize(TestContext testContext)
		{
			TestEnvironment.Initialize(testContext);
		}

		public void TestCleanup()
		{
			TestCleanupHelper.Cleanup();
		}

		[TestMethod]
		public async Task VerifyAxeScanPasses()
		{
			await using (var setup = await TestSetupHelper.Create("Expander-Axe"))
			{
				AxeTestHelper.TestForAxeIssues();
			}
		}

		[TestMethod]
		public async Task ExpandCollapseAutomationTests()
		{
			await using (var setup = await TestSetupHelper.Create("Expander Tests"))
			{
				Expander expander = FindElement.ByName<Expander>("ExpandedExpander");
				expander.SetFocus();
				await Wait.ForIdle();

				Log.Comment("Collapse using keyboard space key.");
				await KeyboardHelper.PressKey(Key.Space);
				Verify.AreEqual(expander.ExpandCollapseState, ExpandCollapseState.Collapsed);

				Log.Comment("Expand using keyboard space key.");
				await KeyboardHelper.PressKey(Key.Space);
				Verify.AreEqual(expander.ExpandCollapseState, ExpandCollapseState.Expanded);

				Log.Comment("Collapse by clicking.");
				expander.Click();
				Verify.AreEqual(expander.ExpandCollapseState, ExpandCollapseState.Collapsed);

				Log.Comment("Expand by clicking.");
				expander.Click();
				Verify.AreEqual(expander.ExpandCollapseState, ExpandCollapseState.Expanded);

				Log.Comment("Collapse using UIA ExpandCollapse pattern");
				await expander.CollapseAndWait();
				Verify.AreEqual(expander.ExpandCollapseState, ExpandCollapseState.Collapsed);

				Log.Comment("Expand using UIA ExpandCollapse pattern");
				await expander.ExpandAndWait();
				Verify.AreEqual(expander.ExpandCollapseState, ExpandCollapseState.Expanded);
			}
		}

		[TestMethod]
		public async Task AutomationPeerTest()
		{
			await using (var setup = await TestSetupHelper.Create("Expander Tests"))
			{
				// An control to take initial focus:
				Button dummyButton = FindElement.ByName<Button>("DummyButton");
				dummyButton.SetFocus();
				await Wait.ForIdle();

				Expander expander = FindElement.ByName<Expander>("ExpanderWithToggleSwitch");
				Verify.IsTrue(expander.IsKeyboardFocusable, "Expander should be Keyboard Focusable");
				Verify.IsFalse(expander.HasKeyboardFocus, "Expander should initially be unfocused");

				expander.SetFocus();
				await Wait.ForIdle();

				Verify.IsTrue(expander.HasKeyboardFocus, "Expander should be focused");

				// Verify ExpandedExpander header content AutomationProperties.Name properties are set
				VerifyElement.Found("This expander with ToggleSwitch is expanded by default.", FindBy.Name);
				VerifyElement.Found("This is the second line of text.", FindBy.Name);
				VerifyElement.Found("SettingsToggleSwitch", FindBy.Name);

				// Verify ExpandedExpander content AutomationProperties.Name property is set
				VerifyElement.Found("ExpanderWithToggleSwitch Content", FindBy.Name);

				Log.Comment("Collapse using keyboard space key.");
				await KeyboardHelper.PressKey(Key.Space);
				Verify.AreEqual(expander.ExpandCollapseState, ExpandCollapseState.Collapsed);

				// Verify ExpandedExpander content AutomationProperties.Name property is not visible once collapsed
				VerifyElement.NotFound("ExpanderWithToggleSwitch Content", FindBy.Name);
			}
		}
	}
}
