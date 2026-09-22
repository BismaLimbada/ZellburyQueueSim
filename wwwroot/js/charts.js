// Thin Chart.js interop layer called from Blazor via IJSRuntime.
// Chart.js itself is loaded locally from wwwroot/js/chart.umd.js (see App.razor).
window.zellburyCharts = (function () {
    const instances = {};
    const INK = '#2a2622';
    const GRID = '#e7e1d8';

    function destroy(canvasId) {
        if (instances[canvasId]) {
            instances[canvasId].destroy();
            delete instances[canvasId];
        }
    }

    function baseOptions(yTitle, xTitle) {
        return {
            responsive: true,
            animation: { duration: 200 },
            plugins: { legend: { display: true, position: 'top', labels: { color: INK, font: { size: 11 } } } },
            scales: {
                x: { title: { display: !!xTitle, text: xTitle || '', color: INK }, ticks: { color: INK, font: { size: 10 } }, grid: { color: GRID } },
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
                    borderColor: s.color || '#c1502e',
                    backgroundColor: (s.color || '#c1502e') + '1f',
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
                        borderColor: '#3f6b5e',
                        backgroundColor: '#3f6b5e',
                        borderWidth: 1,
                        pointRadius: 2.5,
                        pointBackgroundColor: '#3f6b5e',
                        showLine: false
                    },
                    {
                        label: referenceLabel,
                        data: refLine,
                        borderColor: '#c1502e',
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
                    { label: labelA, data: dataA, backgroundColor: '#2a2622' },
                    { label: labelB, data: dataB, backgroundColor: '#c1502e' }
                ]
            },
            options: baseOptions(yTitle, '')
        });
    }

    // Histogram (pre-binned counts) with an optional fitted density curve overlaid as a line,
    // scaled to expected frequency per bin so it's visually comparable to the bars.
    function renderHistogramWithFit(canvasId, binLabels, counts, fittedCounts, xTitle, fitLabel) {
        destroy(canvasId);
        const ctx = document.getElementById(canvasId);
        if (!ctx) return;
        const datasets = [{ type: 'bar', label: 'Observed frequency', data: counts, backgroundColor: '#d8cfc0', order: 2 }];
        if (fittedCounts && fittedCounts.length) {
            datasets.push({
                type: 'line', label: fitLabel || 'Fitted density', data: fittedCounts,
                borderColor: '#c1502e', backgroundColor: '#c1502e', borderWidth: 2,
                pointRadius: 0, tension: 0.35, fill: false, order: 1
            });
        }
        instances[canvasId] = new Chart(ctx, {
            type: 'bar',
            data: { labels: binLabels, datasets: datasets },
            options: baseOptions('Frequency', xTitle)
        });
    }

    // Gantt/timeline strip: one horizontal row per customer, a muted "waiting" segment
    // followed by an accent "service" segment, using Chart.js floating bars.
    function renderGanttChart(canvasId, customerLabels, waitSegments, serviceSegments) {
        destroy(canvasId);
        const ctx = document.getElementById(canvasId);
        if (!ctx) return;
        instances[canvasId] = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: customerLabels,
                datasets: [
                    { label: 'Waiting', data: waitSegments, backgroundColor: '#d8cfc0', stack: 'timeline', barPercentage: 0.6 },
                    { label: 'Service', data: serviceSegments, backgroundColor: '#c1502e', stack: 'timeline', barPercentage: 0.6 }
                ]
            },
            options: {
                indexAxis: 'y',
                responsive: true,
                animation: { duration: 200 },
                plugins: { legend: { display: true, position: 'top', labels: { color: INK, font: { size: 11 } } } },
                scales: {
                    x: { stacked: true, title: { display: true, text: 'Simulation time (min)', color: INK }, ticks: { color: INK, font: { size: 10 } }, grid: { color: GRID } },
                    y: { stacked: true, title: { display: true, text: 'Customer', color: INK }, ticks: { color: INK, font: { size: 9 } }, grid: { display: false } }
                }
            }
        });
    }

    return { renderLineChart, renderScatterWithReference, renderBarComparison, renderHistogramWithFit, renderGanttChart, destroy };
})();
