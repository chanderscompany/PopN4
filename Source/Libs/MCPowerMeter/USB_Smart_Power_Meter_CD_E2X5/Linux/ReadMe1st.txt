Programming Instruction of using mini-circuits Power Sensor PWR-SEN-6GHS+.

The Power Sensor PWR-SEN-6GHS+ is an HID USB power sensor.
LINUX and Windows Programmers who familiar with open connection to
USB HID devices can easily communicate with the PWR-SEN-6GHS+.

To Open a connection to the power sensor Vendor ID and Product ID is required:

Mini-Circuits Vendor ID is: 0x20CE

Power Sensor Product ID is : 0x11


The communication with the sensor is done by USB Interrupt.
The Transmit and receive buffer size is 64 bytes.

Transmit Array should be 64 bytes
Receive  Array should be 64 bytes

commands:

1. Read Power Value:

To get the power value from the sensor you should send to the sensor code number 102
and also the Frequency value for the correction cal factors. 
The Frequency is in MHz and Should be Integer Value split into 2 bytes (MSB and LSB)

1st byte=102 
2nd byte = Frequency - MSB
3rd byte = Frequency - LSB

example: if you wish to read power and you works with frequency of 3000 MHz,
then send
1st byte=102
2nd byte=11
3rd byte=184

( 3000=11*256+184 )


The Power will be returned in the Receive array in 6 ascii charecters this way:
1st byte=102 - the sensor return the same code number.
2nd byte up to byte 7:  the reading power in ascii for example:   "-12.25"
byte 8 = 0



2. Set Low Noise Mode: ( this is the default mode of the sensor )
To set the sensor to Low Noise Mode code number 15 should be send and then the second byte should be 0
1st byte=15
2nd byte=0
The first byte of the receive array is 15

3. Set Faster Mode: 
To set the sensor to Faster Mode code number 15 should be send and then the second byte should be 1
1st byte=15
2nd byte=1
The first byte of the receive array is 15

4. Get Power Sensor Model Name
To get the sensor model name code number 104 should be send
1st byte=104
The model name will be return in the receive array in ascii characters ending with 0.
1st byte=104
2nd byte to the byte with value=0 = model name

5. Get Power Sensor Serial Number
To get the sensor Serial Number , code number 105 should be send
1st byte=105
The Serial Number will be return in the receive array in ascii characters ending with 0.
1st byte=105
2nd byte to the byte with value=0 = Serial Number

6. Read Device Temperature
To get the sensor Temperature , code number 103 should be send
1st byte=103
The Temperature will be return in the receive array in ascii characters ending with 0.
1st byte=103
2nd byte to the byte with value=0 = Device Temperature


Functions related only to FCPM-6000RC Model:

7. Freq. Counter - SetAutoFreq
Send Array:
1st byte=116
2nd byte = 1 for Auto Freq for cal factor or 0 for user defined.
Received Array:
1st byte=116

8. Freq. Counter - GetAutoFreq
Send Array:
1st byte=113
Received Array:
1st byte=113
2nd byte=1 or 0. 1 for Auto freq. 0 for user defined.

9. Freq. Counter - SetRange
Send Array:
1st byte=115
2nd byte = Range. 0 for auto 0r else can be 1,2,3 or 4.
Received Array:
1st byte=115

10. Freq. Counter - GetRange
Send Array:
1st byte=111
Received Array:
1st byte=111
2nd byte=Actual Range. can be 1,2,3 or 4.

11. Freq. Counter - GetRequestedRange
Send Array:
1st byte=135
Received Array:
1st byte=135
2nd byte=Requestedl Range. can be 0, 1,2,3 or 4.


12. Freq. Counter - SetSampleTime
Send Array:
1st byte=117
from 2nd byte - c format string of the Sample time in milliseconds ( example: "2000").
Received Array:
1st byte=117

13. Freq. Counter - GetSampleTime
Send Array:
1st byte=134
Received Array:
1st byte=134
from 2nd byte=c format string of the current Sample Time in milliseconds.

14. Freq. Counter - Read Frequency
Send Array:
1st byte=110
Received Array:
1st byte=110
from 2nd byte=c format string of the current Frequency Reading in Mhz.

15. Freq. Counter - GetRef
Send Array:
1st byte=112
Received Array:
1st byte=112
from 2nd byte=c format string of the Ref detected: "Extref" if Ext ref detected or "IntRef" if Internal Reference is used.









PowerMeter.tar.gz is a zip file with:
MCL_PM.c is an example source code using the libhid & libusb libraries to open the USB HID device.

MCL_PM is an executable to be run under LINUX terminal ,
  The command line is: sudo ./MCL_PM 10   where 10 is the frequency in MHZ (can use 1-6000).


The other file : PowerMeterUSB_LINUX is a GUI sample program under Linux using GTK.
You will need administrator/Root Rights to run it. 
Command line from terminal : sudo ./PowerMeterUSB_LINUX    
After the GUI launch , enter the frequency in the frequency text box.
It will give you the POWER and temperature.



 
 































