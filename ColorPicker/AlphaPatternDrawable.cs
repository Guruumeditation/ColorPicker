// Copyright 2018 Arcana Studio
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// 
// Based on the work of Jared Rummler : https://github.com/jaredrummler/ColorPicker

using Android.Graphics;
using Android.Graphics.Drawables;
using Java.Lang;
using Math = System.Math;

namespace Net.ArcanaStudio.ColorPicker
{
    internal class AlphaPatternDrawable : Drawable
    {
        #region Fields

        private readonly Paint _paint = new Paint();
        private readonly Paint _paintGray = new Paint();
        private readonly Paint _paintWhite = new Paint();
        private readonly int _rectangleSize = 10;

        /**
        * Bitmap in which the pattern will be cached.
        * This is so the pattern will not have to be recreated each time draw() gets called.
        * Because recreating the pattern i rather expensive. I will only be recreated if the size changes.
        */

        private Bitmap _bitmap;

        private int _numRectanglesHorizontal;
        private int _numRectanglesVertical;

        #endregion

        #region Properties

        public override int Alpha
        {
            get => throw new UnsupportedOperationException("Alpha is not supported by this drawable.");
            set => throw new UnsupportedOperationException("Alpha is not supported by this drawable.");
        }

        public override ColorFilter ColorFilter =>
            throw new UnsupportedOperationException("ColorFilter is not supported by this drawable.");


        public override int Opacity => 0;

        #endregion

        #region Constructors

        public AlphaPatternDrawable(int rectangleSize)
        {
            this._rectangleSize = rectangleSize;
            _paintWhite.Color = Color.White;
            _paintGray.Color = new Color(0xCB, 0xCB, 0xCB, 0xFF);
        }

        #endregion

        #region Members

        public override void Draw(Canvas canvas)
        {
            if (_bitmap != null && !_bitmap.IsRecycled)
            {
                canvas.DrawBitmap(_bitmap, null, Bounds, _paint);
            }
        }


        /**
         * This will generate a bitmap with the pattern as big as the rectangle we were allow to draw on.
         * We do this to chache the bitmap so we don't need to recreate it each time draw() is called since it takes a few milliseconds
         */
        private void GeneratePatternBitmap()
        {
            if (Bounds.Width() <= 0 || Bounds.Height() <= 0)
            {
                return;
            }

            _bitmap = Bitmap.CreateBitmap(Bounds.Width(), Bounds.Height(), Bitmap.Config.Argb8888);
            var canvas = new Canvas(_bitmap);

            var r = new Rect();
            var verticalStartWhite = true;
            for (var i = 0; i <= _numRectanglesVertical; i++)
            {
                var isWhite = verticalStartWhite;
                for (var j = 0; j <= _numRectanglesHorizontal; j++)
                {
                    r.Top = i * _rectangleSize;
                    r.Left = j * _rectangleSize;
                    r.Bottom = r.Top + _rectangleSize;
                    r.Right = r.Left + _rectangleSize;
                    canvas.DrawRect(r, isWhite ? _paintWhite : _paintGray);
                    isWhite = !isWhite;
                }

                verticalStartWhite = !verticalStartWhite;
            }
        }

        protected override void OnBoundsChange(Rect bounds)
        {
            base.OnBoundsChange(bounds);
            var height = bounds.Height();
            var width = bounds.Width();
            _numRectanglesHorizontal = (int) Math.Ceiling(width / (double) _rectangleSize);
            _numRectanglesVertical = (int) Math.Ceiling(height / (double) _rectangleSize);
            GeneratePatternBitmap();
        }

        public override void SetAlpha(int alpha)
        {
            throw new UnsupportedOperationException("Alpha is not supported by this drawable.");
        }

        public override void SetColorFilter(ColorFilter colorFilter)
        {
            throw new UnsupportedOperationException("Alpha is not supported by this drawable.");
        }

        #endregion
    }
}