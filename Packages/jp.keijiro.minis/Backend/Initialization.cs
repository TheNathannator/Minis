using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Scripting;

[assembly: AlwaysLinkAssembly]

namespace Minis.Backend
{
    /// <summary>
    /// Handles initialization of the package.
    /// </summary>
    internal static partial class Initialization
    {
        private static MidiBackend _backend;

        /// <summary>
        /// Initializes everything.
        /// </summary>
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#else
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.BeforeSceneLoad)]
#endif
        internal static void Initialize()
        {
#if UNITY_EDITOR
            // Uninstall the driver on domain reload.
            AssemblyReloadEvents.beforeAssemblyReload += Uninitialize;
            // Uninstall the driver when quitting.
            EditorApplication.quitting += Uninitialize;
#else
            // Uninstall the driver when quitting.
            Application.quitting += Uninitialize;
#endif

            if (MidiBackend.TryCreate(out var backend))
            {
                try
                {
                    backend.Start();
                    _backend = backend;

                    InputSystem.onBeforeUpdate += Update;
                    InputSystem.onDeviceChange += OnDeviceChange;
                }
                catch (Exception ex)
                {
                    backend.Dispose();
                    Logging.Exception($"Failed to start backend!", ex);
                }
            }
        }

        internal static void Uninitialize()
        {
#if UNITY_EDITOR
            AssemblyReloadEvents.beforeAssemblyReload -= Uninitialize;
            EditorApplication.quitting -= Uninitialize;
#else
            Application.quitting -= Uninitialize;
#endif

            try
            {
                InputSystem.onBeforeUpdate -= Update;
                InputSystem.onDeviceChange -= OnDeviceChange;

                _backend?.Dispose();
                _backend = null;
            }
            catch (Exception ex)
            {
                Logging.Exception("Failed to stop backend!", ex);
            }
        }

        private static void Update()
        {
            _backend?.Update();
        }

        private static void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            _backend?.OnDeviceChange(device, change);
        }
    }
}