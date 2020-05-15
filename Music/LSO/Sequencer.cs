using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConvertLSO
{
    public class Sequencer
    {
        public List<Sequence> Sequences { get; set; } = new List<Sequence>();
        public byte ChannelStartPosition { get; set; }

    }

    public class Sequence
    {
        public MIDILogicEvent MLE { get; set; }
        public EvType eType { get; set; }
        public int Time { get; set; }
        public byte Channel { get; set; }
        public byte Note { get; set; }
        public byte ThirdByte { get; set; }
    }
}
