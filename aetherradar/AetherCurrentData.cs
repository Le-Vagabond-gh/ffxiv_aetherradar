using System.Collections.Generic;
using System.Numerics;

namespace aetherradar
{
    /// <summary>
    /// Static data for all field aether current positions by zone.
    /// Data sourced from eorzeaworld.com
    /// Note: Quest-rewarded aether currents are not included as they cannot be found in the field.
    /// </summary>
    public static class AetherCurrentData
    {
        public struct AetherCurrentLocation
        {
            public float X;
            public float Y;
            public float Z;
            public string Description;

            public AetherCurrentLocation(float x, float y, float z = 0f, string description = "")
            {
                X = x;
                Y = y;
                Z = z;
                Description = description;
            }

            public Vector2 MapCoords => new Vector2(X, Y);
        }

        // Zone name -> list of field aether current locations (map coordinates)
        public static readonly Dictionary<string, List<AetherCurrentLocation>> FieldCurrentsByZone = new()
        {
            // ==================== HEAVENSWARD ====================
            ["Coerthas Western Highlands"] = new()
            {
                new(30.6f, 33.7f, 1.3f, "On the northern of Falcon's Nest"),
                new(31.5f, 11.8f, 1.1f, "Atop a house, access through stairs at x31.1 y11.5"),
                new(9.3f, 15f, 0.8f, "On the edge of a cliff"),
                new(15.8f, 22.3f, 0.8f, "Overlooking The Convictory"),
            },
            ["The Dravanian Forelands"] = new()
            {
                new(30.6f, 36.2f, 0.6f, "In the Loth and Gnath past the second door"),
                new(12.9f, 14f, 1.5f, "In the cavern on Mourn"),
                new(31.2f, 16.8f, 1f, "In Whilom River north of fork"),
                new(37.8f, 28.3f, 1.4f, "On top of the arched tree"),
            },
            ["The Churning Mists"] = new()
            {
                new(29.3f, 19.9f, 0.5f, "On path leading to Monsterie"),
                new(30.9f, 35.7f, 0.3f, "East of Moghome via east tunnel"),
                new(20.6f, 27f, 0.7f, "In Asah"),
                new(7f, 27.4f, 2f, "At the top of the palace in Zenith"),
            },
            ["The Sea of Clouds"] = new()
            {
                new(11.2f, 15.2f, 2f, "Up the stairs behind vanu chief"),
                new(18.9f, 11.6f, 2.2f, "Next to Cid's Airship"),
                new(7.3f, 20.3f, 2.3f, "Right before Vanu Village entrance"),
                new(7.6f, 25.8f, 1.8f, "Right when you get off the airship"),
            },
            ["The Dravanian Hinterlands"] = new()
            {
                new(13.5f, 36.1f, 1f, "In the caverns by MSQ"),
                new(12.8f, 16.9f, 1.1f, "Entrance to Answering Quarter"),
                new(24.5f, 19f, 0.3f, "Just before the center settlement bridge"),
                new(37.1f, 25.5f, 0.9f, "Entrance to area from main quest"),
            },
            // Azys Lla has no field currents (all quest-based)

            // ==================== STORMBLOOD ====================
            ["The Fringes"] = new()
            {
                new(36.3f, 17.2f, 1.6f, "On a ledge below the path leading up"),
                new(27.9f, 21.6f, 0.7f, "On a cliff overlooking the village"),
                new(25f, 11.1f, 0.6f, "On the edge of mountain"),
                new(11.7f, 16.4f, 0.6f, "Under the bridge"),
            },
            ["Yanxia"] = new()
            {
                new(19.7f, 32.7f, 0.1f, "Edge of broken bridge"),
                new(30.6f, 37.9f, 0.3f, "Edge of cliff, south of Castrum Fluminis"),
                new(24.7f, 21.2f, 1.4f, "Climb to top of cliff in Namai"),
                new(31.4f, 29.5f, 0.1f, "On ledge of rock"),
            },
            ["The Ruby Sea"] = new()
            {
                new(5.3f, 26.1f, 0.3f, "In Isle of Zekki (swim to underwater cave at x12.2 y25.3)"),
                new(35.5f, 20.5f, 0.4f, "On edge of cliff"),
                new(21.9f, 9f, 0.2f, "On roof of hut"),
                new(29.9f, 38.8f, 0.1f, "Behind rocks"),
            },
            ["The Peaks"] = new()
            {
                new(23.8f, 31.8f, 2.5f, "Edge of cliff overlooking tree and lake"),
                new(10.8f, 26.4f, 2.5f, "Edge of a cliff"),
                new(15.8f, 16.9f, 0.8f, "Find stairs at x15.8 y14.2, go 2/3 up then left/right"),
                new(26.7f, 6.9f, 0.8f, "From Ala Gannha go NW up two inclines"),
            },
            ["The Azim Steppe"] = new()
            {
                new(7.5f, 34.6f, 0f, "Southern edge of map"),
                new(23.5f, 20.4f, 1.1f, "Edge of Dawn Throne"),
                new(27.1f, 12.2f, 0.8f, "Climb up cliffside and fall to lower ledge"),
                new(33.9f, 30.6f, -0.4f, "Riverbed just outside Reunion"),
            },
            ["The Lochs"] = new()
            {
                new(34.9f, 31.6f, 0.8f, "Near x33.8 y30.6, up stairs and turn right"),
                new(29.5f, 22.8f, 1.1f, "Look for stairs to left/right as you approach"),
                new(23.6f, 37.2f, 0.4f, "Edge of cliff behind Sali Monastery"),
                new(14.3f, 21.7f, 0.2f, "Navigate to x13.3 y20.3, head SE"),
            },

            // ==================== SHADOWBRINGERS ====================
            ["Lakeland"] = new()
            {
                new(9f, 17.5f, 0.5f, "Ostall Imperative, north of porter on a ledge"),
                new(18.4f, 19.4f, 0.1f, "Above a canopy north of Radisca's Round"),
                new(33.7f, 16.8f, 0.2f, "At the tip of the plateau, approach from north"),
                new(32.5f, 28.5f, 0.1f, "Atop stairs at The Accensor Gate"),
            },
            ["Il Mheg"] = new()
            {
                new(21.2f, 16.5f, 0.8f, "North of entrance to Mithai Glorianda"),
                new(30.1f, 6f, 0.9f, "Northeast of Wolekdorf Aetheryte"),
                new(21.8f, 4.4f, 1.1f, "Up a staircase of mushrooms in Pla Enni"),
                new(16.8f, 24.6f, 0f, "The roof of a sunken house"),
            },
            ["Kholusia"] = new()
            {
                new(20.2f, 21.1f, 3.4f, "On top of a rock mound"),
                new(33.9f, 10.3f, 2.8f, "The Duergar's Tewel, between geysers"),
                new(8.4f, 33.2f, -0.1f, "East side of isthmus to Barrow Island"),
                new(34.4f, 32.5f, 0f, "End of pier along the coast"),
            },
            ["Amh Araeng"] = new()
            {
                new(24.6f, 34.9f, 0.5f, "At map marker for Pristine Palace of Amh Malik"),
                new(14.6f, 16.7f, 1.6f, "On railroad track, east of Twine"),
                new(28.3f, 32.2f, 0.5f, "The Derrick, south from Inn at Journey's Head"),
                new(29.8f, 10.4f, 0.5f, "North of bridge from The Crystarium"),
            },
            ["The Rak'tika Greatwood"] = new()
            {
                new(35.1f, 16.2f, -0.4f, "Across Sleepaway Common, east of Fanow"),
                new(28.2f, 25.5f, 0.2f, "On cliff between The Wild Fete and Bowrest"),
                new(18.6f, 22.4f, -0.1f, "End of the Rotzatl River"),
                new(13.3f, 31.6f, 0f, "North edge of ruins of Fort Gohn"),
            },
            ["The Tempest"] = new()
            {
                new(5.3f, 19.4f, -4.9f, "On edge of cliff overlooking Amaurot"),
                new(28.2f, 15.9f, -2.5f, "Underground path starts at x31.5 y16.2"),
                new(22.8f, 11.2f, -1.7f, "Near rock formations around a pillar"),
                new(29.1f, 7.2f, -1.6f, "Between rock ledges beneath pillar SE of Kholusia transition"),
            },

            // ==================== ENDWALKER ====================
            ["Thavnair"] = new()
            {
                new(18f, 32f, 0.2f, ""),
                new(20f, 7.2f, 1f, ""),
                new(23.3f, 14.2f, 0f, ""),
                new(32.4f, 18.3f, 0.2f, ""),
            },
            ["Labyrinthos"] = new()
            {
                new(27.8f, 5.6f, 4.3f, ""),
                new(36.3f, 22.2f, 3.3f, ""),
                new(18.8f, 35.1f, 2f, ""),
                new(10.6f, 34.4f, 1.9f, ""),
            },
            ["Garlemald"] = new()
            {
                new(17.5f, 29.9f, 0.4f, ""),
                new(25.7f, 33.9f, 0f, ""),
                new(29.1f, 11.8f, 0.4f, ""),
                new(10f, 14.8f, 0.4f, ""),
            },
            ["Mare Lamentorum"] = new()
            {
                new(22.1f, 11f, -0.5f, ""),
                new(27.7f, 9.6f, -1.6f, ""),
                new(11.7f, 9.4f, -1.6f, ""),
                new(22.2f, 18.6f, 1.1f, ""),
            },
            ["Elpis"] = new()
            {
                new(33.7f, 23.4f, 1.7f, ""),
                new(6.1f, 30.2f, 1.2f, ""),
                new(10.9f, 25f, 3.1f, ""),
                new(13.3f, 8f, 4.7f, ""),
            },
            ["Ultima Thule"] = new()
            {
                new(14.8f, 14.2f, 2.2f, ""),
                new(21.5f, 6.2f, 2.3f, ""),
                new(31.9f, 26.3f, 3.9f, ""),
                new(34.1f, 29.6f, 3.9f, ""),
            },

            // ==================== DAWNTRAIL ====================
            ["Urqopacha"] = new()
            {
                new(12.3f, 11.6f, 1.9f, ""),
                new(18.7f, 9.8f, 1.2f, ""),
                new(17.4f, 17.4f, 1.4f, ""),
                new(29.7f, 7.8f, 0.8f, ""),
                new(28.5f, 16.6f, 1.1f, ""),
                new(29.5f, 26.6f, 3f, ""),
                new(28.8f, 21.3f, 2.8f, ""),
                new(17.5f, 20.4f, 2.4f, ""),
                new(5.6f, 23.9f, 2.7f, ""),
                new(22.8f, 36.3f, 2.2f, ""),
            },
            ["Yak T'el"] = new()
            {
                new(19.2f, 10.9f, 3f, ""),
                new(17.7f, 6.1f, 3.2f, ""),
                new(29.3f, 10.8f, 3.1f, ""),
                new(10.4f, 18.6f, 2.9f, ""),
                new(33.7f, 25.9f, 3.1f, ""),
                new(19.2f, 33.8f, 0.8f, ""),
                new(22.3f, 21.5f, 1.4f, ""),
                new(25.6f, 24.7f, 1f, ""),
                new(36.4f, 35.7f, 1.2f, ""),
                new(7.9f, 26.2f, 1.3f, ""),
            },
            ["Kozama'uka"] = new()
            {
                new(8.8f, 11.7f, 0f, ""),
                new(9.5f, 17.9f, 0f, ""),
                new(27.3f, 7.8f, 0f, ""),
                new(31.7f, 14.5f, 0.1f, ""),
                new(39.6f, 13.7f, 0.1f, ""),
                new(31.3f, 37.9f, 1.2f, ""),
                new(23.9f, 32f, 1.1f, ""),
                new(22.5f, 27.3f, 1.1f, ""),
                new(15.5f, 33.9f, 1.1f, ""),
                new(6.3f, 23.9f, 1.3f, ""),
            },
            ["Shaaloani"] = new()
            {
                new(27.4f, 31.6f, 0.7f, ""),
                new(25f, 20.2f, 0.8f, ""),
                new(34.6f, 23.1f, 0.9f, ""),
                new(29f, 11.3f, 0.5f, ""),
                new(34.3f, 13f, 0.6f, ""),
                new(20.4f, 17.9f, 0.9f, ""),
                new(17.3f, 20.1f, 1f, ""),
                new(9.3f, 14.4f, 1.1f, ""),
                new(7.2f, 19.8f, 1f, ""),
                new(8.9f, 28.1f, 0.6f, ""),
            },
            ["Heritage Found"] = new()
            {
                new(29.6f, 26f, 1.6f, ""),
                new(23f, 18.6f, 1f, ""),
                new(33.6f, 17.6f, 1.4f, ""),
                new(35.2f, 11f, 1.5f, ""),
                new(25.6f, 8.5f, 0.7f, ""),
                new(9.5f, 12.5f, 0.3f, ""),
                new(15.7f, 16f, 0.7f, ""),
                new(9.4f, 26.8f, 0.1f, ""),
                new(20.9f, 28.1f, 0.8f, ""),
                new(12.3f, 35.3f, 0f, ""),
            },
            ["Living Memory"] = new()
            {
                new(11.1f, 35f, 0.2f, ""),
                new(7.5f, 30.8f, 0.2f, ""),
                new(25.9f, 32.5f, 0.2f, ""),
                new(34f, 34.2f, 0.2f, ""),
                new(36.3f, 28.3f, 0.6f, ""),
                new(24.9f, 15.3f, 0.4f, ""),
                new(31.7f, 8.3f, 0.5f, ""),
                new(27.6f, 10.3f, 0.6f, ""),
                new(12f, 20.4f, 0.5f, ""),
                new(10.4f, 11.3f, 0f, ""),
            },
        };

        /// <summary>
        /// Get the list of field aether currents for a zone by name.
        /// </summary>
        public static List<AetherCurrentLocation>? GetFieldCurrents(string zoneName)
        {
            return FieldCurrentsByZone.TryGetValue(zoneName, out var currents) ? currents : null;
        }
    }
}
