using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grafika4a
{
    public struct Scene
    {
        public List<Mesh> meshes;
        public Camera mainCamera;
        public List<PointLight> pointLights;
        public Color4 ambientlight;

        public Scene(Camera camera, Color4 ambientLight)
        {
            meshes = new List<Mesh>();
            pointLights = new List<PointLight>();
            mainCamera = camera;
            this.ambientlight = ambientLight;
        }
    }
}
