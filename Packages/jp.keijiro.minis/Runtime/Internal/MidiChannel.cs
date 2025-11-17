using System;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;

namespace Minis
{
    /// <summary>
    /// A single channel output on a MIDI device.
    /// </summary>
    internal sealed class MidiChannel : IDisposable
    {
        private MidiBackend _backend;
        private MidiDevice _device;

        public readonly int channelNumber;
        public MidiDevice device => _device;

        private readonly bool[] _activeNotes = new bool[128];

        public MidiChannel(MidiBackend backend, string portName, int channel)
        {
            _backend = backend;
            channelNumber = channel;

            var description = new InputDeviceDescription()
            {
                interfaceName = "Minis",
            };

            if (channel < 0)
            {
                description.product = portName + " (All Channels)";
            }
            else
            {
                description.product = portName + " Channel " + channel;
                description.capabilities = "{\"channel\":" + channel + "}";
            }

            _backend.QueueDeviceAdd(description, this);
        }

        // Only exists so we can avoid allocating an extra object for the add context
        void IDisposable.Dispose() {}

        public void OnAdded(InputDevice device)
        {
            _device = (MidiDevice)device;
        }

        public void OnRemoved()
        {
            _device = null;
        }

        public void ProcessNoteOn(byte note, byte velocity)
        {
            if (_device == null)
                return;

            // Consecutive note ons need to have a note off inserted in-between
            if (_activeNotes[note])
                ProcessNoteOff(note);

            _backend.QueueDeltaStateEvent(_device.GetNote(note), ref velocity);
            _activeNotes[note] = true;
        }

        public void ProcessNoteOff(byte note)
        {
            if (_device == null)
                return;

            byte velocity = 0;
            _backend.QueueDeltaStateEvent(_device.GetNote(note), ref velocity);
            _activeNotes[note] = false;
        }

        public void ProcessControlChange(byte cc, byte value)
        {
            if (_device == null)
                return;

            _backend.QueueDeltaStateEvent(_device.GetCC(cc), ref value);
        }

        public void ProcessPitchBend(ushort value)
        {
            if (_device == null)
                return;

            _backend.QueueDeltaStateEvent(_device.pitchBend, ref value);
        }
    }
}