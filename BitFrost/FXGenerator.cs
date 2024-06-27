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


        // Not Used, consider adding functionality later to pass colours into shaders or as an overlay
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
                case "truchet":
                    Debug.WriteLine("Triggering Truchet");
                    if (CurrentEffect != StartTruchetShader)
                    {
                        CurrentEffect = StartTruchetShader;
                    }
                    CurrentEffect?.Invoke();
                    break;
                case "kaleidoscope":
                    Debug.WriteLine("Triggering Kaleidoscope");
                    if (CurrentEffect != StartKaleidoscope)
                    {
                        CurrentEffect = StartKaleidoscope;
                    }
                    CurrentEffect?.Invoke();
                    break;
                case "kaleidoscope-audio":
                    Debug.WriteLine("Triggering Kaleidoscope Audio");
                    if (CurrentEffect != StartKaleidoscopeAudio)
                    {
                        CurrentEffect = StartKaleidoscopeAudio;
                    }
                    CurrentEffect?.Invoke();
                    break;
                case "level-meter":
                    Debug.WriteLine("Triggering level meter");
                    if (CurrentEffect != StartLevelMeter)
                    {
                        CurrentEffect = StartLevelMeter;
                    }
                    CurrentEffect?.Invoke();
                    break;
                case "spectral-test":
                    Debug.WriteLine("Triggering spectral test");
                    if (CurrentEffect != CpuFlashTest)
                    {
                        CurrentEffect = CpuFlashTest;
                    }
                    CurrentEffect?.Invoke();
                    break;
                case "sound-eclipse":
                    Debug.WriteLine("Triggering sound eclipse");
                    if (CurrentEffect != StartSoundEclipse)
                    {
                        CurrentEffect = StartSoundEclipse;
                    }
                    CurrentEffect?.Invoke();
                    break;
                case "warm-white":
                    Debug.WriteLine("Triggering warm white");
                    if (CurrentEffect != StartWarmWhite)
                    {
                        CurrentEffect = StartWarmWhite;
                    }
                    CurrentEffect?.Invoke();
                    break;
            }     
        }

        private void StartWarmWhite()
        {
            byte[] ledData = Patch.GetCurrentDMXData();  

            int totalLEDs = Patch.GetTotalLEDs();
            float[] LEDColours = new float[ledData.Length];

            var graphicsDevice = GraphicsDevice.GetDefault();

            var ledBuffer = graphicsDevice.AllocateReadWriteBuffer(LEDColours);


            var shader = new Shaders.WarmWhite(
                ledBuffer
                );

            graphicsDevice.For(WorkspaceWidth, shader);

            ledBuffer.CopyTo(LEDColours, 0, 0, ledBuffer.Length);

            byte[] processedData = LEDColours.Select(x => (byte)(x * 255)).ToArray();

            UpdatePatch(processedData, totalLEDs);

            IsProcessing = false;
        }


        public void UpdateBuffers(float[] magnitudeBuffer, float[] frequencyBuffer, int totalLEDs)
        {
            float[] ledColours = new float[totalLEDs];
            var graphicsDevice = GraphicsDevice.GetDefault();

            _ledBuffer = graphicsDevice.AllocateReadWriteBuffer(ledColours);
            _magBuffer = graphicsDevice.AllocateReadOnlyBuffer(magnitudeBuffer);
            _freqBuffer = graphicsDevice.AllocateReadOnlyBuffer(frequencyBuffer);
        }


        private void CpuFlashTest()
        {
            AudioProcessor.OnAudioBufferEvent += AudioAvailable;
            AudioProcessor.Start();
        }

        private void AudioAvailable(float[] magnitudeBuffer, float[] frequencyBuffer)
        {
            byte[] currentLedData = Patch.GetCurrentDMXData();

            float spectralCentroid = ComputeSpectralCentroid(magnitudeBuffer, frequencyBuffer);

            int freq = (int)Utils.Lerp(spectralCentroid, 0f, 20000f, 0f, 255f);

            byte[] colour = new byte[3];

            if (freq < 255 / 2)
            {
                colour[0] = (byte)((byte)freq * 2);
                colour[1] = (byte)freq;
            }
            else
            {
                colour[1] = (byte)freq;
                colour[2] = (byte)((byte)freq / 2);
            }

            for (int i = 0; i < currentLedData.Length; i += 3)
            {
                if (i + 3 < currentLedData.Length)
                {
                    currentLedData[i] = colour[0];
                    currentLedData[i + 1] = colour[1];
                    currentLedData[i + 2] = colour[2];
                }
                
            }

            Debug.WriteLine($"Spectral Centroid: {spectralCentroid}");

            UpdatePatch(currentLedData, currentLedData.Length / 3);

        }

        private float ComputeSpectralCentroid(float[] magnitudes, float[] frequencies)
        {
            float weightedSum = 0.0f;
            float totalMagnitude = 0.0f;

            for (int i = 0; i < frequencies.Length; i++)
            {
                if (magnitudes[i] > 0)
                {
                    weightedSum += frequencies[i] * magnitudes[i];
                    totalMagnitude += magnitudes[i];
                }
            }

            if (totalMagnitude == 0.0f)
            {
                return 0.0f;
            }

            float spectralCentroid = weightedSum / totalMagnitude;
            return spectralCentroid;
        }


        private void StartLevelMeter()
        {
            AudioProcessor.OnAudioBufferEvent += LevelMeterEffect;
            AudioProcessor.Start();
        }

        private void LevelMeterEffect(float[] magnitudeBuffer, float[] frequencyBuffer) 
        {
            byte[] currentLedData = Patch.GetCurrentDMXData();
            LevelMeterShader(currentLedData, magnitudeBuffer, frequencyBuffer);
        }

        private void LevelMeterShader(byte[] ledData, float[] magnitudeBuffer, float[] frequencyBuffer)
        {
            int totalLEDs = Patch.GetTotalLEDs();
            float[] LEDColours = new float[ledData.Length];

            var graphicsDevice = GraphicsDevice.GetDefault();

            try
            {
                UpdateBuffers(magnitudeBuffer, frequencyBuffer, totalLEDs);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message.ToString());
                return;
            }

            var shader = new Shaders.LevelMeter(
                _ledBuffer,
                _magBuffer,
                _freqBuffer,
                WorkspaceWidth,
                6
                );

            graphicsDevice.For(WorkspaceWidth, WorkspaceHeight, shader);

            _ledBuffer.CopyTo(LEDColours, 0, 0, _ledBuffer.Length);

            byte[] processedData = LEDColours.Select(x => (byte)(x * 255)).ToArray();

            UpdatePatch(processedData, totalLEDs);

            IsProcessing = false;
        }



        private void StartSoundEclipse()
        {
            AudioProcessor.OnAudioBufferEvent += LevelMeterEffect;
            AudioProcessor.Start();
        }

        private void SoundEclipseEffect(float[] magnitudeBuffer, float[] frequencyBuffer)
        {
            byte[] currentLedData = Patch.GetCurrentDMXData();
            SoundEclipseShader(currentLedData, magnitudeBuffer, frequencyBuffer);
        }

        private void SoundEclipseShader(byte[] ledData, float[] magnitudeBuffer, float[] frequencyBuffer)
        {
            int totalLEDs = Patch.GetTotalLEDs();
            float[] LEDColours = new float[ledData.Length];

            var graphicsDevice = GraphicsDevice.GetDefault();

            try
            {
                UpdateBuffers(magnitudeBuffer, frequencyBuffer, totalLEDs);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message.ToString());
                return;
            }

            var shader = new Shaders.SoundEclipse(
                _ledBuffer,
                _magBuffer,
                _freqBuffer,
                WorkspaceWidth,
                WorkspaceHeight,
                (float)DateTime.Now.Millisecond,
                64.0f,
                0.6f,
                0.2f,
                0.5f
                );

            graphicsDevice.For(WorkspaceWidth, WorkspaceHeight, shader);

            _ledBuffer.CopyTo(LEDColours, 0, 0, _ledBuffer.Length);

            byte[] processedData = LEDColours.Select(x => (byte)(x * 255)).ToArray();

            UpdatePatch(processedData, totalLEDs);

            IsProcessing = false;
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



        private void StartKaleidoscopeAudio()
        {
            AudioProcessor.OnAudioBufferEvent += KaleidoscopeAudioEffect;
            AudioProcessor.Start();
        }

        private void KaleidoscopeAudioEffect(float[] magnitudeBuffer, float[] frequencyBuffer)
        {
            byte[] currentLedData = Patch.GetCurrentDMXData();
            KaleidoscopeAudioShader(currentLedData, magnitudeBuffer, frequencyBuffer);
        }

        private void KaleidoscopeAudioShader(byte[] ledData, float[] magnitudeBuffer, float[] frequencyBuffer)
        {
            int totalLEDs = Patch.GetTotalLEDs();
            float[] LEDColours = new float[ledData.Length];

            var graphicsDevice = GraphicsDevice.GetDefault();

            try
            {
                UpdateBuffers(magnitudeBuffer, frequencyBuffer, totalLEDs);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message.ToString());
                return;
            }

            var shader = new Shaders.KaleidoscopeAudio(
                _ledBuffer,
                _magBuffer,
                _freqBuffer,
                (float)DateTime.Now.Millisecond,
                WorkspaceWidth,
                WorkspaceHeight
                );

            graphicsDevice.For(WorkspaceWidth, WorkspaceHeight, shader);

            _ledBuffer.CopyTo(LEDColours, 0, 0, _ledBuffer.Length);

            byte[] processedData = LEDColours.Select(x => (byte)(x * 255)).ToArray();

            UpdatePatch(processedData, totalLEDs);

            IsProcessing = false;
        }


        private void StartKaleidoscope()
        {
            byte[] currentLedData = Patch.GetCurrentDMXData();
            KaleidoscopeShader(currentLedData);
        }

        private void KaleidoscopeShader(byte[] ledData)
        {
            float[] ledColours = new float[ledData.Length];
            var graphicsDevice = GraphicsDevice.GetDefault();

            var buffer = graphicsDevice.AllocateReadWriteBuffer(ledColours);

            var shader = new Shaders.Kaleidoscope(
                buffer,
                (float)DateTime.Now.Millisecond,
                WorkspaceWidth,
                WorkspaceHeight
                );

            graphicsDevice.For(WorkspaceWidth, WorkspaceHeight, shader);

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



        private void StartTruchetShader()
        {
            byte[] currentLedData = Patch.GetCurrentDMXData();
            TruchetShader(currentLedData);
            
        }

        private void TruchetShader(byte[] ledData)
        {
            float[] ledColours = new float[ledData.Length];
            var graphicsDevice = GraphicsDevice.GetDefault();

            var buffer = graphicsDevice.AllocateReadWriteBuffer(ledColours);

            var shader = new Shaders.Truchet(
                buffer,
                (float)DateTime.Now.Millisecond,
                WorkspaceWidth,
                WorkspaceHeight
                );

            graphicsDevice.For(WorkspaceWidth, WorkspaceHeight, shader);

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



        public void UpdatePatch(byte[] processedData, int totalLeds)
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
                    // Debug.WriteLine($"Setting ({coordinate.Item1}, {coordinate.Item2}) to {CurrentLedData[0]} {CurrentLedData[1]} {CurrentLedData[2]}");
                    Patch.SetDMXValue(coordinate.Item1, coordinate.Item2, CurrentLedData);
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

