#nullable enable

using System;
using System.Collections.Generic;
using Uno.Foundation.Extensibility;

namespace Windows.Devices.Haptics
{
	public partial class SimpleHapticsController
    {
		private ISimpleHapticsControllerExtension _simpleHapticsControllerExtension = null!;

		partial void InitPlatform()
		{
			if (!ApiExtensibility.CreateInstance(typeof(SimpleHapticsController), out _simpleHapticsControllerExtension))
			{
				throw new InvalidOperationException($"Unable to find ISimpleHapticsControllerExtension extension");
			}
		}

		public IReadOnlyList<SimpleHapticsControllerFeedback> SupportedFeedback =>
			_simpleHapticsControllerExtension.SupportedFeedback;

		public void SendHapticFeedback(SimpleHapticsControllerFeedback feedback)
		{
			if (feedback is null)
			{
				throw new ArgumentNullException(nameof(feedback));
			}

			_simpleHapticsControllerExtension.SendHapticFeedback(feedback);
		}
	}

	internal interface ISimpleHapticsControllerExtension
	{
		IReadOnlyList<SimpleHapticsControllerFeedback> SupportedFeedback { get; }

		void SendHapticFeedback(SimpleHapticsControllerFeedback feedback);
	}
}
