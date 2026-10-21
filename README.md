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

## Simulator run modes

- **Simulate recorded data** – feeds the real recorded inter-arrival gaps and service times, in
  their recorded order, through the single-server FIFO engine. It is identical for M/M/1, M/G/1 and
  G/G/1 and uses no randomness (no seed); the selected model only decides the theoretical column.
  It can differ from the real observation only where a customer was served out of arrival order.
- **Random simulation** – N customers (max 5000) drawn from the selected model, reproducible via the
  Random seed: M/M/1 uses exponential arrivals and service times (rates from the data); M/G/1 uses
  exponential arrivals with service times resampled from the recorded ones; G/G/1 resamples both the
  recorded gaps and the recorded service times. The customer count and seed are shown only here.

## Two utilization numbers

- **rho (theory)** = lambda/mu = mean service time / mean inter-arrival gap, so it only covers the
  arrival span.
- **Utilization (measured)** = total service time / observation horizon (60 min, or until the last
  exit if later), so the two differ.

Reset restores the defaults (Female counter, Simulate recorded data, 200 customers, seed 42) and
stays on the currently selected model tab.

## Run

```
dotnet run
```

Requires the .NET 8 SDK.

## Check (optional)

```
python3 tools/verify_consistency.py            # needs numpy and scipy
python3 tools/verify_port.py [data.xlsx]       # the .xlsx is optional
```
