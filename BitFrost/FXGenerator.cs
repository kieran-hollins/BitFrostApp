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
        private byte[] Colours { get; set; }
        private float Speed { get; set; } = 0.5f;
        private Action CurrentEffect;

        private FXGenerator()
        {
            Patch = LightingPatch.Instance;
            Colours = new byte[3];
            CurrentEffect = null;
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

        public void setColour(byte[] colourValues)
        {
            Debug.WriteLine($"Colours set: {colourValues[0]}, {colourValues[1]}, {colourValues[2]}");
            Colours = colourValues;
        }

        public void ApplyMovementEffect(string effectName)
        {
            Debug.WriteLine($"Effect Name Received: {effectName}.");
            switch(effectName.ToLower()) 
            {
                case "colour-flash":
                    Debug.WriteLine("Triggering colour-flash");
                    CurrentEffect = ColourFlash;
                    CurrentEffect?.Invoke();
                    break;
            }
        }

        private void ColourFlash()
        {
            // Uses linear interpolation to scale the timer period
            int timerMs = (int)Utils.Scale(Speed, 50, 1000);

            flashTimer = new(_ =>
            {
                if (ColourToggle)
                {
                    FXPatch.SetValues(WorkspaceWidth, WorkspaceHeight, Patch, Colours);
                    ColourToggle = false;
                }
                else
                {
                    FXPatch.SetValues(WorkspaceWidth, WorkspaceHeight, Patch, new byte[3]);
                    ColourToggle = true;
                }

            }, this, timerMs, timerMs);
            
        }

        public void StaticColour(byte[] colourValues)
        {
            Colours = colourValues;
            FXPatch.SetValues(WorkspaceWidth, WorkspaceHeight, Patch, Colours);
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

