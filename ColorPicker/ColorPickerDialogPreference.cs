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
using Android.Graphics;
using Android.OS;
using Android.Support.V4.Graphics;
using Android.Text;
using Android.Views;
using Android.Support.V7.Preferences;
using Android.Util;
using Android.Views.InputMethods;
using Android.Widget;
using Java.Lang;
using Java.Util;
using DialogFragment = Android.Support.V4.App.DialogFragment;
using Exception = System.Exception;
using Math = Java.Lang.Math;
using System.Globalization;

namespace Net.ArcanaStudio.ColorPicker
{
    public class ColorPickerDialogPreference : PreferenceDialogFragmentCompat, View.IOnTouchListener,
        IOnColorChangedListener, ITextWatcher
    {
        #region  Static Fields and Constants

        internal static int ALPHA_THRESHOLD = 165;
        internal static string ARG_ALLOW_CUSTOM = "allowCustom";
        internal static string ARG_ALLOW_PRESETS = "allowPresets";
        internal static string ARG_ALPHA = "alpha";
        internal static string ARG_COLOR = "color";
        internal static string ARG_COLOR_SHAPE = "colorShape";
        internal static string ARG_CUSTOM_BUTTON_TEXT = "customButtonText";
        internal static string ARG_DIALOG_TITLE = "dialogTitle";
        internal static string ARG_ID = "id";
        internal static string ARG_PRESETS = "presets";
        internal static string ARG_PRESETS_BUTTON_TEXT = "presetsButtonText";
        internal static string ARG_SELECTED_BUTTON_TEXT = "selectedButtonText";
        internal static string ARG_SHOW_COLOR_SHADES = "showColorShades";

        internal static string ARG_TYPE = "dialogType";

        /**
         * Material design colors used as the default color presets
         */
        public static Color[] MATERIAL_COLORS =
        {
            new Color(0xFF, 0xF4, 0x43, 0x36), // RED 500
            new Color(0xFF, 0xE9, 0x1E, 0x63), // PINK 500
            new Color(0xFF, 0xFF, 0x2C, 0x93), // LIGHT PINK 500
            new Color(0xFF, 0x9C, 0x27, 0xB0), // PURPLE 500
            new Color(0xFF, 0x67, 0x3A, 0xB7), // DEEP PURPLE 500
            new Color(0xFF, 0x3F, 0x51, 0xB5), // INDIGO 500
            new Color(0xFF, 0x21, 0x96, 0xF3), // BLUE 500
            new Color(0xFF, 0x03, 0xA9, 0xF4), // LIGHT BLUE 500
            new Color(0xFF, 0x00, 0xBC, 0xD4), // CYAN 500
            new Color(0xFF, 0x00, 0x96, 0x88), // TEAL 500
            new Color(0xFF, 0x4C, 0xAF, 0x50), // GREEN 500
            new Color(0xFF, 0x8B, 0xC3, 0x4A), // LIGHT GREEN 500
            new Color(0xFF, 0xCD, 0xDC, 0x39), // LIME 500
            new Color(0xFF, 0xFF, 0xEB, 0x3B), // YELLOW 500
            new Color(0xFF, 0xFF, 0xC1, 0x07), // AMBER 500
            new Color(0xFF, 0xFF, 0x98, 0x00), // ORANGE 500
            new Color(0xFF, 0x79, 0x55, 0x48), // BROWN 500
            new Color(0xFF, 0x60, 0x7D, 0x8B), // BLUE GREY 500
            new Color(0xFF, 0x9E, 0x9E, 0x9E), // GREY 500
        };

        #endregion

        #region Fields

        // -- PRESETS --------------------------
        private ColorPaletteAdapter _adapter;
        private Color _color;

        // -- CUSTOM ---------------------------
        private ColorPickerView _colorPicker;

        private IColorPickerDialogListener _colorPickerDialogListener;

        private ColorShape _colorShape;
        private int _customButtonStringRes;
        private int _dialogId;
        private ColorPickerDialog.DialogType _dialogType;
        private bool _fromEditText;
        private EditText _hexEditText;
        private ColorPanelView _newColorPanel;
        private Color[] _presets;
        private int _presetsButtonStringRes;
        private FrameLayout _rootView;
        private LinearLayout _shadesLayout;
        private bool _showAlphaSlider;
        private bool _showColorShades;

        private TextView _transparencyPercText;
        private SeekBar _transparencySeekBar;

        #endregion

        #region Interfaces

        public void OnColorChanged(Color newColor)
        {
            _color = newColor;
            _newColorPanel.SetColor(newColor);
            if (!_fromEditText)
            {
                SetHex(newColor);
                if (_hexEditText.HasFocus)
                {
                    var imm = (InputMethodManager) Activity.GetSystemService(Context.InputMethodService);
                    imm.HideSoftInputFromWindow(_hexEditText.WindowToken, 0);
                    _hexEditText.ClearFocus();
                }
            }

            _fromEditText = false;
        }


        public bool OnTouch(View v, MotionEvent @event)
        {
            if (v != _hexEditText && _hexEditText.HasFocus)
            {
                _hexEditText.ClearFocus();
                var imm = (InputMethodManager) Activity.GetSystemService(Context.InputMethodService);
                imm.HideSoftInputFromWindow(_hexEditText.WindowToken, 0);
                _hexEditText.ClearFocus();
                return true;
            }

            return false;
        }


        public void AfterTextChanged(IEditable s)
        {
            if (_hexEditText.IsFocused)
            {
                var color = ParseColorString(s.ToString());
                if (color != _colorPicker.GetColor())
                {
                    _fromEditText = true;
                    _colorPicker.SetColor(color, true);
                }
            }
        }


        public void BeforeTextChanged(ICharSequence s, int start, int count, int after)
        {
        }


        public void OnTextChanged(ICharSequence s, int start, int before, int count)
        {
        }

        #endregion

        #region Members

        private void CreateColorShades(Color color)
        {
            var colorShades = GetColorShades(color);

            if (_shadesLayout.ChildCount != 0)
            {
                for (var i = 0; i < _shadesLayout.ChildCount; i++)
                {
                    var layout = (FrameLayout) _shadesLayout.GetChildAt(i);
                    var cpv = layout.FindViewById<ColorPanelView>(Resource.Id.cpv_color_panel_view);
                    var iv = layout.FindViewById<ImageView>(Resource.Id.cpv_color_image_view);
                    cpv.SetColor(colorShades[i]);
                    cpv.Tag = false;
                    iv.SetImageDrawable(null);
                }

                return;
            }

            var horizontalPadding = Resources.GetDimensionPixelSize(Resource.Dimension.cpv_item_horizontal_padding);

            foreach (var colorShade in colorShades)
            {
                var layoutResId = _colorShape == ColorShape.Square
                    ? Resource.Layout.cpv_color_item_square
                    : Resource.Layout.cpv_color_item_circle;

                var view = View.Inflate(Activity, layoutResId, null);
                var colorPanelView = view.FindViewById<ColorPanelView>(Resource.Id.cpv_color_panel_view);

                var @params = (ViewGroup.MarginLayoutParams) colorPanelView.LayoutParameters;
                @params.LeftMargin = @params.RightMargin = horizontalPadding;
                colorPanelView.LayoutParameters = @params;
                colorPanelView.SetColor(colorShade);
                _shadesLayout.AddView(view);

                colorPanelView.Post(new Runnable(() => colorPanelView.SetColor(colorShade)));

                colorPanelView.SetOnClickListener(new OnClickListener(v =>
                {
                    if ((v.Tag is BoolWrapper bw) && bw.Data)
                    {
                        _colorPickerDialogListener.OnColorSelected(_dialogId, color);
                        Dismiss();
                        return; // already selected
                    }

                    color = colorPanelView.GetColor();
                    _adapter.SelectNone();
                    for (var i = 0; i < _shadesLayout.ChildCount; i++)
                    {
                        var layout = (FrameLayout) _shadesLayout.GetChildAt(i);
                        var cpv =
                            layout.FindViewById<ColorPanelView>(Resource.Id.cpv_color_panel_view);
                        var iv = layout.FindViewById<ImageView>(Resource.Id.cpv_color_image_view);
                        iv.SetImageResource(cpv == v ? Resource.Drawable.cpv_preset_checked : 0);
                        if (cpv == v && ColorUtils.CalculateLuminance(cpv.GetColor()) >= 0.65 ||
                            Color.GetAlphaComponent(cpv.GetColor()) <= ALPHA_THRESHOLD)
                        {
                            iv.SetColorFilter(Color.Black, PorterDuff.Mode.SrcIn);
                        }
                        else
                        {
                            iv.SetColorFilter(null);
                        }

                        cpv.Tag = cpv == v;
                    }
                }));

                colorPanelView.SetOnLongClickListener(new OnLongClickListener(v =>
                {
                    colorPanelView.ShowHint();
                    return true;
                }));
            }
        }

        private View CreatePickerView()
        {
            var contentView = View.Inflate(Activity, Resource.Layout.cpv_dialog_color_picker, null);
            _colorPicker = contentView.FindViewById<ColorPickerView>(Resource.Id.cpv_color_picker_view);
            var oldColorPanel = contentView.FindViewById<ColorPanelView>(Resource.Id.cpv_color_panel_old);
            _newColorPanel = contentView.FindViewById<ColorPanelView>(Resource.Id.cpv_color_panel_new);
            var arrowRight = contentView.FindViewById<ImageView>(Resource.Id.cpv_arrow_right);
            _hexEditText = contentView.FindViewById<EditText>(Resource.Id.cpv_hex);

            try
            {
                var value = new TypedValue();
                var typedArray =
                    Activity.ObtainStyledAttributes(value.Data, new[] {Android.Resource.Attribute.TextColorPrimary});
                var arrowColor = typedArray.GetColor(0, Color.Black);
                typedArray.Recycle();
                arrowRight.SetColorFilter(arrowColor);
            }
            catch (Exception)
            {
                // ignored
            }

            _colorPicker.SetAlphaSliderVisible(_showAlphaSlider);
            oldColorPanel.SetColor(new Color(Arguments.GetInt(ARG_COLOR)));
            _colorPicker.SetColor(_color, true);
            _newColorPanel.SetColor(_color);
            SetHex(_color);

            if (!_showAlphaSlider)
            {
                _hexEditText.SetFilters(new IInputFilter[] {new InputFilterLengthFilter(6)});
            }

            _newColorPanel.SetOnClickListener(new OnClickListener(v =>
            {
                if (_newColorPanel.GetColor() == _color)
                {
                    OnColorSelected();
                    Dismiss();
                }
            }));

            contentView.SetOnTouchListener(this);
            _colorPicker.SetOnColorChangedListener(this);
            _hexEditText.AddTextChangedListener(this);

            _hexEditText.OnFocusChangeListener = new OnFocusChangeListener((v, hasFocus) =>
            {
                if (hasFocus)
                {
                    var imm = (InputMethodManager) Activity.GetSystemService(Context.InputMethodService);
                    imm.ShowSoftInput(_hexEditText, ShowFlags.Implicit);
                }
            });
            return contentView;
        }

        private View CreatePresetsView()
        {
            var contentView = View.Inflate(Activity, Resource.Layout.cpv_dialog_presets, null);
            _shadesLayout = contentView.FindViewById<LinearLayout>(Resource.Id.shades_layout);
            _transparencySeekBar = contentView.FindViewById<SeekBar>(Resource.Id.transparency_seekbar);
            _transparencyPercText = contentView.FindViewById<TextView>(Resource.Id.transparency_text);
            var gridView = contentView.FindViewById<GridView>(Resource.Id.gridView);

            LoadPresets();

            if (_showColorShades)
            {
                CreateColorShades(_color);
            }
            else
            {
                _shadesLayout.Visibility = ViewStates.Gone;
                contentView.FindViewById(Resource.Id.shades_divider).Visibility = ViewStates.Gone;
            }

            _adapter = new ColorPaletteAdapter(new OnColorSelectedListener(newColor =>
            {
                if (_color == newColor)
                {
                    OnColorSelected();
                    Dismiss();
                    return;
                }

                _color = newColor;
                if (_showColorShades)
                {
                    CreateColorShades(_color);
                }
            }), _presets, GetSelectedItemPosition(), _colorShape);

            gridView.Adapter = _adapter;

            if (_showAlphaSlider)
            {
                SetupTransparency();
            }
            else
            {
                contentView.FindViewById(Resource.Id.transparency_layout).Visibility = ViewStates.Gone;
                contentView.FindViewById(Resource.Id.transparency_title).Visibility = ViewStates.Gone;
            }

            return contentView;
        }

        private ColorPreferenceSupport GetColorPreference()
        {
            return (ColorPreferenceSupport) Preference;
        }

        private Color[] GetColorShades(Color color)
        {
            return new[]
            {
                ShadeColor(color, 0.9),
                ShadeColor(color, 0.7),
                ShadeColor(color, 0.5),
                ShadeColor(color, 0.333),
                ShadeColor(color, 0.166),
                ShadeColor(color, -0.125),
                ShadeColor(color, -0.25),
                ShadeColor(color, -0.375),
                ShadeColor(color, -0.5),
                ShadeColor(color, -0.675),
                ShadeColor(color, -0.7),
                ShadeColor(color, -0.775),
            };
        }

        private int GetSelectedItemPosition()
        {
            for (var i = 0; i < _presets.Length; i++)
            {
                if (_presets[i] == _color)
                {
                    return i;
                }
            }

            return -1;
        }

        private void LoadPresets()
        {
            var alpha = Color.GetAlphaComponent(_color);
            _presets = GetColorPreference().GetArgs().GetIntArray(ARG_PRESETS).Select(d => new Color(d)).ToArray();
            if (!_presets.Any()) _presets = MATERIAL_COLORS;
            var isMaterialColors = _presets == MATERIAL_COLORS;

            if (alpha != 255)
            {
                // add alpha to the presets
                for (var i = 0; i < _presets.Length; i++)
                {
                    int color = _presets[i];
                    var red = Color.GetRedComponent(color);
                    var green = Color.GetGreenComponent(color);
                    var blue = Color.GetBlueComponent(color);
                    _presets[i] = Color.Argb(alpha, red, green, blue);
                }
            }

            _presets = UnshiftIfNotExists(_presets, _color);
            if (isMaterialColors && _presets.Length == 19)
            {
                // Add black to have a total of 20 colors if the current color is in the material color palette
                _presets = PushIfNotExists(_presets, Color.Argb(alpha, 0, 0, 0));
            }
        }

        public static DialogFragment NewInstance(string key)
        {
            var colorPickerDialogPreference = new ColorPickerDialogPreference();
            var b = new Bundle(1);
            b.PutString("key", key);
            colorPickerDialogPreference.Arguments = b;
            return colorPickerDialogPreference;
        }

        public override void OnAttach(Context context)
        {
            base.OnAttach(context);

            if (TargetFragment is IColorPickerDialogListener)
            {
                _colorPickerDialogListener = (IColorPickerDialogListener) TargetFragment;
            }
        }

        private void OnColorSelected()
        {
            var preference = GetColorPreference();
            if (preference.CallChangeListener(new ColorWrapper(_color)))
            {
                preference.OnColorSelected(_dialogId, _color);
            }
        }

        public override Dialog OnCreateDialog(Bundle savedInstanceState)
        {
            _dialogId = GetColorPreference().GetArgs().GetInt(ARG_ID);
            _showAlphaSlider = GetColorPreference().GetArgs().GetBoolean(ARG_ALPHA);
            _showColorShades = GetColorPreference().GetArgs().GetBoolean(ARG_SHOW_COLOR_SHADES);
            _colorShape = (ColorShape) GetColorPreference().GetArgs().GetInt(ARG_COLOR_SHAPE);
            if (savedInstanceState == null)
            {
                _color = new Color(GetColorPreference().GetColor());
                _dialogType = (ColorPickerDialog.DialogType) GetColorPreference().GetArgs().GetInt(ARG_TYPE);
            }
            else
            {
                _color = new Color(savedInstanceState.GetInt(ARG_COLOR));
                _dialogType = (ColorPickerDialog.DialogType) savedInstanceState.GetInt(ARG_TYPE);
            }

            _rootView = new FrameLayout(Activity);
            if (_dialogType == ColorPickerDialog.DialogType.Custom)
            {
                _rootView.AddView(CreatePickerView());
            }
            else if (_dialogType == ColorPickerDialog.DialogType.Preset)
            {
                _rootView.AddView(CreatePresetsView());
            }

            var selectedButtonStringRes = GetColorPreference().GetArgs().GetInt(ARG_SELECTED_BUTTON_TEXT);
            if (selectedButtonStringRes == 0)
            {
                selectedButtonStringRes = Resource.String.cpv_select;
            }

            var builder = new AlertDialog.Builder(Activity).SetView(_rootView).SetPositiveButton(
                selectedButtonStringRes, new DialogInterfaceOnClickListener(
                    (d, w) => { OnColorSelected(); }));

            var dialogTitleStringRes = GetColorPreference().GetArgs().GetInt(ARG_DIALOG_TITLE);
            if (dialogTitleStringRes != 0)
            {
                builder.SetTitle(dialogTitleStringRes);
            }

            _presetsButtonStringRes = GetColorPreference().GetArgs().GetInt(ARG_PRESETS_BUTTON_TEXT);
            _customButtonStringRes = GetColorPreference().GetArgs().GetInt(ARG_CUSTOM_BUTTON_TEXT);

            int neutralButtonStringRes;
            if (_dialogType == ColorPickerDialog.DialogType.Custom &&
                GetColorPreference().GetArgs().GetBoolean(ARG_ALLOW_PRESETS))
            {
                neutralButtonStringRes =
                    (_presetsButtonStringRes != 0 ? _presetsButtonStringRes : Resource.String.cpv_presets);
            }
            else if (_dialogType == ColorPickerDialog.DialogType.Preset &&
                     GetColorPreference().GetArgs().GetBoolean(ARG_ALLOW_CUSTOM))
            {
                neutralButtonStringRes =
                    (_customButtonStringRes != 0 ? _customButtonStringRes : Resource.String.cpv_custom);
            }
            else
            {
                neutralButtonStringRes = 0;
            }

            if (neutralButtonStringRes != 0)
            {
                builder.SetNeutralButton(neutralButtonStringRes, (IDialogInterfaceOnClickListener) null);
            }

            return builder.Create();
        }

        public override void OnDialogClosed(bool positiveResult)
        {
            if (positiveResult)
            {
                OnColorSelected();
            }
            else
            {
                _colorPickerDialogListener.OnDialogDismissed(_dialogId);
            }
        }

        public override void OnSaveInstanceState(Bundle outState)
        {
            outState.PutInt(ARG_COLOR, _color);
            outState.PutInt(ARG_TYPE, (int) _dialogType);
            base.OnSaveInstanceState(outState);
        }

        public override void OnStart()
        {
            base.OnStart();
            var dialog = (AlertDialog) Dialog;

            // http://stackoverflow.com/a/16972670/1048340
            //noinspection ConstantConditions
            dialog.Window.ClearFlags(WindowManagerFlags.NotFocusable | WindowManagerFlags.AltFocusableIm);
            dialog.Window.SetSoftInputMode(SoftInput.StateVisible);

            // Do not dismiss the dialog when clicking the neutral button.
            var neutralButton = dialog.GetButton((int) DialogButtonType.Neutral);
            neutralButton?.SetOnClickListener(new OnClickListener(v =>
            {
                _rootView.RemoveAllViews();
                switch (_dialogType)
                {
                    case ColorPickerDialog.DialogType.Custom:
                        _dialogType = ColorPickerDialog.DialogType.Preset;
                        ((Button) v).SetText(_customButtonStringRes != 0
                            ? _customButtonStringRes
                            : Resource.String.cpv_custom);
                        _rootView.AddView(CreatePresetsView());
                        break;
                    case ColorPickerDialog.DialogType.Preset:
                        _dialogType = ColorPickerDialog.DialogType.Custom;
                        ((Button) v).SetText(_presetsButtonStringRes != 0
                            ? _presetsButtonStringRes
                            : Resource.String.cpv_presets);
                        _rootView.AddView(CreatePickerView());
                        break;
                }
            }));
        }

// -- endregion --

// region Presets Picker

        private int ParseColorString(string colorString)
        {
            int a, r, g, b = 0;
            if (colorString.StartsWith("#"))
            {
                colorString = colorString.Substring(1);
            }

            if (colorString.Length == 0)
            {
                r = 0;
                a = 255;
                g = 0;
            }
            else if (colorString.Length <= 2)
            {
                a = 255;
                r = 0;
                b = Integer.ParseInt(colorString, 16);
                g = 0;
            }
            else if (colorString.Length == 3)
            {
                a = 255;
                r = Integer.ParseInt(colorString.Substring(0, 1), 16);
                g = Integer.ParseInt(colorString.Substring(1, 2), 16);
                b = Integer.ParseInt(colorString.Substring(2, 3), 16);
            }
            else if (colorString.Length == 4)
            {
                a = 255;
                r = Integer.ParseInt(colorString.Substring(0, 2), 16);
                g = r;
                r = 0;
                b = Integer.ParseInt(colorString.Substring(2, 4), 16);
            }
            else if (colorString.Length == 5)
            {
                a = 255;
                r = Integer.ParseInt(colorString.Substring(0, 1), 16);
                g = Integer.ParseInt(colorString.Substring(1, 3), 16);
                b = Integer.ParseInt(colorString.Substring(3, 5), 16);
            }
            else if (colorString.Length == 6)
            {
                a = 255;
                r = Integer.ParseInt(colorString.Substring(0, 2), 16);
                g = Integer.ParseInt(colorString.Substring(2, 4), 16);
                b = Integer.ParseInt(colorString.Substring(4, 6), 16);
            }
            else if (colorString.Length == 7)
            {
                a = Integer.ParseInt(colorString.Substring(0, 1), 16);
                r = Integer.ParseInt(colorString.Substring(1, 3), 16);
                g = Integer.ParseInt(colorString.Substring(3, 5), 16);
                b = Integer.ParseInt(colorString.Substring(5, 7), 16);
            }
            else if (colorString.Length == 8)
            {
                a = Integer.ParseInt(colorString.Substring(0, 2), 16);
                r = Integer.ParseInt(colorString.Substring(2, 4), 16);
                g = Integer.ParseInt(colorString.Substring(4, 6), 16);
                b = Integer.ParseInt(colorString.Substring(6, 8), 16);
            }
            else
            {
                b = -1;
                g = -1;
                r = -1;
                a = -1;
            }

            return Color.Argb(a, r, g, b);
        }

        private Color[] PushIfNotExists(Color[] array, Color value)
        {
            var present = false;
            foreach (var i in array)
            {
                if (i == value)
                {
                    present = true;
                    break;
                }
            }

            if (!present)
            {
                var newArray = new Color[array.Length + 1];
                newArray[newArray.Length - 1] = value;
                Array.Copy(array, newArray, newArray.Length - 1);
                return newArray;
            }

            return array;
        }

// region Custom Picker

/**
 * Set the callback
 *
 * @param colorPickerDialogListener The callback invoked when a color is selected or the dialog is dismissed.
 */
        public void SetColorPickerDialogListener(IColorPickerDialogListener colorPickerDialogListener)
        {
            _colorPickerDialogListener = colorPickerDialogListener;
        }

        private void SetHex(int color)
        {
            _hexEditText.Text = _showAlphaSlider ? $"{color:X8}" : $"{0xFFFFFF & color:X6}";
        }

        private void SetupTransparency()
        {
            var progress = 255 - Color.GetAlphaComponent(_color);
            _transparencySeekBar.Max = 255;
            _transparencySeekBar.Progress = progress;
            var percentage = (int) ((double) progress * 100 / 255);
            _transparencyPercText.Text = string.Format(new CultureInfo(Locale.English.Language), "{0:d}%", percentage);
            _transparencySeekBar.SetOnSeekBarChangeListener(new OnSeekBarChangeListener((bar, p, f) =>
            {
                var thepercentage = (int) ((double) p * 100 / 255);
                _transparencyPercText.Text =
                    string.Format(new CultureInfo(Locale.English.Language), "{0:d}%", thepercentage);

                var alpha = 255 - progress;
                // update items in GridView:
                for (var i = 0; i < _adapter.Colors.Length; i++)
                {
                    int color = _adapter.Colors[i];
                    var red = Color.GetRedComponent(color);
                    var green = Color.GetGreenComponent(color);
                    var blue = Color.GetBlueComponent(color);
                    _adapter.Colors[i] = Color.Argb(alpha, red, green, blue);
                }


                _adapter.NotifyDataSetChanged();
                // update shades:
                for (var i = 0; i < _shadesLayout.ChildCount; i++)
                {
                    var layout = (FrameLayout) _shadesLayout.GetChildAt(i);
                    var cpv = layout.FindViewById<ColorPanelView>(Resource.Id.cpv_color_panel_view);
                    var iv = layout.FindViewById<ImageView>(Resource.Id.cpv_color_image_view);
                    if (layout.Tag == null)
                    {
                        // save the original border color
                        layout.Tag = new ColorWrapper(cpv.GetBorderColor());
                    }

                    var color = cpv.GetColor();
                    color = Color.Argb(alpha, Color.GetRedComponent(color), Color.GetGreenComponent(color),
                        Color.GetBlueComponent(color));
                    if (alpha <= ALPHA_THRESHOLD)
                    {
                        cpv.SetBorderColor(new Color(color.R, color.G, color.B, (byte) 255));
                    }
                    else
                    {
                        cpv.SetBorderColor(((ColorWrapper) layout.Tag).Data);
                    }

                    if ((cpv.Tag is BoolWrapper bw) && bw.Data)
                    {
                        // The alpha changed on the selected shaded color. Update the checkmark color filter.
                        if (alpha <= ALPHA_THRESHOLD)
                        {
                            iv.SetColorFilter(Color.Black, PorterDuff.Mode.SrcIn);
                        }
                        else
                        {
                            if (ColorUtils.CalculateLuminance(color) >= 0.65)
                            {
                                iv.SetColorFilter(Color.Black, PorterDuff.Mode.SrcIn);
                            }
                            else
                            {
                                iv.SetColorFilter(Color.Black, PorterDuff.Mode.SrcIn);
                            }
                        }
                    }

                    cpv.SetColor(color);
                }

                // update color:
                _color = Color.Argb(alpha, Color.GetRedComponent(_color), Color.GetGreenComponent(_color),
                    Color.GetBlueComponent(_color));
            }, v => { }, v => { }));
        }

        private Color ShadeColor(Color color, double percent)
        {
            var hex = $"#{(0xFFFFFF & color):6x}";
            var f = Long.ParseLong(hex.Substring(1), 16);
            double t = percent < 0 ? 0 : 255;
            var p = percent < 0 ? percent * -1 : percent;
            var r = f >> 16;
            var g = f >> 8 & 0x00FF;
            var b = f & 0x0000FF;
            var alpha = Color.GetAlphaComponent(color);
            var red = (int) (Math.Round((t - r) * p) + r);
            var green = (int) (Math.Round((t - g) * p) + g);
            var blue = (int) (Math.Round((t - b) * p) + b);
            return Color.Argb(alpha, red, green, blue);
        }

        private Color[] UnshiftIfNotExists(Color[] array, Color value)
        {
            var present = false;
            foreach (var i in array)
            {
                if (i == value)
                {
                    present = true;
                    break;
                }
            }

            if (!present)
            {
                var newArray = new Color[array.Length + 1];
                newArray[0] = value;
                Array.Copy(array, newArray, newArray.Length - 1);
                return newArray;
            }

            return array;
        }

        #endregion

// endregion

// region Builder


        // endregion
    }
}