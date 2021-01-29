using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grafika4a
{
    public struct Material
    {
        public Color4 diffuse;
        public Color4 specular;
        public Color4 ambient;

        public Material(Color4 _diffuse, Color4 _specular, Color4 _ambient)
        {
            diffuse = _diffuse;
            specular = _specular;
            ambient = _ambient;
        }
    }
}
