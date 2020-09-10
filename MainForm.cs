using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace RazBot
{
    public partial class MainWindow : Form
    {
        // INIT
        string idleMessage = "Player Stopped: Requests Disabled";
        double downloadProgress = 0.0;
        public bool downloadingAudio = false;
        public bool normalizingAudio = false;
        bool loadingSong = false;
        string unloadPath = null;
        bool draggingTracker = false;
        bool playerStopped = true;
        bool playerVisualizerUp = false;
        bool currentSongAnnounced = false;

        Point playerLocationCache;
        int lastColor = 0;

        WitTrainerForm witTrainerForm;
        PlaylistManager playlistManagerForm;
        readonly List<Color> color = new List<Color>()
        {
            Color.FromArgb(159, 18, 236),
            Color.FromArgb(255, 29, 255),
            Color.FromArgb(159, 18, 236),
            Color.FromArgb(113, 12, 242),
            Color.FromArgb(87, 10, 247),
            Color.FromArgb(59, 6, 251),
            Color.FromArgb(36, 74, 245),
            Color.FromArgb(21, 153, 239),
            Color.FromArgb(8, 220, 233),
            Color.FromArgb(21, 153, 239),
            Color.FromArgb(44, 64, 237),
            Color.FromArgb(59, 6, 251),
            Color.FromArgb(87, 10, 247),
            Color.FromArgb(113, 12, 242)
        };

        public MainWindow()
        {
            Icon = Properties.Resources.raz_zone;

            InitializeComponent();
            InitializeSubForms();
			
            windowsMP.PlayStateChange += new AxWMPLib._WMPOCXEvents_PlayStateChangeEventHandler(OnPlayStateChange);
			LoadControlSettings();
            
            TwitchBot.Start();
            TwitchBot.Commands.MainWindow = this;
			
			DiscordBot.Start();
			
            GuildWars.GuildWarsInit();
			gw2DataFetcher.RunWorkerAsync();
        }
		
		private void LoadControlSettings()
		{
			windowsMP.settings.volume = (int)BotTools.Settings["volume"];
            trackVolume.Value = windowsMP.settings.volume;
            btnVolume.ForeColor = Color.FromArgb(255, 35, 255);
            playerLocationCache = pnlPlayerPanel.Location;
			tbChannel.Text = BotTools.Settings["twitch_channel"];
            cbSongAnnounce.Checked = BotTools.Settings["announce_songs"];
            cbQuestions.Checked = BotTools.Settings["handle_questions"];
            cbVetReqeusts.Checked = BotTools.Settings["vet_requests"];
            cbRandom.Checked = BotTools.Settings["random_play"];
            cbDebug.Checked = BotTools.Settings["debug_output"];
		}

        private void InitializeSubForms()
        {
            witTrainerForm = new WitTrainerForm();
            playlistManagerForm = new PlaylistManager();
            
        }
		
		private Color GetNextColor()
        {
            if (lastColor + 1 >= color.Count)
            {
                lastColor = 0;
            }
            else
            {
                lastColor += 1;
            }
            return color[lastColor];
        }
		
		// Top Buttons
		private void OnDiscordButtonClick(object sender, EventArgs e)
        {
            BotTools.LogLine("RazBot invoked \"discord\"");
            TwitchBot.SendMessage(TwitchBot.Commands.CustomMap["discord"]);
        }
		
		private void OnDLButtonClick(object sender, EventArgs e)
        {
            BotTools.LogLine("RazBot invoked \"dl\"");
            TwitchBot.SendMessage(TwitchBot.Commands.CustomMap["dl"]);
        }
		
		private void OnRazButtonClick(object sender, EventArgs e)
        {
            BotTools.LogLine("RazBot invoked \"pets\"");
            TwitchBot.SendMessage(TwitchBot.Commands.Map["pets"](new TwitchMessage()).FirstOrDefault());
        }

        private void OnLogButtonClick(object sender, EventArgs e)
        {
            if (BotTools.SessionLogFile != null)
            {
                System.Diagnostics.Process.Start(BotTools.SessionLogFile);
            }
        }

        private void OnFilesButtonClick(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
            {
                FileName = BotTools.BasePath,
                UseShellExecute = true,
                Verb = "open"
            });
        }
		
		private void OnExitButtonClick(object sender, EventArgs e) => this.Close();
		
		// Log Window / Chatbox
        private void OnSendButtonClick(object sender, EventArgs e)
        {
            // Send messagebox content as though from MaericTV@Twitch
            if (!string.IsNullOrEmpty(rtbChat.Text))
            {
                var message = new TwitchMessage("MaericTV", rtbChat.Text);
                if (message.BotResponse != null && message.BotResponse.Length > 0)
                {
                    foreach (string line in message.BotResponse)
                    {
                        TwitchBot.SendMessage(line);
                    }
                }
                else if (message.WitResponse != null && message.WitResponse.Length > 0)
                {
                    foreach (string line in message.WitResponse)
                    {
                        TwitchBot.SendMessage(line);
                    }
                }
                rtbChat.Text = "";
            }
        }

        private void OnChatBoxEnterKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                OnSendButtonClick(sender, e);
            }
        }
		
		public System.Diagnostics.Process p = new System.Diagnostics.Process();
        private void OnChatFeedLinkClick(object sender, LinkClickedEventArgs e)
        {
            p = System.Diagnostics.Process.Start(e.LinkText);
        }
		
		private void OnChatFeedTextChange(object sender, EventArgs e) => rtbChatFeed.ScrollToCaret();

        // Player Controls
        private void OnPlayButtonClick(object sender, EventArgs e)
        {
            if (Music.Playlists[Music.LoadedPlaylist].Songs.Count == 0)
            {
                CustomDialog.Form.CallDialog("Where are the tunes?\n\nGet me some tunes", DialogType.OK);
            }
            else if (playerStopped && !loadingSong)
            {
                playerStopped = false;
                trackPosition.Enabled = true;
                windowsMP.Ctlcontrols.play();
                btnPlay.BackColor = Color.FromArgb(255, 29, 255);
                btnStop.BackColor = Color.FromArgb(34, 15, 34);
                btnPause.BackColor = Color.FromArgb(34, 15, 34);
                btnPlay.ForeColor = Color.FromArgb(235, 235, 235);
                btnStop.ForeColor = Color.FromArgb(255, 29, 255);
                btnPause.ForeColor = Color.FromArgb(255, 29, 255);
            }

            if (windowsMP.playState == WMPLib.WMPPlayState.wmppsPaused)
            {
                windowsMP.Ctlcontrols.play();
                btnPlay.BackColor = Color.FromArgb(255, 29, 255);
                btnStop.BackColor = Color.FromArgb(34, 15, 34);
                btnPause.BackColor = Color.FromArgb(34, 15, 34);
                btnPlay.ForeColor = Color.FromArgb(235, 235, 235);
                btnStop.ForeColor = Color.FromArgb(255, 29, 255);
                btnPause.ForeColor = Color.FromArgb(255, 29, 255);
            }
            else if (windowsMP.playState != WMPLib.WMPPlayState.wmppsPlaying)
            {
                cbMute.Checked = false;
            }
        }

        private void OnPauseButtonClick(object sender, EventArgs e)
        {
            idleMessage = "Player Paused: Request Playing Momentarily Disabled";
            if (!playerStopped && !loadingSong)
            {
                windowsMP.Ctlcontrols.pause();
                btnPause.BackColor = Color.FromArgb(255, 29, 255);
                btnStop.BackColor = Color.FromArgb(34, 15, 34);
                btnPlay.BackColor = Color.FromArgb(34, 15, 34);
                btnPause.ForeColor = Color.FromArgb(235, 235, 235);
                btnStop.ForeColor = Color.FromArgb(255, 29, 255);
                btnPlay.ForeColor = Color.FromArgb(255, 29, 255);
            }
        }

        private void OnStopButtonClick(object sender, EventArgs e)
        {
            playerLocationCache = pnlPlayerPanel.Location;
            tmrPlayerVisualizerOut.Start();

            if (!loadingSong)
            {
                btnAddToDefault.Enabled = false;
                idleMessage = "Player Stopped: Request Playing Disabled";
                windowsMP.Ctlcontrols.stop();
                playerStopped = true;
                trackPosition.Value = 50;
                trackPosition.TrackerColor = Color.FromArgb(17, 7, 17); // Alt: 24, 0, 36
                trackPosition.TrackLineColor = Color.FromArgb(17, 7, 17); // Alt: 200, 225, 255
                trackPosition.Enabled = false;
                btnStop.BackColor = Color.FromArgb(255, 29, 255);
                btnPause.BackColor = Color.FromArgb(34, 15, 34);
                btnPlay.BackColor = Color.FromArgb(34, 15, 34);
                btnStop.ForeColor = Color.FromArgb(235, 235, 235);
                btnPause.ForeColor = Color.FromArgb(255, 29, 255);
                btnPlay.ForeColor = Color.FromArgb(255, 29, 255);
                cbMute.Checked = false;
            }
        }

        private void OnSkipButtonClick(object sender, EventArgs e)
        {
            idleMessage = "Loading next song...";
            SkipSong();
        }
		
		public void SkipSong()
        {
            if (windowsMP.currentMedia != null && !playerStopped && !loadingSong)
            {
                trackPosition.Value = 100;
                windowsMP.Ctlcontrols.currentPosition = windowsMP.currentMedia.duration;
                windowsMP.Ctlcontrols.play();
            }
        }
		
		private void OnVetRequestsCheckboxCheckedChanged(object sender, EventArgs e)
        {
            BotTools.Settings["vet_requests"] = cbVetReqeusts.Checked;
            BotTools.SaveSettings();
            cbVetReqeusts.BackColor = cbVetReqeusts.Checked ? Color.FromArgb(30, 225, 255) : Color.FromArgb(34, 15, 34);
            cbVetReqeusts.ForeColor = cbVetReqeusts.Checked ? Color.FromArgb(14, 0, 20) : Color.FromArgb(255, 35, 255);
            cbVetReqeusts.BackgroundImage.Dispose();
            cbVetReqeusts.BackgroundImage = cbVetReqeusts.Checked ? Properties.Resources.vet_requests_black_noborder : Properties.Resources.vet_requests_noborder;
        }
		
        private void OnDiscordAnnounceCheckboxCheckedChanged(object sender, EventArgs e)
        {
            
            BotTools.Settings["announce_songs"] = cbSongAnnounce.Checked;
            BotTools.SaveSettings();
            cbSongAnnounce.BackColor = cbSongAnnounce.Checked ? Color.FromArgb(30, 225, 255) : Color.FromArgb(34, 15, 34);
            cbSongAnnounce.ForeColor = cbSongAnnounce.Checked ? Color.FromArgb(14, 0, 20) : Color.FromArgb(255, 35, 255);
            cbSongAnnounce.BackgroundImage.Dispose();
            cbSongAnnounce.BackgroundImage = cbSongAnnounce.Checked ? Properties.Resources.discord_black : Properties.Resources.discord;
        }

        // Tracking
        private void OnTrackingTrackbarScroll(object sender, EventArgs e)
        {
            if (windowsMP.currentMedia != null)
            {
                double newPosition = (double)trackPosition.Value / 100 * windowsMP.currentMedia.duration;
                int mins = (int)newPosition / 60;
                int secs = (int)newPosition % 60;
                rtbPlayerFeed.Text = $"{mins}m {secs}s";
            }
            else if (draggingTracker == false)
            {
                trackPosition.Value = 0;
            }
        }

        private void OnTrackingTrackbarMouseUp(object sender, MouseEventArgs e)
        {
            draggingTracker = false;
            double newPosition = (double)trackPosition.Value / 100 * windowsMP.currentMedia.duration;
            windowsMP.Ctlcontrols.currentPosition = newPosition;
        }
		
		private delegate void SafeSetTrackbarValueDelegate(int value);
        private void SetTrackbarValue(int value)
        {
            if (trackVolume.InvokeRequired)
            {
                var d = new SafeSetTrackbarValueDelegate(SetTrackbarValue);
                trackVolume.Invoke(d, new object[] { value });
            }
            else
            {
                trackVolume.Value = value;
            }
        }
		
		private void OnTrackingTrackbarMouseDown(object sender, MouseEventArgs e) => draggingTracker = true;
		
		// Volume
        private void OnVolumeTrackbarValueChange(object sender, decimal value)
        {
            windowsMP.settings.volume = trackVolume.Value;
            btnVolume.ForeColor = Color.White;
            btnVolume.Text = $"{windowsMP.settings.volume}";
            if (cbMute.Checked) cbMute.Checked = false;
        }

        private void OnVolumeTrackMouseDown(object sender, MouseEventArgs e)
        {
            btnVolume.ForeColor = Color.White;
            btnVolume.Text = $"{windowsMP.settings.volume}";
        }

        private void OnVolumeTrackbarMouseUp(object sender, MouseEventArgs e)
        {
            btnVolume.ForeColor = Color.FromArgb(255, 35, 255);
            btnVolume.Text = "VOL";
            BotTools.Settings["volume"] = windowsMP.settings.volume;
            BotTools.SaveSettings();
        }

        private void OnVolumeButtonMouseDown(object sender, EventArgs e)
        {
            btnVolume.ForeColor = Color.White;
            btnVolume.Text = $"{windowsMP.settings.volume}";

        }

        private void OnVolumeButtonMouseUp(object sender, MouseEventArgs e)
        {
            btnVolume.ForeColor = Color.FromArgb(255, 35, 255);
            btnVolume.Text = "VOL";
        }
		
		public int AdjustVolume(int adjustmentValue = 0)
        {
            SetTrackbarValue(Math.Min(100, Math.Max(0, windowsMP.settings.volume + adjustmentValue)));
            return windowsMP.settings.volume;
        }

        public int SetVolume(int value)
        {
            SetTrackbarValue(Math.Min(100, Math.Max(0, value)));
            return windowsMP.settings.volume;
        }
		
        private void OnMuteCheckboxCheckedChanged(object sender, EventArgs e)
        {
            windowsMP.settings.mute = cbMute.Checked;
            cbMute.BackColor = cbMute.Checked ? Color.FromArgb(30, 225, 255) : Color.FromArgb(34, 15, 34);
            cbMute.ForeColor = cbMute.Checked ? Color.FromArgb(14, 0, 20) : Color.FromArgb(255, 35, 255);
            cbMute.BackgroundImage.Dispose();
            cbMute.BackgroundImage = cbMute.Checked ? Properties.Resources.speaker_black : Properties.Resources.speaker;
        }
		
        // Playlists
        private void OnPlaylistCheckboxCheckedChanged(object sender, EventArgs e)
        {
            cbPlaylist.BackColor = cbPlaylist.Checked ? Color.FromArgb(30, 225, 255) : Color.FromArgb(34, 15, 34);
            cbPlaylist.ForeColor = cbPlaylist.Checked ? Color.FromArgb(14, 0, 20) : Color.FromArgb(255, 35, 255);
            cbPlaylist.BackgroundImage.Dispose();
            cbPlaylist.BackgroundImage = cbPlaylist.Checked ? Properties.Resources.playlists_black_noborder : Properties.Resources.playlists_noborder;
            if (cbPlaylist.Checked)
            {
                playlistManagerForm.Show();
                Point targetLocation = this.Location;
                targetLocation.Offset(cbPlaylist.Location.X + 2, cbPlaylist.Location.Y + cbPlaylist.Height);
                playlistManagerForm.Location = targetLocation;
                playlistManagerForm.Focus();
            }
            else
            {
                playlistManagerForm.Hide();
                playlistManagerForm.importForm.Hide();
            }
        }
		
		private void OnRemoveFromPlaylistButtonClick(object sender, EventArgs e)
        {
            if (Music.LoadedSong.Requestor == "RazBot")
            {
                CustomDialog.Form.CallDialog("Delete this tune?\n\nLike forever?", DialogType.YesNo, RemoveSongFromPlaylist);
            }
            else
            {
                CustomDialog.Form.CallDialog("That's a request!\n\nLet's skip it", DialogType.YesNo, SkipSong);
            }
        }

        private void RemoveSongFromPlaylist()
        {
            var loadedPlaylist = Music.Playlists[Music.LoadedPlaylist];
            if (loadedPlaylist.ContainsSong(Music.LoadedSong))
            {
                loadedPlaylist.RemoveSong(Music.LoadedSong);
            }
            if (!loadedPlaylist.Songs.Any())
            {
                BotTools.LogLine("Loaded playlist is empty, loading default");
                Music.LoadedPlaylist = "default";
                BotTools.Settings["loaded_playlist"] = Music.LoadedPlaylist;
                BotTools.SaveSettings();
            }
            SkipSong();
        }

        private void OnAddButtonClick(object sender, EventArgs e)
        {
            if (!Music.Playlists["default"].ContainsSongWithID(Music.LoadedSong.ID))
            {
                var playlist = Music.Playlists["default"];
                var songCopy = new SongData(Music.LoadedSong);
                try
                {
                    playlist.AddSong(songCopy);
                    BotTools.LogLine("Added current song to default playlist");
                    btnAddToDefault.Enabled = false;
                }
                catch (ArgumentException)
                {
                    BotTools.LogLine($"Couldn't add current song: Already in default playlist");
                }
                catch (FormatException)
                {
                    BotTools.LogLine($"Couldn't add current song: Bad url");
                }
            }
            else
            {
                BotTools.LogLine("Song already in default playlist!");
            }
        }

        private void OnNormalizeCheckboxCheckChange(object sender, EventArgs e)
        {
            cbNormalize.BackColor = cbNormalize.Checked ? Color.FromArgb(30, 225, 255) : Color.FromArgb(34, 15, 34);
            cbNormalize.ForeColor = cbNormalize.Checked ? Color.FromArgb(14, 0, 20) : Color.FromArgb(255, 35, 255);
            cbNormalize.BackgroundImage.Dispose();
            cbNormalize.BackgroundImage = cbNormalize.Checked ? Properties.Resources.normalize_black_noborder : Properties.Resources.normalize_noborder;
            if (cbNormalize.Checked)
            {
                bgwAudioNormalizer.RunWorkerAsync();
            }
            else
            {
                NAudioTools.DeleteAllWavs();
            }
        }
		
		private void OnRandomPlayCheckboxCheckedChanged(object sender, EventArgs e)
        {
            BotTools.Settings["random_play"] = cbRandom.Checked;
            cbRandom.BackColor = cbRandom.Checked ? Color.FromArgb(30, 225, 255) : Color.FromArgb(34, 15, 34);
            cbRandom.ForeColor = cbRandom.Checked ? Color.FromArgb(14, 0, 20) : Color.FromArgb(255, 35, 255);
            cbRandom.BackgroundImage.Dispose();
            cbRandom.BackgroundImage = cbRandom.Checked ? Properties.Resources.btn_random_black : Properties.Resources.btn_random;
            BotTools.SaveSettings();
        }
		
		// Player State
		private void OnPlayStateChange(object sender, AxWMPLib._WMPOCXEvents_PlayStateChangeEvent e)
        {
            var newState = (WMPLib.WMPPlayState)e.newState;
            if (newState == WMPLib.WMPPlayState.wmppsPlaying)
            {
                loadingSong = false;
                trackPosition.TrackerColor = Color.FromArgb(24, 0, 36);
                trackPosition.TrackLineColor = Color.FromArgb(50, 225, 255);

                if (BotTools.Settings["announce_songs"] && currentSongAnnounced == false && Music.LoadedSong != null)
                {
                    string playing = Music.LoadedSong.IsRequest ? $"Playing {Music.LoadedSong.Requestor}'s Request" : "Playing";
                    string message = $"**{playing}:** `{Music.LoadedSong.Title}`\n{Music.LoadedSong.URL}";
                    DiscordBot.PostToDJChannel(message);
                    currentSongAnnounced = true;
                }

                if (!playerVisualizerUp)
                {
                    pnlPlayerPanel.Width = 1;
                    pnlPlayerPanel.Height = 1;
                    Point newPos = pnlPlayerPanel.Location;
                    newPos.Offset(105, 105);
                    pnlPlayerPanel.Location = newPos;
                    tmrPlayerVisualizerIn.Start();
                }
            }
            if (newState == WMPLib.WMPPlayState.wmppsMediaEnded && Music.LoadedSong != null)
            {
                string playlistName = Music.LoadedSong.IsRequest ? "request" : Music.LoadedPlaylist;
                unloadPath = $"{BotTools.BasePath}\\playlists\\{playlistName}\\{Music.LoadedSong.ID}_Normalized.wav";
                Music.LoadedSong = null;
                currentSongAnnounced = false;
            }
        }
		
        private delegate void SafeSetControlsDelegate<T>(T value);
        private void SetPlayerControlsEnabled(bool state)
        {
            if (btnPlay.InvokeRequired)
            {
                var d = new SafeSetControlsDelegate<bool>(SetPlayerControlsEnabled);
                btnPlay.Invoke(d, new object[] { state });
            }
            else
            {
                cbNormalize.Enabled = state;
                btnPlay.Enabled = state;
                btnPause.Enabled = state;
                btnStop.Enabled = state;
                btnSkip.Enabled = state;
                btnRemoveFromPlaylist.Enabled = state;
                cbMute.Enabled = state;
                trackPosition.Enabled = state;
                btnSkip.Enabled = windowsMP.playState == WMPLib.WMPPlayState.wmppsPlaying;
                btnSkip.BackgroundImage = btnSkip.Enabled ? Properties.Resources.btn_skip_short : Properties.Resources.btn_skip_short_black;
                cbNormalize.Enabled = Music.DownloadedSong != null;
                cbNormalize.BackgroundImage.Dispose();
                cbNormalize.BackgroundImage = Music.DownloadedSong == null || cbNormalize.Checked ?
                    Properties.Resources.normalize_black_noborder :
                    Properties.Resources.normalize_noborder;
            }
            
        }
		
		// Twitch Channel Pane
        private void OnChannelTextboxKeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                OnJoinChannelButtonClick(sender, e);
            }
            if (!(char.IsLetterOrDigit(e.KeyChar) || e.KeyChar == '_' || e.KeyChar == (char)Keys.Back))
            {
                e.Handled = true;
            }
        }

        private void OnLeaveChannelButtonClick(object sender, EventArgs e) => TwitchBot.LeaveChannel();
        private void OnJoinChannelButtonClick(object sender, EventArgs e) => TwitchBot.JoinChannel(tbChannel.Text);
		
		// Wit Corner
        private void OnWitCheckboxCheckChanged(object sender, EventArgs e)
        {
            if (cbWit.Checked)
            {
                witTrainerForm.Show();
                Point targetLocation = this.Location;
                targetLocation.Offset(this.Width / 2 - 2, this.Height / 10 - 21);
                witTrainerForm.Location = targetLocation;
                witTrainerForm.Focus();
            }
            else
            {
                witTrainerForm.Hide();
            }
            cbWit.BackColor = cbWit.Checked ? Color.FromArgb(30, 225, 255) : Color.FromArgb(34, 15, 34);
            cbWit.ForeColor = cbWit.Checked ? Color.FromArgb(34, 15, 34) : Color.FromArgb(255, 35, 255);
            cbWit.BackgroundImage.Dispose();
            cbWit.BackgroundImage = cbWit.Checked ? Properties.Resources.wit_w_black : Properties.Resources.wit_w;
        }
		
		private void OnQuestionBoxCheckedChanged(object sender, EventArgs e)
        {
            cbQuestions.ForeColor = cbQuestions.Checked ? Color.FromArgb(14, 0, 20) : Color.FromArgb(255, 35, 255);
            cbQuestions.BackgroundImage.Dispose();
            cbQuestions.BackgroundImage = cbQuestions.Checked ? Properties.Resources.question_mark_black : Properties.Resources.question_mark;
            var state = cbQuestions.Checked ? "is" : "is not";
            toolTip1.SetToolTip(cbQuestions, $"WitAi {state} fielding questions.");
            BotTools.Settings["handle_questions"] = cbQuestions.Checked;
            BotTools.SaveSettings();
        }

        private void OnQuestionBoxMouseOver(object sender, EventArgs e)
        {
            var state = cbQuestions.Checked ? "is" : "is not";
            toolTip1.SetToolTip(cbQuestions, $"WitAi {state} fielding questions.");
        }

        private void OnDebugModeCheckboxCheckedChanged(object sender, EventArgs e)
        {
            BotTools.Settings["debug_output"] = cbDebug.Checked;
            cbDebug.BackColor = cbDebug.Checked ? Color.FromArgb(30, 255, 255) : Color.FromArgb(34, 15, 34);
            cbDebug.BackgroundImage = cbDebug.Checked ? Properties.Resources.silent_mode_black : Properties.Resources.silent_mode;
            cbDebug.ForeColor = cbDebug.Checked ? Color.FromArgb(14, 0, 20) : Color.FromArgb(255, 35, 255);
            BotTools.SaveSettings();
        }
		
		// Main Form Drag/Close
		public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        private void DragMainWindow(object sender, MouseEventArgs e)
        {
            ReleaseFocus(sender, e);
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }
		
        private void ReleaseFocus(object sender, EventArgs e) => pbLive.Focus();
        private void DragMainWindow(object sender, AxWMPLib._WMPOCXEvents_MouseDownEvent e)
        {
            if (e.nButton == 1)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void OnFormClose(object sender, FormClosedEventArgs e)
        {
            try
            {
                BotTools.DumpToTextFile("RazBot 2.0 is Offline: Request Playing Disabled", "datasources\\now_playing");
            }
            catch { }
            try
            {
                using (StreamWriter sw = File.AppendText(BotTools.SessionLogFile))
                {
                    sw.WriteLine($"\t--End of Session [{DateTime.Now:HH:mm:ss}]--\n");
                }
            }
            catch { }
            try
            {
                Music.ClearRequests();
            }
            catch { }
            try
            {
                NAudioTools.DeleteAllWavs();
            }
            catch { }
        }

        // Tooltips
        private void OnSongAnnounceCheckboxMouseHover(object sender, EventArgs e) => toolTip1.SetToolTip(cbSongAnnounce, "Report played songs to Discord.");
        private void OnRandomCheckboxMouseHover(object sender, EventArgs e) => toolTip1.SetToolTip(cbRandom, "Shuffle loaded playlist.");
        private void OnAddToDefaultButtonMouseHover(object sender, EventArgs e) => toolTip1.SetToolTip(btnAddToDefault, $"Add current song to \"default\" playlist.");
        private void OnRemoveFromPlaylistButtonMouseHover(object sender, EventArgs e) => toolTip1.SetToolTip(btnRemoveFromPlaylist, "Remove current song from containing playlist and skip.");
        private void OnNormalizeCheckboxMouseHover(object sender, EventArgs e) => toolTip1.SetToolTip(cbNormalize, "Increase audio file volume based on its peak.");
        private void OnVetReqeustsCheckboxMouseHover(object sender, EventArgs e) => toolTip1.SetToolTip(cbVetReqeusts, "Send song requests to Discord for moderator vetting.");
        private void OnDebugCheckboxMouseHover(object sender, EventArgs e) => toolTip1.SetToolTip(cbDebug, "Output bot responses to log window instead of chat.");
        private void OnWitBoxMouseOver(object sender, EventArgs e) => toolTip1.SetToolTip(cbWit, $"WitAi and custom command management.");

        // Timers
        private void OnTimer1HourTick(object sender, EventArgs e) => gw2DataFetcher.RunWorkerAsync();

        private void OnTimer2MinTick(object sender, EventArgs e)
        {
            var posts = Instagram.GetPosts();
            if (posts.Count > 0)
            {
                bool petsExist = File.Exists($"{BotTools.BasePath}\\resources\\raz_pets.json");
                var pastPets = petsExist ? BotTools.ReadFromJson<List<string>>("resources\\raz_pets") : new List<string>();

                try
                {
                    var newPets = posts.Where(p => !pastPets.Contains(p["permalink"]));
                    if (newPets.Count() > 0)
                    {
                        foreach (var post in newPets)
                        {
                            BotTools.LogLine($"Saw new Raz pet -> {post["permalink"]}");
                            DiscordBot.PostToPetsChannel(post);
                            pastPets.Add(post["permalink"]);

                            using (var webClient = new System.Net.WebClient())
                            {
                                webClient.DownloadFile(post["media_url"], $"{BotTools.BasePath}\\datasources\\raz.jpg");
                            }
                        }
                        BotTools.WriteToJson(pastPets, "resources\\raz_pets");
                    }
                }
                catch (Exception petsException)
                {
                    BotTools.LogToSessionLogFile(petsException.ToString());
                    BotTools.LogLine("There was a problem loading past Raz Pets! See the session log for details.");
                }
            }
        }

        private void OnTimer200msTick(object sender, EventArgs e)
        {
            // Populate chat feed box
            List<string> offloadBuffer = BotTools.ConsoleBuffer;
            BotTools.ClearConsoleBuffer();

            // Color lines
            foreach (string line in offloadBuffer)
            {
                int startIndex = rtbChatFeed.Text.Length;
                rtbChatFeed.AppendText(line + '\n');
                rtbChatFeed.Select(startIndex, line.Length);
                rtbChatFeed.SelectionColor = GetNextColor();
                if (rtbChatFeed.SelectedText.StartsWith("○"))
                {
                    rtbChatFeed.SelectionBackColor = GetNextColor();//Color.FromArgb(192, 21, 72);
                    rtbChatFeed.SelectionColor = Color.FromArgb(0, 0, 0);
                    rtbChatFeed.SelectionFont = new Font(rtbChatFeed.Font, FontStyle.Bold);
                }
                rtbChatFeed.DeselectAll();
            }

            // Untick SubMenu checkboxes
            cbWit.Checked = witTrainerForm.Visible;
            cbPlaylist.Checked = playlistManagerForm.Visible || playlistManagerForm.importForm.Visible;

            // Update Twitch connection window
            bool channelJoined = TwitchBot.Client.JoinedChannels.Count != 0;
            btnJoinChannel.Enabled = !channelJoined && !String.IsNullOrEmpty(tbChannel.Text);
            btnLeaveChannel.Enabled = channelJoined;
            tbChannel.Enabled = !channelJoined;
            if (!channelJoined && !pnlTwitchConnect.Visible)
            {
                try
                {
                    TwitchBot.JoinChannel(BotTools.Settings["twitch_channel"]);
                }
                catch { }
            }

            // Flash LIVE display if stream is live + hosting
            if (TwitchBot.StreamStatus == StreamState.OnlineHosting)
            {
                timerFlashLive.Enabled = true;
                timerLiveAlert.Enabled = true;
            }
            else
            {
                timerFlashLive.Enabled = false;
                timerLiveAlert.Enabled = false;

                pbLive.BackgroundImage.Dispose();
                pbLive.BackgroundImage = (int)TwitchBot.StreamStatus % 2 == 0 ? Properties.Resources.live_on : Properties.Resources.live_off;
            }

            // Music Player Work/Feed
            SetPlayerControlsEnabled(Music.LoadedSong != null);


            if (!bgwCycleAudioQueue.IsBusy)
            {
                bgwCycleAudioQueue.RunWorkerAsync();
            }

            pnlPlayerPanel.Visible = !playerStopped;

            if (windowsMP.currentMedia != null && windowsMP.playState == WMPLib.WMPPlayState.wmppsPlaying && draggingTracker == false)
            {
                // Display source video title
                if (Music.LoadedSong != null && Music.LoadedSong.Requestor == "RazBot")
                {
                    var playerFeed = $"Playing: {Music.LoadedSong.Title}";
                    if (rtbPlayerFeed.Text != playerFeed)
                    {
                        rtbPlayerFeed.Text = playerFeed;
                    }
                }
                else if (Music.DownloadedSong != null)
                {
                    var playerFeed = $"Playing {Music.LoadedSong.Requestor}'s Request: {Music.LoadedSong.Title}";
                    if (rtbPlayerFeed.Text != playerFeed)
                    {
                        rtbPlayerFeed.Text = playerFeed;
                        rtbPlayerFeed.Select(8, Music.LoadedSong.Requestor.Length);
                        rtbPlayerFeed.SelectionColor = Color.FromArgb(255, 35, 255);
                        btnAddToDefault.Enabled = true;
                    }
                }
                // Save player feed to file for external use
                BotTools.DumpToTextFile(rtbPlayerFeed.Text, "datasources\\now_playing");

                // Update tracking bar
                double position = windowsMP.Ctlcontrols.currentPosition / windowsMP.currentMedia.duration * 100;
                trackPosition.Value = Math.Min(Math.Max((int)position, 0), 100);
            }
            else if (downloadingAudio == true)
            {
                // Show download progress in player feed
                int progress = (int)(downloadProgress * 100);
                rtbPlayerFeed.Text = $"Downloading: {progress}%";
            }
            else if (normalizingAudio == true)
            {
                rtbPlayerFeed.Text = $"Normalizing audio...";
            }
            else if (draggingTracker == false)
            {
                // Show tracker position in player feed
                if (rtbPlayerFeed.Text != idleMessage)
                {
                    rtbPlayerFeed.Text = idleMessage;
                }
                BotTools.DumpToTextFile(idleMessage, "datasources\\now_playing");
                if (!playerStopped)
                {
                    btnAddToDefault.Enabled = false;
                }
            }
        }
		
		private void OnPlayerVisualizerInTimerTick(object sender, EventArgs e)
        {
            playerVisualizerUp = true;
            pnlPlayerPanel.Visible = true;
            double increment = 0.05;
            int maxW = 205;
            int maxH = 184;

            Point newPos = pnlPlayerPanel.Location;
            newPos.Offset(-5, -5);
            pnlPlayerPanel.Location = newPos;
            pnlPlayerPanel.Width = Math.Min(maxW, pnlPlayerPanel.Width + (int)(maxW * increment));
            pnlPlayerPanel.Height = Math.Min(maxH, pnlPlayerPanel.Height + (int)(maxH * increment));

            if (pnlPlayerPanel.Width == maxW && pnlPlayerPanel.Height == maxH)
            {
                tmrPlayerVisualizerIn.Stop();
            }
        }

        private void OnPlayerVisualizerOutTimerTick(object sender, EventArgs e)
        {
            int incrementY = 10;
            int incrementX = 10;

            Point newPos = pnlPlayerPanel.Location;
            newPos.Offset(5, 5);
            pnlPlayerPanel.Location = newPos;
            pnlPlayerPanel.Width = Math.Max(1, pnlPlayerPanel.Width - incrementX);
            pnlPlayerPanel.Height = Math.Max(1, pnlPlayerPanel.Height - incrementY);

            if (pnlPlayerPanel.Width == 1 && pnlPlayerPanel.Height == 1)
            {
                playerVisualizerUp = false;
                pnlPlayerPanel.Visible = false;
                pnlPlayerPanel.Location = playerLocationCache;
                tmrPlayerVisualizerOut.Stop();
            }
        }

        private void OnLiveAlertTimerTick(object sender, EventArgs e)
        {
            using (System.Media.SoundPlayer simpleSound = new System.Media.SoundPlayer(Properties.Resources.alert))
            {
                simpleSound.Play();
            }
        }
		
        private void OnFlashLiveTimerTick(object sender, EventArgs e)
        {
            pbLive.Enabled = !pbLive.Enabled;
            pbLive.BackgroundImage.Dispose();
            pbLive.BackgroundImage = pbLive.Enabled ? Properties.Resources.live_on : Properties.Resources.live_off;
        }

        // Background Workers
		private void GW2InventoryFetcherWork(object sender, DoWorkEventArgs e)
        {
            GuildWars.FetchInventoryData();
            BotTools.LogLine("GW2 Inventory Data Updated");
        }
		
        private void NormalizeDownloadedAudio(object sender, DoWorkEventArgs e)
        {
            normalizingAudio = true;
            string songID = Music.DownloadedSong.ID;
            string playlistName = Music.DownloadedSong.IsRequest ? "request" : Music.LoadedPlaylist;
            NAudioTools.Mp4ToWav($"playlists\\{playlistName}\\{songID}");
            NAudioTools.Normalize($"playlists\\{playlistName}\\{songID}");
            File.Delete($"{BotTools.BasePath}\\playlists\\{playlistName}\\{songID}.wav");
            if(unloadPath != null)
            {
                File.Delete(unloadPath);
                unloadPath = null;
            }
            normalizingAudio = false;
            BotTools.LogLine($"Normalized {Music.DownloadedSong.Title}");
        }

        private async void BackgroundCycleAudioQueue(object sender, DoWorkEventArgs e)
        {
            // Priority 1: Load downloaded song
            try
            {
                if (Music.DownloadedSong != null && Music.LoadedSong == null)
                {
                    Music.LoadedSong = Music.DownloadedSong;
                    Music.DownloadedSong = null;
                    //windowsMP.URL = Music.LoadedSong.URL;
                    //BotTools.LogLine($"Loaded {Music.LoadedSong.Title}");

                    try
                    {
                        string songID = Music.LoadedSong.ID;
                        string playlistName = Music.LoadedSong.Requestor == "RazBot" ? Music.LoadedPlaylist : "request";
                        string filePathNorm = $"{BotTools.BasePath}\\playlists\\{playlistName}\\{songID}_Normalized.wav";
                        string filePath = $"{BotTools.BasePath}\\playlists\\{playlistName}\\{songID}.mp4";

                        windowsMP.URL = File.Exists(filePathNorm) ? filePathNorm : filePath;
                    }
                    catch (NullReferenceException)
                    {
                        BotTools.LogLine($"Failed to play song, skipping...");
                        Music.QueueNextSong();
                    }

                    if (windowsMP.playState != WMPLib.WMPPlayState.wmppsPlaying && !playerStopped && windowsMP.playState != WMPLib.WMPPlayState.wmppsPaused)
                    {
                        try
                        {
                            windowsMP.Ctlcontrols.play();
                        }
                        catch (Exception playErr)
                        {
                            BotTools.LogToSessionLogFile(playErr.ToString());
                        }
                    }
                    else if (windowsMP.playState != WMPLib.WMPPlayState.wmppsPaused)
                    {
                        // Force stop since I can't get autoplay to stay disabled
                        windowsMP.Ctlcontrols.stop();
                    }
                }
                // Priority 2: Downloaded next song
                else if (Music.DownloadedSong == null && !downloadingAudio)
                {
                    if (BotTools.Settings["random_play"] == true)
                    {
                        Random rand = new Random();
                        var playlistCandidates = Music.Playlists.Where(p => p.Value.Songs.Count > 0);
                        Music.LoadedPlaylist = playlistCandidates.ElementAt(rand.Next(playlistCandidates.Count())).Key;
                        BotTools.Settings["loaded_playlist"] = Music.LoadedPlaylist;
                    }

                    SongData nextSong = Music.GetPlaylist("request").Songs.Count > 0 ?
                                        Music.GetPlaylist("request").GetNext() :
                                        Music.GetPlaylist(Music.LoadedPlaylist).GetNext();

                    if (nextSong != null)
                    {
                        Music.QueuedSong = YTE.GetSongData(nextSong.URL, requestor: nextSong.Requestor);

                        downloadProgress = 0.0;
                        downloadingAudio = true;
                        var progress = new Progress<double>(percent =>
                        {
                            downloadProgress = percent;
                        });

                        Music.DownloadedSong = await YTE.DownloadAudio(Music.QueuedSong, progress);
                        Music.QueuedSong = null;
                        downloadingAudio = false;
                        while (bgwAudioNormalizer.IsBusy) ;
                        if (cbNormalize.Checked && Music.DownloadedSong != null)
                        {
                            bgwAudioNormalizer.RunWorkerAsync();
                        }
                    }
                }
            }
            catch (Exception comExc)
            {
                BotTools.LogLine("Exception written to session log file");
                BotTools.LogToSessionLogFile(comExc.ToString());
            }
        }
    }
}
