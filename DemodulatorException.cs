using System;

namespace Rs41Decoder
{
    /// <summary>
    /// An exception that is thrown by an <see cref="IDemodulator"/>.
    /// </summary>
    public class DemodulatorException : Exception
    {
        /// <summary>
        /// Initialises a new instance of the <see cref="DemodulatorException"/> class.
        /// </summary>
        /// <param name="message"></param>
        public DemodulatorException(string message)
            : base(message) { }
    }
}
