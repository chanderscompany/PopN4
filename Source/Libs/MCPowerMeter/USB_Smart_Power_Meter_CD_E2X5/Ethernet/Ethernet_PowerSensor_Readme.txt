
Controlling the Device with LAN/Ethernet:

Communication with the device by Ethernet can be via either static, or dynamic IP Address. 
Factory default is Dynamic IP Address using TCP/IP Port 80 for HTTP Protocol and Port 23 for Telnet Protocol.

Dynamic IP: Device is assigned the local Subnet mask, network gateway and a valid local IP address by 
the network server each time it is connected to the network. the only parameters the user can control in this mode are

1. HTTP Port - The TCP/IP Port the device will use to communicate with the network when operating via HTTP control.
   May be set by user to desired value.

Note:  If HTTP PORT is other than 80 it must be included in every HTTP command, see example below.
       Telnet Port is 23 and can not be changed.

2. Password - a Password (up to 20 characters) can be used to secure commands to the device. 
Factory default is not to use a password.


Static IP: All parameters must be specified by the user using the USB Interface:

1. IP Address - The IP Address of the device - must be a legal and unique IP in the local Network.
2. Subnet Mask - The Subnet Mask of your Local Network.
3. Network Gateway - The Network Gateway of your Firewall.

Tip: In Windows the Subnet Mask and Network Gateway can be found by running the command "IPConfig".

4. HTTP Port - The TCP/IP Port the device will use to communicate with the network when operating via HTTP control.
May be set by user to desired value in the 1-255 range.

Note:  If HTTP PORT is other than 80 it must be included in every HTTP command, see example below.
       Telnet Port is 23 and can not be changed.

5. Password - a Password (up to 20 characters) can be used to secure commands to the device.
Factory default is not to use a password.

Note: clicking on on the arrow between the Dynamic and static IP configurations will copy the current
Dynamic configuration to the static configuration.


Clicking on the "Store..." Button will store all the Ethernet parameters in the device and reset the 
device allowing the parameters to come into effect immediatly.

Communication With the Device with HTTP Protocol:


Communication with the device is done by Sending HTTP commands (Get/Post HTTP).
Get/Post HTTP is very common and simple to implement in almost any Programming Langauge.
Internet Browser may be used as a console/tester.

every command will start with "http://DeviceIP/"
If the PORT is other than 80 then the HTTP will have to start with: "http://DeviceIP:PORT/"
(80 is the default HTTP PORT and can be ommited).

the second part of the HTTP command can be:
1. COMMAND - [SCPI Command]
2. QUERY - [SCPI Query]
3. PWD=[Password].

If using password then every HTTP command should be include the PWD=[password].

Example of typical HTTP Query for Reading the Power is:
1. When Password is not used and port=80 :
http://10.0.100.100/:POWER?
2. When PORT=800
http://10.0.100.100:800/:POWER?
3. When PORT=800, Password is required and is 1234
http://10.0.100.100:800/PWD=1234;:POWER?

if the device recieve the http command then it will return the answer in Ascii format.


Example of a typical HTTP command for Setting the Frequency to 1500 MHz:
1. When Password is not used and port=80 :
http://10.0.100.100/:FREQ:1500
2. When PORT=800
http://10.0.100.100:800/:FREQ:1500
3. When PORT=800, Password is required and is 1234
http://10.0.100.100:800/PWD=1234;:FREQ:1500

if the device recieve the http command then it will return "1"

SCPI_Command List: (Supported by model: PWR-SEN-8GHS-RC)
:FREQ:<value>   - Value is always in MHz Example:  :FREQ:2355
:TEMP:FORMAT:<X> - X can be "C" for Celsius ( °C) or "F" for Fahrenheit (°F)
:MODE:<mode> - valid number for mode is: 0 for Low Noise mode, 1 for faster mode and 2 (if supported)  for Very fast mode. Example: :MODE:1
:AVG:COUNT<count> - Set the Avg Count. example: :AVG:COUNT:8
:AVG:STATE:X -  if X=1 then Enable AVG. if X=0 - disable AVG. (default state is 0 (Disable).

SCPI QUERY List:  (Supported by SSG-6400HS)
:POWER?           Example:   :POWER?   return: -22.05 dBm.
:FREQ?   - Get the current Frequency ( as it set for cal factor calculation ).
:MN?  - Get the Power Sensor Model Mame
:SN? - Get the Power Sensor Serial Number.
:FIRMWARE? - Get the Firmware.
:TEMP? - Get the device temperature.
:TEMP:FORMAT? - Get the current Temperature format. 
:MODE?    - Get the Sensor mode: 0 for Low Noise 1 for fast mode , 2 for fastest mode.
:AVG:STATE? - return 1 if the AVG is enabled or 0 if Disabled
:AVG:COUNT? - get the AVG count value.
:VOLTAGE? - reading voltage.


SCPI commands/Queries related only to Peak Power Sensor Model PWR-SEN-8P-RC:

:TRIGGER:MODE:FREE  ( :TRIGGER:MODE? )  - set trigger mode to Free
:TRIGGER:MODE:INTERNAL  ( :TRIGGER:MODE? )
:TRIGGER:MODE:EXTERNAL  ( :TRIGGER:MODE? )
:TRIGGER:EXTERNAL:ONFALL ( :TRIGGER:EXTERNAL:ONFALL? )
:TRIGGER:EXTERNAL:ONRISE ( :TRIGGER:EXTERNAL:ONRISE? )
:TRIGGER:DELAY:[Delay_usec] ( :TRIGGER:DELAY? ) - set a delay value right after the trigger before sampling the signal.
:EXTOUT:SELECT:VIDEO  ( :EXTOUT:SELECT? ) 
:EXTOUT:SELECT:TRIG   ( :EXTOUT:SELECT? ) 
:SAMPLETIME:[SampleTime_usec]  ( :SAMPLETIME? ) - set the sample time valid value is integer between 10 to 1000000

:POWER_ARRAY?      -  read the power array values of the first package ( package #0 ). First 2 numbers are Number of packages and the array size
:POWER_ARRAY_EP1?  -  read package #1
:POWER_ARRAY_EP2?  -  read package #2
:POWER_ARRAY_EP3?  -  read package #3
:POWER_ARRAY_EP4?  -  read package #4
:POWER_ARRAY_EP5?  -  read package #5
:POWER_ARRAY_EP6?  -  read package #6
:POWER_ARRAY_EP7?  -  read package #7
:POWER_ARRAY_EP8?  -  read package #8
:POWER_ARRAY_EP9?  -  read package #9
:POWER_ARRAY_EP10?  - read package #10





SCPI commands/Queries related only to FCPM-6000RC Model:

:FC:RANGE:<range> - set the freq. counter range
:FC:SAMPLETIME:<sampletime> - set the sample time of the Freq. Counter. sample time in msec can be from 100 to 3000.
:FC:AUTOFREQ:<autofreq> - Set the autofreq indicator for the Frquency counter. 1 for yes. 0 for user defined CAL frequncy.

:FC:FREQ?POWER? - Get the latest reading of the Frequeny (Mhz)  and the power (dbm).
:FC:FREQ? - Get the latest reading of the Frequeny (Mhz).
:FC:REF? - Get the Frequency External ref detector. return ExtRef or IntRef..
:FC:RANGE? - Get the Actual current range.
:FC:RRANGE? - get the last user requested range.
:FC:SAMPLETIME? - get the Freq. Counter Sample time .
:FC:AUTOFREQ? - Get the autofreq indicator for the Frquency counter. 1 for yes. 0 for user defined CAL frequncy.





For the entire SCAPI Command and Query List refer to the Programming Guide of the specific device.

Communication With the Device with Telnet Protocol:

Communication with the device started by creating a Telnet Connection and receiving LF (Line feed) from the Device.
if Password is required then the first command that must be sent is: PWD=[Password].
any legal command can be send in Telnet and must be followed by LF (Line Feed).


