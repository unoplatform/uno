using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Uno.Extensions;
using Uno.UI.Common;
using Uno.UI.Samples.UITests.Helpers;
using Windows.UI.Core;

using ICommand = System.Windows.Input.ICommand;
using EventHandler = System.EventHandler;

#if HAS_UNO
using Uno.Foundation.Logging;
#else
using Microsoft.Extensions.Logging;
using Uno.Logging;
#endif

namespace Uno.UI.Samples.UITests.ImageTests.Models
{
	internal class ImageWithLateSourceViewModel : ViewModelBase
	{
#if HAS_UNO
		private static readonly Logger _log = Uno.Foundation.Logging.LogExtensionPoint.Log(typeof(ImageWithLateSourceViewModel));
#else
		private static readonly ILogger _log = Uno.Extensions.LogExtensionPoint.Log(typeof(ImageWithLateSourceViewModel));
#endif

		public ImageWithLateSourceViewModel(Private.Infrastructure.UnitTestDispatcherCompat dispatcher) : base(dispatcher)
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
			_log.Warn("Setting image source to " + source);

			SourceUri = source;
		}
	}
}
