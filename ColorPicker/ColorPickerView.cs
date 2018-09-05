// Copyright 2018 Arcana Studio
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// 
// Based on the work of Jared Rummler : https://github.com/jaredrummler/ColorPicker

using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Util;
using Android.Views;
using Math = System.Math;

namespace Net.ArcanaStudio.ColorPicker
{
    public class ColorPickerView : View
    {
        #region  Static Fields and Constants

        private static readonly int _sliderTrackerOffsetDp = 2;
        private static readonly int _sliderTrackerSizeDp = 4;
        private static readonly int ALPHA_PANEL_HEIGH_DP = 20;

        /**
         * The width in pixels of the border
         * surrounding all color panels.
         */
        private static readonly int BORDER_WIDTH_PX = 1;
        private static readonly int CIRCLE_TRACKER_RADIUS_DP = 5;
        private static readonly uint DEFAULT_BORDER_COLOR = 0xFF6E6E6E;
        private static readonly uint DEFAULT_SLIDER_COLOR = 0xFFBDBDBD;

        private static readonly int HUE_PANEL_WDITH_DP = 30;
        private static readonly int PANEL_SPACING_DP = 10;

        #endregion

        #region Fields

        /* Current values */
        private int _alpha = 0xff;

        private Paint _alphaPaint;

        /**
         * The height in px of the alpha panel
         */
        private int _alphaPanelHeightPx;

        private AlphaPatternDrawable _alphaPatternDrawable;
        private Rect _alphaRect;
        private Shader _alphaShader;
        private string _alphaSliderText;
        private Paint _alphaTextPaint;
        private Color _borderColor = new Color((int) DEFAULT_BORDER_COLOR);

        private Paint _borderPaint;

        /**
         * The radius in px of the color palette tracker circle.
         */
        private int _circleTrackerRadiusPx;

        /**
         * The Rect in which we are allowed to draw.
         * Trackers can extend outside slightly,
         * due to the required padding we have set.
         */
        private Rect _drawingRect;
        private float _hue = 360f;
        private Paint _hueAlphaTrackerPaint;

        /* We cache the hue background to since its also very expensive now. */
        private BitmapCache _hueBackgroundCache;

        /**
         * The width in px of the hue panel.
         */
        private int _huePanelWidthPx;
        private Rect _hueRect;

        /**
         * Minimum required padding. The offset from the
         * edge we must have or else the finger tracker will
         * get clipped when it's drawn outside of the view.
         */
        private int _mRequiredPadding;
        private IOnColorChangedListener _onColorChangedListener;

        /**
         * The distance in px between the different
         * color panels.
         */
        private int _panelSpacingPx;
        private float _sat;
        private Shader _satShader;

        /*
         * We cache a bitmap of the sat/val panel which is expensive to draw each time.
         * We can reuse it when the user is sliding the circle picker as long as the hue isn't changed.
         */
        private BitmapCache _satValBackgroundCache;

        private Paint _satValPaint;

        private Rect _satValRect;
        private Paint _satValTrackerPaint;

        private bool _showAlphaPanel;
        private Color _sliderTrackerColor = new Color((int) DEFAULT_SLIDER_COLOR);

        /**
         * The px which the tracker of the hue or alpha panel
         * will extend outside of its bounds.
         */
        private int _sliderTrackerOffsetPx;

        /**
         * Height of slider tracker on hue panel,
         * width of slider on alpha panel.
         */
        private int _sliderTrackerSizePx;

        private Point _startTouchPoint;
        private float _val;

        private Shader _valShader;

        #endregion

        #region Properties

        public override int PaddingBottom => Math.Max(base.PaddingTop, _mRequiredPadding);


        public override int PaddingLeft => Math.Max(base.PaddingTop, _mRequiredPadding);

        public override int PaddingRight => Math.Max(base.PaddingTop, _mRequiredPadding);


        public override int PaddingTop => Math.Max(base.PaddingTop, _mRequiredPadding);

        #endregion

        #region Constructors

        public ColorPickerView(Context context) : this(context, null)
        {
        }

        public ColorPickerView(Context context, IAttributeSet attrs) : this(context, attrs, 0)
        {
        }

        public ColorPickerView(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            Init(context, attrs);
        }

        #endregion

        #region Members

        private Point AlphaToPoint(int alpha)
        {
            var rect = _alphaRect;
            float width = rect.Width();

            var p = new Point
            {
                X = (int) (width - alpha * width / 0xff + rect.Left),
                Y = rect.Top
            };


            return p;
        }

        private void ApplyThemeColors(Context c)
        {
            // If no specific border/slider color has been
            // set we take the default secondary text color
            // as border/slider color. Thus it will adopt
            // to theme changes automatically.

            var value = new TypedValue();
            var a = c.ObtainStyledAttributes(value.Data, new[] {Android.Resource.Attribute.TextColorSecondary});

            if (_borderColor == DEFAULT_BORDER_COLOR)
            {
                _borderColor = a.GetColor(0, (int) DEFAULT_BORDER_COLOR);
            }

            if (_sliderTrackerColor == DEFAULT_SLIDER_COLOR)
            {
                _sliderTrackerColor = a.GetColor(0, (int) DEFAULT_SLIDER_COLOR);
            }

            a.Recycle();
        }

        private void DrawAlphaPanel(Canvas canvas)
        {
            /*
             * Will be drawn with hw acceleration, very fast.
                 * Also the AlphaPatternDrawable is backed by a bitmap
                 * generated only once if the size does not change.
                 */

            if (!_showAlphaPanel || _alphaRect == null || _alphaPatternDrawable == null) return;

            var rect = _alphaRect;

            if (BORDER_WIDTH_PX > 0)
            {
                _borderPaint.Color = _borderColor;
                canvas.DrawRect(rect.Left - BORDER_WIDTH_PX,
                    rect.Top - BORDER_WIDTH_PX,
                    rect.Right + BORDER_WIDTH_PX,
                    rect.Bottom + BORDER_WIDTH_PX,
                    _borderPaint);
            }

            _alphaPatternDrawable.Draw(canvas);

            var hsv = new[] {_hue, _sat, _val};
            int color = Color.HSVToColor(hsv);
            int acolor = Color.HSVToColor(0, hsv);

            _alphaShader = new LinearGradient(rect.Left, rect.Top, rect.Right, rect.Top, new Color(color),
                new Color(acolor),
                Shader.TileMode.Clamp);

            _alphaPaint.SetShader(_alphaShader);

            canvas.DrawRect(rect, _alphaPaint);

            if (_alphaSliderText != null && !_alphaSliderText.Equals(""))
            {
                canvas.DrawText(_alphaSliderText, rect.CenterX(), rect.CenterY() + DrawingUtils.DpToPx(Context, 4),
                    _alphaTextPaint);
            }

            var p = AlphaToPoint(_alpha);

            var r = new RectF
            {
                Left = p.X - _sliderTrackerSizePx / 2,
                Right = p.X + _sliderTrackerSizePx / 2,
                Top = rect.Top - _sliderTrackerOffsetPx,
                Bottom = rect.Bottom + _sliderTrackerOffsetPx
            };

            canvas.DrawRoundRect(r, 2, 2, _hueAlphaTrackerPaint);
        }

        private void DrawHuePanel(Canvas canvas)
        {
            var rect = _hueRect;

            if (BORDER_WIDTH_PX > 0)
            {
                _borderPaint.Color = _borderColor;

                canvas.DrawRect(rect.Left - BORDER_WIDTH_PX,
                    rect.Top - BORDER_WIDTH_PX,
                    rect.Right + BORDER_WIDTH_PX,
                    rect.Bottom + BORDER_WIDTH_PX,
                    _borderPaint);
            }

            if (_hueBackgroundCache == null)
            {
                _hueBackgroundCache = new BitmapCache
                {
                    Bitmap = Bitmap.CreateBitmap(rect.Width(), rect.Height(), Bitmap.Config.Argb8888)
                };
                _hueBackgroundCache.Canvas = new Canvas(_hueBackgroundCache.Bitmap);

                var hueColors = new int[(int) (rect.Height() + 0.5f)];

                // Generate array of all colors, will be drawn as individual lines.
                var h = 360f;
                for (var i = 0; i < hueColors.Length; i++)
                {
                    hueColors[i] = Color.HSVToColor(new[] {h, 1f, 1f});
                    h -= 360f / hueColors.Length;
                }

                // Time to draw the hue color gradient,
                // its drawn as individual lines which
                // will be quite many when the resolution is high
                // and/or the panel is large.
                var linePaint = new Paint {StrokeWidth = 0};
                for (var i = 0; i < hueColors.Length; i++)
                {
                    linePaint.Color = new Color(hueColors[i]);
                    _hueBackgroundCache.Canvas.DrawLine(0, i, _hueBackgroundCache.Bitmap.Width, i, linePaint);
                }
            }

            canvas.DrawBitmap(_hueBackgroundCache.Bitmap, null, rect, null);

            var p = HueToPoint(_hue);

            var r = new RectF
            {
                Left = rect.Left - _sliderTrackerOffsetPx,
                Right = rect.Right + _sliderTrackerOffsetPx,
                Top = p.Y - _sliderTrackerSizePx / 2,
                Bottom = p.Y + _sliderTrackerSizePx / 2
            };

            canvas.DrawRoundRect(r, 2, 2, _hueAlphaTrackerPaint);
        }

        private void DrawSatValPanel(Canvas canvas)
        {
            var rect = _satValRect;

            if (BORDER_WIDTH_PX > 0)
            {
                _borderPaint.Color = _borderColor;
                canvas.DrawRect(_drawingRect.Left, _drawingRect.Top,
                    rect.Right + BORDER_WIDTH_PX,
                    rect.Bottom + BORDER_WIDTH_PX, _borderPaint);
            }

            if (_valShader == null)
            {
                //Black gradient has either not been created or the view has been resized.
                _valShader = new LinearGradient(rect.Left, rect.Top, rect.Left, rect.Bottom, Color.White, Color.Black,
                    Shader.TileMode.Clamp);
            }

            //If the hue has changed we need to recreate the cache.
            if (_satValBackgroundCache == null || _satValBackgroundCache.Value != _hue)
            {
                if (_satValBackgroundCache == null)
                {
                    _satValBackgroundCache = new BitmapCache();
                }

                //We create our bitmap in the cache if it doesn't exist.
                if (_satValBackgroundCache.Bitmap == null)
                {
                    _satValBackgroundCache.Bitmap = Bitmap
                        .CreateBitmap(rect.Width(), rect.Height(), Bitmap.Config.Argb8888);
                }

                //We create the canvas once so we can draw on our bitmap and the hold on to it.
                if (_satValBackgroundCache.Canvas == null)
                {
                    _satValBackgroundCache.Canvas = new Canvas(_satValBackgroundCache.Bitmap);
                }

                int rgb = Color.HSVToColor(new[] {_hue, 1f, 1f});

                _satShader = new LinearGradient(rect.Left, rect.Top, rect.Right, rect.Top, Color.White, new Color(rgb),
                    Shader.TileMode.Clamp);

                var mShader = new ComposeShader(
                    _valShader, _satShader, PorterDuff.Mode.Multiply);
                _satValPaint.SetShader(mShader);

                // Finally we draw on our canvas, the result will be
                // stored in our bitmap which is already in the cache.
                // Since this is drawn on a canvas not rendered on
                // screen it will automatically not be using the
                // hardware acceleration. And this was the code that
                // wasn't supported by hardware acceleration which mean
                // there is no need to turn it of anymore. The rest of
                // the view will still be hw accelerated.
                _satValBackgroundCache.Canvas.DrawRect(0, 0,
                    _satValBackgroundCache.Bitmap.Width,
                    _satValBackgroundCache.Bitmap.Height,
                    _satValPaint);

                //We set the hue value in our cache to which hue it was drawn with,
                //then we know that if it hasn't changed we can reuse our cached bitmap.
                _satValBackgroundCache.Value = _hue;
            }

            // We draw our bitmap from the cached, if the hue has changed
            // then it was just recreated otherwise the old one will be used.
            canvas.DrawBitmap(_satValBackgroundCache.Bitmap, null, rect, null);

            var p = SatValToPoint(_sat, _val);

            _satValTrackerPaint.Color = new Color(0xff, 0x00, 0x00, 0x00);
            canvas.DrawCircle(p.X, p.Y, _circleTrackerRadiusPx - DrawingUtils.DpToPx(Context, 1), _satValTrackerPaint);

            _satValTrackerPaint.Color = new Color(0xff, 0xdd, 0xdd, 0xdd);
            canvas.DrawCircle(p.X, p.Y, _circleTrackerRadiusPx, _satValTrackerPaint);
        }

/**
 * Get the current value of the text
 * that will be shown in the alpha
 * slider.
 *
 * @return the slider text
 */
        public string GetAlphaSliderText()
        {
            return _alphaSliderText;
        }

/**
 * Get the color of the border surrounding all panels.
 */
        public int GetBorderColor()
        {
            return _borderColor;
        }

/**
 * Get the current color this view is showing.
 *
 * @return the current color.
 */
        public Color GetColor()
        {
            return Color.HSVToColor(_alpha, new[] {_hue, _sat, _val});
        }

        private int GetPreferredHeight()
        {
            var height = DrawingUtils.DpToPx(Context, 200);

            if (_showAlphaPanel)
            {
                height += _panelSpacingPx + _alphaPanelHeightPx;
            }

            return height;
        }

        private int GetPreferredWidth()
        {
            //Our preferred width and height is 200dp for the square sat / val rectangle.
            var width = DrawingUtils.DpToPx(Context, 200);

            return width + _huePanelWidthPx + _panelSpacingPx;
        }

/**
 * Get color of the tracker slider on the hue and alpha panel.
 *
 * @return the color value
 */
        public int GetSliderTrackerColor()
        {
            return _sliderTrackerColor;
        }

        private Point HueToPoint(float hue)
        {
            var rect = _hueRect;
            float height = rect.Height();

            var p = new Point
            {
                Y = (int) (height - hue * height / 360f + rect.Top),
                X = rect.Left
            };


            return p;
        }

        private void Init(Context context, IAttributeSet attrs)
        {
            //Load those if set in xml resource file.
            var a = Context.ObtainStyledAttributes(attrs, Resource.Styleable.ColorPickerView);
            _showAlphaPanel = a.GetBoolean(Resource.Styleable.ColorPickerView_cpv_alphaChannelVisible, false);
            _alphaSliderText = a.GetString(Resource.Styleable.ColorPickerView_cpv_alphaChannelText);
            _sliderTrackerColor = a.GetColor(Resource.Styleable.ColorPickerView_cpv_sliderColor,
                unchecked((int) 0xFFBDBDBD));
            _borderColor = a.GetColor(Resource.Styleable.ColorPickerView_cpv_borderColor, unchecked((int) 0xFF6E6E6E));
            a.Recycle();

            ApplyThemeColors(context);

            _huePanelWidthPx = DrawingUtils.DpToPx(Context, HUE_PANEL_WDITH_DP);
            _alphaPanelHeightPx = DrawingUtils.DpToPx(Context, ALPHA_PANEL_HEIGH_DP);
            _panelSpacingPx = DrawingUtils.DpToPx(Context, PANEL_SPACING_DP);
            _circleTrackerRadiusPx = DrawingUtils.DpToPx(Context, CIRCLE_TRACKER_RADIUS_DP);
            _sliderTrackerSizePx = DrawingUtils.DpToPx(Context, _sliderTrackerSizeDp);
            _sliderTrackerOffsetPx = DrawingUtils.DpToPx(Context, _sliderTrackerOffsetDp);

            _mRequiredPadding = Resources.GetDimensionPixelSize(Resource.Dimension.cpv_required_padding);

            InitPaintTools();

            Focusable = true;
            //Needed for receiving trackball motion events.
            FocusableInTouchMode = true;
        }

        private void InitPaintTools()
        {
            _satValPaint = new Paint();
            _satValTrackerPaint = new Paint();
            _hueAlphaTrackerPaint = new Paint();
            _alphaPaint = new Paint();
            _alphaTextPaint = new Paint();
            _borderPaint = new Paint();

            _satValTrackerPaint.SetStyle(Paint.Style.Stroke);
            _satValTrackerPaint.StrokeWidth = DrawingUtils.DpToPx(Context, 2);
            _satValTrackerPaint.AntiAlias = true;

            _hueAlphaTrackerPaint.Color = _sliderTrackerColor;
            _hueAlphaTrackerPaint.SetStyle(Paint.Style.Stroke);
            _hueAlphaTrackerPaint.StrokeWidth = DrawingUtils.DpToPx(Context, 2);
            _hueAlphaTrackerPaint.AntiAlias = true;

            _alphaTextPaint.Color = new Color(0xff, 0x1c, 0x1c, 0x1c);
            _alphaTextPaint.TextSize = DrawingUtils.DpToPx(Context, 14);
            _alphaTextPaint.AntiAlias = true;
            _alphaTextPaint.TextAlign = Paint.Align.Center;
            _alphaTextPaint.FakeBoldText = true;
        }

        private bool MoveTrackersIfNeeded(MotionEvent @event)
        {
            if (_startTouchPoint == null)
            {
                return false;
            }

            var update = false;

            var startX = _startTouchPoint.X;
            var startY = _startTouchPoint.Y;

            if (_hueRect.Contains(startX, startY))
            {
                _hue = PointToHue(@event.GetY());

                update = true;
            }
            else if (_satValRect.Contains(startX, startY))
            {
                var result = PointToSatVal(@event.GetX(), @event.GetY());

                _sat = result[0];
                _val = result[1];

                update = true;
            }
            else if (_alphaRect != null && _alphaRect.Contains(startX, startY))
            {
                _alpha = PointToAlpha((int) @event.GetX());

                update = true;
            }

            return update;
        }

        protected override void OnDraw(Canvas canvas)
        {
            if (_drawingRect.Width() <= 0 || _drawingRect.Height() <= 0)
            {
                return;
            }

            DrawSatValPanel(canvas);
            DrawHuePanel(canvas);
            DrawAlphaPanel(canvas);
        }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            int finalWidth;
            int finalHeight;

            var widthMode = MeasureSpec.GetMode(widthMeasureSpec);
            var heightMode = MeasureSpec.GetMode(heightMeasureSpec);

            var widthAllowed = MeasureSpec.GetSize(widthMeasureSpec) - PaddingLeft - PaddingRight;
            var heightAllowed =
                MeasureSpec.GetSize(heightMeasureSpec) - PaddingBottom - PaddingTop;

            if (widthMode == MeasureSpecMode.Exactly || heightMode == MeasureSpecMode.Exactly)
            {
                //A exact value has been set in either direction, we need to stay within this size.

                if (widthMode == MeasureSpecMode.Exactly && heightMode != MeasureSpecMode.Exactly)
                {
                    //The with has been specified exactly, we need to adopt the height to fit.
                    var h = widthAllowed - _panelSpacingPx - _huePanelWidthPx;

                    if (_showAlphaPanel)
                    {
                        h += _panelSpacingPx + _alphaPanelHeightPx;
                    }

                    if (h > heightAllowed)
                    {
                        //We can't fit the view in this container, set the size to whatever was allowed.
                        finalHeight = heightAllowed;
                    }
                    else
                    {
                        finalHeight = h;
                    }

                    finalWidth = widthAllowed;
                }
                else if (heightMode == MeasureSpecMode.Exactly && widthMode != MeasureSpecMode.Exactly)
                {
                    //The height has been specified exactly, we need to stay within this height and adopt the width.

                    var w = heightAllowed + _panelSpacingPx + _huePanelWidthPx;

                    if (_showAlphaPanel)
                    {
                        w -= _panelSpacingPx + _alphaPanelHeightPx;
                    }

                    if (w > widthAllowed)
                    {
                        //we can't fit within this container, set the size to whatever was allowed.
                        finalWidth = widthAllowed;
                    }
                    else
                    {
                        finalWidth = w;
                    }

                    finalHeight = heightAllowed;
                }
                else
                {
                    //If we get here the dev has set the width and height to exact sizes. For example match_parent or 300dp.
                    //This will mean that the sat/val panel will not be square but it doesn't matter. It will work anyway.
                    //In all other senarios our goal is to make that panel square.

                    //We set the sizes to exactly what we were told.
                    finalWidth = widthAllowed;
                    finalHeight = heightAllowed;
                }
            }
            else
            {
                //If no exact size has been set we try to make our view as big as possible
                //within the allowed space.

                //Calculate the needed width to layout using max allowed height.
                var widthNeeded = heightAllowed + _panelSpacingPx + _huePanelWidthPx;

                //Calculate the needed height to layout using max allowed width.
                var heightNeeded = widthAllowed - _panelSpacingPx - _huePanelWidthPx;

                if (_showAlphaPanel)
                {
                    widthNeeded -= _panelSpacingPx + _alphaPanelHeightPx;
                    heightNeeded += _panelSpacingPx + _alphaPanelHeightPx;
                }

                var widthOk = false;
                var heightOk = false;

                if (widthNeeded <= widthAllowed)
                {
                    widthOk = true;
                }

                if (heightNeeded <= heightAllowed)
                {
                    heightOk = true;
                }

                if (widthOk && heightOk)
                {
                    finalWidth = widthAllowed;
                    finalHeight = heightNeeded;
                }
                else if (!heightOk && widthOk)
                {
                    finalHeight = heightAllowed;
                    finalWidth = widthNeeded;
                }
                else if (!widthOk && heightOk)
                {
                    finalHeight = heightNeeded;
                    finalWidth = widthAllowed;
                }
                else
                {
                    finalHeight = heightAllowed;
                    finalWidth = widthAllowed;
                }
            }

            SetMeasuredDimension(finalWidth + PaddingLeft + PaddingRight,
                finalHeight + PaddingTop + PaddingBottom);
        }

        protected override void OnRestoreInstanceState(IParcelable state)
        {
            if (state is Bundle bundle)
            {
                _alpha = bundle.GetInt("alpha");
                _hue = bundle.GetFloat("hue");
                _sat = bundle.GetFloat("sat");
                _val = bundle.GetFloat("val");
                _showAlphaPanel = bundle.GetBoolean("show_alpha");
                _alphaSliderText = bundle.GetString("alpha_text");

                state = bundle.GetParcelable("instanceState") as IParcelable;
            }

            base.OnRestoreInstanceState(state);
        }

        protected override IParcelable OnSaveInstanceState()
        {
            var state = new Bundle();
            state.PutParcelable("instanceState", base.OnSaveInstanceState());
            state.PutInt("alpha", _alpha);
            state.PutFloat("hue", _hue);
            state.PutFloat("sat", _sat);
            state.PutFloat("val", _val);
            state.PutBoolean("show_alpha", _showAlphaPanel);
            state.PutString("alpha_text", _alphaSliderText);

            return state;
        }


        protected override void OnSizeChanged(int w, int h, int oldw, int oldh)
        {
            base.OnSizeChanged(w, h, oldw, oldh);

            _drawingRect = new Rect
            {
                Left = PaddingLeft,
                Right = w - PaddingRight,
                Top = PaddingTop,
                Bottom = h - PaddingBottom
            };

            //The need to be recreated because they depend on the size of the view.
            _valShader = null;
            _satShader = null;
            _alphaShader = null;

            // Clear those bitmap caches since the size may have changed.
            _satValBackgroundCache = null;
            _hueBackgroundCache = null;

            SetUpSatValRect();
            SetUpHueRect();
            SetUpAlphaRect();
        }


        public override bool OnTouchEvent(MotionEvent @event)
        {
            var update = false;

            switch (@event.Action)
            {
                case MotionEventActions.Down:
                    _startTouchPoint = new Point((int) @event.GetX(), (int) @event.GetY());
                    update = MoveTrackersIfNeeded(@event);
                    break;
                case MotionEventActions.Move:
                    update = MoveTrackersIfNeeded(@event);
                    break;
                case MotionEventActions.Up:
                    _startTouchPoint = null;
                    update = MoveTrackersIfNeeded(@event);
                    break;
            }

            if (update)
            {
                _onColorChangedListener?.OnColorChanged(Color.HSVToColor(_alpha, new[]
                {
                    _hue, _sat, _val
                }));

                Invalidate();
                return true;
            }

            return base.OnTouchEvent(@event);
        }

        private int PointToAlpha(int x)
        {
            var rect = _alphaRect;
            var width = rect.Width();

            if (x < rect.Left)
            {
                x = 0;
            }
            else if (x > rect.Right)
            {
                x = width;
            }
            else
            {
                x = x - rect.Left;
            }

            return 0xff - x * 0xff / width;
        }

        private float PointToHue(float y)
        {
            var rect = _hueRect;

            float height = rect.Height();

            if (y < rect.Top)
            {
                y = 0f;
            }
            else if (y > rect.Bottom)
            {
                y = height;
            }
            else
            {
                y = y - rect.Top;
            }

            var hue = 360f - y * 360f / height;

            return hue;
        }

        private float[] PointToSatVal(float x, float y)
        {
            var rect = _satValRect;
            var result = new float[2];

            float width = rect.Width();
            float height = rect.Width();

            if (x < rect.Left)
            {
                x = 0f;
            }
            else if (x > rect.Right)
            {
                x = width;
            }
            else
            {
                x = x - rect.Left;
            }

            if (y < rect.Top)
            {
                y = 0f;
            }
            else if (y > rect.Bottom)
            {
                y = height;
            }
            else
            {
                y = y - rect.Top;
            }

            result[0] = 1.0f / width * x;
            result[1] = 1.0f - 1.0f / height * y;

            return result;
        }

        private Point SatValToPoint(float sat, float val)
        {
            var rect = _satValRect;
            float height = rect.Height();
            float width = rect.Width();

            var p = new Point
            {
                X = (int) (sat * width + rect.Left),
                Y = (int) ((1f - val) * height + rect.Top)
            };


            return p;
        }

/**
 * Set the text that should be shown in the
 * alpha slider. Set to null to disable text.
 *
 * @param res
 *     string resource id.
 */
        public void SetAlphaSliderText(int res)
        {
            var text = Context.GetString(res);
            SetAlphaSliderText(text);
        }

/**
 * Set the text that should be shown in the
 * alpha slider. Set to null to disable text.
 *
 * @param text
 *     Text that should be shown.
 */
        public void SetAlphaSliderText(string text)
        {
            _alphaSliderText = text;
            Invalidate();
        }

/**
 * Set if the user is allowed to adjust the alpha panel. Default is false.
 * If it is set to false no alpha will be set.
 *
 * @param visible
 *     {@code true} to show the alpha slider
 */
        public void SetAlphaSliderVisible(bool visible)
        {
            if (_showAlphaPanel != visible)
            {
                _showAlphaPanel = visible;

                /*
           * Force recreation.
                 */
                _valShader = null;
                _satShader = null;
                _alphaShader = null;
                _hueBackgroundCache = null;
                _satValBackgroundCache = null;

                RequestLayout();
            }
        }

/**
 * Set the color of the border surrounding all panels.
 *
 * @param color
 *     a color value
 */
        public void SetBorderColor(Color color)
        {
            _borderColor = color;
            Invalidate();
        }

/**
 * Set the color the view should show.
 *
 * @param color
 *     The color that should be selected. #argb
 */
        public void SetColor(int color)
        {
            SetColor(color, false);
        }

/**
 * Set the color this view should show.
 *
 * @param color
 *     The color that should be selected. #argb
 * @param callback
 *     If you want to get a callback to your OnColorChangedListener.
 */
        public void SetColor(int color, bool callback)
        {
            var alpha = Color.GetAlphaComponent(color);
            var red = Color.GetRedComponent(color);
            var blue = Color.GetBlueComponent(color);
            var green = Color.GetGreenComponent(color);

            var hsv = new float[3];

            Color.RGBToHSV(red, green, blue, hsv);

            _alpha = alpha;
            _hue = hsv[0];
            _sat = hsv[1];
            _val = hsv[2];

            if (callback)
            {
                _onColorChangedListener?.OnColorChanged(Color.HSVToColor(_alpha, new[] {_hue, _sat, _val}));
            }

            Invalidate();
        }

/**
 * Set a OnColorChangedListener to get notified when the color
 * selected by the user has changed.
 *
 * @param listener
 *     the listener
 */
        public void SetOnColorChangedListener(IOnColorChangedListener listener)
        {
            _onColorChangedListener = listener;
        }

/**
 * Set the color of the tracker slider on the hue and alpha panel.
 *
 * @param color
 *     a color value
 */
        public void SetSliderTrackerColor(Color color)
        {
            _sliderTrackerColor = color;
            _hueAlphaTrackerPaint.Color = _sliderTrackerColor;
            Invalidate();
        }

        private void SetUpAlphaRect()
        {
            if (!_showAlphaPanel) return;

            var dRect = _drawingRect;

            var left = dRect.Left + BORDER_WIDTH_PX;
            var top = dRect.Bottom - _alphaPanelHeightPx + BORDER_WIDTH_PX;
            var bottom = dRect.Bottom - BORDER_WIDTH_PX;
            var right = dRect.Right - BORDER_WIDTH_PX;

            _alphaRect = new Rect(left, top, right, bottom);

            _alphaPatternDrawable = new AlphaPatternDrawable(DrawingUtils.DpToPx(Context, 4));
            _alphaPatternDrawable.SetBounds(_alphaRect.Left, _alphaRect.Top, _alphaRect.Right, _alphaRect.Bottom);
        }

        private void SetUpHueRect()
        {
            //Calculate the size for the hue slider on the left.
            var dRect = _drawingRect;

            var left = dRect.Right - _huePanelWidthPx + BORDER_WIDTH_PX;
            var top = dRect.Top + BORDER_WIDTH_PX;
            var bottom = dRect.Bottom - BORDER_WIDTH_PX -
                         (_showAlphaPanel ? _panelSpacingPx + _alphaPanelHeightPx : 0);
            var right = dRect.Right - BORDER_WIDTH_PX;

            _hueRect = new Rect(left, top, right, bottom);
        }

        private void SetUpSatValRect()
        {
            //Calculate the size for the big color rectangle.
            var dRect = _drawingRect;

            var left = dRect.Left + BORDER_WIDTH_PX;
            var top = dRect.Top + BORDER_WIDTH_PX;
            var bottom = dRect.Bottom - BORDER_WIDTH_PX;
            var right = dRect.Right - BORDER_WIDTH_PX - _panelSpacingPx - _huePanelWidthPx;

            if (_showAlphaPanel)
            {
                bottom -= _alphaPanelHeightPx + _panelSpacingPx;
            }

            _satValRect = new Rect(left, top, right, bottom);
        }

        #endregion

        #region Nested Types

        private class BitmapCache
        {
            #region Fields

            public Bitmap Bitmap;

            public Canvas Canvas;
            public float Value;

            #endregion
        }

        #endregion
    }
}