# Zellbury Checkout Queue Simulation

A Blazor Server app that applies single-server queueing models (M/M/1, M/G/1, G/G/1) to the
checkout queues at the Zellbury outlet, Lucky One Mall, Gulberg Town, Karachi (CS-577 Modeling &
Simulation, Group 11). A discrete-event simulator checks the formulas against the recorded data.

The Men's and Women's counters were recorded separately and are always treated as two independent
single-server queues.

## Pages

- **Home** – overview and the measured utilization and average wait for each counter.
- **Queueing Models** – theoretical calculator for M/M/1, M/G/1 and G/G/1 from your own inputs.
- **Simulator** – uses the recorded data, in two run modes (see below), and compares the result
  with the real observations and the selected model's theory.

## Run

```
dotnet run
```

Requires the .NET 8 SDK.

