using System;
using System.Runtime.InteropServices;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Windows.Foundation;
using Windows.Foundation.Metadata;

namespace Windows.UI.Xaml
{
	public partial class StateTriggerBase : DependencyObject
	{
		protected StateTriggerBase()
		{
			InitializeBinder();

			IsAutoPropertyInheritanceEnabled = false;

			this.RegisterParentChangedCallbackStrong(
				key: this,
				handler: (instance, key, handler)
					=> OnOwnerChanged()
			);
		}

		protected void SetActive(bool IsActive)
		{
			SetActivePrecedence(IsActive ? StateTriggerPrecedence.CustomTrigger : StateTriggerPrecedence.Inactive);
		}

		internal void SetActivePrecedence(StateTriggerPrecedence precedence)
		{
			if (CurrentPrecedence == precedence)
			{
				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					this.Log().DebugFormat($"StateTrigger [{GetType().Name}] (owner={Owner?.Owner ?? (object)"<null>"}/{Owner ?? (object)"<null>"}) precedence:{precedence} [DUPLICATED: IGNORED]");
				}

				return; // nothing to do
			}

			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().DebugFormat($"StateTrigger [{GetType().Name}] (owner={Owner?.Owner ?? (object)"<null>"}/{Owner ?? (object)"<null>"}) precedence:{precedence}");
			}

			CurrentPrecedence = precedence;

			Owner?.Owner?.RefreshStateTriggers();
		}

		internal StateTriggerPrecedence CurrentPrecedence { get; set; }

		internal VisualState Owner => this.GetParent() as VisualState;

		internal virtual void OnOwnerChanged()
		{
		}

		internal virtual void OnOwnerElementChanged()
		{
		}

		internal virtual void OnOwnerElementLoaded()
		{
		}

		internal virtual void OnOwnerElementUnloaded()
		{
		}
	}
}
