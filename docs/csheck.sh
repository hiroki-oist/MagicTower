#!/bin/bash
U=/Applications/Unity/Hub/Editor/2022.3.11f1/Unity.app/Contents
P="$HOME/Code/MagicTower"
OUT="$1"; shift
REFS=(-r:"$U/NetStandard/ref/2.1.0/netstandard.dll")
for f in "$U/Managed/UnityEngine/"UnityEngine.*Module.dll; do REFS+=(-r:"$f"); done
for f in "$P/Library/ScriptAssemblies/"*.dll; do
  case "$(basename "$f")" in Assembly-CSharp*.dll) ;; *) [ -f "$f" ] && REFS+=(-r:"$f");; esac
done
"$U/MonoBleedingEdge/bin/mcs" -target:library -out:"$OUT" -noconfig -nostdlib \
  -nowarn:0169,0649,0414,0108,0219,1685,0618 "${REFS[@]}" "$@" 2>&1 \
  | grep -vE "^/Applications|^/Users|warning CS"
