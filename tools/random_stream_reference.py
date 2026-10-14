#!/usr/bin/env python3
"""Independent reference implementation of the kernel's RandomStream (ADR-0007 "Random streams").

It exists so that the definition can be checked from the ADR alone: tools/tests/test_random_stream_reference.py and
the C# RandomStreamTests pin the same golden values. Print values for a stream:

  python3 tools/random_stream_reference.py <seed> <purpose> [key ...] [--draws N]
"""
import argparse

MASK=(1<<64)-1
GAMMA=0x9E3779B97F4A7C15

def mix(z):
    """SplitMix64 finalizer."""
    z&=MASK
    z=((z^(z>>30))*0xBF58476D1CE4E5B9)&MASK
    z=((z^(z>>27))*0x94D049BB133111EB)&MASK
    return z^(z>>31)

def fnv1a(text):
    """64-bit FNV-1a over UTF-8."""
    value=0xCBF29CE484222325
    for byte in text.encode('utf-8'):
        value=((value^byte)*0x100000001B3)&MASK
    return value

def stream_key(seed,purpose,*keys):
    key=mix(seed^fnv1a(purpose))
    for part in keys:
        key=mix(key^mix((part+GAMMA)&MASK))
    return key

def draw(key,index):
    """Draw number index (0-based) of the stream with this key."""
    return mix((key+(index+1)*GAMMA)&MASK)

def main(argv=None):
    parser=argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument('seed',type=int)
    parser.add_argument('purpose')
    parser.add_argument('keys',type=int,nargs='*')
    parser.add_argument('--draws',type=int,default=3)
    args=parser.parse_args(argv)
    key=stream_key(args.seed,args.purpose,*args.keys)
    print(f'key {key:#018x}')
    for index in range(args.draws): print(f'draw {index} {draw(key,index):#018x}')

if __name__=='__main__': main()
