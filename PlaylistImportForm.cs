using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using AngleSharp.Text;

namespace RazBot
{
    public partial class PlaylistImportForm : Form
    {
        public PlaylistManager returnForm;
        public PlaylistImportForm()
        {
            InitializeComponent();
            this.TopMost = true;
        }

        private void OnLoadButtonClick(object sender, EventArgs e)
        {
            var playlistUrl = tbUrl.Text.Replace(" ", "");
            var playlistName = tbName.Text;
            tbUrl.Clear();
            tbName.Clear();
            var playlistExists = Music.Playlists.Keys.Contains(playlistName);

            if (!Directory.Exists($"{BotTools.BasePath}\\playlists\\{playlistName}"))
            {
                Directory.CreateDirectory($"{BotTools.BasePath}\\playlists\\{playlistName}");
            }
            
            if (!playlistExists)
            {
                Music.Playlists[playlistName] = new Playlist();
            }
            YTE.FillPlaylistFromYoutube(playlistUrl, playlistName);
            returnForm.Show();
            returnForm.Location = this.Location;
            this.Hide();
        }

        private void OnTextBoxTextChanged(object sender, EventArgs e)
        {
            var nameEmpty = String.IsNullOrEmpty(tbName.Text);
            var urlEmpty = String.IsNullOrEmpty(tbUrl.Text);
            btnLoad.Enabled = !(nameEmpty || urlEmpty);

            var playlistExists = Music.Playlists.Keys.Contains(tbName.Text);
            btnLoad.Text = playlistExists ? "ADD" : "GET";
        }

        private void OnNameTextBoxKeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = !(e.KeyChar.IsAlphanumericAscii() || "_ ".Contains(e.KeyChar));
        }

        private void OnNameTextboxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && btnLoad.Enabled)
            {
                OnLoadButtonClick(sender, e);
            }
        }

        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        private void PlaylistImportForm_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
            else if (e.Button == MouseButtons.Right)
            {
                this.Hide();
            }
        }

        private void OnYouTubeButtonClick(object sender, EventArgs e)
        {
            returnForm.Show();
            returnForm.Location = this.Location;
            this.Hide();
        }

        private void ReleaseFocus(object sender, EventArgs e)
        {
            pnlMain.Focus();
        }
    }
}
