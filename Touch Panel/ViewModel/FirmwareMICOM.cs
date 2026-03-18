using BSL430_NET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Touch_Panel.ViewModel
{
    public static class FirmwareMICOM
    {
        public static void WriteFirmware(string portName, string firmwareFilePath, Bsl430NetEventHandler progressChangedEventHandler, EventHandler<string> progressFailedEventHandler)
        {
            try
            {
                using (var dev = new BSL430NET(portName))
                {
                    try
                    {
                        dev.ProgressChanged += new Bsl430NetEventHandler(progressChangedEventHandler);

                        dev.SetBaudRate(BaudRate.BAUD_9600);
                        dev.SetMCU(MCU.MSP430_FR2x33);
                        dev.SetInvokeMechanism(InvokeMechanism.SHARED_JTAG);


                        if (File.Exists(firmwareFilePath))
                        {
                            StatusEx result = dev.Upload($"{firmwareFilePath}");
                        }
                        else
                        {
                            throw new FileNotFoundException("Firmware file does not exist.");
                        }

                    }
                    catch (Exception ex)
                    {
                        progressFailedEventHandler?.Invoke(portName, ex.Message);
                        return;
                    }
                }

            }
            catch (Exception ex)
            {
                progressFailedEventHandler?.Invoke(null, ex.Message);
                return;
            }
        }
    }

}
