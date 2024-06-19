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
        private bool IsProcessing;
        private Action? CurrentEffect;
        public AudioProcessor AudioProcessor;
        private ReadWriteBuffer<float> _ledBuffer;
        private ReadOnlyBuffer<float> _magBuffer;
        private ReadOnlyBuffer<float> _freqBuffer;

        private FXGenerator()
        {
            Patch = LightingPatch.Instance;
            Colours = new byte[3];
            CurrentEffect = null;
            AudioProcessor = new AudioProcessor();
            IsProcessing = false;
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
                case "hello":
                    Debug.WriteLine("Triggering Half Red Half Blue");
                    if (CurrentEffect != TestHalfRedHalfBlue)
                    {
                        CurrentEffect = TestHalfRedHalfBlue;   
                    }
                    CurrentEffect?.Invoke();
                    break;
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
                case "average":
                    Debug.WriteLine("Triggering Average Colour Shader");
                    CurrentEffect = StartAverageColour;
                    CurrentEffect?.Invoke();
                    break;
            }

            
        }


        public void UpdateBuffers(float[] magnitudeBuffer, float[] frequencyBuffer, int totalLEDs)
        {
            float[] ledColours = new float[totalLEDs];
            var graphicsDevice = GraphicsDevice.GetDefault();

            _ledBuffer = graphicsDevice.AllocateReadWriteBuffer(ledColours);
            _magBuffer = graphicsDevice.AllocateReadOnlyBuffer(magnitudeBuffer);
            _freqBuffer = graphicsDevice.AllocateReadOnlyBuffer(frequencyBuffer);
        }



        private void TestRedShader()
        {
            byte[] currentLedData = Patch.GetCurrentDMXData();
            RedShaderEffect(currentLedData);
        }

        private void RedShaderEffect(byte[] ledData)
        {
            int totalLeds = WorkspaceWidth; // Assuming RGB for now...
            float[] ledColours = new float[ledData.Length];

            using (var buffer = GraphicsDevice.GetDefault().AllocateReadWriteBuffer(ledColours))
            {
                var shader = new Shaders.StaticRed(buffer, totalLeds);
                GraphicsDevice.GetDefault().For(totalLeds, shader);

                // Retrieve processed data from GPU
                buffer.CopyTo(ledColours);

            }

            byte[] processedData = ledColours.Select(x => (byte)(x * 255)).ToArray();

            try
            {
                UpdatePatch(processedData, totalLeds);
            }
            catch
            {

            }
        }

        private void TestHalfRedHalfBlue()
        {
            byte[] currentLedData = Patch.GetCurrentDMXData();
            HalfRedHalfBlue(currentLedData);
        }

        private void HalfRedHalfBlue(byte[] ledData)
        {
            float[] ledColours = new float[ledData.Length];
            using var graphicsDevice = GraphicsDevice.GetDefault();

            using var buffer = graphicsDevice.AllocateReadWriteBuffer(ledColours);

            var shader = new Shaders.HalfRedHalfBlue(buffer, WorkspaceWidth);

            graphicsDevice.For(WorkspaceWidth, shader);

            buffer.CopyTo(ledColours);

            byte[] processedData = ledColours.Select(x => (byte)(x * 255)).ToArray();

            try
            {
                UpdatePatch(processedData, WorkspaceWidth * 3);
            }
            catch
            {

            }
        }


        private void TestHelloShader()
        {
            byte[] currentLedData = Patch.GetCurrentDMXData();
            HelloShader(currentLedData);
        }

        private void HelloShader(byte[] ledData)
        {
            if (IsProcessing)
            {
                return;
            }
            IsProcessing = true;
            // Debug.WriteLine($"Starting processing at {DateTime.Now}");

            int totalLEDs = Patch.GetTotalLEDs();
            float[] LEDColours = new float[ledData.Length];

            using var graphicsDevice = GraphicsDevice.GetDefault();

            using var buffer = graphicsDevice.AllocateReadWriteBuffer(LEDColours);

            var shader = new Shaders.HelloShader(
                buffer,
                (float)DateTime.Now.TimeOfDay.TotalMilliseconds
                );
            try
            {
                graphicsDevice.For(WorkspaceWidth, shader);

                buffer.CopyTo(LEDColours);

                byte[] processedData = LEDColours.Select(x => (byte)(x * 255)).ToArray();

                UpdatePatch(processedData, totalLEDs);
            }
            catch (Exception e)
            {
                // Debug.WriteLine(e.Message.ToString());
                return;
            }
            finally
            {
                IsProcessing = false;
                buffer.Dispose();
            }

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
            if (IsProcessing)
            {
                return;
            }
            IsProcessing = true;

            int totalLeds = Patch.GetTotalLEDs();
            float[] ledColours = new float[ledData.Length];

            var graphicsDevice = GraphicsDevice.GetDefault();

            try
            {
                UpdateBuffers(magnitudeBuffer, frequencyBuffer, totalLeds);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message.ToString());
                return;
            }

            try
            {
                var shader = new Shaders.WaveShader(_ledBuffer, _magBuffer, _freqBuffer);

                graphicsDevice.For(WorkspaceWidth, shader);

                _ledBuffer.CopyTo(ledColours);

                byte[] processedData = ledColours.Select(x => (byte)(x * 255)).ToArray();

                UpdatePatch(processedData, totalLeds);

            }
            catch(Exception e)
            {
                // Debug.WriteLine(e.Message.ToString());
            }
            finally
            {
                IsProcessing = false;
            }
            
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
            //int totalLEDs = ledData.Length / 3;
            //float[] LEDColours = new float[ledData.Length];
            //float force = 3.0f;

            //var graphicsDevice = GraphicsDevice.GetDefault();

            //var buffer = graphicsDevice.AllocateReadWriteBuffer(LEDColours);
            //var magBuffer = graphicsDevice.AllocateReadOnlyBuffer(magnitudeBuffer);
            //var freqBuffer = graphicsDevice.AllocateReadOnlyBuffer(frequencyBuffer);

            //try
            //{
            //    Debug.WriteLine("Trying to run FFTGlow");
            //    var shader = new Shaders.FFTGlow(
            //        buffer,
            //        magBuffer,
            //        freqBuffer,
            //        WorkspaceWidth,
            //        WorkspaceHeight,
            //        force
            //    );

            //    //GraphicsDevice.GetDefault().For(WorkspaceWidth, shader);
            //    graphicsDevice.For(WorkspaceWidth, WorkspaceHeight, shader );

            //    buffer.CopyTo(LEDColours, 0);

            //    byte[] processedData = LEDColours.Select(x => (byte)(x * 255)).ToArray();

            //    UpdatePatch(processedData, totalLEDs);
            //}
            //catch(Exception e)
            //{
            //    Debug.WriteLine($"{e.Message}");
            //}
            //finally
            //{
            //    // Dispose buffers to avoid memory leaks
            //    buffer.Dispose();
            //    magBuffer.Dispose();
            //    freqBuffer.Dispose();
            //}
        }


        private void StartAverageColour()
        {
            AudioProcessor.OnAudioBufferEvent += AverageColourShader;
            AudioProcessor.Start();
        }

        private void AverageColourShader(float[] magnitudebuffer, float[] frequencyBuffer)
        {
            byte[] currentLEDData = Patch.GetCurrentDMXData();
            AverageColourEffect(currentLEDData, magnitudebuffer, frequencyBuffer);
        }

        private void AverageColourEffect(byte[] ledData, float[] magnitudebuffer, float[] frequencyBuffer)
        {
            if (IsProcessing)
            {
                return;
            }
            IsProcessing = true;
            // Debug.WriteLine($"Starting processing at {DateTime.Now}");

            int totalLEDs = Patch.GetTotalLEDs();
            float[] LEDColours = new float[ledData.Length];

            var graphicsDevice = GraphicsDevice.GetDefault();

            try
            {
                UpdateBuffers(magnitudebuffer, frequencyBuffer, totalLEDs);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message.ToString());
                return;
            }

            int gain = 10000;

            var shader = new Shaders.AverageColourShader(
                    _ledBuffer,
                    _magBuffer,
                    WorkspaceWidth,
                    6
                    );

            graphicsDevice.For(WorkspaceWidth, shader);

            if (_ledBuffer.Length > LEDColours.Length)
            {
                throw new ArgumentOutOfRangeException($"Buffer size: {_ledBuffer.Length} exceeds destination array length: {nameof(LEDColours)}");
            }

            try
            {
                _ledBuffer.CopyTo(LEDColours, 0, 0, _ledBuffer.Length);

                byte[] processedData = LEDColours.Select(x => (byte)(x * 255)).ToArray();

                UpdatePatch(processedData, totalLEDs);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message.ToString());
            }
            finally
            {
                // Debug.WriteLine($"Finished processing at {DateTime.Now}");
                IsProcessing = false;
            }

        }



        private void UpdatePatch(byte[] processedData, int totalLeds)
        {
            Debug.WriteLine("Updating Patch Data");
            for (int i = 0; i < totalLeds - 1; i += 3)
            {
                byte[] CurrentLedData = new byte[3];

                for (int j = 0; j < 3; j++)
                {
                    CurrentLedData[j] = processedData[i + j];
                }

                try
                {
                    var coordinate = Patch.GetLEDLocation(i + 1);
                    Patch.SetDMXValue(coordinate.Item1, coordinate.Item2, CurrentLedData);
                    // Debug.WriteLine($"Setting ({coordinate.Item1}, {coordinate.Item2}) to {CurrentLedData[0]} {CurrentLedData[1]} {CurrentLedData[2]}");
                }
                catch(Exception e)
                {
                    Debug.WriteLine($"Patch data exception: {e.Message}");
                }

            }

            Debug.WriteLine("Refreshing the patch");

            Patch.SendDMX();
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

