using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace MusicControl
{
    public class ModConfig
    {
        public KeybindList ToggleKey { get; set; } = new KeybindList(SButton.OemPipe);
        public KeybindList VolumeUpKey { get; set; } = new KeybindList(SButton.OemCloseBrackets);
        public KeybindList VolumeDownKey { get; set; } = new KeybindList(SButton.OemOpenBrackets);
        public float VolumeLevel { get; set; } = 0.15f;
        public bool AutoEventMusic { get; set; } = false;
        public bool ShowSongNames { get; set; } = true;
        public int SongDisplayDuration { get; set; } = 3;
        public bool ShowOnEventStart { get; set; } = true;
        public bool ShowVolumeChange { get; set; } = true;
    }
}
