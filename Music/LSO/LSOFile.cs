using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConvertLSO
{
    public abstract class LSOFile
    {
        public double BPM = 120.0;
        public int BPMoffset = 0x11A;
        public const int rootPosition = 0xC;
        protected byte[] arr { get; set; }

        public string FileName { get; set; }
        public byte[] FileBytes { get; private set; }

        public Dictionary<int, byte> ChannelsInstruments { get; private set; } = new Dictionary<int, byte>();
        public List<Track> Tracks { get; private set; } = new List<Track>();
        public Dictionary<byte, List<Track>> Channels { get; private set; } = null;

        public List<TempoStruct> GlobalTempo { get; private set; } = new List<TempoStruct>();


        public int SongLength { get; protected set; } = 0;

        public static LSOFile LSOFactory(string pFile)
        {
            return LSOFactory(File.ReadAllBytes(pFile));
        }

        public static LSOFile LSOFactory(byte[] pArr)
        {
            var version = BitConverter.ToUInt16(pArr, 4);
            return version >= 0x400 ? new LSOv5(pArr) : new LSOv207(pArr);
        }

        public LSOFile(string pFile)
        {
            FileName = pFile;
            arr = File.ReadAllBytes(FileName);
            SetParams();
        }

        public LSOFile(byte[] pArr)
        {
            arr = pArr;
            SetParams();
        }

        private void SetParams()
        {
            var bpmBytes = arr.Skip(BPMoffset).Take(4).ToArray();
            BPM = BitConverter.ToInt32(bpmBytes, 0) / 10000;
        }

        public abstract Dictionary<int, byte> GetChannelInstruments();
        public abstract List<int> GetAllTracks();
        public abstract void ParseTempoTable();
        public abstract void GetSongEndTimeStamp();


        public void ParseFile()
        {
            ChannelsInstruments = GetChannelInstruments();
            ParseTracks(GetAllTracks());
            ParseTempoTable();
            GetSongEndTimeStamp();

            var ordered = Tracks.Where(x => x.Channel > 0).OrderBy(x => x.Channel).ToList();
            Channels = ordered.GroupBy(x => x.Channel).ToDictionary(x => x.Key, y => y.ToList());
            
            //var tempos = con.GlobalTempo;
           
        }

        public virtual void ParseTracks(List<int> tracksOffsets)
        { 

            foreach (var track in tracksOffsets)
            {
                var trackBytes = arr.Skip(track).ToArray();
                var header = trackBytes.Take(4).ToArray().getInt(0);
                if (Track.IsMusicTrack(header))
                {
                    var t = new Track(trackBytes);
                    
                    if (t.Channel > 0 && t.Channel <= 0x10)
                    {
                        t.InitialInstrument = ChannelsInstruments[t.Channel];
                        Tracks.Add(t);
                    }
                }
            }

            HandleNegativeTracks();
        }

        public void HandleNegativeTracks()
        {
            // Handle negative offsets
            if (Tracks.Any(x => x.TrackPosition < 0))
            {
                var ordTracks = Tracks.OrderBy(x => x.TrackPosition).ToList();
                var lowest = ordTracks.First().TrackPosition * -1;

                foreach (var t in Tracks)
                {
                    t.TrackPosition += lowest;
                }
            }
        }

        public int getRoot()
        {

            return arr.getInt(rootPosition);
        }

        public int getRealOffset(int offset)
        {
            return arr.getInt(offset) - getRoot();
        }


    }
}
