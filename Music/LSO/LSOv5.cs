using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConvertLSO
{
    public class LSOv5 : LSOv207
    {

        public LSOv5(string file) : base(file) { }

        public LSOv5(byte[] pArr) : base(pArr) { }


        // Havn't found a better way to do it. This is too hacky but works
        public override List<int> GetAllTracks()
        {
            List<int> toReturn = new List<int>();

            var firstTrackOffset = getRealOffset(firstTrack);

            for (var cur = firstTrackOffset; ((arr.getInt(cur) & 0xFFFF) != 0xB2)
                && ((arr.getInt(cur) & 0xFFFF) != 0x94); cur += 4)
            {
                var index = getRealOffset(cur);
                var size = arr[index];

                if (size != 0xE8 && size != 0xCA)  // Logic 4 = 0xCA, Logic 5 - 0xE8
                    continue;

                toReturn.Add(index);
            }

            return toReturn;
        }

        public override void ParseTracks(List<int> tracksOffsets)
        {

            foreach (var track in tracksOffsets)
            {
                var trackBytes = arr.Skip(track).ToArray();
                var header = trackBytes.Take(4).ToArray().getInt(0);
                if (Track.IsMusicTrack(header))
                {
                    var t = new Track(trackBytes);
                   
                    if (t.Channel > 15)
                        t.Channel -= 15;

                    if (t.Channel > 0 && t.Channel <= 0x10)
                    {
                        t.InitialInstrument = ChannelsInstruments[t.Channel];
                        Tracks.Add(t);
                    }
                }
            }

            HandleNegativeTracks();
        }

        public override Dictionary<int, byte> GetChannelInstruments()
        {
            var toReturn = new Dictionary<int, byte>();
            var tmpArr = arr;
            //var offset = ; //getRealOffset(firstChannel);
            var instCounter = 0;
            for (int i = arr.FindPattern(0x760, 0xA0007); i != -1; i = arr.FindPattern(i + 4, 0xA0007))
            {
                if (arr[i + 4] != 0x20)
                    continue;

                var inst = arr[i + 0x48];
                toReturn[instCounter++] = inst;
                // offset = arr.FindPattern(offset + 0x4C, 0x00000052, 0x00010052, 0x00000084);
            }

            return toReturn;
        }

    }
}
