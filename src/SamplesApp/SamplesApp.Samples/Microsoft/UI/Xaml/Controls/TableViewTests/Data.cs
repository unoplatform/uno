// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference Samples\TableViewSampleApp\Data.cs, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace MUXControlsTestApp;

// Row model for the sample. Name lengths vary to exercise Auto column sizing; Bio strings
// vary in length. The text-bound properties are settable and raise PropertyChanged so the
// editable TextBox cells write back (TwoWay) and the display cell refreshes on commit.
public sealed class Item : INotifyPropertyChanged
{
	private string _name = "";
	private string _role = "";
	private string _city = "";
	private string _notes = "";
	private DateTimeOffset _joined;

	public string Name { get => _name; set => Set(ref _name, value); }
	public string Role { get => _role; set => Set(ref _role, value); }
	public string City { get => _city; set => Set(ref _city, value); }
	public string Notes { get => _notes; set => Set(ref _notes, value); }

	public int Score { get; init; }
	public string Bio { get; init; } = "";
	public DateTimeOffset Joined
	{
		get => _joined;
		set
		{
			if (_joined == value)
			{
				return;
			}
			_joined = value;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Joined)));
		}
	}
	public ImageSource? Avatar { get; init; }

	public event PropertyChangedEventHandler? PropertyChanged;

	// Rendered avatar edge length; varied per row so the Image cells are different sizes (exercises
	// Auto column width sizing to the widest image and Auto row height sizing to the tallest).
	public double ImageSize { get; init; }

	private void Set(ref string field, string value, [CallerMemberName] string? propertyName = null)
	{
		if (field == value)
		{
			return;
		}
		field = value;
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	public Item(string name, string role, string city, int score, string bio, DateTimeOffset joined, string notes, ImageSource? avatar, double imageSize = 40)
	{
		_name = name; _role = role; _city = city; _notes = notes;
		Score = score; Bio = bio; Joined = joined; Avatar = avatar; ImageSize = imageSize;
	}
}

internal static class Data
{
	// Name-width tiers. The wide names are ISOLATED (see Make) so the Auto Name column starts
	// narrow and visibly EXPANDS when a wide name scrolls into view (grow-only, matching v1).
	private static readonly string[] Short = { "Al", "Bo", "Cy", "Jo", "Ed", "Li", "Yu", "Sam", "Mia" };
	private static readonly string[] Medium = { "Ada Lovelace", "Grace Hopper", "Alan Turing", "Nina Patel", "Omar Farouk" };
	private static readonly string[] Wide =
	{
		"Maximilian Alexander Fairbanks-Whittington",
		"Anastasia Konstantinova Rozhdestvenskaya",
		"Wolfgang Amadeus von Habsburg-Lothringen",
	};
	private static readonly string[] Roles = { "Dev", "QA", "PM", "Designer", "Architect", "Researcher" };
	private static readonly string[] Cities = { "London", "Oslo", "Kyoto", "New York", "Shenzhen", "Berlin" };

	// Bio strings of varied length so the wrapping Bio column drives variable row height.
	private static readonly string[] Bios =
	{
		"Fixes bugs.",
		"Runs the manual and automated test passes ahead of every milestone release, triaging incoming regressions across all supported platforms and keeping the sign-off checklist current for each build.",
		"Coordinates sprint planning and delivery.",
		"Designs the visual language and reviews every control spec for accessibility and theming consistency across light, dark, and high-contrast.",
		"Investigates performance and memory footprint on low-end hardware.",
	};
	private static readonly string[] NotesSeed = { "Fix bugs", "Test pass", "Plan", "Review", "Profile" };

	// Avatar edge lengths, cycled per row so the image column holds different-sized images.
	private static readonly double[] ImageSizes = { 24, 40, 64, 96, 48 };

	// 3 dummy avatars (Assets\avatar1..3.png), cycled per row.
	// TODO Uno: the avatars live under Assets/TableView/ so they do not collide with other SamplesApp assets.
	// => new BitmapImage(new Uri($"ms-appx:///Assets/avatar{(i % 3) + 1}.png"));
	private static ImageSource Avatar(int i)
		=> new BitmapImage(new Uri($"ms-appx:///Assets/TableView/avatar{(i % 3) + 1}.png"));

	// ~150 rows so the body virtualizes and scrolls. A single wide name appears in isolation
	// every 50 rows (at 35, 85, 135), so the first screen has only short/medium names and the
	// Auto Name column starts narrow, then grows when you scroll a wide name into view.
	public static List<Item> Make(int n = 150)
	{
		var list = new List<Item>(n);
		var rnd = new Random(42);
		var baseDate = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
		for (int i = 0; i < n; i++)
		{
			string name =
				(i % 50 == 35) ? Wide[(i / 50) % Wide.Length] :
				(i % 3 == 0) ? Short[i % Short.Length] :
							   Medium[i % Medium.Length];
			list.Add(new Item(
				name,
				Roles[i % Roles.Length],
				Cities[i % Cities.Length],
				rnd.Next(0, 101),
				Bios[i % Bios.Length],
				baseDate.AddDays(rnd.Next(0, 500)),
				NotesSeed[i % NotesSeed.Length],
				Avatar(i),
				ImageSizes[i % ImageSizes.Length]));
		}
		return list;
	}
}
