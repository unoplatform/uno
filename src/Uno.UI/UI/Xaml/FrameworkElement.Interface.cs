using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using Windows.Foundation;
using System.Runtime.CompilerServices;
using System.Text;
using Uno;
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

		public object FindName(string name)
			=> IFrameworkElementHelper.FindName(this, this, name) ?? FindNameInNameScope(name);

		// WinUI's FindName is a lookup in the element's namescope, which also holds named non-FrameworkElement
		// objects (Run, brushes, MenuFlyout...). Uno's tree walk runs first so its existing results are kept.
		private object FindNameInNameScope(string name)
			=> NameScope.GetNameScope(this)?.FindName(name);

		// Template-part lookups: the inherited namescope would reach names of an outer page or template.
		internal object FindNameInSubtree(string name)
			=> IFrameworkElementHelper.FindName(this, this, name);


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
