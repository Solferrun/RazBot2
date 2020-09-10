using System;
using System.IO;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using AngleSharp.Text;

namespace RazBot
{
    public partial class PlaylistManager : Form
    {
        // INIT
        private string enteredPlaylist = "";
        public PlaylistImportForm importForm;

        public PlaylistManager()
        {
            InitializeComponent();
            this.TopMost = true;
            importForm = new PlaylistImportForm();
        }

        private bool InPlaylist()
        {
            return !String.IsNullOrEmpty(enteredPlaylist);
        }

        private void OnPlaylistBoxIndexSelected(object sender, EventArgs e)
        {
            if (!InPlaylist())
            {
                string playlistName = $"{listBoxPlaylist.SelectedItem}";
                btnLoad.Enabled = playlistName != "request";
                btnRemove.Enabled = true;
            }
        }

        // BUTTON FUNCTIONS
        private void OnAddButtonClick(object sender, EventArgs e)
        {
            if(!InPlaylist())
            {
                var playlistName = tbPlaylist.Text;
                if (!String.IsNullOrEmpty(playlistName))
                {
                    Music.Playlists.Add(playlistName, new Playlist(new List<SongData>(), true, playlistName));
                    listBoxPlaylist.DataSource = Music.Playlists.Keys.ToList<string>();
                    Music.SavePlaylists();
                    tbPlaylist.Clear();
                    if (!Directory.Exists($"{BotTools.BasePath}\\playlists\\{playlistName}"))
                    {
                        Directory.CreateDirectory($"{BotTools.BasePath}\\playlists\\{playlistName}");
                    }
                }
            }
            else if (InPlaylist() && !String.IsNullOrEmpty(tbPlaylist.Text))
            {
                try
                {
                    SongData song = new SongData(tbPlaylist.Text, "RazBot");
                    var playlist = Music.Playlists[enteredPlaylist];
                    playlist.AddSong(song);
                    listBoxPlaylist.DataSource = playlist.Songs.Select(s => s.Title).ToList<string>();
                    tbPlaylist.Clear();
                }
                catch (Exception)
                {
                    tbPlaylist.Clear();
                }
                btnRemove.Enabled = listBoxPlaylist.Items.Count > 0;
            }
        }

        private void OnRemoveButtonClick(object sender, EventArgs e)
        {
            if (listBoxPlaylist.SelectedItems.Count == 1)
            {
                var selected = $"{listBoxPlaylist.SelectedItem}";
                if (!InPlaylist() && 
                    selected != "default" && 
                    selected != "request")
                {
                    CustomDialog.Form.CallDialog("Delete that?\n\nFor real?", DialogType.YesNo, RemoveSelectedPlaylist);
                }
                else if (!InPlaylist() && 
                    (selected == "default" ||
                     selected == "request"))
                {
                    CustomDialog.Form.CallDialog("Clear it out?", DialogType.YesNo, ClearSelectedPlaylist);
                }
                else if (InPlaylist())
                {
                    CustomDialog.Form.CallDialog("Delete that?\n\nFor real?", DialogType.YesNo, RemoveSelectedSong);
                }
            }
        }

        private void ClearSelectedPlaylist()
        {
            if (listBoxPlaylist.SelectedItems.Count == 1)
            {
                string selected = listBoxPlaylist.SelectedItem.ToString();
                DirectoryInfo dir = new DirectoryInfo($"{BotTools.BasePath}\\playlists\\{selected}");
                FileInfo[] files = dir.GetFiles();
                foreach (FileInfo file in files)
                {
                    try
                    {
                        file.Attributes = FileAttributes.Normal;
                        File.Delete(file.FullName);
                    }
                    catch { }
                }
                Music.Playlists[selected].Songs.Clear();
            }
        }

        private void RemoveSelectedPlaylist()
        {
            if (listBoxPlaylist.SelectedItems.Count == 1)
            {
                string selected = listBoxPlaylist.SelectedItem.ToString();
                string dirPath = $"{BotTools.BasePath}\\playlists\\{selected}";

                // Load default if this playlist is loaded
                if (Music.LoadedPlaylist == selected)
                {
                    Music.LoadedPlaylist = "default";
                    BotTools.Settings["loaded_playlist"] = Music.LoadedPlaylist;
                    BotTools.SaveSettings();
                }

                // Remove from list
                Music.Playlists.Remove(selected);
                listBoxPlaylist.DataSource = Music.Playlists.Keys.ToList<string>();
                Music.SavePlaylists();
                tbPlaylist.Clear();

                // Delete all files and sub-directories in playlist directory
                if (Directory.Exists(dirPath))
                {
                    DirectoryInfo di = new DirectoryInfo(dirPath);
                    foreach (FileInfo file in di.GetFiles())
                    {
                        file.Delete();
                    }
                    foreach (DirectoryInfo directory in di.GetDirectories())
                    {
                        directory.Delete();
                    }
                    di.Delete();
                }
            }
        }

        private void RemoveSelectedSong()
        {
            if (listBoxPlaylist.SelectedItems.Count == 1)
            {
                var index = listBoxPlaylist.SelectedIndex;
                var playlist = Music.Playlists[enteredPlaylist];
                var songID = playlist.Songs[index].ID;
                var filePath = $"{BotTools.BasePath}\\playlists\\{enteredPlaylist}\\{songID}.mp4";

                // Remove from list
                playlist.RemoveSong(index);
                Music.SavePlaylists();

                // Delete mp4 file
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                // Refresh listbox
                listBoxPlaylist.DataSource = playlist.Songs.Select(s => s.Title).ToList<string>();
                btnRemove.Enabled = listBoxPlaylist.Items.Count > 0;

                // Load default playlist if last song in loaded playlist
                if (!Music.Playlists[enteredPlaylist].Songs.Any() && Music.LoadedPlaylist == enteredPlaylist)
                {
                    Music.LoadedPlaylist = "default";
                    BotTools.Settings["loaded_playlist"] = Music.LoadedPlaylist;
                    Music.QueuedSong = Music.DownloadedSong = Music.LoadedSong = null;
                    BotTools.SaveSettings();
                }
            }
        }

        private void OnLoadButtonClick(object sender, EventArgs e)
        {
            if (listBoxPlaylist.SelectedItems.Count == 1)
            {
                if (!InPlaylist())
                {
                    var selected = $"{listBoxPlaylist.SelectedItem}";
                    if (Music.Playlists[selected].Songs.Count > 0)
                    {
                        Music.LoadedPlaylist = selected;
                        Music.Playlists[BotTools.Settings["loaded_playlist"]].CycleBack();
                        BotTools.Settings["loaded_playlist"] = Music.LoadedPlaylist;
                        if (Music.DownloadedSong != null && !Music.DownloadedSong.IsRequest)
                        {
                            Music.DownloadedSong = null;
                        }
                        BotTools.SaveSettings();
                        this.Hide();
                    }
                    else
                    {
                        CustomDialog.Form.CallDialog("Uh, it's empty\n\nI don't do empty", DialogType.OK);
                    }
                }
                else
                {
                    var selected = $"{listBoxPlaylist.SelectedItem}";
                    var playlist = Music.Playlists[enteredPlaylist];
                    playlist.Shuffle();
                    if (Music.LoadedPlaylist == selected && Music.DownloadedSong != null && !Music.DownloadedSong.IsRequest)
                    {
                        Music.DownloadedSong = null;
                    }
                    Music.SavePlaylists();
                    listBoxPlaylist.DataSource = playlist.Songs.Select(s => s.Title).ToList<string>();
                }
            }
        }

        // LISTBOX FUNCTIONS
        private void OnPlaylistBoxClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                if (InPlaylist())
                {
                    listBoxPlaylist.DataSource = Music.Playlists.Keys.ToList<string>();
                    enteredPlaylist = "";
                    btnLoad.Text = "LOAD PLAYLIST";
                    btnLoad.Enabled = false;
                    btnRemove.Enabled = false;
                }
                else
                {
                    this.Hide();
                }
            }
        }

        private void OnPlaylistBoxDoubleClick(object sender, EventArgs e)
        {
            if (!InPlaylist())
            {
                var selected = $"{listBoxPlaylist.SelectedItem}";
                var playlist = Music.Playlists[selected];
                if (playlist != null)
                {
                    listBoxPlaylist.DataSource = playlist.Songs.Select(s => s.Title).ToList<string>();
                    enteredPlaylist = selected;
                    btnLoad.Text = "SHUFFLE";
                    btnRemove.Enabled = listBoxPlaylist.Items.Count > 0;
                }
            }
            else
            {
                var selected = $"{listBoxPlaylist.SelectedItem}";
                var playlist = Music.GetPlaylist(enteredPlaylist);
                var selectedSong = playlist.GetSong(selected);
                if (selectedSong != null)
                {
                    Music.FillSongRequest(selectedSong.URL, songRequestor: "MaericTV", bypassVet: true);
                    BotTools.LogLine($"Added to request Queue: {selectedSong.Title}");
                }
            }
        }

        // MISC FUNCTIONS
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        private void DragPlaylistManager(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
            else if (e.Button == MouseButtons.Right)
            {
                if(InPlaylist())
                {
                    listBoxPlaylist.DataSource = Music.Playlists.Keys.ToList<string>();
                    enteredPlaylist = "";
                    btnLoad.Text = "LOAD PLAYLIST";
                    btnLoad.Enabled = false;
                    btnRemove.Enabled = false;
                }
                else
                {
                    this.Hide();
                }
            }
        }

        private void OnPlaylistTextboxTextChanged(object sender, EventArgs e)
        {
            btnAdd.Enabled = !String.IsNullOrEmpty(tbPlaylist.Text);
        }

        private void OnPlaylistTextboxKeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = !(e.KeyChar.IsAlphanumericAscii() || "_ ".Contains(e.KeyChar));
        }

        private void OnPlaylistTextbotKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                OnAddButtonClick(sender, e);
            }
        }

        private void ReleaseFocus(object sender, EventArgs e)
        {
            tbPlaylist.Focus();
        }

        private void OnYouTubeImportButtonClick(object sender, EventArgs e)
        {
            if (InPlaylist())
            {
                listBoxPlaylist.DataSource = Music.Playlists.Keys.ToList<string>();
                enteredPlaylist = "";
                btnLoad.Text = "LOAD PLAYLIST";
                btnLoad.Enabled = false;
                btnRemove.Enabled = false;
            }
            importForm.Show();
            importForm.Location = this.Location;
            importForm.returnForm = this;
            tbPlaylist.Clear();
            this.Hide();
        }

        private void PlaylistManager_Enter(object sender, EventArgs e)
        {

        }

        private void PlaylistManager_VisibleChanged(object sender, EventArgs e)
        {
            if (Visible)
            {
                listBoxPlaylist.DataSource = Music.Playlists.Keys.ToList();
            }
        }

        private void OnDeleteFilesButtonClick(object sender, EventArgs e)
        {
            CustomDialog.Form.CallDialog("All those files\n\nGone forever...", DialogType.YesNo, DeleteAllMusicFiles);
        }

        private void DeleteAllMusicFiles()
        {
            NAudioTools.DeleteAllMp4s();
            NAudioTools.DeleteAllWavs();
        }
    }
}
