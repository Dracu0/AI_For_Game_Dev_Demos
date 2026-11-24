using System;

// Everything the agent can know about the world, one bit each.
// A "world state" is simply a combination of these flags: HasAxe | FireLit means both are true.
[Flags]
public enum Fact
{
    None = 0,
    HasAxe = 1 << 0,
    HasWood = 1 << 1,
    FireLit = 1 << 2,
    HasFish = 1 << 3,
    Fed = 1 << 4,
}
