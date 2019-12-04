using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using System;
using System.Collections.Generic;
using Windows.Foundation;
using System.Runtime.CompilerServices;
using System.Text;
using Windows.UI.Xaml.Input;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.UI;

namespace Windows.UI.Xaml
{
	public partial class FrameworkElement : UIElement, IFrameworkElement
	{
		public T FindFirstParent<T>() where T : class
		{
			var view = this.Parent;
			while (view != null)
			{
				var typed = view as T;
				if (typed != null)
				{
					return typed;
				}
				view = view.GetParent() as DependencyObject;
			}
			return null;
		}

		partial void Initialize();

		public FrameworkElement(string htmlTag = "div", bool isSvg = false) : base(htmlTag, isSvg)
		{
			Initialize();

			if (!FeatureConfiguration.FrameworkElement.WasmUseManagedLoadedUnloaded)
			{
				Loading += NativeOnLoading;
				Loaded += NativeOnLoaded;
				Unloaded += NativeOnUnloaded;
			}

			_log = this.Log();
			_logDebug = _log.IsEnabled(LogLevel.Debug) ? _log : null;
		}

		private protected readonly ILogger _log;
		private protected readonly ILogger _logDebug;

		private static readonly Uri DefaultBaseUri = new Uri("ms-appx://local");
		public global::System.Uri BaseUri
		{
			get;
			internal set;
		} = DefaultBaseUri;

		protected virtual void OnLoaded()
		{

		}

		protected virtual void OnUnloaded()
		{

		}

		#region Transitions Dependency Property

		public TransitionCollection Transitions
		{
			get { return (TransitionCollection)this.GetValue(TransitionsProperty); }
			set { this.SetValue(TransitionsProperty, value); }
		}

		public static readonly DependencyProperty TransitionsProperty =
			DependencyProperty.Register("Transitions", typeof(TransitionCollection), typeof(FrameworkElement), new PropertyMetadata(null, OnTransitionsChanged));

		private static void OnTransitionsChanged(object dependencyObject, DependencyPropertyChangedEventArgs args)
		{

		}
		#endregion

		public IFrameworkElement FindName(string name)
			=> IFrameworkElementHelper.FindName(this, GetChildren(), name);


		public void Dispose()
		{
		}

		public Size AdjustArrange(Size finalSize)
		{
			return finalSize;
		}

		#region Background DependencyProperty

		public Brush Background
		{
			get => (Brush)GetValue(BackgroundProperty);
			set => SetValue(BackgroundProperty, value);
		}

		// Using a DependencyProperty as the backing store for Background.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty BackgroundProperty =
			DependencyProperty.Register("Background", typeof(Brush), typeof(FrameworkElement), new PropertyMetadata(null, (s, e) => ((FrameworkElement)s)?.OnBackgroundChanged(e)));


		protected virtual void OnBackgroundChanged(DependencyPropertyChangedEventArgs e)
		{
			var brush = e.NewValue as Brush;
			SetBackgroundBrush(brush);
		}

		private void SetBackgroundBrush(Brush brush)
		{
			switch (brush)
			{
				case SolidColorBrush solidColorBrush:
					var color = solidColorBrush.ColorWithOpacity;
					SetStyle("background-color", color.ToCssString());
					RecalculateBrushOnSizeChanged(false);
					break;
				case LinearGradientBrush linearGradientBrush:
					SetStyle("background", linearGradientBrush.ToCssString(RenderSize));
					RecalculateBrushOnSizeChanged(true);
					break;
				default:
					ResetStyle("background-color");
					ResetStyle("background");
					RecalculateBrushOnSizeChanged(false);
					break;
			}
		}

		private static SizeChangedEventHandler _onSizeChangedForBrushCalculation = (sender, args) =>
		{
			var fe = sender as FrameworkElement;
			fe.SetBackgroundBrush(fe.Background);
		};

		private bool onSizeChangedForBrushCalculatioSet = false;

		private void RecalculateBrushOnSizeChanged(bool shouldRecalculate)
		{
			if (onSizeChangedForBrushCalculatioSet == shouldRecalculate)
			{
				return;
			}

			if (shouldRecalculate)
			{
				SizeChanged += _onSizeChangedForBrushCalculation;
			}
			else
			{
				SizeChanged -= _onSizeChangedForBrushCalculation;
			}

			onSizeChangedForBrushCalculatioSet = shouldRecalculate;
		}

		#endregion

		#region IsEnabled DependencyProperty

		public event DependencyPropertyChangedEventHandler IsEnabledChanged;

		public bool IsEnabled
		{
			get { return (bool)GetValue(IsEnabledProperty); }
			set { SetValue(IsEnabledProperty, value); }
		}

		public static readonly DependencyProperty IsEnabledProperty =
			DependencyProperty.Register(
				"IsEnabled",
				typeof(bool),
				typeof(FrameworkElement),
				new FrameworkPropertyMetadata(
					true,
					FrameworkPropertyMetadataOptions.Inherits,
					(s, e) =>
					{
						var elt = (FrameworkElement)s;
						elt?.OnIsEnabledChanged((bool)e.OldValue, (bool)e.NewValue);
						elt?.IsEnabledChanged?.Invoke(s, e);
					}
				)
	);

		protected virtual void OnIsEnabledChanged(bool oldValue, bool newValue)
		{
			UpdateHitTest();

			// TODO: move focus elsewhere if control.FocusState != FocusState.Unfocused
			if (FeatureConfiguration.UIElement.AssignDOMXamlProperties)
			{
				UpdateDOMProperties();
			}
		}

		#endregion

		public int? RenderPhase
		{
			get => throw new NotImplementedException();
			set => throw new NotImplementedException();
		}

		public void ApplyBindingPhase(int phase) => throw new NotImplementedException();
	}
}
