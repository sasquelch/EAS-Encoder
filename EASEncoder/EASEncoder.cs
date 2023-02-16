using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Speech.AudioFormat;
using System.Speech.Synthesis;
using System.Text;
using EASEncoder.Models;
using NAudio.Lame;
using NAudio.Wave;

namespace EASEncoder
{
    public static class EASEncoder
    {
        private static readonly SAMEAudioBit Mark = new SAMEAudioBit(2083, (decimal) 0.00192);
        private static readonly SAMEAudioBit Space = new SAMEAudioBit(1563, (decimal) 0.00192);
        private static int[] _headerSamples;
        private static int TotalHeaderSamples => _headerSamples.Length;
        private static List<int> _silenceSamples;
        private static int[] _eomSamples;
        private static int TotalEomSamples => _eomSamples.Length;
        private static byte[] _ebsTonesStream;
        private static int _ebsToneSamples = 352800;
        private static byte[] _nwsTonesStream;
        private static int _nwstoneSamples = 352800;
        private static byte[] _announcementStream;
        private static int _announcementSamples;
        private static readonly int totalSilenceSamples = 176400;
        
        
        private static int _totalSamples;
        private static bool _useEbsTone = true;
        private static bool _useNwsTone;
        private static string _announcement = string.Empty;
        private static string _fileName = string.Empty;
        private static string _outputPath = "Output\\";

        private static Dictionary<decimal, List<int>> headerByteCache = new Dictionary<decimal, List<int>>();

        public static void CreateNewMessage(EASMessage message, bool ebsTone = true, bool nwsTone = false,
            string announcement = "", string fileName = "", bool openFile = false)
        {
            //headerByteCache = new Dictionary<decimal, List<int>>();
            _useEbsTone = ebsTone;
            _useNwsTone = nwsTone;
            _announcement = announcement;
            _fileName = fileName;

            _headerSamples = new int[0];
            var byteArray = Encoding.Default.GetBytes(message.ToSameHeaderString());
            var volume = 5000;

            var byteSpec = new List<SameWavBit>();
            byte thisByte;
            SAMEAudioBit thisBit;

            for (var i = 0; i < byteArray.Length; i++)
            {
                thisByte = byteArray[i];

                for (var e = 0; e < 8; e++)
                {
                    thisBit = ((thisByte & (short) Math.Pow(2, e)) != 0 ? Mark : Space);
                    byteSpec.Add(new SameWavBit(thisBit.frequency, thisBit.length, volume));
                }
            }

            foreach (var currentSpec in byteSpec)
            {
                int[] returnedBytes = RenderTone(currentSpec);
                int[] c = new int[_headerSamples.Length + returnedBytes.Length];
                Array.Copy(_headerSamples, 0, c, 0, _headerSamples.Length);
                Array.Copy(returnedBytes, 0, c, _headerSamples.Length, returnedBytes.Length);

                _headerSamples = c;
            }

            _eomSamples = new int[0];
            byteArray = Encoding.Default.GetBytes(message.ToSameEndOfMessageString());
            volume = 5000;

            byteSpec = new List<SameWavBit>();

            foreach (var t in byteArray)
            {
                thisByte = t;

                for (var e = 0; e < 8; e++)
                {
                    thisBit = ((thisByte & (short) Math.Pow(2, e)) != 0 ? Mark : Space);
                    byteSpec.Add(new SameWavBit(thisBit.frequency, thisBit.length, volume));
                }
            }

            foreach (var currentSpec in byteSpec)
            {
                int[] returnedBytes = RenderTone(currentSpec);
                int[] c = new int[_eomSamples.Length + returnedBytes.Length];
                Array.Copy(_eomSamples, 0, c, 0, _eomSamples.Length);
                Array.Copy(returnedBytes, 0, c, _eomSamples.Length, returnedBytes.Length);
                _eomSamples = c;
            }

            //1 second silence
            _silenceSamples = new List<int>();
            while (_silenceSamples.Count < 176400)
            {
                _silenceSamples.Add(0);
            }

            _ebsTonesStream = GenerateEbsTones();
            _ebsToneSamples = _ebsTonesStream.Length;

            _nwsTonesStream = GenerateNwsTones();
            _nwstoneSamples = _nwsTonesStream.Length;

            _totalSamples = (TotalHeaderSamples*3) + (totalSilenceSamples*7) + (TotalEomSamples*3) + (_ebsToneSamples*8) +
                           (_nwstoneSamples*8);

            _announcementStream =
                GenerateVoiceAnnouncement(announcement);
            _announcementSamples = _announcementStream.Length;

            //Create Output Folder if it doesn't exist
            if (!Directory.Exists(_outputPath))
            {
                Directory.CreateDirectory(_outputPath);
            }

            GenerateWavFile();
            GenerateMp3File();

            if (openFile)
            {
                Process.Start("explorer.exe",
                    "/select, \"" + _outputPath + _fileName + ".wav" + "\"");
            }
        }

        public static MemoryStream GetMemoryStreamFromNewMessage(EASMessage message, bool ebsTone = true,
            bool nwsTone = false, string announcement = "")
        {
            _useEbsTone = ebsTone;
            _useNwsTone = nwsTone;
            _announcement = announcement;

            _headerSamples = new int[0];
            var byteArray = Encoding.Default.GetBytes(message.ToSameHeaderString());
            var volume = 5000;

            var byteSpec = new List<SameWavBit>();
            byte thisByte;
            SAMEAudioBit thisBit;

            //TODO: Extremely slow - fix!
            foreach (var t in byteArray)
            {
                thisByte = t;

                for (var e = 0; e < 8; e++)
                {
                    thisBit = ((thisByte & (short) Math.Pow(2, e)) != 0 ? Mark : Space);
                    byteSpec.Add(new SameWavBit(thisBit.frequency, thisBit.length, volume));
                }
            }

            foreach (var currentSpec in byteSpec)
            {
                int[] returnedBytes = RenderTone(currentSpec);
                int[] c = new int[_headerSamples.Length + returnedBytes.Length];
                Array.Copy(_headerSamples, 0, c, 0, _headerSamples.Length);
                Array.Copy(returnedBytes, 0, c, _headerSamples.Length, returnedBytes.Length);

                _headerSamples = c;
            }

            _eomSamples = new int[0];
            byteArray = Encoding.Default.GetBytes(message.ToSameEndOfMessageString());
            volume = 5000;

            byteSpec = new List<SameWavBit>();

            foreach (var t in byteArray)
            {
                thisByte = t;

                for (var e = 0; e < 8; e++)
                {
                    thisBit = ((thisByte & (short) Math.Pow(2, e)) != 0 ? Mark : Space);
                    byteSpec.Add(new SameWavBit(thisBit.frequency, thisBit.length, volume));
                }
            }

            foreach (var currentSpec in byteSpec)
            {
                int[] returnedBytes = RenderTone(currentSpec);
                int[] c = new int[_eomSamples.Length + returnedBytes.Length];
                Array.Copy(_eomSamples, 0, c, 0, _eomSamples.Length);
                Array.Copy(returnedBytes, 0, c, _eomSamples.Length, returnedBytes.Length);
                _eomSamples = c;
            }

            //1 second silence
            _silenceSamples = new List<int>();
            while (_silenceSamples.Count < 176400)
            {
                _silenceSamples.Add(0);
            }

            _ebsTonesStream = GenerateEbsTones();
            _ebsToneSamples = _ebsTonesStream.Length;

            _nwsTonesStream = GenerateNwsTones();
            _nwstoneSamples = _nwsTonesStream.Length;

            _totalSamples = (TotalHeaderSamples*3) + (totalSilenceSamples*7) + (TotalEomSamples*3) + (_ebsToneSamples*8) +
                           (_nwstoneSamples*8);

            _announcementStream =
                GenerateVoiceAnnouncement(announcement);
            _announcementSamples = _announcementStream.Length;

            return GenerateMemoryStream();
        }

        private static int[] RenderTone(SameWavBit byteSpec)
        {
            var computedSamples = new List<int>();
            for (var i = 0; i < (44100*byteSpec.length); i++)
            {
                    for (var c = 0; c < 2; c++)
                    {
                        var sample = (decimal) (byteSpec.volume*Math.Sin((2*Math.PI)*(i/44100d)*byteSpec.frequency));
                        if (headerByteCache.ContainsKey(sample))
                        {
                            computedSamples.AddRange(headerByteCache[sample]);
                        }
                        else
                        {
                            List<int> thisSample = PackBytes("v", sample);
                            computedSamples.AddRange(thisSample);
                            headerByteCache.Add(sample, thisSample);
                        }
                    }

            }
            return computedSamples.ToArray();
        }

        private static List<int> PackBytes(string format, decimal sample)
        {
            var output = new List<int>();
            foreach (var c in format)
            {
                var arg = sample;

                switch (c)
                {
                    case 'v': // little-endian unsigned short
                        output.Add(((int) arg & 255));
                        output.Add((((int) arg >> 8) & 255));

                        break;
                }
            }
            return output;
        }

        private static void GenerateWavFile()
        {
            var f = new FileStream(_outputPath + _fileName + ".wav", FileMode.Create);
            using (var wr = new BinaryWriter(f))
            {
                GenerateMemoryStream().CopyTo(wr.BaseStream);
            }
            f.Close();
        }

        private static void GenerateMp3File()
        {

            using (var reader = new WaveFileReader(GenerateMemoryStream()))
            using (var writer = new LameMP3FileWriter(_outputPath + _fileName + ".mp3", reader.WaveFormat, LAMEPreset.ABR_32))
                reader.CopyTo(writer);
        }

        private static MemoryStream GenerateMemoryStream()
        {
            _totalSamples = (TotalHeaderSamples*3) + (totalSilenceSamples*8) + (TotalEomSamples*3); //SAME Header & EOM

            //EBS Tone
            if (_useEbsTone)
            {
                _totalSamples += (_ebsToneSamples*8) + totalSilenceSamples;
            }

            //NWS Tone
            if (_useNwsTone)
            {
                _totalSamples += (_nwstoneSamples*8) + totalSilenceSamples;
            }

            //Spoken announcement
            if (!string.IsNullOrEmpty(_announcement))
            {
                _totalSamples += _announcementSamples;
            }

            uint sampleRate = 44100;
            ushort channels = 2;
            ushort bitsPerSample = 16;
            var bytesPerSample = (ushort) (bitsPerSample/8);

            var f = new MemoryStream();
            var wr = new BinaryWriter(f);
            wr.Write("RIFF".ToArray());
            wr.Write(36 + _totalSamples*channels*bytesPerSample);
            wr.Write("WAVE".ToArray());
            //    // Sub-chunk 1.
            //    // Sub-chunk 1 ID.
            wr.Write("fmt ".ToArray());
            wr.Write(BitConverter.GetBytes(16), 0, 4);

            //    // Audio format (floating point (3) or PCM (1)). Any other format indicates compression.
            wr.Write(BitConverter.GetBytes((ushort) (1)), 0, 2);

            //    // Channels.
            wr.Write(BitConverter.GetBytes(channels), 0, 2);

            //    // Sample rate.
            wr.Write(BitConverter.GetBytes(sampleRate), 0, 4);

            //    // Bytes rate.
            wr.Write(BitConverter.GetBytes(sampleRate*channels*bytesPerSample), 0, 4);

            //    // Block align.
            wr.Write(BitConverter.GetBytes(channels*bytesPerSample), 0, 2);

            //    // Bits per sample.
            wr.Write(BitConverter.GetBytes(bitsPerSample), 0, 2);

            //    // Sub-chunk 2.
            wr.Write("data".ToArray());

            //    // Sub-chunk 2 size.
            wr.Write(BitConverter.GetBytes(_totalSamples/channels*bytesPerSample));

            foreach (var thisChar in _silenceSamples)
            {
                wr.Write((byte)(thisChar));
            }

            var loopCount = 0;
            while (loopCount < 3)
            {
                foreach (var thisChar in _headerSamples)
                {
                    wr.Write((byte)(thisChar));
                }

                foreach (var thisChar in _silenceSamples)
                {
                    wr.Write((byte) (thisChar));
                }
                loopCount++;
            }


            //EBS Tone
            if (_useEbsTone)
            {
                var seconds = 0;
                while (seconds < 8)
                {
                    loopCount = 0;

                    while (loopCount < _ebsTonesStream.Length)
                    {
                        wr.Write(_ebsTonesStream[loopCount]);
                        loopCount++;
                    }
                    seconds++;
                }

                foreach (var thisChar in _silenceSamples)
                {
                    wr.Write((byte) (thisChar));
                }
            }

            //NWS Tone
            if (_useNwsTone)
            {
                var seconds = 0;
                while (seconds < 8)
                {
                    loopCount = 0;

                    while (loopCount < _nwsTonesStream.Length)
                    {
                        wr.Write(_nwsTonesStream[loopCount]);
                        loopCount++;
                    }
                    seconds++;
                }

                foreach (var thisChar in _silenceSamples)
                {
                    wr.Write((byte) (thisChar));
                }
            }

            //Spoken announcement
            if (!string.IsNullOrEmpty(_announcement))
            {
                loopCount = 0;
                while (loopCount < _announcementSamples)
                {
                    wr.Write(_announcementStream[loopCount]);
                    loopCount++;
                }
            }

            loopCount = 0;
            while (loopCount < 3)
            {
                foreach (var thisChar in _silenceSamples)
                {
                    wr.Write((byte) (thisChar));
                }

                foreach (var thisChar in _eomSamples)
                {
                    wr.Write((byte)(thisChar));
                }

                loopCount++;
            }

            foreach (var thisChar in _silenceSamples)
            {
                wr.Write((byte) (thisChar));
            }
            wr.Flush();
            f.Position = 0;
            return f;
        }

        private static byte[] GenerateEbsTones()
        {
            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);
            var samplesPerSecond = 44100;
            var samples = samplesPerSecond*2;
            double ampl = 5000;
            var concert = 1.0;
            for (var i = 0; i < samples/2; i++)
            {
                if (i%2 == 0)
                {
                    var t = i/(double) samplesPerSecond;
                    var s = (short) (ampl*(Math.Sin(t*853.0*concert*2.0*Math.PI)));
                    writer.Write(s);
                    writer.Write(s);
                }
                else
                {
                    var t = i/(double) samplesPerSecond;
                    var s = (short) (ampl*(Math.Sin(t*960.0*concert*2.0*Math.PI)));
                    writer.Write(s);
                    writer.Write(s);
                }
            }
            writer.Close();
            stream.Close();
            return stream.ToArray();
        }

        private static byte[] GenerateNwsTones()
        {
            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);
            var samplesPerSecond = 44100;
            var samples = samplesPerSecond*2;
            double ampl = 5000;
            var concert = 1.0;
            for (var i = 0; i < samples/2; i++)
            {
                var t = i/(double) samplesPerSecond;
                var s = (short) (ampl*(Math.Sin(t*1050.0*concert*2.0*Math.PI)));
                writer.Write(s);
                writer.Write(s);
            }
            writer.Close();
            stream.Close();
            return stream.ToArray();
        }

        private static byte[] GenerateVoiceAnnouncement(string announcement)
        {
            var synthesizer = new SpeechSynthesizer();
            var waveStream = new MemoryStream();
            var firstOrDefault = synthesizer.GetInstalledVoices()
                .FirstOrDefault(x => x.VoiceInfo.Name.ToUpper().Contains("DAVID"));
            if (firstOrDefault != null)
                synthesizer.SelectVoice(
                    firstOrDefault
                        .VoiceInfo.Name);
            synthesizer.SetOutputToAudioStream(waveStream,
                new SpeechAudioFormatInfo(EncodingFormat.Pcm,
                    44100, 16, 2, 176400, 2, null));
            synthesizer.Volume = 100;
            synthesizer.Rate = 1;
            synthesizer.Speak(announcement);
            synthesizer.SetOutputToNull();

            return waveStream.ToArray();
        }
    }
}