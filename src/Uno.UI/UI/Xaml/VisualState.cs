using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using Uno.UI.DataBinding;
using Windows.Foundation.Collections;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media.Animation;
using Uno.UI.Xaml;

namespace Windows.UI.Xaml
{
	[ContentProperty(Name = nameof(Storyboard))]
	public sealed partial class VisualState : DependencyObject
	{
		private Action lazyBuilder;

		public VisualState()
		{
			InitializeBinder();
			IsAutoPropertyInheritanceEnabled = false;
		}

		public string Name { get; set; }

		#region StoryBoard DependencyProperty

		public Storyboard Storyboard
		{
			get
			{
				EnsureMaterialized();
				return (Storyboard)this.GetValue(StoryboardProperty);
			}

			set => this.SetValue(StoryboardProperty, value);
		}

		public static DependencyProperty StoryboardProperty { get; } =
			DependencyProperty.Register(
				"Storyboard",
				typeof(Storyboard),
				typeof(VisualState),
				new FrameworkPropertyMetadata(
					defaultValue: null,
					options: FrameworkPropertyMetadataOptions.ValueInheritsDataContext,
					propertyChangedCallback: OnStoryboardChanged
				)
			);

		private static void OnStoryboardChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			if (args.OldValue is IDependencyObjectStoreProvider oldStoryboard)
			{
				oldStoryboard.SetParent(null);
			}

			if (args.NewValue is IDependencyObjectStoreProvider newStoryboard)
			{
				newStoryboard.SetParent(dependencyObject);
			}
		}

		#endregion

		#region Setters DependencyProperty

		public SetterBaseCollection Setters
		{
			get
			{
				if (!(GetValue(SettersProperty) is SetterBaseCollection collection))
				{
					collection = Setters = new SetterBaseCollection(this, isAutoPropertyInheritanceEnabled: false);
				}

				EnsureMaterialized();

				return collection;
			}

			internal set => SetValue(SettersProperty, value);
		}

		internal static DependencyProperty SettersProperty { get; } =
			DependencyProperty.Register(
				name: "Setters",
				propertyType: typeof(SetterBaseCollection),
				ownerType: typeof(VisualState),
				typeMetadata: new FrameworkPropertyMetadata(defaultValue: null)
			);

		#endregion

		#region StateTriggers DependencyProperty

		public IList<StateTriggerBase> StateTriggers
		{
			get
			{
				if (!(GetValue(StateTriggersProperty) is IList<StateTriggerBase> list))
				{
					var stateTriggers = new DependencyObjectCollection<StateTriggerBase>(this, isAutoPropertyInheritanceEnabled: false);
					stateTriggers.VectorChanged += OnStateTriggerCollectionChanged;

					list = StateTriggers = stateTriggers;
				}

				return list;
			}

			internal set => SetValue(StateTriggersProperty, value);
		}

		internal static DependencyProperty StateTriggersProperty { get; } =
			DependencyProperty.Register(
				name: "StateTriggers",
				propertyType: typeof(IList<StateTriggerBase>),
				ownerType: typeof(VisualState),
				typeMetadata: new FrameworkPropertyMetadata(
					defaultValue: null,
					propertyChangedCallback: StateTriggersChanged
				)
			);

		#endregion

		private static void StateTriggersChanged(DependencyObject dependencyobject, DependencyPropertyChangedEventArgs args)
		{
			if (args.OldValue is IList<StateTriggerBase> oldTriggers)
			{
				foreach (var trigger in oldTriggers)
				{
					trigger.SetParent(null);
				}
			}

			if (args.NewValue is IList<StateTriggerBase> newTriggers)
			{
				foreach (var trigger in newTriggers)
				{
					trigger.SetParent(dependencyobject);
				}
			}
		}

		internal VisualStateGroup Owner => this.GetParent() as VisualStateGroup;

		/// <summary>
		/// Lazy builder provided by the source generator. Invoking this will
		/// optionally fill <see cref="Storyboard"/> and <see cref="Setters"/>.
		/// </summary>
		internal Action LazyBuilder
		{
			get => lazyBuilder;
			set => lazyBuilder = value;
		}

		/// <summary>
		/// Ensures that the lazy builder has been invoked
		/// </summary>
		private void EnsureMaterialized()
		{
			if (LazyBuilder != null)
			{
				var builder = LazyBuilder;
				LazyBuilder = null;
				builder.Invoke();


				// Resolve all theme resources from storyboard children
				// and setters values. This step is needed to ensure that
				// Theme Resources are resolved using the proper visual tree
				// parents, particularly when resources a locally overriden.
				this.UpdateResourceBindings();
			}
		}

		private void OnStateTriggerCollectionChanged(IObservableVector<StateTriggerBase> sender, IVectorChangedEventArgs e)
		{
			Owner?.RefreshStateTriggers();
		}

		public override string ToString() => Name ?? $"<unnamed state {GetHashCode()}>";
	}
}
