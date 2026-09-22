namespace ZellburyQueueSim.Models;

/// <summary>
/// The Zellbury outlet report observed two separate checkout queues -
/// men's and women's counters - each timed independently. Per the project
/// scope, the single-server implementation models exactly one of these at
/// a time (they are never combined into one queue).
/// </summary>
public enum CounterType
{
    FemaleCounter,
    MaleCounter
}
