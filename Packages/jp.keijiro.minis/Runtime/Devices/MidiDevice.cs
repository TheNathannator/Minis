using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;

namespace Minis
{
    [Serializable]
    internal class MidiDeviceCapabilities
    {
        public int channel;
    }

    //
    // Custom input device class that processes input from a MIDI channel
    //
    [InputControlLayout(stateType = typeof(MidiDeviceState), displayName = "MIDI Device")]
    public sealed class MidiDevice : InputDevice
    {
        /// <summary>
        /// The current <see cref="MidiDevice"/>.
        /// </summary>
        public static MidiDevice current { get; private set; }

        /// <summary>
        /// A collection of all <see cref="MidiDevice"/>s currently connected to the system.
        /// </summary>
        public new static IReadOnlyList<MidiDevice> all => s_AllDevices;
        private static readonly List<MidiDevice> s_AllDevices = new List<MidiDevice>();

        internal static void Initialize()
        {
            InputSystem.RegisterLayout<MidiDevice>(
                matches: new InputDeviceMatcher().WithInterface("Minis")
            );
        }

        // MIDI channel number
        //
        // The first channel returns 0.
        public int channel { get; private set; }

        private MidiButtonControl[] _notes;
        private MidiAxisControl[] _cc;

        public MidiAxisControl pitchBend { get; private set; }

        // Get an input control object bound for a specific note.
        public MidiButtonControl GetNote(int noteNumber)
            => _notes[noteNumber];

        // Get an input control object bound for a specific control element (CC).
        public MidiAxisControl GetCC(int controlNumber)
            => _cc[controlNumber];

        protected override void FinishSetup()
        {
            base.FinishSetup();

            // Populate the input controls.
            _notes = new MidiButtonControl[128];
            for (var i = 0; i < 128; i++)
            {
                _notes[i] = GetChildControl<MidiButtonControl>("note" + i.ToString("D3"));
            }

            _cc = new MidiAxisControl[120];
            for (var i = 0; i < 120; i++)
            {
                _cc[i] = GetChildControl<MidiAxisControl>("cc" + i.ToString("D3"));
            }

            pitchBend = GetChildControl<MidiAxisControl>("pitchBend");

            // Retrieve capability info
            var capabilities = new MidiDeviceCapabilities()
            {
                channel = -1, // Default for the all-channel device
            };

            try
            {
                JsonUtility.FromJsonOverwrite(description.capabilities, capabilities);
            }
            catch
            {
                // swallow errors silently
            }

            channel = capabilities.channel;
        }

        /// <inheritdoc/>
        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        protected override void OnAdded()
        {
            base.OnAdded();
            s_AllDevices.Add(this);
        }

        protected override void OnRemoved()
        {
            base.OnRemoved();
            s_AllDevices.Remove(this);
            if (current == this)
                current = null;
        }
    }

} // namespace Minis
