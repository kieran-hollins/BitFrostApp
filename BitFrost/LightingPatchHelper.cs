namespace BitFrost
{
    public interface IPatchHelper
    {
        void AddLED(int x, int y, LED led);
        void RemoveLED(int x, int y);
        string GetLEDLocation(int dmxAddress);
        void ClearAll();
    }

}
