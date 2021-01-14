
// Methods SetupDevicePipe() and FindPipeAndInterface() are Copyright:
#region Copyright(c) Travis Robinson

// Copyright (c) 2011-2012 Travis Robinson <libusbdotnet@gmail.com>
// All rights reserved.
// 
// List.Devices
// 
// Last Updated: 03.08.2012
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 
//     * Redistributions of source code must retain the above copyright
//       notice, this list of conditions and the following disclaimer.
// 	  
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS 
// IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED 
// TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A 
// PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL TRAVIS LEE ROBINSON 
// BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR 
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF 
// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS 
// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN 
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) 
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF 
// THE POSSIBILITY OF SUCH DAMAGE.

#endregion


using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using libusbK;

//
/// Contains library-specific routines
/// to assist in passing data to/from a USB device.
/// This specific version works with UsbLibK devices.
/// David Carter, CIRES and NOAA/ESRL/PSD
/// Initial version 2014-01-14
//
namespace UsbHelper {

    class UsbDevice {

        public int VendorID { get; set; }
        public int ProductID { get; set; }
        public int AltInterfaceID { get; set; }
        public ICollection<EndPoint> EndPoints;  // any collection that implements ICollection, IEnumerable
        /*
        public const byte WRITE_PORTA_CMD = 0x01;
        public const byte READ_PORTA_CMD = 0x02;
        public const byte WRITE_CTRL_CMD = 0x05;
        public const byte READ_CTRL_CMD = 0x06;
        public const byte READ_PORTB_CMD = 0x09;
        public const byte WRITE_PORTB_CMD = 0x0A;
        public const byte READ_PORTD_CMD = 0x0B;
        public const byte WRITE_PORTD_CMD = 0x0C;
    */

        public enum PORTCOMMAND {
            WRITE_PORTA = 0x01,
            READ_PORTA = 0x02,
            WRITE_READBACKMODE = 0x04,
            WRITE_CTRL = 0x05,
            READ_CTRL = 0x06,
            WRITE_READBACKCNT = 0x7,
            READ_PORTB = 0x09,
            WRITE_AUTOCSB = 0x10,
            WRITE_PORTB = 0x0A,
            READ_PORTD = 0x0B,
            WRITE_PORTD = 0x0C
        }

        private int _altSetting;

        public UsbDevice() {
            VendorID = 0;
            ProductID = 0;
            AltInterfaceID = 0;
            _altSetting = AltInterfaceID;
            EndPoints = new List<EndPoint>();
        }

        public UsbDevice(int vid, int pid) {
            VendorID = vid;
            ProductID = pid;
            AltInterfaceID = 0;
            _altSetting = AltInterfaceID;
            EndPoints = new List<EndPoint>();
        }

        public UsbDevice(int vid, int pid, int altInterface) {
            VendorID = vid;
            ProductID = pid;
            AltInterfaceID = altInterface;
            _altSetting = AltInterfaceID;
            EndPoints = new List<EndPoint>();
        }

        public void ClearEndPoints() {
            EndPoints.Clear();
        }

        public EndPoint AddReadEndpoint(int endPoint) {
            EndPoint ep = new ReadEndPoint(VendorID, ProductID, AltInterfaceID, endPoint);
            EndPoints.Add(ep);
            return ep;
        }

        public EndPoint AddWriteEndpoint(int endPoint) {
            EndPoint ep = new WriteEndPoint(VendorID, ProductID, AltInterfaceID, endPoint);
            EndPoints.Add(ep);
            return ep;
        }

        /// <summary>
        /// Get the EndPoint object with exact match to ID epid
        /// </summary>
        /// <param name="epid"></param>
        /// <returns></returns>
        public EndPoint GetEndpoint(int epid) {
            foreach (EndPoint ep in EndPoints) {
                if (ep.EndPointID == epid) {
                    return ep;
                }
            }
            return null;
        }

        /// <summary>
        /// Get the EndPoint that is for read-only that has ID
        /// of epid or (epid | 0x80)
        /// </summary>
        /// <param name="epid"></param>
        /// <returns></returns>
        public EndPoint GetReadEndpoint(int epid) {
            foreach (EndPoint ep in EndPoints) {
                if ((ep is ReadEndPoint) || ((ep.EndPointID & 0x80) != 0x00)) {
                    // ep is read endpoint
                    // Does ID match?
                    if ((ep.EndPointID & 0x7f) == (epid & 0x7f)) {
                        return ep;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Get the EndPoint that is for write-only that has ID
        /// of epid (0x80 bit is cleared)
        /// </summary>
        /// <param name="epid"></param>
        /// <returns></returns>
        public EndPoint GetWriteEndpoint(int epid) {
            foreach (EndPoint ep in EndPoints) {
                if ((ep is WriteEndPoint) || ((ep.EndPointID & 0x80) == 0x00)) {
                    if (ep.EndPointID == epid) {
                        return ep;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Write a byte value to Port A,B,D or CTRL Port
        /// </summary>
        /// <param name="portCommand"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool WritePortValue(PORTCOMMAND portCommand, byte value) {

            EndPoint writeEP1 = GetWriteEndpoint(1);
            if (writeEP1 == null) {
                return false;
            }

            byte[] buf = new byte[2];
            int nBytes;

            buf[0] = (byte)portCommand; 
            buf[1] = value;

            //send 1 byte of data out on EP1OUT
            bool result = writeEP1.Write(buf, out nBytes);

            return result;
        }

        public bool WritePortValue(PORTCOMMAND portCommand, short value) {

            EndPoint writeEP1 = GetWriteEndpoint(1);
            if (writeEP1 == null) {
                return false;
            }

            byte[] buf = new byte[3];
            int nBytes;

            buf[0] = (byte)portCommand;
            buf[1] = MSB(value);
            buf[2] = LSB(value);

            bool result = writeEP1.Write(buf, out nBytes);

            return result;
        }


        // Gets the most significant byte from iVal and returns it
        private byte MSB(short iVal) {
            return (byte)(iVal / 256);
        }
        // Gets the lease significant byte from iVal and returns it
        private byte LSB(short iVal) {
            return (byte)(iVal & 0xFF);
        }

        /// <summary>
        /// Read a byte value to Port A,B,D or CTRL Port
        /// Returns as a short (not sign extended); -1 indicates an error
        /// </summary>
        /// <param name="portCommand"></param>
        /// <returns></returns>
        public short ReadPortValue(PORTCOMMAND portCommand) {

            EndPoint writeEP1 = GetWriteEndpoint(1);
            if (writeEP1 == null) {
                return -1;
            }

            EndPoint readEP1 = GetReadEndpoint(1);
            if (readEP1 == null) {
                return -1;
            }

            int nBytes;
            byte[] buf = new byte[2];
            byte[] data = new byte[2];

            buf[0] = (byte)portCommand;

            //send 1 byte of data out on EP1OUT
            bool success = writeEP1.Write(buf, out nBytes);

            if (success) {
                //read 1 byte back on EP1IN
                success = readEP1.Read(data, out nBytes);
            }

            if (success) {
                return data[0];
            }
            else {
                return -1;
            }
        }


    }  // end of class UsbDevice

    ////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// EndPoint class
    /// Properties include VID, PID, and AltInterface of an endpoint.
    /// Methods include reading and writing an endpoint.
    /// </summary>
    /// <remarks>
    /// NOTE: Some software identifies endpoints as 0x01 through 0x0F,
    ///     such that you can have, for example, a read endpoint 1 and
    ///     a write endpoint 1;
    ///     Other software sets bit 0x80 for read endpoints, so that
    ///     you have write endpoint 0x01 and read endpoint 0x81;
    ///     This object is designed to help handle either.
    ///     An EndPoint can be created as a derived object ReadEndPoint or
    ///     a WriteEndPoint OR
    ///     an EndPoint base object can be made read-only by setting 0x80 bit in ID.
    ///     In either case read endpoints have internal ID set to 0x8_, but can be
    ///     identified as a ReadEndPoint object with ID 0x0_
    ///     or an EndPoint object with ID 0x8_.
    /// </remarks>
    class EndPoint {

        public int Vid;
        public int Pid;
        public int AltInterface;
        public int EndPointID;
        public UsbK UsbEP;
        public string DeviceName;

        private EndPoint() { }  // no default constructor defined

        public EndPoint(int vid, int pid, int altInterface, int endPoint) {

            Vid = vid;
            Pid = pid;
            AltInterface = altInterface;
            EndPointID = endPoint;
            int[] pipeTimeoutMS = new[] { 3000 };

            USB_INTERFACE_DESCRIPTOR interfaceDescriptor;
            WINUSB_PIPE_INFORMATION pipeInfo;
            KLST_DEVINFO_HANDLE deviceInfo;
            bool success = SetupDevicePipe(Vid, Pid, AltInterface, endPoint, out UsbEP, out interfaceDescriptor, out pipeInfo, out deviceInfo);

            if (UsbEP != null) {
                bool bOK = UsbEP.SetPipePolicy(
                  (byte)endPoint,
                  (int)PipePolicyType.PIPE_TRANSFER_TIMEOUT,
                  Marshal.SizeOf(typeof(int)),
                  pipeTimeoutMS);
                DeviceName = deviceInfo.DeviceDesc;
            }
            else {
                DeviceName = "???";
            }
        }

        /// <summary>
        /// Configures the USB device for a particular pipe endpoint
        /// </summary>
        /// <param name="vid"></param>
        /// <param name="pid"></param>
        /// <param name="altInterfaceID">if unknown, try value of 1</param>
        /// <param name="endPoint">endpoints ORed with 0x80 are read, otherwise write</param>
        /// <param name="usbDev"></param>
        /// <param name="interfaceDescriptor"></param>
        /// <param name="pipeInfo"></param>
        /// <remarks>
        /// Copyright (c) 2011-2012 Travis Robinson <libusbdotnet@gmail.com>
        /// modified dac 20140124 to add deviceInfo as out parameter.
        /// </remarks>
        private bool SetupDevicePipe(int vid, int pid, int altInterfaceID, int endPoint,
                                            out UsbK usb, out USB_INTERFACE_DESCRIPTOR interfaceDescriptor,
                                            out WINUSB_PIPE_INFORMATION pipeInfo, out KLST_DEVINFO_HANDLE deviceInfo) {
            usb = null;
            interfaceDescriptor = new USB_INTERFACE_DESCRIPTOR();
            pipeInfo = new WINUSB_PIPE_INFORMATION();
            int MI = -1;

            bool success = false;

            // Use a patttern match to include only matching devices.
            // NOTE: You can use the '*' and '?' chars as wildcards for all chars or a single char (respectively). 
            KLST_PATTERN_MATCH patternMatch = new KLST_PATTERN_MATCH();
            if (MI != -1)
                patternMatch.DeviceID = String.Format("USB\\VID_{0:X4}&PID_{1:X4}&MI_{2:X2}*",
                                                        vid,
                                                        vid,
                                                        MI);
            else
                patternMatch.DeviceID = String.Format("USB\\VID_{0:X4}&PID_{1:X4}*",
                                                        vid,
                                                        pid);

            LstK deviceList = new LstK(KLST_FLAG.NONE,
                                        ref patternMatch);
            //KLST_DEVINFO_HANDLE deviceInfo;
            interfaceDescriptor = new USB_INTERFACE_DESCRIPTOR();
            pipeInfo = new WINUSB_PIPE_INFORMATION();
            usb = null;

            // Iterate the devices looking for a matching alt-interface and endpoint id.
            while (deviceList.MoveNext(out deviceInfo)) {
                // libusbK class contructors can throw exceptions; For instance, if the device is
                // using the WinUsb driver and already in-use by another application.
                usb = new UsbK(deviceInfo);

                //Console.WriteLine("Finding interface and endpoint by PipeId..");
                success = FindPipeAndInterface(usb,
                                                out interfaceDescriptor,
                                                out pipeInfo,
                                                altInterfaceID,
                                                endPoint);
                if (success) break;

                usb.Free();
                usb = null;
            }
            if (ReferenceEquals(usb, null)) {
                success = false;
            }

            //MI = interfaceDescriptor.bInterfaceNumber;
            //AltInterfaceId = interfaceDescriptor.bAlternateSetting;
            //PipeId = pipeInfo.PipeId;
            // Set interface alt setting.
            //Console.WriteLine("Setting interface #{0} to bAlternateSetting #{1}..",
            //                  interfaceDescriptor.bInterfaceNumber,
            //                  interfaceDescriptor.bAlternateSetting);

            if (success) {
                success = usb.SetAltInterface(interfaceDescriptor.bInterfaceNumber,
                                                false,
                                                interfaceDescriptor.bAlternateSetting);
            }

            deviceList.Free();

            return success;

        }  // end of SetupDevicePipe( )

        /// <summary>
        /// 
        /// </summary>
        /// <param name="usb"></param>
        /// <param name="interfaceDescriptor"></param>
        /// <param name="pipeInfo"></param>
        /// <param name="altInterfaceId"></param>
        /// <param name="pipeId"></param>
        /// <returns></returns>
        /// <remarks>
        /// Copyright (c) 2011-2012 Travis Robinson <libusbdotnet@gmail.com>
        /// </remarks>
        private bool FindPipeAndInterface(UsbK usb,
                                        out USB_INTERFACE_DESCRIPTOR interfaceDescriptor,
                                        out WINUSB_PIPE_INFORMATION pipeInfo,
                                        int altInterfaceId,
                                        int pipeId) {
            byte interfaceIndex = 0;
            interfaceDescriptor = new USB_INTERFACE_DESCRIPTOR();
            pipeInfo = new WINUSB_PIPE_INFORMATION();
            while (usb.SelectInterface(interfaceIndex, true)) {
                byte altSettingNumber = 0;
                while (usb.QueryInterfaceSettings(altSettingNumber,
                                                    out interfaceDescriptor)) {
                    if (altInterfaceId == -1 || altInterfaceId == altSettingNumber) {
                        byte pipeIndex = 0;
                        while (usb.QueryPipe(altSettingNumber,
                                                pipeIndex++,
                                                out pipeInfo)) {
                            //if ((pipeInfo.MaximumPacketSize > 0) &&
                            //    pipeId == -1 || pipeInfo.PipeId == pipeId ||
                            //    ((pipeId & 0xF) == 0 && ((pipeId & 0x80) == (pipeInfo.PipeId & 0x80)))) {
                            if ((pipeInfo.MaximumPacketSize > 0) &&
                                pipeId == -1 || pipeInfo.PipeId == pipeId) {
                                //Console.WriteLine("pipeIndex = {0}", pipeIndex-1);
                                goto FindInterfaceDone;
                            }
                            pipeInfo.PipeId = 0;
                        }
                    }
                    altSettingNumber++;
                }
                interfaceIndex++;
            }

        FindInterfaceDone:
            return pipeInfo.PipeId != 0;

        }  // end of FindPipeAndInterface()

        /// <summary>
        /// Writes an array of bytes to this endpoint
        /// </summary>
        /// <param name="buff"></param>
        /// <param name="numBytes"></param>
        /// <returns></returns>
        public bool Write(byte[] buff, out int numBytes) {
            numBytes = 0;
            bool success = UsbEP.WritePipe((byte)EndPointID, buff, buff.Length, out numBytes, IntPtr.Zero);
            return success;
        }

        public bool Read(byte[] data, out int numBytes) {
            numBytes = 0;
            bool success = UsbEP.ReadPipe((byte)EndPointID, data, data.Length, out numBytes, IntPtr.Zero);
            return success;
        }

    }  // end of class EndPoint

    class ReadEndPoint : EndPoint {
        // read endpoint has 0x80 bit set
        public ReadEndPoint(int vid, int pid, int altInterface, int endPoint) :
            base(vid, pid, altInterface, endPoint | 0x80) {
            // do nothing here; everything in base constructor
        }
    }

    class WriteEndPoint : EndPoint {
        // write endpoint has 0x80 bit cleared
        public WriteEndPoint(int vid, int pid, int altInterface, int endPoint) :
            base(vid, pid, altInterface, endPoint & 0x7f) {
            // do nothing here; eveerything in base constructor
        }
    }



}  // end of namespace
