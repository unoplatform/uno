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
		private static readonly object ApplyGate = new();
		private static BadgeUpdater? _coordinatorUpdater;
		private static string? _explicitBadge;
		private static string? _appTaskBadge;
		private static long _badgeVersion;
		private static long _appliedBadgeVersion;

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
			string? effective;
			long version;
			lock (BadgeGate)
			{
				_appTaskBadge = value?.ToString(CultureInfo.InvariantCulture);
				(effective, version) = CaptureEffectiveBadgeLocked();
			}

			ApplyEffectiveBadge(effective, version);
		}

		private static void SetExplicitBadge(string? value)
		{
			string? effective;
			long version;
			lock (BadgeGate)
			{
				// The "none" glyph is how a badge notification asks for the badge to be removed, so it
				// must not keep overriding the app-task count.
				_explicitBadge = string.IsNullOrWhiteSpace(value)
					|| string.Equals(value, NoBadgeGlyph, StringComparison.OrdinalIgnoreCase)
					? null
					: value;
				(effective, version) = CaptureEffectiveBadgeLocked();
			}

			ApplyEffectiveBadge(effective, version);
		}

		private static (string? Value, long Version) CaptureEffectiveBadgeLocked() =>
			(_explicitBadge ?? _appTaskBadge, ++_badgeVersion);

		// The platform back-ends run outside the state lock so a native or JS badge call can never block
		// an unrelated badge update; the version guard keeps a slower call from overwriting a newer value.
		private static void ApplyEffectiveBadge(string? value, long version)
		{
			lock (ApplyGate)
			{
				if (version < _appliedBadgeVersion)
				{
					return;
				}

				_appliedBadgeVersion = version;
				(_coordinatorUpdater ??= new BadgeUpdater()).SetBadge(value);
			}
		}
	}
}
#endif
