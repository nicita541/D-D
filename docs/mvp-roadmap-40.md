# RPG MVP Roadmap

This roadmap is planned module ownership, not a claim that every item is implemented.

1. `play/act` orchestrator -> Play
2. resolve-and-continue -> Play, Mechanics, Turns
3. operation policy expansion -> Changes
4. travel/move -> Travel, World
5. play-integrated combat -> Play, Combat
6. monsters -> World, Combat
7. advanced checks -> Mechanics
8. automatic check consequences -> Play, Changes, Mechanics
9. campaign-template bootstrap -> Campaigns, Play, World, Quests
10. campaign settings -> Campaigns
11. AI JSON contract v2 -> Ai, Turns, Play
12. suggested actions -> Play, Turns
13. player notes -> Notes
14. snapshots/rollback -> Snapshots
15. economy-lite -> Economy
16. shops -> Economy, World
17. loot system -> Economy, Inventory, Combat
18. inventory effects -> Inventory, Mechanics, Conditions
19. carrying capacity -> Inventory, Survival
20. rest system -> Rest, Characters, Time, Conditions
21. time/calendar -> Time
22. survival mechanics -> Survival
23. condition duration/effects -> Conditions
24. death/knockout -> Characters, Combat, Conditions
25. progression/level-up -> Progression
26. quest completion/rewards -> Quests, Economy, Progression
27. NPC relationship/reputation -> World, Campaigns
28. dialogue system -> Dialogues, Mechanics, World
29. world events/clocks -> WorldEvents
30. random encounter system -> WorldEvents, Travel, Combat
31. better campaign memory -> Memory
32. AI context optimization -> Ai, Memory, Play
33. AI turn recovery -> Turns, Ai
34. idempotency keys -> Shared, Play, Turns
35. API documentation -> docs
36. GitHub CI -> `.github/workflows`
37. long scenario integration tests -> Tests/Integration
38. security hardening -> Shared/Security and all modules
39. admin/dev tools -> DevTools
40. export/import/versioning -> ExportImport

## Suggested Tranches

Tranche 2:
- monster lifecycle
- combat victory/defeat
- loot
- XP
- quest completion/rewards
- currency

Tranche 3:
- advanced checks
- skill checks
- saving throws
- attack/damage rolls
- automatic consequences

Tranche 4:
- campaign templates
- campaign settings
- AI JSON contract v2
- suggested actions

Tranche 5:
- snapshots
- notes
- rest
- time
- condition duration
- survival

Tranche 6:
- progression/level-up
- shops
- dialogue
- world clocks
- random encounters
- export/import
- dev tools
- CI/docs/security
