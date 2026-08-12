# Storage data model

ccDiary is moving its persistence from Azure SQL (serverless, EF Core) to **Azure Table
Storage + Azure Blob Storage** in a single `Standard_LRS` account. The driver is spin-up
time and cost: the serverless database pauses after an hour idle and charges the first
request a 30–60 second resume, and its free grant is a cliff rather than a slope. Storage
is an always-on HTTP endpoint, and at this data volume it costs pennies a year.

This document records the decisions that are **expensive to change later**, because a
key-value store has no migration step. Adding a field is free. Changing where a row lives
is not: nothing moves it, so old rows are simply never found again.

> **Status.** The primitives in `src/api/ccDiaryApi/Data/Storage/` exist and are tested.
> The services still read and write SQL. The cutover is a later change.

## Why Table + Blob rather than something else

| Option | Why not |
|---|---|
| Keep Azure SQL serverless | The resume latency and the free-tier cliff are the problem being solved. |
| Cosmos DB (EF provider) | Smaller code change, but a 2 MB item cap still forces images into blobs, so most of the work remains — and it is not what the sibling `slypn` project proved out. |
| SQLite on an Azure Files mount | Cheapest, but file-share locking under a scale-to-zero container app is fragile. |

The design follows `../slypn`, which solves the same problem the same way, with two
deliberate divergences noted below.

## Tables

Six tables, created at startup by `StorageBootstrapper`. Every row carries a
`SchemaVersion` column and a `Json` column holding the serialised entity; the other
columns exist only so they can be filtered or sorted on.

| Table | PartitionKey | RowKey | Broken-out columns |
|---|---|---|---|
| `diary` | `"diary"` (constant) | `DiaryId:N` | DiaryId, Title, Author, OwnerId |
| `diaryentry` | `DiaryId:N` | `{yyyyMMddHHmmssfffffff}-{DiaryEntryId:N}` | DiaryEntryId, DiaryId, Date, HasImage, ImageContentType, JsonInBlob |
| `appuser` | `"user"` (constant) | `EntraObjectId` | UserId, Email, Role |
| `accessrequest` | `"request"` (constant) | `AccessRequestId:N` | Status, Email, RequestedAt |
| `appinfo` | `"appinfo"` | `"1"` | InformationalVersion, DatabaseLastUpdated |
| `geocodingcache` | `"geo"` | `SHA256(normalised query)[..32]` | Query, Lat, Lon, CachedAt |

### The diary entry row key is the load-bearing decision

Table Storage sorts row keys lexicographically within a partition and supports
`RowKey ge` / `RowKey lt` range filters. A fixed-width, zero-padded UTC timestamp prefix
therefore buys, with no secondary index:

- entries returned already in date order;
- date-range queries evaluated server-side;
- min date as the first row of the partition;
- deterministic tie-breaking, via the entry id suffix.

The costs, and how they are handled:

- **Row keys are immutable; dates are not.** Changing an entry's date means insert-new +
  delete-old. Both rows share a partition, so it goes in one transaction and is atomic.
- **There is no descending order.** Max date needs a partition drain selecting only
  `RowKey` — a few KB for a diary of this size.
- **Fetching an entry by id alone** (the route carries no diary id) is a cross-partition
  filter on the `DiaryEntryId` column. One request at this scale; if entries ever pass
  ~20k, add an id-to-location index table.

### Two deliberate divergences from slypn

**Access requests use a constant partition, not status-as-partition-key.** slypn
partitions articles by status. Status here is mutable, and status-as-partition-key turns
every approval into a cross-partition write-then-delete that cannot be transactional —
a trade-off slypn documents in its own model notes. A constant partition with `Status`
broken out keeps the filter server-side and the transition atomic.

**Authentication is managed identity, not a connection string.** slypn uses a connection
string because its Free-tier Static Web App has no managed identity. ccDiary's Container
App already has a system-assigned one, so the account can set
`allowSharedKeyAccess: false` and the application holds no secret at all. A connection
string is still supported for Azurite locally.

## Blob containers

| Container | Key layout | Notes |
|---|---|---|
| `images` | `{diaryId:N}/{entryId:N}` | Content type stored on the blob *and* mirrored in the table row, so listing never needs a blob HEAD. The diary prefix makes cascade delete a prefix scan. |
| `mapcache` | `tiles/{source}/{z}/{x}/{y}`, `routes/{profile}/{key}.json` | Lifecycle-managed, 90 days. |
| `content` | `entries/{entryId:N}.json` | Spill for oversized entry JSON. |

### Why images must leave the row

A Table entity caps at 1 MB and a single string property at 64 KB. The real data has 30
images totalling 23.6 MB of base64, the largest a single 3.3 MB image. They cannot live
in a row under any partitioning scheme.

**The HTTP contract is unchanged.** The API still returns base64 in `imageData`; the blob
is read and re-encoded server-side. Moving to an image URL is a separate change with its
own UI, e2e and archive-format consequences.

### Why the tile and route caches are blobs, not tables

1. **Blob lifecycle management gives free TTL eviction.** The SQL implementation had *no*
   eviction at all — expired rows were never read and never deleted, growing without
   bound. A lifecycle rule fixes that with no code. Table Storage has no equivalent.
2. Tile PNGs and OSRM polyline JSON routinely exceed the 64 KB property cap.
3. **It removes a full table scan.** The SQL routing lookup compared coordinates with
   `Math.Abs(...) < 1e-9`, which cannot use an index. Quantising to six decimal places
   (~0.1 m) makes the lookup an exact-match blob fetch.

### Entry text spill

Entry text averages ~5 KB, but one long entry could exceed the 64 KB property cap. Rather
than blob every entry body the way slypn does — the timeline reads *all* entries, so that
would add one blob read per entry — a row whose serialised JSON exceeds
`JsonSpillThresholdBytes` (30,000) writes to `content/` and sets `JsonInBlob`. In
practice this path is almost never taken.

## Frozen decisions

These cannot be changed without migrating data. They are pinned by tests in
`ccDiaryApiTest/Storage/`.

1. **Every function in `StorageKeys`.** Changing a derivation does not move rows, it
   orphans them.
2. **camelCase property naming in the `Json` column.** Reading is case-insensitive as a
   safety net, but writing is camelCase so a stored row reads like an API payload.
3. **Enums stored as kebab-case strings**, matching the wire format. Strings rather than
   ordinals, so reordering an enum cannot silently reinterpret existing rows.
4. **Dates stored as round-trip UTC.** `DateTimeKind.Unspecified` is treated as UTC, never
   as server-local, so keys and values do not depend on which machine wrote them.

## Schema evolution

There is no migration step. A new property appears with its CLR default on rows written
before it existed, which is the whole mechanism.

For that to work, the storage serializer **disables required-property enforcement**.
`DiaryEntryDTO` marks `ShowMap`/`ShowJourney` `[JsonRequired]` and `DiaryId` `required`,
which is correct for the HTTP contract — a client omitting them should be rejected —
but applied to stored rows it converts "fall back to a default" into a hard
deserialisation failure, making any future field change a data-loss event. The values are
written by this application, not by a client, so there is nothing to validate on the way
back in.

Two sharp edges remain, and no test can catch them:

- **Renaming a property silently loses its data.** Treat a rename as a migration.
- **Removing a field is safe; repurposing one is not.** Old rows still carry the old
  meaning under the same name.

## What this gives up

- **No point-in-time restore and no entity soft-delete.** Azure SQL gave 7-day PITR free.
  Blob soft delete, the in-repo `data/ww1-diary.json`, and a scheduled export are
  mitigations, not equivalents. This is the largest single loss and is accepted knowingly.
- **No referential integrity.** Cascade delete becomes application code.
- **No text search.** The Table filter grammar has no substring or `startswith` operator,
  so text search is an in-memory scan of the diary's partition — comfortable to roughly
  5,000 entries, beyond which the answer is an inverted index or Azure AI Search.
- **Multi-entity writes are not atomic** beyond a single partition's 100-entity
  transaction. Archive import and diary delete are therefore idempotent and re-runnable
  rather than transactional.

## Local development

Azurite provides Table and Blob locally:

```powershell
docker compose -f src/api/docker-compose.yml up -d azurite
```

CI runs the same image as a service container in the `build-api` job. The storage tests
run against the emulator rather than a fake, deliberately: row key ordering, filter
evaluation, the 100-entity transaction limit and the SDK's 404/409 semantics are exactly
what a fake would get wrong, and tests over a fake would be testing the fake.

Azurite is not perfectly faithful — it does not reliably enforce the 64 KB property or
1 MB entity limits, and it permits deleting and immediately recreating a table where real
Azure blocks the name for around 40 seconds. Size thresholds and key formats are
therefore covered by direct unit tests, never by expecting an emulator error.
