using System.ComponentModel;
using System.Diagnostics;
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

        public LED() { LEDProfile = new RGB(); StartDMXAddress = 513; }

        public static LED CreateRGBLED(int startDMXAddress)
        {
            return new LED(startDMXAddress, new RGB());
        }

        public static LED CreateGRBLED(int startDMXAddress)
        {
            return new LED(startDMXAddress, new GRB());
        }

        public static LED CreateRGBWLED(int startDMXAddress)
        {
            return new LED(startDMXAddress, new RGBW());
        }
    }


    public abstract class LEDProfile
    {
        public abstract int Channels { get; }
        public abstract string Type { get; }
        public abstract byte[] GetDMXData();
        public abstract void SetDMXData(byte[] data);
    }

    public class RGB : LEDProfile
    {
        public override int Channels => 3;
        public override string Type => "RGB";

        private byte r;
        private byte g;
        private byte b;

        public byte Red { get { return r; } set { r = value; } }
        public byte Green { get { return g; } set { g = value; } }
        public byte Blue { get { return b; } set { b = value; } }

        public void ConvertColour(byte r, byte g, byte b)
        {
            Red = r;
            Green = g;
            Blue = b;
        }

        public void SetColours(byte R, byte G, byte B)
        {
            r = R;
            g = G;
            b = B;
        }

        public override byte[] GetDMXData()
        {
            byte[] data = new byte[3];

            data[0] = Red;
            data[1] = Green;
            data[2] = Blue;
            // Debug.WriteLine($"DMX GET: R:{this.Red} G:{this.Green} B:{this.Blue}");

            return data;
        }

        public override void SetDMXData(byte[] data)
        {
            // Debug.WriteLine($"Setting Channels {data[0]} {data[1]} {data[2]}");
            Red = data[0];
            Green = data[1];
            Blue = data[2];

            // Debug.WriteLine($"DMX SET: R:{this.Red} G:{this.Green} B:{this.Blue}");
        }
    }

    public class RGBW : LEDProfile
    {
        public override int Channels => 4;
        public override string Type => "RGBW";

        public byte Red { get; set; }
        public byte Green { get; set; }
        public byte Blue { get; set; }
        public byte White { get; set; }

        public void ConvertColour(byte r, byte g, byte b, byte w)
        {
            Red = r;
            Green = g;
            Blue = b;
            White = w;
        }

        public override byte[] GetDMXData()
        {
            byte[] data = new byte[4];

            data[0] = (byte)Red;
            data[1] = (byte)Green;
            data[2] = (byte)Blue;
            data[3] = (byte)White;

            return data;
        }

        public override void SetDMXData(byte[] data)
        {
            Red = data[0];
            Green = data[1];
            Blue = data[2];
            White = data[3];
        }
    }

    public class GRB : LEDProfile
    {
        public override int Channels => 3;
        public override string Type => "GRB";

        public byte Green { get; set; }
        public byte Red { get; set; }
        public byte Blue { get; set; }

        public void ConvertColour(byte r, byte g, byte b)
        {
            Green = g;
            Red = r;
            Blue = b;
        }

        public override byte[] GetDMXData()
        {
            byte[] data = new byte[3];

            data[0] = Green;
            data[1] = Red;
            data[2] = Blue;

            return data;
        }

        public override void SetDMXData(byte[] data)
        {
            Green = data[0];
            Red = data[1];
            Blue = data[2];
        }
    }
}