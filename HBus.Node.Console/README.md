# HBus Node console

## HBus node console example

Implements a simple console node as example of HBus features:

### Installation
1. Build node from sources
	Download sources and compile it
2. Configure node.config.xml
	This is configuration file for HBus node, main properties are:
* **<node><name>** Unique name of node.
* **<node><address>** HBus address, range depends on address width (default one byte).
* **<ports>** List of HBus ports. Each message will be sent on all ports.
* **<pins>** List of input/output pins
* **<wires>** List of wires that connects inputs to outputs
* **<devices>** List of node devices
* **<sensors>** List of node sensors
3. Console commands:
* **0**: Send ping to host node
* **1**: activate input pin 0
* **2**: activate input pin 1
* **3**: activate input pin 2
* **4**: activate input pin 3
* **5**: activate input pin 4
* **6**: activate input pin 5
* **7**: activate input pin 6
* **8**: activate input pin 7
* **q**: activate output pin 0
* **w**: activate output pin 1
* **e**: activate output pin 2
* **r**: activate output pin 3
* **a**: send open command to shutter device 0
* **b**: send close command to shutter device 0
* **c**: send open command to shutter device 1
* **d**: send close command to shutter device 1
* **Q** : read remote sensor 0
* **W** : read remote sensor 1
* **E** : read local sensor 0
* **R** : read local sensor 1
* **T** : read local sensor 2


