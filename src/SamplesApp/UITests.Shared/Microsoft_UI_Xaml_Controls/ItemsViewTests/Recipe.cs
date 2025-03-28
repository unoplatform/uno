// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.ComponentModel;
using Windows.UI.Xaml.Media.Imaging;

namespace MUXControlsTestApp.Samples.Model;

public class Recipe : INotifyPropertyChanged
{
	private BitmapImage _bitmapImage;
	private Uri _imageUri;
	private int _id;
	private string _description;

	public BitmapImage BitmapImage
	{
		get
		{
			return _bitmapImage;
		}
		set
		{
			if (value != _bitmapImage)
			{
				_bitmapImage = value;
				OnPropertyChanged("BitmapImage");
			}
		}
	}

	public Uri ImageUri
	{
		get
		{
			return _imageUri;
		}
		set
		{
			if (value != _imageUri)
			{
				_imageUri = value;
				OnPropertyChanged("ImageUri");
			}
		}
	}

	public int Id
	{
		get
		{
			return _id;
		}
		set
		{
			if (value != _id)
			{
				_id = value;
				OnPropertyChanged("Id");
			}
		}
	}

	public string Description
	{
		get
		{
			return _description;
		}
		set
		{
			if (value != _description)
			{
				_description = value;
				OnPropertyChanged("Description");
			}
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	private void OnPropertyChanged(string propertyName)
	{
		if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); }
	}

	public override string ToString()
	{
		return Description;
	}

	public double AspectRatio
	{
		get
		{
			if (_bitmapImage != null)
			{
				if (_bitmapImage.PixelWidth == 0 || _bitmapImage.PixelHeight == 0)
				{
					return GetKnownAspectRatioFromLocalPath(_bitmapImage.UriSource.LocalPath.ToString());
				}
				else
				{
					return (double)_bitmapImage.PixelWidth / _bitmapImage.PixelHeight;
				}
			}

			if (_imageUri != null)
			{
				return GetKnownAspectRatioFromLocalPath(_imageUri.LocalPath.ToString());
			}

			return 0.0;
		}
	}

	public static double GetKnownAspectRatioFromIndex(int index)
	{
		string imageName = "vette" + index + ".jpg";

		return GetKnownAspectRatioFromImageName(imageName);
	}

	private static double GetKnownAspectRatioFromLocalPath(string localPath)
	{
		string imageName = localPath.Substring(8);

		return GetKnownAspectRatioFromImageName(imageName);
	}

	private static double GetKnownAspectRatioFromImageName(string imageName)
	{
		switch (imageName)
		{
			case "vette1.jpg":
			case "vette2.jpg":
			case "vette3.jpg":
			case "vette7.jpg":
			case "vette8.jpg":
			case "vette9.jpg":
			case "vette10.jpg":
			case "vette11.jpg":
			case "vette14.jpg":
			case "vette15.jpg":
			case "vette19.jpg":
			case "vette21.jpg":
			case "vette22.jpg":
			case "vette25.jpg":
			case "vette29.jpg":
			case "vette30.jpg":
			case "vette31.jpg":
			case "vette33.jpg":
			case "vette34.jpg":
			case "vette37.jpg":
			case "vette40.jpg":
			case "vette41.jpg":
			case "vette42.jpg":
			case "vette43.jpg":
			case "vette46.jpg":
			case "vette51.jpg":
			case "vette52.jpg":
			case "vette55.jpg":
			case "vette58.jpg":
			case "vette68.jpg":
			case "vette69.jpg":
			case "vette71.jpg":
			case "vette73.jpg":
			case "vette74.jpg":
			case "vette75.jpg":
			case "vette76.jpg":
			case "vette78.jpg":
			case "vette79.jpg":
			case "vette80.jpg":
			case "vette81.jpg":
			case "vette82.jpg":
			case "vette84.jpg":
			case "vette85.jpg":
			case "vette87.jpg":
			case "vette90.jpg":
			case "vette91.jpg":
			case "vette92.jpg":
			case "vette94.jpg":
			case "vette95.jpg":
			case "vette96.jpg":
			case "vette97.jpg":
			case "vette100.jpg":
			case "vette101.jpg":
			case "vette102.jpg":
			case "vette103.jpg":
			case "vette104.jpg":
			case "vette105.jpg":
			case "vette106.jpg":
			case "vette107.jpg":
			case "vette108.jpg":
			case "vette110.jpg":
			case "vette112.jpg":
			case "vette113.jpg":
			case "vette114.jpg":
			case "vette115.jpg":
			case "vette116.jpg":
			case "vette117.jpg":
			case "vette118.jpg":
			case "vette119.jpg":
			case "vette120.jpg":
			case "vette123.jpg":
				return 404.0 / 303.0;
			case "vette4.jpg":
				return 71.0 / 404.0;
			case "vette5.jpg":
			case "vette12.jpg":
			case "vette13.jpg":
			case "vette16.jpg":
			case "vette23.jpg":
			case "vette24.jpg":
			case "vette28.jpg":
			case "vette38.jpg":
			case "vette39.jpg":
			case "vette44.jpg":
			case "vette45.jpg":
			case "vette49.jpg":
			case "vette50.jpg":
			case "vette53.jpg":
			case "vette54.jpg":
			case "vette56.jpg":
			case "vette57.jpg":
			case "vette59.jpg":
			case "vette60.jpg":
			case "vette61.jpg":
			case "vette62.jpg":
			case "vette63.jpg":
			case "vette64.jpg":
			case "vette65.jpg":
			case "vette66.jpg":
			case "vette67.jpg":
			case "vette70.jpg":
			case "vette72.jpg":
			case "vette83.jpg":
			case "vette93.jpg":
			case "vette98.jpg":
			case "vette99.jpg":
			case "vette109.jpg":
			case "vette111.jpg":
			case "vette121.jpg":
			case "vette122.jpg":
			case "vette124.jpg":
			case "vette125.jpg":
				return 303.0 / 404.0;
			case "vette6.jpg":
				return 404.0 / 175.0;
			case "vette17.jpg":
				return 212.0 / 404.0;
			case "vette18.jpg":
				return 404.0 / 153.0;
			case "vette20.jpg":
				return 364.0 / 303.0;
			case "vette26.jpg":
				return 404.0 / 238.0;
			case "vette27.jpg":
				return 275.0 / 404.0;
			case "vette32.jpg":
				return 404.0 / 212.0;
			case "vette35.jpg":
				return 404.0 / 190.0;
			case "vette36.jpg":
				return 193.0 / 404.0;
			case "vette47.jpg":
				return 119.0 / 404.0;
			case "vette48.jpg":
				return 188.0 / 404.0;
			case "vette77.jpg":
				return 281.0 / 404.0;
			case "vette86.jpg":
				return 404.0 / 98.0;
			case "vette88.jpg":
				return 404.0 / 141.0;
			case "vette89.jpg":
				return 221.0 / 404.0;
			case "vette126.jpg":
				return 151.0 / 404.0;
			case "Rect0.png":
				return 1.75; // 337.0 / 193.0
			case "Rect1.png":
				return 1.5;  // 289.0 / 193.0
			case "Rect2.png":
				return 1.25; // 241.0 / 193.0
			case "Rect3.png":
				return 1.0;  // 193.0 / 193.0
			case "Rect4.png":
				return 0.75; // 145.0 / 193.0;
			case "Rect5.png":
				return 0.5;  //  97.0 / 193.0;
		}
		return 1.0;
	}
}
