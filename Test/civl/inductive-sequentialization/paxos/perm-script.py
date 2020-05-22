#! /usr/bin/env python3

import itertools

files = ['Paxos', 'PaxosSeqAxioms', 'PaxosActions', 'PaxosAbstractions', 'PaxosSeq']

for perm in itertools.permutations(files):
    perm = list(perm)
    args = ' '.join([s + '.bpl' for s in perm])
    out = 'data/' + '_'.join(perm)
    print(f'echo {out}')
    print(f'gtime --format="%e" --output={out}.time dotnet ../../../../Source/BoogieDriver/bin/Debug/netcoreapp3.1/BoogieDriver.dll -nologo -useArrayTheory -trace {args} > {out}.trace')
