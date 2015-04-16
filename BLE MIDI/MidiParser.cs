using System;
using System.IO;
using System.Threading;

namespace kshoji.BleMidi
{
    /// <summary>
    /// Represents BLE MIDI Input Device
    /// </summary>
    public partial class MidiInputDevice
    {
        private int midiState = MIDI_STATE_TIMESTAMP;
        private byte midiEventKind = 0;
        private byte midiEventNote = 0;
        private byte midiEventVelocity = 0;

        // for RPN/NRPN messages
        private const int PARAMETER_MODE_NONE = 0;
        private const int PARAMETER_MODE_RPN = 1;
        private const int PARAMETER_MODE_NRPN = 2;
        private int parameterMode = PARAMETER_MODE_NONE;
        private int parameterNumber = 0x3fff;
        private int parameterValue = 0x3fff;

        private readonly ReusableMemoryStream systemExclusiveStream = new ReusableMemoryStream();
        private readonly ReusableMemoryStream systemExclusiveRecoveryStream = new ReusableMemoryStream();

        private const int MIDI_STATE_TIMESTAMP = 0;
        private const int MIDI_STATE_WAIT = 1;
        private const int MIDI_STATE_SIGNAL_2BYTES_2 = 21;
        private const int MIDI_STATE_SIGNAL_3BYTES_2 = 31;
        private const int MIDI_STATE_SIGNAL_3BYTES_3 = 32;
        private const int MIDI_STATE_SIGNAL_SYSEX = 41;

        // for Timestamp
        private const int MAX_TIMESTAMP = 8192;
        private const int BUFFER_LENGTH_MILLIS = 10;
        private bool useTimestamp = true;
        private int timestamp = 0;
        private int lastTimestamp;
        private long lastTimestampRecorded = 0;
        private int zeroTimestampCount = 0;

        private readonly object systemExclusiveLock = new object();

        private int calculateTimeToWait(int timestamp)
        {
            long currentTimeMillis = Environment.TickCount;
            if (lastTimestampRecorded == 0)
            {
                // first time
                lastTimestamp = timestamp;
                lastTimestampRecorded = currentTimeMillis;
                return 0;
            }

            if (currentTimeMillis - lastTimestampRecorded > MAX_TIMESTAMP)
            {
                // the event comes after long pause
                lastTimestamp = timestamp;
                lastTimestampRecorded = currentTimeMillis;
                zeroTimestampCount = 0;
                return 0;
            }

            if (timestamp == 0)
            {
                zeroTimestampCount++;
                if (zeroTimestampCount >= 3)
                {
                    // decides timestamp is always zero: event fires immediately
                    useTimestamp = false;
                    return 0;
                }
            }
            else
            {
                zeroTimestampCount = 0;
            }

            int originalTimestamp = timestamp;
            if (timestamp < lastTimestamp)
            {
                timestamp += MAX_TIMESTAMP;
            }

            int result = BUFFER_LENGTH_MILLIS + timestamp - lastTimestamp - (int)(currentTimeMillis - lastTimestampRecorded);
            //        Log.d(Constants.TAG, "timestamp: " + timestamp + ", lastTimestamp: " + lastTimestamp + ", currentTimeMillis: " + currentTimeMillis + ", lastTimestampRecorded:" + lastTimestampRecorded + ", wait: " + result);
            lastTimestamp = originalTimestamp;
            lastTimestampRecorded = currentTimeMillis;
            return result;
        }

        /// <summary>
        /// The MemoryStream can reusable
        /// </summary>
        private class ReusableMemoryStream : MemoryStream
        {
            /// <summary>
            /// Replace the last datum of the Stream.
            /// </summary>
            /// <param name="data">data to replaced</param>
            /// <returns>null if nothing has written</returns>
            protected internal byte? ReplaceLastByte(byte data)
            {
                if (Position == 0)
                {
                    return null;
                }

                byte[] buffer = new byte[1];
                Read(buffer, (int)Position - 1, 1);
                Seek(Position - 1, SeekOrigin.Begin);
                WriteByte(data);

                return buffer[0];
            }

            /// <summary>
            /// Reset the Stream
            /// </summary>
            protected internal void Reset()
            {
                SetLength(0);
            }
        }

        /// <summary>
        /// The data class for sending the event data
        /// </summary>
        private class MidiTimerObject
        {
            private const int INVALID = -1;
            readonly int arg1;
            readonly int arg2;
            readonly int arg3;
            readonly byte[] array;

            protected internal int Arg1
            {
                get { return arg1; }
            }
            protected internal int Arg2
            {
                get { return arg2; }
            }
            protected internal int Arg3
            {
                get { return arg3; }
            }
            protected internal byte[] Array
            {
                get { return array; }
            }

            private MidiTimerObject(int arg1, int arg2, int arg3, byte[] array)
            {
                this.arg1 = arg1;
                this.arg2 = arg2;
                this.arg3 = arg3;
                this.array = array;
            }

            /// <summary>
            /// Constructor with no arguments
            /// </summary>
            protected internal MidiTimerObject() : this(INVALID, INVALID, INVALID, null) { }

            /// <summary>
            /// Constructor with 1 argument
            /// </summary>
            /// <param name="arg1">argument 1</param>
            protected internal MidiTimerObject(int arg1) : this(arg1, INVALID, INVALID, null) { }

            /// <summary>
            /// Constructor with 2 arguments
            /// </summary>
            /// <param name="arg1">argument 1</param>
            /// <param name="arg2">argument 2</param>
            protected internal MidiTimerObject(int arg1, int arg2) : this(arg1, arg2, INVALID, null) { }

            /// <summary>
            /// Constructor with 3 arguments
            /// </summary>
            /// <param name="arg1">argument 1</param>
            /// <param name="arg2">argument 2</param>
            /// <param name="arg3">argument 3</param>
            protected internal MidiTimerObject(int arg1, int arg2, int arg3) : this(arg1, arg2, arg3, null) { }

            /// <summary>
            /// Constructor with array
            /// </summary>
            /// <param name="array">data</param>
            protected internal MidiTimerObject(byte[] array) : this(INVALID, INVALID, INVALID, array) { }
        }

        /// <summary>
        /// MIDI Event parser
        /// </summary>
        /// <param name="header">the header data</param>
        /// <param name="data">single byte data</param>
        private void ParseMidiEvent(byte header, byte data)
        {
            byte midiEvent = data;
            int timeToWait;

            if (midiState == MIDI_STATE_TIMESTAMP)
            {
                if ((midiEvent & 0x80) == 0)
                {
                    // running status
                    midiState = MIDI_STATE_WAIT;
                }

                if (midiEvent == 0xf7)
                {
                    // is this end of SysEx???
                    lock (systemExclusiveLock)
                    {
                        if (systemExclusiveRecoveryStream.Length > 0)
                        {
                            // previous SysEx has been failed, due to timestamp was 0xF7
                            // process SysEx again

                            // last written byte is for timestamp
                            byte? removed = systemExclusiveRecoveryStream.ReplaceLastByte((byte)midiEvent);

                            if (removed.HasValue && removed >= 0)
                            {
                                timestamp = ((header & 0x3f) << 7) | (removed.Value & 0x7f);
                                timeToWait = calculateTimeToWait(timestamp);

                                if (useTimestamp && timeToWait > 0)
                                {
                                    new Timer(obj =>
                                    {
                                        if (SystemExclusive != null)
                                        {
                                            SystemExclusive(this, ((MidiTimerObject)obj).Array);
                                        }
                                    }, new MidiTimerObject(systemExclusiveRecoveryStream.ToArray()), timeToWait, Timeout.Infinite);
                                }
                                else
                                {
                                    if (SystemExclusive != null)
                                    {
                                        SystemExclusive(this, systemExclusiveRecoveryStream.ToArray());
                                    }
                                }
                            }

                            systemExclusiveRecoveryStream.SetLength(0);
                        }

                        // process next byte with state: MIDI_STATE_TIMESTAMP
                        midiState = MIDI_STATE_TIMESTAMP;
                        return;
                    }
                }
                else
                {
                    // there is no error. reset the stream for recovery
                    lock (systemExclusiveLock)
                    {
                        if (systemExclusiveRecoveryStream.Length > 0)
                        {
                            systemExclusiveRecoveryStream.SetLength(0);
                        }
                    }
                }
            }

            if (midiState == MIDI_STATE_TIMESTAMP)
            {
                timestamp = ((header & 0x3f) << 7) | (midiEvent & 0x7f);
                midiState = MIDI_STATE_WAIT;
            }
            else if (midiState == MIDI_STATE_WAIT)
            {
                switch (midiEvent & 0xf0)
                {
                    case 0xf0:
                        {
                            switch (midiEvent)
                            {
                                case 0xf0:
                                    lock (systemExclusiveLock)
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
                                    timeToWait = calculateTimeToWait(timestamp);
                                    if (useTimestamp && timeToWait > 0)
                                    {
                                        new Timer(obj =>
                                        {
                                            if (TuneRequest != null)
                                            {
                                                TuneRequest(this);
                                            }
                                        }, null, timeToWait, Timeout.Infinite);
                                    }
                                    else
                                    {
                                        if (TuneRequest != null)
                                        {
                                            TuneRequest(this);
                                        }
                                    }
                                    midiState = MIDI_STATE_TIMESTAMP;
                                    break;
                                case 0xf8:
                                    // 0xf8 Timing Clock : 1byte
                                    timeToWait = calculateTimeToWait(timestamp);
                                    if (useTimestamp && timeToWait > 0)
                                    {
                                        new Timer(obj =>
                                        {
                                            if (TimingClock != null)
                                            {
                                                TimingClock(this);
                                            }
                                        }, null, timeToWait, Timeout.Infinite);
                                    }
                                    else
                                    {
                                        if (TimingClock != null)
                                        {
                                            TimingClock(this);
                                        }
                                    }
                                    midiState = MIDI_STATE_TIMESTAMP;
                                    break;
                                case 0xfa:
                                    // 0xfa Start : 1byte
                                    timeToWait = calculateTimeToWait(timestamp);
                                    if (useTimestamp && timeToWait > 0)
                                    {
                                        new Timer(obj =>
                                        {
                                            if (Start != null)
                                            {
                                                Start(this);
                                            }
                                        }, null, timeToWait, Timeout.Infinite);
                                    }
                                    else
                                    {
                                        if (Start != null)
                                        {
                                            Start(this);
                                        }
                                    }
                                    midiState = MIDI_STATE_TIMESTAMP;
                                    break;
                                case 0xfb:
                                    // 0xfb Continue : 1byte
                                    timeToWait = calculateTimeToWait(timestamp);
                                    if (useTimestamp && timeToWait > 0)
                                    {
                                        new Timer(obj =>
                                        {
                                            if (Continue != null)
                                            {
                                                Continue(this);
                                            }
                                        }, null, timeToWait, Timeout.Infinite);
                                    }
                                    else
                                    {
                                        if (Continue != null)
                                        {
                                            Continue(this);
                                        }
                                    }
                                    midiState = MIDI_STATE_TIMESTAMP;
                                    break;
                                case 0xfc:
                                    // 0xfc Stop : 1byte
                                    timeToWait = calculateTimeToWait(timestamp);
                                    if (useTimestamp && timeToWait > 0)
                                    {
                                        new Timer(obj =>
                                        {
                                            if (Stop != null)
                                            {
                                                Stop(this);
                                            }
                                        }, null, timeToWait, Timeout.Infinite);
                                    }
                                    else
                                    {
                                        if (Stop != null)
                                        {
                                            Stop(this);
                                        }
                                    }
                                    midiState = MIDI_STATE_TIMESTAMP;
                                    break;
                                case 0xfe:
                                    // 0xfe Active Sensing : 1byte
                                    timeToWait = calculateTimeToWait(timestamp);
                                    if (useTimestamp && timeToWait > 0)
                                    {
                                        new Timer(obj =>
                                        {
                                            if (ActiveSensing != null)
                                            {
                                                ActiveSensing(this);
                                            }
                                        }, null, timeToWait, Timeout.Infinite);
                                    }
                                    else
                                    {
                                        if (ActiveSensing != null)
                                        {
                                            ActiveSensing(this);
                                        }
                                    }
                                    midiState = MIDI_STATE_TIMESTAMP;
                                    break;
                                case 0xff:
                                    // 0xff Reset : 1byte
                                    timeToWait = calculateTimeToWait(timestamp);
                                    if (useTimestamp && timeToWait > 0)
                                    {
                                        new Timer(obj =>
                                        {
                                            if (Reset != null)
                                            {
                                                Reset(this);
                                            }
                                        }, null, timeToWait, Timeout.Infinite);
                                    }
                                    else
                                    {
                                        if (Reset != null)
                                        {
                                            Reset(this);
                                        }
                                    }
                                    midiState = MIDI_STATE_TIMESTAMP;
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
                        timeToWait = calculateTimeToWait(timestamp);
                        if (useTimestamp && timeToWait > 0)
                        {
                            new Timer(obj =>
                            {
                                if (ProgramChange != null)
                                {
                                    ProgramChange(this, ((MidiTimerObject)obj).Arg1, ((MidiTimerObject)obj).Arg2);
                                }
                            }, new MidiTimerObject(midiEventKind & 0xf, midiEventNote), timeToWait, Timeout.Infinite);
                        }
                        else
                        {
                            if (ProgramChange != null)
                            {
                                ProgramChange(this, midiEventKind & 0xf, midiEventNote);
                            }
                        }
                        midiState = MIDI_STATE_TIMESTAMP;
                        break;
                    case 0xd0: // channel after-touch
                        midiEventNote = midiEvent;
                        timeToWait = calculateTimeToWait(timestamp);
                        if (useTimestamp && timeToWait > 0)
                        {
                            new Timer(obj =>
                            {
                                if (ChannelAftertouch != null)
                                {
                                    ChannelAftertouch(this, ((MidiTimerObject)obj).Arg1, ((MidiTimerObject)obj).Arg2);
                                }
                            }, new MidiTimerObject(midiEventKind & 0xf, midiEventNote), timeToWait, Timeout.Infinite);
                        }
                        else
                        {
                            if (ChannelAftertouch != null)
                            {
                                ChannelAftertouch(this, midiEventKind & 0xf, midiEventNote);
                            }
                        }
                        midiState = MIDI_STATE_TIMESTAMP;
                        break;
                    case 0xf0:
                        {
                            switch (midiEventKind)
                            {
                                case 0xf1:
                                    // 0xf1 MIDI Time Code Quarter Frame. : 2bytes
                                    midiEventNote = midiEvent;
                                    timeToWait = calculateTimeToWait(timestamp);
                                    if (useTimestamp && timeToWait > 0)
                                    {
                                        new Timer(obj =>
                                        {
                                            if (TimeCodeQuarterFrame != null)
                                            {
                                                TimeCodeQuarterFrame(this, ((MidiTimerObject)obj).Arg1);
                                            }
                                        }, new MidiTimerObject(midiEventNote), timeToWait, Timeout.Infinite);
                                    }
                                    else
                                    {
                                        if (TimeCodeQuarterFrame != null)
                                        {
                                            TimeCodeQuarterFrame(this, midiEventNote);
                                        }
                                    }
                                    midiState = MIDI_STATE_TIMESTAMP;
                                    break;
                                case 0xf3:
                                    // 0xf3 Song Select. : 2bytes
                                    midiEventNote = midiEvent;
                                    timeToWait = calculateTimeToWait(timestamp);
                                    if (useTimestamp && timeToWait > 0)
                                    {
                                        new Timer(obj =>
                                        {
                                            if (SongSelect != null)
                                            {
                                                SongSelect(this, ((MidiTimerObject)obj).Arg1);
                                            }
                                        }, new MidiTimerObject(midiEventNote), timeToWait, Timeout.Infinite);
                                    }
                                    else
                                    {
                                        if (SongSelect != null)
                                        {
                                            SongSelect(this, midiEventNote);
                                        }
                                    }
                                    midiState = MIDI_STATE_TIMESTAMP;
                                    break;
                                default:
                                    // illegal state
                                    midiState = MIDI_STATE_TIMESTAMP;
                                    break;
                            }
                        }
                        break;
                    default:
                        // illegal state
                        midiState = MIDI_STATE_TIMESTAMP;
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
                        midiState = MIDI_STATE_TIMESTAMP;
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
                        timeToWait = calculateTimeToWait(timestamp);
                        if (useTimestamp && timeToWait > 0)
                        {
                            new Timer(obj =>
                            {
                                if (NoteOff != null)
                                {
                                    NoteOff(this, ((MidiTimerObject)obj).Arg1, ((MidiTimerObject)obj).Arg2, ((MidiTimerObject)obj).Arg3);
                                }
                            }, new MidiTimerObject(midiEventKind & 0xf, midiEventNote, midiEventVelocity), timeToWait, Timeout.Infinite);
                        }
                        else
                        {
                            if (NoteOff != null)
                            {
                                NoteOff(this, midiEventKind & 0xf, midiEventNote, midiEventVelocity);
                            }
                        }
                        midiState = MIDI_STATE_TIMESTAMP;
                        break;
                    case 0x90: // note on
                        midiEventVelocity = midiEvent;
                        if (midiEventVelocity == 0)
                        {
                            timeToWait = calculateTimeToWait(timestamp);
                            if (useTimestamp && timeToWait > 0)
                            {
                                new Timer(obj =>
                                {
                                    if (NoteOff != null)
                                    {
                                        NoteOff(this, ((MidiTimerObject)obj).Arg1, ((MidiTimerObject)obj).Arg2, ((MidiTimerObject)obj).Arg3);
                                    }
                                }, new MidiTimerObject(midiEventKind & 0xf, midiEventNote, midiEventVelocity), timeToWait, Timeout.Infinite);
                            }
                            else
                            {
                                if (NoteOff != null)
                                {
                                    NoteOff(this, midiEventKind & 0xf, midiEventNote, midiEventVelocity);
                                }
                            }
                        }
                        else
                        {
                            timeToWait = calculateTimeToWait(timestamp);
                            if (useTimestamp && timeToWait > 0)
                            {
                                new Timer(obj =>
                                {
                                    if (NoteOn != null)
                                    {
                                        NoteOn(this, ((MidiTimerObject)obj).Arg1, ((MidiTimerObject)obj).Arg2, ((MidiTimerObject)obj).Arg3);
                                    }
                                }, new MidiTimerObject(midiEventKind & 0xf, midiEventNote, midiEventVelocity), timeToWait, Timeout.Infinite);
                            }
                            else
                            {
                                if (NoteOn != null)
                                {
                                    NoteOn(this, midiEventKind & 0xf, midiEventNote, midiEventVelocity);
                                }
                            }
                        }
                        midiState = MIDI_STATE_TIMESTAMP;
                        break;
                    case 0xa0: // control polyphonic key pressure
                        midiEventVelocity = midiEvent;
                        timeToWait = calculateTimeToWait(timestamp);
                        if (useTimestamp && timeToWait > 0)
                        {
                            new Timer(obj =>
                            {
                                if (PolyphonicAftertouch != null)
                                {
                                    PolyphonicAftertouch(this, ((MidiTimerObject)obj).Arg1, ((MidiTimerObject)obj).Arg2, ((MidiTimerObject)obj).Arg3);
                                }
                            }, new MidiTimerObject(midiEventKind & 0xf, midiEventNote, midiEventVelocity), timeToWait, Timeout.Infinite);
                        }
                        else
                        {
                            if (PolyphonicAftertouch != null)
                            {
                                PolyphonicAftertouch(this, midiEventKind & 0xf, midiEventNote, midiEventVelocity);
                            }
                        }
                        midiState = MIDI_STATE_TIMESTAMP;
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

                        timeToWait = calculateTimeToWait(timestamp);
                        if (useTimestamp && timeToWait > 0)
                        {
                            new Timer(obj =>
                            {
                                if (ControlChange != null)
                                {
                                    ControlChange(this, ((MidiTimerObject)obj).Arg1, ((MidiTimerObject)obj).Arg2, ((MidiTimerObject)obj).Arg3);
                                }
                            }, new MidiTimerObject(midiEventKind & 0xf, midiEventNote, midiEventVelocity), timeToWait, Timeout.Infinite);
                        }
                        else
                        {
                            if (ControlChange != null)
                            {
                                ControlChange(this, midiEventKind & 0xf, midiEventNote, midiEventVelocity);
                            }
                        }
                        midiState = MIDI_STATE_TIMESTAMP;
                        break;
                    case 0xe0: // pitch bend
                        midiEventVelocity = midiEvent;
                        timeToWait = calculateTimeToWait(timestamp);
                        if (useTimestamp && timeToWait > 0)
                        {
                            new Timer(obj =>
                            {
                                if (PitchWheel != null)
                                {
                                    PitchWheel(this, ((MidiTimerObject)obj).Arg1, ((MidiTimerObject)obj).Arg2);
                                }
                            }, new MidiTimerObject(midiEventKind & 0xf, (midiEventNote & 0x7f) | ((midiEventVelocity & 0x7f) << 7)), timeToWait, Timeout.Infinite);
                        }
                        else
                        {
                            if (PitchWheel != null)
                            {
                                PitchWheel(this, midiEventKind & 0xf, (midiEventNote & 0x7f) | ((midiEventVelocity & 0x7f) << 7));
                            }
                        }
                        midiState = MIDI_STATE_TIMESTAMP;
                        break;
                    case 0xf0: // Song Position Pointer.
                        midiEventVelocity = midiEvent;
                        timeToWait = calculateTimeToWait(timestamp);
                        if (useTimestamp && timeToWait > 0)
                        {
                            new Timer(obj =>
                            {
                                if (SongPositionPointer != null)
                                {
                                    SongPositionPointer(this, ((MidiTimerObject)obj).Arg1);
                                }
                            }, new MidiTimerObject((midiEventNote & 0x7f) | ((midiEventVelocity & 0x7f) << 7)), timeToWait, Timeout.Infinite);
                        }
                        else
                        {
                            if (SongPositionPointer != null)
                            {
                                SongPositionPointer(this, (midiEventNote & 0x7f) | ((midiEventVelocity & 0x7f) << 7));
                            }
                        }
                        midiState = MIDI_STATE_TIMESTAMP;
                        break;
                    default:
                        // illegal state
                        midiState = MIDI_STATE_TIMESTAMP;
                        break;
                }
            }
            else if (midiState == MIDI_STATE_SIGNAL_SYSEX)
            {
                if (midiEvent == 0xf7)
                {
                    // the end of message
                    lock (systemExclusiveLock)
                    {
                        systemExclusiveStream.WriteByte((byte)midiEvent);
                        timeToWait = calculateTimeToWait(timestamp);
                        if (useTimestamp && timeToWait > 0)
                        {
                            new Timer(obj =>
                            {
                                if (SystemExclusive != null)
                                {
                                    SystemExclusive(this, ((MidiTimerObject)obj).Array);
                                }
                            }, new MidiTimerObject(systemExclusiveStream.ToArray()), timeToWait, Timeout.Infinite);
                        }
                        else
                        {
                            if (SystemExclusive != null)
                            {
                                SystemExclusive(this, systemExclusiveStream.ToArray());
                            }
                        }
                    }
                    midiState = MIDI_STATE_TIMESTAMP;
                }
                else
                {
                    lock (systemExclusiveLock)
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
        public void ParseMidiEvent(byte[] data)
        {
            // System.Diagnostics.Debug.WriteLine("data: " + string.Join(", ", data));

            if (data.Length > 1)
            {
                byte header = data[0];

                byte[] newData = new byte[data.Length - 1];
                Array.Copy(data, 1, newData, 0, newData.Length);

                foreach (byte datum in newData)
                {
                    ParseMidiEvent(header, datum);
                }
            }
        }
    }
}
