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

using ICommand = System.Windows.Input.ICommand;
using EventHandler = System.EventHandler;

namespace Uno.UI.Samples.UITests.ImageTests.Models
{
	public class ImageWithLateSourceViewModel : ViewModelBase
	{
		public ImageWithLateSourceViewModel(CoreDispatcher dispatcher) : base(dispatcher)
		{
		}

		private const string FinalSource = "http://lh5.ggpht.com/lxBMauupBiLIpgOgu5apeiX_YStXeHRLK1oneS4NfwwNt7fGDKMP0KpQIMwfjfL9GdHRVEavmg7gOrj5RYC4qwrjh3Y0jCWFDj83jzg";
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

		public string SourceUriImmediate => FinalSource;

		public ICommand SetSource => GetOrCreateCommand(OnSetSource);

		private void OnSetSource()
		{
			const string source = FinalSource;
			this.Log().Warn("Setting image source to " + source);

			SourceUri = source;
		}
	}
}
