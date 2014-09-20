using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Diagnostics;

namespace kshoji.BleMidi
{
    public class BleMidilCentralProvider
    {
        IReadOnlyList<MidiInputDevice> midiInputDevices;
        IReadOnlyList<MidiOutputDevice> midiOutputDevices;

        public async Task listupDevices()
        {
            // TODO
            midiInputDevices = await MidiInputDevice.GetInstances();
            midiOutputDevices = await MidiOutputDevice.GetInstances();

            var onMidiInputEventListener = new OnMidiInputEventListener();
            onMidiInputEventListener.NoteOn += onMidiInputEventListener_NoteOn;
            foreach (var midiInputDevice in midiInputDevices)
            {
                midiInputDevice.SetOnMidiInputEventListener(onMidiInputEventListener);
            }
        }

        void onMidiInputEventListener_NoteOn(MidiInputDevice sender, int channel, int note, int velocity)
        {
            Debug.WriteLine("Note On:" + note);
        }
    }
}
