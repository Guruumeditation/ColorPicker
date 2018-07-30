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
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.OS;
using Android.Support.V7.Preferences;
using Android.Util;
using String = System.String;

namespace Net.ArcanaStudio.ColorPicker
{
    internal class ColorPreferenceSupport : DialogPreference, IColorPickerDialogListener
    {
        #region  Static Fields and Constants

        private static readonly int SIZE_LARGE = 1;
        private static readonly int SIZE_NORMAL = 0;

        #endregion

        #region Fields

        private bool _allowCustom;
        private bool _allowPresets;
        private Bundle _args;
        private Color _color = Color.Black;
        private ColorShape _colorShape;
        private int _dialogTitle;
        private ColorPickerDialog.DialogType _dialogType;

        private IOnShowDialogListener _onShowDialogListener;
        private Color[] _presets;
        private ColorPanelView _preview;
        private int _previewSize;
        private bool _showAlphaSlider;
        private bool _showColorShades;
        private bool _showDialog;

        #endregion

        #region Constructors

        public ColorPreferenceSupport(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            Init(attrs);
        }

        public ColorPreferenceSupport(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs,
            defStyle)
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

        public Bundle GetArgs()
        {
            return _args;
        }

        public int GetColor()
        {
            return GetPersistedInt(Color.Black);
        }

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

            _args = new Bundle();
            var dialogId = a.GetInt(Resource.Styleable.ColorPreference_cpv_dialogID, 0);
            var presetsButtonText = a.GetResourceId(Resource.Styleable.ColorPreference_cpv_presetsButtonText,
                Resource.String.cpv_default_title);
            var customButtonText = a.GetResourceId(Resource.Styleable.ColorPreference_cpv_customButtonText,
                Resource.String.cpv_default_title);
            var selectedButtonText = a.GetResourceId(Resource.Styleable.ColorPreference_cpv_selectedButtonText,
                Resource.String.cpv_default_title);

            _args.PutInt(ColorPickerDialogPreference.ARG_ID, dialogId);
            _args.PutInt(ColorPickerDialogPreference.ARG_TYPE, (int) _dialogType);
            _args.PutInt(ColorPickerDialogPreference.ARG_COLOR, _color);
            _args.PutIntArray(ColorPickerDialogPreference.ARG_PRESETS, _presets.Select(d => d.ToArgb()).ToArray());
            _args.PutBoolean(ColorPickerDialogPreference.ARG_ALPHA, _showAlphaSlider);
            _args.PutBoolean(ColorPickerDialogPreference.ARG_ALLOW_CUSTOM, _allowCustom);
            _args.PutBoolean(ColorPickerDialogPreference.ARG_ALLOW_PRESETS, _allowPresets);
            _args.PutInt(ColorPickerDialogPreference.ARG_DIALOG_TITLE, _dialogTitle);
            _args.PutBoolean(ColorPickerDialogPreference.ARG_SHOW_COLOR_SHADES, _showColorShades);
            _args.PutInt(ColorPickerDialogPreference.ARG_COLOR_SHAPE, (int) _colorShape);
            _args.PutInt(ColorPickerDialogPreference.ARG_PRESETS_BUTTON_TEXT, presetsButtonText);
            _args.PutInt(ColorPickerDialogPreference.ARG_CUSTOM_BUTTON_TEXT, customButtonText);
            _args.PutInt(ColorPickerDialogPreference.ARG_SELECTED_BUTTON_TEXT, selectedButtonText);
            a.Recycle();
        }

        public override void OnBindViewHolder(PreferenceViewHolder view)
        {
            base.OnBindViewHolder(view);
            _preview = (ColorPanelView) view.FindViewById(Resource.Id.cpv_preference_preview_color_panel);
            _preview?.SetColor(_color);
        }

        protected override Java.Lang.Object OnGetDefaultValue(TypedArray a, int index)
        {
            return a.GetInteger(index, Color.Black);
        }

        protected override void OnSetInitialValue(bool restorePersistedValue, Java.Lang.Object defaultValue)
        {
            if (restorePersistedValue)
            {
                _color = new Color(GetPersistedInt(unchecked((int) 0xFF000000)));
            }
            else
            {
                _color = new Color((int) defaultValue);
                PersistInt(_color);
            }

            _preview?.SetColor(_color);
        }

        /**
         * Set the new color
         *
         * @param color
         *     The newly selected color
         */
        public void SaveValue(Color color)
        {
            _color = color;
            PersistInt(_color);
            NotifyChanged();
            CallChangeListener(new ColorWrapper(_color));
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

            void OnShowColorPickerDialog(string title, int currentColor);

            #endregion
        }

        #endregion
    }
}