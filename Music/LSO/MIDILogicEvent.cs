using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConvertLSO
{
    public class MIDILogicEvent
    {
        public byte MIDIFirstByte { get; set; }
        public int Position { get; set; }
        public byte MIDISecondByte { get; set; }
        public byte MIDIThirdByte { get; set; }
        public int MIDILength { get; set; }

        public int EventSize { get; set; }

        public byte[] RawBytes { get; set; }

        public MIDILogicEvent Clone()
        {
            MIDILogicEvent toReturn = new MIDILogicEvent();
            toReturn.MIDIFirstByte = this.MIDIFirstByte;
            toReturn.Position = this.Position;
            toReturn.MIDISecondByte = this.MIDISecondByte;
            toReturn.MIDIThirdByte = this.MIDIThirdByte;
            toReturn.MIDILength = this.MIDILength;
            toReturn.EventSize = this.EventSize;
            toReturn.RawBytes = this.RawBytes;

            return toReturn;

        }

        public static int GetMIDILogicEventSize(byte firstByte)
        {
            if ((firstByte >> 4) == 9)
                return 12;
            else return 6;

        }
    }
}
