using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace ConvertLSO
{
    public class LSOConverter
    {
 
        public static void ParseTracks(string f, string outFile)
        {          
            var lso = LSOFile.LSOFactory(f);
            lso.ParseFile();
            MIDIFile mf = new MIDIFile(lso.BPM);


            foreach (var tempo in lso.GlobalTempo)
            {
                mf.TrackChunks[0].MTrkEvents.Add(new MTrkEvent((uint)(tempo.Timing / 2))
                { Event = new SetTempoMetaEvent((double)((double)tempo.Tempo / (double)10000)) });
            }

            foreach (var channel in lso.Channels)
            {
                var tracksInChannel = channel.Value.OrderBy(x => x.TrackPosition).ToList();

                foreach (var track in tracksInChannel)
                {
                    var Sequencer = new List<Sequence>();

                    MIDITrackChunk midiTrack = null;

                    midiTrack = new MIDITrackChunk(track.Channel, track.TrackName);
                    midiTrack.MTrkEvents.Add(new MTrkEvent(0)
                    {
                        Event = new ProgramChangeEvent(track.Channel, track.InitialInstrument)
                    });
                    int lastTime = 0;

                    // Check for track loop and duplicate until next track
                    if (track.IsLoopTrack && track.MIDIEvents.Count() > 0)
                    {
                        var trackIndex = tracksInChannel.IndexOf(track);
                        var isTrackLastOne = trackIndex == tracksInChannel.Count() - 1;

                        var stopLoopPosition = isTrackLastOne ? lso.SongLength : tracksInChannel[trackIndex + 1].TrackPosition;

                        // replicate track data until stop

                        var currentEvents = track.MIDIEvents.ToList();
                        var currentTime = track.TrackLength;
                        var currentMIDIEvent = currentEvents.First();

                        var totalMidiInCurrentEvents = currentEvents.Count() - 1;
                        var multiplier = 1;

                        for (int midiEventIndex = 0; currentTime < stopLoopPosition; midiEventIndex++)
                        {
                            currentMIDIEvent = currentEvents[midiEventIndex].Clone();
                            var eventNewTime = currentMIDIEvent.Position + (track.TrackLength * multiplier);

                            if (eventNewTime >= stopLoopPosition)
                                break;

                            currentMIDIEvent.Position = eventNewTime;
                            track.MIDIEvents.Add(currentMIDIEvent);

                            if (midiEventIndex == totalMidiInCurrentEvents)
                            {
                                multiplier++;
                                midiEventIndex = -1;
                            }

                            currentTime = eventNewTime;
                        }
                    }

                    foreach (var lme in track.MIDIEvents)
                    {
                        var del = track.TrackPosition + lme.Position;
                        
                        if (lme.MIDIFirstByte == 0xB0)
                        {
                            // Instrument Change
                            Sequencer.Add(new Sequence()
                            {
                                eType = EvType.ControllerEvent,
                                MLE = lme,
                                Time = del,
                                Channel = (byte)track.Channel,
                                Note = lme.MIDISecondByte
                            });
                            continue;
                        }

                        if (lme.MIDIFirstByte == 0xC0)
                        {
                            // Instrument Change
                            Sequencer.Add(new Sequence()
                            {
                                eType = EvType.ProgramChangeEvent,
                                MLE = lme,
                                Time = del,
                                Channel = (byte)track.Channel,
                                Note = lme.MIDIThirdByte
                            });
                            continue;
                        }

                        if (lme.MIDIFirstByte == 0xE0)
                        {
                            // Pitch Bend
                            Sequencer.Add(new Sequence()
                            {
                                eType = EvType.PitchBend,
                                MLE = lme,
                                Time = del,
                                Channel = (byte)track.Channel,
                                Note = lme.MIDISecondByte,
                                ThirdByte = lme.MIDIThirdByte
                            });
                            continue;
                        }

                        // Note On
                        Sequencer.Add(new Sequence()
                        {
                            eType = EvType.NoteOn,
                            MLE = lme,
                            Time = del,
                            Channel = (byte)track.Channel,
                            Note = lme.MIDISecondByte
                        });

                        // Note Off
                        Sequencer.Add(new Sequence()
                        {
                            eType = EvType.NoteOff,
                            MLE = lme,
                            Time = del + lme.MIDILength,
                            Channel = (byte)track.Channel,
                            Note = lme.MIDISecondByte
                        });
                    }

                    Sequencer = Sequencer.OrderBy(x => x.Time).ToList();

                    // Add MIDI Events
                    foreach (var seq in Sequencer)
                    {
                        var delta = ((uint)(seq.Time - lastTime)) / 2;

                        MTrkEvent midiMtrkEvent = null;

                        switch (seq.eType)
                        {
                            case EvType.NoteOn:
                                midiMtrkEvent = new MTrkEvent(delta);
                                midiMtrkEvent.Event = new MIDIEventNoteOn(seq.Channel, seq.MLE.MIDISecondByte, seq.MLE.MIDIThirdByte);
                                midiTrack.MTrkEvents.Add(midiMtrkEvent);
                                break;
                            case EvType.NoteOff:
                                midiMtrkEvent = new MTrkEvent(delta);
                                midiMtrkEvent.Event = new MIDIEventNoteOn(seq.Channel, seq.MLE.MIDISecondByte, 0);
                                midiTrack.MTrkEvents.Add(midiMtrkEvent);
                                break;
                            case EvType.ProgramChangeEvent:
                                midiMtrkEvent = new MTrkEvent(delta);
                                midiMtrkEvent.Event = new ProgramChangeEvent(seq.Channel, seq.MLE.MIDIThirdByte);
                                midiTrack.MTrkEvents.Add(midiMtrkEvent);
                                break;
                            case EvType.PitchBend:
                                midiMtrkEvent = new MTrkEvent(delta);
                                midiMtrkEvent.Event = new PitchBendEvent(seq.Channel, seq.MLE.MIDIFirstByte, seq.MLE.MIDIThirdByte);
                                midiTrack.MTrkEvents.Add(midiMtrkEvent);
                                break;
                            case EvType.ControllerEvent:
                                midiMtrkEvent = new MTrkEvent(delta);
                                midiMtrkEvent.Event = new ControllerEvent(seq.Channel, seq.MLE.MIDISecondByte, seq.MLE.MIDIThirdByte);
                                midiTrack.MTrkEvents.Add(midiMtrkEvent);
                                break;

                        }

                        lastTime = seq.Time;

                    }
                    mf.TrackChunks.Add(midiTrack);
                }
            }

            var midiFile = mf.ToBytes();            
            File.WriteAllBytes(outFile, midiFile);
        }

        internal static void PrintOffAndName(string file)
        {
            var bytes = File.ReadAllBytes(file);
            var rc = LSOFile.LSOFactory(bytes);
            var list = new List<int>() {
             0x16, 0x1A, 0x1E, 0x22, 0x2E, 0x34,0x7A, 0x11A, 0x134, 0x1D9, 0x1EC, 0x1FA, 0x19C
            };
            list.AddRange(Enumerable.Range(0x800, 0xB74).Where(x => x % 4 == 0).ToList());

            //for (int i = 0xC; i < 0xB74; i += 4)
            foreach (int i in list)
            {
                var off = rc.getRealOffset(i);

                if ((uint)off> bytes.Length)
                {
                    Console.WriteLine($"Offset: {i:X4} ({off:X4})");
                    continue;
                }

                var size = bytes[off];
                var name = Encoding.ASCII.GetString(bytes.Skip(off+0x65).TakeWhile(b => !b.Equals(0)).ToArray());
                name = String.IsNullOrEmpty(name) ? Encoding.ASCII.GetString(bytes.Skip(off + 0x2D).TakeWhile(b => !b.Equals(0)).ToArray())
                    : name;

                Console.WriteLine($"Offset: {i:X4} ({off:X4}), Size: {size:X4}, Name: {name}");

            }

        }

     
    }

}