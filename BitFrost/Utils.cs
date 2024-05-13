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
                }
            }
            else
            {
                throw new ArgumentException($"Invalid colour format: {colour}\nExpected format: #ffffff");
            }

            return channelValues;
        }
    }
}
