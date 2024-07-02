#if DEBUG
#define ENABLE_CONTAINER_VISUAL_TRACKING
#endif

using System;
using System.Collections;
using Uno.UI;
using Uno.UI.Extensions;
using Uno.UI.Xaml;
using Windows.Foundation;

namespace Windows.UI.Xaml
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
				Windows.UI.Xaml.Automation.AutomationProperties.SetAutomationId(this, newValue);
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

#if ENABLE_CONTAINER_VISUAL_TRACKING // Make sure to update the Comment to have the valid depth
		partial void OnLoading()
		{
			if (_visual is not null)
			{
				_visual.Comment = $"{this.GetDebugDepth():D2}-{this.GetDebugName()}";
			}
		}
#endif
	}
}
