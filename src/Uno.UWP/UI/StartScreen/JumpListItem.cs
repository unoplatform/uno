#if __ANDROID__ || __IOS__
using System;

namespace Windows.UI.StartScreen
{
	public partial class JumpListItem
	{
		internal const string UnoShortcutKey = "UnoShortcut";
		internal const string ImagePathKey = "UnoLogoImagePath";
#if __ANDROID__
		internal const string ArgumentsExtraKey = "UnoArguments";
#endif

		private string _description = "";
		private string _displayName = "";
		private Uri? _logo;

		private JumpListItem(string arguments)
		{
			Arguments = arguments ?? throw new ArgumentNullException(nameof(arguments));
		}

		public Uri? Logo
		{
			get => _logo;
			set
			{
				if (value != null)
				{
					var isRelative = !value.IsAbsoluteUri;
					var wrongSchema = !value.Scheme.Equals(
						"ms-appx",
						StringComparison.InvariantCultureIgnoreCase);
					if (isRelative || wrongSchema)
					{
						throw new ArgumentException(
							"Only ms-appx scheme is allowed for Logo Uri",
							nameof(value));
					}
				}
				_logo = value;
			}
		}

		public string DisplayName
		{
			get => _displayName;
			set
			{
				_displayName = value ??
					throw new ArgumentNullException(nameof(value));
			}
		}

		public string Description
		{
			get => _description;
			set
			{
				_description = value ??
					throw new ArgumentNullException(nameof(value));
			}
		}

		public string Arguments { get; }

		public JumpListItemKind Kind => JumpListItemKind.Arguments;

		public bool RemovedByUser => false;

		public static JumpListItem CreateWithArguments(string arguments, string displayName)
		{
			if (displayName == null)
			{
				throw new ArgumentNullException(nameof(displayName));
			}
			return new JumpListItem(arguments)
			{
				DisplayName = displayName
			};
		}
	}
}
#endif
