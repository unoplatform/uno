using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace Uno.UI.Controls
{
	public interface IOnGridViewItemClickListener
	{
		void OnItemClick(int position, object item, View v);
		void OnItemLongClick(int position, object item, View v);
	}
}
