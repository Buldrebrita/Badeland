# The obstacle course

Built by `Badeland > Create S3 Course Test Scene` (and S4 for online). ONE continuous inflatable course on the sea:
the route snakes from the start platform (north-west) east, south, west, south and east again to the finish platform
near the beach. Obstacles are joined end to end; square corner pads where the route turns; railings on the wide parts.

1 Start platform, 2 Wobble bridge, 3 Curved tunnel, 4 Bouncing pillars, 5 Climbing wall, 6 Floating logs,
7 Rotating platform, 8 Trampoline, 9 Swinging balls, 10 Slide, 11 Balance section, 12 Rotating padded arms,
13 Arch maze, 14 Final bridge, 15 Finish platform (the monster appears here).
Plus twenty harder obstacles in four extra lanes (A, B, C, D). The whole course floats 2.4 m above the sea on pylons (you cannot climb back out of the water); swimming is slow and after ~2 s you are put back at the start of the obstacle. Being hit makes you dizzy for 3 s (slower, camera sways).

Code: `InflatableCourse.cs` (route and obstacles), `CourseScenery.cs` (beach, marina, crowds, town, lighthouse, ferris wheel),
`MeshKit.cs`, `CourseExtras.cs` (monster, alarm, trap, slide, look). Scripts: `SoftPlatform`, `RollingLog`, `SpinningPlatform`,
`Pendulum`, `CourseRespawn`, `Cheerer`, `RectBounds`, `HiddenChest`.
Fall in the water: you are put back at the start of the obstacle you fell from (after ~2 s of swimming).
