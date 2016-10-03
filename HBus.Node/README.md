# HBus node library

## Introduction
HBus Node library is part of Hbus Home automation / IoT project. 
Main use of the library is to create autonomous nodes that respond to hardware inputs changing output pins
or executeng devices actions. Nodes can read from sensors and communicates each other through HBus protocol.

## Configuration
Each node can be configured with a xml configuration file. Main parts are:
* **info** Global node information, contains name and HBus address of the node

* **hal** Each node can act physical actions through a simple Hardware Abstraction Layer, allowing to adapt 
to different devices

* **bus** HBus configuration, defines which types of HBus ports node uses to communicate with other nodes. 
Low level nodes as Arduino nodes communicates with serial port (RS232 or RS485). 
High level nodes can communicates with high level message queues as ZeroMq or protocols as TCP/UDP.

* **pins** Defines input/output pins used by the node. Different of pin type are possible:
1. Input: digital pins can be configured with different active modes.
2. Output: digital output pins, different modes available.
3. Analog: analog input pins, read raw values from A/D pins
4. Pwm: analog output pins, write raw values pwm or D/A pins (depends on HAL implementation)
5. Counter: digital pins that increment values at each activation.

* **wires** Wires connects input pins events to output pins or devices. A wire can connect also pins of external 
nodes, sending a HBus command when input is activated.

* **devices** This section configures high level devices with specific configuration parameters of costructor

* **sensors** This section configures sensors connected directly to the node. Parameters depends on implementation.
##Installation
A node can be defined configuring its pins, devices and sensor with node.config.xml file as described before,
or directly by code adding pins, devices and sensors to the node.
Example of code using configurator:
```C#
      //Create configurator
      var configurator = new XmlConfigurator("node.config.xml");

      //Configure all global objects
      var configurator.Configure(false); //true uses default configuration that for low level nodes is readonly

      //Get the node
      var node = configurator.Node;

	  //Get bus controller
      var bus = configurator.Bus;
```
Events handlers could be added to the node to implement specific behaviours

```C#
      //Add handlers to bus
      bus.OnMessageReceived += (source, message) =>
      {
        Console.WriteLine("Received message from {0} to {1} = {2} ({3})", message.Source, message.Destination, message.Command, message.Flags);
      };

      //Add handlers to sensor events
      node.OnSensorRead += ( Address arg1, SensorRead arg2) =>
      {
        Console.WriteLine("Sensor read from {0} : {1} = {2} @ {3}", arg1, arg2.Name, arg2.Value, arg2.Time);
      };
```

After configuration node can be started
```C#
      //Start node
      node.Start();
```
Node is active after start and continously reads inputs, changes outputs and executes device actions.

