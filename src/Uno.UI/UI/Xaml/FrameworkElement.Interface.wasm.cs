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
using Windows.UI.Xaml.Shapes;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.UI;
using Uno.Disposables;
using Uno.UI.Xaml;

namespace Windows.UI.Xaml
{
	public partial class FrameworkElement : UIElement, IFrameworkElement
	{
		private SerialDisposable _backgroundSubscription;
		public T FindFirstParent<T>() where T : class => FindFirstParent<T>(includeCurrent: false);

		public T FindFirstParent<T>(bool includeCurrent) where T : class
		{
			var view = includeCurrent ? (DependencyObject)this : this.Parent;
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
		public global::System.Uri BaseUri { get; internal set; } = DefaultBaseUri;

		#region Transitions Dependency Property

		[GeneratedDependencyProperty(DefaultValue = null, ChangedCallback = true)]
		public static DependencyProperty TransitionsProperty { get; } = CreateTransitionsProperty();

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
		public static DependencyProperty BackgroundProperty { get; } = CreateBackgroundProperty();

		public Brush Background
		{
			get => GetBackgroundValue();
			set => SetBackgroundValue(value);
		}

		protected virtual void OnBackgroundChanged(DependencyPropertyChangedEventArgs e)
			// Warning some controls (eg. CalendarViewBaseItem) takes ownership of the background rendering.
			// They override the OnBackgroundChanged and explicitly do not invokes that base method.
			=> SetAndObserveBackgroundBrush(e.NewValue as Brush);

		private protected void SetAndObserveBackgroundBrush(Brush brush)
		{
			var subscription = _backgroundSubscription ??= new SerialDisposable();

			subscription.Disposable = null;
			subscription.Disposable = BorderLayerRenderer.SetAndObserveBackgroundBrush(this, brush);
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
