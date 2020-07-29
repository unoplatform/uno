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
using Uno.Disposables;
using Uno.UI.Xaml;

namespace Windows.UI.Xaml
{
	public partial class FrameworkElement : UIElement, IFrameworkElement
	{
		private readonly SerialDisposable _backgroundSubscription = new SerialDisposable();
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

		public FrameworkElement() : this(DefaultHtmlTag, false)
		{
		}

		public FrameworkElement(string htmlTag) : this(htmlTag, false)
		{
		}

		public FrameworkElement(string htmlTag, bool isSvg) : base(htmlTag, isSvg)
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

		private protected virtual void OnLoaded()
		{

		}

		private protected virtual void OnUnloaded()
		{

		}

		#region Transitions Dependency Property

		[GeneratedDependencyProperty(DefaultValue = null, ChangedCallback = true)]
		public static DependencyProperty TransitionsProperty { get ; } = CreateTransitionsProperty();

		public TransitionCollection Transitions
		{
			get => GetTransitionsValue();
			set => SetTransitionsValue(value);
		}

		private void OnTransitionsChanged(DependencyPropertyChangedEventArgs args)
		{

		}
		#endregion

		public object FindName(string name)
			=> IFrameworkElementHelper.FindName(this, GetChildren(), name);


		public void Dispose()
		{
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Size AdjustArrange(Size finalSize)
		{
			return finalSize;
		}

		#region Background DependencyProperty

		[GeneratedDependencyProperty(DefaultValue = null, ChangedCallback = true)]
		public static DependencyProperty BackgroundProperty { get ; } = CreateBackgroundProperty();

		public Brush Background
		{
			get => GetBackgroundValue();
			set => SetBackgroundValue(value);
		}

		protected virtual void OnBackgroundChanged(DependencyPropertyChangedEventArgs e)
		{
			_backgroundSubscription.Disposable = null;
			var brush = e.NewValue as Brush;
			SetBackgroundBrush(brush);

			if (brush is ImageBrush imgBrush)
			{
				RecalculateBrushOnSizeChanged(false);
				_backgroundSubscription.Disposable = imgBrush.Subscribe(img =>
				{
					switch (img.Kind)
					{
						case ImageDataKind.Empty:
						case ImageDataKind.Error:
							ResetStyle("background-color");
							ResetStyle("background-image");
							break;

						case ImageDataKind.Base64:
						case ImageDataKind.Url:
						default:
							ResetStyle("background-color");
							SetStyle("background-image", "url(" + img.Value + ")");
							break;
					}
				});
			}
			else
			{
				_backgroundSubscription.Disposable = Brush.AssignAndObserveBrush(brush, _ => SetBackgroundBrush(brush));
			}
		}

		private protected void SetBackgroundBrush(Brush brush)
		{
			switch (brush)
			{
				case SolidColorBrush solidColorBrush:
					var color = solidColorBrush.ColorWithOpacity;
					SetStyle("background-color", color.ToHexString());
					ResetStyle("background-image");
					RecalculateBrushOnSizeChanged(false);
					break;
				case GradientBrush gradientBrush:
					ResetStyle("background-color");
					SetStyle("background-image", gradientBrush.ToCssString(RenderSize));
					RecalculateBrushOnSizeChanged(true);
					break;
				default:
					ResetStyle("background-color");
					ResetStyle("background-image");
					RecalculateBrushOnSizeChanged(false);
					break;
			}
		}

		private static readonly SizeChangedEventHandler _onSizeChangedForBrushCalculation = (sender, args) =>
		{
			var fe = sender as FrameworkElement;
			fe.SetBackgroundBrush(fe.Background);
		};

		private bool _onSizeChangedForBrushCalculationSet = false;

		private void RecalculateBrushOnSizeChanged(bool shouldRecalculate)
		{
			if (_onSizeChangedForBrushCalculationSet == shouldRecalculate)
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

			_onSizeChangedForBrushCalculationSet = shouldRecalculate;
		}

		#endregion

		#region IsEnabled DependencyProperty

		public event DependencyPropertyChangedEventHandler IsEnabledChanged;

		[GeneratedDependencyProperty(DefaultValue = true, ChangedCallback = true, CoerceCallback = true, Options = FrameworkPropertyMetadataOptions.Inherits)]
		public static DependencyProperty IsEnabledProperty { get ; } = CreateIsEnabledProperty();

		public bool IsEnabled
		{
			get => GetIsEnabledValue();
			set => SetIsEnabledValue(value);
		}

		protected virtual void OnIsEnabledChanged(DependencyPropertyChangedEventArgs args)
		{
			OnIsEnabledChanged((bool)args.OldValue, (bool)args.NewValue);
			IsEnabledChanged?.Invoke(this, args);
		}

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
