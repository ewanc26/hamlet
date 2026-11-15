# data

Content atoms: one JSON file per entity at `samples/<entity>.json` (buildings,
needs, policies, goods). `samples/bakery.json` is the reference shape.

Schemas land with M2 (data-driven content, §5.2). The loader will ignore
unknown fields with warnings, fail loudly on malformed files, and record
content hashes in save headers so replays refuse to run against a different
content set (§4, data contract).
