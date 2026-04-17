using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;

using static Minis.Backend.RtMidi;

namespace Minis.Backend
{
    /// <summary>
    /// Manages RtMidi and the devices it reads.
    /// </summary>
    internal sealed class MidiBackend : CustomInputBackend<MidiChannel, MidiChannel>
    {
        private RtMidiInHandle _rtMidi;

        // Track port count separately, for better handling of error scenarios
        private uint _lastPortCount = 0;
        private List<MidiPort> _ports = new List<MidiPort>();

        private MidiBackend(RtMidiInHandle rtMidi)
        {
            _rtMidi = rtMidi;
        }

        protected override void OnDispose()
        {
            _rtMidi?.Dispose();
            _rtMidi = null;
        }

        public static bool TryCreate(out MidiBackend backend)
        {
            backend = null;

            RtMidiInHandle rtMidi = null;
            try
            {
                rtMidi = rtmidi_in_create_default();
                if (rtMidi == null || rtMidi.IsInvalid)
                {
                    Logging.Error("Failed to create RtMidi handle!");
                    return false;
                }

                if (!rtMidi.Ok)
                {
                    Logging.Error($"Failed to create RtMidi handle: {rtMidi.ErrorMessage}");
                    rtMidi.Dispose();
                    return false;
                }

                backend = new MidiBackend(rtMidi);
                return true;
            }
            catch (DllNotFoundException)
            {
                rtMidi?.Dispose();
                Logging.Message("Could not load RtMidi, MIDI input will not be available.");
                return false;
            }
            catch (Exception ex)
            {
                rtMidi?.Dispose();
                Logging.Exception("Failed to create MIDI backend!", ex);
                return false;
            }
        }

        protected override void OnStop()
        {
            foreach (var port in _ports)
                port?.Dispose();
            _ports.Clear();
            _lastPortCount = 0;
        }

        protected override void OnUpdate()
        {
            // Check for port connections/disconnections
            uint portCount = rtmidi_get_port_count(_rtMidi);
            if (!_rtMidi.Ok)
            {
                Logging.Error($"Failed to get RtMidi port count: {_rtMidi.ErrorMessage}");
                return;
            }

            if (portCount != _lastPortCount)
            {
                RefreshPorts(portCount);
            }

            foreach (var port in _ports)
            {
                if (!port.IsAlive())
                {
                    RefreshPorts(portCount);
                    break;
                }
            }
        }

        private void RefreshPorts(uint portCount)
        {
            // Update port count first so we don't repeatedly attempt to open ports that failed
            _lastPortCount = portCount;

            // Completely refresh all MIDI devices
            // Not ideal, but no sane way to track which devices have been added/removed
            foreach (var port in _ports)
                port.Dispose();
            _ports.Clear();

            for (uint port = 0; port < portCount; port++)
            {
                // Attempt port open 3 times
                for (int i = 0; i < 3; i++)
                {
                    try
                    {
                        _ports.Add(new MidiPort(this, port));
                        break;
                    }
                    catch (Exception ex)
                    {
                        Logging.Exception($"Failed to open MIDI port (attempt {i + 1})", ex);
                    }
                }
            }
        }

        protected override MidiChannel OnDeviceAdded(InputDevice device, MidiChannel channel)
        {
            channel.OnAdded(device);
            return channel;
        }

        protected override void OnDeviceRemoved(MidiChannel channel)
        {
            channel.OnRemoved();
        }
    }
}