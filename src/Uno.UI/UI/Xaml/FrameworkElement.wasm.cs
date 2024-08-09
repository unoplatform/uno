using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Disposables;
using System.Text;
using System.Threading.Tasks;
using Uno.Extensions;
using Uno;
using Uno.Foundation.Logging;
using Windows.UI.Xaml.Controls;
using Windows.Foundation;
using View = Windows.UI.Xaml.UIElement;
using System.Collections;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml.Media;
using Uno.UI;
using Uno.UI.Xaml;
using Windows.UI;
using System.Dynamic;
using Windows.UI.Xaml.Shapes;
using Uno.UI.Xaml.Controls;

namespace Windows.UI.Xaml
{
	public partial class FrameworkElement : IEnumerable
	{
		bool IFrameworkElementInternal.HasLayouter => true;

		internal bool HasParent()
			=> Parent != null;

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

		private bool IsTopLevelXamlView() => throw new NotSupportedException();

		internal void SuspendRendering() => throw new NotSupportedException();

		internal void ResumeRendering() => throw new NotSupportedException();

		public IEnumerator GetEnumerator() => _children.GetEnumerator();

		#region Name Dependency Property

		private void OnNameChanged(string oldValue, string newValue)
		{
			if (FrameworkElementHelper.IsUiAutomationMappingEnabled)
			{
				Windows.UI.Xaml.Automation.AutomationProperties.SetAutomationId(this, newValue);
			}

			if (FeatureConfiguration.UIElement.AssignDOMXamlName)
			{
				Uno.UI.Xaml.WindowManagerInterop.SetName(HtmlId, newValue);
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

#if DEBUG
		private void OnGenericPropertyUpdated(DependencyPropertyChangedEventArgs args)
		{
			if (FeatureConfiguration.UIElement.AssignDOMXamlProperties)
			{
				UpdateDOMProperties();
			}
		}
#endif

		/// <summary>
		/// If corresponding feature flag is enabled, set layout properties as DOM attributes to aid in debugging.
		/// </summary>
		/// <remarks>
		/// Calls to this method should be wrapped in a check of the feature flag, to avoid the expense of a virtual method call
		/// that will most of the time do nothing in hot code paths.
		/// </remarks>
		private protected override void UpdateDOMProperties()
		{
			if (FeatureConfiguration.UIElement.AssignDOMXamlProperties && IsLoaded)
			{
				UpdateDOMXamlProperty(nameof(Margin), Margin);
				UpdateDOMXamlProperty(nameof(HorizontalAlignment), HorizontalAlignment);
				UpdateDOMXamlProperty(nameof(VerticalAlignment), VerticalAlignment);
				UpdateDOMXamlProperty(nameof(Width), Width);
				UpdateDOMXamlProperty(nameof(Height), Height);
				UpdateDOMXamlProperty(nameof(MinWidth), MinWidth);
				UpdateDOMXamlProperty(nameof(MinHeight), MinHeight);
				UpdateDOMXamlProperty(nameof(MaxWidth), MaxWidth);
				UpdateDOMXamlProperty(nameof(MaxHeight), MaxHeight);

				if (this is Control control)
				{
					UpdateDOMXamlProperty(nameof(Control.IsEnabled), control.IsEnabled);
				}

				if (this.TryGetPadding(out var padding))
				{
					UpdateDOMXamlProperty("Padding", padding);
				}

				base.UpdateDOMProperties();
			}
		}
	}
}
