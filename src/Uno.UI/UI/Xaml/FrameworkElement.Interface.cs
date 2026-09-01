using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Collections.Generic;
using Windows.Foundation;
using System.Runtime.CompilerServices;
using System.Text;
using Uno;
using Uno.UI.Extensions;
using Uno.UI.Xaml;

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

		/// <summary>
		/// MUX Reference: FrameworkElement::FindNameImpl (FrameworkElement_partial.cpp:38-54) - a
		/// namescope lookup, not a tree walk: a name only resolves inside the scope it was registered in.
		/// </summary>
		/// <remarks>
		/// Uno has always walked the visual tree here, so a name declared inside a template resolves from
		/// outside it. That walk stays as a fallback behind
		/// <see cref="Uno.UI.FeatureConfiguration.FrameworkElement.UseLegacyFindNameTreeWalk"/> until the
		/// breaking-changes train can drop it; the namescope answer always wins.
		/// </remarks>
		public object FindName(string name)
		{
			if (this.GetContext().TryGetElementByName(name, this) is { } named)
			{
				return named;
			}

			return Uno.UI.FeatureConfiguration.FrameworkElement.UseLegacyFindNameTreeWalk
				? IFrameworkElementHelper.FindName(this, this, name)
				: null;
		}


		public void Dispose()
		{
		}

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
