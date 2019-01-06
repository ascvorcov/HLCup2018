highloadcup_tester2018 -addr http://127.0.0.1:80 -hlcupdocs ./input_min/ -concurrent 50 -test -phase 1 -utf8 -allow-nulls > test
highloadcup_tester2018 -addr http://127.0.0.1:80 -hlcupdocs ./input_min/ -concurrent 50 -test -phase 2 -utf8 -allow-nulls >> test
highloadcup_tester2018 -addr http://127.0.0.1:80 -hlcupdocs ./input_min/ -concurrent 50 -test -phase 3 -utf8 -allow-nulls >> test
