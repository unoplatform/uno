using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Uno.Disposables;
using System.Text;
using Uno.Extensions;
using System.Collections.Specialized;
using Uno.UI.Common;
using Uno;
using Windows.Foundation.Collections;

#if XAMARIN_ANDROID
using Android.Views;
using NativeView = Android.Views.View;
#elif XAMARIN_IOS
using NativeView = UIKit.UIView;
#elif __MACOS__
using NativeView = AppKit.NSView;
#else
using NativeView = Windows.UI.Xaml.UIElement;
#endif

namespace Windows.UI.Xaml.Controls
{
	public partial class Grid : Panel, IDisposable
	{
		private readonly static DependencyProperty[] ChildrenProperties;

		private readonly RowDefinitionCollection _rowDefinitions;
		private readonly ColumnDefinitionCollection _columnDefinitions;

		private readonly SerialDisposable _subscriptions;
		private readonly SerialDisposable _rowSubscriptions;
		private readonly SerialDisposable _columnSubscriptions;
		private readonly Dictionary<object, IDisposable> _childSubscriptions = new Dictionary<object, IDisposable>(ReferenceEqualityComparer<object>.Default);
		private readonly Dictionary<RowDefinition, IDisposable> _rowsHeightSubscriptions = new Dictionary<RowDefinition, IDisposable>(ReferenceEqualityComparer<DependencyObject>.Default);
		private readonly Dictionary<RowDefinition, IDisposable> _rowsMinHeightSubscriptions = new Dictionary<RowDefinition, IDisposable>(ReferenceEqualityComparer<DependencyObject>.Default);
		private readonly Dictionary<RowDefinition, IDisposable> _rowsMaxHeightSubscriptions = new Dictionary<RowDefinition, IDisposable>(ReferenceEqualityComparer<DependencyObject>.Default);
		private readonly Dictionary<ColumnDefinition, IDisposable> _columnsWidthSubscriptions = new Dictionary<ColumnDefinition, IDisposable>(ReferenceEqualityComparer<DependencyObject>.Default);
		private readonly Dictionary<ColumnDefinition, IDisposable> _columnsMinWidthSubscriptions = new Dictionary<ColumnDefinition, IDisposable>(ReferenceEqualityComparer<DependencyObject>.Default);
		private readonly Dictionary<ColumnDefinition, IDisposable> _columnsMaxWidthSubscriptions = new Dictionary<ColumnDefinition, IDisposable>(ReferenceEqualityComparer<DependencyObject>.Default);

		static Grid()
		{
			ChildrenProperties = new[] {
				RowProperty,
				ColumnProperty,
				RowSpanProperty,
				ColumnSpanProperty,
			};
		}

		public Grid() : base()
		{
			_rowDefinitions = new RowDefinitionCollection(this);
			_columnDefinitions = new ColumnDefinitionCollection(this);

			_subscriptions = new SerialDisposable();
			_rowSubscriptions = new SerialDisposable();
			_columnSubscriptions = new SerialDisposable();

			Children.CollectionChanged += Children_CollectionChanged;
		}

		private protected override void OnLoaded()
		{
			base.OnLoaded();

			ObserveGridDefinitions();

			// The two calls below use the InnerList property to be able to
			// enumerate collection using the allocation-less List<T>.Enumerator.
			ObserveRowDefinitions(RowDefinitions.InnerList);
			ObserveColumnDefinitions(ColumnDefinitions.InnerList);

			CreateAllChildrenPropertiesSubscription();
		}

		private protected override void OnUnloaded()
		{
			base.OnUnloaded();

			_subscriptions.Dispose();
			_rowSubscriptions.Dispose();
			_columnSubscriptions.Dispose();
			DisposeAllRowsColumnsSubscriptions();
			DisposeAllChildrenPropertiesSubscription();
		}

		protected override bool? IsWidthConstrainedInner(NativeView requester)
		{
			if (requester == null)
			{
				// The Grid itself called invalidate
				return this.IsWidthConstrainedSimple();
			}

			var column = GetColumn(requester);
			var columnSpan = GetColumnSpan(requester);
			columnSpan = Math.Max(columnSpan, 1);
			for (int i = column; i < column + columnSpan; i++)
			{
				if (i >= ColumnDefinitions.Count)
				{
					return this.IsWidthConstrainedSimple();
				}
				var width = ColumnDefinitions[i].Width;
				if (width.IsAuto||width.IsStar)
				{
					//At least one of the columns spanned by the requester is auto/star-sized, so width may change
					return false;
				}
			}
			for (int i=column;i<column+columnSpan;i++)
			{
				var width = ColumnDefinitions[i].Width;
				if (!width.IsAbsolute)
				{
					//At least one of the columns spanned by the requester has no fixed width, so we fall back on simple case
					return this.IsWidthConstrainedSimple();
				}
			}
			//All of the columns spanned by the requester have fixed width, so it is constrained
			return true;
		}

		protected override bool? IsHeightConstrainedInner(NativeView requester)
		{
			if (requester == null)
			{
				// The Grid itself called invalidate
				return this.IsHeightConstrainedSimple();
			}

			var row = GetRow(requester);
			var rowSpan = GetRowSpan(requester);
			rowSpan = Math.Max(rowSpan, 1);
			for (int i = row; i < row + rowSpan; i++)
			{
				if (i >= RowDefinitions.Count)
				{
					return this.IsHeightConstrainedSimple();
				}
				GridLength height = RowDefinitions[i].Height;
				if (height.IsAuto || height.IsStar)
				{
					//At least one of the rows spanned by the requester is auto/star-sized, so height may change
					return false;
				}
			}
			for (int i = row; i < row + rowSpan; i++)
			{
				GridLength height = RowDefinitions[i].Height;
				if (!height.IsAbsolute)
				{
					//At least one of the rows spanned by the requester has no fixed height, so we fall back on simple case
					return this.IsHeightConstrainedSimple();
				}
			}

			//All of the rows spanned by the requester have fixed height, so it is constrained
			return true;
		}

		public ColumnDefinitionCollection ColumnDefinitions => _columnDefinitions;

		public RowDefinitionCollection RowDefinitions => _rowDefinitions;

		private void ObserveGridDefinitions()
		{
			_rowDefinitions.CollectionChanged += OnRowDefinitions_CollectionChanged;
			_rowSubscriptions.Disposable = Disposable.Create(() => _rowDefinitions.CollectionChanged -= OnRowDefinitions_CollectionChanged);

			_columnDefinitions.CollectionChanged += OnColumnDefinitions_CollectionChanged;
			_columnSubscriptions.Disposable = Disposable.Create(() => _columnDefinitions.CollectionChanged -= OnColumnDefinitions_CollectionChanged);
		}

		private void OnColumnDefinitions_CollectionChanged(object sender, IVectorChangedEventArgs change)
		{
			this.InvalidateMeasure();
			ObserveColumnDefinitions(_columnDefinitions.InnerList);
		}

		private void OnRowDefinitions_CollectionChanged(object sender, IVectorChangedEventArgs e)
		{
			this.InvalidateMeasure();
			ObserveRowDefinitions(_rowDefinitions.InnerList);
		}

		private void DisposeAllRowsColumnsSubscriptions()
		{
			DisposeDefinitionsSubscriptions(_rowsHeightSubscriptions);
			DisposeDefinitionsSubscriptions(_rowsMinHeightSubscriptions);
			DisposeDefinitionsSubscriptions(_rowsMaxHeightSubscriptions);
			DisposeDefinitionsSubscriptions(_columnsWidthSubscriptions);
			DisposeDefinitionsSubscriptions(_columnsMinWidthSubscriptions);
			DisposeDefinitionsSubscriptions(_columnsMaxWidthSubscriptions);
		}

		private void ObserveRowDefinitions(List<RowDefinition> definitions)
		{
			ObserveDefinitions(_rowsHeightSubscriptions, definitions, RowDefinition.HeightProperty, OnGridDefinitionChanged);
			ObserveDefinitions(_rowsMinHeightSubscriptions, definitions, RowDefinition.MinHeightProperty, OnGridDefinitionChanged);
			ObserveDefinitions(_rowsMaxHeightSubscriptions, definitions, RowDefinition.MaxHeightProperty, OnGridDefinitionChanged);
		}

		private void ObserveColumnDefinitions(List<ColumnDefinition> definitions)
		{
			ObserveDefinitions(_columnsWidthSubscriptions, definitions, ColumnDefinition.WidthProperty, OnGridDefinitionChanged);
			ObserveDefinitions(_columnsMinWidthSubscriptions, definitions, ColumnDefinition.MinWidthProperty, OnGridDefinitionChanged);
			ObserveDefinitions(_columnsMaxWidthSubscriptions, definitions, ColumnDefinition.MaxWidthProperty, OnGridDefinitionChanged);
		}

		/// <remarks>
		/// This method uses <see cref="List{T}"/> as an input in order to benefit from its allocation-less enumerator.
		/// </remarks>
		private static void ObserveDefinitions<T>(
			Dictionary<T, IDisposable> subcriptions,
			List<T> definitions,
			DependencyProperty property,
			PropertyChangedCallback callback
		) where T:DependencyObject
		{

			// Process new definitions
			foreach (var definition in definitions)
			{
				if (!subcriptions.TryGetValue(definition, out var disposable))
				{
					subcriptions[definition] =
						definition.RegisterDisposablePropertyChangedCallback(property, callback);
				}
			}

			// Removed definitions
			var definitionSet = new HashSet<T>(definitions, ReferenceEqualityComparer<T>.Default);

			foreach (var existing in subcriptions.ToArray())
			{
				if (!definitionSet.Contains(existing.Key))
				{
					existing.Value.Dispose();

					subcriptions.Remove(existing.Key);
				}
			}
		}

		private static void DisposeDefinitionsSubscriptions<T>(Dictionary<T, IDisposable> subcriptions)
		{
			foreach (var pair in subcriptions.ToArray())
			{
				pair.Value.Dispose();
			}

			subcriptions.Clear();
		}

		private void OnGridDefinitionChanged(object dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			this.InvalidateMeasure();
		}

		private void OnChildGridPropertyChanged(Uno.UI.DataBinding.ManagedWeakReference instance, DependencyProperty property, DependencyPropertyChangedEventArgs args)
		{
			for (int i = 0; i < ChildrenProperties.Length; i++)
			{
				if(ChildrenProperties[i] == property)
				{
					this.InvalidateMeasure();
				}
			}
		}

		private void Children_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Replace:
				case NotifyCollectionChangedAction.Add:
				case NotifyCollectionChangedAction.Remove:
					if (e.NewItems != null) //As per windows implementation, we cannot assume that NewItems is set
					{
						foreach (NativeView newItems in e.NewItems)
						{
							CreateGridChildPropertiesSubscription(newItems);
						}
					}

					if (e.OldItems != null) //As per windows implementation, we cannot assume that OldItems is set
					{
						foreach (NativeView oldItems in e.OldItems)
						{
							DisposeGridChildPropertiesSubscription(oldItems);
						}
					}
					break;
			}
		}

		private void CreateAllChildrenPropertiesSubscription()
		{
			foreach (var child in Children)
			{
				CreateGridChildPropertiesSubscription(child);
			}
		}

		private void DisposeAllChildrenPropertiesSubscription()
		{
			foreach (var child in Children)
			{
				DisposeGridChildPropertiesSubscription(child);
			}
		}

		/// <summary>
		/// Registers to all the attached properties on the children, so that 
		/// the control is invalidated when their value changes.
		/// </summary>
		/// <param name="child"></param>
		private void CreateGridChildPropertiesSubscription(NativeView child)
		{
			if (!_childSubscriptions.ContainsKey(child))
			{
				// The tradeoff made here is to assume that registering to all property changes, and checking for DP
				// equality in the callback is less expensive that forcing the initialization of the four grid attached properties 
				// storage on all the children.
				_childSubscriptions[child] = 
					DependencyObjectExtensions
						.RegisterDisposablePropertyChangedCallback(child, OnChildGridPropertyChanged);
			}
		}

		/// <summary>
		/// Removes the attached properties registrations for the specified object.
		/// </summary>
		/// <param name="view"></param>
		private void DisposeGridChildPropertiesSubscription(NativeView view)
		{
			IDisposable d;

			if (_childSubscriptions.TryGetValue(view, out d))
			{
				d.Dispose();
				_childSubscriptions.Remove(view);
			}
		}
	}
}
