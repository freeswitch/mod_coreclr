#!/bin/bash
swig -I/usr/include/freeswitch -I./inc -v -O -c++ -outdir Loader -o src/swig.cpp -csharp -namespace FreeSWITCH -dllimport mod_coreclr -outfile swig.cs swig.i
