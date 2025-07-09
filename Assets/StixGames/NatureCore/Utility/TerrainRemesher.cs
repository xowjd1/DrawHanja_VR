using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace StixGames.NatureCore
{
    public class TerrainRemesher
    {
        public float SubmeshesPerSide = 1;
        public float SampleMultiplier = 1;

        private Terrain terrain;

        private float[,] heights;

        private int dataWidth;
        private float sampleCount;
        private float samplesPerSubmesh;
        private float maxHeight;
        private float sampleWidth;
        private float sampleLength;

        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<Vector3> normals = new List<Vector3>();
        private readonly List<Vector2> uvs = new List<Vector2>();
        private readonly List<int> triangles = new List<int>();

        private readonly Dictionary<Coordinate, int> indexLookup = new Dictionary<Coordinate, int>();

        public TerrainRemesher(Terrain terrain)
        {
            this.terrain = terrain;
        }

        /// <summary>
        /// Takes the terrain and converts it into meshes inside the local space of the terrain
        /// </summary>
        /// <returns></returns>
        public List<Mesh> GenerateTerrainMesh()
        {
            Debug.Log(
                $"Generating terrain mesh for grass rendering. The terrain will be split in {SubmeshesPerSide} meshes per side, resulting in a total of {SubmeshesPerSide * SubmeshesPerSide} meshes. " +
                $"The terrain will be sub-sampled by a factor of {SampleMultiplier}, this value should only be above 1 if the grass can't reach the desired density otherwise.");

            var terrainData = terrain.terrainData;
            heights = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);

            dataWidth = terrainData.heightmapResolution;

            sampleCount = dataWidth * SampleMultiplier - 1;
            samplesPerSubmesh = sampleCount / SubmeshesPerSide;

            //Calculate the step size / the distance between all vertices
            sampleWidth = terrainData.size.x / sampleCount;
            sampleLength = terrainData.size.z / sampleCount;
            maxHeight = terrainData.size.y;

            var meshList = new List<Mesh>();

            for (var y = 0; y < SubmeshesPerSide; y++)
            {
                for (var x = 0; x < SubmeshesPerSide; x++)
                {
                    GenerateSubTerrain(meshList, x, y);
                }
            }

            return meshList;
        }

        private void GenerateSubTerrain(List<Mesh> meshList, int subTerrainX, int subTerrainY)
        {
            var xStart = (int)Mathf.Min(subTerrainX * samplesPerSubmesh, sampleCount);
            var yStart = (int)Mathf.Min(subTerrainY * samplesPerSubmesh, sampleCount);
            var xEnd = Mathf.Min((subTerrainX + 1) * samplesPerSubmesh, sampleCount);
            var yEnd = Mathf.Min((subTerrainY + 1) * samplesPerSubmesh, sampleCount);
            for (var x = xStart; x < xEnd; x++)
            {
                for (var y = yStart; y < yEnd; y++)
                {
                    if (vertices.Count + 4 > ushort.MaxValue)
                    {
                        meshList.Add(NextSubMesh(subTerrainX, subTerrainY));
                    }

                    var v0 = GetOrCreateVertex(x, y);
                    var v1 = GetOrCreateVertex(x + 1, y);
                    var v2 = GetOrCreateVertex(x + 1, y + 1);
                    var v3 = GetOrCreateVertex(x, y + 1);

                    triangles.Add(v0);
                    triangles.Add(v1);
                    triangles.Add(v2);

                    triangles.Add(v0);
                    triangles.Add(v2);
                    triangles.Add(v3);
                }
            }

            meshList.Add(NextSubMesh(subTerrainX, subTerrainY));
        }

        private Mesh NextSubMesh(int x, int y)
        {
            var mesh = new Mesh();
            mesh.name = $"SubTerrain Mesh ({x}, {y})";

            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);

            mesh.RecalculateBounds();

            vertices.Clear();
            uvs.Clear();
            normals.Clear();
            triangles.Clear();
            indexLookup.Clear();

            return mesh;
        }

        private int GetOrCreateVertex(int x, int y)
        {
            var coord = new Coordinate(x, y);
            if (indexLookup.ContainsKey(coord))
            {
                return indexLookup[coord];
            }

            var center = GetPosition(x, y);
            vertices.Add(center);
            uvs.Add(new Vector2(y / sampleCount, x / sampleCount));

            //Calculate normal
            var left = GetPosition(x - 1, y);
            var right = GetPosition(x + 1, y);
            var front = GetPosition(x, y - 1);
            var back = GetPosition(x, y + 1);

            var widthDiff = right - left;
            var lengthDiff = front - back;
            normals.Add(Vector3.Cross(lengthDiff, widthDiff));

            int index = vertices.Count - 1;
            indexLookup[coord] = index;
            return index;
        }

        private Vector3 GetPosition(float x, float y)
        {
            // Change the samples to fit the real terrain size
            var sampleX = x / SampleMultiplier;
            var sampleY = y / SampleMultiplier;

            if (sampleX < 0)
            {
                sampleX = 0;
            }

            if (sampleY < 0)
            {
                sampleY = 0;
            }

            if (sampleX > dataWidth - 1)
            {
                sampleX = dataWidth - 1;
            }

            if (sampleY > dataWidth - 1)
            {
                sampleY = dataWidth - 1;
            }

            //Interpolate
            var minX = Mathf.FloorToInt(sampleX);
            var maxX = Mathf.CeilToInt(sampleX);
            var minY = Mathf.FloorToInt(sampleY);
            var maxY = Mathf.CeilToInt(sampleY);
            var fracX = sampleX - minX;
            var fracY = sampleY - minY;
            var invFracX = 1 - fracX;
            var invFracY = 1 - fracY;

            var q11 = heights[minX, minY];
            var q12 = heights[minX, maxY];
            var q21 = heights[maxX, minY];
            var q22 = heights[maxX, maxY];

            var height = q11 * invFracX * invFracY +
                         q21 * fracX * invFracY +
                         q12 * invFracX * fracY +
                         q22 * fracX * fracY;

            return new Vector3(y * sampleLength, height * maxHeight, x * sampleWidth);
        }

        private struct Coordinate
        {
            public readonly int x, y;

            public Coordinate(int x, int y)
            {
                this.x = x;
                this.y = y;
            }

            public bool Equals(Coordinate other)
            {
                return x == other.x && y == other.y;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                return obj is Coordinate && Equals((Coordinate)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (x * 397) ^ y;
                }
            }
        }
    }
}