# HBus Node console

## HBus node console example

Implements a simple console node as example of HBus features:

### Installation
1. Build node from sources
* download sources 
* compile with visual studio 2015
use ```ARTIK_DEMO_LOCAL``` or ```ARTIK_DEMO_LOCAL``` pragmas to enable
artik demo commands for software only or real hardware demos.

2. Configure node.config.xml
Pragmas ```ARTIK_DEMO_LOCAL``` and ```ARTIK_DEMO_LOCAL``` will copy specific node.config.xml files into
node.config.xml:
* ```node.config.windows.xml``` for software only test (console hal)
* ```node.config.raspberry.xml``` for Gpio / Rly816 hardware hals

node.config.xml is configuration file for HBus node, main properties are:

* ```<node><name>``` Unique name of node.
* ```<node><address>``` HBus address, range depends on address width (default one byte).
* ```<ports>``` List of HBus ports. Each message will be sent on all ports.
* ```<pins>``` List of input/output pins
* ```<wires>``` List of wires that connects inputs to outputs
* ```<devices>``` List of node devices
* ```<sensors>``` List of node sensors

3. Run Node.Console
Node start as executable from command line, after launch these parameters are available

If compiled without pragmas
* ```0```: Send ping to host node
* ```h```: Show help about commands
* ```x```: Exit

If compiled with ```ARTIK_DEMO_LOCAL``` or ```ARTIK_DEMO_RASPBERRY```
these commands will be available:
* ```1```: activate input pin 0
* ```2```: activate input pin 1
* ```3```: activate input pin 2
* ```4```: activate input pin 3
* ```5```: activate input pin 4
* ```6```: activate input pin 5
* ```7```: activate input pin 6
* ```8```: activate input pin 7
* ```q```: activate output pin 0
* ```w```: activate output pin 1
* ```e```: activate output pin 2
* ```r```: activate output pin 3
* ```t```: send open command to shutter device 0
* ```y```: send close command to shutter device 0
* ```u```: send open command to shutter device 1
* ```i```: send close command to shutter device 1
* ```a``` : read local sensor 0
* ```s``` : read local sensor 1
* ```d``` : read remote sensor 0
* ```f``` : read remote sensor 1
* ```g``` : read remote sensor 2

Debug messages are shown through console out and saved to log-file.txt.
App.config contains log configuration (log4net)
