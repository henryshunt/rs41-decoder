using System;

namespace Rs41Decoder
{
    /// <summary>
    /// Represents a demodulator for the RS41 radiosonde.
    /// </summary>
    internal interface IDemodulator : IDisposable
    {
        /// <summary>
        /// Opens the demodulator.
        /// </summary>
        public void Open();

        /// <summary>
        /// Closes the demodulator.
        /// </summary>
        public void Close();

        /// <summary>
        /// Reads a number of demodulated bits from the data source.
        /// </summary>
        /// <returns>
        /// The demodulated bits.
        /// </returns>
        public bool[] ReadDemodulatedBits();
    }
}
