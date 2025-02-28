INCLUDE included1.ink
INCLUDE inc/included2.ink

VAR TEST_RUN = false

-> Intro

EXTERNAL TestFn()
=== function TestFn() ===
~ return true

=== Intro
{TEST_RUN:
    We are testing!
}

~ TestFn()
Test Line 1
Test Line 2
* [Branch 1]
    -> Branch1
* [Branch 2]
    -> Branch2
* [Branch 3]
    -> Branch3
+ Not hidden option.
    -> OptionBranch
* [Hidden Option] With Extra Text.
    -> OptionBranch
* Another Option [] With Extra Text.
    -> OptionBranch

TODO: Does this work?
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

=== OptionBranch
Fancy option branch.
-> END