using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConvertLSO
{
    public class LSOv207 : LSOFile
    {        
        protected const int firstTrack = 0x1A;
        protected const int firstChannel = 0x92C;
        protected const int almostGlobalTempoOffset = 0x800;
        protected const int offsetToOffsetToTrash = 0x7BC;
        protected const int SongLengthOffset = 0x33;
        
        public LSOv207(string pFilename) : base(pFilename) { }
        public LSOv207(byte[] pArr) : base(pArr) { }


        public override Dictionary<int, byte> GetChannelInstruments()
        {
            var toReturn = new Dictionary<int, byte>();
            var tmpArr = arr;
            var offset = arr.FindPattern(0x760, 0x00000052, 0x00010052, 0x00000084); //getRealOffset(firstChannel);

            for (int i = 1; i <= 16; i++)
            {
                var inst = arr[offset + 0x4C];
                toReturn[i] = inst;
                offset = arr.FindPattern(offset + 0x4C, 0x00000052, 0x00010052, 0x00000084);
            }

            return toReturn;
        }

        public override List<int> GetAllTracks()
        {
            List<int> toReturn = new List<int>();

            var firstTrackOffset = getRealOffset(firstTrack);

            for (var cur = firstTrackOffset; (arr.getInt(cur) & 0xFFFF) != 0x94; cur += 4)
            {
                toReturn.Add(getRealOffset(cur));
            }

            return toReturn;
        }

        public override void ParseTempoTable()
        {
            var almostTempo = getRealOffset(almostGlobalTempoOffset);
            var isTempoStart = almostTempo + 0x94;
            var isTempoValue = arr.getInt(isTempoStart) == 0x960060;

            if (!isTempoValue)
            {
                almostTempo = getRealOffset(almostGlobalTempoOffset - 4);
                isTempoStart = almostTempo + 0x94;
                isTempoValue = arr.getInt(isTempoStart) == 0x960060;
            }

            if (isTempoValue)
            {
                for (int i = isTempoStart; i < arr.Length - 4 &&
                    arr[i] != 0xF1 && arr[i + 1] != 0xFF && arr[i + 2] != 0xFF && arr[i + 3] != 0x7F; i += 18)
                {
                    var arrTempo = arr.Skip(i).Take(18).ToArray();
                    TempoStruct ts = new TempoStruct();
                    ts.Timing = arrTempo.getInt(1) - 0x9600;
                    var arrTempoEntry = arrTempo.Skip(12).Take(4).ToArray();
                    arrTempoEntry[3] = 0;
                    ts.Tempo = BitConverter.ToInt32(arrTempoEntry, 0);
                    GlobalTempo.Add(ts);

                }
            }
        }

        public override void GetSongEndTimeStamp()
        {
            // This is tricky...
            var TrashStructOffset = getRealOffset(getRealOffset(offsetToOffsetToTrash));
            var TrashSize = arr[TrashStructOffset];
            var SongStructArrSubset = arr.Skip(TrashStructOffset).Skip(TrashSize).Skip(4).ToArray();
            var arrSongLength = SongStructArrSubset.Skip(SongLengthOffset).Take(4).ToArray();
            arrSongLength[3] = 0;
            SongLength = BitConverter.ToInt32(arrSongLength, 0);
        }
    }
}
