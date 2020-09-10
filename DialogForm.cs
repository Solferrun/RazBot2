using System;
using System.Drawing;
using System.Windows.Forms;

namespace RazBot
{
    public enum DialogType
    {
        OK,
        YesNo
    }

    public partial class DialogForm : Form
    {
        private Action PendingAction { get; set; } = null;
        public void CallDialog(string text, DialogType dialogType, Action action=null)
        {
            Show();
            lblText.Text = text.ToUpper();
            PendingAction = action;
            if (dialogType == DialogType.OK)
            {
                btnOK.Show();
                btnYes.Hide();
                btnNo.Hide();
                pnlBackground.BackgroundImage = Properties.Resources.redroom_ok;
                btnOK.Focus();
            }
            else if (dialogType == DialogType.YesNo)
            {
                btnOK.Hide();
                btnYes.Show();
                btnNo.Show();
                pnlBackground.BackgroundImage = Properties.Resources.redroom_yesno;
                btnYes.Focus();
            }
            Point targetLocation = MousePosition;
            targetLocation.Offset(-Width / 2, -Height / 2);
            Location = targetLocation;
        }

        public DialogForm()
        {
            InitializeComponent();
            this.TopMost = true;
        }

        private void OnOkButtonClick(object sender, EventArgs e)
        {
            Hide();
        }

        private void OnYesButtonClick(object sender, EventArgs e)
        {
            if (PendingAction != null)
            {
                PendingAction();
                PendingAction = null;
            }
            Hide();
        }

        private void OnNoButtonClick(object sender, EventArgs e)
        {
            Hide();
        }

        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        private void OnTextLabelMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
            if (e.Button == MouseButtons.Right)
            {
                Hide();
            }
        }

        private void DialogForm_Deactivate(object sender, EventArgs e)
        {
            PendingAction = null;
        }
    }
}
