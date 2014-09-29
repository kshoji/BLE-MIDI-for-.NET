using System.IO;

namespace kshoji.BleMidi
{
    /// <summary>
    /// Represents BLE MIDI Input Device
    /// </summary>
    public partial class MidiInputDevice
    {
        /// <summary>
        /// MIDI Parser, referred from MidiInputDevice
        /// </summary>
        private class MidiParser
        {
            private int midiState = MIDI_STATE_WAIT;
            private int midiEventKind = 0;
            private int midiEventNote = 0;
            private int midiEventVelocity = 0;

            // for RPN/NRPN messages
            private const int PARAMETER_MODE_NONE = 0;
            private const int PARAMETER_MODE_RPN = 1;
            private const int PARAMETER_MODE_NRPN = 2;
            private int parameterMode = PARAMETER_MODE_NONE;
            private int parameterNumber = 0x3fff;
            private int parameterValue = 0x3fff;

            private readonly MemoryStream systemExclusiveStream = new MemoryStream();

            private const int MIDI_STATE_WAIT = 0;
            private const int MIDI_STATE_SIGNAL_2BYTES_2 = 21;
            private const int MIDI_STATE_SIGNAL_3BYTES_2 = 31;
            private const int MIDI_STATE_SIGNAL_3BYTES_3 = 32;
            private const int MIDI_STATE_SIGNAL_SYSEX = 41;

            readonly OnMidiInputEventListener midiInputEventListener = new OnMidiInputEventListener();
            readonly MidiInputDevice sender;

            public MidiParser(MidiInputDevice sender)
            {
                this.sender = sender;

                midiState = MIDI_STATE_WAIT;
                midiEventKind = 0;
                midiEventNote = 0;
                midiEventVelocity = 0;
            }

            public OnMidiInputEventListener GetMidiInputEventListener()
            {
                return midiInputEventListener;
            }

            private void ParseMidiEvent(byte data)
            {
                int midiEvent = data & 0xff;
                if (midiState == MIDI_STATE_WAIT)
                {
                    switch (midiEvent & 0xf0)
                    {
                        case 0xf0:
                            {
                                switch (midiEvent)
                                {
                                    case 0xf0:
                                        lock (systemExclusiveStream)
                                        {
                                            systemExclusiveStream.SetLength(0);
                                            systemExclusiveStream.WriteByte((byte)midiEvent);
                                            midiState = MIDI_STATE_SIGNAL_SYSEX;
                                        }
                                        break;

                                    case 0xf1:
                                    case 0xf3:
                                        // 0xf1 MIDI Time Code Quarter Frame. : 2bytes
                                        // 0xf3 Song Select. : 2bytes
                                        midiEventKind = midiEvent;
                                        midiState = MIDI_STATE_SIGNAL_2BYTES_2;
                                        break;

                                    case 0xf2:
                                        // 0xf2 Song Position Pointer. : 3bytes
                                        midiEventKind = midiEvent;
                                        midiState = MIDI_STATE_SIGNAL_3BYTES_2;
                                        break;

                                    case 0xf6:
                                        // 0xf6 Tune Request : 1byte
                                        if (midiInputEventListener != null)
                                        {
                                            midiInputEventListener.OnMidiTuneRequest(sender);
                                        }
                                        midiState = MIDI_STATE_WAIT;
                                        break;
                                    case 0xf8:
                                        // 0xf8 Timing Clock : 1byte
                                        if (midiInputEventListener != null)
                                        {
                                            midiInputEventListener.OnMidiTimingClock(sender);
                                        }
                                        midiState = MIDI_STATE_WAIT;
                                        break;
                                    case 0xfa:
                                        // 0xfa Start : 1byte
                                        if (midiInputEventListener != null)
                                        {
                                            midiInputEventListener.OnMidiStart(sender);
                                        }
                                        midiState = MIDI_STATE_WAIT;
                                        break;
                                    case 0xfb:
                                        // 0xfb Continue : 1byte
                                        if (midiInputEventListener != null)
                                        {
                                            midiInputEventListener.OnMidiContinue(sender);
                                        }
                                        midiState = MIDI_STATE_WAIT;
                                        break;
                                    case 0xfc:
                                        // 0xfc Stop : 1byte
                                        if (midiInputEventListener != null)
                                        {
                                            midiInputEventListener.OnMidiStop(sender);
                                        }
                                        midiState = MIDI_STATE_WAIT;
                                        break;
                                    case 0xfe:
                                        // 0xfe Active Sensing : 1byte
                                        if (midiInputEventListener != null)
                                        {
                                            midiInputEventListener.OnMidiActiveSensing(sender);
                                        }
                                        midiState = MIDI_STATE_WAIT;
                                        break;
                                    case 0xff:
                                        // 0xff Reset : 1byte
                                        if (midiInputEventListener != null)
                                        {
                                            midiInputEventListener.OnMidiReset(sender);
                                        }
                                        midiState = MIDI_STATE_WAIT;
                                        break;

                                    default:
                                        break;
                                }
                            }
                            break;
                        case 0x80:
                        case 0x90:
                        case 0xa0:
                        case 0xb0:
                        case 0xe0:
                            // 3bytes pattern
                            midiEventKind = midiEvent;
                            midiState = MIDI_STATE_SIGNAL_3BYTES_2;
                            break;
                        case 0xc0: // program change
                        case 0xd0: // channel after-touch
                            // 2bytes pattern
                            midiEventKind = midiEvent;
                            midiState = MIDI_STATE_SIGNAL_2BYTES_2;
                            break;
                        default:
                            // 0x00 - 0x70: running status
                            if ((midiEventKind & 0xf0) != 0xf0)
                            {
                                // previous event kind is multi-bytes pattern
                                midiEventNote = midiEvent;
                                midiState = MIDI_STATE_SIGNAL_3BYTES_3;
                            }
                            break;
                    }
                }
                else if (midiState == MIDI_STATE_SIGNAL_2BYTES_2)
                {
                    switch (midiEventKind & 0xf0)
                    {
                        // 2bytes pattern
                        case 0xc0: // program change
                            midiEventNote = midiEvent;
                            if (midiInputEventListener != null)
                            {
                                midiInputEventListener.OnMidiProgramChange(sender, midiEventKind & 0xf, midiEventNote);
                            }
                            midiState = MIDI_STATE_WAIT;
                            break;
                        case 0xd0: // channel after-touch
                            midiEventNote = midiEvent;
                            if (midiInputEventListener != null)
                            {
                                midiInputEventListener.OnMidiChannelAftertouch(sender, midiEventKind & 0xf, midiEventNote);
                            }
                            midiState = MIDI_STATE_WAIT;
                            break;
                        case 0xf0:
                            {
                                switch (midiEventKind)
                                {
                                    case 0xf1:
                                        // 0xf1 MIDI Time Code Quarter Frame. : 2bytes
                                        midiEventNote = midiEvent;
                                        if (midiInputEventListener != null)
                                        {
                                            midiInputEventListener.OnMidiTimeCodeQuarterFrame(sender, midiEventNote);
                                        }
                                        midiState = MIDI_STATE_WAIT;
                                        break;
                                    case 0xf3:
                                        // 0xf3 Song Select. : 2bytes
                                        midiEventNote = midiEvent;
                                        if (midiInputEventListener != null)
                                        {
                                            midiInputEventListener.OnMidiSongSelect(sender, midiEventNote);
                                        }
                                        midiState = MIDI_STATE_WAIT;
                                        break;
                                    default:
                                        // illegal state
                                        midiState = MIDI_STATE_WAIT;
                                        break;
                                }
                            }
                            break;
                        default:
                            // illegal state
                            midiState = MIDI_STATE_WAIT;
                            break;
                    }
                }
                else if (midiState == MIDI_STATE_SIGNAL_3BYTES_2)
                {
                    switch (midiEventKind & 0xf0)
                    {
                        case 0x80:
                        case 0x90:
                        case 0xa0:
                        case 0xb0:
                        case 0xe0:
                        case 0xf0:
                            // 3bytes pattern
                            midiEventNote = midiEvent;
                            midiState = MIDI_STATE_SIGNAL_3BYTES_3;
                            break;
                        default:
                            // illegal state
                            midiState = MIDI_STATE_WAIT;
                            break;
                    }
                }
                else if (midiState == MIDI_STATE_SIGNAL_3BYTES_3)
                {
                    switch (midiEventKind & 0xf0)
                    {
                        // 3bytes pattern
                        case 0x80: // note off
                            midiEventVelocity = midiEvent;
                            if (midiInputEventListener != null)
                            {
                                midiInputEventListener.OnMidiNoteOff(sender, midiEventKind & 0xf, midiEventNote, midiEventVelocity);
                            }
                            midiState = MIDI_STATE_WAIT;
                            break;
                        case 0x90: // note on
                            midiEventVelocity = midiEvent;
                            if (midiInputEventListener != null)
                            {
                                if (midiEventVelocity == 0)
                                {
                                    midiInputEventListener.OnMidiNoteOff(sender, midiEventKind & 0xf, midiEventNote, midiEventVelocity);
                                }
                                else
                                {
                                    midiInputEventListener.OnMidiNoteOn(sender, midiEventKind & 0xf, midiEventNote, midiEventVelocity);
                                }
                            }
                            midiState = MIDI_STATE_WAIT;
                            break;
                        case 0xa0: // control polyphonic key pressure
                            midiEventVelocity = midiEvent;
                            if (midiInputEventListener != null)
                            {
                                midiInputEventListener.OnMidiPolyphonicAftertouch(sender, midiEventKind & 0xf, midiEventNote, midiEventVelocity);
                            }
                            midiState = MIDI_STATE_WAIT;
                            break;
                        case 0xb0: // control change
                            midiEventVelocity = midiEvent;
                            switch (midiEventNote & 0x7f)
                            {
                                case 98:
                                    // NRPN LSB
                                    parameterNumber &= 0x3f80;
                                    parameterNumber |= midiEventVelocity & 0x7f;
                                    parameterMode = PARAMETER_MODE_NRPN;
                                    break;
                                case 99:
                                    // NRPN MSB
                                    parameterNumber &= 0x007f;
                                    parameterNumber |= (midiEventVelocity & 0x7f) << 7;
                                    parameterMode = PARAMETER_MODE_NRPN;
                                    break;
                                case 100:
                                    // RPN LSB
                                    parameterNumber &= 0x3f80;
                                    parameterNumber |= midiEventVelocity & 0x7f;
                                    parameterMode = PARAMETER_MODE_RPN;
                                    break;
                                case 101:
                                    // RPN MSB
                                    parameterNumber &= 0x007f;
                                    parameterNumber |= (midiEventVelocity & 0x7f) << 7;
                                    parameterMode = PARAMETER_MODE_RPN;
                                    break;
                                case 38:
                                    // data LSB
                                    parameterValue &= 0x3f80;
                                    parameterValue |= midiEventVelocity & 0x7f;

                                    if (parameterNumber != 0x3fff)
                                    {
                                        if (parameterMode == PARAMETER_MODE_RPN)
                                        {
                                            if (midiInputEventListener != null)
                                            {
                                                midiInputEventListener.OnRPNMessage(sender, midiEventKind & 0xf, parameterNumber & 0x3fff, parameterValue & 0x3fff);
                                            }
                                        }
                                        else if (parameterMode == PARAMETER_MODE_NRPN)
                                        {
                                            if (midiInputEventListener != null)
                                            {
                                                midiInputEventListener.OnNRPNMessage(sender, midiEventKind & 0xf, parameterNumber & 0x3fff, parameterValue & 0x3fff);
                                            }
                                        }
                                    }
                                    break;
                                case 6:
                                    // data MSB
                                    parameterValue &= 0x007f;
                                    parameterValue |= (midiEventVelocity & 0x7f) << 7;

                                    if (parameterNumber != 0x3fff)
                                    {
                                        if (parameterMode == PARAMETER_MODE_RPN)
                                        {
                                            if (midiInputEventListener != null)
                                            {
                                                midiInputEventListener.OnRPNMessage(sender, midiEventKind & 0xf, parameterNumber & 0x3fff, parameterValue & 0x3fff);
                                            }
                                        }
                                        else if (parameterMode == PARAMETER_MODE_NRPN)
                                        {
                                            if (midiInputEventListener != null)
                                            {
                                                midiInputEventListener.OnNRPNMessage(sender, midiEventKind & 0xf, parameterNumber & 0x3fff, parameterValue & 0x3fff);
                                            }
                                        }
                                    }
                                    break;
                            }

                            if (midiInputEventListener != null)
                            {
                                midiInputEventListener.OnMidiControlChange(sender, midiEventKind & 0xf, midiEventNote, midiEventVelocity);
                            }
                            midiState = MIDI_STATE_WAIT;
                            break;
                        case 0xe0: // pitch bend
                            midiEventVelocity = midiEvent;
                            if (midiInputEventListener != null)
                            {
                                midiInputEventListener.OnMidiPitchWheel(sender, midiEventKind & 0xf, (midiEventNote & 0x7f) | ((midiEventVelocity & 0x7f) << 7));
                            }
                            midiState = MIDI_STATE_WAIT;
                            break;
                        case 0xf0: // Song Position Pointer.
                            midiEventVelocity = midiEvent;
                            if (midiInputEventListener != null)
                            {
                                midiInputEventListener.OnMidiSongPositionPointer(sender, (midiEventNote & 0x7f) | ((midiEventVelocity & 0x7f) << 7));
                            }
                            midiState = MIDI_STATE_WAIT;
                            break;
                        default:
                            // illegal state
                            midiState = MIDI_STATE_WAIT;
                            break;
                    }
                }
                else if (midiState == MIDI_STATE_SIGNAL_SYSEX)
                {
                    if (midiEvent == 0xf7)
                    {
                        // the end of message
                        lock (systemExclusiveStream)
                        {
                            systemExclusiveStream.WriteByte((byte)midiEvent);
                            if (midiInputEventListener != null)
                            {
                                midiInputEventListener.OnMidiSystemExclusive(sender, systemExclusiveStream.ToArray());
                            }
                        }
                        midiState = MIDI_STATE_WAIT;
                    }
                    else
                    {
                        lock (systemExclusiveStream)
                        {
                            systemExclusiveStream.WriteByte((byte)midiEvent);
                        }
                    }
                }
            }

            public void parse(byte[] data)
            {
                foreach (byte dat in data)
                {
                    ParseMidiEvent(dat);
                }
            }
        }
    }
}
