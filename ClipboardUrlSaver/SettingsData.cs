// <copyright file="SettingsData.cs" company="PublicDomain.com">
//     CC0 1.0 Universal (CC0 1.0) - Public Domain Dedication
//     https://creativecommons.org/publicdomain/zero/1.0/legalcode
// </copyright>
using System.Drawing;

namespace ClipboardUrlSaver
{
    // Directives
    using System;

    /// <summary>
    /// Settings data.
    /// </summary>
    public class SettingsData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:ClipboardUrlSaver.SettingsData"/> class.
        /// </summary>
        public SettingsData()
        {
            // Parameterless constructor for serialization to work
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:ClipboardUrlSaver.SettingsData"/> is always on top.
        /// </summary>
        /// <value><c>true</c> if always on top; otherwise, <c>false</c>.</value>
        public bool AlwaysOnTop { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:MultilingualWordCounter.SettingsData"/> hides the close button.
        /// </summary>
        /// <value><c>true</c> if hide close button; otherwise, <c>false</c>.</value>
        public bool HideCloseButton { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:MultilingualWordCounter.SettingsData"/> runs at startup.
        /// </summary>
        /// <value><c>true</c> if run at startup; otherwise, <c>false</c>.</value>
        public bool RunAtStartup { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:ClipboardUrlSaver.SettingsData"/> starts in tray.
        /// </summary>
        /// <value><c>true</c> if start in tray; otherwise, <c>false</c>.</value>
        public bool StartInTray { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether <see cref="T:ClipboardUrlSaver.SettingsData"/> is prefixed.
        /// </summary>
        /// <value><c>true</c> if prefix; otherwise, <c>false</c>.</value>
        public bool Prefix { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:ClipboardUrlSaver.SettingsData"/> keeps URL list between runs.
        /// </summary>
        /// <value><c>true</c> if keep list; otherwise, <c>false</c>.</value>
        public bool KeepList { get; set; } = true;

        /// <summary>
        /// Gets or sets the size of the window.
        /// </summary>
        /// <value>The size of the window.</value>
        public Size WindowSize { get; set; }

        /// <summary>
        /// Gets or sets the window location.
        /// </summary>
        /// <value>The window location.</value>
        public Size WindowLocation { get; set; }
    }
}
