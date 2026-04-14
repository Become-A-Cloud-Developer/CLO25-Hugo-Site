# CloudSoft Recruitment Portal — Implementation Progress

Last updated: 2026-03-28
Last commit: 155fbbb

## Phases
- [x] Phase 1: Project Scaffolding (f9c2fd2)
- [x] Phase 2: Domain Models + Repositories (7458922)
- [x] Phase 3: Service Layer + Controller + Views (c82be09)
- [x] Phase 4: Unit Tests — Week 1 (9a7b211)
- [x] Phase 5: Docker + Docker Compose — Week 1 (816309a)
- [x] Phase 6: Identity + Auth — Week 2 (0b2586b)
- [x] Phase 7: Application Entity + Service — Week 2 (a1a166e)
- [x] Phase 8: Blob Storage — CV Upload — Week 3 (4ce3f93)
- [x] Phase 9: Azure Infrastructure — Bicep (155fbbb)
- [x] Phase 10: Documentation

DONE

## Notes
- Used `AddIdentity` instead of `AddDefaultIdentity` (no Identity UI package needed)
- Used `ConfigureExternalAuthenticationProperties` (API updated in .NET 10)
- Bicep validates with warnings only (cosmos-db connection string output, log analytics key output) — acceptable for teaching
- docker-compose uses `mongo:latest` instead of `mongo:7` to avoid long pull on ARM Mac
