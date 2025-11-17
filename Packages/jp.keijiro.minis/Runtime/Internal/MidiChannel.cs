using System;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;

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

        public void ProcessNotePressure(byte note, byte velocity)
        {
            if (_device == null)
                return;

            velocity = Math.Max(velocity, (byte)1);
            _backend.QueueDeltaStateEvent(_device.GetNote(note), ref velocity);
        }

        public void ProcessCC(byte cc, byte value)
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

        public void ProcessChannelPressure(byte value)
        {
            if (_device == null)
                return;

            _backend.QueueDeltaStateEvent(_device.channelPressure, ref value);
        }

        public void ProcessPlaybackButton(MidiPlaybackButton button)
        {
            if (_device == null)
                return;

            byte value = (byte)button;
            _backend.QueueDeltaStateEvent(_device, MidiDeviceState.PlaybackButtonsOffset, ref value);
        }

        public void ResetAllNotes()
        {
            if (_device == null)
                return;

            for (byte note = 0; note < 128; note++)
            {
                byte velocity = 0;
                _backend.QueueDeltaStateEvent(_device.GetNote(note), ref velocity);
                _activeNotes[note] = false;
            }
        }

        public void ResetAllCC()
        {
            if (_device == null)
                return;

            for (byte cc = 0; cc < 120; cc++)
            {
                byte value = 0;
                _backend.QueueDeltaStateEvent(_device.GetCC(cc), ref value);
            }
        }

        public void FullReset()
        {
            if (_device == null)
                return;

            var state = new MidiDeviceState()
            {
                pitchBend = 8192,
            };
            _backend.QueueStateEvent(_device, ref state);
        }
    }
}