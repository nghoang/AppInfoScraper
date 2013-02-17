using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;


namespace AppInfoScraper
{
    public partial class FormScraper : Form, IAppInfoBot
    {
        Thread thBot;
        AppInfoBot bot = new AppInfoBot();

        public FormScraper()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            cbRegion.SelectedIndex = 0;
            cbCategory.SelectedIndex = 0;
            cbStore.SelectedIndex = 0;
            Control.CheckForIllegalCrossThreadCalls = false;
        }

        public void Log(string log)
        {
            listBoxLog.Items.Add(log);
            listBoxLog.SelectedIndex = listBoxLog.Items.Count - 1;
        }

        private void buttonStart_Click(object sender, EventArgs e)
        {
            if (buttonStart.Text == "Start")
                Start();
            else
                Stop();
        }

        void Start()
        {
            cbCategory.Enabled = false ;
            cbRegion.Enabled = false;
            cbStore.Enabled = false;
            buttonStart.Text = "Stop";
            bot.main = this;
            bot.region = cbRegion.SelectedItem.ToString();
            bot.category = cbCategory.SelectedItem.ToString();
            bot.store = cbStore.SelectedItem.ToString();

            if (bot.store == "App Store")
            {
                thBot = new Thread(new ThreadStart(bot.runAppStore));
                thBot.Start();
            }
            else if (bot.store == "Google Play")
            {
                thBot = new Thread(new ThreadStart(bot.runGooglePlay));
                thBot.Start();
            }
        }

        void Stop()
        {
            cbCategory.Enabled = true;
            cbRegion.Enabled = true;
            cbStore.Enabled = true;
            buttonStart.Text = "Start";
            if (thBot != null)
            {
                bot.stop = true;
                thBot.Abort();
            }
        }

        public void Finished()
        {
            Stop();
        }
    }
}
