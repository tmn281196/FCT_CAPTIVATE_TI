using RJCP.IO.Ports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Touch_Panel.Model
{
    public class DeviceManager
    {
        public SerialPortStream MicomPort1 = new SerialPortStream();

        public SerialPortStream MicomPort2 = new SerialPortStream();

        public SerialPortStream SystemPort = new SerialPortStream();

        public SerialPortStream Solenoid1Port = new SerialPortStream();

        public SerialPortStream Solenoid2Port = new SerialPortStream();

        public SerialPortStream Solenoid3Port = new SerialPortStream();

        public readonly object PortLockMicom1 = new();
        public readonly object PortLockMicom2 = new();
        public readonly object PortLockSystem = new();
        public readonly object PortLockSol1 = new();
        public readonly object PortLockSol2 = new();
        public readonly object PortLockSol3 = new();


        public void Dispose()
        {

            MicomPort1.Dispose();
            MicomPort2.Dispose();
            SystemPort.Dispose();
            Solenoid1Port.Dispose();
            Solenoid2Port.Dispose();
            Solenoid3Port.Dispose();
        }
    }
}
