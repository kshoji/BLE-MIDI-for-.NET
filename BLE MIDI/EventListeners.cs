using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kshoji.BleMidi
{
    /// <summary>
    /// Listener for MIDI events
    /// </summary>
    public sealed class OnMidiInputEventListener
    {
        /// <summary>
        /// SysEx
        /// </summary>
        /// <param name="sender">the device sent this message</param>
        /// <param name="systemExclusive">received message</param>
        public delegate void SystemExclusiveDelegate(MidiInputDevice sender, byte[] systemExclusive);
        public event SystemExclusiveDelegate SystemExclusive;
        public void OnMidiSystemExclusive(MidiInputDevice sender, byte[] systemExclusive)
        {
            if (SystemExclusive != null)
            {
                SystemExclusive(sender, systemExclusive);
            }
        }

        /// <summary>
        /// Note-off
        /// </summary>
        /// <param name="sender">the device sent this message</param>
        /// <param name="channel">0-15</param>
        /// <param name="note">0-127</param>
        /// <param name="velocity">0-127</param>
        public delegate void NoteOffDelegate(MidiInputDevice sender, int channel, int note, int velocity);
        public event NoteOffDelegate NoteOff;
        public void OnMidiNoteOff(MidiInputDevice sender, int channel, int note, int velocity)
        {
            if (NoteOff != null)
            {
                NoteOff(sender, channel, note, velocity);
            }
        }


        /// <summary>
        /// Note-on
        /// </summary>
        /// <param name="sender">the device sent this message</param>
        /// <param name="channel">0-15</param>
        /// <param name="note">0-127</param>
        /// <param name="velocity">0-127</param>
        public delegate void NoteOnDelegate(MidiInputDevice sender, int channel, int note, int velocity);
        public event NoteOnDelegate NoteOn;
        public void OnMidiNoteOn(MidiInputDevice sender, int channel, int note, int velocity)
        {
            if (NoteOn != null)
            {
                NoteOn(sender, channel, note, velocity);
            }
        }

        /// <summary>
        /// Poly-KeyPress
        /// </summary>
        /// <param name="sender">the device sent this message</param>
        /// <param name="channel">0-15</param>
        /// <param name="note">0-127</param>
        /// <param name="pressure">0-127</param>
        public delegate void PolyphonicAftertouchDelegate(MidiInputDevice sender, int channel, int note, int pressure);
        public event PolyphonicAftertouchDelegate PolyphonicAftertouch;
        public void OnMidiPolyphonicAftertouch(MidiInputDevice sender, int channel, int note, int pressure)
        {
            if (PolyphonicAftertouch != null)
            {
                PolyphonicAftertouch(sender, channel, note, pressure);
            }
        }

        /// <summary>
        /// Control Change
        /// </summary>
        /// <param name="sender">the device sent this message</param>
        /// <param name="channel">0-15</param>
        /// <param name="function">0-127</param>
        /// <param name="value">0-127</param>
        public delegate void ControlChangeDelegate(MidiInputDevice sender, int channel, int function, int value);
        public event ControlChangeDelegate ControlChange;
        public void OnMidiControlChange(MidiInputDevice sender, int channel, int function, int value)
        {
            if (ControlChange != null)
            {
                ControlChange(sender, channel, function, value);
            }
        }

        /// <summary>
        /// Program Change
        /// </summary>
        /// <param name="sender">the device sent this message</param>
        /// <param name="channel">0-15</param>
        /// <param name="program">0-127</param>
        public delegate void ProgramChangeDelegate(MidiInputDevice sender, int channel, int program);
        public event ProgramChangeDelegate ProgramChange;
        public void OnMidiProgramChange(MidiInputDevice sender, int channel, int program)
        {
            if (ProgramChange != null)
            {
                ProgramChange(sender, channel, program);
            }
        }

        /// <summary>
        /// Channel Pressure
        /// </summary>
        /// <param name="sender">the device sent this message</param>
        /// <param name="channel">0-15</param>
        /// <param name="pressure">0-127</param>
        public delegate void ChannelAftertouchDelegate(MidiInputDevice sender, int channel, int pressure);
        public event ChannelAftertouchDelegate ChannelAftertouch;
        public void OnMidiChannelAftertouch(MidiInputDevice sender, int channel, int pressure)
        {
            if (ChannelAftertouch != null)
            {
                ChannelAftertouch(sender, channel, pressure);
            }
        }

        /// <summary>
        /// PitchBend Change
        /// </summary>
        /// <param name="sender">the device sent this message</param>
        /// <param name="channel">0-15</param>
        /// <param name="amount">0(low)-8192(center)-16383(high)</param>
        public delegate void PitchWheelDelegate(MidiInputDevice sender, int channel, int amount);
        public event PitchWheelDelegate PitchWheel;
        public void OnMidiPitchWheel(MidiInputDevice sender, int channel, int amount)
        {
            if (PitchWheel != null)
            {
                PitchWheel(sender, channel, amount);
            }
        }

        /// <summary>
        /// MIDI Time Code(MTC) Quarter Frame
        /// </summary>
        /// <param name="sender">the device sent this message</param>
        /// <param name="timing">0-127</param>
        public delegate void TimeCodeQuarterFrameDelegate(MidiInputDevice sender, int timing);
        public event TimeCodeQuarterFrameDelegate TimeCodeQuarterFrame;
        public void OnMidiTimeCodeQuarterFrame(MidiInputDevice sender, int timing)
        {
            if (TimeCodeQuarterFrame != null)
            {
                TimeCodeQuarterFrame(sender, timing);
            }
        }

        /// <summary>
        /// Song Select
        /// </summary>
        /// <param name="sender">the device sent this message</param>
        /// <param name="song">0-127</param>
        public delegate void SongSelectDelegate(MidiInputDevice sender, int song);
        public event SongSelectDelegate MidiSongSelect;
        public void OnMidiSongSelect(MidiInputDevice sender, int song)
        {
            if (MidiSongSelect != null)
            {
                MidiSongSelect(sender, song);
            }
        }

        /// <summary>
        /// Song Position Pointer
        /// </summary>
        /// <param name="sender">the device sent this message</param>
        /// <param name="position">0-16383</param>
        public delegate void SongPositionPointerDelegate(MidiInputDevice sender, int position);
        public event SongPositionPointerDelegate SongPositionPointer;
        public void OnMidiSongPositionPointer(MidiInputDevice sender, int position)
        {
            if (SongPositionPointer != null)
            {
                SongPositionPointer(sender, position);
            }
        }

        /// <summary>
        /// Tune Request
        /// </summary>
        /// <param name="sender">the device sent this message</param>
        public delegate void TuneRequestDelegate(MidiInputDevice sender);
        public event TuneRequestDelegate TuneRequest;
        public void OnMidiTuneRequest(MidiInputDevice sender)
        {
            if (TuneRequest != null)
            {
                TuneRequest(sender);
            }
        }

        /// <summary>
        /// Timing Clock
        /// </summary>
        /// <param name="sender">the device sent this message</param>
        public delegate void TimingClockDelegate(MidiInputDevice sender);
        public event TimingClockDelegate TimingClock;
        public void OnMidiTimingClock(MidiInputDevice sender)
        {
            if (TimingClock != null)
            {
                TimingClock(sender);
            }
        }

        /// <summary>
        /// Start Playing
        /// </summary>
        /// <param name="sender">the device sent this message</param>
        public delegate void StartDelegate(MidiInputDevice sender);
        public event StartDelegate Start;
        public void OnMidiStart(MidiInputDevice sender)
        {
            if (Start != null)
            {
                Start(sender);
            }
        }

        /// <summary>
        /// Continue Playing
        /// </summary>
        /// <param name="sender">the device sent this message</param>
        public delegate void ContinueDelegate(MidiInputDevice sender);
        public event ContinueDelegate Contiune;
        public void OnMidiContinue(MidiInputDevice sender)
        {
            if (Contiune != null)
            {
                Contiune(sender);
            }
        }

        /// <summary>
        /// Stop Playing
        /// </summary>
        /// <param name="sender">the device sent this message</param>
        public delegate void StopDelegate(MidiInputDevice sender);
        public event StopDelegate Stop;
        public void OnMidiStop(MidiInputDevice sender)
        {
            if (Stop != null)
            {
                Stop(sender);
            }
        }

        /// <summary>
        /// Active Sensing
        /// </summary>
        /// <param name="sender">the device sent this message</param>
        public delegate void ActiveSensingDelegate(MidiInputDevice sender);
        public event ActiveSensingDelegate ActiveSensing;
        public void OnMidiActiveSensing(MidiInputDevice sender)
        {
            if (ActiveSensing != null)
            {
                ActiveSensing(sender);
            }
        }

        /// <summary>
        /// Reset Device
        /// </summary>
        /// <param name="sender">the device sent this message</param>
        public delegate void ResetDelegate(MidiInputDevice sender);
        public event ResetDelegate Reset;
        public void OnMidiReset(MidiInputDevice sender)
        {
            if (Reset != null)
            {
                Reset(sender);
            }
        }

        /// <summary>
        /// RPN message<br />
        /// invoked when value's MSB or LSB changed
        /// </summary>
        /// <param name="sender">the device sent this message</param>
        /// <param name="channel">0-15</param>
        /// <param name="function">14bits</param>
        /// <param name="value">7 bits or 14 bits</param>
        public delegate void RPNMessageDelegate(MidiInputDevice sender, int channel, int function, int value);
        public event RPNMessageDelegate RPNMessage;
        public void OnRPNMessage(MidiInputDevice sender, int channel, int function, int value)
        {
            if (RPNMessage != null)
            {
                RPNMessage(sender, channel, function, value);
            }
        }

        /// <summary>
        /// NRPN message<br />
        /// invoked when value's MSB or LSB changed
        /// </summary>
        /// <param name="sender">the device sent this message</param>
        /// <param name="channel">0-15</param>
        /// <param name="function">14bits</param>
        /// <param name="value">7 bits or 14 bits</param>
        public delegate void NRPNMessageDelegate(MidiInputDevice sender, int channel, int function, int value);
        public event NRPNMessageDelegate NRPNMessage;
        public void OnNRPNMessage(MidiInputDevice sender, int channel, int function, int value)
        {
            if (NRPNMessage != null)
            {
                NRPNMessage(sender, channel, function, value);
            }
        }
    }

    /// <summary>
    /// Listener for MIDI attached events
    /// </summary>
    public interface OnMidiDeviceAttatchedListener
    {
        /// <summary>
        /// MIDI input device has been attached
        /// </summary>
        /// <param name="midiInputDevice"></param>
        void OnMidiInputDeviceAttached(MidiInputDevice midiInputDevice);

        /// <summary>
        /// MIDI output device has been attached
        /// </summary>
        /// <param name="midiOutputDevice"></param>
        void OnMidiOutputDeviceAttached(MidiOutputDevice midiOutputDevice);
    }

    /// <summary>
    /// Listener for MIDI detached events
    /// </summary>
    public interface OnMidiDeviceDetchedListener
    {
        /// <summary>
        /// MIDI input device has been detached
        /// </summary>
        /// <param name="midiInputDevice"></param>
        void OnMidiInputDeviceDetached(MidiInputDevice midiInputDevice);

        /// <summary>
        /// MIDI output device has been detached
        /// </summary>
        /// <param name="midiOutputDevice"></param>
        void OnMidiOutputDeviceDetached(MidiOutputDevice midiOutputDevice);
    }
}
