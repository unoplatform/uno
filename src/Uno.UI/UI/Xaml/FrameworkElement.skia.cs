using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Disposables;
using System.Text;
using System.Threading.Tasks;
using Uno.Extensions;
using Uno;
using Uno.Foundation.Logging;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;
using View = Microsoft.UI.Xaml.UIElement;
using System.Collections;

using Uno.UI.Xaml;
using System.Numerics;
using Uno.UI;

namespace Microsoft.UI.Xaml
{
	public partial class FrameworkElement : IEnumerable
	{
		protected FrameworkElement()
		{
			Initialize();
		}

		bool IFrameworkElementInternal.HasLayouter => true;

		partial void Initialize();


		internal bool HasParent()
		{
			return Parent != null;
		}

		private bool IsTopLevelXamlView() => false;

		internal void SuspendRendering() => throw new NotSupportedException();

		internal void ResumeRendering() => throw new NotSupportedException();
		public IEnumerator GetEnumerator() => _children.GetEnumerator();

		#region Name Dependency Property

		private void OnNameChanged(string oldValue, string newValue)
		{
			if (FrameworkElementHelper.IsUiAutomationMappingEnabled)
			{
				Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(this, newValue);
			}
		}

		[GeneratedDependencyProperty(DefaultValue = "", ChangedCallback = true)]
		public static DependencyProperty NameProperty { get; } = CreateNameProperty();

		public string Name
		{
			get => GetNameValue();
			set => SetNameValue(value);
		}

		#endregion

		#region Margin Dependency Property
		[GeneratedDependencyProperty(
			Options = FrameworkPropertyMetadataOptions.AutoConvert | FrameworkPropertyMetadataOptions.AffectsMeasure
#if DEBUG
			, ChangedCallbackName = nameof(OnGenericPropertyUpdated)
#endif
		)]
		public static DependencyProperty MarginProperty { get; } = CreateMarginProperty();

		public Thickness Margin
		{
			get => GetMarginValue();
			set => SetMarginValue(value);
		}
		private static Thickness GetMarginDefaultValue() => Thickness.Empty;
		#endregion

		#region HorizontalAlignment Dependency Property
		[GeneratedDependencyProperty(
			DefaultValue = Xaml.HorizontalAlignment.Stretch,
			Options = FrameworkPropertyMetadataOptions.AutoConvert | FrameworkPropertyMetadataOptions.AffectsMeasure
#if DEBUG
			, ChangedCallbackName = nameof(OnGenericPropertyUpdated)
#endif
		)]
		public static DependencyProperty HorizontalAlignmentProperty { get; } = CreateHorizontalAlignmentProperty();

		public HorizontalAlignment HorizontalAlignment
		{
			get => GetHorizontalAlignmentValue();
			set => SetHorizontalAlignmentValue(value);
		}
		#endregion

		#region HorizontalAlignment Dependency Property
		[GeneratedDependencyProperty(
			DefaultValue = Xaml.HorizontalAlignment.Stretch,
			Options = FrameworkPropertyMetadataOptions.AutoConvert | FrameworkPropertyMetadataOptions.AffectsMeasure
#if DEBUG
			, ChangedCallbackName = nameof(OnGenericPropertyUpdated)
#endif
		)]
		public static DependencyProperty VerticalAlignmentProperty { get; } = CreateVerticalAlignmentProperty();

		public VerticalAlignment VerticalAlignment
		{
			get => GetVerticalAlignmentValue();
			set => SetVerticalAlignmentValue(value);
		}
		#endregion

		#region Width Dependency Property
		[GeneratedDependencyProperty(
			DefaultValue = double.NaN,
			Options = FrameworkPropertyMetadataOptions.AutoConvert | FrameworkPropertyMetadataOptions.AffectsMeasure
#if DEBUG
			, ChangedCallbackName = nameof(OnGenericPropertyUpdated)
#endif
		)]
		public static DependencyProperty WidthProperty { get; } = CreateWidthProperty();

		public double Width
		{
			get => GetWidthValue();
			set => SetWidthValue(value);
		}
		#endregion

		#region Height Dependency Property
		[GeneratedDependencyProperty(
			DefaultValue = double.NaN,
			Options = FrameworkPropertyMetadataOptions.AutoConvert | FrameworkPropertyMetadataOptions.AffectsMeasure
#if DEBUG
			, ChangedCallbackName = nameof(OnGenericPropertyUpdated)
#endif
		)]
		public static DependencyProperty HeightProperty { get; } = CreateHeightProperty();

		public double Height
		{
			get => GetHeightValue();
			set => SetHeightValue(value);
		}
		#endregion

		#region MinWidth Dependency Property
		[GeneratedDependencyProperty(
			DefaultValue = 0.0d,
			Options = FrameworkPropertyMetadataOptions.AutoConvert | FrameworkPropertyMetadataOptions.AffectsMeasure
#if DEBUG
			, ChangedCallbackName = nameof(OnGenericPropertyUpdated)
#endif
		)]
		public static DependencyProperty MinWidthProperty { get; } = CreateMinWidthProperty();

		public double MinWidth
		{
			get => GetMinWidthValue();
			set => SetMinWidthValue(value);
		}
		#endregion

		#region MinHeight Dependency Property

		[GeneratedDependencyProperty(
			DefaultValue = 0.0d,
			Options = FrameworkPropertyMetadataOptions.AutoConvert | FrameworkPropertyMetadataOptions.AffectsMeasure
#if DEBUG
			, ChangedCallbackName = nameof(OnGenericPropertyUpdated)
#endif
		)]
		public static DependencyProperty MinHeightProperty { get; } = CreateMinHeightProperty();

		public double MinHeight
		{
			get => GetMinHeightValue();
			set => SetMinHeightValue(value);
		}
		#endregion

		#region MaxWidth Dependency Property
		[GeneratedDependencyProperty(
			DefaultValue = double.PositiveInfinity,
			Options = FrameworkPropertyMetadataOptions.AutoConvert | FrameworkPropertyMetadataOptions.AffectsMeasure
#if DEBUG
			, ChangedCallbackName = nameof(OnGenericPropertyUpdated)
#endif
		)]
		public static DependencyProperty MaxWidthProperty { get; } = CreateMaxWidthProperty();

		public double MaxWidth
		{
			get => GetMaxWidthValue();
			set => SetMaxWidthValue(value);
		}
		#endregion

		#region MaxHeight Dependency Property

		[GeneratedDependencyProperty(
			DefaultValue = double.PositiveInfinity,
			Options = FrameworkPropertyMetadataOptions.AutoConvert | FrameworkPropertyMetadataOptions.AffectsMeasure
#if DEBUG
			, ChangedCallbackName = nameof(OnGenericPropertyUpdated)
#endif
		)]
		public static DependencyProperty MaxHeightProperty { get; } = CreateMaxHeightProperty();

		public double MaxHeight
		{
			get => GetMaxHeightValue();
			set => SetMaxHeightValue(value);
		}
		#endregion

#if DEBUG
		private void OnGenericPropertyUpdated(DependencyPropertyChangedEventArgs args)
		{
		}
#endif

		private event TypedEventHandler<FrameworkElement, object> _loading;
		public event TypedEventHandler<FrameworkElement, object> Loading
		{
			add
			{
				_loading += value;
			}
			remove
			{
				_loading -= value;
			}
		}

		private event RoutedEventHandler _loaded;
		public event RoutedEventHandler Loaded
		{
			add
			{
				_loaded += value;
			}
			remove
			{
				_loaded -= value;
			}
		}

		private event RoutedEventHandler _unloaded;
		public event RoutedEventHandler Unloaded
		{
			add
			{
				_unloaded += value;
			}
			remove
			{
				_unloaded -= value;
			}
		}
	}
}
