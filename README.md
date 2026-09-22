# Zellbury Checkout Queue Simulation

A discrete-event simulation of the checkout/counter queue at the Zellbury outlet,
Lucky One Mall, Gulberg Town, Karachi — built for **CS-577 Modeling & Simulation**.

The system modeled is intentionally narrow, matching the approved project scope:

```
Customers → Waiting Queue → ONE Cashier → Exit
```

A **single-server queueing model** is run separately against two real, independently
recorded datasets — the Men's counter and the Women's counter. The two datasets are
never combined into one queue.

## Features

- **Queueing Models** — a plain-language explanation of M/M/1, M/G/1 and G/G/1 (the
  three single-server models the project supports), each with its formulas and a
  table of theoretical steady-state values computed from the recorded data for both
  counters. This page only explains the models; it does not run a simulation.
- **Simulator** — the working page. Pick a counter (Men's / Women's), pick a
  queueing model, and either:
  - **replay the exact recorded field observation** (trace-driven, no randomness), or
  - **run a random (Monte Carlo) simulation** for N customers, seeded for
    reproducibility, using arrivals/service drawn from the selected model.

  Results include a single-server queue visualization, a results ticker (waiting
  time, service time, queue length, utilization, time in system), a
  theoretical-vs-simulated comparison table, charts, and a full per-customer table
  (arrival, service start/end, waiting time, service time, time in system).
- Statistical validation (Chi-square, histograms) is **out of scope for this app on
  purpose** — the group is handling that separately as part of the raw data
  submission.

## Tech stack

- .NET 8, ASP.NET Core Blazor **Server** (interactive server render mode)
- Chart.js (loaded locally from `wwwroot/js/chart.umd.js`, no CDN/network dependency)
- No database — the two datasets are recorded field data embedded directly in
  `Services/ZellburyDataService.cs`

## Project structure

```
ZellburyQueueSim/
├── Components/
│   ├── Layout/            # Sidebar + shell
│   └── Pages/
│       ├── Home.razor
│       ├── Simulator.razor            # the main working page
│       └── QueueingModels/            # explanation-only pages (M/M/1, M/G/1, G/G/1)
├── Engine/                 # Future Event List + discrete-event simulation engine
├── Models/                 # Customer, SimulationResult, TheoreticalResult, ...
├── Queueing/                # MM1Model, MG1Model, GG1Model — closed-form / approximate formulas
├── Services/                # RandomGenerator, ZellburyDataService, StatisticsService
└── wwwroot/                 # CSS + Chart.js interop
```

## Running locally

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```bash
cd ZellburyQueueSim
dotnet restore
dotnet run
```

Then open the URL printed in the console (typically `https://localhost:5001` or
similar — check `Properties/launchSettings.json`).

If you're opening the project in VS Code, use the built-in **Run and Debug** (F5) or
a real browser tab pointed at that URL — VS Code's Simple Browser / webview preview
does not always render an interactive Blazor Server circuit identically to a normal
browser tab, so prefer a real browser for actual use and testing.

## Scope notes

- Only a single-server queueing model is implemented (Men's and Women's counters
  are each simulated independently — never as two servers routing to one queue).
- No multi-server model, no Chi-square testing, and no histograms are implemented
  in this software, per the approved project scope.
