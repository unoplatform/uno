/*

Implementation based on https://github.com/mmin18/RealtimeBlurView.
with some modifications and removal of unused features.

------------------------------------------------------------------------------

 https://github.com/mmin18/RealtimeBlurView
 Latest commit    82df352     on 24 May 2019

 Copyright 2016 Tu Yimin (http://github.com/mmin18)

 Licensed under the Apache License, Version 2.0 (the "License");
 you may not use this file except in compliance with the License.
 You may obtain a copy of the License at

 http://www.apache.org/licenses/LICENSE-2.0

 Unless required by applicable law or agreed to in writing, software
 distributed under the License is distributed on an "AS IS" BASIS,
 WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 See the License for the specific language governing permissions and
 limitations under the License.

------------------------------------------------------------------------------
 Adapted to csharp by Jean-Marie Alfonsi
------------------------------------------------------------------------------
*/

using Android.Content;
using Android.Graphics;

namespace Uno.UI.Xaml.Media
{
	internal interface IBlurImpl
    {
        bool Prepare(Context context, Bitmap buffer, float radius);

        void Release();

        void Blur(Bitmap input, Bitmap output);
    }
}
