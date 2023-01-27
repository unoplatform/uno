using Uno.Extensions;
using System;

namespace Uno.UI.Controls
{
	public class SearchSuggestion
	{
		public string Group { get; private set; }
		public string Text { get; private set; }
		public string DetailText { get; private set; }
		public string Tag { get; private set; }
		public Uri Image { get; private set; }
		public string ImageAlternateText { get; private set; }
		protected SearchSuggestion() { }

		public static SearchSuggestion CreateSuggestion(string text, string group = null)
		{
			if (text == null)
			{
				throw new ArgumentNullException(nameof(text));
			}

			return new SearchSuggestion { Text = text, Group = group };
		}

		public static SearchSuggestion CreateResult(string text, string detailText, string tag, string imageAlternateText, Uri image = null, string group = null)
		{
			if (text == null)
			{
				throw new ArgumentNullException(nameof(text));
			}

			if (detailText == null)
			{
				throw new ArgumentNullException(nameof(detailText));
			}

			if (tag == null)
			{
				throw new ArgumentNullException(nameof(tag));
			}

			if (imageAlternateText == null)
			{
				throw new ArgumentNullException(nameof(imageAlternateText));
			}

			var sbs = new SearchSuggestion
			{
				Text = text.ToUpperInvariant(),
				DetailText = detailText,
				Tag = tag,
				Image = image,
				ImageAlternateText = imageAlternateText,
				Group = group
			};

			if (image != null)
			{
				sbs.Image = image;
			}

			return sbs;
		}
	}
}
