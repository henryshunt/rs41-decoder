using System;

namespace RSDecoder.RS41
{
    /// <summary>
    /// An exception that is thrown by a <see cref="Demodulator"/>.
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
