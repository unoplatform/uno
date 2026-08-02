using System;
using System.Collections;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

#if HAS_UNO
using static Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation.WasmSemanticDomHelper;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation
{
	[TestClass]
	public class Given_AccessibilityActivationRollback
	{
#if __SKIA__
		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Aom_Creation_Throws_Then_Activation_Rolls_Back_And_Can_Retry()
		{
			var scrollViewer = new ScrollViewer { Content = new TextBlock { Text = "Subscribed before failure" } };
			var throwingList = new ThrowingNameListView { ItemsSource = new[] { "Broken" } };
			var sibling = new Button { Content = "Retry target" };
			var panel = new StackPanel { Children = { scrollViewer, throwingList, sibling } };

			await UITestHelper.Load(panel);
			await UITestHelper.WaitForIdle();
			EnableAccessibilityThroughDom();

			await UITestHelper.WaitFor(
				() => InvokeBrowserJs("document.getElementById('uno-enable-accessibility') ? '1' : '0'") == "1",
				timeoutMS: 5000,
				message: "Failed accessibility activation did not restore the retry affordance.");
			Assert.AreEqual(
				"0",
				InvokeBrowserJs("document.getElementById('uno-semantics-root').childElementCount.toString()"),
				"Failed activation must remove every partially-created semantic node.");
			var accessibility = GetAccessibilityInstance();
			Assert.AreEqual(0, GetPrivateCollectionCount(accessibility, "_scrollViewerSubscriptions"), "Rollback must detach every ScrollViewer subscription created during AOM construction.");
			Assert.AreEqual(0, GetPrivateCollectionCount(accessibility, "_scrollPresenterSubscriptions"), "Rollback must detach every ScrollPresenter subscription created during AOM construction.");

			panel.Children.Remove(throwingList);
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(
				() => SemanticElementExists(sibling),
				timeoutMS: 5000,
				message: "Accessibility could not be enabled after the failing control was removed.");

			Assert.AreEqual("0", InvokeBrowserJs("document.getElementById('uno-enable-accessibility') ? '1' : '0'"));
			Assert.IsTrue(SemanticElementExists(sibling));
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Focus_Synchronizer_Uninitializes_Then_Tracked_Element_Is_Released()
		{
			var button = new Button { Content = "Tracked focus" };
			await UITestHelper.Load(button);

			var accessibility = GetAccessibilityInstance();
			var synchronizerType = accessibility.GetType().Assembly.GetType("Uno.UI.Runtime.Skia.FocusSynchronizer", throwOnError: true)!;
			var constructor = synchronizerType.GetConstructor(
				BindingFlags.Instance | BindingFlags.NonPublic,
				binder: null,
				new[] { accessibility.GetType() },
				modifiers: null);
			Assert.IsNotNull(constructor, "Unable to locate the FocusSynchronizer constructor.");
			var synchronizer = constructor!.Invoke(new[] { accessibility });
			var track = synchronizerType.GetMethod("TrackFocusedElement", BindingFlags.Instance | BindingFlags.NonPublic);
			var uninitialize = synchronizerType.GetMethod("Uninitialize", BindingFlags.Instance | BindingFlags.NonPublic);
			var trackedElement = synchronizerType.GetField("_trackedElement", BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.IsNotNull(track);
			Assert.IsNotNull(uninitialize);
			Assert.IsNotNull(trackedElement);

			track!.Invoke(synchronizer, new object[] { button });
			Assert.AreSame(button, trackedElement!.GetValue(synchronizer), "The fixture did not establish focused-element ownership.");
			uninitialize!.Invoke(synchronizer, parameters: null);
			Assert.IsNull(trackedElement.GetValue(synchronizer), "Uninitialize must release the tracked element and its event subscriptions.");
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Modal_Scope_Is_Active_Then_Background_Actions_Are_Rejected()
		{
			var background = new Button { Content = "Background" };
			var modalChild = new Button { Content = "Modal action" };
			var modal = new Border { Child = modalChild, Width = 180, Height = 80 };
			AutomationProperties.SetName(modal, "Modal");
			var panel = new StackPanel { Children = { background, modal } };

			await UITestHelper.Load(panel);
			await TestServices.WindowHelper.WaitForIdle();
			background.GetOrCreateAutomationPeer();
			modal.GetOrCreateAutomationPeer();
			modalChild.GetOrCreateAutomationPeer();
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(
				() => SemanticElementExists(background) && SemanticElementExists(modal) && SemanticElementExists(modalChild),
				timeoutMS: 5000,
				message: "Timed out waiting for modal authorization semantic nodes.");

			var accessibility = GetAccessibilityInstance();
			var accessibilityType = accessibility.GetType();
			var scopeType = accessibilityType.Assembly.GetType("Uno.UI.Runtime.Skia.ModalFocusScope", throwOnError: true)!;
			var constructor = scopeType.GetConstructor(
				BindingFlags.Instance | BindingFlags.NonPublic,
				binder: null,
				new[] { typeof(IntPtr), typeof(IntPtr), typeof(System.Collections.Generic.List<IntPtr>) },
				modifiers: null);
			Assert.IsNotNull(constructor);
			var scope = constructor!.Invoke(new object[]
			{
				modal.Visual.Handle,
				IntPtr.Zero,
				new System.Collections.Generic.List<IntPtr> { modalChild.Visual.Handle },
			});
			scopeType.GetField("_isActive", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(scope, true);
			accessibilityType.GetProperty("ActiveModalScope", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(accessibility, scope);

			try
			{
				var authorize = accessibilityType.GetMethod("IsActionAllowedByModalScope", BindingFlags.Instance | BindingFlags.NonPublic);
				Assert.IsNotNull(authorize);
				Assert.AreEqual(false, authorize!.Invoke(accessibility, new object[] { background, background.Visual.Handle }));
				Assert.AreEqual(true, authorize.Invoke(accessibility, new object[] { modal, modal.Visual.Handle }));
				Assert.AreEqual(true, authorize.Invoke(accessibility, new object[] { modalChild, modalChild.Visual.Handle }));
			}
			finally
			{
				accessibilityType.GetProperty("ActiveModalScope", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(accessibility, null);
			}
		}

		private static object GetAccessibilityInstance()
		{
			var type = Type.GetType("Uno.UI.Runtime.Skia.WebAssemblyAccessibility, Uno.UI.Runtime.Skia.WebAssembly.Browser", throwOnError: true)!;
			var instance = type.GetProperty("Instance", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null);
			Assert.IsNotNull(instance, "Unable to resolve the WebAssembly accessibility singleton.");
			return instance!;
		}

		private static int GetPrivateCollectionCount(object instance, string fieldName)
		{
			for (var type = instance.GetType(); type is not null; type = type.BaseType)
			{
				if (type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(instance) is ICollection collection)
				{
					return collection.Count;
				}
			}

			Assert.Fail($"Unable to locate collection field {fieldName}.");
			return -1;
		}

		private sealed partial class ThrowingNameListView : ListView
		{
			protected override AutomationPeer OnCreateAutomationPeer() => new ThrowingNameListViewPeer(this);
		}

		private sealed partial class ThrowingNameListViewPeer : ListViewAutomationPeer
		{
			public ThrowingNameListViewPeer(ListView owner) : base(owner) { }
			protected override string GetNameCore() => throw new InvalidOperationException("Name provider failed.");
		}
#endif
	}
}