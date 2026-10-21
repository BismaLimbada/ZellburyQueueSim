"""Independent Python port of the C# logic (validation, trace builder, M/M/1 / M/G/1 / G/G/1 formulas,
KS / lag-1 checks, discrete-event engine). The data arrays are parsed OUT OF the C# source, so this also
proves the C# data transcription matches the workbook.

Usage (from the project root):  python3 tools/verify_port.py [path/to/Group11-data_CORRECTED.xlsx]
The workbook is optional: without it the C#-vs-xlsx comparison is skipped.
Needs: openpyxl. Written because the C# could not be compiled in the environment it was developed in -
run `dotnet build` / `dotnet run` yourself as well."""
import re, math, random, statistics as st, sys, heapq
SRC='Services/ZellburyDataService.cs'
src=open(SRC).read()
def arr(name):
    blk=re.search(name+r'\s*=\s*\n\s*\{(.*?)\n    \};',src,re.S).group(1)
    return [tuple((int(h)-16)*60+int(m)-20 for h,m in re.findall(r'new\(\s*(\d+),\s*(\d+)\)',row)) for row in re.findall(r'\(new.*?\),?\n|\(new.*?\)\)',blk)]
raw={'F':arr('FemaleRaw'),'M':arr('MaleRaw')}
# --- 1. transcription vs xlsx
if len(sys.argv)>1:
    import openpyxl
    wb=openpyxl.load_workbook(sys.argv[1]); ws=wb.active; cur=None; x={}
    for r in ws.iter_rows(values_only=True):
        if r[1]=='Female Counter': cur='F'; x[cur]=[]; continue
        if r[1]=='Male Counter': cur='M'; x[cur]=[]; continue
        if cur and hasattr(r[0],'hour'): x[cur].append(tuple((t.hour-16)*60+t.minute-20 for t in r))
    for k in 'FM':
        assert raw[k]==x[k], k
        print(k,len(raw[k]),'rows: C# arrays == xlsx  OK')
else:
    print('no .xlsx given: skipping C#-vs-workbook comparison')
def clock(m): return '%02d:%02d'%(16+(20+m)//60,(20+m)%60)
def customers(k):
    c=[]
    for i,(a,s,e) in enumerate(raw[k]):
        c.append(dict(id=i+1,ia=a if i==0 else a-raw[k][i-1][0],a=a,s=s,e=e,w=s-a,sv=e-s,t=e-a))
    return c
def horizon(k): return max(60,max(r[2] for r in raw[k]))
# --- 2. issues
def issues(k):
    out=[];rows=customers(k);n=len(rows)
    for c in rows:
        if c['s']<c['a']: out.append(('Error',c['id'],'neg wait'))
        if c['e']<c['s']: out.append(('Error',c['id'],'neg service'))
    for i in range(1,n):
        if rows[i]['a']<rows[i-1]['a']: out.append(('Warning',rows[i]['id'],'arrival order'))
    bs=sorted(rows,key=lambda c:(c['s'],c['e'],c['id']))
    for q in range(1,n):
        p,c=bs[q-1],bs[q]
        if c['s']<p['e']: out.append(('Error',c['id'],'overlap'))
        if c['s']>p['e']:
            w=[d['id'] for d in bs[q:] if d['a']<=p['e']]
            if w: out.append(('Warning',w[0],f"idle {clock(p['e'])}-{clock(c['s'])} while {w[0]} waiting; {c['id']} first"))
    for c in rows:
        parts=[nm for nm,v in(('Arrival',c['a']),('Start',c['s']),('Exit',c['e'])) if v<0 or v>60]
        if parts: out.append(('Warning',c['id'],'outside window: '+','.join(parts)+' '+clock(max(c['a'],c['s'],c['e']))))
    return out
for k in 'FM':
    print('--',k,'issues'); [print('  ',i) for i in issues(k)]
# --- 3. trace builder
def trace(k):
    rows=customers(k);H=horizon(k);ch={}
    def add(t,dw,ds): w,s=ch.get(t,(0,0)); ch[t]=(w+dw,s+ds)
    for c in rows: add(c['a'],1,1); add(c['s'],-1,0); add(c['e'],0,-1)
    last=0;aq=ab=0;w=ins=mx=0
    for t in sorted(ch):
        dt=t-last; aq+=w*dt; ab+= dt if ins-w>0 else 0; last=t; w+=ch[t][0]; ins+=ch[t][1]; mx=max(mx,w)
    end=max(H,last)
    if end>last: aq+=w*(end-last); ab+= (end-last) if ins-w>0 else 0
    n=len(rows)
    r=dict(Wq=st.mean(c['w'] for c in rows),W=st.mean(c['t'] for c in rows),S=st.mean(c['sv'] for c in rows),Lq=aq/end,util=ab/end,maxq=mx,H=end,
           ia=st.mean(c['ia'] for c in rows[1:]),sumT=sum(c['t'] for c in rows),sumS=sum(c['sv'] for c in rows))
    r['L']=r['Lq']+r['util']
    return r
for k in 'FM':
    r=trace(k); print(k,{a:round(b,4) for a,b in r.items()})
    assert abs(r['L']-r['sumT']/r['H'])<1e-9, 'Little/area identity'
    assert abs(r['util']-r['sumS']/r['H'])<1e-9, 'busy area == sum service'
    # Little's law with observed: L = lambda_obs * W where lambda_obs=n/H
    print('   Little check  L=%.4f  n/H*W=%.4f'%(r['L'],len(raw[k])/r['H']*r['W']))
# --- 4. theory
def mm1(l,m):
    rho=l/m; lq=rho**2/(1-rho); wq=lq/l; w=wq+1/m; return dict(rho=rho,Lq=lq,Wq=wq,W=w,L=l*w)
def mg1(l,es,v):
    rho=l*es; lq=(l*l*v+rho**2)/(2*(1-rho)); wq=lq/l; w=wq+es; return dict(rho=rho,Lq=lq,Wq=wq,W=w,L=l*w)
def gg1(l,m,ca,cs):
    rho=l/m; lq=rho**2*(1+cs)*(ca+rho**2*cs)/(2*(1-rho)*(1+rho**2*cs)); wq=lq/l; w=wq+1/m; return dict(rho=rho,Lq=lq,Wq=wq,W=w,L=l*w)
# consistency identities
a=mm1(.5,.6); b=mg1(.5,1/.6,1/.6**2); c=gg1(.5,.6,1,1)
for key in a: assert abs(a[key]-b[key])<1e-12 and abs(a[key]-c[key])<1e-12,key
print('identity: MG1(var=1/mu^2)==MM1==GG1(Ca=Cs=1) OK', {k:round(v,4) for k,v in a.items()})
params={}
for k in 'FM':
    rows=customers(k); ia=[c['ia'] for c in rows[1:]]; sv=[c['sv'] for c in rows]
    mi,vi,ms,vs=st.mean(ia),st.variance(ia),st.mean(sv),st.variance(sv)
    p=dict(l=1/mi,m=1/ms,es=ms,vs=vs,ca=vi/mi**2,cs=vs/ms**2,ia=ia,sv=sv); params[k]=p
    print(k,'lambda=%.4f mu=%.4f rho=%.4f Ca2=%.3f Cs2=%.3f Var(S)=%.3f'%(p['l'],p['m'],p['l']/p['m'],p['ca'],p['cs'],vs))
    print('  MM1',{a:round(b,3) for a,b in mm1(p['l'],p['m']).items()})
    print('  MG1',{a:round(b,3) for a,b in mg1(p['l'],ms,vs).items()})
    print('  GG1',{a:round(b,3) for a,b in gg1(p['l'],p['m'],p['ca'],p['cs']).items()})
# --- 5. KS / lag1 (port of StatisticsService)
def ks(sample,rate):
    # Exponential with ESTIMATED rate (D'Agostino & Stephens 1986): reject at 5% when
    # (D - 0.2/n)(sqrt(n)+0.26+0.5/sqrt(n)) > 1.094, reported as the equivalent critical value of D.
    s=sorted(sample);n=len(s);md=0
    for i,v in enumerate(s):
        F=1-math.exp(-rate*v); md=max(md,abs(F-i/n),abs(F-(i+1)/n))
    r=math.sqrt(n)
    return md,1.094/(r+0.26+0.5/r)+0.2/n
def lag1(seq):
    n=len(seq);m=st.mean(seq);num=sum((seq[i]-m)*(seq[i+1]-m) for i in range(n-1));den=sum((x-m)**2 for x in seq);return num/den,1.96/math.sqrt(n)
for k in 'FM':
    p=params[k]
    print(k,'KS arr',[round(v,3) for v in ks(p['ia'],p['l'])],'KS svc',[round(v,3) for v in ks(p['sv'],p['m'])],'lag1 arr',[round(v,3) for v in lag1(p['ia'])],'lag1 svc',[round(v,3) for v in lag1(p['sv'])])
# --- 6. engine port + convergence of the 3 random runs vs theory
def engine(next_ia,next_sv,N):
    fel=[];seq=0
    def sched(t,typ,cid):
        nonlocal seq; heapq.heappush(fel,(t,seq,typ,cid)); seq+=1
    cust={};q=[];busy=False;clock=0;last=0;aq=ab=0
    ia=next_ia(); cust[1]=dict(a=ia,ia=ia); sched(ia,'A',1); la=ia; nid=2;sch=1
    while fel:
        t,_,typ,cid=heapq.heappop(fel); clock=t; d=clock-last; aq+=len(q)*d; ab+=(1 if busy else 0)*d; last=clock
        if typ=='A':
            c=cust[cid]
            if not busy: busy=True; c['s']=clock; c['sv']=next_sv(); c['e']=c['s']+c['sv']; sched(c['e'],'D',cid)
            else: q.append(cid)
            if sch<N:
                g=next_ia(); la+=g; cust[nid]=dict(a=la,ia=g); sched(la,'A',nid); nid+=1; sch+=1
        else:
            cust[cid]['done']=True
            if q:
                n=q.pop(0);c=cust[n];c['s']=clock;c['sv']=next_sv();c['e']=clock+c['sv'];sched(c['e'],'D',n)
            else: busy=False
    cs=[c for c in cust.values() if c.get('done')]
    return dict(Wq=st.mean(c['s']-c['a'] for c in cs),W=st.mean(c['e']-c['a'] for c in cs),Lq=aq/clock,util=ab/clock,n=len(cs),ia=st.mean(c['ia'] for c in cust.values()))
rnd=random.Random(42)
for k in 'FM':
    p=params[k]; N=200000
    ex=lambda r: (lambda: rnd.expovariate(r))
    emp=lambda s: (lambda: rnd.choice(s))
    runs={'MM1':(ex(p['l']),ex(p['m']),mm1(p['l'],p['m'])),
          'MG1':(ex(p['l']),emp(p['sv']),mg1(p['l'],p['es'],st.pvariance(p['sv']))),   # bootstrap var is population var
          'GG1':(emp(p['ia']),emp(p['sv']),gg1(p['l'],p['m'],p['ca'],p['cs']))}
    for nm,(a,s,th) in runs.items():
        r=engine(a,s,N)
        print(k,nm,'sim Wq=%.3f W=%.3f Lq=%.3f rho=%.3f | theory Wq=%.3f W=%.3f Lq=%.3f rho=%.3f'%(r['Wq'],r['W'],r['Lq'],r['util'],th['Wq'],th['W'],th['Lq'],th['rho']))
