INCLUDE included1.ink
INCLUDE inc/included2.ink

VAR Testing = false

{Testing:
    We are testing!
}

-> Intro

=== Intro

Test Line 1
Test Line 2
* [Branch 1]
    -> Branch1
* [Branch 2]
    -> Branch2
* [Branch 3]
    -> Branch3
* Branch 4
    -> Branch3

=== Branch1
Branch 1
-> IncludedStuff ->
-> END

=== Branch2
Branch 2
-> IncludedStuff2 ->
-> END

=== Branch3
Branch 3
-> END