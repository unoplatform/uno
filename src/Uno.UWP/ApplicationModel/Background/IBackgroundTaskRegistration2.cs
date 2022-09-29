#nullable disable

using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.ApplicationModel.Background
{
	public partial interface IBackgroundTaskRegistration2 : IBackgroundTaskRegistration
	{
		IBackgroundTrigger Trigger { get; }
	}
}
