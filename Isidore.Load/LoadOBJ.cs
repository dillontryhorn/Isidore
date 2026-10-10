using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Isidore.Maths;
using Isidore.Render;

namespace Isidore.Load
{
    /// <summary>
    /// A toolbox for loading Wavefront Object as a polymesh
    /// </summary>
    public class OBJ
    {
        /// <summary>
        /// Loads a Wavefront Object as a polymesh
        /// </summary>
        /// <param name="fileName"> File name of object file to load </param>
        /// <returns> Polymesh </returns>
        public static Polyshape Load(string fileName)
        {
            return Read(Text.Load(fileName));
        }

        /// <summary>
        /// Reads OBJ positions, normals, texture coordinates and faces.
        /// Objects and groups become separate meshes; convex polygon faces
        /// are triangulated as a fan. Material declarations are ignored.
        /// </summary>
        /// <param name="lines"> String array containing the Object data </param>
        /// <returns> Polymesh </returns>
        public static Polyshape Read(string[] lines)
        {
            if (lines == null)
                throw new ArgumentNullException("lines");

            Polyshape meshes = new Polyshape();
            // OBJ indices refer to file-wide tables, including across objects.
            List<double[]> positions = new List<double[]>();
            List<double[]> textures = new List<double[]>();
            List<double[]> normals = new List<double[]>();
            Vertices vertices = new Vertices();
            List<int[]> facets = new List<int[]>();
            Dictionary<Tuple<int, int, int>, int> vertexIndices =
                new Dictionary<Tuple<int, int, int>, int>();
            string name = null;

            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                string line = (lines[lineIndex] ?? "").Trim();
                while (line.EndsWith("\\", StringComparison.Ordinal))
                {
                    if (lineIndex + 1 >= lines.Length)
                        throw new FormatException("Missing OBJ continuation line.");
                    line = line.Substring(0, line.Length - 1) + " " +
                        (lines[++lineIndex] ?? "").Trim();
                }
                int comment = line.IndexOf('#');
                if (comment >= 0)
                    line = line.Substring(0, comment);
                string[] fields = line.Split((char[])null,
                    StringSplitOptions.RemoveEmptyEntries);
                if (fields.Length == 0)
                    continue;

                switch (fields[0])
                {
                    case "o":
                    case "g":
                        if (facets.Count > 0)
                        {
                            Mesh mesh = new Mesh(facets, vertices);
                            mesh.Name = name;
                            meshes.Add(mesh);
                            facets = new List<int[]>();
                            vertices = new Vertices();
                            vertexIndices.Clear();
                        }
                        name = string.Join(" ", fields.Skip(1));
                        break;
                    case "v":
                        positions.Add(ParseCoordinates(fields, 3));
                        break;
                    case "vt":
                        // A missing second texture coordinate is zero in OBJ.
                        double[] uv = ParseCoordinates(fields, 1);
                        textures.Add(new double[] { uv[0], uv.Length > 1 ? uv[1] : 0 });
                        break;
                    case "vn":
                        normals.Add(ParseCoordinates(fields, 3));
                        break;
                    case "f":
                        if (fields.Length < 4)
                            throw new FormatException("An OBJ face needs at least three vertices.");
                        int[] polygon = new int[fields.Length - 1];
                        for (int corner = 1; corner < fields.Length; corner++)
                        {
                            string[] indices = fields[corner].Split('/');
                            if (indices.Length > 3)
                                throw new FormatException("Invalid OBJ face vertex.");
                            int positionIndex = ResolveIndex(indices[0], positions.Count);
                            int textureIndex = indices.Length > 1 && indices[1].Length > 0
                                ? ResolveIndex(indices[1], textures.Count) : -1;
                            int normalIndex = indices.Length > 2 && indices[2].Length > 0
                                ? ResolveIndex(indices[2], normals.Count) : -1;
                            Tuple<int, int, int> key = Tuple.Create(positionIndex,
                                textureIndex, normalIndex);
                            int vertexIndex;
                            if (!vertexIndices.TryGetValue(key, out vertexIndex))
                            {
                                vertexIndex = vertices.Count;
                                Vertex vertex = new Vertex(new Point(
                                    positions[positionIndex].Take(3).ToArray()));
                                if (normalIndex >= 0)
                                    vertex.Normal = new Normal(normals[normalIndex].Take(3).ToArray());
                                if (textureIndex >= 0)
                                    vertex.UV = (double[])textures[textureIndex].Clone();
                                vertices.Add(vertex);
                                vertexIndices.Add(key, vertexIndex);
                            }
                            polygon[corner - 1] = vertexIndex;
                        }
                        for (int corner = 1; corner < polygon.Length - 1; corner++)
                            facets.Add(new int[] { polygon[0], polygon[corner], polygon[corner + 1] });
                        break;
                }
            }

            if (facets.Count > 0)
            {
                Mesh mesh = new Mesh(facets, vertices);
                mesh.Name = name;
                meshes.Add(mesh);
            }
            return meshes;
        }

        private static double[] ParseCoordinates(string[] fields, int minimum)
        {
            if (fields.Length <= minimum)
                throw new FormatException("Incomplete OBJ coordinates.");
            return fields.Skip(1).Select(value => double.Parse(value,
                NumberStyles.Float, CultureInfo.InvariantCulture)).ToArray();
        }

        private static int ResolveIndex(string field, int count)
        {
            int index = int.Parse(field, CultureInfo.InvariantCulture);
            // Positive indices are one-based; negative ones count backwards
            // from the end of the table as it exists on the face's line.
            int resolved = index > 0 ? index - 1 : count + index;
            if (index == 0 || resolved < 0 || resolved >= count)
                throw new FormatException("OBJ index is outside the available coordinate table.");
            return resolved;
        }
    }
}