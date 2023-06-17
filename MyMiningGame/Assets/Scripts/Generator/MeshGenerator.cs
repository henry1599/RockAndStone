using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnknownGame.Managers
{
    public class MeshGenerator : MonoBehaviour
    {
        public SquareGrid Grid;
        public MeshFilter WallsMeshFilter;
        public MeshFilter CellMeshFilter;
        public Material mapMat;
        List<Vector3> ceilVertices;
        List<int> ceilTriangles;
        List<Vector3> wallVertices;
        List<int> wallTriangles;
        Dictionary<int, List<Triangle>> triangleDict = new();
        List<List<int>> outlines = new();
        HashSet<int> checkedVertices = new();
        private List<GameObject> meshes = new List<GameObject>();
        float chunkSize = 10f;
        int[,] map;
        float squareSize;
        Mesh ceilMesh, wallMesh;
        public void GenerateMesh(int[,] map, float squareSize, Vector3 worldPosition, float wallHeight)
        {
            this.map = map;
            this.squareSize = squareSize;
            this.triangleDict.Clear();
            this.outlines.Clear();
            this.checkedVertices.Clear();

            // * Make a grid of squares (each square contains 4 controlnodes and 4 sidenodes)
            Grid = new(map, squareSize, worldPosition);
            CreateCeilMesh();
            CreateWallMesh(wallHeight);
            var finalMesh = CombineMeshes(this.CellMeshFilter, this.WallsMeshFilter);
        }

        void CreateCeilMesh()
        {
            this.ceilVertices = new();
            this.ceilTriangles = new();
            for (int x = 0; x < Grid.Squares.GetLength(0); x++)
            {
                for (int y = 0; y < Grid.Squares.GetLength(1); y++)
                {
                    // * Triangulate each square
                    // * Add vertices into vertices list
                    // * Add triangles into triangles list
                    TriangualateSquare(Grid.Squares[x, y]);
                }
            }

            // * Then make a mesh with vertices and triangles information
            this.ceilMesh = new Mesh();
            this.CellMeshFilter.mesh = this.ceilMesh;
            this.ceilMesh.vertices = this.ceilVertices.ToArray();
            this.ceilMesh.triangles = this.ceilTriangles.ToArray();
            this.ceilMesh.RecalculateNormals();
        }
        void CreateWallMesh(float wallHeight)
        {
            CalculateMeshOutlines();
            wallVertices = new();
            wallTriangles = new();

            this.wallMesh = new();
            foreach (var outline in this.outlines)
            {
                for (int i = 0; i < outline.Count - 1; i++)
                {
                    int startIndex = wallVertices.Count;
                    wallVertices.Add(this.ceilVertices[outline[i]]); // top Left -> 0
                    wallVertices.Add(this.ceilVertices[outline[i + 1]]); // top Right -> 1
                    wallVertices.Add(this.ceilVertices[outline[i]] - Vector3.up * wallHeight); // bottom left -> 2
                    wallVertices.Add(this.ceilVertices[outline[i + 1]] - Vector3.up * wallHeight); // bottom right -> 3


                    // * Make 2 triangles which form a rectangle
                    // * but vertices lie in an anti-clockwise to face them inside
                    wallTriangles.Add(startIndex + 0);
                    wallTriangles.Add(startIndex + 2);
                    wallTriangles.Add(startIndex + 3);

                    wallTriangles.Add(startIndex + 3);
                    wallTriangles.Add(startIndex + 1);
                    wallTriangles.Add(startIndex + 0);
                }
            }
            WallsMeshFilter.mesh = this.wallMesh;
            this.wallMesh.vertices = wallVertices.ToArray();
            this.wallMesh.triangles = wallTriangles.ToArray();
            this.wallMesh.RecalculateNormals();

            // int tileAmount = 10;
            // Vector2[] uvs = new Vector2[wallVertices.Count];
            // for (int i = 0; i < wallVertices.Count; i++)
            // {
            //     float percentX = 0.5f * tileAmount;
            //     float percentY = 0.5f * tileAmount;
            //     uvs[i] = new(percentX, percentY);
            // }
            // WallsMeshFilter.mesh.uv = uvs;

            MeshCollider wallCollider = WallsMeshFilter.gameObject.GetComponent<MeshCollider>();
            if (wallCollider == null)
            {
                wallCollider = WallsMeshFilter.gameObject.AddComponent<MeshCollider>();
            }
            else
            {
                Destroy(wallCollider);
                wallCollider = WallsMeshFilter.gameObject.AddComponent<MeshCollider>();
            }
            wallCollider.sharedMesh = this.wallMesh;
        }
        Mesh CombineMeshes(params MeshFilter[] meshFilters)
        {
            // Create a new combined Mesh
            Mesh combinedMesh = new Mesh();

            // Prepare CombineInstance array
            CombineInstance[] combineInstances = new CombineInstance[meshFilters.Length];

            for (int i = 0; i < meshFilters.Length; i++)
            {
                MeshFilter meshFilter = meshFilters[i];

                // Get the Mesh from the MeshFilter
                Mesh mesh = meshFilter.sharedMesh;

                // Create a new CombineInstance
                combineInstances[i].mesh = mesh;
                combineInstances[i].transform = meshFilter.transform.localToWorldMatrix;

                // Optionally, you may want to destroy the original GameObjects if you no longer need them
                Destroy(meshFilter.gameObject);
            }

            // Combine the meshes
            combinedMesh.CombineMeshes(combineInstances);

            // Create a new GameObject to hold the combined mesh
            GameObject combinedGameObject = new GameObject("CombinedMesh");
            combinedGameObject.transform.SetParent(transform);
            MeshFilter combinedMeshFilter = combinedGameObject.AddComponent<MeshFilter>();
            MeshRenderer combinedMeshRenderer = combinedGameObject.AddComponent<MeshRenderer>();

            combinedMeshFilter.sharedMesh = combinedMesh;
            combinedMeshRenderer.material = this.mapMat;
            return combinedMesh;
            // Set any additional properties or materials for the combined MeshRenderer as needed

            // var combinedMeshVertices = combinedMesh.vertices;
            // int tileAmount = 10;
            // Vector2[] uvs = new Vector2[this.ceilVertices.Count];
            // for (int i = 0; i < this.ceilVertices.Count; i++)
            // {
            //     float percentX = Mathf.InverseLerp(- map.GetLength(0) / 2 * squareSize, map.GetLength(0) / 2 * squareSize, combinedMeshVertices[i].x) * tileAmount;
            //     float percentY = Mathf.InverseLerp(- map.GetLength(0) / 2 * squareSize, map.GetLength(0) / 2 * squareSize, combinedMeshVertices[i].z) * tileAmount;
            //     uvs[i] = new(percentX, percentY);
            // }
            // combinedMesh.uv = uvs;
        }
        void SplitMeshes(Mesh mesh)
        {
            
            // create a mesh splitter with some parameters (see MeshSplitParameters.cs for default settings)
            // var meshSplitter = new MeshSplitter(new MeshSplitParameters
            // {
            //     GridSize = 32,
            //     GenerateColliders = true
            // });

            // // split mesh into submeshes assigned to points
            // var subMeshes = meshSplitter.Split(mesh);
        }
        void TriangualateSquare(Square square)
        {
            switch (square.Configuration)
            {
                default:
                case 0:
                    break;

                #region 1 Point
                case 1:
                    MeshFromPoints(square.CenterLeft, square.CenterBottom, square.BottomLeft); //
                    break;
                case 2:
                    MeshFromPoints(square.BottomRight, square.CenterBottom, square.CenterRight); //
                    break;
                case 4:
                    MeshFromPoints(square.TopRight, square.CenterRight, square.CenterTop); //
                    break;
                case 8:
                    MeshFromPoints(square.TopLeft, square.CenterTop, square.CenterLeft); //
                    break;
                #endregion

                #region 2 Points
                case 3:
                    MeshFromPoints(square.CenterRight, square.BottomRight, square.BottomLeft, square.CenterLeft);
                    break;
                case 5:
                    MeshFromPoints(square.CenterTop, square.TopRight, square.CenterRight, square.CenterBottom, square.BottomLeft, square.CenterLeft);
                    break;
                case 6:
                    MeshFromPoints(square.CenterTop, square.TopRight, square.BottomRight, square.CenterBottom);
                    break;
                case 9:
                    MeshFromPoints(square.TopLeft, square.CenterTop, square.CenterBottom, square.BottomLeft);
                    break;
                case 10:
                    MeshFromPoints(square.TopLeft, square.CenterTop, square.CenterRight, square.BottomRight, square.CenterBottom, square.CenterLeft);
                    break;
                case 12:
                    MeshFromPoints(square.TopLeft, square.TopRight, square.CenterRight, square.CenterLeft);
                    break;
                #endregion

                #region 3 Points
                case 7:
                    MeshFromPoints(square.CenterTop, square.TopRight, square.BottomRight, square.BottomLeft, square.CenterLeft);
                    break;
                case 11:
                    MeshFromPoints(square.TopLeft, square.CenterTop, square.CenterRight, square.BottomRight, square.BottomLeft);
                    break;
                case 13:
                    MeshFromPoints(square.TopLeft, square.TopRight, square.CenterRight, square.CenterBottom, square.BottomLeft);
                    break;
                case 14:
                    MeshFromPoints(square.TopLeft, square.TopRight, square.BottomRight, square.CenterBottom, square.CenterLeft);
                    break;
                #endregion

                #region 4 Points
                case 15:
                    MeshFromPoints(square.TopLeft, square.TopRight, square.BottomRight, square.BottomLeft);
                    // * All the vertices is covered, so they are not outline vertices at all cases
                    this.checkedVertices.Add(square.TopLeft.VertexIndex);
                    this.checkedVertices.Add(square.TopRight.VertexIndex);
                    this.checkedVertices.Add(square.BottomRight.VertexIndex);
                    this.checkedVertices.Add(square.BottomLeft.VertexIndex);
                    break;
                    #endregion
            }
        }
        void MeshFromPoints(params Node[] points)
        {
            AssignVertices(points);
            if (points.Length >= 3)
                CreateTriangle(points[0], points[1], points[2]);
            if (points.Length >= 4)
                CreateTriangle(points[0], points[2], points[3]);
            if (points.Length >= 5)
                CreateTriangle(points[0], points[3], points[4]);
            if (points.Length >= 6)
                CreateTriangle(points[0], points[4], points[5]);
        }
        void AssignVertices(Node[] points)
        {
            for (int i = 0; i < points.Length; i++)
            {
                if (points[i].VertexIndex == -1)
                {
                    points[i].VertexIndex = this.ceilVertices.Count;
                    this.ceilVertices.Add(points[i].Position);
                }
            }
        }
        void CreateTriangle(Node a, Node b, Node c)
        {
            this.ceilTriangles.Add(a.VertexIndex);
            this.ceilTriangles.Add(b.VertexIndex);
            this.ceilTriangles.Add(c.VertexIndex);

            Triangle triangle = new(a.VertexIndex, b.VertexIndex, c.VertexIndex);
            AddTriangleToDictionary(triangle.VertexIndexA, triangle);
            AddTriangleToDictionary(triangle.VertexIndexB, triangle);
            AddTriangleToDictionary(triangle.VertexIndexC, triangle);
        }
        void AddTriangleToDictionary(int vertexIndex, Triangle triangle)
        {
            if (this.triangleDict.ContainsKey(vertexIndex))
            {
                this.triangleDict[vertexIndex].Add(triangle);
            }
            else
            {
                List<Triangle> triangles = new();
                triangles.Add(triangle);
                this.triangleDict.Add(vertexIndex, triangles);
            }
        }
        int GetConnectedOutlineVertex(int vertexIndex)
        {
            List<Triangle> trianglesContainingVertex = this.triangleDict[vertexIndex];
            for (int i = 0; i < trianglesContainingVertex.Count; i++)
            {
                Triangle triangle = trianglesContainingVertex[i];
                for (int j = 0; j < 3; j++)
                {
                    int vertexB = triangle[j];
                    if (vertexIndex != vertexB && !this.checkedVertices.Contains(vertexB))
                    {
                        if (IsOutlineEdge(vertexIndex, vertexB))
                        {
                            return vertexB;
                        }
                    }
                }
            }
            return -1;
        }
        bool IsOutlineEdge(int vertexA, int vertexB)
        {
            List<Triangle> triangleContainingVertexA = this.triangleDict[vertexA];
            int sharedTriangleCount = 0;
            for (int i = 0; i < triangleContainingVertexA.Count; i++)
            {
                if (triangleContainingVertexA[i].Contains(vertexB))
                {
                    sharedTriangleCount++;
                    if (sharedTriangleCount > 1)
                        break;
                }
            }
            return sharedTriangleCount == 1;
        }
        void CalculateMeshOutlines()
        {
            for (int vertexIndex = 0; vertexIndex < this.ceilVertices.Count; vertexIndex++)
            {
                if (!this.checkedVertices.Contains(vertexIndex))
                {
                    // * Find A new outline vertex from this vertexIndex
                    int newOutlineVertex = GetConnectedOutlineVertex(vertexIndex);
                    // * If it's an outline Vertex
                    if (newOutlineVertex != -1)
                    {
                        // * Mark vertexIndex as checked
                        this.checkedVertices.Add(vertexIndex);
                        // * Create a new outline list with 1st ele is vertexIndex
                        List<int> newOutline = new();
                        newOutline.Add(vertexIndex);
                        // * Add it into the outlines list
                        this.outlines.Add(newOutline);

                        // * Recursively follow all outline vertices until it reaches right before the vertex index (a close outline)
                        FollowOutline(newOutlineVertex, this.outlines.Count - 1);

                        // * Add another vertexIndex at the end of the list of outline to make the outline close
                        this.outlines[this.outlines.Count - 1].Add(vertexIndex);
                    }
                }
            }
        }

        private void FollowOutline(int vertexIndex, int outlineIndex)
        {
            this.outlines[outlineIndex].Add(vertexIndex);
            this.checkedVertices.Add(vertexIndex);
            int nextVertex = GetConnectedOutlineVertex(vertexIndex);
            if (nextVertex != -1)
            {
                FollowOutline(nextVertex, outlineIndex);
            }
        }

        // * 2D array of square
        public class SquareGrid
        {
            public Square[,] Squares;
            public SquareGrid(int[,] map, float squareSize, Vector3 worldPosition)
            {
                int nodeCountX = map.GetLength(0);
                int nodeCountY = map.GetLength(1);

                float mapWidth = nodeCountX * squareSize;
                float mapHeight = nodeCountY * squareSize;

                ControlNode[,] controlNodes = new ControlNode[nodeCountX, nodeCountY];
                for (int x = 0; x < nodeCountX; x++)
                {
                    for (int y = 0; y < nodeCountY; y++)
                    {
                        Vector3 position = new(-mapWidth / 2 + x * squareSize + squareSize / 2f + worldPosition.x, worldPosition.y, -mapHeight / 2 + y * squareSize + squareSize / 2 + worldPosition.z);
                        controlNodes[x, y] = new(position, map[x, y] == 1, squareSize);
                    }
                }
                Squares = new Square[nodeCountX - 1, nodeCountY - 1];
                // * Since a square contains 4 control nodes
                for (int x = 0; x < nodeCountX - 1; x++)
                {
                    for (int y = 0; y < nodeCountY - 1; y++)
                    {
                        Squares[x, y] = new(controlNodes[x, y + 1], controlNodes[x + 1, y + 1], controlNodes[x, y], controlNodes[x + 1, y]);
                    }
                }
            }
        }
        public class Triangle
        {
            public int VertexIndexA;
            public int VertexIndexB;
            public int VertexIndexC;
            int[] vertices;
            public int this[int i]
            {
                get => this.vertices[i];
            }
            public Triangle(int vertexIndexA, int vertexIndexB, int vertexIndexC)
            {
                this.VertexIndexA = vertexIndexA;
                this.VertexIndexB = vertexIndexB;
                this.VertexIndexC = vertexIndexC;

                this.vertices = new int[3]
                {
                    this.VertexIndexA,
                    this.VertexIndexB,
                    this.VertexIndexC
                };
            }
            public bool Contains(int vertexIndex)
            {
                return this.VertexIndexA == vertexIndex || this.VertexIndexB == vertexIndex || this.VertexIndexC == vertexIndex;
            }
        }
        public class Square
        {
            public ControlNode TopLeft, TopRight, BottomLeft, BottomRight;
            public Node CenterTop, CenterLeft, CenterBottom, CenterRight;
            public int Configuration; // * 0 -> 16
            public Square(ControlNode topLeft, ControlNode topRight, ControlNode bottomLeft, ControlNode bottomRight)
            {
                this.TopLeft = topLeft;
                this.TopRight = topRight;
                this.BottomLeft = bottomLeft;
                this.BottomRight = bottomRight;

                this.CenterTop = this.TopLeft.RightNode;
                this.CenterLeft = this.BottomLeft.AboveNode;
                this.CenterRight = this.BottomRight.AboveNode;
                this.CenterBottom = this.BottomLeft.RightNode;

                // * 1-------2
                // * +       +
                // * +       +
                // * +       +
                // * +       +
                // * +       +
                // * 4-------3

                // * 1 2 3 4
                // * 0 0 0 0 = 0
                if (TopLeft.Active)
                    Configuration += 8;
                if (TopRight.Active)
                    Configuration += 4;
                if (BottomRight.Active)
                    Configuration += 2;
                if (BottomLeft.Active)
                    Configuration += 1;
            }
        }
        public class Node
        {
            public Vector3 Position;
            public int VertexIndex = -1;
            public Node(Vector3 position)
            {
                this.Position = position;
            }
        }
        // * Is the node at 4 corner node of a square (ABCD)
        // * Square is in surface xOz
        // * +-------+
        // * +       +
        // * +       +
        // * A       +
        // * +       +
        // * +       +
        // * C---R---+
        public class ControlNode : Node
        {
            public bool Active;
            public Node AboveNode, RightNode;
            public ControlNode(Vector3 position, bool active, float squareSize) : base(position)
            {
                this.Active = active;
                this.AboveNode = new(position + Vector3.forward * squareSize / 2f);
                this.RightNode = new(position + Vector3.right * squareSize / 2f);
            }
        }
    }
}
