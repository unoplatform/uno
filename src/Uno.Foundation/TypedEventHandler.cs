#nullable disable

using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.Foundation
{
	public delegate void TypedEventHandler<TSender, TResult>(
		TSender sender,
		TResult args
	);
}
