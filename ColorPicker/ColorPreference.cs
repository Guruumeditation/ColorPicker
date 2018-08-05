// Copyright 2018 Arcana Studio
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// 
// Based on the work of Jared Rummler : https://github.com/jaredrummler/ColorPicker

using System;
using System.Linq;
using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.Preferences;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Java.Lang;
using Object = Java.Lang.Object;
using String = System.String;

namespace Net.ArcanaStudio.ColorPicker
{
    public class ColorPreference : Preference, IColorPickerDialogListener
    {
        #region  Static Fields and Constants

        private static readonly int SIZE_LARGE = 1;
        private static readonly int SIZE_NORMAL = 0;

        #endregion

        #region Fields

        private bool _allowCustom;
        private bool _allowPresets;
        private Color _color = Color.Black;
        private ColorShape _colorShape;
        private int _dialogTitle;
        private ColorPickerDialog.DialogType _dialogType;

        private IOnShowDialogListener _onShowDialogListener;
        private Color[] _presets;
        private int _previewSize;
        private bool _showAlphaSlider;
        private bool _showColorShades;
        private bool _showDialog;

        #endregion

        #region Constructors

        public ColorPreference(System.IntPtr reference, JniHandleOwnership transfer) : base(reference, transfer)
        {
        }

        public ColorPreference(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            Init(attrs);
        }

        public ColorPreference(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            Init(attrs);
        }

        #endregion

        #region Interfaces

        public void OnColorSelected(int dialogId, Color color)
        {
            SaveValue(color);
        }

        public void OnDialogDismissed(int dialogId)
        {
            // no-op
        }

        #endregion

        #region Members

        /**
         * The tag used for the {@link ColorPickerDialog}.
         *
         * @return The tag
         */
        public String GetFragmentTag()
        {
            return "color_" + Key;
        }

        /**
         * Get the colors that will be shown in the {@link ColorPickerDialog}.
         *
         * @return An array of color ints
         */
        public Color[] GetPresets()
        {
            return _presets;
        }

        private void Init(IAttributeSet attrs)
        {
            Persistent = true;
            var a = Context.ObtainStyledAttributes(attrs, Resource.Styleable.ColorPreference);
            _showDialog = a.GetBoolean(Resource.Styleable.ColorPreference_cpv_showDialog, true);
            //noinspection WrongConstant
            _dialogType = (ColorPickerDialog.DialogType) a.GetInt(Resource.Styleable.ColorPreference_cpv_dialogType,
                (int) ColorPickerDialog.DialogType.Preset);
            _colorShape = (ColorShape) a.GetInt(Resource.Styleable.ColorPreference_cpv_colorShape,
                (int) ColorShape.Circle);
            _allowPresets = a.GetBoolean(Resource.Styleable.ColorPreference_cpv_allowPresets, true);
            _allowCustom = a.GetBoolean(Resource.Styleable.ColorPreference_cpv_allowCustom, true);
            _showAlphaSlider = a.GetBoolean(Resource.Styleable.ColorPreference_cpv_showAlphaSlider, false);
            _showColorShades = a.GetBoolean(Resource.Styleable.ColorPreference_cpv_showColorShades, true);
            _previewSize = a.GetInt(Resource.Styleable.ColorPreference_cpv_previewSize, SIZE_NORMAL);
            var presetsResId = a.GetResourceId(Resource.Styleable.ColorPreference_cpv_colorPresets, 0);
            _dialogTitle = a.GetResourceId(Resource.Styleable.ColorPreference_cpv_dialogTitle,
                Resource.String.cpv_default_title);
            if (presetsResId != 0)
            {
                _presets = Context.Resources.GetIntArray(presetsResId).Select(d => new Color(d)).ToArray();
            }
            else
            {
                _presets = ColorPickerDialog.MATERIAL_COLORS;
            }

            if (_colorShape == ColorShape.Circle)
            {
                WidgetLayoutResource = _previewSize == SIZE_LARGE
                    ? Resource.Layout.cpv_preference_circle_large
                    : Resource.Layout.cpv_preference_circle;
            }
            else
            {
                WidgetLayoutResource = _previewSize == SIZE_LARGE
                    ? Resource.Layout.cpv_preference_square_large
                    : Resource.Layout.cpv_preference_square;
            }

            a.Recycle();
        }

        protected override void OnAttachedToActivity()
        {
            base.OnAttachedToActivity();

            if (_showDialog)
            {
                var activity = (Activity) Context;
                var fragment =
                    (ColorPickerDialog) activity.FragmentManager.FindFragmentByTag(GetFragmentTag());
                // re-bind preference to fragment
                fragment?.SetColorPickerDialogListener(this);
            }
        }

        protected override void OnBindView(View view)
        {
            base.OnBindView(view);
            var preview = view.FindViewById<ColorPanelView>(Resource.Id.cpv_preference_preview_color_panel);
            preview?.SetColor(_color);
        }

        protected override void OnClick()
        {
            base.OnClick();
            if (_onShowDialogListener != null)
            {
                _onShowDialogListener.OnShowColorPickerDialog(Title, _color);
            }
            else if (_showDialog)
            {
                var dialog = ColorPickerDialog.NewBuilder()
                    .SetDialogType(_dialogType)
                    .SetDialogTitle(_dialogTitle)
                    .SetColorShape(_colorShape)
                    .SetPresets(_presets)
                    .SetAllowPresets(_allowPresets)
                    .SetAllowCustom(_allowCustom)
                    .SetShowAlphaSlider(_showAlphaSlider)
                    .SetShowColorShades(_showColorShades)
                    .SetColor(_color)
                    .Create();
                dialog.SetColorPickerDialogListener(this);
                var activity = (Activity) Context;
                dialog.Show(activity.FragmentManager, GetFragmentTag());
            }
        }

        protected override Object OnGetDefaultValue(TypedArray a, int index)
        {
            return a.GetInteger(index, Color.Black);
        }

        protected override void OnSetInitialValue(bool restorePersistedValue, Object defaultValue)
        {
            if (restorePersistedValue)
            {
                _color = new Color(GetPersistedInt(unchecked((int) 0xFF000000)));
            }
            else
            {
                if (defaultValue is Number n)
                    _color = new Color(n.IntValue());
                else if (defaultValue is ColorWrapper cw)
                    _color = cw.Data;
                PersistInt(_color);
            }
        }

        /**
         * Set the new color
         *
         * @param color
         *     The newly selected color
         */
        public void SaveValue(Color color)
        {
            if (_color != color)
            {
                _color = color;
                PersistInt(_color);
                NotifyChanged();
                CallChangeListener(new Integer(color.ToArgb()));
            }
        }

        /**
         * The listener used for showing the {@link ColorPickerDialog}.
         * Call {@link #saveValue(int)} after the user chooses a color.
         * If this is set then it is up to you to show the dialog.
         *
         * @param listener
         *     The listener to show the dialog
         */
        public void SetOnShowDialogListener(IOnShowDialogListener listener)
        {
            _onShowDialogListener = listener;
        }

        /**
         * Set the colors shown in the {@link ColorPickerDialog}.
         *
         * @param presets An array of color ints
         */
        public void SetPresets(Color[] presets)
        {
            _presets = presets ?? throw new ArgumentNullException(nameof(presets));
        }

        #endregion

        #region Nested Types

        public interface IOnShowDialogListener
        {
            #region Members

            void OnShowColorPickerDialog(String title, int currentColor);

            #endregion
        }

        #endregion
    }
}