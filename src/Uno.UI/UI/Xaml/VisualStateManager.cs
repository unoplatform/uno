using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.Diagnostics.Eventing;
using System.Globalization;

namespace Microsoft.UI.Xaml
{
	public partial class VisualStateManager : DependencyObject
	{
		private static readonly IEventProvider _trace = Tracing.Get(TraceProvider.Id);
		private static readonly Logger _log = typeof(VisualStateManager).Log();

		public static class TraceProvider
		{
			public static readonly Guid Id = Guid.Parse("{2F38E5F4-90A2-4872-BD49-3696F897BAD1}");

			public const int StoryBoard_GoToState = 1;
		}

		public VisualStateManager()
		{
			IsAutoPropertyInheritanceEnabled = false;
			InitializeBinder();
		}

		#region VisualStateGroups Attached Property
		internal static IList<VisualStateGroup> GetVisualStateGroups(IFrameworkElement obj)
			=> (IList<VisualStateGroup>)obj.GetValue(VisualStateGroupsProperty);
		public static IList<VisualStateGroup> GetVisualStateGroups(FrameworkElement obj)
			=> (IList<VisualStateGroup>)obj.GetValue(VisualStateGroupsProperty);

		public static void SetVisualStateGroups(FrameworkElement obj, IList<VisualStateGroup> value)
		{
			obj.SetValue(VisualStateGroupsProperty, value);
		}

		public static DependencyProperty VisualStateGroupsProperty
		{
			[DynamicDependency(nameof(GetVisualStateGroups))]
			[DynamicDependency(nameof(SetVisualStateGroups))]
			get;
		} = DependencyProperty.RegisterAttached(
				"VisualStateGroups",
				typeof(IList<VisualStateGroup>),
				typeof(VisualStateManager),
				new FrameworkPropertyMetadata(
					defaultValue: Array.Empty<VisualStateGroup>(),
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

		internal static DependencyProperty VisualStateManagerProperty
		{
			[DynamicDependency(nameof(GetVisualStateManager))]
			[DynamicDependency(nameof(SetVisualStateManager))]
			get;
		} = DependencyProperty.RegisterAttached(
				"VisualStateManager",
				typeof(VisualStateManager),
				typeof(VisualStateManager),
				new FrameworkPropertyMetadata(null));

		#endregion

		#region CustomVisualStateManager Attached Property
		public static DependencyProperty CustomVisualStateManagerProperty
		{
			[DynamicDependency(nameof(GetCustomVisualStateManager))]
			[DynamicDependency(nameof(SetCustomVisualStateManager))]
			get;
		} = DependencyProperty.RegisterAttached(
				"CustomVisualStateManager", typeof(VisualStateManager),
				typeof(VisualStateManager),
				new FrameworkPropertyMetadata(default(VisualStateManager)));

		public static VisualStateManager GetCustomVisualStateManager(FrameworkElement obj)
			=> (VisualStateManager)obj.GetValue(CustomVisualStateManagerProperty);

		public static void SetCustomVisualStateManager(FrameworkElement obj, VisualStateManager value)
			=> obj.SetValue(CustomVisualStateManagerProperty, value);
		#endregion

		public static bool GoToState(Control control, string stateName, bool useTransitions)
		{
			var templateRoot = control.GetTemplateRoot();
			if (templateRoot is null)
			{
				if (_log.IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					_log.DebugFormat("Failed to set state [{0}], unable to find template root on [{1}]", stateName, control);
				}

				return false;
			}

			if (templateRoot is FrameworkElement fe)
			{
				if (fe.GoToElementState(stateName, useTransitions))
				{
					if (_log.IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
					{
						_log.DebugFormat($"GoToElementStateCore({stateName}) override on [{control}]");
					}

					return true;
				}
			}

			var groups = GetVisualStateGroups(templateRoot);
			if (groups is null)
			{
				if (_log.IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					_log.DebugFormat("Failed to set state [{0}], no visual state group on [{1}]", stateName, control);
				}

				return false;
			}

			// Get all the groups with a state that matches the state name
			var (group, state) = GetValidGroupAndState(stateName, groups);
			if (group is null)
			{
				if (_log.IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					_log.DebugFormat("Failed to set state [{0}], there are no matching groups on [{1}]", stateName, control);
				}

				return false;
			}

#if __WASM__
			TryAssignDOMVisualStates(groups, templateRoot);
#endif

			if (templateRoot is not FrameworkElement fwRoot)
			{
				// For backward compatibility!
				return GetVisualStateManager(control).GoToStateCorePrivateBaseImplementation(control, group, state, useTransitions);
			}

			// Note: We resolve the 'CustomVisualStateManager' on the 'fwRoot' like UWP,
			//		 but for compatibility reason we resolve the default visual state manager on the 'control' itself.
			//		 We should validate the behavior on UWP, including for controls that does not have templates!
			var vsm = GetCustomVisualStateManager(fwRoot) ?? GetVisualStateManager(control);

			return vsm.GoToStateCore(control, fwRoot, stateName, group, state, useTransitions);
		}

		protected virtual bool GoToStateCore(Control control, FrameworkElement templateRoot, string stateName, VisualStateGroup group, VisualState state, bool useTransitions)
			=> GoToStateCorePrivateBaseImplementation(control, group, state, useTransitions);

		private bool GoToStateCorePrivateBaseImplementation(Control control, VisualStateGroup group, VisualState state, bool useTransitions)
		{
#if IS_UNO
			if (_trace.IsEnabled)
			{
				_trace.WriteEvent(
					TraceProvider.StoryBoard_GoToState,
					EventOpcode.Send,
					new[] {
						control.GetType()?.ToString(),
						control?.GetDependencyObjectId().ToString(CultureInfo.InvariantCulture),
						state.Name,
						useTransitions ? "UseTransitions" : "NoTransitions"
					}
				);
			}
#endif

			var originalState = group.CurrentState;
			if (object.Equals(originalState, state))
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
				useTransitions,
				() =>
				{
					if (wr?.Target is Control)
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

			// Avoid using groups.FirstOrDefault as it incurs unnecessary Func<VisualStateGroup, bool> allocations
			var groups = GetVisualStateGroups(templateRoot);
			if (groups is null)
			{
				return null;
			}

			foreach (var group in groups)
			{
				if (group.Name == groupName)
				{
					return group.CurrentState;
				}
			}

			return null;
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

		internal static void InitializeStateTriggers(DependencyObject pDO, bool forceUpdate = false)
		{
			if (pDO is not Control pControl)
			{
				return;
			}

			var groupCollection = VisualStateManager.GetVisualStateGroups(pControl);
			if (groupCollection is null)
			{
				return;
			}

			// auto& groupCollectionMap = groupCollection->GetStateTriggerVariantMaps();
			// if (forceUpdate)
			// {
			// 	// Forcing an update, clear the map
			// 	groupCollectionMap.clear();
			//
			// 	// If we're forcing an update, we should also clear the state trigger variant maps in each group.
			// 	for (auto group : *groupCollection)
			// 	{
			// 		static_cast<CVisualStateGroup*>(group)->m_pStateTriggerVariantMap = nullptr;
			// 	}
			//
			// 	for (auto& groupContext : groupCollection->GetGroupContext())
			// 	{
			// 		groupContext.GetStateTriggerVariantMap() = nullptr;
			// 	}
			// }
			//
			// if (!groupCollectionMap.empty())
			// {
			// 	// If the map isn't clear, then we are already initialized or we weren't forced by diagnostics to update.
			// 	// Adding triggers isn't something usually supported at runtime.
			// 	return S_OK;
			// }
			//
			// auto dataSource = CreateVisualStateManagerDataSource(groupCollection);
			// auto dataSourceMap = dataSource->GetStateTriggerVariantMaps();
			//
			// if (!dataSourceMap.empty())
			// {
			// 	if (forceUpdate)
			// 	{
			// 		// Forcing an update, clear the map
			// 		dataSourceMap.clear();
			// 	}
			//
			// 	if (!dataSourceMap.empty())
			// 	{
			// 		// If the map isn't clear, then we are already initialized or we weren't forced by diagnostics to update.
			// 		// Adding triggers isn't something usually supported at runtime.
			// 		return S_OK;
			// 	}
			// }
			//
			// VisualStateManagerActuator actuator(
			// 	static_cast<CFrameworkElement*>(pControl->GetFirstChildNoAddRef()), dataSource.get());
			// IFC_RETURN(actuator.InitializeStateTriggers(pControl));

			// Uno Specific
			foreach (var visualStateGroup in GetVisualStateGroups(pControl))
			{
				visualStateGroup.OnOwnerElementLoaded(pControl, null);
				visualStateGroup.RefreshStateTriggers(forceUpdate);
			}
		}
	}
}
