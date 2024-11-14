# ConveyorBelts

A random project that I keep spamming weird ideas into. It was originally just intended to be a simple example of a disgustingly indulgent conveyor belt algorithm.

First, we imagine the conveyor system as a series of IO operations, where an item is Input into the beginning of a conveyor belt, transitions across the length of the belt, and is Output from the tail of the belt.

Algorithmically, the simplest approach to this problem, is to visit each conveyor belt, and to have it advance all items within its bounds. This becomes O(N) for the number of belts, and the number of visited items, and presents a foundational model for the final solution.

Thereby, two optimizations become immediately apparent.

# Belt Graphing

Firstly, to mimize the number of belts visited, belts should be aggressively merged. For example, where two adjacently connected tiles may each act indepdently as belts, constructing them into the branch of a tree will allow IO to be processed on both tiles simultaneously. Such merging can be performed by taking every tile, counting the number of tiles that provide inputs to it, and then taking all tiles with exactly one input and merging them.

This allows the model to be efficiently represented as a tree diagram, where IO is resolved at the junctions of the tree, and transitions are resolved on each branch independently.

# Item Combination

Secondly, to minimize the expense of item transition, items can be grouped. For example, if 5 items are jammed together in a line, these can be described as a singular entity; "a single group containing 5 items." 

Observing that an individual conveyor belt will move all contents at a constant rate, we also notice that the gaps between item groups will remain consistent. As such, every item on the belt can be moved in a single step - the first group is advanced along the belt, while subsequent groups simply record their distance from the group ahead.

# Halting Problem

At this point, one notices that from a functional perspective, the only thing we need to care about, are the IO operations of the conveyor network into productive entities. The oven doesn't care how the cookie dough arrives - it only needs to know WHEN the dough arrives so that it can begin baking.

Unfortunately, this brings us to the Halting Problem. In short, while it is trivial to e.g notice that an item will reach the end of a conveyor belt in 5 seconds time, it is not trivial to determine whether the end of that belt may successfully output that item. Equally problematically, in these 5 seconds, it is not trivial to determine whether an additional item will be added to the conveyor belt.

Or in short: every IO boundary must be checked and resolved for every discrete moment of time at which an interaction may occur across this boundary. We refer to these moments of interaction, as potential or projected halting events. By constructing the problem in this way, the solution becomes somewhat apparent. 

For example, if a belt will not provide an item from its tail for 5 seconds into the future, then the output branch is given a prospective halting event 5 seconds into the future. So this IO junction now predicts a halting event 5 seconds in the future. For any branch, the soonest projected halting event at its head and tail, determines the furthest moment into the future that the state can be predicted reliably.

By representing these halting events as a numerical timestamp (e.g the halting event will occur at the Nth tick of the simulation), then we can simply wait until this tick arrives, and immediately project the belt ahead to this moment. As there is minimal computational difference between advancing by 1 tick and advancing by 2, 10, or 1000 ticks, we can now massively reduce the CPU cost of updating the graph by propagating these projections forwards and resolving the IO boundaries when these moments arrive.

# Other Details

For optimization reasons (aka extensive performance profiling), I've also written a simple Deque class which allows extremely efficient getting and setting at both the head and tail, removed pretty much everything even resembling LINQ, rely heavily on array caching to avoid reference creation, and so on. The project also uses extensive manual vertex buffer construction, texture caching, etc, to construct fully animated and directional conveyor belts from a single 16x16 component sprite, and then render insane numbers of them very very quickly.

Over time, I hope to build this out into a fully fledged game ala Factorio or Mindustry, but haven't had the time or motivation lately.
