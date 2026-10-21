// Thin Chart.js interop layer called from Blazor via IJSRuntime.
// Chart.js itself is loaded locally from wwwroot/js/chart.umd.js (see App.razor).
window.zellburyCharts = (function () {
    const instances = {};
    const INK = '#161a23';
    const GRID = '#dbe0e8';
    const ACCENT = '#2f5fd6';

    function destroy(canvasId) {
        if (instances[canvasId]) {
            instances[canvasId].destroy();
            delete instances[canvasId];
        }
    }

    // NOTE: maintainAspectRatio must be false whenever we set an explicit canvas
    // height (e.g. the Gantt chart's height scales with the number of customers).
    // Chart.js otherwise recalculates the canvas height from its own default
    // aspect ratio and silently overrides the height we asked for.
    function baseOptions(yTitle, xTitle) {
        return {
            responsive: true,
            maintainAspectRatio: false,
            animation: { duration: 200 },
            plugins: { legend: { display: true, position: 'top', labels: { color: INK, font: { size: 11 } } } },
            scales: {
                x: { title: { display: !!xTitle, text: xTitle || '', color: INK }, ticks: { color: INK, font: { size: 10 }, maxTicksLimit: 12, maxRotation: 0 }, grid: { color: GRID } },
                y: { title: { display: !!yTitle, text: yTitle || '', color: INK }, beginAtZero: true, ticks: { color: INK, font: { size: 10 } }, grid: { color: GRID } }
            }
        };
    }

    // Generic line chart. series = [{ label, data: number[], color, dashed? }]
    function renderLineChart(canvasId, labels, series, yTitle, xTitle, stepped) {
        destroy(canvasId);
        const ctx = document.getElementById(canvasId);
        if (!ctx) return;
        instances[canvasId] = new Chart(ctx, {
            type: 'line',
            data: {
                labels: labels,
                datasets: series.map(s => ({
                    label: s.label,
                    data: s.data,
                    borderColor: s.color || '#2f5fd6',
                    backgroundColor: (s.color || '#2f5fd6') + '1f',
                    borderWidth: s.dashed ? 1.5 : 2,
                    borderDash: s.dashed ? [6, 4] : [],
                    pointRadius: 0,
                    tension: stepped ? 0 : 0.15,
                    stepped: !!stepped,
                    fill: !s.dashed
                }))
            },
            options: baseOptions(yTitle, xTitle)
        });
    }

    // Scatter of per-customer values plus a dashed horizontal reference line (e.g. theoretical Wq).
    function renderScatterWithReference(canvasId, xLabels, yValues, referenceValue, pointLabel, referenceLabel, yTitle, xTitle) {
        destroy(canvasId);
        const ctx = document.getElementById(canvasId);
        if (!ctx) return;
        const refLine = xLabels.map(() => referenceValue);
        instances[canvasId] = new Chart(ctx, {
            type: 'line',
            data: {
                labels: xLabels,
                datasets: [
                    {
                        label: pointLabel,
                        data: yValues,
                        borderColor: '#1c3fa8',
                        backgroundColor: '#1c3fa8',
                        borderWidth: 1,
                        pointRadius: 2.5,
                        pointBackgroundColor: '#1c3fa8',
                        showLine: false
                    },
                    {
                        label: referenceLabel,
                        data: refLine,
                        borderColor: '#2f5fd6',
                        borderWidth: 2,
                        borderDash: [7, 5],
                        pointRadius: 0,
                        fill: false
                    }
                ]
            },
            options: baseOptions(yTitle, xTitle)
        });
    }

    // Grouped bar chart comparing two series (e.g. theoretical vs simulated, or Male vs Female).
    function renderBarComparison(canvasId, labels, dataA, dataB, labelA, labelB, yTitle) {
        destroy(canvasId);
        const ctx = document.getElementById(canvasId);
        if (!ctx) return;
        instances[canvasId] = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [
                    { label: labelA, data: dataA, backgroundColor: '#161a23' },
                    { label: labelB, data: dataB, backgroundColor: '#2f5fd6' }
                ]
            },
            options: baseOptions(yTitle, '')
        });
    }

    // Gantt/timeline strip: one horizontal row per customer, a muted "waiting" segment
    // followed by an accent "service" segment, using Chart.js floating bars.
    // IMPORTANT: do NOT stack these. Stacking floating [start,end] bars ADDS the values, which
    // shifted every service bar right by the customer's wait-end time. grouped:false puts both
    // datasets on the same row instead, each drawn at its true [start,end] position.
    function renderGanttChart(canvasId, customerLabels, waitSegments, serviceSegments) {
        destroy(canvasId);
        const ctx = document.getElementById(canvasId);
        if (!ctx) return;
        instances[canvasId] = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: customerLabels,
                datasets: [
                    { label: 'Waiting', data: waitSegments, backgroundColor: '#dbe0e8', barPercentage: 0.6, grouped: false },
                    { label: 'Service', data: serviceSegments, backgroundColor: '#2f5fd6', barPercentage: 0.6, grouped: false }
                ]
            },
            options: {
                indexAxis: 'y',
                responsive: true,
                // Required because the canvas's HTML height is set dynamically
                // (one row per customer, see simulatorGantt in Simulator.razor).
                // Without this, Chart.js ignores that height and forces its own
                // default aspect ratio, squashing every row once there are more
                // than a handful of customers.
                maintainAspectRatio: false,
                animation: { duration: 200 },
                plugins: { legend: { display: true, position: 'top', labels: { color: INK, font: { size: 11 } } } },
                scales: {
                    x: { stacked: false, title: { display: true, text: 'Simulation time (min)', color: INK }, ticks: { color: INK, font: { size: 10 } }, grid: { color: GRID } },
                    y: { stacked: false, reverse: false, title: { display: true, text: 'Customer', color: INK }, ticks: { color: INK, font: { size: 9 }, autoSkip: false }, grid: { display: false } }
                }
            }
        });
    }

    return { renderLineChart, renderScatterWithReference, renderBarComparison, renderGanttChart, destroy };
})();
