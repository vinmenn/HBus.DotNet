# HBus DotNet implementation

HBus is composed of two main libraries:

## HBus.dll
Communication library, used to send and receive commands to remote nodes, protocol is the same used on Arduino libraries.

## HBus.Node.dll
This library implements nodes functions and executes commands received with HBus library.

In this repository there are two more projects:
## HBus.Node.Console
Implements a simple console node (tested with Raspberry Pi 2). This node can read read real ds18b20 sensor or simulated sensor.
Node uses emulated hardware Hal to test I/O and devices without having real hardware. 

## HBus.Server
Manages events in a producer/consumer architecture, called processors.
A processor could be producer and/or consumer. 
Available processors are:
* **HBusProcessor** : receives and sends HBus commands and translate them into processor events.
* **ScheduledProcessor** : act as producer only and send scheduled events to consumer.
* **WebsocketProcessor** : receive and sends events from websocket (useful for web apps)
* **ThingSpeakProcessor** : receive events and publish them into ThingSpeak channel
* **ArtikProcessor** : receive events and publish them into Artik cloud


