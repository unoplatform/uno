using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Windows.ApplicationModel.Background;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Uno.UI.Extensions
{
	// Note: This file is included in Uno.UI ** AND ** linked in the toolkit for Windows Only

	/// <summary>
	/// Possible modes to enumerate the children of a tree
	/// </summary>
	internal enum TreeEnumerationMode
	{
		/// <summary>
		/// Enumerates visual tree branch per branch
		/// (i.e. go as deep as possible on the first branch before enumerating the second one etc.)
		/// </summary>
		Branch,

		/// <summary>
		/// Enumerates visual tree layer per layer
		/// (i.e. enumerates all direct children before enumerating sub-children of those etc.)
		/// </summary>
		Layer
	}

	internal static partial class DependencyObjectExtensions
	{
		/// <summary>
		/// ** Recursively ** gets an enumerable sequence of all the parent objects of a given element.
		/// Parents are ordered from bottom to the top, i.e. from direct parent to the root of the window.
		/// </summary>
		/// <param name="element">The element to search from</param>
		/// <param name="includeCurrent">Determines if the current <paramref name="element"/> should be included or not.</param>
		public static IEnumerable<DependencyObject> GetAllParents(this DependencyObject element, bool includeCurrent = true)
		{
			if (includeCurrent)
			{
				yield return element;
			}

			for (var parent = (element as FrameworkElement)?.Parent ?? VisualTreeHelper.GetParent(element);
				parent != null;
				parent = VisualTreeHelper.GetParent(parent))
			{
				yield return parent;
			}
		}

		/// <summary>
		/// Search for the first parent of the given type.
		/// </summary>
		/// <typeparam name="T">The type of child we are looking for</typeparam>
		/// <param name="element">The element to search from</param>
		/// <param name="includeCurrent">Determines if the current <paramref name="element"/> should be tested or not.</param>
		/// <returns>The first found parent that is of the given type.</returns>
		public static T FindFirstParent<T>(this DependencyObject element, bool includeCurrent = true)
			where T : DependencyObject
			=> element.GetAllParents(includeCurrent).OfType<T>().FirstOrDefault();

		/// <summary>
		/// Search for the first parent of the given type that is matching the given predicate.
		/// </summary>
		/// <typeparam name="T">The type of child we are looking for</typeparam>
		/// <param name="element">The element to search from</param>
		/// <param name="predicate">The predicate to use to find the right expected parent</param>
		/// <param name="includeCurrent">Determines if the current <paramref name="element"/> should be tested or not.</param>
		/// <returns>The first found parent that is of the given type.</returns>
		public static T FindFirstParent<T>(this DependencyObject element, Func<T, bool> predicate, bool includeCurrent = true)
			where T : DependencyObject
			=> element.GetAllParents(includeCurrent).OfType<T>().FirstOrDefault(predicate);

		/// <summary>
		/// Gets direct children of the given DependencyObject
		/// </summary>
		public static IEnumerable<DependencyObject> GetChildren(this DependencyObject obj, bool includeCurrent = false)
		{
			if (includeCurrent)
			{
				yield return obj;
			}

			var count = VisualTreeHelper.GetChildrenCount(obj);

			for (var i = 0; i < count; i++)
			{
				yield return VisualTreeHelper.GetChild(obj, i);
			}
		}

		/// <summary>
		/// **Recursively** get all children of the given DependencyObject
		/// </summary>
		public static IEnumerable<DependencyObject> GetAllChildren(
			this DependencyObject obj,
			int? childLevelLimit = null,
			bool includeCurrent = false,
			TreeEnumerationMode mode = TreeEnumerationMode.Branch)
			=> new VisualTreeEnumerable(obj, childLevelLimit, includeCurrent, mode);

		/// <summary>
		/// **Recursively** search for the first child of the given type.
		/// </summary>
		/// <typeparam name="T">The type of child we are looking for</typeparam>
		/// <param name="element">The element to search from</param>
		/// <param name="childLevelLimit">The max sub-level to dig into. Cf. Remarks</param>
		/// <param name="includeCurrent">Determines if the current <paramref name="element"/> should be tested or not.</param>
		/// <returns>The first found child that is of the given type.</returns>
		/// <remarks>
		/// It's always preferable to specify a <paramref name="childLevelLimit"/> in order to not enumerate the full visual tree
		/// while searching for an element. Note also that enumeration is achieve branch per branch, so no matter how the expected element
		/// is close to the <paramref name="element"/>, the children branches before it are going to be enumerated.
		/// </remarks>
		public static T FindFirstChild<T>(this DependencyObject element, int? childLevelLimit = null, bool includeCurrent = true)
			where T : DependencyObject
			=> element.GetAllChildren(childLevelLimit, includeCurrent).OfType<T>().FirstOrDefault();

		/// <summary>
		/// **Recursively** search for the first child of the given type that is matching the given predicate.
		/// </summary>
		/// <typeparam name="T">The type of child we are looking for</typeparam>
		/// <param name="element">The element to search from</param>
		/// <param name="predicate">The predicate to use to find the right expected child</param>
		/// <param name="childLevelLimit">The max sub-level to dig into. Cf. Remarks</param>
		/// <param name="includeCurrent">Determines if the current <paramref name="element"/> should be tested or not.</param>
		/// <returns>The first found child that is of the given type.</returns>
		/// <remarks>
		/// It's always preferable to specify a <paramref name="childLevelLimit"/> in order to not enumerate the full visual tree
		/// while searching for an element. Note also that enumeration is achieve branch per branch, so no matter how the expected element
		/// is close to the <paramref name="element"/>, the children branches before it are going to be enumerated.
		/// </remarks>
		public static T FindFirstChild<T>(this DependencyObject element, Func<T, bool> predicate, int? childLevelLimit = null, bool includeCurrent = true)
			where T : DependencyObject
			=> element.GetAllChildren(childLevelLimit, includeCurrent).OfType<T>().FirstOrDefault(predicate);

		private class VisualTreeEnumerable : IEnumerable<DependencyObject>
		{
			private readonly DependencyObject _obj;
			private readonly int? _childLevelLimit;
			private readonly bool _includeCurrent;
			private readonly TreeEnumerationMode _mode;

			public VisualTreeEnumerable(DependencyObject obj, int? childLevelLimit, bool includeCurrent, TreeEnumerationMode mode)
			{
				_obj = obj;
				_childLevelLimit = childLevelLimit;
				_includeCurrent = includeCurrent;
				_mode = mode;
			}

			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
			public IEnumerator<DependencyObject> GetEnumerator()
			{
				switch (_mode)
				{
					case TreeEnumerationMode.Layer:
						return new LayerEnumerator(_obj, _childLevelLimit ?? int.MaxValue, _includeCurrent);

					case TreeEnumerationMode.Branch:
					default:
						return new BranchEnumerator(_obj, _childLevelLimit ?? int.MaxValue, _includeCurrent);
				}
			}
		}

		private class BranchEnumerator : IEnumerator<DependencyObject>
		{
			private readonly DependencyObject _obj;
			private readonly int _childLevelLimit;
			private readonly int _count;

			private int _index;
			private BranchEnumerator _children;

			public BranchEnumerator(DependencyObject obj, int childLevelLimit, bool includeCurrent)
			{
				_obj = obj;
				_childLevelLimit = childLevelLimit;

				_index = includeCurrent ? -2 : -1;
				_count = VisualTreeHelper.GetChildrenCount(obj);
			}

			object IEnumerator.Current => Current;
			public DependencyObject Current { get; private set; }

			public bool MoveNext()
			{
				if (_children?.MoveNext() ?? false)
				{
					Current = _children.Current;
					return true;
				}
				else if (++_index == -1)
				{
					// The flag 'includeCurrent' was set
					Current = _obj;
					return true;
				}
				else if (_index < _count && _childLevelLimit > 0)
				{
					var child = VisualTreeHelper.GetChild(_obj, _index);

					_children = new BranchEnumerator(child, _childLevelLimit - 1, includeCurrent: true);
					return MoveNext();
				}
				else
				{
					Current = null;
					return false;
				}
			}

			public void Reset()
				=> throw new NotSupportedException();

			public void Dispose() { }
		}

		private class LayerEnumerator : IEnumerator<DependencyObject>
		{
			private readonly int _childLevelLimit;

			private Child _head, _tail;
			private int _index, _count;

			public LayerEnumerator(DependencyObject obj, int childLevelLimit, bool includeCurrent)
			{
				_childLevelLimit = childLevelLimit;
				_head = _tail = new Child(obj, 0);

				_index = includeCurrent ? -2 : -1;
				_count = childLevelLimit <= 0 ? 0 : VisualTreeHelper.GetChildrenCount(_head.Value);
			}

			object IEnumerator.Current => Current;
			public DependencyObject Current { get; private set; }

			public bool MoveNext()
			{
				if (_tail == null)
				{
					throw new ObjectDisposedException(nameof(LayerEnumerator));
				}

				if (++_index == -1)
				{
					// The flag 'includeCurrent' was set
					Current = _head.Value;
					return true;
				}

				// If no items remaining on current _head, try to move to the next element which has at least 1 child
				while (_head != null && _index >= _count)
				{
					_head = _head.Next;

					if (_head != null)
					{
						_index = 0;
						_count = VisualTreeHelper.GetChildrenCount(_head.Value);
					}
				}

				if (_head == null)
				{
					Current = null;
					return false;
				}
				else
				{
					var current = VisualTreeHelper.GetChild(_head.Value, _index);
					var level = _head.Level + 1;
					if (current != null && level < _childLevelLimit)
					{
						_tail = _tail.Next = new Child(current, level);
					}

					Current = current;
					return true;
				}
			}

			public void Reset()
				=> throw new NotSupportedException();

			public void Dispose()
				=> _head = _tail = null;

			private class Child
			{
				public Child(DependencyObject value, int level)
				{
					Value = value;
					Level = level;
				}

				public DependencyObject Value { get; }

				public int Level { get; }

				public Child Next { get; set; }
			}
		}
	}
}
