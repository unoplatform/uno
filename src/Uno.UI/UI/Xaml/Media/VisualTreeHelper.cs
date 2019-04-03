using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno.UI;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Uno.Extensions;
using Uno.Disposables;
#if XAMARIN_IOS
using UIKit;
using _View = UIKit.UIView;
using _ViewGroup = UIKit.UIView;
#elif __MACOS__
using AppKit;
using _View = AppKit.NSView;
using _ViewGroup = AppKit.NSView;
#elif XAMARIN_ANDROID
using _ViewGroup = Android.Views.ViewGroup;
using _View = Android.Views.ViewGroup;
#else
using _View = System.Object;
#endif

namespace Windows.UI.Xaml.Media
{
    public partial class VisualTreeHelper
    {
		private static List<WeakReference<IPopup>> _openPopups = new List<WeakReference<IPopup>>();

		internal static IDisposable RegisterOpenPopup(IPopup popup)
		{
			var weakPopup = new WeakReference<IPopup>(popup);

			_openPopups.AddDistinct(weakPopup);
			return Disposable.Create(() => _openPopups.Remove(weakPopup));
		}

		[Uno.NotImplemented]
		public static void DisconnectChildrenRecursive(UIElement element)
		{
			throw new NotSupportedException();
		}

		[Uno.NotImplemented]
		public static IEnumerable<UIElement> FindElementsInHostCoordinates(Point intersectingPoint, UIElement subtree)
		{
			throw new NotSupportedException();
		}

		[Uno.NotImplemented]
		public static IEnumerable<UIElement> FindElementsInHostCoordinates(Rect intersectingRect, UIElement subtree)
		{
			throw new NotSupportedException();
		}

		[Uno.NotImplemented]
		public static IEnumerable<UIElement> FindElementsInHostCoordinates(Point intersectingPoint, UIElement subtree, bool includeAllElements)
		{
			throw new NotSupportedException();
		}

		[Uno.NotImplemented]
		public static IEnumerable<UIElement> FindElementsInHostCoordinates(Rect intersectingRect, UIElement subtree, bool includeAllElements)
		{
			throw new NotSupportedException();
		}

		public static DependencyObject GetChild(DependencyObject reference, int childIndex)
		{
#if XAMARIN
			return (reference as _ViewGroup)?
				.GetChildren()
				.OfType<DependencyObject>()
				.ElementAtOrDefault(childIndex);
#else
			return (reference as UIElement)?
				.GetChildren()
				.OfType<DependencyObject>()
				.ElementAtOrDefault(childIndex);
#endif
		}

		public static int GetChildrenCount(DependencyObject reference)
		{
#if XAMARIN
			return (reference as _ViewGroup)?
				.GetChildren()
				.OfType<DependencyObject>()
				.Count() ?? 0;
#else
			return (reference as UIElement)?
				.GetChildren()
				.OfType<DependencyObject>()
				.Count() ?? 0;
#endif
		}

		internal static IReadOnlyList<IPopup> GetOpenPopups(Window window)
		{
			return _openPopups
				.Select(WeakReferenceExtensions.GetTarget)
				.Trim()
				.ToList()
				.AsReadOnly();
		}

		public static DependencyObject GetParent(DependencyObject reference)
		{
#if XAMARIN
			return (reference as _View)?
				.FindFirstParent<DependencyObject>();
#else
			return reference.GetParent() as DependencyObject;
#endif
		}

		internal static void CloseAllPopups()
		{
			foreach (var popup in GetOpenPopups(Window.Current))
			{
				popup.IsOpen = false;
			}
		}
	}
}
