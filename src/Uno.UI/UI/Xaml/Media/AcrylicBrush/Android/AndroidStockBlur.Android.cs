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

#pragma warning disable CS0618 // RenderScript is obsolete

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
#pragma warning disable CA1422 // Validate platform compatibility
                    _mRenderScript = RenderScript.Create(context);
                    _mBlurScript = ScriptIntrinsicBlur.Create(_mRenderScript, Element.U8_4(_mRenderScript));
#pragma warning restore CA1422 // Validate platform compatibility
                }
                catch (Android.Renderscripts.RSRuntimeException)
                {
#pragma warning disable CS0162
					if (DEBUG)
                    {
                        throw;
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

#pragma warning disable CA1422 // Validate platform compatibility
            _mBlurScript.SetRadius(radius);

            _mBlurInput = Allocation.CreateFromBitmap(
                _mRenderScript,
                buffer,
                Allocation.MipmapControl.MipmapNone,
                AllocationUsage.Script);
            _mBlurOutput = Allocation.CreateTyped(_mRenderScript, _mBlurInput.Type);
#pragma warning restore CA1422 // Validate platform compatibility
            return true;
        }

        public void Release()
        {
#pragma warning disable CA1422 // Validate platform compatibility
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
#pragma warning restore CA1422 // Validate platform compatibility
        }

        public void Blur(Bitmap input, Bitmap output)
        {
#pragma warning disable CA1422 // Validate platform compatibility
            _mBlurInput.CopyFrom(input);
            _mBlurScript.SetInput(_mBlurInput);
            _mBlurScript.ForEach(_mBlurOutput);
			_mBlurOutput.CopyTo(output);
#pragma warning restore CA1422 // Validate platform compatibility
        }
    }
}
