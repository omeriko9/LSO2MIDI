using System;
using System.IO;

namespace ConvertLSO
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: ConvertLSO.exe [LSO File] [MIDI Output File]");
                return;
            }

            var lsoFile = args[0];
            var midiFile = args[1];

            LSOConverter.ParseTracks(lsoFile, midiFile);

            Console.WriteLine("");
            Console.WriteLine("Done.");
        }

    }
}
