using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Drawing;
using ShortName = GVOLauncher.modules.API;
using Game = GVOLauncher.modules.API_Game;
using System.IO;
using System.Collections.Specialized;
using System.Text;
using Microsoft.Win32;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using DiscordRPC;

namespace GVOLauncher
{
    public partial class Form1 : Form
    {
        public DiscordRpcClient client;
        ShortName API = new ShortName();
        Game API_Game = new Game();
        private const int WM_NCHITTEST = 0x84;
        private const int HTCLIENT = 0x1;
        private const int HTCAPTION = 0x2;
        private bool isDragging = false;
        private Point dragStartPosition;
        private Timer timer;
        public int Status_Launcher = 0;
        // 0: Choose GamePath
        // 1: Play
        // 2: Upgrade

        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn
        (
            int nLeftRect,     // x-coordinate of upper-left corner
            int nTopRect,      // y-coordinate of upper-left corner
            int nRightRect,    // x-coordinate of lower-right corner
            int nBottomRect,   // y-coordinate of lower-right corner
            int nWidthEllipse, // height of ellipse
            int nHeightEllipse // width of ellipse
        );

        public static List<Dictionary<string, string>> GetDataFromJS(string json)
        {
            var result = new List<Dictionary<string, string>>();

            // Phân tích JSON thành JArray
            JArray jsonArray = JArray.Parse(json);

            foreach (JObject obj in jsonArray)
            {
                var item = new Dictionary<string, string>();

                foreach (var property in obj.Properties())
                {
                    item[property.Name] = property.Value.ToString();
                }

                result.Add(item);
            }

            return result;
        }

        public Form1()
        {
            InitializeComponent();

            this.FormBorderStyle = FormBorderStyle.None;
            Region = System.Drawing.Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, 30, 30));

            this.MouseDown += (sender, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    isDragging = true;
                    dragStartPosition = new Point(e.X, e.Y);
                }
            };

            this.MouseMove += (sender, e) =>
            {
                if (isDragging)
                {
                    Point currentPos = PointToScreen(new Point(e.X, e.Y));
                    Location = new Point(currentPos.X - dragStartPosition.X, currentPos.Y - dragStartPosition.Y);
                }
            };

            this.MouseUp += (sender, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    isDragging = false;
                }
            };

            timer = new Timer();
            timer.Interval = 300;
            timer.Tick += Timer_Tick;
            timer.Start();
        }
        private void Timer_Tick(object sender, EventArgs e)
        {
            var data = API.LoadClientConfig(Path.Combine(API.Launcher_Data_Path, "gvo_config.ini"), "Launcher");
            NickName.Text = data["Name"];
            var news = GetDataFromJS(API.CallAPI($"{API.ServerAPI}data/news.json"));
            if (news[0]["content"] != "")
            {
                API.SetPictureBoxImageFromUrl(news[1]["thumbnail"].ToString(), img_new1);
                label_news1.Text = news[0]["content"].ToString();
            }
            if (news[1]["content"] != "")
            {
                API.SetPictureBoxImageFromUrl(news[1]["thumbnail"].ToString(), img_news2);
                label_news2.Text = news[1]["content"].ToString();
            }
            if (data["GamePath"] == "")
            {
                pictureBox2.Image = Properties.Resources.choose;
                Status_Launcher = 0;
            }
            else
            {
                if(API.IsVaildSAMP(data["GamePath"]) == false)
                {
                    pictureBox2.Image = Properties.Resources.download;
                    Status_Launcher = 3;
                }
                else
                {
                    if (API.GetUpgrade() == 1)
                    {
                        Status_Launcher = 2;
                        pictureBox2.Image = Properties.Resources.upgrade;
                    }
                    else
                    {
                        Status_Launcher = 1;
                        pictureBox2.Image = Properties.Resources.Play;
                    }
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        private void progressBar1_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox5_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox9_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox9_Click_1(object sender, EventArgs e)
        {
            Setting setting_f = new Setting();
            setting_f.Show();
        }

        private void pictureBox8_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void img_new1_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            var data = API.LoadClientConfig(Path.Combine(API.Launcher_Data_Path, "gvo_config.ini"), "Launcher");
            if (Status_Launcher == 0) // choose
            {
                using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
                {
                    DialogResult result = folderDialog.ShowDialog();

                    if (result == DialogResult.OK)
                    {
                        string selectedPath = folderDialog.SelectedPath;

                        if (!string.IsNullOrWhiteSpace(selectedPath))
                        {
                            string[] files = Directory.GetFiles(folderDialog.SelectedPath);
                            string[] subDirs = Directory.GetDirectories(folderDialog.SelectedPath);
                            if (files.Length == 0 && subDirs.Length == 0)
                            {
                                data["GamePath"] = folderDialog.SelectedPath;
                                API.SaveClientConfig(Path.Combine(API.Launcher_Data_Path, "gvo_config.ini"), data, "Launcher");
                            }
                            else API.ShowMsgError("Bạn cần chọn 1 Folder trống");
                        }
                        else
                        {
                            MessageBox.Show("No folder selected.");
                        }
                    }
                    else
                    {
                        MessageBox.Show("Folder selection canceled.");
                    }
                }
            }
            else if(Status_Launcher == 1)
            {
                bool hack_checking = false;
                label1.Text = "Kiểm tra file game, vui lòng đợi trong giây lát";

                StringBuilder result = API.CheckCleo(data["GamePath"], label1, progressBar1);


                if (result.Length > 0)
                {
                    API.ShowMsgError($"Vui lòng gỡ: \n{result.ToString()}");
                    hack_checking = true;
                    label1.Text = "Invalid file detected!";
                }
                else
                {
                    label1.Text = "Complete checking";
                    var server = GetDataFromJS(API.CallAPI($"{API.ServerAPI}data/server.json"));
                    if (API.StartGame(NickName.Text, server[0]["ip"].ToString(), server[0]["port"].ToString()) == 0)
                    {
                        API.ShowMsgError("Khong tim thay samp.exe");
                    }
                }

            }
        }

        private void NickName_TextChanged(object sender, EventArgs e)
        {
            API.SaveNickName(NickName.Text);
        }
    }
}
