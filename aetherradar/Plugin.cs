using Dalamud.Game.Command;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Bindings.ImGui;
using Lumina.Excel.Sheets;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace aetherradar
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Aether Radar";
        private const string CommandName = "/aetherradar";

        private IDalamudPluginInterface PluginInterface { get; init; }
        private ICommandManager CommandManager { get; init; }
        public Configuration Configuration { get; init; }
        public WindowSystem WindowSystem = new("AetherRadar");
        private ConfigWindow ConfigWindow { get; init; }
        private IconPickerWindow IconPickerWindow { get; init; }

        // Aether current names in all supported languages
        private static readonly HashSet<string> AetherCurrentNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "aether current",   // English
            "風脈の泉",          // Japanese
            "windätherquelle",  // German
            "vent éthéré"       // French
        };

        // Cache of all aether currents by RowId for unlock checking
        private Dictionary<uint, AetherCurrent> aetherCurrentsById = new();
        // Cache EObj DataId -> AetherCurrent mapping (built from EObj sheet)
        private Dictionary<uint, AetherCurrent> aetherCurrentsByEObjId = new();

        // Map marker tracking - cached to avoid redundant lookups each frame
        private string cachedZoneName = "";
        private uint cachedMapId = 0;
        private List<AetherCurrentData.AetherCurrentLocation>? cachedCurrents = null;
        private float cachedSizeFactor = 0;
        private int cachedOffsetX = 0;
        private int cachedOffsetY = 0;

        // Cooldown after zone changes to let the game finish tearing down/rebuilding
        // the AreaMap addon's node tree. ResetMapMarkers during this window leaves stale
        // pointers that the game's next OnRequestedUpdate reads, causing an access violation.
        private int zoneChangeCooldown = 0;
        private const int ZoneChangeCooldownFrames = 120; // ~2 seconds at 60fps

        public Plugin(
            IDalamudPluginInterface pluginInterface,
            ICommandManager commandManager)
        {
            this.PluginInterface = pluginInterface;
            this.CommandManager = commandManager;
            this.PluginInterface.Create<Service>();

            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);

            // Build aether current lookup
            BuildAetherCurrentCache();

            ConfigWindow = new ConfigWindow(this);
            IconPickerWindow = new IconPickerWindow(this.Configuration, this);
            WindowSystem.AddWindow(ConfigWindow);
            WindowSystem.AddWindow(IconPickerWindow);

            this.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Open Aether Radar settings"
            });

            this.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
            this.PluginInterface.UiBuilder.Draw += DrawUI;

            // Hook for map markers - use AddonLifecycle to safely modify markers
            // after the game finishes its own AreaMap update, avoiding race conditions
            Service.AddonLifecycle.RegisterListener(AddonEvent.PostRequestedUpdate, "AreaMap", OnAreaMapPostUpdate);
            Service.ClientState.TerritoryChanged += OnTerritoryChanged;

            Service.PluginLog.Info("Aether Radar initialized");
        }

        private void OnTerritoryChanged(ushort territoryId)
        {
            cachedZoneName = "";
            cachedMapId = 0;
            cachedCurrents = null;
            zoneChangeCooldown = ZoneChangeCooldownFrames;
        }

        /// <summary>
        /// Called by AddonLifecycle after the game finishes its own AreaMap update.
        /// The game's OnRequestedUpdate resets markers each time it runs, so we must
        /// re-add ours after every update. This is safe because the game's update pass
        /// is already complete - no race condition with stale node pointers.
        /// </summary>
        private unsafe void OnAreaMapPostUpdate(AddonEvent type, AddonArgs args)
        {
            try
            {
                if (!Configuration.Enabled || !Configuration.ShowStaticMapMarkers)
                    return;

                // Cooldown after zone changes - the game is still tearing down and
                // rebuilding the AreaMap node tree. Calling ResetMapMarkers here would
                // leave stale pointers for the game's next OnRequestedUpdate.
                if (zoneChangeCooldown > 0)
                {
                    zoneChangeCooldown--;
                    return;
                }

                if (Service.ClientState.LocalPlayer == null)
                    return;

                // Skip during transitions, cutscenes, etc.
                if (Service.Condition[ConditionFlag.BetweenAreas] ||
                    Service.Condition[ConditionFlag.BetweenAreas51] ||
                    Service.Condition[ConditionFlag.WatchingCutscene] ||
                    Service.Condition[ConditionFlag.WatchingCutscene78] ||
                    Service.Condition[ConditionFlag.OccupiedInCutSceneEvent])
                {
                    // Reset cooldown if we hit a condition flag - the transition isn't done yet
                    zoneChangeCooldown = ZoneChangeCooldownFrames;
                    return;
                }

                var addon = (AtkUnitBase*)args.Addon.Address;
                if (addon == null || !addon->IsVisible)
                    return;

                var agentMap = AgentMap.Instance();
                if (agentMap == null)
                    return;

                // Refresh cached data when zone/map changes
                var territoryId = Service.ClientState.TerritoryType;
                var mapId = agentMap->CurrentMapId;
                if (mapId != cachedMapId || cachedCurrents == null)
                {
                    cachedMapId = mapId;
                    cachedZoneName = GetCurrentZoneName();
                    cachedCurrents = AetherCurrentData.GetFieldCurrents(cachedZoneName);

                    var territorySheet = Service.DataManager.GetExcelSheet<TerritoryType>();
                    var territory = territorySheet?.GetRow(territoryId);
                    if (territory != null)
                    {
                        var map = territory.Value.Map.Value;
                        cachedSizeFactor = map.SizeFactor / 100.0f;
                        cachedOffsetX = map.OffsetX;
                        cachedOffsetY = map.OffsetY;
                    }

                    if (Configuration.DebugLogging)
                        Service.PluginLog.Debug($"Cached map data for zone: {cachedZoneName}, mapId: {mapId}");
                }

                if (cachedCurrents == null || cachedCurrents.Count == 0)
                    return;

                // Reset and recreate game markers, then add ours on top.
                // This must happen after the game's OnRequestedUpdate (guaranteed by
                // PostRequestedUpdate timing) to avoid racing with the game's node reads.
                agentMap->ResetMapMarkers();
                agentMap->CreateMapMarkers(true);

                uint iconId = Configuration.MapMarkerIconId;
                var tooltipBytes = Encoding.UTF8.GetBytes("Aether Current\0");
                fixed (byte* tooltipPtr = tooltipBytes)
                {
                    foreach (var current in cachedCurrents)
                    {
                        var worldPos = MapToWorld(current.X, current.Y, cachedSizeFactor, cachedOffsetX, cachedOffsetY);
                        agentMap->AddMapMarker(worldPos, iconId, 0, tooltipPtr, 3, 0);
                    }
                }
            }
            catch (Exception ex)
            {
                if (Configuration.DebugLogging)
                    Service.PluginLog.Debug(ex, "Map marker update failed");
            }
        }

        private string GetCurrentZoneName()
        {
            try
            {
                var territoryId = Service.ClientState.TerritoryType;
                var territorySheet = Service.DataManager.GetExcelSheet<TerritoryType>();
                var territory = territorySheet?.GetRow(territoryId);

                if (territory != null)
                {
                    var placeName = territory.Value.PlaceName.Value;
                    return placeName.Name.ExtractText();
                }
            }
            catch (Exception ex)
            {
                if (Configuration.DebugLogging)
                    Service.PluginLog.Error(ex, "Failed to get zone name");
            }

            return "";
        }

        private Vector3 MapToWorld(float mapX, float mapY, float sizeFactor, int offsetX, int offsetY)
        {
            // Inverse of the WorldToMap formula:
            // mapX = (worldX - offsetX) * sizeFactor / 50.0f + 21.5f
            // Solving for worldX:
            // worldX = (mapX - 21.5f) * 50.0f / sizeFactor + offsetX
            var worldX = (mapX - 21.5f) * 50.0f / sizeFactor + offsetX;
            var worldZ = (mapY - 21.5f) * 50.0f / sizeFactor + offsetY;

            return new Vector3(worldX, 0, worldZ);
        }

        // Track which DataIds we've already logged (to avoid spam)
        private HashSet<uint> loggedDataIds = new();

        private void BuildAetherCurrentCache()
        {
            try
            {
                var sheet = Service.DataManager.GetExcelSheet<AetherCurrent>();
                if (sheet == null) return;

                foreach (var row in sheet)
                {
                    if (row.RowId == 0) continue;
                    aetherCurrentsById[row.RowId] = row;
                }

                // Try to build EObj -> AetherCurrent mapping
                var eobjSheet = Service.DataManager.GetExcelSheet<EObj>();
                if (eobjSheet != null)
                {
                    foreach (var eobj in eobjSheet)
                    {
                        // EObj has a Data field that might reference AetherCurrent
                        var dataRef = eobj.Data.RowId;
                        if (dataRef != 0 && aetherCurrentsById.ContainsKey(dataRef))
                        {
                            aetherCurrentsByEObjId[eobj.RowId] = aetherCurrentsById[dataRef];
                        }
                    }
                }

                Service.PluginLog.Info($"Cached {aetherCurrentsById.Count} aether currents, {aetherCurrentsByEObjId.Count} EObj mappings");
            }
            catch (Exception ex)
            {
                Service.PluginLog.Error(ex, "Failed to build aether current cache");
            }
        }

        private bool IsAetherCurrentUnlocked(uint dataId, bool debugLog = false)
        {
            AetherCurrent? current = null;
            string matchType = "";

            // Try direct RowId match first
            if (aetherCurrentsById.TryGetValue(dataId, out var byRowId))
            {
                current = byRowId;
                matchType = "RowId";
            }
            // Try EObj mapping
            else if (aetherCurrentsByEObjId.TryGetValue(dataId, out var byEObj))
            {
                current = byEObj;
                matchType = "EObj";
            }

            if (current.HasValue)
            {
                try
                {
                    var unlocked = Service.UnlockState.IsAetherCurrentUnlocked(current.Value);
                    if (debugLog && !loggedDataIds.Contains(dataId))
                    {
                        Service.PluginLog.Debug($"AetherCurrent DataId={dataId} matched via {matchType}, RowId={current.Value.RowId}, Unlocked={unlocked}");
                        loggedDataIds.Add(dataId);
                    }
                    return unlocked;
                }
                catch (Exception ex)
                {
                    if (debugLog)
                        Service.PluginLog.Error(ex, $"Error checking unlock state for DataId={dataId}");
                    return false;
                }
            }

            // No match found - log once per unique DataId
            if (debugLog && !loggedDataIds.Contains(dataId))
            {
                Service.PluginLog.Debug($"No AetherCurrent found for DataId={dataId} (not in RowId or EObj cache)");
                loggedDataIds.Add(dataId);
            }
            return false;
        }

        private void OnCommand(string command, string args)
        {
            ConfigWindow.IsOpen = true;
        }

        public void DrawConfigUI()
        {
            ConfigWindow.IsOpen = true;
        }

        public void OpenIconPicker()
        {
            IconPickerWindow.IsOpen = true;
        }

        public void RefreshMapMarkers()
        {
            // Invalidate cache so marker data is re-fetched on next update
            cachedCurrents = null;
            cachedMapId = 0;
        }

        public void DrawUI()
        {
            this.WindowSystem.Draw();

            if (!Configuration.Enabled)
                return;

            // Only draw overlay when logged in
            if (Service.ClientState.LocalPlayer == null)
                return;

            DrawAetherCurrentOverlay();
        }

        private void DrawAetherCurrentOverlay()
        {
            var playerPos = Service.ClientState.LocalPlayer!.Position;
            var aetherCurrents = new List<(string name, Vector3 pos, float distance, string direction, bool collected)>();

            // Scan for aether currents in the object table
            foreach (var obj in Service.ObjectTable)
            {
                if (obj == null)
                    continue;

                // Aether currents are EventObj type
                if (obj.ObjectKind != ObjectKind.EventObj)
                    continue;

                var name = obj.Name.TextValue;
                if (string.IsNullOrEmpty(name))
                    continue;

                if (!IsAetherCurrent(name))
                    continue;

                var objPos = obj.Position;
                var distance = Vector3.Distance(playerPos, objPos);

                // Skip if beyond max distance (0 or less means unlimited)
                if (Configuration.MaxDisplayDistance > 0 && distance > Configuration.MaxDisplayDistance)
                    continue;

                // Check if this specific current is collected
                var dataId = obj.DataId;
                bool isCollected = IsAetherCurrentUnlocked(dataId, Configuration.DebugLogging);

                // Skip collected unless ShowCollected is enabled
                if (isCollected && !Configuration.ShowCollected)
                    continue;

                var direction = GetCardinalDirection(playerPos, objPos);
                aetherCurrents.Add((name, objPos, distance, direction, isCollected));
            }

            if (aetherCurrents.Count == 0)
                return;

            // Draw list overlay window
            if (Configuration.ShowList)
            {
                var flags = ImGuiWindowFlags.AlwaysAutoResize |
                            ImGuiWindowFlags.NoSavedSettings |
                            ImGuiWindowFlags.NoFocusOnAppearing |
                            ImGuiWindowFlags.NoBringToFrontOnFocus |
                            ImGuiWindowFlags.NoCollapse;

                if (Configuration.LockListPosition)
                {
                    flags |= ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoInputs;
                }

                // Default position: top-right under minimap (roughly)
                var io = ImGui.GetIO();
                ImGui.SetNextWindowPos(new Vector2(io.DisplaySize.X - 220, 180), ImGuiCond.FirstUseEver);
                ImGui.SetNextWindowBgAlpha(0.7f);

                if (ImGui.Begin("Aether Currents Nearby", flags))
                {
                    foreach (var (name, pos, distance, direction, collected) in aetherCurrents)
                    {
                        var text = "";

                        if (Configuration.ShowDistance && Configuration.ShowDirection)
                            text = $"{distance:F0}y {direction}";
                        else if (Configuration.ShowDistance)
                            text = $"{distance:F0}y";
                        else if (Configuration.ShowDirection)
                            text = direction;

                        // Color based on distance and collection status
                        Vector4 color;
                        if (collected)
                            color = new Vector4(0.5f, 0.5f, 0.5f, 1.0f); // Gray - collected
                        else if (distance < 30)
                            color = new Vector4(0.3f, 1.0f, 0.3f, 1.0f); // Green - very close
                        else if (distance < 60)
                            color = new Vector4(1.0f, 1.0f, 0.3f, 1.0f); // Yellow - close
                        else
                            color = new Vector4(1.0f, 1.0f, 1.0f, 1.0f); // White - far

                        var prefix = collected ? "[x] " : "    ";
                        ImGui.TextColored(color, $"{prefix}{text}");

                        if (Configuration.ShowMapCoords)
                        {
                            var mapCoords = WorldToMap(pos);
                            ImGui.SameLine();
                            ImGui.TextDisabled($"({mapCoords.X:F1}, {mapCoords.Y:F1})");
                        }
                    }

                    ImGui.End();
                }
            }

            // Draw screen markers for each aether current
            if (Configuration.ShowMarkers || Configuration.ShowOffscreenIndicators)
            {
                foreach (var (name, pos, distance, direction, collected) in aetherCurrents)
                {
                    DrawScreenMarker(pos, distance, collected, playerPos);
                }
            }
        }

        private void DrawScreenMarker(Vector3 worldPos, float distance, bool collected, Vector3 playerPos)
        {
            var drawList = ImGui.GetBackgroundDrawList();
            var io = ImGui.GetIO();
            var screenCenter = new Vector2(io.DisplaySize.X / 2, io.DisplaySize.Y / 2);

            // Get marker color based on distance and collection status
            uint color;
            if (collected)
                color = 0xFF808080; // Gray (ABGR format)
            else if (distance < 30)
                color = 0xFF00FF00; // Green
            else if (distance < 60)
                color = 0xFF00FFFF; // Yellow
            else
                color = 0xFFFFFFFF; // White

            // Convert world position to screen position
            bool onScreen = Service.GameGui.WorldToScreen(worldPos, out var screenPos);

            // Check if on screen (with some margin)
            float margin = 50f;
            bool inScreenBounds = onScreen &&
                screenPos.X >= margin && screenPos.X <= io.DisplaySize.X - margin &&
                screenPos.Y >= margin && screenPos.Y <= io.DisplaySize.Y - margin;

            if (inScreenBounds && Configuration.ShowMarkers)
            {
                // Draw on-screen marker
                var radius = Math.Max(8f, 20f - (distance / 20f));
                drawList.AddCircleFilled(screenPos, radius, color);
                drawList.AddCircle(screenPos, radius + 2, 0xFF000000, 0, 2f); // Black outline

                // Draw distance text
                if (Configuration.ShowDistance)
                {
                    var text = $"{distance:F0}y";
                    var textSize = ImGui.CalcTextSize(text);
                    var textPos = new Vector2(screenPos.X - textSize.X / 2, screenPos.Y + radius + 4);
                    drawList.AddText(textPos, 0xFFFFFFFF, text);
                }
            }
            else if (Configuration.ShowOffscreenIndicators)
            {
                // Draw off-screen indicator at edge of screen
                DrawOffscreenIndicator(drawList, worldPos, playerPos, color, distance, io.DisplaySize);
            }
        }

        private void DrawOffscreenIndicator(ImDrawListPtr drawList, Vector3 worldPos, Vector3 playerPos, uint color, float distance, Vector2 screenSize)
        {
            // Calculate direction from player to target in world space
            var dirX = worldPos.X - playerPos.X;
            var dirZ = worldPos.Z - playerPos.Z;

            // Get camera forward direction (we'll use a simplified approach based on player facing)
            // Since we don't have direct camera access, use the screen projection approach
            var screenCenter = new Vector2(screenSize.X / 2, screenSize.Y / 2);

            // Try to get a point along the direction to determine screen-space direction
            var nearPoint = playerPos + new Vector3(dirX, 0, dirZ) * 0.1f;
            var farPoint = playerPos + new Vector3(dirX, 0, dirZ) * 100f;

            Service.GameGui.WorldToScreen(nearPoint, out var nearScreen);
            Service.GameGui.WorldToScreen(farPoint, out var farScreen);

            // Calculate screen direction
            var screenDir = farScreen - screenCenter;
            var screenDirLen = MathF.Sqrt(screenDir.X * screenDir.X + screenDir.Y * screenDir.Y);
            if (screenDirLen < 0.001f)
                return;

            screenDir /= screenDirLen;

            // Find edge position
            float edgeMargin = 40f;
            var edgePos = screenCenter;

            // Calculate intersection with screen edges
            float tX = screenDir.X > 0 ? (screenSize.X - edgeMargin - screenCenter.X) / screenDir.X
                                        : (-screenCenter.X + edgeMargin) / screenDir.X;
            float tY = screenDir.Y > 0 ? (screenSize.Y - edgeMargin - screenCenter.Y) / screenDir.Y
                                        : (-screenCenter.Y + edgeMargin) / screenDir.Y;

            float t = MathF.Min(MathF.Abs(tX), MathF.Abs(tY));
            edgePos = screenCenter + screenDir * t;

            // Clamp to screen bounds
            edgePos.X = Math.Clamp(edgePos.X, edgeMargin, screenSize.X - edgeMargin);
            edgePos.Y = Math.Clamp(edgePos.Y, edgeMargin, screenSize.Y - edgeMargin);

            // Draw arrow pointing in direction
            float arrowSize = 15f;
            float angle = MathF.Atan2(screenDir.Y, screenDir.X);

            var tip = edgePos;
            var left = edgePos - new Vector2(
                MathF.Cos(angle - 2.5f) * arrowSize,
                MathF.Sin(angle - 2.5f) * arrowSize);
            var right = edgePos - new Vector2(
                MathF.Cos(angle + 2.5f) * arrowSize,
                MathF.Sin(angle + 2.5f) * arrowSize);

            drawList.AddTriangleFilled(tip, left, right, color);
            drawList.AddTriangle(tip, left, right, 0xFF000000, 2f); // Black outline

            // Draw distance text near arrow
            if (Configuration.ShowDistance)
            {
                var text = $"{distance:F0}y";
                var textSize = ImGui.CalcTextSize(text);
                var textPos = edgePos - screenDir * 25f - new Vector2(textSize.X / 2, textSize.Y / 2);
                drawList.AddText(textPos, 0xFFFFFFFF, text);
            }
        }

        private Vector2 WorldToMap(Vector3 worldPos)
        {
            try
            {
                // Get current territory and map info
                var territoryId = Service.ClientState.TerritoryType;
                var territorySheet = Service.DataManager.GetExcelSheet<TerritoryType>();
                var territory = territorySheet?.GetRow(territoryId);

                if (territory != null)
                {
                    var map = territory.Value.Map.Value;
                    var scale = map.SizeFactor / 100.0f;
                    var offsetX = map.OffsetX;
                    var offsetY = map.OffsetY;

                    // Convert world coordinates to map coordinates
                    // FFXIV formula: mapCoord = (worldCoord - offset) * scale / 50 + 21.5
                    var mapX = (worldPos.X - offsetX) * scale / 50.0f + 21.5f;
                    var mapY = (worldPos.Z - offsetY) * scale / 50.0f + 21.5f;

                    return new Vector2(mapX, mapY);
                }
            }
            catch (Exception ex)
            {
                if (Configuration.DebugLogging)
                    Service.PluginLog.Error(ex, "Failed to convert world to map coordinates");
            }

            // Fallback to raw coordinates if conversion fails
            return new Vector2(worldPos.X, worldPos.Z);
        }

        private static bool IsAetherCurrent(string name)
        {
            return AetherCurrentNames.Contains(name.ToLowerInvariant());
        }

        private static string GetCardinalDirection(Vector3 from, Vector3 to)
        {
            var dx = to.X - from.X;
            var dz = to.Z - from.Z;

            // Normalize
            var length = MathF.Sqrt(dx * dx + dz * dz);
            if (length < 0.001f)
                return "";

            dx /= length;
            dz /= length;

            // Determine direction based on normalized vector
            // In FFXIV: +X is East, -X is West, +Z is South, -Z is North
            var threshold = 0.383f; // sin(22.5 degrees) for 8-direction compass

            string ns = "";
            string ew = "";

            if (dz < -threshold)
                ns = "N";
            else if (dz > threshold)
                ns = "S";

            if (dx > threshold)
                ew = "E";
            else if (dx < -threshold)
                ew = "W";

            return ns + ew;
        }

        public void Dispose()
        {
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PostRequestedUpdate, "AreaMap", OnAreaMapPostUpdate);
            Service.ClientState.TerritoryChanged -= OnTerritoryChanged;
            this.WindowSystem.RemoveAllWindows();
            ConfigWindow.Dispose();
            IconPickerWindow.Dispose();
            this.CommandManager.RemoveHandler(CommandName);
            this.PluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUI;
            this.PluginInterface.UiBuilder.Draw -= DrawUI;
            Service.PluginLog.Info("Aether Radar disposed");
        }
    }
}
