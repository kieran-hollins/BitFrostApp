using ComputeSharp;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.Serialization.Formatters;
using TerraFX.Interop.Windows;

namespace BitFrost
{
    public class FXGenerator
    {
        private LightingPatch Patch;
        public int WorkspaceWidth { get; set; } = 30;
        public int WorkspaceHeight { get; set; } = 4;
        private Timer? flashTimer;
        private bool ColourToggle = true;
        private byte[] Colours { get; set; }
        private float Speed { get; set; } = 0.5f;
        private Action? CurrentEffect;
        public AudioProcessor AudioProcessor;
        private readonly ReadWriteBuffer<float> _buffer;
        private readonly ReadOnlyBuffer<float> _magBuffer;
        private readonly ReadOnlyBuffer<float> _freqBuffer;

        private FXGenerator()
        {
            Patch = LightingPatch.Instance;
            Colours = new byte[3];
            CurrentEffect = null;
            AudioProcessor = new AudioProcessor();
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

        public void SetColour(byte[] colourValues)
        {
            Debug.WriteLine($"Colours set: {colourValues[0]}, {colourValues[1]}, {colourValues[2]}");
            Colours = colourValues;
        }

        public void ApplyMovementEffect(string effectName)
        {
            Debug.WriteLine($"Effect Name Received: {effectName}.");
            switch(effectName.ToLower()) 
            {
                case "fft-glow":
                    Debug.WriteLine("Triggering FFT Glow Effect");
                    CurrentEffect = StartFFTGlow;
                    CurrentEffect?.Invoke();
                    break;
                case "waves":
                    Debug.WriteLine("Triggering Waves Audio");
                    CurrentEffect = StartAudioWaveShader;
                    CurrentEffect?.Invoke();
                    break;
                case "red":
                    Debug.WriteLine("Triggering Red Shader");
                    CurrentEffect = TestRedShader;
                    CurrentEffect?.Invoke();
                    break;

                    //case "colour-flash":
                    //    Debug.WriteLine("Triggering colour-flash");
                    //    CurrentEffect = ColourFlash;
                    //    CurrentEffect?.Invoke();
                    //    break;
                    //case "horizontal-bounce":
                    //    Debug.WriteLine("Triggering horizontal-bounce");
                    //    CurrentEffect = HorizontalBounceStart;
                    //    CurrentEffect?.Invoke();
                    //    break;
                    //case "beat-change":
                    //    Debug.WriteLine("Triggering beat-change");
                    //    CurrentEffect = BeatColourChange;
                    //    CurrentEffect?.Invoke();
                    //    break;
                    //case "rainbow-audio":
                    //    Debug.WriteLine("Triggering rainbow-audio");
                    //    CurrentEffect = StartRainbowAudio;
                    //    CurrentEffect?.Invoke();
                    //    break;
                    //case "red-shader-test":
                    //    Debug.WriteLine("Triggering Shader Test");
                    //    CurrentEffect = StartAudioWaveShader;
                    //    CurrentEffect?.Invoke();
                    //    break;
            }
        }

        //private void HorizontalBounceStart()
        //{
        //    HorizontalBounce(0);
        //}

        //private void HorizontalBounce(int pos)
        //{
        //    if (pos <= 0)
        //    {
        //        pos += 1;
        //        FXPatch.SetVerticalLineValues(pos, WorkspaceHeight, Patch, Colours);
        //        FXPatch.SetVerticalLineValues(pos - 1, WorkspaceHeight, Patch, new byte[3]);
        //    }
        //    else if (pos >= WorkspaceWidth)
        //    {
        //        pos -= 1;
        //        FXPatch.SetVerticalLineValues(pos, WorkspaceHeight, Patch, Colours);
        //        FXPatch.SetVerticalLineValues(pos + 1, WorkspaceHeight, Patch, new byte[3]);
        //    }

        //    Thread.Sleep(30);

        //    HorizontalBounce(pos);
        //}

        private void TestRedShader()
        {
            byte[] currentLedData = Patch.GetCurrentDMXData();
            RedShaderEffect(currentLedData);
        }

        private void RedShaderEffect(byte[] ledData)
        {
            int totalLeds = ledData.Length / 3; // Assuming RGB for now...
            float[] ledColours = new float[ledData.Length];

            using (var buffer = GraphicsDevice.GetDefault().AllocateReadWriteBuffer(ledColours))
            {
                var shader = new Shaders.StaticRed(buffer, totalLeds);
                GraphicsDevice.GetDefault().For(totalLeds, shader);

                // Retrieve processed data from GPU
                buffer.CopyTo(ledColours);

            }

            byte[] processedData = ledColours.Select(x => (byte)(x * 255)).ToArray();

            UpdatePatch(processedData, totalLeds);

        }

        private void StartAudioWaveShader()
        {
            AudioProcessor.OnAudioBufferEvent += AudioWaveShader;
            AudioProcessor.Start();
        }

        private void AudioWaveShader(float[] magnitudeBuffer, float[] frequencyBuffer)
        {
            byte[] currentLedData = Patch.GetCurrentDMXData();
            WaveShaderEffect(currentLedData, magnitudeBuffer, frequencyBuffer);

        }

        private void WaveShaderEffect(byte[] ledData, float[] magnitudeBuffer, float[] frequencyBuffer)
        {
            int totalLeds = ledData.Length / 3; // Assuming RGB for now...
            float[] ledColours = new float[ledData.Length];

            using (var buffer = GraphicsDevice.GetDefault().AllocateReadWriteBuffer(ledColours))
            {
                var magBuffer = GraphicsDevice.GetDefault().AllocateReadOnlyBuffer(magnitudeBuffer);
                var freqBuffer = GraphicsDevice.GetDefault().AllocateReadOnlyBuffer(frequencyBuffer);
                var shader = new Shaders.WaveShader(buffer, magBuffer, freqBuffer, (float)DateTime.Now.TimeOfDay.TotalSeconds, WorkspaceWidth, WorkspaceHeight);
                // GraphicsDevice.GetDefault().For(WorkspaceWidth, WorkspaceHeight, shader);
                GraphicsDevice.GetDefault().For(WorkspaceWidth, WorkspaceHeight, shader);

                // Retrieve processed data from GPU
                buffer.CopyTo(ledColours);

            }

            byte[] processedData = ledColours.Select(x => (byte)(x * 255)).ToArray();

            UpdatePatch(processedData, totalLeds);
        }

        private void StartFFTGlow()
        {
            AudioProcessor.OnAudioBufferEvent += FFTGlowShader;
            AudioProcessor.Start();
        }

        private void FFTGlowShader(float[] magnitudebuffer, float[] frequencyBuffer)
        {
            byte[] currentLEDData = Patch.GetCurrentDMXData();
            FFTGlowEffect(currentLEDData, magnitudebuffer, frequencyBuffer);
        }

        private void FFTGlowEffect(byte[] ledData, float[] magnitudeBuffer, float[] frequencyBuffer)
        {
            int totalLEDs = ledData.Length / 3;
            float[] LEDColours = new float[ledData.Length];
            float force = 3.0f;

            var graphicsDevice = GraphicsDevice.GetDefault();

            var buffer = graphicsDevice.AllocateReadWriteBuffer(LEDColours);
            var magBuffer = graphicsDevice.AllocateReadOnlyBuffer(magnitudeBuffer);
            var freqBuffer = graphicsDevice.AllocateReadOnlyBuffer(frequencyBuffer);

            try
            {
                Debug.WriteLine("Trying to run FFTGlow");
                var shader = new Shaders.FFTGlow(
                    buffer,
                    magBuffer,
                    freqBuffer,
                    (float)DateTime.Now.TimeOfDay.TotalSeconds,
                    WorkspaceWidth,
                    WorkspaceHeight,
                    force
                );

                //GraphicsDevice.GetDefault().For(WorkspaceWidth, shader);
                 graphicsDevice.For(WorkspaceWidth, WorkspaceHeight, shader );

                buffer.CopyTo(LEDColours, 0);

                byte[] processedData = LEDColours.Select(x => (byte)(x * 255)).ToArray();

                UpdatePatch(processedData, totalLEDs);
            }
            catch(Exception e)
            {
                Debug.WriteLine($"{e.Message}");
            }
            finally
            {
                // Dispose buffers to avoid memory leaks
                buffer.Dispose();
                magBuffer.Dispose();
                freqBuffer.Dispose();
            }
        }

        

        //public void SendTestAudio()
        //{
        //    double[] audioData = new double[5];
        //    for (int i = 0; i < audioData.Length; i++)
        //    {
        //        audioData[i] = 101.0;
        //    }
        //    AudioProcessor.ProcessAudio(audioData);
        //}

        //private void BeatColourChange()
        //{
        //    AudioProcessor.OnBeatEvent += SetRandomColour;
        //    AudioProcessor.Start();
        //}

        //private void SetRandomColour(double frequency, double magnitude)
        //{
        //    Debug.WriteLine($"Frequency: {frequency} Magnitude: {magnitude}");
        //    Colours = Utils.GetRandomColour();
        //    StaticColour(Colours);
        //}

        //private void StartRainbowAudio()
        //{
        //    AudioProcessor.OnBeatEvent += RainbowAudio;
        //    AudioProcessor.Start();
        //}

        //private void RainbowAudio(double frequency, double magnitude) 
        //{
        //    // This is the old method using the CPU. Replace when possible.
        //    double maxEnergy = 8000f;

        //    byte[] red = Utils.GetColour("red");
        //    byte[] green = Utils.GetColour("green");
        //    byte[] blue = Utils.GetColour("blue");
        //    byte[] yellow = Utils.GetColour("yellow");
        //    byte[] teal = Utils.GetColour("teal");
        //    byte[] violet = Utils.GetColour("violet");

        //    int sectionWidth = (int)Math.Ceiling(WorkspaceWidth / 6f);

        //    Debug.WriteLine($"Frequency: {frequency} Magnitude: {magnitude}");

        //    if (frequency > 0 && frequency <= 100) 
        //    { 
        //        for (int i = 0; i < sectionWidth; i++)
        //        {
        //            FXPatch.SetVerticalLineValues(i, (int)Utils.Lerp(magnitude, 0f, maxEnergy, 0f, WorkspaceHeight), Patch, red);
        //        }
        //    }

        //    if (frequency > 100 && frequency <= 300)
        //    {
        //        for (int i = sectionWidth; i < sectionWidth*2; i++)
        //        {
        //            FXPatch.SetVerticalLineValues(i, (int)Utils.Lerp(magnitude, 0f, maxEnergy, 0f, WorkspaceHeight), Patch, yellow);
        //        }
        //    }

        //    if (frequency > 300 && frequency <= 600)
        //    {
        //        for (int i = sectionWidth*2; i < sectionWidth * 3; i++)
        //        {
        //            FXPatch.SetVerticalLineValues(i, (int)Utils.Lerp(magnitude, 0f, maxEnergy, 0f, WorkspaceHeight), Patch, green);
        //        }
        //    }

        //    if (frequency > 800 && frequency <= 1500)
        //    {
        //        for (int i = sectionWidth * 3; i < sectionWidth * 4; i++)
        //        {
        //            FXPatch.SetVerticalLineValues(i, (int)Utils.Lerp(magnitude, 0f, maxEnergy, 0f, WorkspaceHeight), Patch, teal);
        //        }
        //    }

        //    if (frequency > 1500 && frequency <= 3000)
        //    {
        //        for (int i = sectionWidth * 4; i < sectionWidth * 5; i++)
        //        {
        //            FXPatch.SetVerticalLineValues(i, (int)Utils.Lerp(magnitude, 0f, maxEnergy, 0f, WorkspaceHeight), Patch, blue);
        //        }
        //    }

        //    if (frequency > 3000 && frequency <= 18800)
        //    {
        //        for (int i = sectionWidth * 6; i < sectionWidth * 7; i++)
        //        {
        //            FXPatch.SetVerticalLineValues(i, (int)Utils.Lerp(magnitude, 0f, maxEnergy, 0f, WorkspaceHeight), Patch, violet);
        //        }
        //    }
        //}
        

        //private void ColourFlash()
        //{
        //    // Uses linear interpolation to scale the timer period
        //    int timerMs = (int)Utils.Scale(Speed, 50, 1000);

        //    flashTimer = new(_ =>
        //    {
        //        if (ColourToggle)
        //        {
        //            FXPatch.SetValues(WorkspaceWidth, WorkspaceHeight, Patch, Colours);
        //            ColourToggle = false;
        //        }
        //        else
        //        {
        //            FXPatch.SetValues(WorkspaceWidth, WorkspaceHeight, Patch, new byte[3]);
        //            ColourToggle = true;
        //        }

        //    }, this, timerMs, timerMs);
            
        //}

        //public void StaticColour(byte[] colourValues)
        //{
        //    Colours = colourValues;
        //    FXPatch.SetValues(WorkspaceWidth, WorkspaceHeight, Patch, Colours);
        //}

        private void UpdatePatch(byte[] processedData, int totalLeds)
        {
            Debug.WriteLine("Updating Patch");
            for (int i = 1; i < totalLeds; i += 3)
            {
                byte[] CurrentLedData = new byte[3];

                for (int j = 0; j < 3; j++)
                {
                    CurrentLedData[j] = processedData[i + j];
                }

                try
                {
                    var coordinate = Patch.GetLEDLocation(i);
                    Patch.SetDMXValue(coordinate.Item1, coordinate.Item2, CurrentLedData);
                    Debug.WriteLine($"Setting ({coordinate.Item1}, {coordinate.Item2}) to {CurrentLedData[0]} {CurrentLedData[1]} {CurrentLedData[2]}");
                }
                catch
                {

                }

            }

            Patch.GetCurrentDMXData();
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

            public static void SetHorizontalLineValues(int width, int y, LightingPatch patch, byte[] colourValues)
            {
                for (int x = 0; x < width; x++)
                {
                    patch.SetDMXValue(x, y, colourValues);
                }
                patch.GetCurrentDMXData();
            }

            public static void SetVerticalLineValues(int x, int height, LightingPatch patch, byte[] colourValues)
            {
                for (int y = 0; y < height; y++)
                {
                    patch.SetDMXValue(x, y, colourValues);
                }
                patch.GetCurrentDMXData();
            }

            public static void SetPixelValueNoUpdate(int x, int y, LightingPatch patch, byte[] colourValues)
            {
                patch.SetDMXValue(x, y, colourValues);
            }

            public static void UpdatePatch(LightingPatch patch)
            {
                patch.GetCurrentDMXData();
            }
        }
    }
}

