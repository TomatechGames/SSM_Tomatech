# SuperSmashyMaker_Prod
 
My contributions to turn Super Smashy Maker into a playable experience with an easily extendable set of subsystems.

In addittion to project-specific elements such as the players movement controller, the main branch contains implementations of 
- Reanimation (an open source animation package developed by Aarthificial)
- Reanimation Graph (a package i developed to provide node-based animation logic for Reanimation)
- RePalette (a Theme System designed to provide an easy way to specify texture changes based on the selected Game Style and Course Theme)
- AFASS (a save system for levels that serialises tiles and level objects using their Addressables address)

The spawners-incomplete branch, as the name implies, contains partial work towards getting AFASS objects to be pooled and spawned by abstract spawner objects
