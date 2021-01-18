The *.hex file is the firmware for the AD9959 DDS.
Run fx2loader to load firmware to generic default USB device 0xee06,
	which will then renumerate to AD9959 device 0xee07:
	fx2loader -v 0456:EE06 AD9959_FW.hex
	RunFx2Loader does this.
First time on each PC, run libusbK-inf-wizard.exe and create Libusbk driver for device 0xee06,
	then after fx2loader, run wizard on device 0xee07.  Only need to do this once.

fx2loader MUST be run each time the DDS is powered up.
This is AUTOMATICALLY DONE in POPN4 at the beginning of the worker thread.
