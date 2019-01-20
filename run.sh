#!/bin/sh

warmup () {
    sleep 60

    for i in {1..10}; do
        curl -s -o /dev/null http://127.0.0.1/accounts/filter/?likes_contains=$1&query_id=$1&limit=50
        curl -s -o /dev/null http://127.0.0.1/accounts/group/?likes=$1&keys=sex&query_id=$1&limit=50
        curl -s -o /dev/null http://127.0.0.1/accounts/$1/suggest/?query_id=$1&limit=50
        curl -s -o /dev/null http://127.0.0.1/accounts/$1/recommend/?query_id=$1&limit=50
    done

    curl -s -o /dev/null http://127.0.0.1/accounts/10000000
}

warmup & hlcup2018.exe
