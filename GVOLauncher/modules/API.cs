using Microsoft.Win32;
using System;
using System.IO;
using System.Management;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using System.Text.Json;
using System.Collections.Specialized;
using IniParser;
using IniParser.Model;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Drawing;
using System.IO.Compression;

namespace GVOLauncher.modules
{
    class Config
    {
        public AppSettings AppSettings { get; set; }
    }

    class AppSettings
    {
        public string Setting1 { get; set; }
        public int Setting2 { get; set; }
        public bool Setting3 { get; set; }
    }
    class API
    {
        public string ServerAPI = "https://khnguyen.store/";
        public string LauncherPath = Directory.GetCurrentDirectory();
        public string Launcher_Data_Path = "Data";

        public int ShowMsgError(string msg)
        {
            MessageBoxIcon icon = MessageBoxIcon.Error;
            MessageBox.Show(msg, "Thông báo", MessageBoxButtons.OK, icon);
            return 1;
        }
        public int ShowMsgInfo(string msg)
        {
            MessageBoxIcon icon = MessageBoxIcon.Information;
            MessageBox.Show(msg, "Thông báo", MessageBoxButtons.OK, icon);
            return 1;
        }


        public int GetUpgrade()
        {
            var (paths, hashcodes) = LoadJsonConfig();
            if (paths.Length > 0 && hashcodes.Length > 0) return 1;
            else return 0;
        }
        public (string[] paths, string[] hashcodes) LoadJsonConfig()
        {
            try
            {
                string jsonString = this.CallAPI($"{ServerAPI}upgrade_output.json");
                var jsonData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonString);

                if (jsonData != null)
                {
                    string[] paths = jsonData.Keys.ToArray();
                    string[] hashcodes = jsonData.Values.Select(v => v.GetString()).ToArray();

                    return (paths, hashcodes);
                }
                return (null, null);
            }
            catch (Exception)
            {
                return (null, null);
            }
        }
        public NameValueCollection LoadClientConfig(string path, string sectionData)
        {
            var parser = new FileIniDataParser();
            IniData data;
            if (!File.Exists(path))
            {
                data = new IniData();
                data.Sections.AddSection(sectionData);
                data[sectionData].AddKey("GamePath", "");
                data[sectionData].AddKey("Name", "");
                parser.WriteFile(path, data);
            }
            else
            {
                data = parser.ReadFile(path);
            }
            var config = new NameValueCollection();
            foreach (var key in data[sectionData])
            {
                config[key.KeyName] = key.Value;
            }
            return config;
        }

        public void SaveClientConfig(string path, NameValueCollection config, string sectionName)
        {
            var parser = new FileIniDataParser();
            IniData data;
            if (File.Exists(path))
            {
                data = parser.ReadFile(path);
            }
            else
            {
                data = new IniData();
            }
            if (!data.Sections.ContainsSection(sectionName))
            {
                data.Sections.AddSection(sectionName);
            }

            var section = data.Sections.GetSectionData(sectionName);
            foreach (string key in config)
            {
                section.Keys[key] = config[key];
            }
            parser.WriteFile(path, data);
        }


        public string GetLauncherPath()
        {
            string LauncherPathzzz = Directory.GetCurrentDirectory();
            return LauncherPathzzz;
        }
        public void LauncherLog(string Logmsg)
        {
            string logFilePath = Path.Combine(Launcher_Data_Path, "log.txt");

            try
            {
                using (StreamWriter sw = new StreamWriter(logFilePath, true))
                {
                    sw.WriteLine($"[{DateTime.Now}]: {Logmsg}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        public string GetSAMPDirectory()
        {
            try
            {
                var datag = LoadClientConfig(Path.Combine(Launcher_Data_Path, "gvo_config.ini"), "Launcher");
                return datag["GamePath"];
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi: " + ex.Message);
            }

            return null;
        }
        public bool IsVaildSAMP(string gamepath)
        {
            return File.Exists(Path.Combine(gamepath, "samp.exe"));
        }
        public int StartGame(string N_Name, string ServerIP , string Port)
        {
            var datag = this.LoadClientConfig(Path.Combine(this.Launcher_Data_Path, "gvo_config.ini"), "Launcher");

            string samppath = Path.Combine(datag["GamePath"], "samp.exe");
            if (!File.Exists(samppath)) return 0;
            Registry.CurrentUser.OpenSubKey("Software\\SAMP", true).SetValue("PlayerName", N_Name);
            Process.Start(samppath, $"{ServerIP}:{Port}");
            return 1;
        }
        public void SaveNickName(string NickName)
        {
            var data = LoadClientConfig(Path.Combine(Launcher_Data_Path, "gvo_config.ini"), "Launcher");
            data["Name"] = NickName.ToString();
            SaveClientConfig(Path.Combine(Launcher_Data_Path, "gvo_config.ini"), data, "Launcher");
        }
        public async Task SetPictureBoxImageFromUrl(string url, PictureBox _pictureBox)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    _pictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
                    var response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    var imageStream = await response.Content.ReadAsStreamAsync();
                    _pictureBox.Image = Image.FromStream(imageStream);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi tải hình ảnh: {ex.Message}");
                }
            }
        }
        public StringBuilder CheckCleo(string game_path, Label msg, ProgressBar _progressBar)
        {
            StringBuilder UnAllowCleo = new StringBuilder();
            DirectoryInfo directory = new DirectoryInfo(game_path);
            string[] extensions = { "*.cs", "*.asi", "*.dll", "*.lib", "*.lua", "*.luac", "*.cleo", "*.ahk" };
            FileInfo[] files = extensions
                .SelectMany(ext => directory.GetFiles(ext, SearchOption.AllDirectories))
                .ToArray();
            int current_values = 0;
            _progressBar.Value = 0;
            _progressBar.Maximum = files.Length;
            foreach (FileInfo file in files)
            {
                current_values++;
                _progressBar.Value = current_values;
                string CurlUrl = GetAPIUrl("CheckCleo", $"KNCMS_HASHFILE={HashCleo(file.FullName)}&Name={file.Name}");
                msg.Text = $"Checking {file.Name}";
                string Result_Out = "";
                string Result_HW = CallAPI(CurlUrl);
                foreach (char c in Result_HW)
                {
                    if (char.IsDigit(c))
                    {
                        Result_Out += c;
                    }
                }
                LauncherLog($"{CurlUrl} \n Response: {Result_Out}");
                if (Result_Out != "1")
                {
                    UnAllowCleo.AppendLine(file.FullName);
                }
            }
            return UnAllowCleo;
        }

        public string GetIPV4(string domain)
        {
            IPHostEntry hostEntry = Dns.GetHostEntry(domain);

            foreach (IPAddress address in hostEntry.AddressList)
            {
                if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    string ipv4Address = address.ToString();
                    return ipv4Address;
                }
            }
            return null;
        }
        public string CallAPI(string API_URL)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    ServicePointManager.Expect100Continue = true;
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                    HttpResponseMessage response = client.GetAsync(API_URL).Result;

                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = response.Content.ReadAsStringAsync().Result;
                        return responseBody;
                    }
                    else
                    {
                        Console.WriteLine("Không thể kết nối tới URL.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ket qua THAT BAI \nAPI Tra ve: {ex.Message}");
            }
            return null;
        }
        public void DownloadAndExtractZip(string fileUrl, string destinationPath, string extractPath)
        {
            try
            {
                DownloadFileFromWeb(fileUrl, destinationPath);
                ExtractZipFile(destinationPath, extractPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}");
            }
        }

        public void DownloadFileFromWeb(string fileUrl, string destinationPath)
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    client.DownloadFile(fileUrl, destinationPath);
                }
            }
            catch (WebException ex)
            {
                MessageBox.Show($"Lỗi tải xuống tệp: {ex.Message}");
                if (ex.Response != null)
                {
                    using (var responseStream = ex.Response.GetResponseStream())
                    using (var reader = new StreamReader(responseStream))
                    {
                        string responseText = reader.ReadToEnd();
                        MessageBox.Show($"Chi tiết lỗi: {responseText}");
                    }
                }
            }
        }

        public int ExtractZipFile(string zipPath, string extractPath)
        {
            try
            {
                ZipFile.ExtractToDirectory(zipPath, extractPath);
                return 1;
            }
            catch (InvalidDataException ex)
            {
                MessageBox.Show($"Lỗi tệp zip không hợp lệ: {ex.Message}");
                return 0;
            }
        }
        public string HashCleo(string filepath)
        {
            try
            {
                using (var md5 = MD5.Create())
                {
                    using (var stream = File.OpenRead(filepath))
                    {
                        byte[] hash = md5.ComputeHash(stream);
                        string fileHash = BitConverter.ToString(hash).Replace("-", "").ToLower();

                        return fileHash;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error Message: {ex.Message}");
            }
            return null;
        }
        public string GetAPIUrl(string Action, string Extension)
        {
            string UrlAPI = $"{ServerAPI}api/api/api.php?KNCMS_ACTION={Action}&{Extension}";
            return UrlAPI;
        }
        public string[] AppendToStringArray(string[] array, string value)
        {
            Array.Resize(ref array, array.Length + 1);
            array[array.Length - 1] = value;
            return array;
        }

        public string GetHardwareID()
        {
            string hardwareID = string.Empty;
            try
            {
                ManagementObjectCollection mbs = new ManagementClass("Win32_Processor").GetInstances();
                foreach (ManagementObject mo in mbs)
                {
                    hardwareID = mo.Properties["ProcessorID"].Value.ToString();
                    break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error getting hardware ID: " + ex.Message);
            }
            return hardwareID;
        }

        public Config LoadConfig(string path)
        {
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<Config>(json);
        }

        public void SaveConfig(string path, Config config)
        {
            string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
    }
}
