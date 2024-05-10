using System.Drawing;

namespace BitFrost
{
    public class FXGenerator
    {
        private LightingPatch Patch;
        public int WorkspaceWidth { get; set; } = 30;
        public int WorkspaceHeight { get; set; } = 30;

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

        public void StaticColour(string hexColour)
        {
            byte[] data = new byte[3];
            if (hexColour.Length > 7)
            {
                throw new ArgumentException("Ensure your string is formatted '#0D0D0D'");
            }
            if (hexColour[0] == '#')
            {
                for (int i = 0; i < 3; i++)
                {
                    string str = hexColour.Substring(i * 2 + 1, 2);
                    data[i] = Convert.ToByte(str, 16);
                }
            }

            for (int x = 0; x < WorkspaceWidth; x++)
            {
                for (int y = 0; y < WorkspaceHeight; y++)
                {
                    Patch.SetDMXValue(x, y, data);
                }
            }

            Patch.GetCurrentDMXData();
        }
    }
}

