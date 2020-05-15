using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace ConvertLSO
{
    public class MIDIFile : IToBytes
    {
        public List<MIDITrackChunk> TrackChunks { get; private set; } = new List<MIDITrackChunk>();
        public MIDIHeader Header { get; private set; }



        public MIDIFile(string fileName)
        {
            var arr = File.ReadAllBytes(fileName);
            Header = new MIDIHeader(arr);

            var counter = 0;

            while (counter + 14 < arr.Length)
            {
                var tc = new MIDITrackChunk(arr.Skip(14 + counter).ToArray());
                TrackChunks.Add(tc);
                counter += (int)tc.Size + 8;
            }
        }


        public MIDIFile(double BPM)
        {
            Header = new MIDIHeader();
            TrackChunks = new List<MIDITrackChunk>();
            var FirstTrack = new MIDITrackChunk(0, "");

            FirstTrack.MTrkEvents.Add(new MTrkEvent(0)
            {
                Event = new TimeSignatureMetaEvent()
            });

            FirstTrack.MTrkEvents.Add(new MTrkEvent(0)
            {
                Event = new KeySignatureMetaEvent()
            });

            FirstTrack.MTrkEvents.Add(new MTrkEvent(0)
            {
                Event = new SMPTEOffsetMetaEvent()
            });

            FirstTrack.MTrkEvents.Add(new MTrkEvent(0)
            {
                Event = new SetTempoMetaEvent(BPM)
            });


            TrackChunks.Add(FirstTrack);
        }


        public byte[] ToBytes()
        {
            var b = new List<byte>();
            Header.NumberOfTracks = (ushort)TrackChunks.Count();
            b.AddRange(Header.ToBytes());
            foreach (var trck in TrackChunks)
            {
                b.AddRange(trck.ToBytes());
            }

            return b.ToArray();
        }
    }

    public interface IToBytes
    {
        byte[] ToBytes();
    }


    public class MIDITrackChunk : IToBytes
    {
        public const string MIDITrackChunkType = "MTrk";
        public uint Size;
        public List<MTrkEvent> MTrkEvents = new List<MTrkEvent>();
        public byte Channel { get; set; }
        public string Name { get; set; }

        public MIDITrackChunk(byte[] arr)
        {
            if (Encoding.ASCII.GetString(arr.Take(4).ToArray()) != MIDITrackChunkType)
                throw new Exception("Track Chunk Type is not " + MIDITrackChunkType);

            Size = BitConverter.ToUInt32(arr.Skip(4).Take(4).Reverse().ToArray(), 0);

            var toParse = 0;

            while (toParse < Size)
            {
                var ev = new MTrkEvent(arr.Skip(8 + toParse).ToArray());
                MTrkEvents.Add(ev);
                toParse += ev.ToBytes().Length;
            }

            var firstEvent = (MIDIEvent)(MTrkEvents.Where(x => x.Event is MIDIEvent).Select(x => x.Event).FirstOrDefault());
            if (firstEvent != null)
            {
                Channel = firstEvent.Channel;
            }


        }
        public MIDITrackChunk(byte pChannel, string pName)
        {
            Channel = (byte)(pChannel - (byte)1);
            Name = pName;

        }

        public byte[] ToBytes()
        {
            var b = new List<byte>();

            b.AddRange(Encoding.ASCII.GetBytes(MIDITrackChunkType));

            var c = new List<byte>();

            if (!String.IsNullOrEmpty(Name))
            {
                c.AddRange(new MTrkEvent(0) { DeltaTime = new byte[] { 0x0 }, Event = new MIDIChannelPrefixMetaEvent(Channel) }.ToBytes());
                c.AddRange(new MTrkEvent(0) { DeltaTime = new byte[] { 0x0 }, Event = new SetChannelNameEvent(Name) }.ToBytes());
            }

            foreach (var e in MTrkEvents)
            {
                c.AddRange(e.ToBytes());
            }

            b.AddRange(BitConverter.GetBytes(c.Count() + 4).Reverse());
            b.AddRange(c);

            b.AddRange(new MTrkEvent(0) { Event = new EndOfTrackMetaEvent() }.ToBytes());
            return b.ToArray();
        }

    }

    public class MIDIEventParser
    {
        public static Event Parse(byte[] arr)
        {
            Event toReturn = null;
            /*
            // create instances of all events
            var type = typeof(Event);
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => type.IsAssignableFrom(p) && !p.IsInterface && p.IsClass && !p.IsAbstract);

            var EventTypes = new List<Event>();

            foreach (var t in types)
            {
                EventTypes.Add((Event)Activator.CreateInstance(t));
            }
            */

            var firstByte = arr[0];
            var secondByte = arr[1];
            var thirdByte = arr[2];

            switch (firstByte)
            {
                case 0xFF:
                    switch (secondByte)
                    {
                        case 0x2F:
                            return new EndOfTrackMetaEvent();
                        case 0x59:
                            return new KeySignatureMetaEvent() { ThirdByte = thirdByte };
                        case 0x54:
                            return new SMPTEOffsetMetaEvent() { ThirdByte = thirdByte };
                        case 0x58:
                            return new TimeSignatureMetaEvent() { ThirdByte = thirdByte };
                        case 0x04:
                            return new InstrumentNameMetaEvent(arr);
                        case 0x03:
                            return new SetChannelNameEvent(arr);
                        case 0x51:
                            return new SetTempoMetaEvent(arr);
                        case 0x20:
                            return new MIDIChannelPrefixMetaEvent(arr[3]);


                    };
                    break;

                default:
                    var firstByteHigh = (byte)(arr[0] >> 4);
                    var firstByteLow = (byte)(arr[0] & 0xf);
                    switch (firstByteHigh)
                    {
                        case 0x9: return new MIDIEventNoteOn(firstByteLow, secondByte, thirdByte);
                        case 0xB: return new ControllerEvent(firstByteLow, secondByte, thirdByte);
                        case 0xC: return new ProgramChangeEvent(firstByteLow, secondByte);
                        case 0xE: return new PitchBendEvent(firstByteLow, secondByte, thirdByte);
                        default:
                            throw new Exception("Unknown MIDI event");
                    };
                    break;
            }


            return toReturn;
        }
    }

    public class MTrkEvent : IToBytes
    {
        public uint DeltaTimeNormal { get; set; }
        public byte[] DeltaTime { get; set; }
        public Event Event { get; set; }

        public MTrkEvent(byte[] arr)
        {
            // Delta Time

            var deltaTimeList = new List<byte>();
            var counter = 0;

            deltaTimeList.Add(arr[counter]);

            if ((arr[0] & 0x80) != 0)
            {
                while ((arr[counter] & 0x80) != 0)
                {
                    deltaTimeList.Add(arr[++counter]);

                }
            }

            DeltaTime = deltaTimeList.ToArray();
            DeltaTimeNormal = ReadVarLenBytes(DeltaTime);

            Event = MIDIEventParser.Parse(arr.Skip(DeltaTime.Length).ToArray());
        }

        public MTrkEvent(uint pTrackPosition)
        {
            DeltaTimeNormal = pTrackPosition;
            DeltaTime = SetVarLen(pTrackPosition);
            //var norm = ReadVarLen(DeltaTime);
            //if (norm != DeltaTimeNormal)
            //    throw new Exception("Problem");
        }

        public static int ReadVarLen2(byte[] arr)
        {
            // Read out an Int32 7 bits at a time.  The high bit 
            // of the byte when on means to continue reading more bytes.
            int count = 0;
            int shift = 0;
            byte b;
            int index = 0;
            do
            {
                // Check for a corrupted stream.  Read a max of 5 bytes.
                // In a future version, add a DataFormatException.
                if (shift == 5 * 7)  // 5 bytes max per Int32, shift += 7
                    throw new FormatException("Format_Bad7BitInt32");

                // ReadByte handles end of stream cases for us. 
                b = arr[index++];
                count |= (b & 0x7F) << shift;
                shift += 7;
            } while ((b & 0x80) != 0);
            return count;
        }

        public static byte[] SetVarLen2(uint value)
        {
            List<byte> toReturn = new List<byte>();

            // Write out an int 7 bits at a time.  The high bit of the byte,
            // when on, tells reader to continue reading more bytes.
            uint v = value;   // support negative numbers
            while (v >= 0x80)
            {
                toReturn.Add((byte)(v | 0x80));
                v >>= 7;
            }
            toReturn.Add((byte)v);

            return toReturn.ToArray();
        }

        public static byte[] SetVarLen(uint value)
        {
            var toReturn = new List<byte>();
            uint buffer;
            buffer = value & 0x7f;
            toReturn.Add((byte)(value & 0x7f));
            while ((value >>= 7) > 0)
            {
                buffer <<= 8;
                buffer |= 0x80;
                buffer += (value & 0x7f);
                toReturn.Add((byte)(0x80 | value & 0x7f));
            }
            /*while (true)
            {
                if ((buffer & 0x80) != 0) buffer >>= 8;
                else
                    break;
            }*/

            return toReturn.ToArray();
        }

        public static uint ReadVarLenBytes(byte[] b)
        {
            uint value;
            byte c;
            int counter = 0;

            if (((value = b[counter++]) & 0x80) != 0)
            {
                value &= 0x7f;
                do
                {
                    value = (uint)((value << 7) + ((c = b[counter++]) & 0x7f));
                } while ((c & 0x80) != 0);
            }
            return (value);
        }

        public static uint ReadVarLen(uint varLen)
        {
            uint value;
            byte c = 0x80;

            if (((value = varLen) & 0x80) != 0)
            {
                value &= 0x7f;
                for (int i = 1; i < 4 && (c & 0x80) != 0; i++)
                {
                    value = (uint)((value << 7) + (((c = (byte)(varLen >> (i * 8)))) & 0x7f));
                }
            }

            return (value);
        }

        public byte[] ToBytes()
        {
            var b = new List<byte>();
            /* if (DeltaTime == 0)
                 b.Add(0x0);
             else
             {
                 var deltaTimebArr = BitConverter.GetBytes(DeltaTime).Reverse().ToList();
                 var ind = 0;
                 // Find first non-zero number
                 for (int i = 0; i < deltaTimebArr.Count(); i++)
                 {
                     if (deltaTimebArr[i] != 0)
                     {
                         ind = i;
                         break;
                     }
                 }
                 b.AddRange(deltaTimebArr.Skip(ind).Reverse());
             }                */

            b.AddRange(DeltaTime.Reverse());
            b.AddRange(Event.ToBytes());

            return b.ToArray();
        }

    }

    public abstract class Event : IToBytes
    {
        public abstract byte[] ToBytes();
        public byte FirstByte { get; set; }

    }

    public abstract class MIDIEvent : Event
    {
        public string EventName { get; set; }

        //public override byte FirstByte { get; set; }
        public byte SecondByte { get; set; }
        public byte ThirdByte { get; set; }

        public byte Channel { get; set; }

        public MIDIEvent(byte cmdByte, byte pChannel)
        {
            Channel = pChannel == 0 ? (byte)0 : (byte)(pChannel - 1);
            FirstByte = (byte)(cmdByte << 4 | (byte)Channel);
        }

        public override abstract byte[] ToBytes();

    }

    public class MIDIEventNoteOn : MIDIEvent
    {
        public MIDIEventNoteOn(byte pChannel, byte note, byte velocity) : base(0x9, pChannel)
        {
            SecondByte = note;
            ThirdByte = velocity;
        }

        public override byte[] ToBytes()
        {
            var toReturn = new List<byte>();

            toReturn.Add(FirstByte);
            toReturn.Add(SecondByte);
            toReturn.Add(ThirdByte);

            return toReturn.ToArray();
        }
    }

    public class ProgramChangeEvent : MIDIEvent
    {
        byte Instrument;

        public ProgramChangeEvent(byte pChannel, byte pInstrument) : base(0xC, pChannel)
        {
            Instrument = pInstrument;
            SecondByte = Instrument;
        }

        public override byte[] ToBytes()
        {
            var toReturn = new List<byte>();

            toReturn.Add(FirstByte);
            toReturn.Add(SecondByte);

            return toReturn.ToArray();
        }
    }

    public class PitchBendEvent : MIDIEvent
    {


        public PitchBendEvent(byte pChannel, byte val1, byte val2) : base(0xE, pChannel)
        {
            SecondByte = val1;
            ThirdByte = val2;

        }

        public override byte[] ToBytes()
        {
            var toReturn = new List<byte>();

            toReturn.Add(FirstByte);
            toReturn.Add(SecondByte);
            toReturn.Add(ThirdByte);


            return toReturn.ToArray();
        }
    }

    public class ControllerEvent : MIDIEvent
    {
        public ControllerEvent(byte pChannel, byte ControllerType, byte Value) : base(0xB, pChannel)
        {

            SecondByte = ControllerType;
            ThirdByte = Value;

        }

        public override byte[] ToBytes()
        {
            var toReturn = new List<byte>();

            toReturn.Add(FirstByte);
            toReturn.Add(SecondByte);
            toReturn.Add(ThirdByte);


            return toReturn.ToArray();
        }
    }


    public class SysexEvent : Event
    {
        public SysexEvent()
        {
            FirstByte = 0xFF;
        }
        public override byte[] ToBytes()
        {
            throw new NotImplementedException();
        }
    }
    public abstract class MetaEvent : Event
    {
        public MetaEvent()
        {
            FirstByte = 0xFF;
        }
        public abstract byte SecondByte { get; }
    }

    public class MIDIChannelPrefixMetaEvent : MetaEvent
    {
        public override byte SecondByte { get { return 0x20; } }

        public byte Channel { get; set; }

        public MIDIChannelPrefixMetaEvent(byte forthByte)
        {
            Channel = forthByte;
        }

        public MIDIChannelPrefixMetaEvent(int pChannel)
        {
            Channel = (byte)pChannel;
        }

        public override byte[] ToBytes()
        {
            var toReturn = new List<byte>();
            toReturn.Add(FirstByte);
            toReturn.Add(SecondByte);
            toReturn.Add(0x1); // Length
            toReturn.Add(Channel);
            return toReturn.ToArray();
        }
    }

    public class SetTempoMetaEvent : MetaEvent
    {
        public override byte SecondByte { get { return 0x51; } }
        public int Length { get { return 3; } }
        public int Tempo { get; private set; }

        public SetTempoMetaEvent(byte[] arr)
        {
            Tempo = BitConverter.ToInt32(arr.Skip(2).Take(3).Concat(new byte[] { 0 }).ToArray(), 0);
        }

        public SetTempoMetaEvent(double pBPM)
        {
            FirstByte = 0xFF;
            Tempo = (int)(60000000 / pBPM);
        }

        public override byte[] ToBytes()
        {
            var toReturn = new List<byte>();
            toReturn.Add(FirstByte);
            toReturn.Add(SecondByte);
            toReturn.Add(0x3);

            var tempoBytes = BitConverter.GetBytes(Tempo);
            toReturn.AddRange(tempoBytes.Take(3).Reverse());
            return toReturn.ToArray();
        }
    }

    public class SetChannelNameEvent : MetaEvent
    {
        public override byte SecondByte { get { return 0x03; } }

        public byte ThirdByte { get; set; }
        public byte[] NameBytes;

        public SetChannelNameEvent(byte[] arr)
        {
            var strLength = arr[2];
            ThirdByte = (byte)arr[2];
            NameBytes = arr.Skip(2).Take(strLength).ToArray();
        }

        public SetChannelNameEvent(string Name)
        {
            ThirdByte = (byte)Name.Length;
            NameBytes = Encoding.ASCII.GetBytes(Name);
        }

        public override byte[] ToBytes()
        {
            var toReturn = new List<byte>();
            toReturn.Add(FirstByte);
            toReturn.Add(SecondByte);
            toReturn.Add(ThirdByte);
            toReturn.AddRange(NameBytes);
            return toReturn.ToArray();

        }
    }

    public class InstrumentNameMetaEvent : MetaEvent
    {
        public override byte SecondByte { get { return 0x04; } }

        public byte ThirdByte { get; set; }
        public byte[] NameBytes;

        public InstrumentNameMetaEvent(byte[] arr)
        {
            var strLength = arr[1];
            NameBytes = arr.Skip(2).Take(strLength).ToArray();

        }

        public InstrumentNameMetaEvent(string Name)
        {
            ThirdByte = (byte)Name.Length;
            NameBytes = Encoding.ASCII.GetBytes(Name);
        }

        public override byte[] ToBytes()
        {
            var toReturn = new List<byte>();
            toReturn.Add(FirstByte);
            toReturn.Add(SecondByte);
            toReturn.Add(ThirdByte);
            toReturn.AddRange(NameBytes);
            return toReturn.ToArray();

        }
    }

    public class TimeSignatureMetaEvent : MetaEvent
    {
        public override byte SecondByte { get { return 0x58; } }
        public byte ThirdByte { get; set; }


        public TimeSignatureMetaEvent()
        {
            ThirdByte = 0x4;
        }

        public override byte[] ToBytes()
        {
            var b = new List<byte>();

            b.Add(FirstByte);
            b.Add(SecondByte);
            b.Add(ThirdByte);
            b.AddRange(new byte[] { 0x4, 0x2, 0x18, 0x8 });

            return b.ToArray();
        }
    }

    public class SMPTEOffsetMetaEvent : MetaEvent
    {
        public override byte SecondByte { get { return 0x54; } }
        public byte ThirdByte { get; set; }

        public SMPTEOffsetMetaEvent()
        {
            ThirdByte = 0x5;
        }

        public override byte[] ToBytes()
        {
            var b = new List<byte>();

            b.Add(FirstByte);
            b.Add(SecondByte);
            b.Add(ThirdByte);
            b.AddRange(new byte[] { 0x20, 0, 0, 0, 0 });

            return b.ToArray();
        }
    }


    public class KeySignatureMetaEvent : MetaEvent
    {
        public override byte SecondByte { get { return 0x59; } }
        public byte ThirdByte { get; set; }

        public KeySignatureMetaEvent()
        {
            ThirdByte = 0x2;
        }

        public override byte[] ToBytes()
        {
            var b = new List<byte>();

            b.Add(FirstByte);
            b.Add(SecondByte);
            b.Add(ThirdByte);
            b.AddRange(new byte[] { 0, 0 });

            return b.ToArray();
        }
    }




    public class EndOfTrackMetaEvent : MetaEvent
    {
        public override byte SecondByte { get { return 0x2F; } }


        public override byte[] ToBytes()
        {
            var b = new List<byte>();

            b.Add(FirstByte);
            b.Add(SecondByte);
            b.Add(0);

            return b.ToArray();
        }
    }



    public class MIDIHeader : IToBytes
    {
        public const string TypeString = "MThd";
        public uint Size = 6;
        public ushort Format = 1;
        public ushort NumberOfTracks = 1;
        public ushort Division = 0x1E0;

        public MIDIHeader() { }

        public MIDIHeader(byte[] arr)
        {
            if (Encoding.ASCII.GetString(arr.Take(4).ToArray()) != TypeString)
                throw new Exception("MIDI Header is not " + TypeString);

            Size = BitConverter.ToUInt32(arr.Skip(4).Take(4).Reverse().ToArray(), 0);

            if (Size != 6)
                throw new Exception("MIDI Header Length is not 6, usually this is a problem");

            Format = BitConverter.ToUInt16(arr.Skip(8).Take(2).Reverse().ToArray(), 0);
            NumberOfTracks = BitConverter.ToUInt16(arr.Skip(10).Take(2).Reverse().ToArray(), 0);
            Division = BitConverter.ToUInt16(arr.Skip(12).Take(2).Reverse().ToArray(), 0);

        }


        public byte[] ToBytes()
        {
            List<byte> toReturn = new List<byte>();

            toReturn.AddRange(Encoding.ASCII.GetBytes(TypeString));
            toReturn.AddRange(BitConverter.GetBytes(Size).Reverse());
            toReturn.AddRange(BitConverter.GetBytes(Format).Reverse());
            toReturn.AddRange(BitConverter.GetBytes(NumberOfTracks).Reverse());
            toReturn.AddRange(BitConverter.GetBytes(Division).Reverse());

            return toReturn.ToArray();
        }
    }
}
