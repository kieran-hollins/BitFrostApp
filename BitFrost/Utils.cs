using System.Diagnostics;
using System.Numerics;
using System.Text.RegularExpressions;

namespace BitFrost
{
    public static class Utils
    {
        // Compiled regex for better efficiency
        private static readonly Regex _colourRegex = new Regex(@"^#?([a-fA-F0-9]{2})([a-fA-F0-9]{2})([a-fA-F0-9]{2})$");
        public static byte[] GetColourValuesFromHex(string colour)
        {
            byte[] channelValues = new byte[3];
            Match match = _colourRegex.Match(colour);

            if (match.Success)
            {
                for (int i = 0; i < 3; i++)
                {
                    channelValues[i] = Convert.ToByte(match.Groups[i + 1].Value, 16);
                    // Debug.WriteLine($"Setting channel {i} to {channelValues[i]}");
                }
            }
            else
            {
                throw new ArgumentException($"Invalid colour format: {colour}\nExpected format: #ffffff");
            }

            return channelValues;
        }

        public static byte[] GetRandomColour()
        {
            byte[] channels = new byte[3];

            Random rnd = new Random();
            channels[0] = (byte)rnd.Next(0, 255);
            channels[1] = (byte)rnd.Next(0, 255);
            channels[2] = (byte)rnd.Next(0, 255);

            return channels;
        }

        public static double Scale(double t, double min, double max)
        {
            if (t < 0 || t > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(t), "Value must be between 0 and 1");
            }

            Debug.WriteLine($"Scaled value = {min + t * (max - min)}");
            return min + t * (max - min);
        }
    }
}
