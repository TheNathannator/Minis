using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;

namespace Minis
{
    //
    // Thin wrapper class for communication between threads about device management.
    //
    class ThreadedMidiDevice
    {
        public InputDeviceDescription description;

        public volatile MidiDevice device = null;
    }

    //
    // Wrangler class that installs/uninstalls MIDI subsystems on system events
    // and handles many lower-level aspects that are necessary for correct threading.
    //
    static class MidiSystemWrangler
    {
        #region Internal objects and methods

        static MidiDriver _driver;

        // Devices must be added/removed on the main thread
        private static readonly ConcurrentBag<ThreadedMidiDevice> _additionQueue = new ConcurrentBag<ThreadedMidiDevice>();
        private static readonly ConcurrentBag<ThreadedMidiDevice> _removalQueue = new ConcurrentBag<ThreadedMidiDevice>();

        // Keep track of unclaimed devices to avoid leaking them if they don't get claimed
        private static readonly List<ThreadedMidiDevice> _unclaimedDevices = new List<ThreadedMidiDevice>();

        static readonly SlimEventBuffer[] _inputBuffers = new SlimEventBuffer[2];
        static readonly object _bufferLock = new object();
        static int _currentBuffer = 0;

        static void RegisterLayout()
        {
            MidiNoteControl.Initialize();
            MidiValueControl.Initialize();

            MidiDevice.Initialize();
        }

        internal static ThreadedMidiDevice QueueDeviceAddition(in InputDeviceDescription description)
        {
            var device = new ThreadedMidiDevice() { description = description };
            _additionQueue.Add(device);
            return device;
        }

        internal static void QueueDeviceRemoval(ThreadedMidiDevice device)
        {
            _removalQueue.Add(device);
        }

        internal static unsafe void QueueEvent(InputEventPtr eventPtr)
        {
            lock (_bufferLock)
            {
                _inputBuffers[_currentBuffer].AppendEvent(eventPtr);
            }
        }

        static void FlushEventBuffer()
        {
            SlimEventBuffer buffer;
            lock (_bufferLock)
            {
                buffer = _inputBuffers[_currentBuffer];
                _currentBuffer = (_currentBuffer + 1) % _inputBuffers.Length;
            }

            foreach (var eventPtr in buffer)
            {
                try
                {
                    InputSystem.QueueEvent(eventPtr);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Minis] Error when flushing an event: {ex}");
                }
            }

            buffer.Reset();
        }

        static void ProcessDeviceQueue()
        {
            // Process additions
            while (!_additionQueue.IsEmpty && _additionQueue.TryTake(out var pending))
            {
                pending.device = (MidiDevice)InputSystem.AddDevice(pending.description);
                _unclaimedDevices.Add(pending);
            }

            // Process removals
            while (!_removalQueue.IsEmpty && _removalQueue.TryTake(out var pending))
            {
                InputSystem.RemoveDevice(pending.device);
                _unclaimedDevices.Remove(pending);
            }
        }

        #endregion

        #region System initialization/finalization callback

        internal static void Initialize()
        {
            RegisterLayout();
            _driver = new MidiDriver();

            // Initialize event buffers
            for (int i = 0; i < _inputBuffers.Length; i++)
            {
                _inputBuffers[i] = new SlimEventBuffer();
            }

            // We use the input system's onBeforeUpdate callback to queue events
            // just before it processes its event queue. This is called in both
            // edit mode and play mode.
            InputSystem.onBeforeUpdate += Update;
        }

        internal static void Uninitialize()
        {
            InputSystem.onBeforeUpdate -= Update;

            _driver?.Dispose();
            _driver = null;
        }

        #endregion

        #region Update loop

        static void Update()
        {
            FlushEventBuffer();
            ProcessDeviceQueue();
        }

        #endregion
    }
}
