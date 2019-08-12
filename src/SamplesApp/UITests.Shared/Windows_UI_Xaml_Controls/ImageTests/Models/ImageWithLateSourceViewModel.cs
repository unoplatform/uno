using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Uno.Extensions;
using Uno.Logging;
using Uno.UI.Common;
using Uno.UI.Samples.UITests.Helpers;
using Windows.UI.Core;

namespace Uno.UI.Samples.UITests.ImageTests.Models
{
	public class ImageWithLateSourceViewModel : ViewModelBase
	{
		public ImageWithLateSourceViewModel(CoreDispatcher dispatcher) : base(dispatcher)
		{
		}

		private string _sourceUri;

		public string SourceUri
		{
			get => _sourceUri;
			set
			{
				_sourceUri = value;
				RaisePropertyChanged();
			}
		}

		public ICommand SetSource => GetOrCreateCommand(OnSetSource);

		private void OnSetSource()
		{
			const string source = "http://lh5.ggpht.com/lxBMauupBiLIpgOgu5apeiX_YStXeHRLK1oneS4NfwwNt7fGDKMP0KpQIMwfjfL9GdHRVEavmg7gOrj5RYC4qwrjh3Y0jCWFDj83jzg";
			this.Log().Warn("Setting image source to " + source);

			SourceUri = source;
		}
	}
}
