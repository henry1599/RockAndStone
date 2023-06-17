using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using System;

namespace UnknownGame.Managers
{
    public enum eNeighbourScaningType
    {
        None = 0,
        TopDownLeftRight,
        Diagonal,
        All
    }
    public class MapGenerator : MonoBehaviour
    {
        [Header("Setting Rules")]
        public Vector3 MapWorldPosition;
        [Tooltip("Width of the map in top-down view (x-axis)")]
        public int Width;
        [Tooltip("Height of the map in top-down view (z-axis)")]
        public int Height;
        public float CellSize = 1;
        public float WallHeight = 5;
        [Tooltip("A wall contains less than this amount of blocks will be removed")]
        public int WallThreshold = 10;
        [Tooltip("A room contains less than this amount of spaces will be removed")]
        public int RoomThreshold = 10;
        public bool IsRandomPassageWayRadius;
        public int PassageWayRadius;
        public int BorderSize;
        [ShowIf("IsRandomPassageWayRadius")] public int MinPassageWayRadius;
        [ShowIf("IsRandomPassageWayRadius")] public int MaxPassageWayRadius;
        public string Seed;
        public bool IsRandomSeed;
        [Range(0, 100)]
        public int RandomFillPercentage;
        [Tooltip("How many times the smooth function is called")]
        public int SmoothCycle = 5;
        [Range(0, 8)]
        [Tooltip("How many neighbour cells surrounding current cell to be considered as a smooth")]
        public int NeighborAllowed = 4;
        [Tooltip("How to scan neighbours")]
        public eNeighbourScaningType NeighbourScaningType = eNeighbourScaningType.All;
        [SerializeField] MeshGenerator meshGenerator;
        int [,] map;
        int finalPassageWayRadius;
        void Start()
        {
            GenerateMap();
        }
        [Button]
        void GenerateMap()
        {
            map = new int[Width, Height];
            RandomFillMap();
            for (int i = 0; i < SmoothCycle; i++)
            {
                SmoothMap();
            }

            this.finalPassageWayRadius = IsRandomPassageWayRadius ? UnityEngine.Random.Range(MinPassageWayRadius, MaxPassageWayRadius) : PassageWayRadius;

            ClearIsolatedWalls();
            ClearIsolatedRooms();
            var rooms = CreateListOfRooms();
            ConnectClosestRoom(rooms);

            int[,] borderedMap = new int[Width + BorderSize * 2, Height + BorderSize * 2];
            int borderCountX = borderedMap.GetLength(0);
            int borderCountY = borderedMap.GetLength(1);

            // * Set all value in bordererMap the same as map
            // * except the border
            for (int x = 0; x < borderCountX; x++)
            {
                for (int y = 0; y < borderCountY; y++)
                {
                    // * Current pos is inside the map
                    if (x >= BorderSize && x < Width + BorderSize && y >= BorderSize && y < Height + BorderSize)
                    {
                        borderedMap[x, y] = map[x - BorderSize, y - BorderSize];
                    }
                    else
                    {
                        borderedMap[x, y] = 1;
                    }
                }
            }

            this.meshGenerator.GenerateMesh(borderedMap, CellSize, MapWorldPosition, WallHeight);
        }
        void ClearIsolatedWalls()
        {
            // * Get all wall regions
            List<List<Coord>> wallRegions = GetRegions(1);
            // * Wall block that is made out of less than this number will be clear in the map
            foreach (var wallRegion in wallRegions)
            {
                if (wallRegion.Count < WallThreshold)
                {
                    foreach (var tile in wallRegion)
                    {
                        map[tile.TileX, tile.TileY] = 0;
                    }
                }
            }
        }
        void ClearIsolatedRooms()
        {
            // * Get all wall regions
            List<List<Coord>> roomRegions = GetRegions(0);
            // * Wall block that is made out of less than this number will be clear in the map
            foreach (var roomRegion in roomRegions)
            {
                if (roomRegion.Count < RoomThreshold)
                {
                    foreach (var tile in roomRegion)
                    {
                        map[tile.TileX, tile.TileY] = 1;
                    }
                }
            }
        }
        List<Room> CreateListOfRooms()
        {
            List<List<Coord>> roomRegions = GetRegions(0);
            List<Room> rooms = new();
            foreach (var roomRegion in roomRegions)
            {
                rooms.Add(new(roomRegion, map));
            }
            if (rooms.Count > 0)
            {
                rooms.Sort();
                rooms[0].IsMainRoom = rooms[0].IsAccessibleFromMainRoom = true;
            }
            return rooms;
        }
        void ConnectClosestRoom(List<Room> rooms, bool forceAccessibilityFromMainRoom = false)
        {
            List<Room> roomListA = new();
            List<Room> roomListB = new();
            if (forceAccessibilityFromMainRoom)
            {
                foreach (var room in rooms)
                {
                    if (room.IsAccessibleFromMainRoom)
                    {
                        roomListB.Add(room);
                    }
                    else
                    {
                        roomListA.Add(room);
                    }
                }
            }
            else
            {
                roomListA = rooms;
                roomListB = rooms;
            }

            int shortestDistance = 0;
            Coord shorestTileA = new();
            Coord shorestTileB = new();
            Room shorestRoomA = new();
            Room shorestRoomB = new();
            bool possibleConnectionFound = false;

            foreach (var roomA in roomListA)
            {
                if (!forceAccessibilityFromMainRoom)
                {
                    possibleConnectionFound = false;
                    if (roomA.ConnectedRooms.Count > 0)
                    {
                        continue;
                    }
                }
                foreach (var roomB in roomListB)
                {
                    if (roomA == roomB || roomA.IsConnected(roomB))
                        continue;
                    
                    for (int tileIndexA = 0; tileIndexA < roomA.EdgeTiles.Count; tileIndexA++)
                    {
                        for (int tileIndexB = 0; tileIndexB < roomB.EdgeTiles.Count; tileIndexB++)
                        {
                            Coord tileA = roomA.EdgeTiles[tileIndexA];
                            Coord tileB = roomB.EdgeTiles[tileIndexB];
                            int distanceBtwRooms = (tileA.TileX - tileB.TileX) * (tileA.TileX - tileB.TileX) + (tileA.TileY - tileB.TileY) * (tileA.TileY - tileB.TileY);
                            if (distanceBtwRooms < shortestDistance || !possibleConnectionFound)
                            {
                                shortestDistance = distanceBtwRooms;
                                possibleConnectionFound = true;
                                shorestTileA = tileA;
                                shorestTileB = tileB;
                                shorestRoomA = roomA;
                                shorestRoomB = roomB;
                            }
                        }
                    }
                }
                if (possibleConnectionFound && !forceAccessibilityFromMainRoom)
                {
                    CreatePassage(shorestRoomA, shorestRoomB, shorestTileA, shorestTileB);
                }
            }
            if (possibleConnectionFound && forceAccessibilityFromMainRoom)
            {
                CreatePassage(shorestRoomA, shorestRoomB, shorestTileA, shorestTileB);
                ConnectClosestRoom(rooms, true);
            }
            if (!forceAccessibilityFromMainRoom)
            {
                ConnectClosestRoom(rooms, true);
            }
        }
        void CreatePassage(Room roomA, Room roomB, Coord tileA, Coord tileB)
        {
            Room.ConnectRooms(roomA, roomB);
            List<Coord> line = GetLine(tileA, tileB);
            foreach (var tile in line)
            {
                DrawCircle(tile, this.finalPassageWayRadius);
            }
        }
        void DrawCircle(Coord c, int r)
        {
            for (int x = -r; x <= r; x++)
            {
                for (int y = -r; y <= r; y++)
                {
                    // * if the point is in the circle
                    if (x * x + y * y <= r * r)
                    {
                        int realX = c.TileX + x;
                        int realY = c.TileY + y;
                        // * If this inside map
                        if (IsInMapRange(realX, realY))
                        {
                            map[realX, realY] = 0;
                        }
                    }
                }
            }
        }
        // * Return list of coord that a line from-to pass
        List<Coord> GetLine(Coord from, Coord to)
        {
            List<Coord> line = new();

            int x = from.TileX;
            int y = from.TileY;

            int dx = to.TileX - from.TileX;
            int dy = to.TileY - from.TileY;

            bool inverted = false;
            int step = Math.Sign(dx);
            int gradientStep = Math.Sign(dy);
            int longest = Mathf.Abs(dx);
            int shorest = Mathf.Abs(dy);
            if (longest < shorest)
            {
                inverted = true;
                longest = Mathf.Abs(dy);
                shorest = Mathf.Abs(dx);
                step = Math.Sign(dy);
                gradientStep = Math.Sign(dx);
            }
            int gradientAccumulation = longest / 2;
            for (int i = 0; i < longest; i++)
            {
                line.Add(new(x, y));
                if (inverted)
                {
                    y += step;
                }
                else
                {
                    x += step;
                }
                gradientAccumulation += shorest;
                if (gradientAccumulation >= longest)
                {
                    if (inverted)
                    {
                        x += gradientStep;
                    }
                    else
                    {
                        y += gradientStep;
                    }
                    gradientAccumulation -= longest;
                }
            }
            return line;
        }
        Vector3 CoordToWorldPoint(Coord tile)
        {
            return new Vector3(-Width / 2 + tile.TileX + MapWorldPosition.x, 2 + MapWorldPosition.y, -Height / 2 + tile.TileY + MapWorldPosition.z) * CellSize;
        }
        // * Get all regions for each tileType
        List<List<Coord>> GetRegions(int tileType)
        {
            List<List<Coord>> regions = new();
            int[,] mapFlags = new int[Width, Height];
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (mapFlags[x, y] == 0 && map[x, y] == tileType)
                    {
                        List<Coord> newRegion = GetRegionTiles(x, y);
                        regions.Add(newRegion);
                        foreach (var tile in newRegion)
                        {
                            mapFlags[tile.TileX, tile.TileY] = 1;
                        }
                    }
                }
            }
            return regions;
        }
        // * Get list of tiles that have the same type as map[startX, startY] using BFS
        List<Coord> GetRegionTiles(int startX, int startY)
        {
            List<Coord> tiles = new();
            int[,] mapFlags = new int[Width, Height];
            int tileType = map[startX, startY];

            Queue<Coord> queue = new();
            queue.Enqueue(new(startX, startY));
            mapFlags[startX, startY] = 1;

            while (queue.Count > 0)
            {
                Coord tile = queue.Dequeue();
                tiles.Add(tile);
                for (int x = tile.TileX - 1; x <= tile.TileX + 1; x++)
                {
                    for (int y = tile.TileY - 1; y <= tile.TileY + 1; y++)
                    {
                        // * Get the plus neighbor tiles
                        if (IsInMapRange(x, y) && (x == tile.TileX || y == tile.TileY))
                        {
                            if (mapFlags[x, y] == 0 && map[x, y] == tileType)
                            {
                                mapFlags[x, y] = 1;
                                queue.Enqueue(new(x, y));
                            }
                        }
                    }
                }
            }
            return tiles;
        }

        bool IsInMapRange(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }
        void RandomFillMap()
        {
            if (IsRandomSeed)
            {
                Seed = Time.time.ToString();
            }
            System.Random pseudoRandom = new System.Random(Seed.GetHashCode());
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    // * Make the border of the grid always return a WALL (1)
                    if (x == 0 || x == Width - 1 || y == 0 || y == Height - 1)
                    {
                        map[x, y] = 1;
                    }
                    else
                    {
                        map[x, y] = (pseudoRandom.Next(0, 100) < RandomFillPercentage) ? 1 : 0;
                    }
                }
            }
        }
        void SmoothMap()
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    int neighborWalLTiles = GetSurroundingWallCount(x, y, NeighbourScaningType);
                    if (neighborWalLTiles > NeighborAllowed)
                    {
                        map[x, y] = 1;
                    }
                    else if (neighborWalLTiles < NeighborAllowed)
                    {
                        map[x, y] = 0;
                    }
                }
            }
        }
        // * Looking for 8 surrounding grid
        int GetSurroundingWallCount(int gridX, int gridY, eNeighbourScaningType scanType)
        {
            int wallCount = 0;
            List<Vector2Int> notScanCells = new() {new(gridX, gridY)};
            bool isScan = false;
            switch (scanType)
            {
                case eNeighbourScaningType.None:
                default:
                    break;
                case eNeighbourScaningType.TopDownLeftRight:
                    notScanCells.Add(new(gridX - 1, gridY - 1));
                    notScanCells.Add(new(gridX + 1, gridY - 1));
                    notScanCells.Add(new(gridX - 1, gridY + 1));
                    notScanCells.Add(new(gridX + 1, gridY + 1));
                    break;
                case eNeighbourScaningType.Diagonal:
                    notScanCells.Add(new(gridX, gridY - 1));
                    notScanCells.Add(new(gridX, gridY + 1));
                    notScanCells.Add(new(gridX - 1, gridY));
                    notScanCells.Add(new(gridX + 1, gridY));
                    break;
                case eNeighbourScaningType.All:
                    isScan = true;
                    break;
            }
            if (!isScan)
                return wallCount;
            for (int neighborX = gridX - 1; neighborX <= gridX + 1; neighborX++)
            {
                for (int neighborY = gridY - 1; neighborY <= gridY + 1; neighborY++)
                {
                    if (!IsInMapRange(neighborX, neighborY))
                    {
                        wallCount ++;
                        continue;
                    }
                    if (notScanCells.Contains(new(neighborX, neighborY)))
                    {
                        continue;
                    }
                    wallCount += map[neighborX, neighborY];
                }
            }
            return wallCount;
        }
        public struct Coord
        {
            public int TileX;
            public int TileY;
            public Coord(int x, int y)
            {
                this.TileX = x;
                this.TileY = y;
            }
        }
        public class Room : IComparable<Room>
        {
            public List<Coord> Tiles;
            public List<Coord> EdgeTiles;
            public List<Room> ConnectedRooms;
            public int RoomSize;
            public bool IsAccessibleFromMainRoom;
            public bool IsMainRoom;
            public Room() {}
            public Room(List<Coord> roomTiles, int[,] map)
            {
                this.Tiles = roomTiles;
                this.RoomSize = this.Tiles.Count;
                this.ConnectedRooms = new();
                this.EdgeTiles = new();
                foreach (var tile in this.Tiles)
                {
                    for (int x = tile.TileX - 1; x <= tile.TileX + 1; x++)
                    {
                        for (int y = tile.TileY - 1; y <= tile.TileY + 1; y++)
                        {
                            // * Only scan plus neighbor
                            if (x == tile.TileX || y == tile.TileY)
                            {
                                if (map[x, y] == 1)
                                {
                                    this.EdgeTiles.Add(tile);
                                }
                            }
                        }
                    }
                }
            }
            public void SetAccessibleFromMainRoom()
            {
                if (!this.IsAccessibleFromMainRoom)
                {
                    this.IsAccessibleFromMainRoom = true;
                    foreach (var room in this.ConnectedRooms)
                    {
                        room.SetAccessibleFromMainRoom();
                    }
                }
            }
            public static void ConnectRooms(Room roomA, Room roomB)
            {
                if (roomA.IsAccessibleFromMainRoom)
                {
                    roomB.SetAccessibleFromMainRoom();
                }
                else if (roomB.IsAccessibleFromMainRoom)
                {
                    roomA.SetAccessibleFromMainRoom();
                }
                roomA.ConnectedRooms.Add(roomB);
                roomB.ConnectedRooms.Add(roomA);
            }

            public int CompareTo(Room other)
            {
                return other.RoomSize.CompareTo(this.RoomSize);
            }

            public bool IsConnected(Room otherRoom)
            {
                return this.ConnectedRooms.Contains(otherRoom);
            }
        }
    }
}
