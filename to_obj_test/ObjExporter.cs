using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace to_obj_test
{
    class ObjExporter : IJtFaceEmitter
    {
        static bool _add_color = true;
        static bool _more_transparent = false; //设置此标志以将具有一点透明度的所有内容切换为完全透明以用于测试目的。

        #region mtl statement format strings
        const string _mtl_newmtl_d
            = "newmtl {0}\r\n"
            + "ka {1} {2} {3}\r\n"
            + "Kd {1} {2} {3}\r\n"
            + "d {4}";
        const string _mtl_newmtl_tr
            = "newmtl {0}\r\n"
            + "Ka {1} {2} {3}\r\n"
            + "Kd {1} {2} {3}\r\n"
            + "Tr {4}";
        const string _mtl_mtllib = "mtllib {0}";
        const string _mtl_usemtl = "usemtl {0}";
        const string _mtl_vertex = "v {0} {1} {2}";
        const string _mtl_face = "f {0} {1} {2}";
        #endregion

        #region VertexLookupInt  一个基于整形的三维点类
        class PointInt : IComparable<PointInt>
        {
            public int X { get; set; }
            public int Y { get; set; }
            public int Z { get; set; }

            const double _feet_to_mm = 25.4 * 12;
            static int ConvertFeetToMillimetres(double d)
            {
                return (int)(_feet_to_mm * d + 0.5);
            }
            public PointInt(XYZ p)
            {
                X = ConvertFeetToMillimetres(p.X);
                Y = ConvertFeetToMillimetres(p.Y);
                Z = ConvertFeetToMillimetres(p.Z);
            }

            public int CompareTo(PointInt a)
            {
                int d = X - a.X;
                if (0 == d)
                {
                    d = Y - a.Y;
                    if (0 == d)
                    {
                        d = Z - a.Z;
                    }
                }
                return d;
            }
        }

        class VertexLookupInt : Dictionary<PointInt, int>
        {
            #region PointIntEqualityComparer  Define equality for integer-based PointInt.
            class PointIntEqualityComparer : IEqualityComparer<PointInt>
            {
                public bool Equals(PointInt p, PointInt q)
                {
                    return 0 == p.CompareTo(q);
                }
                public int GetHashCode(PointInt p)
                {
                    return (p.X.ToString()
                        + "," + p.Y.ToString()
                        + "," + p.Z.ToString()).GetHashCode();
                }
            }
            #endregion

            public VertexLookupInt(): base(new PointIntEqualityComparer())
            {
            }

            public int AddVertex(PointInt p)
            {
                return ContainsKey(p) ? this[p] : this[p] = Count;
            }
        }
        #endregion

        #region ColorTransparencyLookup
        class ColorTransparencyLookup : Dictionary<int, int>
        {
            int _current;

            public ColorTransparencyLookup()
            {
                _current = Util.ColorTransparencyToInt(Class1.DefaultColor, 0);
            }

            public bool AddColorTransparency(Color color, int shininess, int transparency)
            {
                int trgb = Util.ColorTransparencyToInt(color, transparency);

                if (!ContainsKey(trgb))
                {
                    this[trgb] = Count;
                }

                bool rc = !_current.Equals(trgb);
                _current = trgb;
                return rc;     
            }
        }
        #endregion //ColorTransparencyLookup


        VertexLookupInt _vertices;
        ColorTransparencyLookup _color_transparency_lookup;

        List<int> _triangles;
        int _faceCount;
        int _triangleCount;

        public ObjExporter()
        {
            _faceCount = 0;
            _triangleCount = 0;
            _vertices = new VertexLookupInt();
            _triangles = new List<int>();

            if (_add_color)
            {
                _color_transparency_lookup = new ColorTransparencyLookup();
            }
        }

        void StoreColorTransparency(Color color,int transparency)  //Set a color for the following faces.
        {
            _triangles.Add(-1); // color marker

            _triangles.Add(Util.ColorTransparencyToInt(
              color, transparency));

            _triangles.Add(0); // multiple of three
        }

        // 将三角形顶点添加到列表中
        void StoreTriangle(MeshTriangle triangle)

        {
            for (int i = 0; i < 3; ++i)
            {
                XYZ p = triangle.get_Vertex(i);
                PointInt q = new PointInt(p);
                _triangles.Add(_vertices.AddVertex(q));
            }
        }

        public int EmitFace(Face face,Color color,int shininess,int transparency)
        {
            Debug.Assert(0 <= shininess,
              "expected non-negative shininess");

            Debug.Assert(128 >= shininess,
              "expected shininess between 0 and 128");

            Debug.Assert(0 <= transparency,
              "expected non-negative transparency");

            Debug.Assert(100 >= transparency,
              "expected transparency between 0 and 100");

            Debug.Assert(100 * Math.Pow(2, 24) == 1677721600,
              "expected shifted transparency to fit into a signed integer");

            Debug.Assert(1677721600 < int.MaxValue,
              "expected transparency to fit into a signed integer");

            ++_faceCount;

            if (_add_color
              && _color_transparency_lookup
                .AddColorTransparency(color,
                  shininess, transparency))
            {
                StoreColorTransparency(color, transparency);
            }
            //对面进行三角面片化形成一个网格
            Mesh mesh = face.Triangulate();
            int n = mesh.NumTriangles;//网格包含的三角形数
            Debug.Print(" {0} mesh triangles", n);
            for (int i = 0; i < n; ++i)
            {
                ++_triangleCount;
                MeshTriangle t = mesh.get_Triangle(i);
                StoreTriangle(t);
            }
            return n;
        }

        public int GetFaceCount()
        {
            return _faceCount;
        } 

        public int GetTriangleCount()
        {
            // Originally, we just returned _triangles.Count
            // divided by 3, but that no longer works now 
            // that colours may be stored as well.

            if (!_add_color)
            {
                int n = _triangles.Count;

                Debug.Assert(0 == n % 3,
                  "expected a multiple of 3");

                Debug.Assert(_triangleCount.Equals(n / 3),
                  "expected equal triangle count");
            }
            return _triangleCount;
        }

        public int GetVertexCount()
        {
            return _vertices.Count;
        }

        #region ExportTo:output the mtl file
        static void EmitColorTransparency(StreamWriter s,int trgb)
        {
            int transparency;

            Color color = Util.IntToColorTransparency(
              trgb, out transparency);

            string name = Util.ColorTransparencyString(
              color, transparency);

            if (_more_transparent && 0 < transparency)
            {
                transparency = 100;
            }

            s.WriteLine(_mtl_newmtl_d,
              name,
              color.Red / 256.0,
              color.Green / 256.0,
              color.Blue / 256.0,
              (100 - transparency) / 100.0);
        }
        #endregion ExportTo:output the mtl file

        #region ExportTo: output the OBJ file
        /// <summary>
        /// Emit a vertex to OBJ. The first vertex listed 
        /// in the file has index 1, and subsequent ones
        /// are numbered sequentially.
        /// </summary>
        static void EmitVertex(StreamWriter s, PointInt p)
        {
            s.WriteLine(_mtl_vertex, p.X, p.Y, p.Z);
        }

        /// <summary>
        /// Set colour and transparency for subsequent 
        /// faces, referring to the named materials in 
        /// the material library.
        /// </summary>
        static void SetColorTransparency(StreamWriter s,int trgb)
        {
            int transparency;

            Color color = Util.IntToColorTransparency(trgb, out transparency);

            string name = Util.ColorTransparencyString(color, transparency);

            s.WriteLine(_mtl_usemtl, name);
        }

        /// <summary>
        /// Emit an OBJ triangular face.
        /// </summary>
        static void EmitFacet( StreamWriter s, int i, int j, int k)
        {
            s.WriteLine(_mtl_face, i + 1, j + 1, k + 1);
        }

        public void ExportTo(string path)
        {
            string material_library_path = null;

            if (_add_color)
            {
                material_library_path = Path.ChangeExtension(path, "mtl");

                using (StreamWriter s = new StreamWriter( material_library_path))
                {
                    foreach (int key in _color_transparency_lookup.Keys)
                    {
                        EmitColorTransparency(s, key);
                    }
                }
            }

            using (StreamWriter s = new StreamWriter(path))
            {
                if (_add_color)
                {
                    s.WriteLine(_mtl_mtllib, Path.GetFileName( material_library_path).ToString());
                }

                foreach (PointInt key in _vertices.Keys)
                {
                    EmitVertex(s, key);
                }

                int i = 0;
                int n = _triangles.Count;

                while (i < n)
                {
                    int i1 = _triangles[i++];
                    int i2 = _triangles[i++];
                    int i3 = _triangles[i++];

                    if (-1 == i1)
                    {
                        SetColorTransparency(s, i2);
                    }
                    else
                    {
                        EmitFacet(s, i1, i2, i3);
                    }
                }
            }
        }
        #endregion // ExportTo: output the OBJ file
    }
}