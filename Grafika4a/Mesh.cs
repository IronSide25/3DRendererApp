using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grafika4a
{
    public class Mesh
    {
        public string Name { get; set; }
        public Vertex[] Vertices { get; private set; }
        public Face[] Faces { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set; }

        public Material material;
        public Texture Texture { get; set; }

        public Mesh(string name, int verticesCount, int facesCount)
        {
            Vertices = new Vertex[verticesCount];
            Faces = new Face[facesCount];
            Name = name;
        }
    }
}
