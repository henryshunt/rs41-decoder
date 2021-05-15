using NAudio.Wave;
using System.Collections.Concurrent;
using System.Threading;

namespace Rs41Decoder
{
    /// <summary>
    /// Represents a demodulator for the RS41 radiosonde where the data source is an audio input device.
    /// </summary>
    internal class LiveDemodulator : WavDemodulator, IDemodulator
    {
        private const int SAMPLE_RATE = 37500;

        private readonly int deviceNumber;
        private WaveInEvent? audioDevice = null;
        private ConcurrentQueue<byte> audioBytes = new ConcurrentQueue<byte>();

        /// <summary>
        /// Initialises a new instance of the <see cref="FileDemodulator"/> class.
        /// </summary>
        /// <param name="deviceNumber">
        /// 
        /// </param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/>, used for cancelling the demodulation.
        /// </param>
        public LiveDemodulator(int deviceNumber, CancellationToken cancellationToken)
            : base(cancellationToken)
        {
            this.deviceNumber = deviceNumber;

            samplesPerDemodBit = (double)SAMPLE_RATE / Constants.BAUD_RATE;
        }

        /// <summary>
        /// Opens the demodulator.
        /// </summary>
        public void Open()
        {
            audioDevice = new WaveInEvent()
            {
                DeviceNumber = deviceNumber,
                WaveFormat = new WaveFormat(
                    SAMPLE_RATE, bitsPerWavSample, numberOfChannels)
            };

            audioDevice.DataAvailable += Stream_DataAvailable;
            audioDevice.StartRecording();
        }

        private void Stream_DataAvailable(object? sender, WaveInEventArgs e)
        {
            foreach (byte b in e.Buffer)
                audioBytes.Enqueue(b);
        }

        /// <summary>
        /// Closes the demodulator.
        /// </summary>
        public void Close()
        {
            audioDevice?.StopRecording();
        }

        protected override byte ReadWavByte()
        {
            while (true)
            {
                if (!audioBytes.IsEmpty && audioBytes.TryDequeue(out byte b))
                    return b;
            }
        }

        /// <summary>
        /// Disposes the demodulator.
        /// </summary>
        public void Dispose()
        {
            Close();
            audioDevice?.Dispose();
        }
    }
}
