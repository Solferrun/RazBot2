using System;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace RazBot
{
    public partial class WitTrainerForm : Form
    {
        public WitTrainerForm()
        {
            InitializeComponent();
            TopMost = true;
        }

        private void OnTrainUtteranceButtonClick(object sender, EventArgs e)
        {
            if (lbNewUtterances.SelectedItems.Count == 1)
            {
                string selectedIntent = $"{lbKnownIntents.SelectedItem}".Replace("*", "");
                CustomDialog.Form.CallDialog($"Train {selectedIntent}?\n\nYou sure?", DialogType.YesNo, PerformTraining);
            }
        }

        private void PerformTraining()
        {
            string newUtterance = $"{lbNewUtterances.SelectedItem}";
            string targetIntent = $"{lbKnownIntents.SelectedItem}".Replace("*", "");
            BotTools.NewUtterances.Remove(newUtterance);
            Wit.TrainUtterance(newUtterance, targetIntent);
            lbNewUtterances.DataSource = BotTools.NewUtterances.ToList();
        }

        // Window dragging
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        private void OnPlayerWindowMouseDownw(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
            else if (e.Button == MouseButtons.Right)
            {
                Hide();
            }
        }

        private void WitTrainerForm_VisibleChanged(object sender, EventArgs e)
        {
            if (Visible)
            {
                lbNewUtterances.DataSource = BotTools.NewUtterances.ToList();
                lbKnownIntents.DataSource = Wit.ListIntents().Select(i => TwitchBot.Commands.Map.Keys.Contains(i) ? $"*{i}" : i).ToList();
            }
        }

        private void OnNewUtterancesListboxMouseHover(object sender, EventArgs e)
        {
            if (lbNewUtterances.SelectedItems.Count == 1)
            {
                ttDetails.SetToolTip(lbNewUtterances, $"{lbNewUtterances.SelectedItem}");
            }
        }

        private void OnNewUtterancesListboxSelectedIndexChanged(object sender, EventArgs e)
        {
            ttDetails.SetToolTip(lbNewUtterances, $"{lbNewUtterances.SelectedItem}");
        }

        private void OnTokenTextBoxKeyPress(object sender, KeyPressEventArgs e)
        {
            e.KeyChar = char.ToLower(e.KeyChar);
            if (!(char.IsLetterOrDigit(e.KeyChar) || e.KeyChar == '_' || e.KeyChar == (char)Keys.Back))
            {
                e.Handled = true;
            }
        }

        private void OnAddCommandButtonClick(object sender, EventArgs e)
        {
            string newToken = rtbToken.Text;
            string commandBody = rtbCommandBody.Text;
            if (!Wit.ListIntents().Contains(newToken))
            {
                Wit.AddIntent(newToken);
                Wit.TrainUtterance($"!{newToken}", newToken);
            }

            TwitchBot.Commands.CustomMap[newToken] = commandBody;
            lbKnownIntents.DataSource = Wit.ListIntents().Select(i => TwitchBot.Commands.Map.Keys.Contains(i) ? $"*{i}": i).ToList<string>();
            TwitchBot.Commands.SaveCustomCommands();
            rtbToken.Text = "";
            rtbCommandBody.Text = "";
        }

        private void OnRemoveUtteranceButtonClick(object sender, EventArgs e)
        {
            if (lbNewUtterances.SelectedItems.Count == 1)
            {
                CustomDialog.Form.CallDialog("Useless\n\nGet rid of it", DialogType.YesNo, RemoveUtterance);
            }
        }

        private void RemoveUtterance()
        {
            string uselessUtterance = $"{lbNewUtterances.SelectedItem}";
            BotTools.NewUtterances = BotTools.NewUtterances.Where(u => u != uselessUtterance).ToList();
            lbNewUtterances.DataSource = BotTools.NewUtterances.ToList();
        }

        private void OnRefreshButtonClick(object sender, EventArgs e)
        {
            lbNewUtterances.DataSource = BotTools.NewUtterances.ToList();
        }

        private void ReleaseFocus(object sender, EventArgs e)
        {
            pbFocus.Focus();
        }

        private void OnRemoveIntentButtonClick(object sender, EventArgs e)
        {
            if (lbKnownIntents.SelectedItems.Count == 1)
            {
                string targetToken = $"{lbKnownIntents.SelectedItem}";
                if (TwitchBot.Commands.CustomMap.Keys.Contains(targetToken))
                {
                    CustomDialog.Form.CallDialog($"Unlearn {targetToken}?\n\nFor real?!", DialogType.YesNo, RemoveIntent);
                }
                else
                {
                    CustomDialog.Form.CallDialog("That's my command\n\nWe're keeping it", DialogType.OK);
                }
            }
        }

        private void RemoveIntent()
        {
            string targetToken = $"{lbKnownIntents.SelectedItem}";
            TwitchBot.Commands.CustomMap.Remove(targetToken);
            lbKnownIntents.DataSource = Wit.ListIntents().Select(i => TwitchBot.Commands.Map.Keys.Contains(i) ? $"*{i}" : i).ToList<string>();
            TwitchBot.Commands.SaveCustomCommands();
            Wit.DeleteIntent(targetToken);
        }

        private void OnTrainWwitButtonMouseover(object sender, EventArgs e)
        {
            ttDetails.SetToolTip(btnTrainWit, "Associate command or question with intent; send to WitAI to train.");
        }

        private void OnTokenTextBoxTextChanged(object sender, EventArgs e)
        {
            var tokenReserved = TwitchBot.Commands.Map.Keys.Contains(rtbToken.Text);
            btnAddCommand.Enabled = (!String.IsNullOrEmpty(rtbToken.Text) && !tokenReserved);
            if (TwitchBot.Commands.CustomMap.Keys.Contains(rtbToken.Text))
            {
                btnAddCommand.Text = "EDIT";
                rtbCommandBody.Text = TwitchBot.Commands.CustomMap[rtbToken.Text];
            }
            else
            {
                btnAddCommand.Text = "ADD";
            }
        }

        private void OnKnownIntentsListboxDoubleClick(object sender, EventArgs e)
        {
            string token = lbKnownIntents.SelectedItem.ToString();
            if (TwitchBot.Commands.CustomMap.Keys.Contains(token))
            {
                rtbToken.Text = token;
                rtbCommandBody.Text = TwitchBot.Commands.CustomMap[token];
            }
        }

        private void KnownIntentsListBoxKeyPress(object sender, KeyPressEventArgs e)
        {
            
        }

        private void OnKnownIntentsListboxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                OnRemoveIntentButtonClick(sender, e);
            }
        }

        private void NewUtterancesListboxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                OnRemoveUtteranceButtonClick(sender, e);
            }
            if (e.KeyCode == Keys.R)
            {
                OnRefreshButtonClick(sender, e);
            }
        }

        private void OnTokenTextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Tab)
            {
                rtbCommandBody.Focus();
                rtbCommandBody.Text = "F";
            }
        }

        private void OnCommandBodyTextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Tab)
            {
                rtbToken.Focus();
                rtbToken.Text = "F";
            }
        }
    }
}
