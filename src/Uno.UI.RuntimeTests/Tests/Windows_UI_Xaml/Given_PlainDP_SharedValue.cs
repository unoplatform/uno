#if HAS_UNO
#nullable enable

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml
{
	/// <summary>
	/// Values on a plain custom DependencyProperty (see <see cref="Given_SharedTemplate_DataContext"/>), held outside any
	/// ResourceDictionary: a ResourceDictionary item never caches a DataContext, so these cover the store-level opt-out.
	/// </summary>
	[TestClass]
	[RunsOnUIThread]
	public class Given_PlainDP_SharedValue
	{
		[TestMethod]
		public async Task When_Style_Not_In_Dictionary_Owner_Removed_Then_Owner_And_ViewModel_Collected()
		{
			var style = new Style(typeof(Border));
			style.Setters.Add(new Setter(FrameworkElement.MinWidthProperty, 10d));

			await AssertOwnerAndViewModelCollected(h => h.ItemStyle = style);
			GC.KeepAlive(style);
		}

		[TestMethod]
		public async Task When_DataTemplate_Not_In_Dictionary_Owner_Removed_Then_Owner_And_ViewModel_Collected()
		{
			var template = CreateTemplate();

			await AssertOwnerAndViewModelCollected(h => h.ItemTemplate = template);
			GC.KeepAlive(template);
		}

		[TestMethod]
		public async Task When_Owner_Style_Setter_Assigns_Shared_DataTemplate_Owner_Removed_Then_Owner_And_ViewModel_Collected()
		{
			// The issue's main shape: a control Style assigns a shared resource to the control's own plain DP.
			var template = CreateTemplate();
			var resources = new ResourceDictionary { ["SharedTemplate"] = template };
			var ownerStyle = new Style(typeof(SharedValueHolder));
			ownerStyle.Setters.Add(new Setter(SharedValueHolder.ItemTemplateProperty, template));

			await AssertOwnerAndViewModelCollected(h => h.Style = ownerStyle);
			GC.KeepAlive(resources);
			GC.KeepAlive(ownerStyle);
		}

		[TestMethod]
		[DataRow(true)]
		[DataRow(false)]
		public async Task When_Two_Owners_Hold_Child_And_One_Releases_Then_Other_Keeps_DataContext(bool releaseFirst)
		{
			var probe = CreateBoundProbe();
			var first = new SharedValueHolder { Payload = probe };
			var second = new SharedValueHolder { Payload = probe };
			await UITestHelper.Load(new StackPanel { DataContext = new ViewModel { Name = "vm" }, Children = { first, second } }, x => x.IsLoaded);
			Assert.AreEqual("vm", probe.Value);

			(releaseFirst ? first : second).Payload = null;

			Assert.AreEqual("vm", probe.Value, "The child is still held by the other owner, which shares the DataContext.");
		}

		[TestMethod]
		public async Task When_Owner_Holds_Child_On_Two_Properties_And_One_Releases_Then_DataContext_Kept()
		{
			var probe = CreateBoundProbe();
			var holder = new TwoSlotHolder { First = probe, Second = probe, DataContext = new ViewModel { Name = "vm" } };
			await UITestHelper.Load(holder, x => x.IsLoaded);
			Assert.AreEqual("vm", probe.Value);

			holder.First = null;

			Assert.AreEqual("vm", probe.Value, "The child is still held by the owner's other property.");
		}

		[TestMethod]
		public async Task When_All_Owners_Release_Child_Then_ViewModel_Collected()
		{
			var probe = CreateBoundProbe();
			SharedValueHolder? first = new SharedValueHolder { Payload = probe };
			SharedValueHolder? second = new SharedValueHolder { Payload = probe };
			var weakViewModel = await LoadWithViewModel(new StackPanel { Children = { first, second } });
			Assert.AreEqual("vm", probe.Value);

			first.Payload = null;
			second.Payload = null;
			first = second = null;
			TestServices.WindowHelper.WindowContent = null;
			await TestServices.WindowHelper.WaitForIdle();

			// The owners and their tree are gone; only the released child is still referenced.
			Assert.IsTrue(await TestHelper.TryWaitUntilCollected(weakViewModel), "The released child must not keep the owners' DataContext alive.");
			GC.KeepAlive(probe);
		}

		private static async Task<WeakReference> LoadWithViewModel(FrameworkElement root)
		{
			var weakViewModel = SetViewModel(root);
			await UITestHelper.Load(root, x => x.IsLoaded);
			return weakViewModel;
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static WeakReference SetViewModel(FrameworkElement root)
		{
			var viewModel = new ViewModel { Name = "vm" };
			root.DataContext = viewModel;
			return new WeakReference(viewModel);
		}

		private static DataContextProbeObject CreateBoundProbe()
		{
			var probe = new DataContextProbeObject();
			BindingOperations.SetBinding(probe, DataContextProbeObject.ValueProperty, new Binding { Path = new PropertyPath(nameof(ViewModel.Name)) });
			return probe;
		}

		private static async Task AssertOwnerAndViewModelCollected(Action<SharedValueHolder> configure)
		{
			var (weakViewModel, weakHolder) = await LoadAndRemove(configure);

			Assert.IsTrue(await TestHelper.TryWaitUntilCollected(weakHolder), "The removed owner must not be kept alive by the shared value.");
			Assert.IsTrue(await TestHelper.TryWaitUntilCollected(weakViewModel), "The removed owner's DataContext must not be kept alive by the shared value.");
		}

		private static async Task<(WeakReference ViewModel, WeakReference Holder)> LoadAndRemove(Action<SharedValueHolder> configure)
		{
			var (holder, weakViewModel, weakHolder) = CreateHolder(configure);

			await UITestHelper.Load(holder!, x => x.IsLoaded);
			TestServices.WindowHelper.WindowContent = null;
			await TestServices.WindowHelper.WaitForIdle();
			holder = null;

			return (weakViewModel, weakHolder);
		}

		// Not inlined, so the view model is never held by a local of the calling async state machine.
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static (SharedValueHolder? Holder, WeakReference ViewModel, WeakReference HolderRef) CreateHolder(Action<SharedValueHolder> configure)
		{
			var viewModel = new ViewModel { Name = "vm" };
			var holder = new SharedValueHolder();
			configure(holder);
			holder.DataContext = viewModel;

			return (holder, new WeakReference(viewModel), new WeakReference(holder));
		}

		private static DataTemplate CreateTemplate()
			=> (DataTemplate)XamlReader.Load(
				"""<DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"><TextBlock Text="{Binding}" /></DataTemplate>""");

		private sealed class ViewModel
		{
			public string Name { get; set; } = "";
		}

		private sealed partial class TwoSlotHolder : Control
		{
			public static DependencyProperty FirstProperty { get; } = DependencyProperty.Register(
				nameof(First), typeof(object), typeof(TwoSlotHolder), new PropertyMetadata(null));

			public object? First
			{
				get => GetValue(FirstProperty);
				set => SetValue(FirstProperty, value);
			}

			public static DependencyProperty SecondProperty { get; } = DependencyProperty.Register(
				nameof(Second), typeof(object), typeof(TwoSlotHolder), new PropertyMetadata(null));

			public object? Second
			{
				get => GetValue(SecondProperty);
				set => SetValue(SecondProperty, value);
			}
		}
	}
}
#endif
