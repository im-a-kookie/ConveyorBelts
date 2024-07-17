# ConveyorBelts

A random project that I keep spamming weird ideas into. It was originally just intended to be a simple example of a disgustingly indulgent conveyor belt algorithm.

The conveyor algorithm contains a few main components. Firstly, when the tiles are placed, they are graphed into a tree structure, where each branch of the tree describes a continuous progression of conveyor tiles, starting from the earliest input, and ending at any point where another branch may provide IO.

Essentially, all IO operations are resolved at the heads and tails of conveyor belts. As these IOs are computationally expensive to resolve, we seek to minimize them by creating the largest branches possible.

The IO's are computationally expensive, because of the Halting Problem (see: Turing Machines). In other words, every IO boundary needs to be checked, because we cannot otherwise know its state and whether its state will cause an obstruction.

Where this algorithm takes a left turn, is in projecting forwards to the soonest point at which a given IO boundary needs to be checked for an IO event. For example, if a belt takes 5 seconds to move items from start to finish, and currently contains no items, then the belt is guaranteed to provide no outputs for 5 seconds into the future. As a result, any downstream branches can ignore this belt completely for 5 seconds.

For optimization reasons (aka extensive performance profiling), I've also written a simple Deque class which allows extremely efficient getting and setting at both the head and tail. The project also uses extensive manual vertex buffer construction, texture caching, etc, to construct fully animated and directional conveyor belts from a single 16x16 component sprite, and then render insane numbers of them very very quickly.

Over time, I hope to build this out into a fully fledged game ala Factorio or Mindustry, but haven't had the time or motivation lately.
