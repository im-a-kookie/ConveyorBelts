# ConveyorBelts

A random project that I keep spamming weird ideas into. It was originally just intended to be a simple example of a disgustingly indulgent conveyor belt algorithm.

First, we imagine the conveyor system as a series of IO operations, where an item is Input into the beginning of a conveyor belt, transitions across the length of the belt, and is Output from the tail of the belt.

The first optimization is to group the conveyor tiles into a graph, such that a single branch of the graph describes multiple tiles. The second, is to group items by defining their position relative to the first in the group, so that only one item actually needs to be moved.

The third optimization is significantly more complicated, and results in a complicated graph problem. The main challenge is optimizing a per-tick system while maintaining correctness after skipping ticks. This presents an example of the Halting Problem - a branch cannot make predictions about the states of adjacent branches without examining a much larger proportion of the graph. But it's noted that interactions are generally predictable in a forwards direction, and up until this moment, assumptions can be made about the state of subsequent branches in the diagram. Therefore, these moments can be propagated through the graph, and we can use them to predict and thus skip past ticks in which the graph state can be trivially predicted from the present state.

For optimization reasons (aka extensive performance profiling), I've also written a simple Deque class which allows extremely efficient getting and setting at both the head and tail, removed pretty much everything even resembling LINQ, rely heavily on array caching to avoid reference creation, and so on. The project also uses extensive manual vertex buffer construction, texture caching, etc, to construct fully animated and directional conveyor belts from a single 16x16 component sprite, and then render insane numbers of them very very quickly.

One day I hope to build this out into a fully fledged factory-style game, but extensive profiling and optimization rendered the algorithm a little unmaintainable, so a redesign is likely necessary.
