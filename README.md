# RS41 Radiosonde Decoder
An ongoing project to port the RS41 section of [zilog80's radiosonde decoding project](https://github.com/rs1729/RS) from C to C#.

The key goal is to port the decoder so it can be directly integrated with other C# programs, and to massively refactor the code to make it better organised (using OOP) and more understandable. The code is based entirely on zilog80's code, but has been written by me from the ground up.

This is work in progress, but decoding is implemented and working. It reads a WAV file and decodes most of the frame attributes and some of the subframe attributes. Further work is needed on decoding additional frame and subframe attributes, and on getting the error correction working. I also intend to add the ability to decode live audio from a sound device.

# Usage
	Rs41Decoder.Rs41Decoder decoder = new Rs41Decoder.Rs41Decoder("audio.wav");
	List<Rs41Frame> frames = await decoder.Decode();