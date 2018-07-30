// Copyright 2018 Arcana Studio
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// 
// Based on the work of Jared Rummler : https://github.com/jaredrummler/ColorPicker

using Android.OS;
using Android.Preferences;
using Android.Util;
using Net.ArcanaStudio.ColorPicker;
using Java.Lang;

namespace Net.ArcanaStudio.ColorPickerDemo
{
    public class DemoFragment : PreferenceFragment
    {
        #region  Static Fields and Constants

        private static readonly string KEY_DEFAULT_COLOR = "default_color";
        private static readonly string TAG = "DemoFragment";

        #endregion

        #region Members

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            AddPreferencesFromResource(Resource.Xml.main);

            // Example showing how we can get the new color when it is changed:
            var colorPreference = (ColorPreference) FindPreference(KEY_DEFAULT_COLOR);
            colorPreference.OnPreferenceChangeListener = new OnPreferenceChangeListener();
        }

        #endregion

        #region Nested Types

        private class OnPreferenceChangeListener : Object, Preference.IOnPreferenceChangeListener
        {
            #region Interfaces

            public bool OnPreferenceChange(Preference preference, Object newValue)
            {
                if (KEY_DEFAULT_COLOR.Equals(preference.Key))
                {
                    var newDefaultColor = Integer.ToHexString((int) newValue);
                    Log.Debug(TAG, "New default color is: #" + newDefaultColor);
                }

                return true;
            }

            #endregion
        }

        #endregion
    }
}