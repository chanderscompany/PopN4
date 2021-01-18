
INSTALLATION INSTRUCTIONS FOR POPN
	All necessary files should be found in POPNExtras folder


Install Intel IPP from CD
		for IA32 processor (32-bit)
		custom install - no demo, no static libs, no tools
	ONLY need to do this to create proper IPP DLL's.
	If DLL's already created, just copy them to executable folder.
	NOTE: 32- and 64-bit compiled versions should be in install folder, 
		copy all from correct folder to POPN4 executable folder.
---
Run TVicPortInstall41.exe for hardware I/O access
---
Install .NET 4.0 from dotnetfx... file
	May be instructed to run Windows Installer update and/or WIC installer.
---
Install DAQ software from DAQ CD
  OR
Install DAQ COM software from file daqcom2setup.exe and daqviewsetup.exe
---
Install AD9959 Eval Board software from CD
	(before connecting DDS device)
  OR  Run AD9959_Setup1.0.exe from this install folder 
---
Run LibUsbDotNetSetup program for DDS AD9959 (POPN3)
	http://sourceforge.net/projects/libusbdotnet/ 
	LibUsb-win32 is installed at same time.
		Plug in AD9959 (dismiss Found Hardware dialog)
		run inf-wizard (10/29/2010 2.2.8.104)
		  if AD9959 is in list, select it, else
		    (create new) to create *.inf file for specific device:
			VID = 0x456
			PID = 0xEE07
		specify folder for output
		select title for files (e.g. DDS_USB_20110818)
		select name for device (e.g. AD9959)
		next ->
		creates folder (DDS_USB_20110818) in output folder
			which contains drivers, etc.
		click Install Driver

	When plug in DDS USB first time -> found new hardware
		Should let Windows find driver, else
		Select install from specified folder and choose 
			folder with the above driver files
			or this POPN Install folder (?).

AD9959 for use with POPN4 (either 32- or 64-bit OS) must be LibUsbK device.
	Run libusbK-inf-wizard.exe
	Select LibUsbK from driver list.
	Select device 0456:ee06  or 0456:ee07 from device list.
	Install drivers.


Installation files not on CD can be found in POPN Extras folder.

Copy IPP 32-bit dll's from subdirectory to executable directory.

