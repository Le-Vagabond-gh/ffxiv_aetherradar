using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using System;
using System.Numerics;

namespace aetherradar
{
    public class IconPickerWindow : Window, IDisposable
    {
        private readonly Configuration Configuration;
        private readonly Plugin Plugin;
        private const int IconsPerRow = 10;
        private const float IconSize = 32f;
        private const float IconSpacing = 4f;

        // Range of map marker icons to display
        private const uint IconStart = 60400;
        private const uint IconEnd = 60600;

        public IconPickerWindow(Configuration configuration, Plugin plugin) : base(
            "Select Map Marker Icon",
            ImGuiWindowFlags.NoCollapse)
        {
            this.Size = new Vector2(460, 500);
            this.SizeCondition = ImGuiCond.FirstUseEver;
            this.Configuration = configuration;
            this.Plugin = plugin;
        }

        public void Dispose() { }

        public override void Draw()
        {
            ImGui.Text($"Current Icon: {Configuration.MapMarkerIconId}");
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            ImGui.BeginChild("IconGrid", new Vector2(0, 0), false, ImGuiWindowFlags.AlwaysVerticalScrollbar);

            int col = 0;
            for (uint iconId = IconStart; iconId <= IconEnd; iconId++)
            {
                try
                {
                    var icon = Service.TextureProvider.GetFromGameIcon(new GameIconLookup(iconId));
                    if (!icon.TryGetWrap(out var texture, out _) || texture == null)
                        continue;

                    ImGui.PushID((int)iconId);

                    bool isSelected = Configuration.MapMarkerIconId == iconId;
                    if (isSelected)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.6f, 0.2f, 1.0f));
                    }

                    if (ImGui.ImageButton(texture.Handle, new Vector2(IconSize, IconSize)))
                    {
                        Configuration.MapMarkerIconId = iconId;
                        Configuration.Save();
                        Plugin.RefreshMapMarkers();
                    }

                    if (isSelected)
                    {
                        ImGui.PopStyleColor();
                    }

                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip($"Icon ID: {iconId}");
                    }

                    ImGui.PopID();

                    col++;
                    if (col < IconsPerRow)
                    {
                        ImGui.SameLine(0, IconSpacing);
                    }
                    else
                    {
                        col = 0;
                    }
                }
                catch
                {
                    // Icon doesn't exist, skip it
                }
            }

            ImGui.EndChild();
        }
    }
}
