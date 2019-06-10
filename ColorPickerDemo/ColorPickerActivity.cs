// Copyright 2018 Arcana Studio
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// 
// Based on the work of Jared Rummler : https://github.com/jaredrummler/ColorPicker

using Android.App;
using Android.Graphics;
using Android.OS;
using Android.Preferences;
using Android.Views;
using Android.Widget;
using Net.ArcanaStudio.ColorPicker;

namespace Net.ArcanaStudio.ColorPickerDemo
{
    [Activity(Label = "ColorPickerActivity", Name = "net.ArcanaStudio.ColorPickerDemo.ColorPickerActivity")]
    public class ColorPickerActivity : Activity, IOnColorChangedListener, View.IOnClickListener
    {
        #region Fields

        private ColorPickerView _colorPickerView;
        private ColorPanelView _newColorPanelView;

        #endregion

        #region Interfaces

        public void OnClick(View v)
        {
            switch (v.Id)
            {
                case Resource.Id.okButton:
                    var edit = PreferenceManager.GetDefaultSharedPreferences(this).Edit();
                    edit.PutInt("color_3", _colorPickerView.GetColor());
                    edit.Apply();
                    Finish();
                    break;
                case Resource.Id.cancelButton:
                    Finish();
                    break;
            }
        }

        public void OnColorChanged(Color newColor)
        {
            _newColorPanelView.SetColor(_colorPickerView.GetColor());
        }

        #endregion

        #region Members

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Window.SetFormat(Format.Rgba8888);

            SetContentView(Resource.Layout.activity_color_picker);

            var prefs = PreferenceManager.GetDefaultSharedPreferences(this);
            var initialColor = new Color(prefs.GetInt("color_3", Color.Black.ToArgb()));

            _colorPickerView = FindViewById<ColorPickerView>(Resource.Id.cpv_color_picker_view);
            var colorPanelView = FindViewById<ColorPanelView>(Resource.Id.cpv_color_panel_old);
            _newColorPanelView = FindViewById<ColorPanelView>(Resource.Id.cpv_color_panel_new);

            var btnOK = FindViewById<Button>(Resource.Id.okButton);
            var btnCancel = FindViewById<Button>(Resource.Id.cancelButton);

            ((LinearLayout) colorPanelView.Parent).SetPadding(_colorPickerView.PaddingLeft, 0,
                _colorPickerView.PaddingRight, 0);

            _colorPickerView.SetOnColorChangedListener(this);
            _colorPickerView.SetColor(initialColor, true);
            colorPanelView.SetColor(initialColor);

            btnOK.SetOnClickListener(this);
            btnCancel.SetOnClickListener(this);
        }

        #endregion
    }
}