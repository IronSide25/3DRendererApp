using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;

namespace Grafika4a
{
    public class Camera
    {
        public Vector3 Position;
        public Vector3 Target;
        public float fov;

        public Camera(Vector3 position, Vector3 target)
        {
            Position = position;
            Target = target;
            fov = 1.0472f;
        }
    }
}
