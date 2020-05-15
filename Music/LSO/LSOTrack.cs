using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConvertLSO
{
    public class Track
    {
        const int TrackStart = 0x000300CA;
        const int TrackStart2 = 0x00030094;
        const int TrackStart3 = 0x000300F2;
        const int TrackLengthStart = 0x33;
        const int IsLoopTrackOffset = 0x43;
        const int TrackChannel = 0x7C;
        const int TrackPositionStart = 0x8B;
        const int EndSignal = 0x7FFFFFF1;
        const int MidiEndSignal2 = 0x7FFFFFFF;
        public int TrackMIDIStart { get; private set; } //= 0xCA;


        public int Length { get; private set; }
        public string TrackName { get; set; }
        public int TrackLength { get; set; }
        public int TrackPosition { get; set; }
        public bool IsLoopTrack { get; set; }

        public byte Channel { get; set; }

        public byte InitialInstrument { get; set; }

        public byte[] MIDIarray { get; set; }

        public List<MIDILogicEvent> MIDIEvents { get; private set; }

        public static bool IsMusicTrack(int header)
        {
            header &= 0xFFFFFF;
            return header == TrackStart || header == TrackStart2 || header == TrackStart3;
        }

        public static byte GetMidiStart(int header)
        {
            header &= 0xFFFFFF;
            return (byte)(header & 0xFF);
        }

        public Track(byte[] arr)
        {
            var header = arr.getInt(0);

            if (!IsMusicTrack(header))
                throw new Exception("Not Track Header!");

            TrackMIDIStart = GetMidiStart(header);

            TrackName = Encoding.ASCII.GetString(arr, 0xF, 0x1E);
            TrackName = TrackName.Trim('\0') + '\0';

            TrackLength = arr.getInt(TrackLengthStart);
            TrackPosition = arr.ToUInt24(TrackPositionStart);
            IsLoopTrack = arr[IsLoopTrackOffset] != 0;
            Channel = (byte)(arr.getInt(TrackChannel) / 4 - 0xF);
            MIDIarray = arr.SubPattern(TrackMIDIStart, EndSignal, MidiEndSignal2);
            MIDIEvents = new List<MIDILogicEvent>();

            var count = 0;

            while (count < MIDIarray.Length)
            {
                var ev = ParseMIDI(MIDIarray.Skip(count).ToArray());
                MIDIEvents.Add(ev);
                count += ev.EventSize;
            }

        }

        private MIDILogicEvent ParseMIDI(byte[] arr)
        {
            var toReturn = new MIDILogicEvent();
            toReturn.MIDIFirstByte = arr[0];

            //(toReturn.MIDIFirstByte <= 0x9F) ? 12 : 6;
            toReturn.EventSize = MIDILogicEvent.GetMIDILogicEventSize(toReturn.MIDIFirstByte);
            toReturn.RawBytes = arr.Take(toReturn.EventSize).ToArray();
            //(ushort)((ushort)(BitConverter.ToInt16(arr, 1) & 0xFFFF) - (ushort)0x9600);
            List<byte> posArr = new List<byte>();

            posArr.AddRange(arr.Skip(1).Take(3).ToArray());
            posArr.Add(0);
            //posArr.Reverse();
            var posInt = posArr.ToArray().ToUInt24(0);
            
            toReturn.Position = posInt >= 0x9600 ? posInt - 0x9600 : 0;// posInt;

            toReturn.MIDIThirdByte = arr[4];
            toReturn.MIDISecondByte = (byte)(arr[5] & 0x7F);

            if (toReturn.EventSize < 12)
                return toReturn;

            toReturn.MIDILength = 0x1000000 - (BitConverter.ToInt32(arr, 7) & 0xFFFFFF);

            return toReturn;
        }

    }
}
