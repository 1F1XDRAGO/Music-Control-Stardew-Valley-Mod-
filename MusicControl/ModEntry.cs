using System;
using System.Globalization;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace MusicControl
{
    public class ModEntry : Mod
    {
        private IGenericModConfigMenuApi configMenu;
        private ModConfig config;
        private bool eventOn = false;
        private bool eventVolumeSwitched = false;
        private string lastDisplayedSong = "";
        private bool isFirstLaunch = true;
        private int volumeNotificationTimer = 0;
        private string volumeNotification = "";

        public override void Entry(IModHelper helper)
        {
            this.config = helper.ReadConfig<ModConfig>();
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
            helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
            helper.Events.GameLoop.OneSecondUpdateTicked += this.OnOneSecondUpdateTicked;
            helper.Events.Display.RenderedHud += this.OnRenderedHud;
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>(
                "spacechase0.GenericModConfigMenu"
            );
            if (configMenu == null)
                return;
            configMenu.Register(
                mod: ModManifest,
                reset: () => config = new ModConfig(),
                save: () => Helper.WriteConfig(config)
            );
            configMenu.AddKeybindList(
                mod: ModManifest,
                name: () => Helper.Translation.Get("toggleMusicHotkey.name"),
                getValue: () => config.ToggleKey,
                setValue: value => config.ToggleKey = value,
                tooltip: () => Helper.Translation.Get("toggleMusicHotkey.tooltip")
            );
            configMenu.AddKeybindList(
                mod: ModManifest,
                name: () => Helper.Translation.Get("volumeUpKey.name"),
                getValue: () => config.VolumeUpKey,
                setValue: value => config.VolumeUpKey = value,
                tooltip: () => Helper.Translation.Get("volumeUpKey.tooltip")
            );
            configMenu.AddKeybindList(
                mod: ModManifest,
                name: () => Helper.Translation.Get("volumeDownKey.name"),
                getValue: () => config.VolumeDownKey,
                setValue: value => config.VolumeDownKey = value,
                tooltip: () => Helper.Translation.Get("volumeDownKey.tooltip")
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => Helper.Translation.Get("musicVolume.name"),
                getValue: () => config.VolumeLevel,
                setValue: value => config.VolumeLevel = value,
                min: 0f,
                max: 1f,
                interval: 0.05f,
                formatValue: value => $"{value:P0}",
                tooltip: () => Helper.Translation.Get("musicVolume.tooltip")
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => Helper.Translation.Get("showVolumeChange.name"),
                getValue: () => config.ShowVolumeChange,
                setValue: value => config.ShowVolumeChange = value,
                tooltip: () => Helper.Translation.Get("showVolumeChange.tooltip")
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => Helper.Translation.Get("autoEventMusic.name"),
                getValue: () => config.AutoEventMusic,
                setValue: value => config.AutoEventMusic = value,
                tooltip: () => Helper.Translation.Get("autoEventMusic.tooltip")
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => Helper.Translation.Get("showSongNames.name"),
                getValue: () => config.ShowSongNames,
                setValue: value => config.ShowSongNames = value,
                tooltip: () => Helper.Translation.Get("showSongNames.tooltip")
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => Helper.Translation.Get("showOnEventStart.name"),
                getValue: () => config.ShowOnEventStart,
                setValue: value => config.ShowOnEventStart = value,
                tooltip: () => Helper.Translation.Get("showOnEventStart.tooltip")
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => Helper.Translation.Get("songDisplayDuration.name"),
                getValue: () => config.SongDisplayDuration,
                setValue: value => config.SongDisplayDuration = value,
                min: 1,
                max: 10,
                interval: 1,
                formatValue: value => $"{value} seconds",
                tooltip: () => Helper.Translation.Get("songDisplayDuration.tooltip")
            );
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;
            if (config.ToggleKey.JustPressed())
            {
                if (Game1.options.musicVolumeLevel > 0f)
                {
                    Game1.options.musicVolumeLevel = 0f;
                    Game1.musicCategory.SetVolume(0f);
                    Game1.addHUDMessage(new HUDMessage("Music Disabled", 2));
                }
                else
                {
                    Game1.options.musicVolumeLevel = config.VolumeLevel;
                    Game1.musicCategory.SetVolume(config.VolumeLevel);
                    Game1.addHUDMessage(new HUDMessage("Music Enable", 2));
                    if (config.ShowSongNames && Game1.currentSong != null)
                    {
                        DisplayCurrentSong();
                    }
                }
            }
            else if (config.VolumeUpKey.JustPressed())
            {
                AdjustVolume(0.05f);
            }
            else if (config.VolumeDownKey.JustPressed())
            {
                AdjustVolume(-0.05f);
            }
        }

        private void AdjustVolume(float change)
        {
            float newVolume = config.VolumeLevel + change;
            newVolume = Math.Max(0f, Math.Min(newVolume, 1f));
            newVolume = (float)Math.Round(newVolume * 20) / 20;
            if (Math.Abs(newVolume - config.VolumeLevel) < 0.01f)
                return;
            config.VolumeLevel = newVolume;
            Helper.WriteConfig(config);
            if (Game1.options.musicVolumeLevel > 0f && !Game1.eventUp)
            {
                Game1.options.musicVolumeLevel = config.VolumeLevel;
                Game1.musicCategory.SetVolume(config.VolumeLevel);
            }
            if (config.ShowVolumeChange)
            {
                volumeNotification = $"Volume: {config.VolumeLevel * 100:0}%";
                volumeNotificationTimer = 150;
            }
        }

        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;
            if (volumeNotificationTimer > 0)
            {
                volumeNotificationTimer--;
            }
            if (!eventOn && Game1.eventUp)
            {
                if (config.AutoEventMusic && Game1.options.musicVolumeLevel <= 0f)
                {
                    Game1.options.musicVolumeLevel = config.VolumeLevel;
                    Game1.musicCategory.SetVolume(config.VolumeLevel);
                    eventVolumeSwitched = true;
                    if (
                        config.ShowSongNames
                        && config.ShowOnEventStart
                        && Game1.currentSong != null
                    )
                    {
                        DisplayCurrentSong();
                    }
                }
            }
            else if (eventOn && !Game1.eventUp && eventVolumeSwitched)
            {
                Game1.options.musicVolumeLevel = 0f;
                Game1.musicCategory.SetVolume(0f);
                eventVolumeSwitched = false;
            }
            eventOn = Game1.eventUp;
        }

        private void OnOneSecondUpdateTicked(object sender, OneSecondUpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady || !config.ShowSongNames)
                return;
            if (
                Game1.currentSong != null
                && Game1.currentSong.IsPlaying
                && Game1.options.musicVolumeLevel > 0f
            )
            {
                string currentSongName = FormatSongName(Game1.currentSong.Name);
                if (
                    currentSongName != lastDisplayedSong
                    && (isFirstLaunch || config.ShowOnEventStart || !Game1.eventUp)
                )
                {
                    DisplayCurrentSong();
                    lastDisplayedSong = currentSongName;
                }
            }
            isFirstLaunch = false;
        }

        private void DisplayCurrentSong()
        {
            try
            {
                if (Game1.currentSong == null)
                    return;
                string songName = FormatSongName(Game1.currentSong.Name);
                string messageText = $"Now Playing: {songName}";
                HUDMessage message = new HUDMessage(messageText, 3)
                {
                    timeLeft = config.SongDisplayDuration * 1000,
                };
                Game1.addHUDMessage(message);
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error displaying song name: {ex.Message}", LogLevel.Error);
            }
        }

        private string FormatSongName(string cueName)
        {
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(
                cueName
                    .Replace('_', ' ')
                    .Replace("spring", "Spring")
                    .Replace("summer", "Summer")
                    .Replace("fall", "Fall")
                    .Replace("winter", "Winter")
                    .Replace("night", "Night")
                    .Replace("day", "Day")
            );
        }

        private void OnRenderedHud(object sender, RenderedHudEventArgs e)
        {
            if (volumeNotificationTimer > 0)
            {
                int x =
                    Game1.uiViewport.Width / 2
                    - (int)Game1.dialogueFont.MeasureString(volumeNotification).X / 2;
                int y = Game1.uiViewport.Height - 100;
                IClickableMenu.drawHoverText(
                    e.SpriteBatch,
                    volumeNotification,
                    Game1.dialogueFont,
                    x,
                    y
                );
            }
        }
    }
}
