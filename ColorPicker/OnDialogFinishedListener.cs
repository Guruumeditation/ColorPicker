using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace Net.ArcanaStudio.ColorPicker
{
    public class OnDialogFinishedListener : IColorPickerDialogListener
    {
        public Task<Color?> DialogPoppedTask { get { return tcs.Task; } }
        private TaskCompletionSource<Color?> tcs;

        public OnDialogFinishedListener()
        {
            tcs = new TaskCompletionSource<Color?>();
        }

        /// <summary>
        /// Completes the task and sets it result
        /// </summary>
        /// <param name="result"></param>
        protected /* virtual */ void SetPopupResult(Color? result)
        {
            if (DialogPoppedTask.IsCompleted == false)
                tcs.SetResult(result);
        }

        public void OnColorSelected(int dialogId, Color color)
        {
            tcs.SetResult(color);
        }

        public void OnDialogDismissed(int dialogId)
        {
            if (!tcs.Task.IsCompleted)
                tcs.SetResult(null);
        }
    }
}