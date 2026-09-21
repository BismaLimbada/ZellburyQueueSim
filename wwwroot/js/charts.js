// Thin Chart.js interop layer called from Blazor via IJSRuntime.
// Chart.js itself is loaded from a CDN script tag in Components/App.razor.
window.zellburyCharts = (function () {
    const instances = {};

    function destroy(canvasId) {
        if (instances[canvasId]) {
            instances[canvasId].destroy();
            delete instances[canvasId];
        }
    }

    function baseOptions(yTitle, xTitle) {
        return {
            responsive: true,
            animation: { duration: 250 },
            plugins: { legend: { display: true, position: 'top' } },
            scales: {
                x: { title: { display: !!xTitle, text: xTitle || '' } },
                y: { title: { display: !!yTitle, text: yTitle || '' }, beginAtZero: true }
            }
        };
    }

    // Generic line chart. series = [{ label, data: number[], color }]
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
                    borderColor: s.color || '#2f6fed',
                    backgroundColor: (s.color || '#2f6fed') + '22',
                    borderWidth: 2,
                    pointRadius: 0,
                    tension: stepped ? 0 : 0.15,
                    stepped: !!stepped,
                    fill: false
                }))
            },
            options: baseOptions(yTitle, xTitle)
        });
    }

    // Grouped bar chart comparing two series (e.g. theoretical vs simulated).
    function renderBarComparison(canvasId, labels, dataA, dataB, labelA, labelB, yTitle) {
        destroy(canvasId);
        const ctx = document.getElementById(canvasId);
        if (!ctx) return;
        instances[canvasId] = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [
                    { label: labelA, data: dataA, backgroundColor: '#2f6fed' },
                    { label: labelB, data: dataB, backgroundColor: '#1f9d55' }
                ]
            },
            options: baseOptions(yTitle, '')
        });
    }

    // Histogram from pre-binned counts.
    function renderHistogram(canvasId, binLabels, counts, xTitle) {
        destroy(canvasId);
        const ctx = document.getElementById(canvasId);
        if (!ctx) return;
        instances[canvasId] = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: binLabels,
                datasets: [{ label: 'Frequency', data: counts, backgroundColor: '#c9821a' }]
            },
            options: baseOptions('Frequency', xTitle)
        });
    }

    return { renderLineChart, renderBarComparison, renderHistogram, destroy };
})();
