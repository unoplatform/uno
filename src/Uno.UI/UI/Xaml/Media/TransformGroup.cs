using Windows.Foundation;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Numerics;
using System.Text;
using Windows.UI.Xaml.Markup;

namespace Windows.UI.Xaml.Media
{
	[ContentProperty(Name = "Children")]
	public partial class TransformGroup : Transform
	{
		public TransformGroup()
		{
			Children = new TransformCollection();
		}

		/// <summary>
		/// Backing dependency property for the <see cref="Children"/>
		/// </summary>
		public static readonly DependencyProperty ChildrenProperty =
			DependencyProperty.Register("Children", typeof(TransformCollection), typeof(TransformGroup), new PropertyMetadata(OnChildrenChanged));

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

		private void OnChildAdded(Transform transform) => transform.Changed += OnChildTransformChanged;
		private void OnChildRemoved(Transform transform) => transform.Changed -= OnChildTransformChanged;

		private void OnChildTransformChanged(object sender, EventArgs e) => NotifyChanged();

		internal override Matrix3x2 ToMatrix(Point absoluteOrigin)
		{
			var matrix = Matrix3x2.Identity;
			if (Children != null)
			{
				foreach (var child in Children)
				{
					matrix *= child.ToMatrix(absoluteOrigin);
					absoluteOrigin = new Point(
						(absoluteOrigin.X * matrix.M11) + (absoluteOrigin.Y * matrix.M21) + matrix.M31,
						(absoluteOrigin.X * matrix.M12) + (absoluteOrigin.Y * matrix.M22) + matrix.M32);
				}
			}

			return matrix;
		}
	}

}

