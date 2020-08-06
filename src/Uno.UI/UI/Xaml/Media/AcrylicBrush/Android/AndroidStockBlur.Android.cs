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
using Android.Renderscripts;

namespace Uno.UI.Xaml.Media
{
    internal class AndroidStockBlur : IBlurImpl
    {
#if DEBUG
        private const bool DEBUG = true;
#else
        private const bool DEBUG = false;
#endif

        private RenderScript _mRenderScript;

        private ScriptIntrinsicBlur _mBlurScript;

        private Allocation _mBlurInput;

        private Allocation _mBlurOutput;

        public bool Prepare(Context context, Bitmap buffer, float radius)
        {
            if (_mRenderScript == null)
            {
                try
                {
                    _mRenderScript = RenderScript.Create(context);
                    _mBlurScript = ScriptIntrinsicBlur.Create(_mRenderScript, Element.U8_4(_mRenderScript));
                }
                catch (Android.Renderscripts.RSRuntimeException e)
                {
#pragma warning disable CS0162
					if (DEBUG)
                    {
                        throw e;
                    }
                    else
                    {
						// In release mode, just ignore
						Release();
                        return false;
                    }
#pragma warning restore CS0162
				}
			}

            _mBlurScript.SetRadius(radius);

            _mBlurInput = Allocation.CreateFromBitmap(
                _mRenderScript,
                buffer,
                Allocation.MipmapControl.MipmapNone,
                AllocationUsage.Script);
            _mBlurOutput = Allocation.CreateTyped(_mRenderScript, _mBlurInput.Type);

            return true;
        }

        public void Release()
        {
            if (_mBlurInput != null)
            {
                _mBlurInput.Destroy();
                _mBlurInput = null;
            }

            if (_mBlurOutput != null)
            {
                _mBlurOutput.Destroy();
                _mBlurOutput = null;
            }

            if (_mBlurScript != null)
            {
                _mBlurScript.Destroy();
                _mBlurScript = null;
            }

            if (_mRenderScript != null)
            {
                _mRenderScript.Destroy();
                _mRenderScript = null;
            }
        }

        public void Blur(Bitmap input, Bitmap output)
        {
            _mBlurInput.CopyFrom(input);
            _mBlurScript.SetInput(_mBlurInput);
            _mBlurScript.ForEach(_mBlurOutput);
			_mBlurOutput.CopyTo(output);
        }
    }
}
