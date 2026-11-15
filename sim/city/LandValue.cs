namespace Hamlet.Sim.City;

/// <summary>
/// Per-tile land value on the shared 0-100 scale (§5.1).
/// Value rises with accessibility (proximity to jobs, shops, services)
/// and falls with pollution. Computed each tick as a multi-source BFS
/// distance field over the road graph — pre-sized arrays, no allocation
/// in the hot path, deterministic across runs.
/// </summary>
public sealed class LandValue
{
    /// <summary>Base value for road tiles with no particular proximity.</summary>
    private const int BaseValue = 30;

    /// <summary>Value added when a tile is on a firm source (distance 0).</summary>
    private const int FirmAtSource = 20;

    /// <summary>Value added when a tile is on a shop source (distance 0).</summary>
    private const int ShopAtSource = 15;

    /// <summary>How quickly value falls with distance from a firm.</summary>
    private const int FirmDistanceFalloff = 3;

    /// <summary>How quickly value falls with distance from a shop.</summary>
    private const int ShopDistanceFalloff = 2;

    /// <summary>Pollution penalty per level (0-100 scale).</summary>
    private const int PollutionPenaltyPerLevel = 5;

    /// <summary>Amenity bonus per level (0-100 scale).</summary>
    private const int AmenityBonusPerLevel = 5;

    /// <summary>Maximum grid size the land value system supports.</summary>
    private const int MaxGridSize = 256;

    /// <summary>The world grid for road/terrain queries.</summary>
    private readonly WorldGrid _grid;

    /// <summary>Distance from each road tile to the nearest firm source.</summary>
    private readonly sbyte[,] _firmDistance;

    /// <summary>Distance from each road tile to the nearest shop source.</summary>
    private readonly sbyte[,] _shopDistance;

    /// <summary>Pollution level per tile (0-100).</summary>
    private readonly byte[,] _pollutionLevel;

    /// <summary>Amenity level per tile (0-100, e.g. parks, plazas).</summary>
    private readonly byte[,] _amenityLevel;

    /// <summary>Per-tile composed land value (0-100).</summary>
    private readonly byte[,] _valueGrid;

    /// <summary>Reusable BFS queue — max size is gridWidth * gridHeight.</summary>
    private readonly (int x, int y)[] _bfsQueue;

    /// <summary>Number of entries currently in the BFS queue.</summary>
    private int _bfsHead;

    /// <summary>Create a land value calculator for the given grid.</summary>
    public LandValue(WorldGrid grid)
    {
        _grid = grid;
        var size = grid.Size;
        // Clamp to max supported size
        var effectiveSize = Math.Min(size, MaxGridSize);

        _firmDistance = new sbyte[effectiveSize, effectiveSize];
        _shopDistance = new sbyte[effectiveSize, effectiveSize];
        _pollutionLevel = new byte[effectiveSize, effectiveSize];
        _amenityLevel = new byte[effectiveSize, effectiveSize];
        _valueGrid = new byte[effectiveSize, effectiveSize];
        _bfsQueue = new (int x, int y)[effectiveSize * effectiveSize];

        // Initialise distances to max (far away)
        for (var y = 0; y < effectiveSize; y++)
            for (var x = 0; x < effectiveSize; x++)
            {
                _firmDistance[y, x] = sbyte.MaxValue;
                _shopDistance[y, x] = sbyte.MaxValue;
            }

        // Initialise pollution and amenities to zero
        for (var y = 0; y < effectiveSize; y++)
            for (var x = 0; x < effectiveSize; x++)
            {
                _pollutionLevel[y, x] = 0;
                _amenityLevel[y, x] = 0;
            }
    }

    /// <summary>Record a firm as a land-value source at grid position (x, y).</summary>
    public void AddFirmSource(int x, int y)
    {
        if (!_grid.InBounds(x, y)) return;
        var gx = x;
        var gy = y;
        if (!_grid.IsRoadAt(gx, gy)) return;
        _firmDistance[gy, gx] = 0;
    }

    /// <summary>Record a shop as a land-value source at grid position (x, y).</summary>
    public void AddShopSource(int x, int y)
    {
        if (!_grid.InBounds(x, y)) return;
        var gx = x;
        var gy = y;
        if (!_grid.IsRoadAt(gx, gy)) return;
        _shopDistance[gy, gx] = 0;
    }

    /// <summary>Record an amenity (park, plaza) as a land-value source at grid position (x, y).</summary>
    public void AddAmenitySource(int x, int y)
    {
        if (!_grid.InBounds(x, y)) return;
        var gx = x;
        var gy = y;
        // Amenities can be on or off road; set the amenity level here
        SetAmenity(gx, gy, 30); // park gives moderate amenity bonus
    }

    /// <summary>Set the pollution level at a tile (0-100).</summary>
    public void SetPollution(int x, int y, byte level)
    {
        if (!_grid.InBounds(x, y)) return;
        _pollutionLevel[y, x] = level;
    }

    /// <summary>Set the amenity level at a tile (0-100, e.g. parks, plazas).</summary>
    public void SetAmenity(int x, int y, byte level)
    {
        if (!_grid.InBounds(x, y)) return;
        _amenityLevel[y, x] = level;
    }

    /// <summary>
    /// Run one tick of the multi-source BFS distance field, then compose
    /// the final 0-100 land value per tile. Call once per simulation tick.
    /// The BFS queue is reused allocation-free each invocation.
    /// </summary>
    public void Tick()
    {
        var size = _valueGrid.GetLength(0); // assume square

        // ---- BFS from all firm sources ----
        // Reset distances: only propagate from tiles explicitly marked as source (distance 0)
        // First pass: reset all firm distances to MaxValue
        for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
                _firmDistance[y, x] = sbyte.MaxValue;

        // Second pass: enqueue all distance-0 firm sources
        _bfsHead = 0;
        for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
                if (_firmDistance[y, x] == 0)
                    _bfsQueue[_bfsHead++] = (x, y);

        // Third pass: BFS propagate outward
        for (var qi = 0; qi < _bfsHead; qi++)
        {
            var (cx, cy) = _bfsQueue[qi];
            var curDist = _firmDistance[cy, cx];

            // Four neighbours (4-neighbour road graph)
            var neighbours = new (int, int)[]
            {
                (cx, cy - 1),
                (cx, cy + 1),
                (cx - 1, cy),
                (cx + 1, cy),
            };

            foreach (var (nx, ny) in neighbours)
            {
                if (!(_grid.InBounds(nx, ny) && _grid.IsRoadAt(nx, ny))) continue;
                if (_firmDistance[ny, nx] <= curDist + 1) continue;
                _firmDistance[ny, nx] = (sbyte)(curDist + 1);
                _bfsQueue[_bfsHead++] = (nx, ny);
            }
        }

        // ---- BFS from all shop sources ----
        // Reset shop distances
        for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
                _shopDistance[y, x] = sbyte.MaxValue;

        _bfsHead = 0;
        for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
                if (_shopDistance[y, x] == 0)
                    _bfsQueue[_bfsHead++] = (x, y);

        for (var qi = 0; qi < _bfsHead; qi++)
        {
            var (cx, cy) = _bfsQueue[qi];
            var curDist = _shopDistance[cy, cx];

            var neighbours = new (int, int)[]
            {
                (cx, cy - 1),
                (cx, cy + 1),
                (cx - 1, cy),
                (cx + 1, cy),
            };

            foreach (var (nx, ny) in neighbours)
            {
                if (!(_grid.InBounds(nx, ny) && _grid.IsRoadAt(nx, ny))) continue;
                if (_shopDistance[ny, nx] <= curDist + 1) continue;
                _shopDistance[ny, nx] = (sbyte)(curDist + 1);
                _bfsQueue[_bfsHead++] = (nx, ny);
            }
        }

        // ---- Compose final land value per tile ----
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                if (!_grid.IsRoadAt(x, y))
                {
                    // Non-road tiles: low value, only amenity contribution
                    var val = (byte)Math.Clamp((int)(BaseValue - 20 + _amenityLevel[y, x]), 0, 100);
                    _valueGrid[x, y] = val;
                    continue;
                }

                // Compute proximity-based contributions from firms and shops
                // Distance is in ticks/steps; closer = higher value
                // Falloff: value drops by Falloff per distance unit, clamped to 0

                // Firm contribution: higher when closer
                var firmDist = _firmDistance[y, x];
                int firmContrib = 0;
                if (firmDist >= 0 && firmDist < sbyte.MaxValue)
                {
                    // Value decreases with distance; at distance 0 = FirmAtSource, falls off
                    var distClamped = Math.Clamp((int)firmDist, 0, FirmAtSource);
                    firmContrib = FirmAtSource - (distClamped * FirmDistanceFalloff);
                }
                // If firmDist is still MaxValue after BFS (no firm sources placed),
                // firmContrib stays 0

                // Shop contribution
                var shopDist = _shopDistance[y, x];
                int shopContrib = 0;
                if (shopDist >= 0 && shopDist < sbyte.MaxValue)
                {
                    var distClamped = Math.Clamp((int)shopDist, 0, ShopAtSource);
                    shopContrib = ShopAtSource - (distClamped * ShopDistanceFalloff);
                }

                // Pollution penalty (per tile, 0-100 scale)
                var pollution = _pollutionLevel[y, x];
                var pollutionPenalty = pollution * PollutionPenaltyPerLevel;

                // Amenity bonus (per tile, 0-100 scale)
                var amenity = _amenityLevel[y, x];
                var amenityBonus = amenity * AmenityBonusPerLevel;

                // Compose total value: base + firm + shop - pollution + amenity
                var total = BaseValue + firmContrib + shopContrib - pollutionPenalty + amenityBonus;
                _valueGrid[x, y] = (byte)Math.Clamp((int)total, 0, 100);
            }
        }
    }

    /// <summary>Land value at grid position (x, y), 0-100 inclusive.</summary>
    public byte ValueAt(int x, int y)
    {
        if (!_grid.InBounds(x, y)) return 0;
        return _valueGrid[x, y];
    }

    /// <summary>Convenience: read value from the pre-composed grid.</summary>
    public int this[int x, int y] => (int)ValueAt(x, y);
}