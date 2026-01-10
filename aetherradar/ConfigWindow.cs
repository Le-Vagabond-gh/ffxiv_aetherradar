using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using System;
using System.Numerics;

namespace aetherradar
{
    public class ConfigWindow : Window, IDisposable
    {
        private Configuration Configuration;

        public ConfigWindow(Plugin plugin) : base(
            "Aether Radar Settings",
            ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
            ImGuiWindowFlags.NoScrollWithMouse)
        {
            this.Size = new Vector2(300, 430);
            this.SizeCondition = ImGuiCond.Always;
            this.Configuration = plugin.Configuration;
        }

        public void Dispose() { }

        public override void Draw()
        {
            var enabled = this.Configuration.Enabled;
            if (ImGui.Checkbox("Enable Overlay", ref enabled))
            {
                this.Configuration.Enabled = enabled;
                this.Configuration.Save();
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1.0f), "Display Options");
            ImGui.Spacing();

            var showList = this.Configuration.ShowList;
            if (ImGui.Checkbox("Show List Window", ref showList))
            {
                this.Configuration.ShowList = showList;
                this.Configuration.Save();
            }

            var showMarkers = this.Configuration.ShowMarkers;
            if (ImGui.Checkbox("Show Screen Markers", ref showMarkers))
            {
                this.Configuration.ShowMarkers = showMarkers;
                this.Configuration.Save();
            }

            var showOffscreen = this.Configuration.ShowOffscreenIndicators;
            if (ImGui.Checkbox("Show Off-screen Indicators", ref showOffscreen))
            {
                this.Configuration.ShowOffscreenIndicators = showOffscreen;
                this.Configuration.Save();
            }

            var lockPosition = this.Configuration.LockListPosition;
            if (ImGui.Checkbox("Lock List Position", ref lockPosition))
            {
                this.Configuration.LockListPosition = lockPosition;
                this.Configuration.Save();
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1.0f), "Info Display");
            ImGui.Spacing();

            var showDistance = this.Configuration.ShowDistance;
            if (ImGui.Checkbox("Show Distance", ref showDistance))
            {
                this.Configuration.ShowDistance = showDistance;
                this.Configuration.Save();
            }

            var showDirection = this.Configuration.ShowDirection;
            if (ImGui.Checkbox("Show Direction", ref showDirection))
            {
                this.Configuration.ShowDirection = showDirection;
                this.Configuration.Save();
            }

            var showMapCoords = this.Configuration.ShowMapCoords;
            if (ImGui.Checkbox("Show Map Coordinates", ref showMapCoords))
            {
                this.Configuration.ShowMapCoords = showMapCoords;
                this.Configuration.Save();
            }

            ImGui.Spacing();

            var unlimited = this.Configuration.MaxDisplayDistance <= 0;
            if (ImGui.Checkbox("Unlimited Distance", ref unlimited))
            {
                this.Configuration.MaxDisplayDistance = unlimited ? 0f : 200f;
                this.Configuration.Save();
            }

            if (!unlimited)
            {
                var maxDistance = this.Configuration.MaxDisplayDistance;
                if (ImGui.SliderFloat("Max Distance", ref maxDistance, 50f, 500f, "%.0f"))
                {
                    this.Configuration.MaxDisplayDistance = maxDistance;
                    this.Configuration.Save();
                }
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1.0f), "Debug");
            ImGui.Spacing();

            var showCollected = this.Configuration.ShowCollected;
            if (ImGui.Checkbox("Show Collected Currents", ref showCollected))
            {
                this.Configuration.ShowCollected = showCollected;
                this.Configuration.Save();
            }

            var debug = this.Configuration.DebugLogging;
            if (ImGui.Checkbox("Debug Logging", ref debug))
            {
                this.Configuration.DebugLogging = debug;
                this.Configuration.Save();
            }
        }
    }
}
