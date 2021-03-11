# Managing activities in Android

[Activities](https://developer.android.com/reference/android/app/Activity) are an integral element of the Android platform. By default your Uno Platform application runs in a single activity, but you might for example spawn a new activity when a user shares content, or picks an image from their device. This article covers Activity management in Uno. 

## Android documentation links

[Introduction to Activities](https://developer.android.com/guide/components/activities/intro-activities)

[Understand the Activity Lifecycle](https://developer.android.com/guide/components/activities/activity-lifecycle)

## Creating/Using Android Activities
At the root of every Android Uno app, lies a `BaseActivity` class that extends from `Android.Support.V7.App.AppCompatActivity` which is part of the [Android v7 AppCompat Support Library](https://developer.android.com/topic/libraries/support-library/features.html#v7-appcompat). If you ever need to create a new Activity within your app or within Uno you must be sure to extend `BaseActivity` and, if you need to apply a Theme to the activity, ensure that the Theme you set is a `Theme.AppCompat` theme (or descendant).