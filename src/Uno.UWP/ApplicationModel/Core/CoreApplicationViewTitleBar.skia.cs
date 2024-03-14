using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.ApplicationModel.Core;

partial class CoreApplicationViewTitleBar
{
	private bool _extendViewIntoTitleBar;

	public bool ExtendViewIntoTitleBar
	{
		get => _extendViewIntoTitleBar;
		set
		{
			_extendViewIntoTitleBar = value;
			ExtendViewIntoTitleBarChanged?.Invoke();
		}
	}
}
