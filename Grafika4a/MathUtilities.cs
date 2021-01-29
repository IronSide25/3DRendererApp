using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grafika4a
{
    static class MathUtilities
    {
        static public float Clamp(float value, float min = 0, float max = 1)
        {
            return Math.Max(min, Math.Min(value, max));
        }

        static public float Interpolate(float min, float max, float gradient)
        {
            return min + (max - min) * Clamp(gradient);
        }


        static public float ComputeNDotL(Vector3 vertex, Vector3 normal, Vector3 lightPosition, int n = 1)
        {
            var lightDirection = lightPosition - vertex;
            normal.Normalize();
            lightDirection.Normalize();
            return Math.Max(0, Vector3.Dot(normal, lightDirection));
        }

        public static void Barycentric(Vector3 p, Vector3 a, Vector3 b, Vector3 c, ref float u, ref float v, ref float w)
        {
            Vector3 v0 = b - a, v1 = c - a, v2 = p - a;
            float d00 = Vector3.Dot(v0, v0);
            float d01 = Vector3.Dot(v0, v1);
            float d11 = Vector3.Dot(v1, v1);
            float d20 = Vector3.Dot(v2, v0);
            float d21 = Vector3.Dot(v2, v1);
            float denom = d00 * d11 - d01 * d01;
            v = (d11 * d20 - d01 * d21) / denom;
            w = (d00 * d21 - d01 * d20) / denom;
            u = 1.0f - v - w;
        }

        private static Vector3 Get3PlanesInterPoint(ref Plane p1, ref Plane p2, ref Plane p3)
        {
            //P = -d1 * N2xN3 / N1.N2xN3 - d2 * N3xN1 / N2.N3xN1 - d3 * N1xN2 / N3.N1xN2 
            Vector3 v =
                -p1.D * Vector3.Cross(p2.Normal, p3.Normal) / Vector3.Dot(p1.Normal, Vector3.Cross(p2.Normal, p3.Normal))
                - p2.D * Vector3.Cross(p3.Normal, p1.Normal) / Vector3.Dot(p2.Normal, Vector3.Cross(p3.Normal, p1.Normal))
                - p3.D * Vector3.Cross(p1.Normal, p2.Normal) / Vector3.Dot(p3.Normal, Vector3.Cross(p1.Normal, p2.Normal));

            return v;
        }

        private static void GetPlanesFromMatrix(ref Matrix matrix, out Plane near, out Plane far, out Plane left, out Plane right, out Plane top, out Plane bottom)
        {
            // Left plane
            left.Normal.X = matrix.M14 + matrix.M11;
            left.Normal.Y = matrix.M24 + matrix.M21;
            left.Normal.Z = matrix.M34 + matrix.M31;
            left.D = matrix.M44 + matrix.M41;
            left.Normalize();

            // Right plane
            right.Normal.X = matrix.M14 - matrix.M11;
            right.Normal.Y = matrix.M24 - matrix.M21;
            right.Normal.Z = matrix.M34 - matrix.M31;
            right.D = matrix.M44 - matrix.M41;
            right.Normalize();

            // Top plane
            top.Normal.X = matrix.M14 - matrix.M12;
            top.Normal.Y = matrix.M24 - matrix.M22;
            top.Normal.Z = matrix.M34 - matrix.M32;
            top.D = matrix.M44 - matrix.M42;
            top.Normalize();

            // Bottom plane
            bottom.Normal.X = matrix.M14 + matrix.M12;
            bottom.Normal.Y = matrix.M24 + matrix.M22;
            bottom.Normal.Z = matrix.M34 + matrix.M32;
            bottom.D = matrix.M44 + matrix.M42;
            bottom.Normalize();

            // Near plane
            near.Normal.X = matrix.M13;
            near.Normal.Y = matrix.M23;
            near.Normal.Z = matrix.M33;
            near.D = matrix.M43;
            near.Normalize();

            // Far plane
            far.Normal.X = matrix.M14 - matrix.M13;
            far.Normal.Y = matrix.M24 - matrix.M23;
            far.Normal.Z = matrix.M34 - matrix.M33;
            far.D = matrix.M44 - matrix.M43;
            far.Normalize();
        }

        public static void GetCorners(Vector3[] corners, Matrix matrix)
        {
            Plane pNear = new Plane();
            Plane pFar = new Plane();
            Plane pBottom = new Plane();
            Plane pTop = new Plane();
            Plane pLeft = new Plane();
            Plane pRight = new Plane();

            GetPlanesFromMatrix(ref matrix, out pNear, out pFar, out pLeft, out pRight, out pTop, out pBottom);

            corners[0] = Get3PlanesInterPoint(ref pFar, ref pBottom, ref pRight);    //Far1
            corners[1] = Get3PlanesInterPoint(ref pFar, ref pTop, ref pRight);       //Far2
            corners[2] = Get3PlanesInterPoint(ref pFar, ref pTop, ref pLeft);        //Far3
            corners[3] = Get3PlanesInterPoint(ref pFar, ref pBottom, ref pLeft);     //Far3
        }

        public static double AngleBetween(Vector3 u, Vector3 v, bool returndegrees)
        {
            double toppart = 0;
            for (int d = 0; d < 3; d++) toppart += u[d] * v[d];

            double u2 = 0; //u squared
            double v2 = 0; //v squared
            for (int d = 0; d < 3; d++)
            {
                u2 += u[d] * u[d];
                v2 += v[d] * v[d];
            }

            double bottompart = 0;
            bottompart = Math.Sqrt(u2 * v2);


            double rtnval = Math.Acos(toppart / bottompart);
            if (returndegrees) rtnval *= 360.0 / (2 * Math.PI);
            return rtnval;
        }
    }
}
 