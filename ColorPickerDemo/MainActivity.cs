// Copyright 2018 Arcana Studio
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// 
// Based on the work of Jared Rummler : https://github.com/jaredrummler/ColorPicker

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Widget;
using Android.OS;
using Android.Support.V7.App;
using Android.Views;
using Net.ArcanaStudio.ColorPicker;
using Java.Lang;

namespace Net.ArcanaStudio.ColorPickerDemo
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity, IColorPickerDialogListener
    {
        #region  Static Fields and Constants

        // Give your color picker dialog unique IDs if you have multiple dialogs.
        private const int DIALOG_ID = 0;

        #endregion

        #region Interfaces

        public void OnColorSelected(int dialogId, Color color)
        {
            switch (dialogId)
            {
                case DIALOG_ID:
                    // We got result from the dialog that is shown when clicking on the icon in the action bar.
                    Toast.MakeText(this, "Selected Color: #" + Integer.ToHexString(color), ToastLength.Short).Show();
                    break;
            }
        }

        public void OnDialogDismissed(int dialogId)
        {
        }

        #endregion

        #region Members

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            if (savedInstanceState == null)
            {
                FragmentManager.BeginTransaction().Add(Android.Resource.Id.Content, new DemoFragment()).Commit();
            }
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.main, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.menu_color_picker_dialog:
                    ColorPickerDialog.NewBuilder()
                        .SetDialogType(ColorPickerDialog.DialogType.Custom)
                        .SetAllowPresets(false)
                        .SetDialogId(DIALOG_ID)
                        .SetColor(Color.Black)
                        .SetShowAlphaSlider(true)
                        .Show(this);
                    return true;
                case Resource.Id.menu_github:
                    try
                    {
                        StartActivity(new Intent(Intent.ActionView,
                            Android.Net.Uri.Parse("https://github.com/jaredrummler/ColorPicker")));
                    }
                    catch (ActivityNotFoundException ignored)
                    {
                    }

                    return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        #endregion
    }
}