using System;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using ShorName = GVOLauncher.modules.API;

namespace GVOLauncher
{
    public partial class Setting : Form
    {
        int Cleo1;
        int Cleo2;
        int Cleo3;
        int Cleo4;
        private Timer timer;
        ShorName API = new ShorName();
        private bool isDragging = false;
        private Point dragStartPosition;
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
        public Setting()
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

            var data = API.LoadClientConfig(Path.Combine(API.Launcher_Data_Path, "gvo_config.ini"), "Launcher");
            label2.Text = data["GamePath"];

            Cleo1 = int.Parse(data["Cleo1"]);
            Cleo2 = int.Parse(data["Cleo2"]);
            Cleo3 = int.Parse(data["Cleo3"]);
            Cleo4 = int.Parse(data["Cleo4"]);

            timer = new Timer();
            timer.Interval = 500;
            timer.Tick += Timer_Tick;
            timer.Start();
        }
        private void Timer_Tick(object sender, EventArgs e)
        {
            var data = API.LoadClientConfig(Path.Combine(API.Launcher_Data_Path, "gvo_config.ini"), "Launcher");
            label2.Text = data["GamePath"];

            Cleo1 = int.Parse(data["Cleo1"]);
            Cleo2 = int.Parse(data["Cleo2"]);
            Cleo3 = int.Parse(data["Cleo3"]);
            Cleo4 = int.Parse(data["Cleo4"]);


            ActionBtn(option1, Cleo1);
            ActionBtn(option2, Cleo2);
            ActionBtn(option3, Cleo3);
            ActionBtn(option4, Cleo4);
        }

        private void Setting_Load(object sender, EventArgs e)
        {

        }

        private void pictureBox3_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            var data = API.LoadClientConfig(Path.Combine(API.Launcher_Data_Path, "gvo_config.ini"), "Launcher");
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
                        if (files.Length == 0 && subDirs.Length == 0 || File.Exists(Path.Combine(selectedPath,"gvo.asi")))
                        {
                            data["GamePath"] = folderDialog.SelectedPath;
                            API.SaveClientConfig(Path.Combine(API.Launcher_Data_Path, "gvo_config.ini"), data, "Launcher");
                        }
                        else API.ShowMsgError("Bạn cần chọn 1 Folder trống hoặc sử dụng Client của GVO !");
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
        private void SaveCleoStatus(int CleoID, int status)
        {
            var data = API.LoadClientConfig(Path.Combine(API.Launcher_Data_Path, "gvo_config.ini"), "Launcher");
            data[$"Cleo{CleoID}"] = status.ToString();
            API.SaveClientConfig(Path.Combine(API.Launcher_Data_Path, "gvo_config.ini"), data, "Launcher");
        }
        private void DeleteCleo(string cleo)
        {
            string GamePath = API.GetSAMPDirectory();
            string CleoPath = Path.Combine(GamePath, "cleo");

            if (File.Exists(Path.Combine(CleoPath, cleo)))
            {
                File.Delete(Path.Combine(CleoPath, cleo));
            }
        }
        private void DownloadCleo(string Cleo)
        {
            string GamePath = API.GetSAMPDirectory();
            string CleoPath = Path.Combine(GamePath, "cleo");
            Cleo = Cleo.Replace(".cs", ".zip");
            string UrlCleo = $"{API.ServerAPI}data/cleo/{Cleo}";
            string DownloadPath = $"{CleoPath}/{Cleo}";
            API.DownloadFileFromWeb(UrlCleo, DownloadPath);
            int _Cleo_Extract = API.ExtractZipFile(DownloadPath, CleoPath);
            if (_Cleo_Extract == 1)
            {
                API.ShowMsgInfo($"Cài đặt thành công Cleo {Cleo.Replace(".zip", "")}");
                File.Delete(DownloadPath);
            }
            else
            {
                API.ShowMsgError($"Cài đặt thất bại Cleo {Cleo.Replace(".zip", "")}");
            }
        }
        private void option1_Click(object sender, EventArgs e)
        {
            int status = ActionBtn(option1, Cleo1);
            if(status == 1)
            {
                string _Cleo = "tracer.cs";
                DeleteCleo(_Cleo);
                DownloadCleo(_Cleo);
            }
            else
            {
                string _Cleo = "tracer.cs";
                DeleteCleo(_Cleo);
            }
            SaveCleoStatus(1, status);
        }


        private int ActionBtn(PictureBox _button, int active = 1)
        {
            if(active == 1)
            {
                _button.Image = Properties.Resources.enable;
                return 0;
            }
            else
            {
                _button.Image = Properties.Resources.disable;
                return 1;
            }
        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox5_Click(object sender, EventArgs e)
        {
            int status = ActionBtn(option2, Cleo2);
            if (status == 1)
            {
                string _Cleo = "2GBStream.cs";
                DeleteCleo(_Cleo);
                DownloadCleo(_Cleo);

                _Cleo = "memory512.cs";
                DeleteCleo(_Cleo);
                DownloadCleo(_Cleo);

                _Cleo = "StreamMemory.cs";
                DeleteCleo(_Cleo);
                DownloadCleo(_Cleo);

            }
            else
            {
                string _Cleo = "tracer.cs";
                DeleteCleo(_Cleo);
                _Cleo = "memory512.cs";
                DeleteCleo(_Cleo);
                _Cleo = "StreamMemory.cs";
                DeleteCleo(_Cleo);
            }

            SaveCleoStatus(2, status);
        }

        private void pictureBox4_Click(object sender, EventArgs e)
        {

            int status = ActionBtn(option3, Cleo3);
            if (status == 1)
            {
                string _Cleo = "Car-HP.cs";
                DeleteCleo(_Cleo);
                DownloadCleo(_Cleo);

            }
            else
            {
                string _Cleo = "Car-HP.cs";
                DeleteCleo(_Cleo);
            }
            SaveCleoStatus(3, status);
        }

        private void pictureBox6_Click(object sender, EventArgs e)
        {
            int status = ActionBtn(option4, Cleo4);
            if (status == 1)
            {
                string _Cleo = "FastPed.cs";
                DeleteCleo(_Cleo);
                DownloadCleo(_Cleo);

            }
            else
            {
                string _Cleo = "FastPed.cs";
                DeleteCleo(_Cleo);
            }
            SaveCleoStatus(4, status);
        }
    }
}
