#if HAS_UNO
#nullable enable

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml
{
	/// <summary>
	/// Values assigned to a plain custom DependencyProperty (no FrameworkPropertyMetadataOptions) must not retain the
	/// owner or the owner's DataContext once the owner is removed:
	/// - a shared DataTemplate or Style (no DataContext in WinUI),
	/// - a shared Brush (shareable value, held by the owner only through a weak inheritance-context association),
	/// - a value released from the property (replaced or cleared).
	/// </summary>
	[TestClass]
	[RunsOnUIThread]
	public class Given_SharedTemplate_DataContext
	{
		[TestMethod]
		public async Task When_Shared_DataTemplate_On_Plain_DP_Owner_Removed_Then_ViewModel_Collected()
		{
			var template = CreateTemplate();
			var resources = new ResourceDictionary { ["SharedTemplate"] = template };

			await AssertOwnerAndViewModelCollected(h => h.ItemTemplate = template, "a shared DataTemplate");
			GC.KeepAlive(resources);
		}

		[TestMethod]
		public async Task When_Shared_Style_On_Plain_DP_Owner_Removed_Then_Owner_And_ViewModel_Collected()
		{
			var style = new Style(typeof(Border));
			style.Setters.Add(new Setter(FrameworkElement.MinWidthProperty, 10d));
			var resources = new ResourceDictionary { ["SharedStyle"] = style };

			await AssertOwnerAndViewModelCollected(h => h.ItemStyle = style, "a shared Style");
			GC.KeepAlive(resources);
		}

		[TestMethod]
		public async Task When_Shared_Brush_On_Plain_DP_Owner_Removed_Then_Owner_And_ViewModel_Collected()
		{
			var brush = new SolidColorBrush(Colors.Red);
			var resources = new ResourceDictionary { ["SharedBrush"] = brush };

			await AssertOwnerAndViewModelCollected(h => h.ItemBrush = brush, "a shared Brush");
			GC.KeepAlive(resources);
		}

		[TestMethod]
		public async Task When_DataTemplate_In_Collection_On_Plain_DP_Owner_Removed_Then_ViewModel_Collected()
		{
			// The template outlives the owner (e.g. it is cached by the app), and is reached through a collection.
			var template = CreateTemplate();

			await AssertOwnerAndViewModelCollected(h => h.Payload = new DependencyObjectCollection { template }, "a DataTemplate held in a collection");
			GC.KeepAlive(template);
		}

		[TestMethod]
		public async Task When_Shared_DataTemplate_As_ContentTemplate_Owner_Removed_Then_ViewModel_Collected()
		{
			// Adjacent regression: framework DP flagged ValueDoesNotInheritDataContext (already worked before the fix).
			var template = CreateTemplate();
			var resources = new ResourceDictionary { ["SharedTemplate"] = template };

			var (holder, weakViewModel, _) = CreateHolder(_ => { });
			var contentControl = new ContentControl { ContentTemplate = template, Content = "x", DataContext = holder!.DataContext };
			holder = null;

			await UITestHelper.Load(contentControl, x => x.IsLoaded);
			TestServices.WindowHelper.WindowContent = null;
			await TestServices.WindowHelper.WaitForIdle();
			contentControl = null;

			Assert.IsTrue(await TestHelper.TryWaitUntilCollected(weakViewModel));
			GC.KeepAlive(resources);
		}

		[TestMethod]
		public async Task When_Plain_DP_Holds_DependencyObject_Then_Binding_Still_Resolves_DataContext()
		{
			// Adjacent regression: non-template, non-Style DependencyObjects on a plain DP keep their inheritance context.
			var probe = CreateBoundProbe();
			var holder = new SharedValueHolder { Payload = probe, DataContext = new LeakProbeViewModel { Name = "first" } };
			await UITestHelper.Load(holder, x => x.IsLoaded);
			Assert.AreEqual("first", probe.Value);

			holder.DataContext = new LeakProbeViewModel { Name = "second" };
			Assert.AreEqual("second", probe.Value);
		}

		[TestMethod]
		public async Task When_Plain_DP_Value_Replaced_By_Template_Then_Old_Child_Released()
		{
			var template = CreateTemplate();
			await AssertReleasedChildDropsDataContext(h => h.Payload = template);
			GC.KeepAlive(template);
		}

		[TestMethod]
		public async Task When_Plain_DP_Value_Cleared_Then_Old_Child_Released()
		{
			await AssertReleasedChildDropsDataContext(h => h.Payload = null);
		}

		private static async Task AssertOwnerAndViewModelCollected(Action<SharedValueHolder> configure, string what)
		{
			var (holder, weakViewModel, weakHolder) = CreateHolder(configure);

			await UITestHelper.Load(holder!, x => x.IsLoaded);
			TestServices.WindowHelper.WindowContent = null;
			await TestServices.WindowHelper.WaitForIdle();
			holder = null;

			Assert.IsTrue(await TestHelper.TryWaitUntilCollected(weakHolder), $"The removed owner must not be kept alive by {what}.");
			Assert.IsTrue(await TestHelper.TryWaitUntilCollected(weakViewModel), $"The removed owner's DataContext must not be kept alive by {what}.");
		}

		/// <summary>
		/// A child released from the owner's property must stop following the owner's DataContext,
		/// and must not keep the DataContext it was given alive.
		/// </summary>
		private static async Task AssertReleasedChildDropsDataContext(Action<SharedValueHolder> release)
		{
			var probe = CreateBoundProbe();
			var (holder, weakViewModel, _) = CreateHolder(h => h.Payload = probe);

			await UITestHelper.Load(holder!, x => x.IsLoaded);
			Assert.AreEqual("vm", probe.Value);

			release(holder!);
			holder!.DataContext = new LeakProbeViewModel { Name = "other" };
			Assert.AreNotEqual("other", probe.Value, "The released child must no longer receive the owner's DataContext.");

			Assert.IsTrue(await TestHelper.TryWaitUntilCollected(weakViewModel), "The released child must not keep the owner's previous DataContext alive.");
			GC.KeepAlive(probe);
			GC.KeepAlive(holder);
		}

		private static DataTemplate CreateTemplate()
			=> (DataTemplate)XamlReader.Load(
				"""<DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"><TextBlock Text="{Binding}" /></DataTemplate>""");

		private static DataContextProbeObject CreateBoundProbe()
		{
			var probe = new DataContextProbeObject();
			BindingOperations.SetBinding(probe, DataContextProbeObject.ValueProperty, new Binding { Path = new PropertyPath(nameof(LeakProbeViewModel.Name)) });
			return probe;
		}

		// Not inlined, so the view model is never held by a local of the calling async state machine.
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static (SharedValueHolder? Holder, WeakReference ViewModel, WeakReference HolderRef) CreateHolder(Action<SharedValueHolder> configure)
		{
			var viewModel = new LeakProbeViewModel { Name = "vm" };
			var holder = new SharedValueHolder();

			// Values first, then the DataContext: like XAML (and Style setters), and independent of whether a value
			// assigned after the DataContext receives it immediately.
			configure(holder);
			holder.DataContext = viewModel;
			return (holder, new WeakReference(viewModel), new WeakReference(holder));
		}

		private sealed class LeakProbeViewModel
		{
			public string Name { get; set; } = "";

			public byte[] Payload { get; } = new byte[1024];
		}
	}

	/// <summary>
	/// Mirrors a third-party control declaring template/style/brush properties with plain <see cref="PropertyMetadata"/>.
	/// </summary>
	public partial class SharedValueHolder : Control
	{
		public static DependencyProperty ItemTemplateProperty { get; } = DependencyProperty.Register(
			nameof(ItemTemplate), typeof(DataTemplate), typeof(SharedValueHolder), new PropertyMetadata(null));

		public DataTemplate? ItemTemplate
		{
			get => (DataTemplate?)GetValue(ItemTemplateProperty);
			set => SetValue(ItemTemplateProperty, value);
		}

		public static DependencyProperty ItemStyleProperty { get; } = DependencyProperty.Register(
			nameof(ItemStyle), typeof(Style), typeof(SharedValueHolder), new PropertyMetadata(null));

		public Style? ItemStyle
		{
			get => (Style?)GetValue(ItemStyleProperty);
			set => SetValue(ItemStyleProperty, value);
		}

		public static DependencyProperty ItemBrushProperty { get; } = DependencyProperty.Register(
			nameof(ItemBrush), typeof(Brush), typeof(SharedValueHolder), new PropertyMetadata(null));

		public Brush? ItemBrush
		{
			get => (Brush?)GetValue(ItemBrushProperty);
			set => SetValue(ItemBrushProperty, value);
		}

		public static DependencyProperty PayloadProperty { get; } = DependencyProperty.Register(
			nameof(Payload), typeof(object), typeof(SharedValueHolder), new PropertyMetadata(null));

		public object? Payload
		{
			get => GetValue(PayloadProperty);
			set => SetValue(PayloadProperty, value);
		}
	}

	public partial class DataContextProbeObject : DependencyObject
	{
		public static DependencyProperty ValueProperty { get; } = DependencyProperty.Register(
			nameof(Value), typeof(object), typeof(DataContextProbeObject), new PropertyMetadata(null));

		public object? Value
		{
			get => GetValue(ValueProperty);
			set => SetValue(ValueProperty, value);
		}
	}
}
#endif
