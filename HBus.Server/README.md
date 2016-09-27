# HBus Server

HBus Server is an example of integration of HBus nodes and eterogenous sources/consumers.
Each source/consumer is called *processor*
A processor can act as a source and generate events according to its behaviours.
The same processor can also act as consumer (if has that part implemented).

Processors are connected in a publish/subscribe pattern where each source publish events for its subscribed consumers, 
that in turn publish processed or raw events.

Configured in the code there are 2 internal processors as sources and 2 external processors as consumer:

An event contains various useful informations to be more flexible:
* Name: is the public name of event
* MessageType: classify event in a more specific way. Default is simply "event".
* Source: name of source device/sensor that generates the event
* Address: this is used from HBus nodes to directly address nodes, but could be any type of specific 
address (e.g. TCP or URL) depends on wich is the consume. Default is empty.

* SchedulerProcessor: generate events at specific date/time intervals.

More documentation to be added....