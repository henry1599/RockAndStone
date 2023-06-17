using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MeshSplit.Scripts;
using System.Linq;

namespace UnknownGame.Managers
{
    public class MeshGenerator : MonoBehaviour
    {
        public SquareGrid Grid;
        public MeshSplitParameters Parameters;
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
        private GameObject combinedMeshObject;
        private List<GameObject> meshes = new List<GameObject>();
        float chunkSize = 10f;
        int[,] map;
        float squareSize;
        Mesh ceilMesh, wallMesh;
        MeshRenderer finalMeshRenderer;
        private List<GameObject> Children = new();
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
            SplitMeshes(finalMesh, this.finalMeshRenderer);
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
            Mesh combinedMesh = new Mesh();
            CombineInstance[] combineInstances = new CombineInstance[meshFilters.Length];

            for (int i = 0; i < meshFilters.Length; i++)
            {
                MeshFilter meshFilter = meshFilters[i];

                Mesh mesh = meshFilter.sharedMesh;
                combineInstances[i].mesh = mesh;
                combineInstances[i].transform = meshFilter.transform.localToWorldMatrix;
                Destroy(meshFilter.gameObject);
            }

            // Combine the meshes
            combinedMesh.CombineMeshes(combineInstances);

            // Create a new GameObject to hold the combined mesh
            this.combinedMeshObject = new GameObject("CombinedMesh");
            this.combinedMeshObject.transform.SetParent(transform);
            MeshFilter combinedMeshFilter = this.combinedMeshObject.AddComponent<MeshFilter>();
            this.finalMeshRenderer = this.combinedMeshObject.AddComponent<MeshRenderer>();

            combinedMeshFilter.sharedMesh = combinedMesh;
            this.finalMeshRenderer.material = this.mapMat;
            return combinedMesh;
        }
        private void SplitMeshes(Mesh mesh, MeshRenderer rend)
        {
            DestroyChildren();
            var meshSplitter = new MeshSplitter(Parameters, false);
            var subMeshData = meshSplitter.Split(mesh);

            foreach (var (gridPoint, m) in subMeshData)
            {
                if (m.vertexCount > 0)
                    CreateChild(gridPoint, m, rend, this.combinedMeshObject.transform);
            }
            var meshFilter = this.combinedMeshObject.GetComponent<MeshFilter>();
            var meshRend = this.combinedMeshObject.GetComponent<MeshRenderer>();
            Destroy(meshFilter);
            Destroy(meshRend);
        }
        private void DestroyChildren()
        {
            // find child submeshes which are not in child list
            var unassignedSubMeshes = GetComponentsInChildren<MeshRenderer>()
                .Where(child => child.name.Contains("SubMesh") && !Children.Contains(child.gameObject));

            foreach (var subMesh in unassignedSubMeshes)
            {
                Children.Add(subMesh.gameObject);
            }

            foreach (var t in Children)
            {
                // destroy mesh
                DestroyImmediate(t.GetComponent<MeshFilter>().sharedMesh);
                DestroyImmediate(t);
            }

            Children.Clear();
        }

        private void CreateChild(Vector3Int gridPoint, Mesh mesh, MeshRenderer rend, Transform parent)
        {
            var newGameObject = new GameObject
            {
                name = $"SubMesh {gridPoint}"
            };
        
            newGameObject.transform.SetParent(parent, false);
            if (Parameters.UseParentLayer)
            {
                newGameObject.layer = gameObject.layer;
            }
            if (Parameters.UseParentStaticFlag)
            {
                newGameObject.isStatic = gameObject.isStatic;
            }
            
            // assign the new mesh to this submeshes mesh filter
            var newMeshFilter = newGameObject.AddComponent<MeshFilter>();
            newMeshFilter.sharedMesh = mesh;

            var newMeshRenderer = newGameObject.AddComponent<MeshRenderer>();
            if (Parameters.UseParentMeshRendererSettings && this.finalMeshRenderer)
            {
                newMeshRenderer.sharedMaterial = this.finalMeshRenderer.sharedMaterial;
                newMeshRenderer.sortingOrder = this.finalMeshRenderer.sortingOrder;
                newMeshRenderer.sortingLayerID = this.finalMeshRenderer.sortingLayerID;
                newMeshRenderer.shadowCastingMode = this.finalMeshRenderer.shadowCastingMode;
            }

            if (Parameters.GenerateColliders)
            {
                var meshCollider = newGameObject.AddComponent<MeshCollider>();
                meshCollider.convex = Parameters.UseConvexColliders;
                meshCollider.sharedMesh = mesh;
            }
            
            Children.Add(newGameObject);
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
