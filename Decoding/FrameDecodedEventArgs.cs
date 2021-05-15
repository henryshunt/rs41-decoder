using System;

namespace Rs41Decoder.Decoding
{
    /// <summary>
    /// Represents event data about a frame that has been decoded.
    /// </summary>
    public class FrameDecodedEventArgs : EventArgs
    {
        /// <summary>
        /// The decoded frame.
        /// </summary>
        public Rs41Frame Frame { get; private set; }

        /// <summary>
        /// Initialises a new instance of the <see cref="FrameDecodedEventArgs"/> class.
        /// </summary>
        /// <param name="frame">
        /// The decoded frame.
        /// </param>
        public FrameDecodedEventArgs(Rs41Frame frame)
        {
            Frame = frame;
        }
    }
}
