using System.Diagnostics;
using System.Drawing;

namespace BitFrost
{
    public class FXGenerator
    {
        private LightingPatch Patch;
        public int WorkspaceWidth { get; set; } = 30;
        public int WorkspaceHeight { get; set; } = 30;
        private Timer? flashTimer;
        private bool ColourToggle = true;
        private byte[] Colours;

        private FXGenerator()
        {
            Patch = LightingPatch.Instance;
            Colours = new byte[3];
        }

        public static FXGenerator Instance { get { return Nested.generatorInstance; } }

        private class Nested
        {
            // Explicit static constructor to tell C# compiler
            // not to mark type as beforefieldinit
            static Nested()
            {
            }

            internal static readonly FXGenerator generatorInstance = new FXGenerator ();
        }

        public void StaticColour(byte[] colourValues)
        {
            Colours = colourValues;
            FXPatch.SetValues(WorkspaceWidth, WorkspaceHeight, Patch, Colours);
        }

        public void ColourFlash(int intervalMS, byte[] colourValues)
        {
            Colours = colourValues;
            byte[] dark = new byte[3];

            flashTimer = new (_ => {
                if (ColourToggle)
                {
                    FXPatch.SetValues(WorkspaceWidth, WorkspaceHeight, Patch, Colours);
                    ColourToggle = false;
                }
                else
                {
                    FXPatch.SetValues(WorkspaceWidth, WorkspaceHeight, Patch, dark);
                    ColourToggle = true;
                }
                
            }, this, intervalMS, intervalMS);
        }

        private static class FXPatch
        {
            public static void SetValues(int width, int height, LightingPatch patch, byte[] colourValues)
            {
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        // Debug.Write($"Setting patch ({x}, {y}) to {colourValues[0]} {colourValues[1]} {colourValues[2] }");
                        patch.SetDMXValue(x, y, colourValues);
                    }
                }
                patch.GetCurrentDMXData();
            }
        }
    }
}

