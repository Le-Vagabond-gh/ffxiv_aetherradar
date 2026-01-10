using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace aetherradar
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;

        public bool Enabled { get; set; } = true;
        public bool ShowList { get; set; } = true;
        public bool ShowMarkers { get; set; } = true;
        public bool ShowOffscreenIndicators { get; set; } = true;
        public bool LockListPosition { get; set; } = false;
        public bool ShowDistance { get; set; } = true;
        public bool ShowDirection { get; set; } = true;
        public bool ShowMapCoords { get; set; } = false;
        public float MaxDisplayDistance { get; set; } = 200f;
        public bool ShowCollected { get; set; } = false;
        public bool DebugLogging { get; set; } = false;

        [NonSerialized]
        private IDalamudPluginInterface? PluginInterface;

        public void Initialize(IDalamudPluginInterface pluginInterface)
        {
            this.PluginInterface = pluginInterface;
        }

        public void Save()
        {
            this.PluginInterface!.SavePluginConfig(this);
        }
    }
}
