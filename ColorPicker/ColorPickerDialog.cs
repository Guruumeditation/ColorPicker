// Copyright 2018 Arcana Studio
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// 
// Based on the work of Jared Rummler : https://github.com/jaredrummler/ColorPicker

using System;
using System.Globalization;
using System.Linq;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Support.V4.Graphics;
using Android.Text;
using Android.Util;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using Java.Lang;
using Java.Util;
using Exception = System.Exception;
using Math = Java.Lang.Math;

namespace Net.ArcanaStudio.ColorPicker
{
    public class ColorPickerDialog : DialogFragment, View.IOnTouchListener, IOnColorChangedListener, ITextWatcher
    {
        public enum DialogType
        {
            Custom,
            Preset


            // endregion
        }

        #region  Static Fields and Constants

        private static Color[] _materialColors;

        internal static int ALPHA_THRESHOLD = 165;
        private const string ARG_ALLOW_CUSTOM = "allowCustom";
        private const string ARG_ALLOW_PRESETS = "allowPresets";
        private const string ARG_ALPHA = "alpha";
        private const string ARG_COLOR = "color";
        private const string ARG_COLOR_SHAPE = "colorShape";
        private const string ARG_CUSTOM_BUTTON_TEXT = "customButtonText";
        private const string ARG_DIALOG_TITLE = "dialogTitle";
        private const string ARG_ID = "id";
        private const string ARG_PRESETS = "presets";
        private const string ARG_PRESETS_BUTTON_TEXT = "presetsButtonText";
        private const string ARG_SELECTED_BUTTON_TEXT = "selectedButtonText";
        private const string ARG_SHOW_COLOR_SHADES = "showColorShades";
        private const string ARG_TYPE = "dialogType";

        private static readonly uint[] materialcolors =
        {
            0xFFF44336, // RED 500
            0xFFE91E63, // PINK 500
            0xFFFF2C93, // LIGHT PINK 500
            0xFF9C27B0, // PURPLE 500
            0xFF673AB7, // DEEP PURPLE 500
            0xFF3F51B5, // INDIGO 500
            0xFF2196F3, // BLUE 500
            0xFF03A9F4, // LIGHT BLUE 500
            0xFF00BCD4, // CYAN 500
            0xFF009688, // TEAL 500
            0xFF4CAF50, // GREEN 500
            0xFF8BC34A, // LIGHT GREEN 500
            0xFFCDDC39, // LIME 500
            0xFFFFEB3B, // YELLOW 500
            0xFFFFC107, // AMBER 500
            0xFFFF9800, // ORANGE 500
            0xFF795548, // BROWN 500
            0xFF607D8B, // BLUE GREY 500
            0xFF9E9E9E, // GREY 500
        };

        public static Color[] MATERIAL_COLORS =
            _materialColors ?? (_materialColors = materialcolors.Select(d => new Color((int) d)).ToArray());


        /**
         * Material design colors used as the default color presets
         */

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
        private DialogType _dialogType;
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

                colorPanelView.Post(() => colorPanelView.SetColor(colorShade));

                colorPanelView.SetOnClickListener(new OnClickListener((v) =>
                {
                    if ((v.Tag is BoolWrapper bw) && bw.Data)
                    {
                        _colorPickerDialogListener.OnColorSelected(_dialogId, _color);
                        Dismiss();
                    }

                    _color = colorPanelView.GetColor();
                    _adapter.SelectNone();
                    for (var i = 0; i < _shadesLayout.ChildCount; i++)
                    {
                        var layout = (FrameLayout) _shadesLayout.GetChildAt(i);
                        var cpv = layout.FindViewById<ColorPanelView>(Resource.Id.cpv_color_panel_view);
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

                colorPanelView.SetOnLongClickListener(new OnLongClickListener((v) =>
                {
                    colorPanelView.ShowHint();
                    return true;
                }));
            }
        }

// region Custom Picker

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
                    Activity.ObtainStyledAttributes(value.Data,
                        new[] {Android.Resource.Attribute.TextColorPrimary});
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
                    _colorPickerDialogListener.OnColorSelected(_dialogId, _color);
                    Dismiss();
                }
            }));

            contentView.SetOnTouchListener(this);
            _colorPicker.SetOnColorChangedListener(this);
            _hexEditText.AddTextChangedListener(this);

            _hexEditText.OnFocusChangeListener = new OnFocusChangeListener((v, hasfocus) =>
            {
                if (hasfocus)
                {
                    var imm = (InputMethodManager) Activity.GetSystemService(Context.InputMethodService);
                    imm.ShowSoftInput(_hexEditText, ShowFlags.Implicit);
                }
            });


            return contentView;
        }

// -- endregion --

// region Presets Picker

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

            _adapter = new ColorPaletteAdapter(new OnColorSelectedListener(newcolor =>
            {
                if (_color == newcolor)
                {
                    _colorPickerDialogListener.OnColorSelected(_dialogId, _color);
                    Dismiss();
                    return;
                }

                _color = newcolor;
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
            _presets = Arguments.GetIntArray(ARG_PRESETS).Select(d => new Color(d)).ToArray();
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

        /**
         * Create a new Builder for creating a {@link ColorPickerDialog} instance
         *
         * @return The {@link Builder builder} to create the {@link ColorPickerDialog}.
         */
        public static Builder NewBuilder()
        {
            return Builder.Instance;
        }

#pragma warning disable 672
        public override void OnAttach(Activity activity)
#pragma warning restore 672
        {
#pragma warning disable 618
            base.OnAttach(activity);
#pragma warning restore 618
            if (_colorPickerDialogListener == null && activity is IColorPickerDialogListener listener)
            {
                _colorPickerDialogListener = listener;
            }
        }

        public override void OnAttach(Context context)
        {
            base.OnAttach(context);

            if (_colorPickerDialogListener == null && context is IColorPickerDialogListener activity)
            {
                _colorPickerDialogListener = activity;
            }
        }

        public override Dialog OnCreateDialog(Bundle savedInstanceState)
        {
            _dialogId = Arguments.GetInt(ARG_ID);
            _showAlphaSlider = Arguments.GetBoolean(ARG_ALPHA);
            _showColorShades = Arguments.GetBoolean(ARG_SHOW_COLOR_SHADES);
            _colorShape = (ColorShape) Arguments.GetInt(ARG_COLOR_SHAPE);
            if (savedInstanceState == null)
            {
                _color = new Color(Arguments.GetInt(ARG_COLOR));
                _dialogType = (DialogType) Arguments.GetInt(ARG_TYPE);
            }
            else
            {
                _color = new Color(savedInstanceState.GetInt(ARG_COLOR));
                _dialogType = (DialogType) savedInstanceState.GetInt(ARG_TYPE);
            }

            _rootView = new FrameLayout(Activity);
            if (_dialogType == DialogType.Custom)
            {
                _rootView.AddView(CreatePickerView());
            }
            else if (_dialogType == DialogType.Preset)
            {
                _rootView.AddView(CreatePresetsView());
            }

            var selectedButtonStringRes = Arguments.GetInt(ARG_SELECTED_BUTTON_TEXT);
            if (selectedButtonStringRes == 0)
            {
                selectedButtonStringRes = Resource.String.cpv_select;
            }

            var builder = new AlertDialog.Builder(Activity)
                .SetView(_rootView)
                .SetPositiveButton(selectedButtonStringRes,
                    new DialogInterfaceOnClickListener((d, i) =>
                        _colorPickerDialogListener.OnColorSelected(_dialogId, _color)));

            var dialogTitleStringRes = Arguments.GetInt(ARG_DIALOG_TITLE);
            if (dialogTitleStringRes != 0)
            {
                builder.SetTitle(dialogTitleStringRes);
            }

            _presetsButtonStringRes = Arguments.GetInt(ARG_PRESETS_BUTTON_TEXT);
            _customButtonStringRes = Arguments.GetInt(ARG_CUSTOM_BUTTON_TEXT);

            int neutralButtonStringRes;
            if (_dialogType == DialogType.Custom && Arguments.GetBoolean(ARG_ALLOW_PRESETS))
            {
                neutralButtonStringRes =
                    (_presetsButtonStringRes != 0 ? _presetsButtonStringRes : Resource.String.cpv_presets);
            }
            else if (_dialogType == DialogType.Preset && Arguments.GetBoolean(ARG_ALLOW_CUSTOM))
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

        public override void OnDismiss(IDialogInterface dialog)
        {
            base.OnDismiss(dialog);
            _colorPickerDialogListener.OnDialogDismissed(_dialogId);
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
                    case DialogType.Custom:
                        _dialogType = DialogType.Preset;
                        ((Button) v).SetText(_customButtonStringRes != 0
                            ? _customButtonStringRes
                            : Resource.String.cpv_custom);
                        _rootView.AddView(CreatePresetsView());
                        break;
                    case DialogType.Preset:
                        _dialogType = DialogType.Custom;
                        ((Button) v).SetText(_presetsButtonStringRes != 0
                            ? _presetsButtonStringRes
                            : Resource.String.cpv_presets);
                        _rootView.AddView(CreatePickerView());
                        break;
                }
            }));
        }

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
            foreach (int i in array)
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

/**
 * Set the callback
 *
 * @param colorPickerDialogListener
 *     The callback invoked when a color is selected or the dialog is dismissed.
 */
        public void SetColorPickerDialogListener(IColorPickerDialogListener colorPickerDialogListener)
        {
            _colorPickerDialogListener = colorPickerDialogListener;
        }

        private void SetHex(Color color)
        {
            _hexEditText.Text = _showAlphaSlider ? $"{(color.ToArgb()):x8}" : $"{(color.ToArgb()):x6}";
        }

        private void SetupTransparency()
        {
            var progress = 255 - Color.GetAlphaComponent(_color);
            _transparencySeekBar.Max = 255;
            _transparencySeekBar.Progress = progress;
            var percentage = (int) ((double) progress * 100 / 255);
            var englishlocale = CultureInfo.CreateSpecificCulture(Locale.English.Language);
            _transparencyPercText.Text = string.Format(new CultureInfo(Locale.English.Language), "{0:d}%", percentage);
            _transparencySeekBar.SetOnSeekBarChangeListener(new OnSeekBarChangeListener((seekbar, prog, boolean) =>
            {
                var thepercentage = (int) ((double) prog * 100 / 255);
                _transparencyPercText.Text =
                    string.Format(new CultureInfo(Locale.English.Language), "{0:d}%", thepercentage);
                var alpha = 255 - prog;
                // update items in GridView:
                for (var i = 0; i < _adapter.Colors.Length; i++)
                {
                    int itemcolor = _adapter.Colors[i];
                    _adapter.Colors[i] = Color.Argb(alpha, Color.GetRedComponent(itemcolor),
                        Color.GetGreenComponent(itemcolor), Color.GetBlueComponent(itemcolor));
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

                    var thecolor = cpv.GetColor();
                    thecolor = Color.Argb(alpha, Color.GetRedComponent(thecolor), Color.GetGreenComponent(thecolor),
                        Color.GetBlueComponent(thecolor));
                    if (alpha <= ALPHA_THRESHOLD)
                    {
                        cpv.SetBorderColor(new Color(thecolor.R, thecolor.G, thecolor.B, (byte) 255));
                    }
                    else
                    {
                        cpv.SetBorderColor(((ColorWrapper) layout.Tag).Data);
                    }

                    if (cpv.Tag != null && (bool) cpv.Tag)
                    {
                        // The alpha changed on the selected shaded color. Update the checkmark color filter.
                        if (alpha <= ALPHA_THRESHOLD)
                        {
                            iv.SetColorFilter(Color.Black, PorterDuff.Mode.SrcIn);
                        }
                        else
                        {
                            if (ColorUtils.CalculateLuminance(thecolor) >= 0.65)
                            {
                                iv.SetColorFilter(Color.Black, PorterDuff.Mode.SrcIn);
                            }
                            else
                            {
                                iv.SetColorFilter(Color.White, PorterDuff.Mode.SrcIn);
                            }
                        }
                    }

                    cpv.SetColor(thecolor);
                }

                // update color:
                var red = Color.GetRedComponent(_color);
                var green = Color.GetGreenComponent(_color);
                var blue = Color.GetBlueComponent(_color);
                _color = Color.Argb(alpha, red, green, blue);
            }, s => { }, s => { }));
        }


        private Color ShadeColor(Color color, double percent)
        {
            var hex = $"{0xFFFFFF & color.ToArgb():X8}";
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
            foreach (int i in array)
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

        #region Nested Types

// endregion

// region Builder

        public class Builder
        {
            #region  Static Fields and Constants

            private static Builder _builder;

            #endregion

            #region Fields

            private bool _allowCustom = true;
            private bool _allowPresets = true;
            private int _color = Color.Black;
            private ColorShape _colorShape = ColorShape.Circle;
            private int _customButtonText = Resource.String.cpv_custom;
            private int _dialogId;

            private int _dialogTitle = Resource.String.cpv_default_title;
            private DialogType _dialogType = DialogType.Preset;
            private Color[] _presets = MATERIAL_COLORS;
            private int _presetsButtonText = Resource.String.cpv_presets;
            private int _selectedButtonText = Resource.String.cpv_select;
            private bool _showAlphaSlider;
            private bool _showColorShades = true;

            #endregion

            #region Properties

            public static Builder Instance => _builder ?? (_builder = new Builder());

            #endregion

            #region Constructors

            private Builder()
            {
            }

            #endregion

            #region Members

            /**
             * Create the {@link ColorPickerDialog} instance.
             *
             * @return A new {@link ColorPickerDialog}.
             * @see #show(Activity)
             */
            public ColorPickerDialog Create()
            {
                var dialog = new ColorPickerDialog();
                var args = new Bundle();
                args.PutInt(ARG_ID, _dialogId);
                args.PutInt(ARG_TYPE, (int) _dialogType);
                args.PutInt(ARG_COLOR, _color);
                args.PutIntArray(ARG_PRESETS, _presets.Select(d => d.ToArgb()).ToArray());
                args.PutBoolean(ARG_ALPHA, _showAlphaSlider);
                args.PutBoolean(ARG_ALLOW_CUSTOM, _allowCustom);
                args.PutBoolean(ARG_ALLOW_PRESETS, _allowPresets);
                args.PutInt(ARG_DIALOG_TITLE, _dialogTitle);
                args.PutBoolean(ARG_SHOW_COLOR_SHADES, _showColorShades);
                args.PutInt(ARG_COLOR_SHAPE, (int) _colorShape);
                args.PutInt(ARG_PRESETS_BUTTON_TEXT, _presetsButtonText);
                args.PutInt(ARG_CUSTOM_BUTTON_TEXT, _customButtonText);
                args.PutInt(ARG_SELECTED_BUTTON_TEXT, _selectedButtonText);
                dialog.Arguments = args;
                return dialog;
            }

            /**
             * Show/Hide the neutral button to select a custom color.
             *
             * @param allowCustom
             *     {@code false} to disable showing the custom button.
             * @return This builder object for chaining method calls
             */
            public Builder SetAllowCustom(bool allowCustom)
            {
                _allowCustom = allowCustom;
                return this;
            }

            /**
             * Show/Hide a neutral button to select preset colors.
             *
             * @param allowPresets
             *     {@code false} to disable showing the presets button.
             * @return This builder object for chaining method calls
             */
            public Builder SetAllowPresets(bool allowPresets)
            {
                _allowPresets = allowPresets;
                return this;
            }

            /**
             * Set the original color
             *
             * @param color
             *     The default color for the color picker
             * @return This builder object for chaining method calls
             */
            public Builder SetColor(Color color)
            {
                _color = color;
                return this;
            }

            /**
             * Set the shape of the color panel view.
             *
             * @param colorShape
             *     Either {@link ColorShape#CIRCLE} or {@link ColorShape#SQUARE}.
             * @return This builder object for chaining method calls
             */
            public Builder SetColorShape(ColorShape colorShape)
            {
                _colorShape = colorShape;
                return this;
            }

            /**
             * Set the custom button text string resource id
             *
             * @param customButtonText
             *     The string resource used for the custom button text
             * @return This builder object for chaining method calls
             */
            public Builder SetCustomButtonText(int customButtonText)
            {
                _customButtonText = customButtonText;
                return this;
            }

            /**
             * Set the dialog id used for callbacks
             *
             * @param dialogId
             *     The id that is sent back to the {@link ColorPickerDialogListener}.
             * @return This builder object for chaining method calls
             */
            public Builder SetDialogId(int dialogId)
            {
                _dialogId = dialogId;
                return this;
            }


            /**
             * Set the dialog title string resource id
             *
             * @param dialogTitle
             *     The string resource used for the dialog title
             * @return This builder object for chaining method calls
             */
            public Builder SetDialogTitle(int dialogTitle)
            {
                _dialogTitle = dialogTitle;
                return this;
            }

            /**
             * Set which dialog view to show.
             *
             * @param dialogType
             *     Either {@link ColorPickerDialog#TYPE_CUSTOM} or {@link ColorPickerDialog#TYPE_PRESETS}.
             * @return This builder object for chaining method calls
             */
            public Builder SetDialogType(DialogType dialogType)
            {
                _dialogType = dialogType;
                return this;
            }

            /**
             * Set the colors used for the presets
             *
             * @param presets
             *     An array of color ints.
             * @return This builder object for chaining method calls
             */
            public Builder SetPresets( Color[] presets)
            {
                _presets = presets ?? throw new ArgumentNullException(nameof(presets));
                return this;
            }

            /**
             * Set the presets button text string resource id
             *
             * @param presetsButtonText
             *     The string resource used for the presets button text
             * @return This builder object for chaining method calls
             */
            public Builder SetPresetsButtonText(int presetsButtonText)
            {
                _presetsButtonText = presetsButtonText;
                return this;
            }

            /**
             * Set the selected button text string resource id
             *
             * @param selectedButtonText
             *     The string resource used for the selected button text
             * @return This builder object for chaining method calls
             */
            public Builder SetSelectedButtonText(int selectedButtonText)
            {
                _selectedButtonText = selectedButtonText;
                return this;
            }

            /**
             * Show the alpha slider
             *
             * @param showAlphaSlider
             *     {@code true} to show the alpha slider. Currently only supported with the {@link ColorPickerView}.
             * @return This builder object for chaining method calls
             */
            public Builder SetShowAlphaSlider(bool showAlphaSlider)
            {
                _showAlphaSlider = showAlphaSlider;
                return this;
            }

            /**
             * Show/Hide the color shades in the presets picker
             *
             * @param showColorShades
             *     {@code false} to hide the color shades.
             * @return This builder object for chaining method calls
             */
            public Builder SetShowColorShades(bool showColorShades)
            {
                _showColorShades = showColorShades;
                return this;
            }

            /**
             * Create and show the {@link ColorPickerDialog} created with this builder.
             *
             * @param activity
             *     The current activity.
             */
            public void Show(Activity activity)
            {
                Create().Show(activity.FragmentManager, "color-picker-dialog");
            }

            #endregion
        }

        #endregion
    }
}