using System;
using System.Linq;
using System.Threading.Tasks;
using FrameworkPoolEditorRecycling;
using Uno.UI.RuntimeTests.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using static Private.Infrastructure.TestServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using FluentAssertions;
using MUXControlsTestApp.Utilities;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Windows.ApplicationModel.UserDataTasks.DataProvider;
using System.Collections.Generic;
using System.Reflection;
using Windows.Web.Syndication;


#if WINAPPSDK
using Uno.UI.Extensions;
#elif __IOS__
using UIKit;
#elif __MACOS__
using AppKit;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
internal partial class Given_FrameworkTemplatePool
{
#if HAS_UNO
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Recycle()
	{
		using (FeatureConfigurationHelper.UseTemplatePooling())
		{
			FrameworkTemplatePool.Instance.Scavenge(force: true);

			async Task<(WeakReference control, WeakReference root)> CreateAndRelease()
			{
				var content = new Button();
				WindowHelper.WindowContent = content;
				await WindowHelper.WaitForLoaded(content);
				var templatedRoot = content.TemplatedRoot;

				WindowHelper.WindowContent = null;

				return (
					new WeakReference(content),
					new WeakReference(templatedRoot));
			}

			var (targetInstance, targetTemplateRoot) = await CreateAndRelease();

			var timeout = Stopwatch.StartNew();
			while (targetInstance.IsAlive && timeout.Elapsed < TimeSpan.FromSeconds(5))
			{
				GC.Collect(2);
				GC.WaitForPendingFinalizers();
				await WindowHelper.WaitForIdle();
				await Task.Delay(50);
			}

			await WindowHelper.WaitForIdle();

			Assert.IsNull(targetInstance.Target, "targetInstance.Target is not null");
			Assert.IsNotNull(targetTemplateRoot.Target, "targetTemplateRoot.Target is null");

			Assert.AreEqual(1, FrameworkTemplatePool.Instance.GetPooledTemplateCount(), "GetPooledTemplateCount is incorrect");

			FrameworkTemplatePool.Instance.Scavenge(force: true);

			var timeout2 = Stopwatch.StartNew();
			while (targetTemplateRoot.IsAlive && timeout2.Elapsed < TimeSpan.FromSeconds(5))
			{
				GC.Collect(2);
				GC.WaitForPendingFinalizers();
				await WindowHelper.WaitForIdle();
				await Task.Delay(50);
			}

			Assert.AreEqual(0, FrameworkTemplatePool.Instance.GetPooledTemplateCount(), "GetPooledTemplateCount is incorrect");
		}
	}
#endif

	[TestMethod]
	public async Task When_CheckBox()
	{
		using var _ = FeatureConfigurationHelper.UseTemplatePooling();

		var scrollViewer = new ScrollViewer();
		var elevatedViewChild = new Uno.UI.Toolkit.ElevatedView();
		scrollViewer.Content = elevatedViewChild;

		var c = new CheckBox();
		c.IsChecked = true;
		bool uncheckedFired = false;
		c.Unchecked += (_, _) => uncheckedFired = true;
		elevatedViewChild.ElevatedContent = c;

		WindowHelper.WindowContent = scrollViewer;
		await WindowHelper.WaitForLoaded(scrollViewer);
		var template = scrollViewer.Template;

		scrollViewer.Template = null;
		scrollViewer.Template = template;

		scrollViewer.ApplyTemplate();

		Assert.IsFalse(uncheckedFired);
	}

	[TestMethod]
	public async Task When_TextBox()
	{
		using (FeatureConfigurationHelper.UseTemplatePooling())
		{
			var scrollViewer = new ScrollViewer();

			var textBox = new TextBox();
			bool textChangedFired = false;
			textBox.TextChanged += (_, _) => textChangedFired = true;
			textBox.Text = "Text";

			// TextBox dispatches raising the event. We wait here until the above TextChanged is raised.
			await Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => { });

			Assert.IsTrue(textChangedFired);
			textChangedFired = false;

			scrollViewer.Content = textBox;

			WindowHelper.WindowContent = scrollViewer;
			await WindowHelper.WaitForLoaded(scrollViewer);
			var template = scrollViewer.Template;

			scrollViewer.Template = null;
			scrollViewer.Template = template;

			scrollViewer.ApplyTemplate();

			await Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => { });

			Assert.IsFalse(textChangedFired);
		}
	}

	[TestMethod]
	public async Task When_ToggleSwitch()
	{
		using (FeatureConfigurationHelper.UseTemplatePooling())
		{
			var scrollViewer = new ScrollViewer();

			var toggleSwitch = new ToggleSwitch();
			toggleSwitch.IsOn = true;
			bool toggledFired = false;
			toggleSwitch.Toggled += (_, _) => toggledFired = true;
			scrollViewer.Content = toggleSwitch;

			WindowHelper.WindowContent = scrollViewer;
			await WindowHelper.WaitForLoaded(scrollViewer);
			var template = scrollViewer.Template;

			scrollViewer.Template = null;
			scrollViewer.Template = template;

			scrollViewer.ApplyTemplate();

			Assert.IsFalse(toggledFired);
		}
	}

	[TestMethod]
	public async Task When_Editor_Recycling()
	{
		using var _ = FeatureConfigurationHelper.UseTemplatePooling();

		var page = new EditorTestPage();

		WindowHelper.WindowContent = page;
		await WindowHelper.WaitForLoaded(page);
		await WindowHelper.WaitForIdle();

		var vm = page.ViewModel;

		void AssertEditorContents()
		{
			var textBox = page.FindFirstChild<TextBox>();
			var checkBox = page.FindFirstChild<CheckBox>();
			var toggleSwitch = page.FindFirstChild<ToggleSwitch>();
			Assert.IsTrue(vm.Editors.All(e => !string.IsNullOrEmpty(e.Text)));
			Assert.IsTrue(vm.Editors.All(e => e.IsChecked));
			Assert.IsTrue(vm.Editors.All(e => e.IsOn));
			Assert.IsTrue(!string.IsNullOrEmpty(textBox.Text));
			Assert.IsTrue(checkBox.IsChecked);
			Assert.IsTrue(toggleSwitch.IsOn);
			Assert.AreEqual(vm.CurrentEditor.IsChecked, checkBox.IsChecked);
			Assert.AreEqual(vm.CurrentEditor.IsOn, toggleSwitch.IsOn);
		}

		// Verify initial state
		AssertEditorContents();

		TextBox textBox = null;
		CheckBox checkBox = null;
		ToggleSwitch toggleSwitch = null;
		// Cycle twice - once without focus, once with focus			
		for (int i = 0; i < vm.Editors.Length * 2; i++)
		{
			vm.SetNextEditor();

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitFor(() => (textBox = page.FindFirstChild<TextBox>()) is not null);
			await WindowHelper.WaitFor(() => (checkBox = page.FindFirstChild<CheckBox>()) is not null);
			await WindowHelper.WaitFor(() => (toggleSwitch = page.FindFirstChild<ToggleSwitch>()) is not null);
			if (i > vm.Editors.Length - 1)
			{
				textBox.Focus(FocusState.Programmatic);
				await WindowHelper.WaitFor(() => Equals(FocusManager.GetFocusedElement(WindowHelper.XamlRoot), textBox));
			}

			AssertEditorContents();
		}
	}

#if HAS_UNO
	[TestMethod]
	public async Task When_ContentControl_Template_Recycled()
	{
		await using var _1 = ValidateActiveInstanceTrackers();
		using var _2 = FeatureConfigurationHelper.UseTemplatePooling();

		var TemplateCreated = 0;
		List<WeakReference> created = new();
		var dataTemplate = new ControlTemplate(() =>
		{
			TemplateCreated++;
			var b = new TemplatePoolAwareControl();
			created.Add(new(b));
			return b;
		});

		var SUT = new ContentControl()
		{
			Template = dataTemplate
		};

		var root = new Grid();
		root.Children.Add(SUT);
		WindowHelper.WindowContent = root;

		Assert.AreEqual(1, TemplateCreated);

		SUT.Template = null;

		Assert.AreEqual(1, TemplateCreated);
		Assert.AreEqual(1, created.Count);
		Assert.AreEqual(1, ((TemplatePoolAwareControl)created[0].Target)?.TemplateRecycled);
	}

	[TestMethod]
	public async Task When_ContentControl_Template_Replaced_Recycled()
	{
		await using var _1 = ValidateActiveInstanceTrackers();
		using var _2 = FeatureConfigurationHelper.UseTemplatePooling();

		var template1Created = 0;
		List<WeakReference> _created = new();
		var template1 = new ControlTemplate(() =>
		{
			template1Created++;
			var b = new TemplatePoolAwareControl();
			_created.Add(new WeakReference(b));
			return b;
		});

		var template2Created = 0;
		var template2 = new ControlTemplate(() =>
		{
			template2Created++;
			var b = new TemplatePoolAwareControl();
			_created.Add(new WeakReference(b));
			return b;
		});

		var SUT = new ContentControl()
		{
			Template = template1
		};

		var root = new Grid();
		root.Children.Add(SUT);
		WindowHelper.WindowContent = root;

		Assert.AreEqual(1, template1Created);

		SUT.Template = template2;

		Assert.AreEqual(1, template1Created);
		Assert.AreEqual(1, template2Created);
		Assert.AreEqual(2, _created.Count);

		SUT.Template = null;

		Assert.AreEqual(1, ((TemplatePoolAwareControl)_created[0].Target).TemplateRecycled);
		Assert.AreEqual(1, ((TemplatePoolAwareControl)_created[1].Target).TemplateRecycled);
	}

	[TestMethod]
	public async Task When_ContentControl_ContentTemplate_Recycled()
	{
		await using var _1 = ValidateActiveInstanceTrackers();
		using var _2 = FeatureConfigurationHelper.UseTemplatePooling();

		var TemplateCreated = 0;
		List<WeakReference> _created = new();
		var dataTemplate = new DataTemplate(() =>
		{
			TemplateCreated++;
			var b = new TemplatePoolAwareControl();
			_created.Add(new WeakReference(b));
			return b;
		});

		var SUT = new ContentControl()
		{
			ContentTemplate = dataTemplate
		};

		var root = new Grid();
		root.Children.Add(SUT);
		WindowHelper.WindowContent = root;

		Assert.AreEqual(1, TemplateCreated);

		SUT.ContentTemplate = null;

		Assert.AreEqual(1, TemplateCreated);
		Assert.AreEqual(1, _created.Count);
		Assert.AreEqual(1, ((TemplatePoolAwareControl)_created[0].Target)?.TemplateRecycled);
	}

	[TestMethod]
	public async Task When_ContentControl_ContentTemplate_Replaced_Recycled()
	{
		await using var _1 = ValidateActiveInstanceTrackers();
		using var _2 = FeatureConfigurationHelper.UseTemplatePooling();

		var template1Created = 0;
		List<WeakReference> _created = new();
		var dataTemplate1 = new DataTemplate(() =>
		{
			template1Created++;
			var b = new TemplatePoolAwareControl();
			_created.Add(new(b));
			return b;
		});

		var template2Created = 0;
		var dataTemplate2 = new DataTemplate(() =>
		{
			template2Created++;
			var b = new TemplatePoolAwareControl();
			_created.Add(new(b));
			return b;
		});

		var SUT = new ContentControl()
		{
			ContentTemplate = dataTemplate1
		};

		var root = new Grid();
		root.Children.Add(SUT);
		WindowHelper.WindowContent = root;

		Assert.AreEqual(1, template1Created);

		SUT.ContentTemplate = dataTemplate2;

		Assert.AreEqual(1, template1Created);
		Assert.AreEqual(1, template2Created);
		Assert.AreEqual(2, _created.Count);

		SUT.ContentTemplate = null;

		Assert.AreEqual(1, ((TemplatePoolAwareControl)_created[0].Target)?.TemplateRecycled);
		Assert.AreEqual(1, ((TemplatePoolAwareControl)_created[1].Target)?.TemplateRecycled);

		await AssertCollectedReference(_created[0]);
		await AssertCollectedReference(_created[1]);
	}

	[TestMethod]
	public async Task When_ContentPresenter_ContentTemplate_Replaced_Recycled()
	{
		await using var _1 = ValidateActiveInstanceTrackers();
		using var _2 = FeatureConfigurationHelper.UseTemplatePooling();

		var template1Created = 0;
		List<WeakReference> _created = new();
		var dataTemplate1 = new DataTemplate(() =>
		{
			template1Created++;
			var b = new TemplatePoolAwareControl();
			_created.Add(new(b));
			return b;
		});

		var template2Created = 0;
		var dataTemplate2 = new DataTemplate(() =>
		{
			template2Created++;
			var b = new TemplatePoolAwareControl();
			_created.Add(new(b));
			return b;
		});

		var SUT = new ContentPresenter()
		{
			ContentTemplate = dataTemplate1
		};

		var root = new Grid();
		root.Children.Add(SUT);
		WindowHelper.WindowContent = root;

		Assert.AreEqual(1, template1Created);

		SUT.ContentTemplate = dataTemplate2;

		Assert.AreEqual(1, template1Created);
		Assert.AreEqual(1, template2Created);
		Assert.AreEqual(2, _created.Count);

		SUT.ContentTemplate = null;

		Assert.AreEqual(1, ((TemplatePoolAwareControl)_created[0].Target)?.TemplateRecycled);
		Assert.AreEqual(1, ((TemplatePoolAwareControl)_created[1].Target)?.TemplateRecycled);
	}

	public partial class TemplatePoolAwareControl : Grid, IFrameworkTemplatePoolAware
	{
		public int TemplateRecycled { get; private set; }

		public void OnTemplateRecycled()
		{
			TemplateRecycled++;
		}
	}

	private IAsyncDisposable ValidateActiveInstanceTrackers()
	{
		GC.Collect(2);
		GC.WaitForPendingFinalizers();
		FrameworkTemplatePool.Scavenge();

		var originalTrackers = FrameworkTemplatePool.ActiveInstanceTrackers;

		return new AsyncDisposableAction(async () =>
		{
			var sw = Stopwatch.StartNew();

			while (sw.Elapsed < TimeSpan.FromSeconds(5))
			{
				GC.Collect(2);
				GC.WaitForPendingFinalizers();

				if (originalTrackers == FrameworkTemplatePool.ActiveInstanceTrackers)
				{
					return;
				}

				FrameworkTemplatePool.Scavenge();

				await Task.Delay(100);
			}

			Assert.AreEqual(originalTrackers, FrameworkTemplatePool.ActiveInstanceTrackers);
		});
	}

	private async Task AssertCollectedReference(WeakReference reference, string message = "")
	{
		var sw = Stopwatch.StartNew();
		while (sw.Elapsed < TimeSpan.FromSeconds(5))
		{
			GC.Collect(2);
			GC.WaitForPendingFinalizers();

			if (!reference.IsAlive)
			{
				return;
			}

			FrameworkTemplatePool.Scavenge();

			await Task.Delay(100);
		}

		Assert.IsFalse(reference.IsAlive, message);
	}
#endif
}
