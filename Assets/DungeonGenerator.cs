using Astar;
using System;
using System.Collections.Generic;
using UnityEditor.U2D.Aseprite;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

public class DungeonGenerator : MonoBehaviour
{
    public Vector2Int mapSize;
    //public List<MapData> templateMaps;

    private int MinRoomSize = 5;
    private int MaxRoomSize;
    private int RoomCount;
    private int Padding = 1;

    private int templateChance = 3;// Range(0, templateChance) == 0
    private int mergeChance = 4;// Range(0, mergeChance) == 0

    TileType[,] map;

    List<RoomInfo> rooms = new List<RoomInfo>();
    List<Vector2Int> doors = new List<Vector2Int>();

    enum TileType
    {
        floor = 1,
        door = 2,
        corridor = 3,

        stone = 4,
        softWall = 5,
        wall = 20,

        empty = 0
    };

    struct RoomInfo
    {
        public RectInt RoomRect { get; private set; }
        public readonly Vector2Int center;

        public RoomInfo(RectInt roomRect)
        {
            RoomRect = roomRect;
            center = new Vector2Int((int)roomRect.center.x, (int)roomRect.center.y);
        }
        public RoomInfo(RectInt roomRect, Vector2Int center)
        {
            RoomRect = roomRect;
            this.center = center + roomRect.position;
        }
    }

    private void Initialize()
    {
        map = new TileType[mapSize.x, mapSize.y];
        rooms.Clear();
        doors.Clear();

        for (int x = 0; x < mapSize.x; x++)
        {
            for (int y = 0; y < mapSize.y; y++)
            {
                map[x, y] = TileType.stone;
            }
        }

        MaxRoomSize = (int)(Math.Min(mapSize.x, mapSize.y) / 3f);
        RoomCount = (int)(Math.Sqrt(mapSize.x * mapSize.y) / 4f);
    }

    /// <summary>
    /// Generate and Render New Map
    /// </summary>
    public void Generate()
    {
        Initialize();

        Dictionary<int, int> excludedPoints = new Dictionary<int, int>();

        int padConst = Padding * 2;
        int randomSize = (mapSize.x - padConst) * (mapSize.y - padConst);

        while (randomSize > 0 && rooms.Count < RoomCount)
        {
            int index = Random.Range(0, randomSize);

            while (excludedPoints.ContainsKey(index))
            {
                index = randomSize + excludedPoints[index];
            }

            Vector2Int point = Int2Vector2Int(index, mapSize.x - padConst) + new Vector2Int(Padding, Padding);

            RectInt? area = IdentifyArea(point);
            if (area == null)
            {
                excludedPoints.Add(index, excludedPoints.Count);
                randomSize--;
                continue;
            }

            //if (Random.Range(0, templateChance) == 0)
            //{
            //    MakeTemplateRoom((RectInt)area);
            //}
            //else
            //{
                MakeRectRoom((RectInt)area);
            //}
        }

        MakeTunnels();
        MergeRooms();

        MapRender();
    }

    private RectInt? IdentifyArea(Vector2Int point)
    {
        if (!IsValidTile(point, (x) => x != TileType.floor))
            return null;

        Stack<Vector2Int> stack = new Stack<Vector2Int>();
        Stack<Vector2Int> corners = new Stack<Vector2Int>();

        stack.Push(point);

        while (stack.Count > 0)
        {
            Vector2Int curr = stack.Pop();

            if (curr.y == point.y &&
                IsValidTile(curr + Vector2Int.right, (x) => x != TileType.floor))
            {
                stack.Push(curr + Vector2Int.right);
            }

            if (IsValidTile(curr + Vector2Int.up, (x) => x != TileType.floor) &&
               (corners.Count == 0 || curr.y < corners.Peek().y))
            {
                stack.Push(curr + Vector2Int.up);
            }
            else
            {
                if (corners.TryPeek(out Vector2Int value) && value.y <= curr.y)
                {
                    corners.Pop();
                }

                corners.Push(curr);
            }
        }

        List<RectInt> areas = new List<RectInt>();

        foreach (Vector2Int corner in corners)
        {
            Vector2Int size = new Vector2Int(corner.x - point.x, corner.y - point.y);
            if (size.x < MinRoomSize || size.y < MinRoomSize)
            {
                continue;
            }

            areas.Add(new RectInt(point, size));
        }

        return areas.Count == 0 ? null : areas[Random.Range(0, areas.Count)];
    }


    private void MakeRectRoom(RectInt area)
    {
        Vector2Int size = new Vector2Int(Random.Range(MinRoomSize, Math.Min(area.size.x, MaxRoomSize)), Random.Range(MinRoomSize, Math.Min(area.size.y, MaxRoomSize)));

        RectInt room = new RectInt(area.position, size);

        for (int x = room.xMin; x <= room.xMax; x++)
        {
            for (int y = room.yMin; y <= room.yMax; y++)
            {
                if (x != room.xMin && x != room.xMax &&
                    y != room.yMin && y != room.yMax)
                {
                    map[x, y] = TileType.floor;
                }
                else
                {
                    map[x, y] = Random.Range(0, 10) < 9 ? TileType.wall : TileType.softWall;
                }
            }
        }
        map[room.xMin, room.yMin] = TileType.wall;
        map[room.xMax, room.yMin] = TileType.wall;
        map[room.xMin, room.yMax] = TileType.wall;
        map[room.xMax, room.yMax] = TileType.wall;


        rooms.Add(new RoomInfo(room));
    }

    //private void MakeTemplateRoom(RectInt area)
    //{
    //    List<MapData> validTemplates = new List<MapData>();

    //    foreach (var template in templateMaps)
    //    {
    //        Vector2Int _size = template.Size;
    //        if (_size.x <= area.width && _size.y <= area.height)
    //        {
    //            validTemplates.Add(template);
    //        }
    //    }

    //    if (validTemplates.Count <= 0)
    //    {
    //        MakeRectRoom(area);
    //        return;
    //    }

    //    MapData selectedTemplate = validTemplates[Random.Range(0, validTemplates.Count)];

    //    Vector2Int selectedSize = selectedTemplate.Size;

    //    Vector2Int size = selectedTemplate.Size;

    //    RectInt room = new RectInt(area.position, size - new Vector2Int(1, 1));

    //    for (int x = 0; x < size.x; x++)
    //    {
    //        for (int y = 0; y < size.y; y++)
    //        {
    //            if (selectedTemplate.Map[x, y] != 0)
    //            {
    //                map[room.xMin + x, room.yMin + y] = (TileType)selectedTemplate.Map[x, y];
    //            }
    //        }
    //    }

    //    rooms.Add(new RoomInfo(room, selectedTemplate.Center));
    //}

    private void MakeTunnels()
    {
        for (int i = 0; i < rooms.Count - 1; i++)
        {
            Vector2Int start = rooms[i].center;
            Vector2Int end = rooms[i + 1].center;

            List<Vector2Int> path = PathFinder.FindPath(start, end, GetCost, (pos) => (IsValidTile(pos, (_) => true)), false);

            foreach (Vector2Int pos in path)
            {
                switch (map[pos.x, pos.y])
                {
                    case TileType.stone:
                        map[pos.x, pos.y] = TileType.corridor;
                        break;
                    case TileType.softWall:
                    case TileType.wall:
                        map[pos.x, pos.y] = TileType.door;
                        doors.Add(pos);
                        break;
                }
            }
        }
    }

    private int GetCost(Vector2Int position)
    {
        switch (map[position.x, position.y])
        {
            case TileType.floor:
            case TileType.door:
                return (int)TileType.floor;
            case TileType.stone:
            case TileType.softWall:
                return (int)TileType.stone;
            case TileType.wall:
                return (int)TileType.wall;
        }
        return 0;
    }
    private void MergeRooms()
    {
        foreach (Vector2Int door in doors)
        {
            int adjacentFloors = 0;

            foreach (Vector2Int dir in PathFinder.s_FourDirs)
            {
                Vector2Int adjacent = door + dir;
                if (map[adjacent.x, adjacent.y] == TileType.floor)
                {
                    adjacentFloors++;
                }
            }

            if (adjacentFloors < 2 || Random.Range(0, mergeChance) > 0)
                continue;

            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            queue.Enqueue(door);

            map[door.x, door.y] = TileType.floor;

            while (queue.Count > 0)
            {
                Vector2Int curr = queue.Dequeue();

                List<Vector2Int> expandables = new List<Vector2Int>();
                int floorCount = 0;

                foreach (Vector2Int dir in PathFinder.s_FourDirs)
                {
                    Vector2Int next = curr + dir;

                    if (IsValidTile(next, IsExpandable))
                        expandables.Add(next);

                    if (IsValidTile(next, (x) => x == TileType.floor))
                        floorCount++;
                }

                if (floorCount < 3 && curr != door)
                    continue;

                map[curr.x, curr.y] = TileType.floor;

                foreach (Vector2Int expandable in expandables)
                {
                    queue.Enqueue(expandable);
                }
            }
        }
    }

    private bool IsExpandable(TileType tileType)
    {
        switch (tileType)
        {
            case TileType.door:
            case TileType.softWall:
            case TileType.wall:
                return true;
        }
        return false;
    }

    /// <summary>
    /// Checks whether the given position is within the valid map bounds and
    /// whether the tile at that position satisfies a custom comparison condition against the specified type.
    /// </summary>
    /// <param name="pos">The tile position to check.</param>
    /// <param name="requiredType">The expected tile type to compare against.</param>
    /// <param name="comp">A comparison function that takes the tile's type and the required type, and returns true if valid.</param>
    /// <returns>True if the position is valid and the comparison condition is met; otherwise, false.</returns>
    bool IsValidTile(Vector2Int pos, Func<TileType, bool> comp)
    {
        if (pos.x < Padding || pos.y < Padding || pos.x >= mapSize.x - Padding || pos.y >= mapSize.y - Padding)
            return false;

        return comp(map[pos.x, pos.y]);
    }


    public Vector2Int Int2Vector2Int(int index, int gridWidth)
    {
        int x = index % gridWidth;
        int y = index / gridWidth;

        return new Vector2Int(x, y);
    }

    public Tilemap floorTilemap;
    public Tilemap wallTilemap;

    public TileBase floorTile;
    public TileBase wallTile;

    public void MapRender()
    {
        floorTilemap.ClearAllTiles();
        wallTilemap.ClearAllTiles();

        for (int x = 0; x < mapSize.x; x++)
        {
            for (int y = 0; y < mapSize.y; y++)
            {
                Vector3Int tilePos = new Vector3Int(x, y, 0);
                TileType type = map[x, y];

                floorTilemap.SetTile(tilePos, floorTile);
                switch (type)
                {
                    case TileType.stone:
                    case TileType.softWall:
                    case TileType.wall:
                        wallTilemap.SetTile(tilePos, wallTile);
                        break;
                }
            }
        }
    }
}