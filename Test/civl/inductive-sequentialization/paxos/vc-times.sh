for f in $1/*.trace
do
    boogie-trace-parser.py $f
done
