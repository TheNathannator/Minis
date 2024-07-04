using System;
using System.Collections.Generic;
using Minis.Native;
using UnityEngine.InputSystem;

using static Minis.Native.RtMidi;

namespace Minis
{
    /// <summary>
    /// Manages RtMidi and the devices it reads.
    /// </summary>
    internal sealed class MidiBackend : CustomInputBackend<MidiChannel>
    {
        private RtMidiInHandle _rtMidi;

        private List<MidiPort> _ports = new List<MidiPort>();

        public MidiBackend()
        {
            _rtMidi = rtmidi_in_create_default();
            if (_rtMidi == null || _rtMidi.IsInvalid)
                throw new Exception("Failed to create RtMidi handle!");
            if (!_rtMidi.Ok)
                throw new Exception($"Failed to create RtMidi handle: {_rtMidi.ErrorMessage}");
        }

        protected override void OnDispose()
        {
            foreach (var port in _ports)
                port?.Dispose();
            _ports.Clear();

            _rtMidi?.Dispose();
            _rtMidi = null;
        }

        protected override void OnUpdate()
        {
            // Check for port connections/disconnections
            uint portCount = rtmidi_get_port_count(_rtMidi);
            if (_rtMidi.Ok && portCount != _ports.Count)
            {
                // Completely refresh all MIDI devices
                // Not ideal, but no sane way to track which devices have been added/removed
                foreach (var port in _ports)
                    port.Dispose();
                _ports.Clear();

                for (uint i = 0; i < portCount; i++)
                    _ports.Add(new MidiPort(this, i));
            }
        }

        protected override MidiChannel OnDeviceAdded(InputDevice device, IDisposable context)
        {
            var channel = (MidiChannel)context;
            channel.OnAdded(device);
            return channel;
        }

        protected override void OnDeviceRemoved(MidiChannel channel)
        {
            channel.OnRemoved();
        }
    }
}