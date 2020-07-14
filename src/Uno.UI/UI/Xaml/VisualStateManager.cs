using Windows.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno.Extensions;
using Uno.Logging;
using Uno.Diagnostics.Eventing;

namespace Windows.UI.Xaml
{
	public partial class VisualStateManager : DependencyObject
	{
		private readonly static IEventProvider _trace = Tracing.Get(TraceProvider.Id);

		public static class TraceProvider
		{
			public readonly static Guid Id = Guid.Parse("{2F38E5F4-90A2-4872-BD49-3696F897BAD1}");

			public const int StoryBoard_GoToState = 1;
		}

		public VisualStateManager()
		{
			IsAutoPropertyInheritanceEnabled = false;
			InitializeBinder();
		}

		#region VisualStateGroups Attached Property
		public static IList<VisualStateGroup> GetVisualStateGroups(IFrameworkElement obj)
			=> (IList<VisualStateGroup>)obj.GetValue(VisualStateGroupsProperty);

		public static IList<VisualStateGroup> GetVisualStateGroups(FrameworkElement obj)
			=> (IList<VisualStateGroup>)obj.GetValue(VisualStateGroupsProperty);

		public static void SetVisualStateGroups(IFrameworkElement obj, IList<VisualStateGroup> value)
		{
			obj.SetValue(VisualStateGroupsProperty, value);
		}

		// Using a DependencyProperty as the backing store for VisualStateGroups.  This enables animation, styling, binding, etc...
		public static DependencyProperty VisualStateGroupsProperty { get ; } =
			DependencyProperty.RegisterAttached(
				"VisualStateGroups",
				typeof(IList<VisualStateGroup>),
				typeof(VisualStateManager),
				new FrameworkPropertyMetadata(
					defaultValue: null,
					options: FrameworkPropertyMetadataOptions.ValueInheritsDataContext,
					propertyChangedCallback: OnVisualStateGroupsChanged
				)
			);

		private static void OnVisualStateGroupsChanged(object sender, DependencyPropertyChangedEventArgs args)
		{
			if (sender is IFrameworkElement fe)
			{
				if (args.OldValue is IList<VisualStateGroup> oldGroups)
				{
					foreach (VisualStateGroup group in oldGroups)
					{
						group.SetParent(null);
					}
				}

				if (args.NewValue is IList<VisualStateGroup> newGroups)
				{
					foreach (VisualStateGroup group in newGroups)
					{
						group.SetParent(fe);
					}
				}
			}
		}
		#endregion

		#region VisualStateManager Attached Property

		internal static VisualStateManager GetVisualStateManager(IFrameworkElement obj)
		{
			var value = (VisualStateManager)obj.GetValue(VisualStateManagerProperty);

			if (value == null)
			{
				obj.SetValue(VisualStateManagerProperty, value = new VisualStateManager());
			}

			return value;
		}

		internal static void SetVisualStateManager(IFrameworkElement obj, VisualStateManager value)
		{
			obj.SetValue(VisualStateManagerProperty, value);
		}

		internal static DependencyProperty VisualStateManagerProperty { get ; } =
			DependencyProperty.RegisterAttached("VisualStateManager", typeof(VisualStateManager), typeof(VisualStateManager), new PropertyMetadata(null));

		#endregion

		public static bool GoToState(Control control, string stateName, bool useTransitions)
		{
			var templateRoot = control.GetTemplateRoot();

			if (templateRoot == null)
			{
				if (typeof(VisualStateManager).Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					typeof(VisualStateManager).Log().DebugFormat("Failed to set state [{0}], unable to find template root on [{1}]", stateName, control);
				}

				return false;
			}

			if (templateRoot is FrameworkElement fe)
			{
				if (fe.GoToElementState(stateName, useTransitions))
				{
					if (typeof(VisualStateManager).Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
					{
						typeof(VisualStateManager).Log().DebugFormat($"GoToElementStateCore({stateName}) override on [{control}]");
					}

					return true;
				}
			}

			var groups = GetVisualStateGroups(templateRoot);

			if (groups == null)
			{
				if (typeof(VisualStateManager).Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					typeof(VisualStateManager).Log().DebugFormat("Failed to set state [{0}], no visual state group on [{1}]", stateName, control);
				}

				return false;
			}

			// Get all the groups with a state that matches the state name
			var (group, state) = GetValidGroupAndState(stateName, groups);

			if (group == null)
			{
				if (typeof(VisualStateManager).Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					typeof(VisualStateManager).Log().DebugFormat("Failed to set state [{0}], there are no matching groups on [{1}]", stateName, control);
				}

				return false;
			}

			var vsm = GetVisualStateManager(control);

			if (vsm == null)
			{
				if (typeof(VisualStateManager).Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					typeof(VisualStateManager).Log().DebugFormat("Failed to set state [{0}], there is no VisualStateManagr on [{1}]", stateName, control);
				}

				return false;
			}

			return vsm.GoToStateCore(control, templateRoot, stateName, group, state, useTransitions);
		}

		private static (VisualStateGroup, VisualState) GetValidGroupAndState(string stateName, IList<VisualStateGroup> groups)
		{
			foreach (var group in groups)
			{
				foreach (var state in group.States)
				{
					if (state.Name?.Equals(stateName) ?? false)
					{
						return (group, state);
					}
				}
			}

			return (null, null);
		}

		protected virtual bool GoToStateCore(Control control, IFrameworkElement templateRoot, string stateName, VisualStateGroup group, VisualState state, bool useTransitions)
		{
#if IS_UNO
			if (_trace.IsEnabled)
			{
				_trace.WriteEvent(
					TraceProvider.StoryBoard_GoToState,
					EventOpcode.Send,
					new[] {
						control.GetType()?.ToString(),
						control?.GetDependencyObjectId().ToString(),
						stateName,
						useTransitions ? "UseTransitions" : "NoTransitions"
					}
				);
			}
#endif

			var originalState = group.CurrentState;

			if (VisualState.Equals(originalState, state))
			{
				// Already in the right state
				return true;
			}

			RaiseCurrentStateChanging(group, originalState, state);

			// The visual state group must not keep a hard reference to the control, 
			// otherwise it may leak.
			var wr = Uno.UI.DataBinding.WeakReferencePool.RentWeakReference(this, control);

			group.GoToState(
				control,
				state,
				originalState,
				useTransitions,
				() =>
				{
					var innerControl = wr?.Target as Control;

					if (innerControl != null)
					{
						RaiseCurrentStateChanged(group, originalState, state);
					}
				}
			);

			return true;
		}

		protected virtual void RaiseCurrentStateChanging(VisualStateGroup stateGroup, VisualState oldState, VisualState newState)
		{
			if (stateGroup == null)
			{
				return;
			}

			stateGroup.RaiseCurrentStateChanging(oldState, newState);
		}

		protected virtual void RaiseCurrentStateChanged(VisualStateGroup stateGroup, VisualState oldState, VisualState newState)
		{
			if (stateGroup == null)
			{
				return;
			}

			stateGroup.RaiseCurrentStateChanged(oldState, newState);
		}

		internal static VisualState GetCurrentState(Control control, string groupName)
		{
			var templateRoot = control.GetTemplateRoot();
			if (templateRoot == null)
			{
				return null;
			}

			var group = GetVisualStateGroups(templateRoot)?
				.Where(g => g.Name == groupName)
				.FirstOrDefault();

			return group?.CurrentState;
		}
	}
}
