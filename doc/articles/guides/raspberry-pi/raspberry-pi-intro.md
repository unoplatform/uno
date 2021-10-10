# Getting Started with Uno Platform and the Raspberry Pi

In this series of posts we'll build a cloud connected Raspberry Pi GUI app to perform some sstraightforward GPIO (General Purpose Input Output) operations.

1. [Introduction and Getting Started](raspberry-pi-intro.md)
2. [GPIO Control](raspberry-pi-gpio.md)
3. [Connecting to an Azure IoT Hub](raspberry-pi-azure.md)

## Prerequisites

For this series of posts, you'll need various peices of hardware and an Azure Account;

- Raspberry Pi 3b+ and above (I'll be using a [4Gb Pi 4](https://shop.pimoroni.com/products/raspberry-pi-4?variant=29157087445075)) 
- [Raspberry Pi Power Supply](https://shop.pimoroni.com/products/universal-usb-c-power-supply-5-1v-3a)
- [16GB SD Card](https://amzn.to/2YAI07e)
- SSH Client (Both Windows and Mac have a built in ssh client)
- Code Editor - [Visual Studio Code](https://code.visualstudio.com)
- [Jumper Cables, Breadboard, 220 Ohm Resistors, Assorted LEDs](https://amzn.to/3uYybMu)
- [Microsoft Azure Account](https://portal.azure.com)
- Optional: [LCD Touchscreen](https://amzn.to/3uYSXvt)
- [VNC Viewer](https://www.realvnc.com/en/connect/download/viewer/)

## What we'll be doing

In this post we'll be setting up our Raspberry Pi to launch a "Hello World" Uno Application.

There will be a series of steps involved in this;

1. [Update Raspberry Pi OS](#update-the-raspberry-pi-os)
2. Install .NET Framework
3. Install Uno Framework Templates
4. Create a new GTK Project
5. Give the SSH Session access to use the screen
6. Build and run the application 

## Update Raspberry Pi OS

Before we can do anything, assuming yu've gotten your Raspberry Pi all set up with Raspberry Pi OS, we need to make sure that everything is up to date.

Run the following two commands;

`sudo apt-get update`
`sudo apt-get upgrade`
