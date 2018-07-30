// Copyright 2018 Arcana Studio
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// 
// Based on the work of Jared Rummler : https://github.com/jaredrummler/ColorPicker

using System;
using Android.Content;
using Android.Graphics;
using Android.Support.V4.Graphics;
using Android.Views;
using Android.Widget;

namespace Net.ArcanaStudio.ColorPicker
{
    internal class ColorPaletteAdapter : BaseAdapter
    {
        #region Fields

        /*package*/
        private readonly ColorShape _colorShape;

        /*package*/
        private readonly IOnColorSelectedListener _listener;

        /*package*/
        private int _selectedPosition;

        #endregion

        #region Properties

        /*package*/
        internal Color[] Colors { get; }

        public override int Count => Colors.Length;

        #endregion

        #region Constructors

        public ColorPaletteAdapter(IOnColorSelectedListener listener,
            Color[] colors,
            int selectedPosition,
            ColorShape colorShape)
        {
            _listener = listener;
            Colors = colors;
            _selectedPosition = selectedPosition;
            _colorShape = colorShape;
        }

        #endregion

        #region Members

        public override Java.Lang.Object GetItem(int position)
        {
            throw new NotImplementedException();
        }

        public override long GetItemId(int position)
        {
            return Colors[position];
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            ViewHolder holder;

            if (convertView == null)
            {
                holder = new ViewHolder(parent.Context, this);
                convertView = holder.View;
            }
            else
            {
                holder = (ViewHolder) convertView.Tag;
            }

            holder.Setup(position);
            return convertView;
        }

        internal void SelectNone()
        {
            _selectedPosition = -1;
            NotifyDataSetChanged();
        }

        #endregion

        #region Nested Types

        private sealed class ViewHolder : Java.Lang.Object
        {
            #region Fields

            private readonly ColorPaletteAdapter _colorPaletteAdapter;
            private readonly ColorPanelView _colorPanelView;
            private readonly ImageView _imageView;
            private readonly Color _originalBorderColor;
            private readonly View view;

            #endregion

            #region Properties

            public View View => view;

            #endregion

            #region Constructors

            public ViewHolder(Context context, ColorPaletteAdapter colorpaletteadapter)
            {
                _colorPaletteAdapter = colorpaletteadapter;

                var layoutResId = _colorPaletteAdapter._colorShape == ColorShape.Square
                    ? Resource.Layout.cpv_color_item_square
                    : Resource.Layout.cpv_color_item_circle;

                view = View.Inflate(context, layoutResId, null);
                _colorPanelView = view.FindViewById<ColorPanelView>(Resource.Id.cpv_color_panel_view);
                _imageView = view.FindViewById<ImageView>(Resource.Id.cpv_color_image_view);
                _originalBorderColor = _colorPanelView.GetBorderColor();
                view.Tag = this;
            }

            #endregion

            #region Members

            private void SetColorFilter(int position)
            {
                if (position == _colorPaletteAdapter._selectedPosition &&
                    ColorUtils.CalculateLuminance(_colorPaletteAdapter.Colors[position]) >= 0.65)
                {
                    _imageView.SetColorFilter(Color.Black, PorterDuff.Mode.SrcIn);
                }
                else
                {
                    _imageView.SetColorFilter(null);
                }
            }

            private void SetOnClickListener(int position)
            {
                _colorPanelView.SetOnClickListener(new OnClickListener(v =>
                {
                    if (_colorPaletteAdapter._selectedPosition != position)
                    {
                        _colorPaletteAdapter._selectedPosition = position;
                        _colorPaletteAdapter.NotifyDataSetChanged();
                    }

                    _colorPaletteAdapter._listener.OnColorSelected(_colorPaletteAdapter.Colors[position]);
                }));

                _colorPanelView.SetOnLongClickListener(new OnLongClickListener(v =>
                {
                    _colorPanelView.ShowHint();
                    return true;
                }));
            }

            internal void Setup(int position)
            {
                var color = _colorPaletteAdapter.Colors[position];
                var alpha = Color.GetAlphaComponent(color);
                _colorPanelView.SetColor(color);
                _imageView.SetImageResource(_colorPaletteAdapter._selectedPosition == position
                    ? Resource.Drawable.cpv_preset_checked
                    : 0);
                if (alpha != 255)
                {
                    if (alpha <= ColorPickerDialog.ALPHA_THRESHOLD)
                    {
                        _colorPanelView.SetBorderColor(new Color(color.R, color.G, color.B, (byte) 255));
                        _imageView.SetColorFilter( /*color | 0xFF000000*/Color.Black, PorterDuff.Mode.SrcIn);
                    }
                    else
                    {
                        _colorPanelView.SetBorderColor(_originalBorderColor);
                        _imageView.SetColorFilter(Color.White, PorterDuff.Mode.SrcIn);
                    }
                }
                else
                {
                    SetColorFilter(position);
                }

                SetOnClickListener(position);
            }

            #endregion
        }

        #endregion
    }
}