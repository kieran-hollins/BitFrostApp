using System.ComponentModel;
using System.Drawing;

namespace BitFrost
{
    public class LED
    {
        public int StartDMXAddress { get; set; }
        public LEDProfile LEDProfile { get; set; }

        public LED(int startDMXAddress, LEDProfile ledProfile)
        {
            StartDMXAddress = startDMXAddress;
            LEDProfile = ledProfile;
        }
    }


    public abstract class LEDProfile
    {
        public abstract int Channels { get; }
        public abstract string Type { get; }
    }

    public class RGB : LEDProfile
    {
        public override int Channels => 3;
        public override string Type => "RGB";

        public int Red { get; set; }
        public int Green { get; set; }
        public int Blue { get; set; }
        
        public void ConvertColour(int r, int g, int b)
        {
            Red = Clamp(r);
            Green = Clamp(g);
            Blue = Clamp(b);
        }

        private static int Clamp(int input)
        {
            if (input < 0) return 0;
            if (input > 255) return 255;
            else return input;
        }
    }

    public class RGBW : LEDProfile
    {
        public override int Channels => 4;
        public override string Type => "RGBW";

        public int Red { get; set; }
        public int Green { get; set; }
        public int Blue { get; set; }
        public int White { get; set; }

        public void ConvertColour(int r, int g, int b, int w)
        {
            Red = Clamp(r);
            Green = Clamp(g);
            Blue = Clamp(b);
            White = Clamp(w);
        }

        private static int Clamp(int input)
        {
            if (input < 0) return 0;
            if (input > 255) return 255;
            else return input;
        }
    }

    public class GRB : LEDProfile
    {
        public override int Channels => 3;
        public override string Type => "GRB";

        public int Green { get; set; }
        public int Red { get; set; }
        public int Blue { get; set; }

        public void ConvertColour(int r, int g, int b)
        {
            Green = Clamp(g);
            Red = Clamp(r);
            Blue = Clamp(b);
        }

        private static int Clamp(int input)
        {
            if (input < 0) return 0;
            if (input > 255) return 255;
            else return input;
        }
    }
}