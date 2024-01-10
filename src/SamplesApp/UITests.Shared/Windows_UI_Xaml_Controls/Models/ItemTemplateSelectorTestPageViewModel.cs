
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uno.UI.Samples.UITests.Helpers;
using Windows.UI.Core;

namespace Uno.UI.Samples.Presentation.SamplePages
{
	internal partial class ItemTemplateSelectorTestPageViewModel : ViewModelBase
	{

		public ItemTemplateSelectorTestPageViewModel(Private.Infrastructure.UnitTestDispatcherCompat coreDispatcher) : base(coreDispatcher)
		{
			ListItemsObsStatic = GetSampleItemsSync();
		}

		public object ListItemsObsStatic { get; }


		private ListItem[] GetSampleItemsSync()
		{
			return ColourList;
		}

		public class ListItem
		{
			public string ColourString { get; set; }

			public override int GetHashCode()
			{
				return ColourString.GetHashCode();
			}

			public override string ToString()
			{
				return "ListItem: " + ColourString;
			}
		}

		private static readonly ListItem[] ColourList = new[]
			{
				new ListItem() {ColourString= "red1" },
				new ListItem() {ColourString= "Blue2" },
				new ListItem() {ColourString= "Green3" },
				new ListItem() {ColourString= "Red4" },
				new ListItem() {ColourString= "red5" },
				new ListItem() {ColourString= "Green6" },
				new ListItem() {ColourString= "Green7" },
				new ListItem() {ColourString= "Blue8" },
				new ListItem() {ColourString= "red9" },
				new ListItem() {ColourString= "Green10" },
				new ListItem() {ColourString= "Red11" },
				new ListItem() {ColourString= "Blue12" },
				new ListItem() {ColourString= "red13" },
				new ListItem() {ColourString= "Green14" },
				new ListItem() {ColourString= "Green15" },
				new ListItem() {ColourString= "Blue16" },
				new ListItem() {ColourString= "red17" },
				new ListItem() {ColourString= "Green18" },
				new ListItem() {ColourString= "Red19" },
				new ListItem() {ColourString= "Blue20" },
				new ListItem() {ColourString= "red21" },
				new ListItem() {ColourString= "Green22" },
				new ListItem() {ColourString= "Green23" },
				new ListItem() {ColourString= "Blue24" },
				new ListItem() {ColourString= "Green25" },
				new ListItem() {ColourString= "Red26" },
				new ListItem() {ColourString= "Blue27" },
				new ListItem() {ColourString= "red28" },
				new ListItem() {ColourString= "Green29" },
				new ListItem() {ColourString= "Green30" },
				new ListItem() {ColourString= "Blue31" },
				new ListItem() {ColourString= "red32" },
				new ListItem() {ColourString= "Green33" },
				new ListItem() {ColourString= "Red34" },
				new ListItem() {ColourString= "Blue35" },
				new ListItem() {ColourString= "red36" },
				new ListItem() {ColourString= "Green37" },
				new ListItem() {ColourString= "Green38" },
				new ListItem() {ColourString= "Green39" },
				new ListItem() {ColourString= "Red40" },
				new ListItem() {ColourString= "Blue41" },
				new ListItem() {ColourString= "red42" },
				new ListItem() {ColourString= "Green43" },
				new ListItem() {ColourString= "Green44" },
				new ListItem() {ColourString= "Blue45" },
				new ListItem() {ColourString= "red46" },
				new ListItem() {ColourString= "Green47" },
				new ListItem() {ColourString= "Red48" },
				new ListItem() {ColourString= "Blue49" },
				new ListItem() {ColourString= "red50" },
				new ListItem() {ColourString= "Green51" },
				new ListItem() {ColourString= "Green52" },
				new ListItem() {ColourString= "Blue53" },
			};
	}
}
