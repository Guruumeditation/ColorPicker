// Copyright 2018 Arcana Studio
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// 
// Based on the work of Jared Rummler : https://github.com/jaredrummler/ColorPicker

using System;
using Android.Widget;

namespace Net.ArcanaStudio.ColorPicker
{
    internal class OnSeekBarChangeListener : Java.Lang.Object, SeekBar.IOnSeekBarChangeListener
    {
        #region Fields

        private readonly Action<SeekBar, int, bool> _actionOnProgressChanged;
        private readonly Action<SeekBar> _actionOnStartTrackingTouch;
        private readonly Action<SeekBar> _actionOnStopTrackingTouch;

        #endregion

        #region Constructors

        public OnSeekBarChangeListener(Action<SeekBar, int, bool> actionOnProgressChanged,
            Action<SeekBar> actionOnStartTrackingTouch, Action<SeekBar> actionOnStopTrackingTouch)
        {
            _actionOnProgressChanged = actionOnProgressChanged ?? ((s, i, b) => { });
            _actionOnStartTrackingTouch = actionOnStartTrackingTouch ?? (b => { });
            _actionOnStopTrackingTouch = actionOnStopTrackingTouch ?? (b => { });
        }

        #endregion

        #region Interfaces

        public void OnProgressChanged(SeekBar seekBar, int progress, bool fromUser)
        {
            _actionOnProgressChanged(seekBar, progress, fromUser);
        }

        public void OnStartTrackingTouch(SeekBar seekBar)
        {
            _actionOnStartTrackingTouch(seekBar);
        }

        public void OnStopTrackingTouch(SeekBar seekBar)
        {
            _actionOnStopTrackingTouch(seekBar);
        }

        #endregion
    }
}