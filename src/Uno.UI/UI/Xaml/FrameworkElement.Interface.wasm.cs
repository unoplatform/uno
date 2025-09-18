using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Collections.Generic;
using Windows.Foundation;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Shapes;

using Uno;
using Uno.Extensions;
using Uno.UI;
using Uno.Disposables;
using Uno.UI.Xaml;
using Uno.Foundation.Logging;
using Uno.UI.Helpers;
using Uno.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml
{
	public partial class FrameworkElement : UIElement, IFrameworkElement
	{
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

		protected FrameworkElement() : this(DefaultHtmlTag, false)
		{
		}

		private protected FrameworkElement(string htmlTag) : this(htmlTag, false)
		{
		}

		private protected FrameworkElement(string htmlTag, bool isSvg) : base(htmlTag, isSvg)
		{
			Initialize();

			_log = this.Log();
			_logDebug = _log.IsEnabled(LogLevel.Debug) ? _log : null;
		}

		private new protected readonly Logger _log;
		private new protected readonly Logger _logDebug;

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
			=> IFrameworkElementHelper.FindName(this, this, name);


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
		{
		}
		#endregion

		[NotImplemented]
		public int? RenderPhase
		{
			get
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.FrameworkElement", "RenderPhase");
				return null;
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.FrameworkElement", "RenderPhase");
			}
		}

		public void ApplyBindingPhase(int phase) { }
	}
}
