highloadcup_tester2018 -addr http://127.0.0.1:80 -hlcupdocs ./input_min/ -test -phase 1 -utf8 -allow-nulls > test
highloadcup_tester2018 -addr http://127.0.0.1:80 -hlcupdocs ./input_min/ -test -phase 2 -utf8 -allow-nulls >> test
timeout 1
highloadcup_tester2018 -addr http://127.0.0.1:80 -hlcupdocs ./input_min/ -test -phase 3 -utf8 -allow-nulls >> test

rem highloadcup_tester2018 -addr http://127.0.0.1:80 -hlcupdocs ./input_min/ -tank 100 -phase 1 -utf8 -allow-nulls > test
rem highloadcup_tester2018 -addr http://127.0.0.1:80 -hlcupdocs ./input_min/ -tank 200 -phase 2 -utf8 -allow-nulls >> test
rem highloadcup_tester2018 -addr http://127.0.0.1:80 -hlcupdocs ./input_min/ -tank 1000 -phase 3 -utf8 -allow-nulls >> test
