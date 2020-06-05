using System;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions;
using System.Linq;
using Uno.Disposables;
using Uno.Logging;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Automation.Peers;

namespace Windows.UI.Xaml.Controls
{
	public partial class RadioButton : ToggleButton
	{
		/// <summary>
		/// Groups all radioButtons of a given GroupName
		/// </summary>
		private static readonly Dictionary<string, List<WeakReference<RadioButton>>> _namedGroups = new Dictionary<string, List<WeakReference<RadioButton>>>();

		private readonly SerialDisposable _groupMembership = new SerialDisposable();

		public RadioButton()
		{
			InitializeVisualStates();

			// When a Radio button is checked, clicking it again won't uncheck it.
			CanRevertState = false;
			DefaultStyleKey = typeof(RadioButton);
		}

		protected override void OnIsCheckedChanged(bool? oldValue, bool? newValue)
		{
			// Uncheck the others from the group first, so that the checked event handler
			// can get the final state ohe the group.
			if (newValue.HasValue && newValue.Value)
			{
				UncheckOthersFromGroup();
			}
			
			base.OnIsCheckedChanged(oldValue, newValue);
		}

		private void UncheckOthersFromGroup()
		{
			//If GroupName is set, the group is all RadioButtons with the same GroupName, else it is other RadioButtons with the same logical parent
			var group = GroupName.IsNullOrEmpty() ?
				GetOtherHierarchicalGroupMembers() :
				GetOtherNamedGroupMembers();

			foreach(var rb in group)
			{
				if(this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().DebugFormat("Unchecking radio [{0}/{1}] in group [{2}]", rb.ToString(), rb.Name, GroupName);
				}

				rb.IsChecked = false;
			}
		}

		private IEnumerable<RadioButton> GetOtherNamedGroupMembers()
		{
			var group = _namedGroups.FindOrCreate(GroupName, () => new List<WeakReference<RadioButton>>());
			return group
				.Select(wr => wr.GetTarget())
				.Trim()
				.Where(rb => rb != this);
		}

		public string GroupName
		{
			get { return (string)GetValue(GroupNameProperty); }
			set { SetValue(GroupNameProperty, value); }
		}

		// Using a DependencyProperty as the backing store for GroupName.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty GroupNameProperty =
			DependencyProperty.Register(
				"GroupName", 
				typeof(string), 
				typeof(RadioButton), 
				new PropertyMetadata(
					(string)null, 
					propertyChangedCallback: (s, e) => (s as RadioButton)?.OnGroupNameChanged((string)e.OldValue, (string)e.NewValue)
				)
			);

		private void OnGroupNameChanged(string oldValue, string newValue)
		{
			RegisterInGroup(this, newValue).DisposeWith(_groupMembership);
		}

		private static IDisposable RegisterInGroup(RadioButton button, string groupName)
		{
			if (button == null || groupName.IsNullOrEmpty())
			{
				return Disposable.Empty;
			}

			CleanGroupReferences();

			var group = _namedGroups.FindOrCreate(groupName, () => new List<WeakReference<RadioButton>>());
			var weakRef = new WeakReference<RadioButton>(button);
			group.Add(weakRef);

			return Disposable.Create(() => group.Remove(weakRef));
		}

		/// <summary>
		/// Removes stale WeakReferences from _namedGroups to avoid a memory leak
		/// </summary>
		private static void CleanGroupReferences()
		{
			foreach (var group in _namedGroups.Values)
			{
				var staleReferences = group.Where(wr => !wr.HasTarget()).ToList();
				foreach (var sr in staleReferences)
				{
					group.Remove(sr);
				}
			}
		}

		protected override void OnLoaded()
		{
			base.OnLoaded();

			RegisterInGroup(this, GroupName).DisposeWith(_groupMembership);
		}

		protected override void OnUnloaded()
		{
			base.OnUnloaded();

			_groupMembership.Disposable = null;
		}

		protected override AutomationPeer OnCreateAutomationPeer()
		{
			return new RadioButtonAutomationPeer(this);
		}
	}
}
