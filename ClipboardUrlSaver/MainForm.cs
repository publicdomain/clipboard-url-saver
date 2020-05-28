// <copyright file="MainForm.cs" company="PublicDomain.com">
//     CC0 1.0 Universal (CC0 1.0) - Public Domain Dedication
//     https://creativecommons.org/publicdomain/zero/1.0/legalcode
// </copyright>

namespace ClipboardUrlSaver
{
    // Directives
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Text.RegularExpressions;
    using System.Windows.Forms;
    using System.Xml.Serialization;
    using Microsoft.Win32;

    /// <summary>
    /// Description of MainForm.
    /// </summary>
    public partial class MainForm : Form
    {
        /// <summary>
        /// The clipboard update windows message.
        /// </summary>
        private const int WmClipboardUpdate = 0x031D;

        /// <summary>
        /// The URL count.
        /// </summary>
        private int urlCount = 0;

        /// <summary>
        /// The settings data.
        /// </summary>
        private SettingsData settingsData = new SettingsData();

        /// <summary>
        /// The assembly version.
        /// </summary>
        private Version assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;

        /// <summary>
        /// The semantic version.
        /// </summary>
        private string semanticVersion = string.Empty;

        /// <summary>
        /// The associated icon.
        /// </summary>
        private Icon associatedIcon = null;

        /// <summary>
        /// The friendly name of the program.
        /// </summary>
        private string friendlyName = "Clipboard URL saver";

        /// <summary>
        /// Initializes a new instance of the <see cref="T:ClipboardUrlSaver.MainForm"/> class.
        /// </summary>
        public MainForm()
        {
            // The InitializeComponent() call is required for Windows Forms designer support.
            this.InitializeComponent();

            // Set notify icon
            this.mainNotifyIcon.Icon = this.Icon;

            // Set semantic version
            this.semanticVersion = this.assemblyVersion.Major + "." + this.assemblyVersion.Minor + "." + this.assemblyVersion.Build;

            // TODO Set current directory [can be made conditional to: args[1] == "/autostart"]
            Directory.SetCurrentDirectory(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));

            // Set initial file
            this.saveFileTextBox.Text = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "toRead.html");

            /* Process settings */

            // Set settings file path
            var settingsFilePath = "SettingsData.txt";

            // Check for settings data file
            if (!File.Exists(settingsFilePath))
            {
                // Not present, assume first run and create it
                this.SaveSettingsData();
            }

            // Populate settings data
            this.settingsData = this.LoadSettingsData();

            // Set registry entry based on settings data
            this.ProcessRunAtStartupRegistry();

            /* Clipboard */

            // Add clipboard listener
            AddClipboardFormatListener(this.Handle);
        }

        /// <summary>
        /// The Window procedure.
        /// </summary>
        /// <param name="m">The message.</param>
        protected override void WndProc(ref Message m)
        {
            // Test incoming message
            switch (m.Msg)
            {
                // Check for clipboard update
                case WmClipboardUpdate:

                    // Check for copied text
                    if (Clipboard.ContainsText())
                    {
                        // Set clipboard text variable
                        string clipboardText = Clipboard.GetText().Trim();

                        // Chek if must prepend "https://"
                        if (this.prefixWithhttpsToolStripMenuItem.Checked && !Uri.IsWellFormedUriString(clipboardText, UriKind.Absolute))
                        {
                            // Add prefix to clipboard text
                            clipboardText = $"https://{clipboardText}";
                        }

                        // Check for a valid URI
                        if (Uri.TryCreate(clipboardText, UriKind.Absolute, out var uri) &&
                            (uri.Scheme == Uri.UriSchemeHttps ||
                            uri.Scheme == Uri.UriSchemeHttp ||
                            uri.Scheme == Uri.UriSchemeFtp ||
                            uri.Scheme == Uri.UriSchemeMailto ||
                            uri.Scheme == Uri.UriSchemeFile ||
                            uri.Scheme == Uri.UriSchemeNews ||
                            uri.Scheme == Uri.UriSchemeNntp ||
                            uri.Scheme == Uri.UriSchemeGopher ||
                            uri.Scheme == Uri.UriSchemeNetTcp ||
                            uri.Scheme == Uri.UriSchemeNetPipe))
                        {
                            // Check for duplicates
                            if (!this.urlCheckedListBox.Items.Contains(clipboardText))
                            {
                                // Add to valid list
                                this.urlCheckedListBox.Items.Add(clipboardText);

                                // Increment copy count
                                this.urlCount++;
                            }
                        }
                    }

                    // Halt flow
                    break;

                // Continue processing
                default:

                    // Pass message
                    base.WndProc(ref m);

                    // Halt flow
                    break;
            }
        }

        /// <summary>
        /// Adds the clipboard format listener.
        /// </summary>
        /// <returns><c>true</c>, if clipboard format listener was added, <c>false</c> otherwise.</returns>
        /// <param name="hwnd">The handle.</param>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AddClipboardFormatListener(IntPtr hwnd);

        /// <summary>
        /// Removes the clipboard format listener.
        /// </summary>
        /// <returns><c>true</c>, if clipboard format listener was removed, <c>false</c> otherwise.</returns>
        /// <param name="hwnd">The handle.</param>c
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

        /// <summary>
        /// Handles the pause/resume button click event.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnPauseResumeButtonClick(object sender, EventArgs e)
        {
            // Check if must pause
            if (this.pauseResumeButton.Text.StartsWith("&P", StringComparison.InvariantCulture))
            {
                // Remove clipboard listener
                RemoveClipboardFormatListener(this.Handle);

                // Update monitor status
                this.monitorGroupBox.Text = "Monitor is: INACTIVE";

                // Set button text
                this.pauseResumeButton.Text = "&Resume";
            }
            else
            {
                // Add clipboard listener
                AddClipboardFormatListener(this.Handle);

                // Update monitor status
                this.monitorGroupBox.Text = "Monitor is: ACTIVE";

                // Set button text
                this.pauseResumeButton.Text = "&Pause";
            }
        }

        /// <summary>
        /// Processes the run at startup registry action.
        /// </summary>
        private void ProcessRunAtStartupRegistry()
        {
            // Open registry key
            using (RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
            {
                // Check for run at startup in settings data
                if (this.settingsData.RunAtStartup)
                {
                    // Add app value
                    registryKey.SetValue(Application.ProductName, $"\"{Application.ExecutablePath}\" /autostart");
                }
                else
                {
                    // Check for app value
                    if (registryKey.GetValue(Application.ProductName) != null)
                    {
                        // Erase app value
                        registryKey.DeleteValue(Application.ProductName, false);
                    }
                }
            }
        }

        /// <summary>
        /// Handles the delete checked button click event.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnDeleteCheckedButtonClick(object sender, EventArgs e)
        {
            // Prevent drawing
            this.urlCheckedListBox.BeginUpdate();

            // Process until there are no checked items
            while (this.urlCheckedListBox.CheckedItems.Count > 0)
            {
                // Remove the first checked one
                this.urlCheckedListBox.Items.RemoveAt(this.urlCheckedListBox.CheckedIndices[0]);
            }

            // Restore drawing
            this.urlCheckedListBox.EndUpdate();
        }

        /// <summary>
        /// Handles the clear button click event.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnClearButtonClick(object sender, EventArgs e)
        {
            // Ask user
            if (this.urlCheckedListBox.Items.Count > 0 && MessageBox.Show($"Would you like to clear {this.urlCheckedListBox.Items.Count} items?", "Clear", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) == DialogResult.Yes)
            {
                // Clear all items
                this.urlCheckedListBox.Items.Clear();
            }
        }

        /// <summary>
        /// Handles the browse button click event.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnBrowseButtonClick(object sender, EventArgs e)
        {
            // Set file name
            this.saveHtmlFileDialog.FileName = this.saveFileTextBox.Text;

            // Open save file dialog
            if (this.saveHtmlFileDialog.ShowDialog() == DialogResult.OK && this.saveHtmlFileDialog.FileName.Length > 0)
            {
                // Set file in text box
                this.saveFileTextBox.Text = this.saveHtmlFileDialog.FileName;
            }
        }

        /// <summary>
        /// Handles the open button click event.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnOpenButtonClick(object sender, EventArgs e)
        {
            // Check for items to work with
            if (this.urlCheckedListBox.Items.Count == 0)
            {
                // Advise user
                MessageBox.Show("No items to work with!", "Empty list", MessageBoxButtons.OK, MessageBoxIcon.Stop);

                // Halt flow
                return;
            }

            // Save HTML file
            this.SaveUrlHtmlFile(this.saveFileTextBox.Text);

            // Open HTML file in browser
            Process.Start(this.GetBrowserPath(), (new Uri(Path.GetFullPath(this.saveFileTextBox.Text))).ToString());
        }

        /// <summary>
        /// Gets the browser path.
        /// </summary>
        /// <returns>The browser path.</returns>
        private string GetBrowserPath()
        {
            // Declare sub key path
            string subKeyPath = string.Empty;

            // Declare browser path
            string browserPath = "iexplore.exe";

            // Set sub key path
            using (RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\http\UserChoice", false))
            {
                // Check for user choice
                if (registryKey != null)
                {
                    // Set sub key path by ProgID
                    subKeyPath = registryKey.GetValue("ProgId").ToString() + @"\shell\open\command";
                }
                else
                {
                    // Set sub key path by http command
                    subKeyPath = @"\http\shell\open\command";
                }
            }

            // Set browser path
            using (RegistryKey registryKey = Registry.ClassesRoot.OpenSubKey(subKeyPath, false))
            {
                // Set regex match
                Match match = new Regex("(?<=\").*?(?=\")").Match(registryKey.GetValue(string.Empty).ToString());

                // Check for success
                if (match.Success)
                {
                    // Set browser path
                    browserPath = match.Value;
                }
            }

            // Return browser path
            return browserPath;
        }

        /// <summary>
        /// Handles the new tool strip menu item click event.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnNewToolStripMenuItemClick(object sender, EventArgs e)
        {
            // TODO Add code
        }

        /// <summary>
        /// Opens the tool strip menu item click event.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OpenToolStripMenuItemClick(object sender, EventArgs e)
        {
            // TODO Add code
        }

        /// <summary>
        /// Handles the save tool strip menu item click event.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnSaveToolStripMenuItemClick(object sender, EventArgs e)
        {
            // TODO Add code
        }

        /// <summary>
        /// Handles the minimize tool strip menu item click event.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnMinimizeToolStripMenuItemClick(object sender, EventArgs e)
        {
            // Minimize program window
            this.WindowState = FormWindowState.Minimized;
        }

        /// <summary>
        /// Handles the text tool strip menu item click event.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnTextToolStripMenuItemClick(object sender, EventArgs e)
        {
            // TODO Add code
        }

        /// <summary>
        /// Handles the HTML Tool strip menu item click event.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnHTMLToolStripMenuItemClick(object sender, EventArgs e)
        {
            // TODO Add code
        }

        /// <summary>
        /// Handles the always on top tool strip menu item click event.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnAlwaysOnTopToolStripMenuItemClick(object sender, EventArgs e)
        {
            // Toggle check state
            this.alwaysOnTopToolStripMenuItem.Checked = !this.alwaysOnTopToolStripMenuItem.Checked;

            // Set topmost state
            this.TopMost = this.alwaysOnTopToolStripMenuItem.Checked;

            // Save setting
            this.settingsData.AlwaysOnTop = this.TopMost;
        }

        /// <summary>
        /// Handles the run at startup tool strip menu item click event.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnRunAtStartupToolStripMenuItemClick(object sender, EventArgs e)
        {
            // Toggle check state
            this.runAtStartupToolStripMenuItem.Checked = !this.runAtStartupToolStripMenuItem.Checked;

            // Save setting
            this.settingsData.RunAtStartup = this.runAtStartupToolStripMenuItem.Checked;
        }

        /// <summary>
        /// Handles the start in tray tool strip menu item click event.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnStartInTrayToolStripMenuItemClick(object sender, EventArgs e)
        {
            // Toggle check state
            this.startInTrayToolStripMenuItem.Checked = !this.startInTrayToolStripMenuItem.Checked;

            // Save setting
            this.settingsData.RunAtStartup = this.startInTrayToolStripMenuItem.Checked;
        }

        /// <summary>
        /// Handles the hide close button tool strip menu item click event.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnHideCloseButtonToolStripMenuItemClick(object sender, EventArgs e)
        {
            // Toggle hide close button check box
            this.hideCloseButtonToolStripMenuItem.Checked = !this.hideCloseButtonToolStripMenuItem.Checked;

            // Set form's control box visibility
            this.ControlBox = !this.hideCloseButtonToolStripMenuItem.Checked;

            // Set control box visibility on settings data
            this.settingsData.HideCloseButton = this.hideCloseButtonToolStripMenuItem.Checked;
        }

        /// <summary>
        /// Handles the prefix https tool strip menu item click.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnPrefixWithhttpsToolStripMenuItemClick(object sender, EventArgs e)
        {
            // Toggle check state
            this.prefixWithhttpsToolStripMenuItem.Checked = !this.prefixWithhttpsToolStripMenuItem.Checked;
        }

        /// <summary>
        /// Handles the save tool strip menu item drop down item clicked event.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnSaveToolStripMenuItemDropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            // TODO Add code
        }

        /// <summary>
        /// Handles the exit tool strip menu item click event.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnExitToolStripMenuItemClick(object sender, EventArgs e)
        {
            // Close application
            this.Close();
        }

        /// <summary>
        /// Handles the headquarters patreoncom tool strip menu item click event.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnHeadquartersPatreoncomToolStripMenuItemClick(object sender, EventArgs e)
        {
            // Open Patreon headquarters
            Process.Start("https://www.patreon.com/publicdomain");
        }

        /// <summary>
        /// Handles the source code githubcom tool strip menu item click event.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnSourceCodeGithubcomToolStripMenuItemClick(object sender, EventArgs e)
        {
            // Open GitHub
            Process.Start("https://github.com/publicdomain");
        }

        /// <summary>
        /// Handles the main form form closing event.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnMainFormFormClosing(object sender, FormClosingEventArgs e)
        {
            // Remove clipboard listener
            RemoveClipboardFormatListener(this.Handle);
        }

        /// <summary>
        /// Handles the main form shown event.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnMainFormShown(object sender, EventArgs e)
        {
            // TODO Add code
        }

        /// <summary>
        /// Handles the main form resize event.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnMainFormResize(object sender, EventArgs e)
        {
            // Check for minimized state
            if (this.WindowState == FormWindowState.Minimized)
            {
                // Send to the system tray
                this.SendToSystemTray();
            }
        }

        /// <summary>
        /// Saves the URL html file.
        /// </summary>
        /// <param name="filePath">File path.</param>
        private void SaveUrlHtmlFile(string filePath)
        {
            // TODO Check there's something to work with [Checks can be made before / improved]
            if (this.urlCheckedListBox.Items.Count == 0)
            {
                // Halt flow
                return;
            }

            /* Set html string */

            // Top
            string html =
                $"<html>{Environment.NewLine}" +
                $"\t<head>{Environment.NewLine}" +
                $"\t\t<title>Clipboard URLs @ {DateTime.Now}</title>{Environment.NewLine}" +
                $"\t<head>{Environment.NewLine}" +
                $"\t<body>{Environment.NewLine}" +
                $"\t\t<h1>Clipboard URLs @ {DateTime.Now}</h1>{Environment.NewLine}" +
                $"\t\t<ul>{Environment.NewLine}";

            // URLs
            foreach (string url in this.urlCheckedListBox.Items)
            {
                // Append link
                html += $"\t\t\t<li><a href=\"{url}\" target=\"_blank\">{url}</a></li>{Environment.NewLine}";
            }

            // Bottom
            html +=
                $"\t\t<ul>{Environment.NewLine}" +
                $"\t</body>{Environment.NewLine}" +
                $"</html>";

            // Save to disk
            File.WriteAllText(filePath, html);
        }

        /// <summary>
        /// Sends the program to the system tray.
        /// </summary>
        private void SendToSystemTray()
        {
            // Hide main form
            this.Hide();

            // Show notify icon 
            this.mainNotifyIcon.Visible = true;
        }

        /// <summary>
        /// Restores the window back from system tray to the foreground.
        /// </summary>
        private void RestoreFromSystemTray()
        {
            // Make form visible again
            this.Show();

            // Return window back to normal
            this.WindowState = FormWindowState.Normal;

            // Make it topmost
            this.TopMost = true;

            // Bring to the front of the Z-order
            this.BringToFront();

            // Reset topmost
            this.TopMost = false;

            // Hide system tray icon
            this.mainNotifyIcon.Visible = false;
        }

        /// <summary>
        /// Handles the show tool strip menu item click event.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnShowToolStripMenuItemClick(object sender, EventArgs e)
        {
            // TODO Add code
        }

        /// <summary>
        /// Handles the main notify icon mouse click event.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnMainNotifyIconMouseClick(object sender, MouseEventArgs e)
        {
            // Check for left click
            if (e.Button == MouseButtons.Left)
            {
                // Restore window 
                this.RestoreFromSystemTray();
            }
        }

        /// <summary>
        /// Saves the settings data.
        /// </summary>
        private void SaveSettingsData()
        {
            // Use stream writer
            using (StreamWriter streamWriter = new StreamWriter("SettingsData.txt", false))
            {
                // Set xml serialzer
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(SettingsData));

                // Serialize settings data
                xmlSerializer.Serialize(streamWriter, this.settingsData);
            }
        }

        /// <summary>
        /// Loads the settings data.
        /// </summary>
        /// <returns>The settings data.</returns>ing
        private SettingsData LoadSettingsData()
        {
            // Use file stream
            using (FileStream fileStream = File.OpenRead("SettingsData.txt"))
            {
                // Set xml serialzer
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(SettingsData));

                // Return populated settings data
                return xmlSerializer.Deserialize(fileStream) as SettingsData;
            }
        }
    }
}
