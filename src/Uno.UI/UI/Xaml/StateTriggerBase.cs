using System;
using System.Runtime.InteropServices;
using Uno.Extensions;
using Uno.Logging;
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

			this.RegisterParentChangedCallback(
				key: this,
				handler: (instance, key, handler)
					=> OnOwnerChanged()
			);
		}

		protected void SetActive(bool isActive)
		{
			if (InternalIsActive == isActive)
			{
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().DebugFormat($"StateTrigger [{GetType().Name}] (owner={Owner?.Owner?.Name ?? "<null>"}/{Owner?.Name ?? "<null>"}) isActive:{isActive} [DUPLICATED: IGNORED]");
				}

				return; // nothing to do
			}

			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().DebugFormat($"StateTrigger [{GetType().Name}] (owner={Owner?.Owner?.Name ?? "<null>"}/{Owner?.Name ?? "<null>"}) isActive:{isActive}");
			}

			InternalIsActive = isActive;

			Owner?.Owner?.RefreshStateTriggers();
		}

		internal bool InternalIsActive { get; set; }

		internal VisualState Owner => this.GetParent() as VisualState;

		internal virtual void OnOwnerChanged()
		{

		}
	}
}
