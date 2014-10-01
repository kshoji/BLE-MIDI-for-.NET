using System.IO;

namespace kshoji.BleMidi
{
    /// <summary>
    /// Represents BLE MIDI Input Device
    /// </summary>
    public partial class MidiInputDevice
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

        /// <summary>
        /// MIDI Event parser
        /// </summary>
        /// <param name="data">single byte data</param>
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
                                    if (TuneRequest != null)
                                    {
                                        TuneRequest(this);
                                    }
                                    midiState = MIDI_STATE_WAIT;
                                    break;
                                case 0xf8:
                                    // 0xf8 Timing Clock : 1byte
                                    if (TimingClock != null)
                                    {
                                        TimingClock(this);
                                    }
                                    midiState = MIDI_STATE_WAIT;
                                    break;
                                case 0xfa:
                                    // 0xfa Start : 1byte
                                    if (Start != null)
                                    {
                                        Start(this);
                                    }
                                    midiState = MIDI_STATE_WAIT;
                                    break;
                                case 0xfb:
                                    // 0xfb Continue : 1byte
                                    if (Continue != null)
                                    {
                                        Continue(this);
                                    }
                                    midiState = MIDI_STATE_WAIT;
                                    break;
                                case 0xfc:
                                    // 0xfc Stop : 1byte
                                    if (Stop != null)
                                    {
                                        Stop(this);
                                    }
                                    midiState = MIDI_STATE_WAIT;
                                    break;
                                case 0xfe:
                                    // 0xfe Active Sensing : 1byte
                                    if (ActiveSensing != null)
                                    {
                                        ActiveSensing(this);
                                    }
                                    midiState = MIDI_STATE_WAIT;
                                    break;
                                case 0xff:
                                    // 0xff Reset : 1byte
                                    if (Reset != null)
                                    {
                                        Reset(this);
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
                        if (ProgramChange != null)
                        {
                            ProgramChange(this, midiEventKind & 0xf, midiEventNote);
                        }
                        midiState = MIDI_STATE_WAIT;
                        break;
                    case 0xd0: // channel after-touch
                        midiEventNote = midiEvent;
                        if (ChannelAftertouch != null)
                        {
                            ChannelAftertouch(this, midiEventKind & 0xf, midiEventNote);
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
                                    if (TimeCodeQuarterFrame != null)
                                    {
                                        TimeCodeQuarterFrame(this, midiEventNote);
                                    }
                                    midiState = MIDI_STATE_WAIT;
                                    break;
                                case 0xf3:
                                    // 0xf3 Song Select. : 2bytes
                                    midiEventNote = midiEvent;
                                    if (SongSelect != null)
                                    {
                                        SongSelect(this, midiEventNote);
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
                        if (NoteOff != null)
                        {
                            NoteOff(this, midiEventKind & 0xf, midiEventNote, midiEventVelocity);
                        }
                        midiState = MIDI_STATE_WAIT;
                        break;
                    case 0x90: // note on
                        midiEventVelocity = midiEvent;
                        if (midiEventVelocity == 0)
                        {
                            if (NoteOff != null)
                            {
                                NoteOff(this, midiEventKind & 0xf, midiEventNote, midiEventVelocity);
                            }
                        }
                        else
                        {
                            if (NoteOn != null)
                            {
                                NoteOn(this, midiEventKind & 0xf, midiEventNote, midiEventVelocity);
                            }
                        }
                        midiState = MIDI_STATE_WAIT;
                        break;
                    case 0xa0: // control polyphonic key pressure
                        midiEventVelocity = midiEvent;
                        if (PolyphonicAftertouch != null)
                        {
                            PolyphonicAftertouch(this, midiEventKind & 0xf, midiEventNote, midiEventVelocity);
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
                                        if (RPNMessage != null)
                                        {
                                            RPNMessage(this, midiEventKind & 0xf, parameterNumber & 0x3fff, parameterValue & 0x3fff);
                                        }
                                    }
                                    else if (parameterMode == PARAMETER_MODE_NRPN)
                                    {
                                        if (NRPNMessage != null)
                                        {
                                            NRPNMessage(this, midiEventKind & 0xf, parameterNumber & 0x3fff, parameterValue & 0x3fff);
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
                                        if (RPNMessage != null)
                                        {
                                            RPNMessage(this, midiEventKind & 0xf, parameterNumber & 0x3fff, parameterValue & 0x3fff);
                                        }
                                    }
                                    else if (parameterMode == PARAMETER_MODE_NRPN)
                                    {
                                        if (NRPNMessage != null)
                                        {
                                            NRPNMessage(this, midiEventKind & 0xf, parameterNumber & 0x3fff, parameterValue & 0x3fff);
                                        }
                                    }
                                }
                                break;
                        }

                        if (ControlChange != null)
                        {
                            ControlChange(this, midiEventKind & 0xf, midiEventNote, midiEventVelocity);
                        }
                        midiState = MIDI_STATE_WAIT;
                        break;
                    case 0xe0: // pitch bend
                        midiEventVelocity = midiEvent;
                        if (PitchWheel != null)
                        {
                            PitchWheel(this, midiEventKind & 0xf, (midiEventNote & 0x7f) | ((midiEventVelocity & 0x7f) << 7));
                        }
                        midiState = MIDI_STATE_WAIT;
                        break;
                    case 0xf0: // Song Position Pointer.
                        midiEventVelocity = midiEvent;
                        if (SongPositionPointer != null)
                        {
                            SongPositionPointer(this, (midiEventNote & 0x7f) | ((midiEventVelocity & 0x7f) << 7));
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
                        if (SystemExclusive != null)
                        {
                            SystemExclusive(this, systemExclusiveStream.ToArray());
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

        /// <summary>
        /// Parses array of MIDI data
        /// </summary>
        /// <param name="data">raw MIDI bytes</param>
        public void parse(byte[] data)
        {
            foreach (byte dat in data)
            {
                ParseMidiEvent(dat);
            }
        }
    }
}
