using Windows.Foundation;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Numerics;
using System.Text;
using Windows.UI.Xaml.Markup;
using Uno.Extensions;
using Uno.Foundation.Logging;

#if __ANDROID__
using _View = Android.Views.View;
#elif __IOS__
using _View = UIKit.UIView;
#elif __MACOS__
using _View = AppKit.NSView;
#endif

namespace Windows.UI.Xaml.Media
{
	[ContentProperty(Name = nameof(Children))]
	public partial class TransformGroup : Transform
	{
		public TransformGroup()
		{
			Children = new TransformCollection();
		}

		/// <summary>
		/// Backing dependency property for the <see cref="Children"/>
		/// </summary>
		public static DependencyProperty ChildrenProperty { get; } =
			DependencyProperty.Register("Children", typeof(TransformCollection), typeof(TransformGroup), new FrameworkPropertyMetadata(OnChildrenChanged));

		public TransformCollection Children
		{
			get => (TransformCollection)this.GetValue(ChildrenProperty);
			set => this.SetValue(ChildrenProperty, value);
		}

		private static void OnChildrenChanged(DependencyObject dependencyobject, DependencyPropertyChangedEventArgs args)
			=> ((TransformGroup)dependencyobject).OnChildrenChanged(args);

		private void OnChildrenChanged(DependencyPropertyChangedEventArgs args)
		{
			if (args.OldValue is TransformCollection oldItems)
			{
				oldItems.CollectionChanged -= OnChildrenItemsChanged;
				foreach (var item in oldItems)
				{
					OnChildRemoved(item);
				}
			}

			if (args.NewValue is TransformCollection newItems)
			{
				newItems.CollectionChanged += OnChildrenItemsChanged;
				foreach (var item in newItems)
				{
					OnChildAdded(item);
				}
			}

			NotifyChanged();
		}

		private void OnChildrenItemsChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
				case NotifyCollectionChangedAction.Remove:
				case NotifyCollectionChangedAction.Replace:
					if (e.NewItems != null)
					{
						foreach (var child in e.NewItems)
						{
							if (child is Transform transform)
							{
								OnChildAdded(transform);
							}
						}
					}

					if (e.OldItems != null)
					{
						foreach (var child in e.OldItems)
						{
							if (child is Transform transform)
							{
								OnChildRemoved(transform);
							}
						}
					}
					break;
			}

			NotifyChanged();
		}

		private void OnChildAdded(Transform transform)
		{
#if __ANDROID__ || __IOS__ || __MACOS__
			transform.View = View; // Animation support
#endif
			transform.Changed += OnChildTransformChanged;
		}

		private void OnChildRemoved(Transform transform)
		{
#if __ANDROID__ || __IOS__ || __MACOS__
			transform.View = null; // Animation support
#endif
			transform.Changed -= OnChildTransformChanged;
		}

		private void OnChildTransformChanged(object sender, EventArgs e)
			=> NotifyChanged();

		public Matrix Value => new Matrix(MatrixCore);

		internal override Matrix3x2 ToMatrix(Point absoluteOrigin)
		{
			var matrix = Matrix3x2.Identity;
			if (Children != null)
			{
				foreach (var child in Children)
				{
					matrix *= child.ToMatrix(absoluteOrigin);
				}
			}

			return matrix;
		}

		#region Support of Animation of TransformGroup
		/*
		 * Animation are not running on the TransformGroup itself but on child transforms.
		 * We are only delegating the required properties for animation to children transforms here.
		 * All those can be safely removed once animation will not assume that a given Transform
		 * can be used only for a single element at a time!
		 */
#if __IOS__
		internal override bool IsAnimating => Children.Any(child => child.IsAnimating);
#elif __ANDROID__
		internal override bool IsAnimating
		{
			get => Children.Any(child => child.IsAnimating);
			set => this.Log().Error("Nothing is animatable on a TransformGroup.");
		}
#endif

#if __ANDROID__ || __IOS__ || __MACOS__
		internal override _View View
		{
			get => base.View;
			set
			{
				base.View = value;
				foreach (var child in Children)
				{
					child.View = value;
				}
			}
		}
#endif
		#endregion
	}
}

