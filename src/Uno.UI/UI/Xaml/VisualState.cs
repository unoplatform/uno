using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using Uno.UI.DataBinding;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media.Animation;
using Uno.UI.Xaml;

namespace Microsoft.UI.Xaml
{
	[ContentProperty(Name = nameof(Storyboard))]
	public sealed partial class VisualState : DependencyObject
	{
		/// <summary>
		/// Lazy builder provided by the source generator. Invoking this will
		/// optionally fill <see cref="Storyboard"/> and <see cref="Setters"/>.
		/// </summary>
		internal Action LazyBuilder { get; set; }
#if ENABLE_LEGACY_TEMPLATED_PARENT_SUPPORT
		internal bool? FromLegacyTemplate { get; set; }
#endif
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
			if (args.OldValue is DependencyObject oldStoryboard)
			{
				oldStoryboard.SetParent(null);
			}

			if (args.NewValue is DependencyObject newStoryboard)
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
		/// Ensures that the lazy builder has been invoked
		/// </summary>
		private void EnsureMaterialized()
		{
			if (LazyBuilder != null)
			{
				var builder = LazyBuilder;
				LazyBuilder = null;
				try
				{
#if ENABLE_LEGACY_TEMPLATED_PARENT_SUPPORT
					TemplatedParentScope.PushScope(this.GetTemplatedParent(), FromLegacyTemplate == true);
#endif
					builder.Invoke();
				}
				finally
				{
#if ENABLE_LEGACY_TEMPLATED_PARENT_SUPPORT
					TemplatedParentScope.PopScope();
#endif
				}

				// Establish the per-object theme on the lazily-built VisualState chain (storyboard,
				// animations, keyframes, setters); without it a keyframe keeps _theme=None and later
				// re-resolves its {ThemeResource} against the unscoped app base theme.
				// Read pinned-safe: while the owner is mid-walk its per-object theme is still the
				// pre-switch one, persisted only after the subtree completes (Theming.cpp:153-156).
				// Templated parent first, so the keyframes resolve under the same owner the state's
				// Setters do (VisualStateManager.cs:234 passes the Control into VisualStateGroup.GoToState,
				// which is what Setter.ApplyValue then receives). The groups owner is the fallback for
				// groups declared outside a ControlTemplate (MUX: CVisualStateManager2.cpp:345-354, :464).
				// TODO Uno: no WinUI counterpart for the notify itself — WinUI's fault-in only calls
				// CreateInstance (OptimizedVisualStateManagerDataSource.cpp:92-129); the chain is themed
				// when the VSM parents it (CVisualState::EnterImpl enters its storyboard,
				// components/vsm/VisualState.cpp:67-76, plus CDependencyObject::EnterImpl, depends.cpp:1044-1069).
				var groupsOwner = this.GetTemplatedParent() ?? Owner?.GetParent() as DependencyObject;
				var ownerTheme = ThemeResolution.ResolvePinnedOwnerTheme(groupsOwner);
				NotifyThemeChanged(ownerTheme, forceRefresh: true);
			}
		}

		private void OnStateTriggerCollectionChanged(IObservableVector<StateTriggerBase> sender, IVectorChangedEventArgs e)
		{
			Owner?.RefreshStateTriggers();
		}

		public override string ToString() => Name ?? $"<unnamed state {GetHashCode()}>";
	}
}
