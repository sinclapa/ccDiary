# Grafana assets

## `city-gazetteer.json`

Coordinates for the **ccDiary — Diary readership** dashboard's "Where readers are" map
(`cookingcode.grafana.net/d/ccdiary-readership`).

Faro reports a reader's location as names only (`geo_city`, `geo_country_iso`), and Grafana's
built-in gazetteer is country level, which places every UK reader at the UK's centre in southern
Scotland. This file maps `"<city>|<country ISO>"` (for example `Wimbledon|GB`) to a latitude and
longitude so the map can place each reader at their city.

- **Source:** [GeoNames](https://www.geonames.org) `cities5000` (places of 5,000+ people),
  licensed [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/). The map panel credits it.
- **Keys:** the GeoNames name, its ASCII form and its accent-stripped form, so `Zürich|CH`,
  `Zuerich|CH` and `Zurich|CH` all match. Where a name repeats within a country, the most
  populous place keeps it.
- **Served to Grafana** through jsDelivr, which sets CORS headers, pinned to a commit:
  `https://cdn.jsdelivr.net/gh/sinclapa/ccDiary@<commit>/observability/grafana/city-gazetteer.json`.
  Pinning means a regenerated file never changes the live map until the panel is pointed at it.
- **Misses:** a city geo-IP reports that is not in the list gets no dot on the map; it still
  appears in the "Readers by location" table beside it.

Regenerate with:

```powershell
./scripts/buildCityGazetteer.ps1                           # cities5000 (default)
./scripts/buildCityGazetteer.ps1 -MinimumPopulation 1000   # more towns, ~15 MB
```
