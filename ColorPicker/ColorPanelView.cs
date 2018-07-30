// Copyright 2018 Arcana Studio
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// 
// Based on the work of Jared Rummler : https://github.com/jaredrummler/ColorPicker

using System.Globalization;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Support.V4.View;
using Android.Util;
using Android.Views;
using Android.Widget;
using Java.Lang;
using StringBuilder = System.Text.StringBuilder;

namespace Net.ArcanaStudio.ColorPicker
{
    public class ColorPanelView : View
    {
        #region  Static Fields and Constants

        private static readonly Color DEFAULT_BORDER_COLOR = new Color(0xff, 0x6e, 0x6e, 0x6e);

        #endregion

        #region Fields

        private Paint _alphaPaint;

        private Drawable _alphaPattern;
        private Color _borderColor = DEFAULT_BORDER_COLOR;
        private Paint _borderPaint;

        /* The width in pixels of the border surrounding the color panel. */
        private int _borderWidthPx;
        private RectF _centerRect = new RectF();
        private Color _color = Color.Black;
        private Paint _colorPaint;
        private Rect _colorRect;
        private Rect _drawingRect;
        private Paint _originalPaint;
        private ColorShape _shape;
        private bool _showOldColor;

        #endregion

        #region Constructors

        public ColorPanelView(Context context) : this(context, null)
        {
        }

        public ColorPanelView(Context context, IAttributeSet attrs) : this(context, attrs, 0)
        {
        }

        public ColorPanelView(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            Init(context, attrs);
        }

        #endregion

        #region Members

        /**
         * @return the color of the border surrounding the panel.
         */
        public Color GetBorderColor()
        {
            return _borderColor;
        }

        /**
         * Get the color currently show by this view.
         *
         * @return the color value
         */
        public Color GetColor()
        {
            return _color;
        }

        /**
         * Get the shape
         *
         * @return Either {@link ColorShape#SQUARE} or {@link ColorShape#CIRCLE}.
         */
        public ColorShape GetShape()
        {
            return _shape;
        }

        private void Init(Context context, IAttributeSet attrs)
        {
            var a = Context.ObtainStyledAttributes(attrs, Resource.Styleable.ColorPanelView);
            _shape = (ColorShape) a.GetInt(Resource.Styleable.ColorPanelView_cpv_colorShape, (int) ColorShape.Circle);
            _showOldColor = a.GetBoolean(Resource.Styleable.ColorPanelView_cpv_showOldColor, false);
            if (_showOldColor && _shape != ColorShape.Circle)
            {
                throw new IllegalStateException("Color preview is only available in circle mode");
            }

            _borderColor = a.GetColor(Resource.Styleable.ColorPanelView_cpv_borderColor, DEFAULT_BORDER_COLOR);
            a.Recycle();
            if (_borderColor == DEFAULT_BORDER_COLOR)
            {
                // If no specific border color has been set we take the default secondary text color as border/slider color.
                // Thus it will adopt to theme changes automatically.
                var value = new TypedValue();
                var typedArray =
                    context.ObtainStyledAttributes(value.Data, new[] {Android.Resource.Attribute.TextColorSecondary});
                _borderColor = typedArray.GetColor(0, _borderColor);
                typedArray.Recycle();
            }

            _borderWidthPx = DrawingUtils.DpToPx(context, 1);
            _borderPaint = new Paint {AntiAlias = true};
            _colorPaint = new Paint {AntiAlias = true};
            if (_showOldColor)
            {
                _originalPaint = new Paint();
            }

            if (_shape == ColorShape.Circle)
            {
                var bitmap = ((BitmapDrawable) context.GetDrawable(Resource.Drawable.cpv_alpha)).Bitmap;
                _alphaPaint = new Paint {AntiAlias = true};
                var shader = new BitmapShader(bitmap, Shader.TileMode.Repeat, Shader.TileMode.Repeat);
                _alphaPaint.SetShader(shader);
            }
        }

        protected override void OnDraw(Canvas canvas)
        {
            _borderPaint.Color = _borderColor;
            _colorPaint.Color = _color;
            if (_shape == (int) ColorShape.Square)
            {
                if (_borderWidthPx > 0)
                {
                    canvas.DrawRect(_drawingRect, _borderPaint);
                }

                _alphaPattern?.Draw(canvas);

                canvas.DrawRect(_colorRect, _colorPaint);
            }
            else if (_shape == ColorShape.Circle)
            {
                var outerRadius = MeasuredWidth / 2;
                if (_borderWidthPx > 0)
                {
                    canvas.DrawCircle(MeasuredWidth / 2,
                        MeasuredHeight / 2,
                        outerRadius,
                        _borderPaint);
                }

                if (Color.GetAlphaComponent(_color) < 255)
                {
                    canvas.DrawCircle(MeasuredWidth / 2,
                        MeasuredHeight / 2,
                        outerRadius - _borderWidthPx, _alphaPaint);
                }

                if (_showOldColor)
                {
                    canvas.DrawArc(_centerRect, 90, 180, true, _originalPaint);
                    canvas.DrawArc(_centerRect, 270, 180, true, _colorPaint);
                }
                else
                {
                    canvas.DrawCircle(MeasuredWidth / 2,
                        MeasuredHeight / 2,
                        outerRadius - _borderWidthPx,
                        _colorPaint);
                }
            }
        }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            if (_shape == ColorShape.Square)
            {
                var width = MeasureSpec.GetSize(widthMeasureSpec);
                var height = MeasureSpec.GetSize(heightMeasureSpec);
                SetMeasuredDimension(width, height);
            }
            else if (_shape == ColorShape.Circle)
            {
                base.OnMeasure(widthMeasureSpec, widthMeasureSpec);
                SetMeasuredDimension(MeasuredWidth, MeasuredWidth);
            }
            else
            {
                base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
            }
        }

        protected override void OnRestoreInstanceState(IParcelable state)
        {
            if (state is Bundle bundle)
            {
                _color = new Color(bundle.GetInt("color"));
                state = (IParcelable) bundle.GetParcelable("instanceState");
            }

            base.OnRestoreInstanceState(state);
        }

        protected override IParcelable OnSaveInstanceState()
        {
            var state = new Bundle();
            state.PutParcelable("instanceState", base.OnSaveInstanceState());
            state.PutInt("color", _color);
            return state;
        }

        protected override void OnSizeChanged(int w, int h, int oldw, int oldh)
        {
            base.OnSizeChanged(w, h, oldw, oldh);
            if (_shape == ColorShape.Square || _showOldColor)
            {
                _drawingRect = new Rect
                {
                    Left = PaddingLeft,
                    Right = w - PaddingRight,
                    Top = PaddingTop,
                    Bottom = h - PaddingBottom
                };
                if (_showOldColor)
                {
                    SetUpCenterRect();
                }
                else
                {
                    SetUpColorRect();
                }
            }
        }

        /**
         * Set the color of the border surrounding the panel.
         *
         * @param color
         *     the color value
         */
        public void SetBorderColor(Color color)
        {
            _borderColor = color;
            Invalidate();
        }

        /**
         * Set the color that should be shown by this view.
         *
         * @param color
         *     the color value
         */
        public void SetColor(Color color)
        {
            _color = color;
            Invalidate();
        }

        /**
         * Set the original color. This is only used for previewing colors.
         *
         * @param color
         *     The original color
         */
        public void SetOriginalColor(Color color)
        {
            if (_originalPaint != null)
            {
                _originalPaint.Color = color;
            }
        }

        /**
         * Set the shape.
         *
         * @param shape
         *     Either {@link ColorShape#SQUARE} or {@link ColorShape#CIRCLE}.
         */
        public void SetShape(ColorShape shape)
        {
            _shape = shape;
            Invalidate();
        }

        private void SetUpCenterRect()
        {
            var dRect = _drawingRect;
            var left = dRect.Left + _borderWidthPx;
            var top = dRect.Top + _borderWidthPx;
            var bottom = dRect.Bottom - _borderWidthPx;
            var right = dRect.Right - _borderWidthPx;
            _centerRect = new RectF(left, top, right, bottom);
        }

        private void SetUpColorRect()
        {
            var dRect = _drawingRect;
            var left = dRect.Left + _borderWidthPx;
            var top = dRect.Top + _borderWidthPx;
            var bottom = dRect.Bottom - _borderWidthPx;
            var right = dRect.Right - _borderWidthPx;
            _colorRect = new Rect(left, top, right, bottom);
            _alphaPattern = new AlphaPatternDrawable(DrawingUtils.DpToPx(Context, 4));
            _alphaPattern.SetBounds(_colorRect.Left,
                _colorRect.Top,
                _colorRect.Right,
                _colorRect.Bottom);
        }

        /**
         * Show a toast message with the hex color code below the view.
         */
        public void ShowHint()
        {
            var screenPos = new int[2];
            var displayFrame = new Rect();
            GetLocationOnScreen(screenPos);
            GetWindowVisibleDisplayFrame(displayFrame);
            var context = Context;
            var width = Width;
            var height = Height;
            var midy = screenPos[1] + height / 2;
            var referenceX = screenPos[0] + width / 2;
            if (ViewCompat.GetLayoutDirection(this) == ViewCompat.LayoutDirectionLtr)
            {
                var screenWidth = context.Resources.DisplayMetrics.WidthPixels;
                referenceX = screenWidth - referenceX; // mirror
            }

            var hint = new StringBuilder("#");
            if (Color.GetAlphaComponent(_color) != 255)
            {
                hint.Append(Integer.ToHexString(_color).ToUpper(new CultureInfo("en")));
            }
            else
            {
                hint.Append($"{0xFFFFFF & _color:X6}".ToUpper(new CultureInfo("en")));
            }

            var cheatSheet = Toast.MakeText(context, hint.ToString(), ToastLength.Short);
            if (midy < displayFrame.Height())
            {
                // Show along the top; follow action buttons
                cheatSheet.SetGravity(GravityFlags.Top | GravityFlags.End, referenceX,
                    screenPos[1] + height - displayFrame.Top);
            }
            else
            {
                // Show along the bottom center
                cheatSheet.SetGravity(GravityFlags.Bottom | GravityFlags.CenterHorizontal, 0, height);
            }

            cheatSheet.Show();
        }

        #endregion
    }
}