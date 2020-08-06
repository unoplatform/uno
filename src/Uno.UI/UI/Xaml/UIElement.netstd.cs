using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Uno.Collections;
using Uno.Extensions;
using Uno.Foundation;
using Uno.Logging;
using Uno.UI;
using Uno.UI.Extensions;
using Uno.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.System;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Uno.Core.Comparison;

namespace Windows.UI.Xaml
{
	public partial class UIElement : DependencyObject
	{
		internal protected readonly ILogger _log;
		private protected readonly ILogger _logDebug;

		private readonly bool _isFrameworkElement;
		internal readonly MaterializableList<UIElement> _children = new MaterializableList<UIElement>();

		// Even if this a concept of FrameworkElement, the loaded state is handled by the UIElement in order to avoid
		// to cast to FrameworkElement each time a child is added or removed.
		internal bool IsLoaded { get; private protected set; }

		/// <summary>
		/// This flag is transiently set while element is 'loading' but not yet 'loaded'.
		/// </summary>
		internal bool IsLoading { get; private protected set; }

		private protected int Depth { get; private set; } = int.MinValue;

		internal static void LoadingRootElement(UIElement visualTreeRoot)
			=> visualTreeRoot.OnElementLoading(1);

		internal static void RootElementLoaded(UIElement visualTreeRoot)
			=> visualTreeRoot.OnElementLoaded();

		internal static void RootElementUnloaded(UIElement visualTreeRoot)
			=> visualTreeRoot.OnElementUnloaded();

		private protected virtual void OnElementLoading(int depth)
		{
			IsLoading = true;
			Depth = depth;

			foreach (var child in _children)
			{
				child.OnElementLoading(depth + 1);
			}
		}

		private protected virtual void OnElementLoaded()
		{
			IsLoading = false;
			IsLoaded = true;

			foreach (var child in _children)
			{
				child.OnElementLoaded();
			}
		}

		private protected virtual void OnElementUnloaded()
		{
			IsLoaded = false;
			Depth = int.MinValue;

			foreach (var child in _children)
			{
				child.OnElementUnloaded();
			}
		}

		private void OnAddingChild(UIElement child)
		{
			if (IsLoaded)
			{
				child.OnElementLoading(Depth + 1);
			}
		}

		private void OnChildAdded(UIElement child)
		{
			if (
#if __WASM__
				!FeatureConfiguration.FrameworkElement.WasmUseManagedLoadedUnloaded ||
#endif
				!IsLoaded
				|| !child._isFrameworkElement)
			{
				return;
			}

			if (child.IsLoaded)
			{
				this.Log().Error($"{this}: Inconsistent state: child {child} is already loaded (OnChildAdded)");
			}
			else
			{
				child.OnElementLoaded();
			}
		}

		private void OnChildRemoved(UIElement child)
		{
			if (
#if __WASM__
				!FeatureConfiguration.FrameworkElement.WasmUseManagedLoadedUnloaded ||
#endif
				!IsLoaded
				|| !child._isFrameworkElement)
			{
				return;
			}

			if (child.IsLoaded)
			{
				child.OnElementUnloaded();
			}
			else
			{
				this.Log().Error($"{this}: Inconsistent state: child {child} is not loaded (OnChildRemoved)");
			}
		}

		internal Point GetPosition(Point position, UIElement relativeTo)
			=> TransformToVisual(relativeTo).TransformPoint(position);
	}
}
