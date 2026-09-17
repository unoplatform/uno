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
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Media;
using Uno.UI;
using Uno.UI.Xaml;
using Windows.UI;
using System.Dynamic;

namespace Microsoft.UI.Xaml
{
	public partial class FrameworkElement
	{
#pragma warning disable CS0067 // Unused only in reference API.
		public event SizeChangedEventHandler SizeChanged;
#pragma warning restore CS0067

		internal bool WantsSizeChanged => SizeChanged != null;

		public double ActualWidth => GetActualWidth();
		public double ActualHeight => GetActualHeight();

		#region HorizontalAlignment Dependency Property
		[GeneratedDependencyProperty(
			DefaultValue = Xaml.HorizontalAlignment.Stretch,
			Options = FrameworkPropertyMetadataOptions.AutoConvert | FrameworkPropertyMetadataOptions.AffectsArrange
#if DEBUG
			, ChangedCallbackName = nameof(OnGenericPropertyUpdated)
#endif
		)]
		public partial HorizontalAlignment HorizontalAlignment { get; set; }
		#endregion

		#region VerticalAlignment Dependency Property
		[GeneratedDependencyProperty(
			DefaultValue = Xaml.VerticalAlignment.Stretch,
			Options = FrameworkPropertyMetadataOptions.AutoConvert | FrameworkPropertyMetadataOptions.AffectsArrange
#if DEBUG
			, ChangedCallbackName = nameof(OnGenericPropertyUpdated)
#endif
		)]
		public partial VerticalAlignment VerticalAlignment { get; set; }
		#endregion

		#region Width Dependency Property
		[GeneratedDependencyProperty(
			DefaultValue = double.NaN,
			Options = FrameworkPropertyMetadataOptions.AutoConvert | FrameworkPropertyMetadataOptions.AffectsMeasure
#if DEBUG
			, ChangedCallbackName = nameof(OnGenericPropertyUpdated)
#endif
		)]
		public partial double Width { get; set; }
		#endregion

		#region Height Dependency Property
		[GeneratedDependencyProperty(
			DefaultValue = double.NaN,
			Options = FrameworkPropertyMetadataOptions.AutoConvert | FrameworkPropertyMetadataOptions.AffectsMeasure
#if DEBUG
			, ChangedCallbackName = nameof(OnGenericPropertyUpdated)
#endif
		)]
		public partial double Height { get; set; }
		#endregion

		#region MinWidth Dependency Property
		[GeneratedDependencyProperty(
			DefaultValue = 0.0d,
			Options = FrameworkPropertyMetadataOptions.AutoConvert | FrameworkPropertyMetadataOptions.AffectsMeasure
#if DEBUG
			, ChangedCallbackName = nameof(OnGenericPropertyUpdated)
#endif
		)]
		public partial double MinWidth { get; set; }
		#endregion

		#region MinHeight Dependency Property

		[GeneratedDependencyProperty(
			DefaultValue = 0.0d,
			Options = FrameworkPropertyMetadataOptions.AutoConvert | FrameworkPropertyMetadataOptions.AffectsMeasure
#if DEBUG
			, ChangedCallbackName = nameof(OnGenericPropertyUpdated)
#endif
		)]
		public partial double MinHeight { get; set; }
		#endregion

		#region MaxWidth Dependency Property
		[GeneratedDependencyProperty(
			DefaultValue = double.PositiveInfinity,
			Options = FrameworkPropertyMetadataOptions.AutoConvert | FrameworkPropertyMetadataOptions.AffectsMeasure
#if DEBUG
			, ChangedCallbackName = nameof(OnGenericPropertyUpdated)
#endif
		)]
		public partial double MaxWidth { get; set; }
		#endregion

		#region MaxHeight Dependency Property

		[GeneratedDependencyProperty(
			DefaultValue = double.PositiveInfinity,
			Options = FrameworkPropertyMetadataOptions.AutoConvert | FrameworkPropertyMetadataOptions.AffectsMeasure
#if DEBUG
			, ChangedCallbackName = nameof(OnGenericPropertyUpdated)
#endif
		)]
		public partial double MaxHeight { get; set; }
		#endregion

		#region Margin Dependency Property
		[GeneratedDependencyProperty(
			Options = FrameworkPropertyMetadataOptions.AutoConvert | FrameworkPropertyMetadataOptions.AffectsMeasure
#if DEBUG
			, ChangedCallbackName = nameof(OnGenericPropertyUpdated)
#endif
		)]
		public partial Thickness Margin { get; set; }

		private static Thickness GetMarginDefaultValue() => Thickness.Empty;
		#endregion

		public new bool IsLoaded => base.IsLoaded; // The IsLoaded state is managed by the UIElement, FrameworkElement only makes it publicly visible

		private protected virtual void OnLoaded() { }

		partial void OnLoadingPartial();

		private protected sealed override void OnFwEltUnloaded()
		{
			// TODO: Unloaded is fired asynchronously in WinUI.
			void InvokeUnloaded()
			{
				// Raise event after invoking base in order to raise them bottom to top
				OnUnloaded();
				_unloaded?.Invoke(this, new RoutedEventArgs(this));
				OnUnloadedPartial();
			}

			if (FeatureConfiguration.FrameworkElement.HandleLoadUnloadExceptions)
			{
				/// <remarks>
				/// This method contains or is called by a try/catch containing method and
				/// can be significantly slower than other methods as a result on WebAssembly.
				/// See https://github.com/dotnet/runtime/issues/56309
				/// </remarks>
				void InvokeUnloadedWithTry()
				{
					try
					{
						InvokeUnloaded();
					}
					catch (Exception error)
					{
						_log.Error("OnElementUnloaded failed in FrameworkElement", error);
						Application.Current.RaiseRecoverableUnhandledException(error);
					}
				}

				InvokeUnloadedWithTry();
			}
			else
			{
				InvokeUnloaded();
			}
		}

		partial void OnUnloadedPartial();

		private protected virtual void OnUnloaded() { }


		public override string ToString()
		{
			return base.ToString();
		}
	}
}
