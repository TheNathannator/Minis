using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;

namespace Minis
{
    //
    // Wrangler class that installs/uninstalls MIDI subsystems on system events
    // and handles many lower-level aspects that are necessary for correct threading.
    //
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoad]
#endif
    static class MidiSystemWrangler
    {
        #region Internal objects and methods

        static MidiDriver _driver;

        static readonly SlimEventBuffer[] _inputBuffers = new SlimEventBuffer[2];
        static readonly object _bufferLock = new object();
        static int _currentBuffer = 0;

        static void RegisterLayout()
        {
            InputSystem.RegisterLayout<MidiNoteControl>("MidiNote");
            InputSystem.RegisterLayout<MidiValueControl>("MidiValue");

            InputSystem.RegisterLayout<MidiDevice>(
                matches: new InputDeviceMatcher().WithInterface("Minis")
            );
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

        #endregion

        #region System initialization/finalization callback

        #if UNITY_EDITOR

        //
        // On Editor, we use InitializeOnLoad to install the subsystem. At the
        // same time, we use AssemblyReloadEvents to temporarily disable the
        // system to avoid issue #1192379.
        // #FIXME This workaround should be removed when the issue is solved.
        //

        static MidiSystemWrangler()
        {
            Initialize();
        }

        #endif

        //
        // On Player, we use RuntimeInitializeOnLoadMethod to install the
        // subsystems.
        //

    #if !UNITY_EDITOR
        [UnityEngine.RuntimeInitializeOnLoadMethod]
    #endif
        static void Initialize()
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

        #if UNITY_EDITOR
            // Uninstall the driver on domain reload.
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += Uninitialize;
            // Uninstall the driver when quitting.
            UnityEditor.EditorApplication.quitting += Uninitialize;
        #else
            // Uninstall the driver when quitting.
            Application.quitting += Uninitialize;
        #endif
        }

        static void Uninitialize()
        {
            InputSystem.onBeforeUpdate -= Update;

        #if UNITY_EDITOR
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload -= Uninitialize;
            UnityEditor.EditorApplication.quitting -= Uninitialize;
        #else
            Application.quitting -= Uninitialize;
        #endif

            _driver?.Dispose();
            _driver = null;
        }

        #endregion

        #region Update loop

        static void Update()
        {
            _driver?.Update();
            FlushEventBuffer();
        }

        #endregion
    }
}
