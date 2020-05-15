using ConvertLSO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MIDIParser
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: MIDIParser.exe [MIDI File]");
                return;
            }

            var midiFile = args[0];

            Parse(midiFile);

            Console.WriteLine("");
            Console.WriteLine("Done.");
            Console.ReadKey();

        }

        static void Parse(string midiFile)
        {
            var file = new MIDIFile(midiFile);

            Console.WriteLine("MIDI Header");
            Console.WriteLine("-----------");
            Console.WriteLine($"Chunk Type: MThd, Size: {file.Header.Size.ToString()}, " +
                $"Format: {file.Header.Format.ToString("X2")}, NTRKS: {file.Header.NumberOfTracks.ToString("X2")}, " +
                $"Division: {file.Header.Division.ToString("X2")}");

            Console.WriteLine();
            foreach (var track in file.TrackChunks)
            {
                Console.WriteLine($"Track ({track.Name} - {track.Channel}), Size: {track.Size}, Total Events: {track.MTrkEvents.Count()}" +
                    $" Total Delta: {track.MTrkEvents.Sum(x => x.DeltaTimeNormal).ToString()}");

              // if (track.MTrkEvents.Sum(x => x.DeltaTimeNormal) > 1000000)
                    foreach (var ev in track.MTrkEvents)
                    {
                        Console.WriteLine($"\tEvent: {ev.Event.GetType().Name} \tDelta: {BitConverter.ToString(ev.DeltaTime).Replace("-", "")}\t");
                    }
            }


        }
    }
}
