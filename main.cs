using System;
using System.IO;
using System.Net;
using System.Windows.Forms;
using System.Reflection;
using System.Diagnostics;

public class InstallerForm : Form
{
    private TextBox pathTextBox;
    private Button browseButton, installButton;
    private CheckBox desktopShortcutCheckBox, startMenuShortcutCheckBox;
    private ProgressBar downloadProgressBar;
    private Label progressLabel;
    
    private string downloadUrl = "https://zephyrgddp.github.io/steaminstaller/archive.zip"; // Fixed URL
    private string shortcutTargetFile = "Steam/Steam.exe"; // Editable target file for shortcuts
    
    public InstallerForm()
    {
        this.Text = "Steam Installer";
        this.Width = 550;
        this.Height = 300;

        Label pathLabel = new Label { Text = "Select install path:", Top = 20, Left = 10, Width = 120 };
        pathTextBox = new TextBox { Top = 20, Left = 140, Width = 280 };
        browseButton = new Button { Text = "Browse", Top = 20, Left = 430 };
        browseButton.Click += BrowseButton_Click;

        desktopShortcutCheckBox = new CheckBox { Text = "Create Desktop Shortcut", Top = 60, Left = 10 };
        startMenuShortcutCheckBox = new CheckBox { Text = "Create Start Menu Shortcut", Top = 90, Left = 10 };

        installButton = new Button { Text = "Install Steam", Top = 130, Left = 10, Width = 120 };
        installButton.Click += InstallButton_Click;

        downloadProgressBar = new ProgressBar { Top = 170, Left = 10, Width = 500, Height = 20 };
        progressLabel = new Label { Text = "Download progress: 0%", Top = 200, Left = 10, Width = 500 };

        this.Controls.Add(pathLabel);
        this.Controls.Add(pathTextBox);
        this.Controls.Add(browseButton);
        this.Controls.Add(desktopShortcutCheckBox);
        this.Controls.Add(startMenuShortcutCheckBox);
        this.Controls.Add(installButton);
        this.Controls.Add(downloadProgressBar);
        this.Controls.Add(progressLabel);
    }

    private void BrowseButton_Click(object sender, EventArgs e)
    {
        using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
        {
            if (folderDialog.ShowDialog() == DialogResult.OK)
            {
                pathTextBox.Text = folderDialog.SelectedPath;
            }
        }
    }

    private void InstallButton_Click(object sender, EventArgs e)
    {
        string installPath = pathTextBox.Text;
        string zipPath = Path.Combine(Path.GetTempPath(), "archive.zip");

        if (string.IsNullOrWhiteSpace(installPath))
        {
            MessageBox.Show("Please provide a valid installation path.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        try
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11;
            WebClient client = new WebClient();
            client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DownloadProgressCallback);
            client.DownloadFileCompleted += new System.ComponentModel.AsyncCompletedEventHandler(DownloadFileCompletedCallback);
            client.DownloadFileAsync(new Uri(downloadUrl), zipPath, installPath);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error: " + ex.Message, "Installation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void DownloadProgressCallback(object sender, DownloadProgressChangedEventArgs e)
    {
        downloadProgressBar.Value = e.ProgressPercentage;
        progressLabel.Text = "Download progress: " + e.ProgressPercentage + "%";
    }

    private void DownloadFileCompletedCallback(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
    {
        if (e.Error == null)
        {
            string installPath = (string)e.UserState;
            InstallSteam(Path.Combine(Path.GetTempPath(), "archive.zip"), installPath);
        }
        else
        {
            MessageBox.Show("Download failed: " + e.Error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void InstallSteam(string zipPath, string installPath)
    {
        if (!Directory.Exists(installPath))
        {
            Directory.CreateDirectory(installPath);
        }

        ExtractZipWithShell(zipPath, installPath);
        File.Delete(zipPath);

        if (desktopShortcutCheckBox.Checked)
        {
            CreateShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Steam.lnk"), Path.Combine(installPath, shortcutTargetFile));
        }

        if (startMenuShortcutCheckBox.Checked)
        {
            string startMenuProgramsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs");
            CreateShortcut(Path.Combine(startMenuProgramsPath, "Steam.lnk"), Path.Combine(installPath, shortcutTargetFile));
        }

        MessageBox.Show("Installation complete!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ExtractZipWithShell(string zipPath, string extractPath)
    {
        string shellCmd = "powershell -Command \"Expand-Archive -Path '" + zipPath + "' -DestinationPath '" + extractPath + "' -Force\"";
        Process process = new Process();
        process.StartInfo.FileName = "cmd.exe";
        process.StartInfo.Arguments = "/c " + shellCmd;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.CreateNoWindow = true;
        process.Start();
        process.WaitForExit();
    }

    private void CreateShortcut(string shortcutPath, string targetPath)
    {
        using (StreamWriter writer = new StreamWriter(shortcutPath + ".url"))
        {
            writer.WriteLine("[InternetShortcut]");
            writer.WriteLine("URL=file://" + targetPath);
            writer.WriteLine("IconIndex=0");
            writer.WriteLine("IconFile=" + targetPath + "\\public\\steam_tray.ico");
        }
    }

    [STAThread]
    public static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new InstallerForm());
    }
}
