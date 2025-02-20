using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.UI.Xaml.Controls.Maps
{
	/// <summary>
	/// A set of Uno.UI specific APIs to be used by presenters
	/// </summary>
	public interface IUnoMapControl
	{
		void RaiseCenterChanged(object sender, object args);

		void RaiseZoomLevelChanged(object sender, object args);
	}
}
