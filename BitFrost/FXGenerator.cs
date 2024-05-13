using System.Drawing;

namespace BitFrost
{
    public class FXGenerator
    {
        private LightingPatch Patch;
        public int WorkspaceWidth { get; set; } = 30;
        public int WorkspaceHeight { get; set; } = 30;
        private Timer flashTimer;
        private bool colourToggle = false;
        private byte[] Colours = new byte[3];

        private FXGenerator()
        {
            Patch = LightingPatch.Instance;
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
            FXPatch.SetValues(WorkspaceWidth, WorkspaceHeight, Patch, colourValues);
        }

        public void ColourFlash(int intervalMS, byte[] colourValues)
        {
            Colours = colourValues;
            byte[] dark = new byte[3];

            flashTimer = new (_ => {
                if (colourToggle)
                {
                    FXPatch.SetValues(WorkspaceWidth, WorkspaceHeight, Patch, Colours);
                    colourToggle = false;
                }
                else
                {
                    FXPatch.SetValues(WorkspaceWidth, WorkspaceHeight, Patch, dark);
                    colourToggle = true;
                }
                
            }, null, intervalMS, intervalMS);
        }

        private static class FXPatch
        {
            public static void SetValues(int width, int height, LightingPatch patch, byte[] colourValues)
            {
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        patch.SetDMXValue(x, y, colourValues);
                    }
                }
                patch.GetCurrentDMXData();
            }
        }
    }
}

