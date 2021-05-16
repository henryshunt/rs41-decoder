using NAudio.Wave;
using System;
using System.Collections.Concurrent;

namespace Rs41Decoder.Demodulation
{
    /// <summary>
    /// Represents a demodulator for the RS41 radiosonde where the data source is an audio input device.
    /// </summary>
    internal class LiveDemodulator : DemodulatorBase
    {
        /// <summary>
        /// The number of the audio input device to use as the data source.
        /// </summary>
        private readonly int deviceNumber;

        private WaveInEvent? audioDevice = null;

        /// <summary>
        /// Buffers bytes received from the audio input device.
        /// </summary>
        private readonly ConcurrentQueue<byte> audioBuffer = new ConcurrentQueue<byte>();

        /// <summary>
        /// The frequency to sample the audio data at, in samples per second.
        /// </summary>
        private const int SAMPLE_RATE = 37500;

        /// <summary>
        /// Initialises a new instance of the <see cref="FileDemodulator"/> class.
        /// </summary>
        /// <param name="deviceNumber">
        /// The number of the audio input device to use as the data source. The device should output mono-channel audio
        /// at 16 bits per sample.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <paramref name="deviceNumber"/> is less than zero.
        /// </exception>
        public LiveDemodulator(int deviceNumber)
        {
            if (deviceNumber < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(deviceNumber),
                    nameof(deviceNumber) + " is less than zero");
            }

            this.deviceNumber = deviceNumber;
            samplesPerDemodBit = (double)SAMPLE_RATE / Constants.BAUD_RATE;
        }

        /// <summary>
        /// Opens the demodulator.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the demodulator is already open.
        /// </exception>
        public override void Open()
        {
            if (IsOpen)
                throw new InvalidOperationException("The demodulator is already open");
            IsOpen = true;

            audioDevice = new WaveInEvent()
            {
                DeviceNumber = deviceNumber,
                WaveFormat = new WaveFormat(SAMPLE_RATE, bitsPerWavSample, numberOfChannels)
            };

            audioDevice.DataAvailable += AudioDevice_DataAvailable;
            audioDevice.StartRecording();
        }

        public override void Close()
        {
            audioDevice?.Dispose();
            audioBuffer.Clear();
            IsOpen = false;
        }

        private void AudioDevice_DataAvailable(object? sender, WaveInEventArgs e)
        {
            foreach (byte b in e.Buffer)
                audioBuffer.Enqueue(b);
        }

        /// <summary>
        /// Reads a byte from the WAV data, hanging indefinitely until a byte is available.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the demodulator is not open.
        /// </exception>
        protected override byte ReadWavByte()
        {
            while (true)
            {
                if (!IsOpen)
                    throw new InvalidOperationException("The demodulator is not open");

                if (audioBuffer.TryDequeue(out byte b))
                    return b;
            }
        }
    }
}
