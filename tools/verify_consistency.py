"""Independent Python check of the formulas, statistical tests and the two Simulator run modes
("Simulate recorded data" and "Random simulation") in this project.

Needs only the standard library plus numpy/scipy (no workbook). The recorded data is parsed OUT OF
Services/ZellburyDataService.cs, so it tests the numbers the C# actually contains.
Usage (from the project root):  python3 tools/verify_consistency.py [--quick]

IMPORTANT: this is a *port* of the C# logic, not the C# itself. It cannot prove the C# compiles; run
`dotnet build` and `dotnet run` as well.  (tools/verify_port.py is the older port; it still uses the
previous K-S critical value and has no chi-square, and needs the .xlsx.)
"""
import re, sys, math, heapq, random, statistics as st
from scipy import stats

QUICK = '--quick' in sys.argv
src = open('Services/ZellburyDataService.cs', encoding='utf-8').read()

def arr(name):
    blk = re.search(name + r'\s*=\s*\n\s*\{(.*?)\n    \};', src, re.S).group(1)
    rows = re.findall(r'\(new.*?\),?\n|\(new.*?\)\)', blk)
    return [tuple((int(h) - 16) * 60 + int(m) - 20 for h, m in re.findall(r'new\(\s*(\d+),\s*(\d+)\)', r)) for r in rows]

RAW = {'Female': arr('FemaleRaw'), 'Male': arr('MaleRaw')}
ok = True
def check(cond, msg):
    global ok
    print(('  PASS  ' if cond else '  FAIL  ') + msg)
    ok = ok and cond

def customers(k):
    r = RAW[k]; out = []
    for i, (a, s, e) in enumerate(r):
        out.append(dict(id=i + 1, ia=a if i == 0 else a - r[i - 1][0], a=a, s=s, e=e, w=s - a, sv=e - s, t=e - a))
    return out
def horizon(k): return max(60, max(x[2] for x in RAW[k]))

# ---------------- 1. formulas ----------------
def mm1(l, m):
    rho = l / m; lq = rho**2 / (1 - rho); wq = lq / l; w = wq + 1 / m
    return dict(rho=rho, P0=1 - rho, Lq=lq, Wq=wq, W=w, L=l * w)
def mg1(l, es, v):
    rho = l * es; lq = (l * l * v + rho**2) / (2 * (1 - rho)); wq = lq / l; w = wq + es
    return dict(rho=rho, P0=1 - rho, Lq=lq, Wq=wq, W=w, L=l * w)
def gg1(l, m, ca, cs):
    rho = l / m; lq = rho**2 * (1 + cs) * (ca + rho**2 * cs) / (2 * (1 - rho) * (1 + rho**2 * cs)); wq = lq / l; w = wq + 1 / m
    return dict(rho=rho, P0=1 - rho, Lq=lq, Wq=wq, W=w, L=l * w)

print('1. Formulas')
a = mm1(.5, .6); b = mg1(.5, 1 / .6, 1 / .6**2); c = gg1(.5, .6, 1, 1)
check(all(abs(a[x] - b[x]) < 1e-12 and abs(a[x] - c[x]) < 1e-12 for x in a), 'M/G/1 with Var(S)=1/mu^2 and G/G/1 with Ca2=Cs2=1 both reproduce M/M/1')
check(all(abs(d['P0'] - (1 - d['rho'])) < 1e-15 for d in (a, b, c)), 'P0 = 1 - rho for M/M/1, M/G/1 and G/G/1')
for d in (a, b, c):
    check(abs(d['L'] - (d['Lq'] + d['rho'])) < 1e-12, f"L = Lq + rho (Little's law consistency), rho={d['rho']:.3f}")
check(abs(mg1(.3, 2, 0)['Lq'] - 0.6**2 / (2 * 0.4)) < 1e-12, 'M/G/1 deterministic service (Var=0): Lq = rho^2/(2(1-rho))')

# ---------------- 2. statistical tests ----------------
print('2. Statistical tests')
CRIT = {1: 3.841, 2: 5.991, 3: 7.815, 4: 9.488, 5: 11.070, 6: 12.592, 7: 14.067, 8: 15.507, 9: 16.919, 10: 18.307}
def chi_crit(df):
    if df in CRIT: return CRIT[df]
    z = 1.645; t = 1 - 2 / (9 * df) + z * math.sqrt(2 / (9 * df)); return df * t**3
def ks(sample, rate):
    s = sorted(sample); n = len(s); md = 0
    for i, v in enumerate(s):
        F = 1 - math.exp(-rate * v); md = max(md, abs(F - i / n), abs(F - (i + 1) / n))
    crit = 1.094 / (math.sqrt(n) + 0.26 + 0.5 / math.sqrt(n)) + 0.2 / n
    return md, crit
def chi(sample, rate, maxbins=10):
    n = len(sample); k = min(maxbins, n // 5)
    if rate <= 0 or k < 3: return None
    obs = [0] * k
    for x in sample: obs[max(0, min(int(((1 - math.exp(-rate * x))) * k), k - 1))] += 1
    e = n / k; stat = sum((o - e)**2 / e for o in obs); df = k - 2
    return stat, df, chi_crit(df), k, obs, e
def lag1(q):
    n = len(q); m = st.mean(q)
    return sum((q[i] - m) * (q[i + 1] - m) for i in range(n - 1)) / sum((v - m)**2 for v in q), 1.96 / math.sqrt(n)

check(all(abs(CRIT[d] - stats.chi2.ppf(.95, d)) < 0.002 for d in CRIT), 'chi-square critical-value table (df 1-10) matches scipy.chi2.ppf(0.95)')
rng = random.Random(3); x = [rng.expovariate(.4) for _ in range(40)]; r = 1 / st.mean(x)
check(abs(ks(x, r)[0] - stats.kstest(x, 'expon', args=(0, 1 / r)).statistic) < 1e-12, 'K-S statistic equals scipy.stats.kstest')
res = chi(x, r); check(abs(res[0] - stats.chisquare(res[4], [res[5]] * res[3]).statistic) < 1e-9, 'chi-square statistic equals scipy.stats.chisquare on the same equal-probability bins')
y = [rng.gauss(0, 1) for _ in range(30)]; m = st.mean(y)
ref = sum((y[i] - m) * (y[i + 1] - m) for i in range(29)) / sum((v - m)**2 for v in y)
check(abs(lag1(y)[0] - ref) < 1e-12, 'lag-1 autocorrelation equals an independent computation')

R = 1500 if QUICK else 6000
mc = random.Random(11)
for n in (19, 20, 21, 22):
    rk = rc = rl = 0
    for _ in range(R):
        s = [mc.expovariate(1.0) for _ in range(n)]; rate = 1 / st.mean(s)
        D, cr = ks(s, rate); rk += D > cr
        cc = chi(s, rate); rc += bool(cc and cc[0] > cc[2])
        l, lc = lag1(s); rl += abs(l) > lc
    print(f'        n={n}: size under a TRUE exponential (nominal 5%): K-S {rk/R:.3f}  chi-square {rc/R:.3f}  lag-1 {rl/R:.3f}')
    check(0.03 < rk / R < 0.07, f'K-S (corrected critical value) size is near 5% at n={n}')
    check(rc / R < 0.10, f'chi-square size is not badly inflated at n={n}')

print('   Test results on the recorded data (rate = 1/sample mean):')
for k in RAW:
    cs = customers(k); ia = [c['ia'] for c in cs[1:]]; sv = [c['sv'] for c in cs]
    for nm, s in (('arrivals', ia), ('service ', sv)):
        D, cr = ks(s, 1 / st.mean(s)); cc = chi(s, 1 / st.mean(s)); l, lc = lag1(s)
        ctxt = f'chi2={cc[0]:.2f} vs {cc[2]:.2f} (df {cc[1]}, {cc[3]} bins) {"REJECT" if cc[0] > cc[2] else "not rejected"}' if cc else 'chi2 not applicable'
        print(f'        {k:6s} {nm} n={len(s)}  KS D={D:.3f} vs {cr:.3f} {"REJECT" if D > cr else "not rejected"} | {ctxt} | lag1 r={l:.2f} vs +-{lc:.2f} {"DEPENDENT" if abs(l) > lc else "ok"}')

# ---------------- 3. engine port + "Simulate recorded data" ----------------
def engine(next_ia, next_sv, N, min_horizon=0.0):
    fel = []; seq = 0
    def sched(t, typ, cid):
        nonlocal seq; heapq.heappush(fel, (t, seq, typ, cid)); seq += 1
    cust = {}; q = []; busy = False; clock = 0.0; last = 0.0; aq = ab = 0.0
    g = next_ia(); cust[1] = dict(a=g, ia=g); sched(g, 'A', 1); la = g; nid = 2; sch = 1
    while fel:
        t, _, typ, cid = heapq.heappop(fel); clock = t; d = clock - last; aq += len(q) * d; ab += (1 if busy else 0) * d; last = clock
        if typ == 'A':
            c = cust[cid]
            if not busy: busy = True; c['s'] = clock; c['sv'] = next_sv(); c['e'] = c['s'] + c['sv']; sched(c['e'], 'D', cid)
            else: q.append(cid)
            if sch < N:
                g = next_ia(); la += g; cust[nid] = dict(a=la, ia=g); sched(la, 'A', nid); nid += 1; sch += 1
        else:
            cust[cid]['done'] = True
            if q:
                n = q.pop(0); c = cust[n]; c['s'] = clock; c['sv'] = next_sv(); c['e'] = clock + c['sv']; sched(c['e'], 'D', n)
            else: busy = False
    cs = [c for c in cust.values() if c.get('done')]; T = max(clock, min_horizon, 1e-4)
    return dict(c=sorted(cs, key=lambda c: c['a']), Wq=st.mean(c['s'] - c['a'] for c in cs), W=st.mean(c['e'] - c['a'] for c in cs), Lq=aq / T, util=ab / T, T=T, clock=clock)

def fifo_violations(k):
    cs = customers(k); n = len(cs); bs = sorted(cs, key=lambda c: (c['s'], c['e'], c['id'])); out = []
    for q in range(1, n):
        p, c = bs[q - 1], bs[q]
        if c['s'] > p['e'] and [d for d in bs[q:] if d['a'] <= p['e']]: out.append(c['id'])
    return out

print('3. Simulate recorded data (real gaps + service times in order, all models; deterministic, so it can be compared with the observation exactly)')
for k in RAW:
    cs = customers(k); H = horizon(k); n = len(cs)
    gi = iter(c['ia'] for c in cs); si = iter(c['sv'] for c in cs)
    calls = {'ia': 0, 'sv': 0}
    def nxt_ia(): calls['ia'] += 1; return next(gi)
    def nxt_sv(): calls['sv'] += 1; return next(si)
    rep = engine(nxt_ia, nxt_sv, n, H)
    obs_w = [c['w'] for c in cs]; rep_w = [c['s'] - c['a'] for c in rep['c']]
    diff_ids = [i + 1 for i in range(n) if abs(obs_w[i] - rep_w[i]) > 1e-9]
    viol = fifo_violations(k)
    print(f'   {k}: observed Wq={st.mean(obs_w):.3f}  simulated Wq={rep["Wq"]:.3f};  customers whose wait differs: {diff_ids};  out-of-order service flagged in validation (row of the later-served customer): {viol}')
    check(calls == {'ia': n, 'sv': n}, f'{k}: each recorded gap and service time is consumed exactly once, in order (no random numbers)')
    check([c['a'] for c in rep['c']] == [c['a'] for c in cs], f'{k}: simulated arrival times equal the recorded arrival times')
    check(rep['T'] == H, f'{k}: simulated run is averaged over the observation horizon ({H} min), same as the real data')
    if not viol:
        check(not diff_ids, f'{k}: no out-of-order service -> simulation reproduces every recorded wait')
    else:
        first = min(diff_ids) if diff_ids else None
        check(bool(diff_ids), f'{k}: out-of-order service present -> simulation differs from the observation (first difference at customer {first})')
    ia_only = [c['ia'] for c in cs[1:]]; rho_th = st.mean(c['sv'] for c in cs) / st.mean(ia_only)
    print(f'   {k}: rho (theory) = mean service / mean gap = {rho_th:.3f};  Utilization (measured) = total service / horizon {H} = {sum(c["sv"] for c in cs) / H:.3f}  (they differ: arrival span vs whole horizon)')
    busy_sum = sum(c['sv'] for c in rep['c'])
    check(abs(rep['util'] - busy_sum / H) < 1e-9, f'{k}: simulated busy fraction = total service time / horizon')
    # Little's law on the simulated recorded run, exactly: area under number-in-system = sum of times in system
    L = rep['Lq'] + rep['util']; sumT = sum(c['e'] - c['a'] for c in rep['c'])
    check(abs(L - sumT / H) < 1e-9, f'{k}: L = Lq + busy fraction = (sum of times in system)/horizon (area identity)')

print('3b. Engine vs an independent exact method (Lindley recursion W[i+1] = max(0, W[i] + S[i] - gap[i+1]) on identical sequences)')
lr = random.Random(5)
for k in RAW:
    cs = customers(k); ia = [c['ia'] for c in cs[1:]]; sv = [c['sv'] for c in cs]
    n = 3000; gaps = [lr.choice(ia) for _ in range(n)]; svc = [lr.choice(sv) for _ in range(n)]
    gi = iter(gaps); si = iter(svc)
    eng = engine(lambda: next(gi), lambda: next(si), n)
    w = [0.0]
    for i in range(n - 1): w.append(max(0.0, w[-1] + svc[i] - gaps[i + 1]))
    worst = max(abs((c['s'] - c['a']) - w[i]) for i, c in enumerate(eng['c']))
    check(worst < 1e-9, f'{k}: engine waits equal the Lindley recursion for all {n} customers (max abs diff {worst:.1e})')

print('4. Long random runs vs theory (6 replications x 100,000 customers; bootstrap service times have POPULATION variance)')
N = 20000 if QUICK else 100000; REPS = 3 if QUICK else 6
for k in RAW:
    cs = customers(k); ia = [c['ia'] for c in cs[1:]]; sv = [c['sv'] for c in cs]
    mi, ms = st.mean(ia), st.mean(sv); l, m_ = 1 / mi, 1 / ms
    rnd = random.Random(42)
    runs = {'M/M/1': (lambda: rnd.expovariate(l), lambda: rnd.expovariate(m_), mm1(l, m_)),
            'M/G/1': (lambda: rnd.expovariate(l), lambda: rnd.choice(sv), mg1(l, ms, st.pvariance(sv))),
            'G/G/1': (lambda: rnd.choice(ia), lambda: rnd.choice(sv), gg1(l, m_, st.pvariance(ia) / mi**2, st.pvariance(sv) / ms**2))}
    for nm, (a_, s_, th) in runs.items():
        rs = [engine(a_, s_, N) for _ in range(REPS)]
        wq = st.mean(r_['Wq'] for r_ in rs); ut = st.mean(r_['util'] for r_ in rs)
        err = (wq - th['Wq']) / th['Wq']
        print(f'   {k} {nm}: sim Wq={wq:.3f} theory Wq={th["Wq"]:.3f} ({err:+.1%}) | sim busy={ut:.3f} theory rho={th["rho"]:.3f}')
        check(abs(ut - th['rho']) < 0.02, f'{k} {nm}: simulated busy fraction ~ rho')
        if nm != 'G/G/1':   # exact formulas must agree with simulation up to Monte Carlo noise
            check(abs(err) < 0.05, f'{k} {nm}: simulated Wq within 5% of the exact formula')
        else:               # approximation: reported, not asserted
            print(f'        (informational) G/G/1 is an approximation; gap to the simulated value here is {err:+.1%}')
    print(f'   {k}: the app\'s M/G/1 theory uses SAMPLE variance: Wq={mg1(l, ms, st.variance(sv))["Wq"]:.3f} (population variance, which the bootstrap run actually has: {mg1(l, ms, st.pvariance(sv))["Wq"]:.3f})')

print('\nALL CHECKS PASSED' if ok else '\nSOME CHECKS FAILED')
sys.exit(0 if ok else 1)
