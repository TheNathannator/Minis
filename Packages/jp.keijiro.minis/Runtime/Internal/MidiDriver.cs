using System;
using System.Collections.Generic;
using System.Threading;

namespace Minis
{
    //
    // MIDI device driver class that manages all MIDI ports (interfaces) found
    // in the system.
    //
    sealed class MidiDriver : IDisposable
    {
        #region Internal objects and methods

        volatile bool _keepReading = true;
        Thread _readThread;

        MidiProbe _probe = new MidiProbe();
        List<MidiPort> _ports = new List<MidiPort>();

        public MidiDriver()
        {
            _readThread = new Thread(ReadThread) { IsBackground = true };
            _readThread.Start();
        }

        ~MidiDriver()
        {
            Dispose(false);
        }

        void ReadThread()
        {
            while (_keepReading)
            {
                // Rescan the ports if the count of the ports doesn't match.
                if (_ports.Count != _probe.PortCount)
                {
                    DisposePorts();
                    ScanPorts();
                }

                // Process MIDI message queues in the port objects.
                foreach (var p in _ports) p.ProcessMessageQueue();

                Thread.Yield();
            }
        }

        void ScanPorts()
        {
            for (var i = 0; i < _probe.PortCount; i++)
                _ports.Add(new MidiPort(i, _probe.GetPortName(i)));
        }

        void DisposePorts()
        {
            foreach (var p in _ports) p.Dispose();
            _ports.Clear();
        }

        void Dispose(bool disposing)
        {
            _keepReading = false;

            if (disposing)
            {
                _readThread?.Join();
                _readThread = null;
            }

            DisposePorts();

            _probe?.Dispose();
            _probe = null;
        }

        #endregion

        #region Public methods

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}
