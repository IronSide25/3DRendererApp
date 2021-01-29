using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grafika4a
{
    public struct PointLight
    {
        public Vector3 position { get; set; }
        public Color4 diffuse { get; set; }
        public Color4 ambient { get; set; }
        public Color4 specular { get; set; }

        public PointLight(Vector3 position, Color4 diffuse, Color4 ambient, Color4 specular)
        {
            this.position = position;
            this.diffuse = diffuse;
            this.ambient = ambient;
            this.specular = specular;
        }
    }
}
