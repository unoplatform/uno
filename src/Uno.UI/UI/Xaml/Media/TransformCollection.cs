using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Windows.UI.Xaml.Media
{
	public partial class TransformCollection : ObservableCollection<Transform>, IList<Transform>, IEnumerable<Transform>
	{
	}
}
