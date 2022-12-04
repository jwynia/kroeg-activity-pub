t# Kroeg
> Noun (plural **kroegen**, diminutive **kroegje**) - (informal) a pub.

A reference implementation for [ActivityPub](http://www.w3.org/TR/activitypub/).

## Info
This server can be set up to create a mostly-functional ActivityPub server, which is able to federate to other servers. It is mostly standalone, only requires a PostgreSQL server, and can be ran in load-balancing situations.
See [Kroeg.Server's README](Kroeg.Server/README.md) for more information. 

## License
Kroeg is licensed under the MIT license. See [LICENSE](LICENSE) for more information.

## JWynia Notes
Hard-coded a few package versions to get it to compile. Not sure whether this is going to be just so it can be easily referred to or if it's worth pulling it forward to 6.0LTS or 7.0 versions and get it actually running. It looks like there are still problems with the views, but it does build on my Mac with these package versions.
There were some broken paths to actions/views. Given there are several actions that exist with the same name in more than one controller, I made my best guess to get them to resolve. See my commits from 2022/12/03 for what those guesses were.