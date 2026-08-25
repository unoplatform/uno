#nullable enable

#if !__ANDROID__

using System;
using System.Globalization;
using Windows.Data.Xml.Dom;

namespace Windows.UI.Notifications
{
	public partial class BadgeUpdater
	{
		private const string BadgeNodeXPath = "/badge";
		private const string ValueAttribute = "value";
		private const string NoBadgeGlyph = "none";
		private static readonly object BadgeGate = new();
		private static BadgeUpdater? _coordinatorUpdater;
		private static string? _explicitBadge;
		private static string? _appTaskBadge;

		internal BadgeUpdater()
		{
			InitPlatform();
		}

		partial void InitPlatform();

		public void Update(BadgeNotification notification)
		{
			if (notification is null)
			{
				throw new ArgumentNullException(nameof(notification));
			}

			var element = notification.Content.SelectSingleNode(BadgeNodeXPath) as XmlElement;
			var attributeValue = element?.GetAttribute(ValueAttribute);
			SetExplicitBadge(attributeValue);
		}

		public void Clear() => SetExplicitBadge(null);

		partial void SetBadge(string? value);

		internal static void SetAppTaskBadge(int? value)
		{
			lock (BadgeGate)
			{
				_appTaskBadge = value?.ToString(CultureInfo.InvariantCulture);
				ApplyEffectiveBadgeLocked();
			}
		}

		private static void SetExplicitBadge(string? value)
		{
			lock (BadgeGate)
			{
				// The "none" glyph is how a badge notification asks for the badge to be removed, so it
				// must not keep overriding the app-task count.
				_explicitBadge = string.IsNullOrWhiteSpace(value)
					|| string.Equals(value, NoBadgeGlyph, StringComparison.OrdinalIgnoreCase)
					? null
					: value;
				ApplyEffectiveBadgeLocked();
			}
		}

		private static void ApplyEffectiveBadgeLocked()
		{
			_coordinatorUpdater ??= new BadgeUpdater();
			_coordinatorUpdater.SetBadge(_explicitBadge ?? _appTaskBadge);
		}
	}
}
#endif
