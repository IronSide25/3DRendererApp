using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.InteropServices.WindowsRuntime;
using SharpDX;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.IO;

namespace Grafika4a
{
    public class Device
    {
        bool ortho;
        private byte[] backBuffer;
        private readonly float[] depthBuffer;
        private object[] lockBuffer;
        private WriteableBitmap bmp;
        private readonly int renderWidth;
        private readonly int renderHeight;

        private Shading shading = Shading.phong;
        public RenderMode renderMode = RenderMode.wireframe;

        Scene scene;
        Camera camera;
        public float fov = 1.0472f;
        public bool reverseBackFace;

        public Device(WriteableBitmap bmp, bool ortho, Scene scene, Camera camera)
        {
            this.bmp = bmp;
            this.scene = scene;
            this.camera = camera;
            this.ortho = ortho;
            renderWidth = bmp.PixelWidth;
            renderHeight = bmp.PixelHeight;
            backBuffer = new byte[renderWidth  * renderHeight * 4];
            depthBuffer = new float[renderWidth  * renderHeight];
            lockBuffer = new object[renderWidth * renderHeight];
            for (var i = 0; i < lockBuffer.Length; i++)
            {
                lockBuffer[i] = new object();
            }
        }

        public void Clear(byte r, byte g, byte b, byte a)
        {
            for (int index = 0; index < backBuffer.Length; index += 4)//clearing back buffer
            {
                backBuffer[index] = b;
                backBuffer[index + 1] = g;
                backBuffer[index + 2] = r;
                backBuffer[index + 3] = a;
            }
            for (int index = 0; index < depthBuffer.Length; index++)//clearing depth buffer
            {
                depthBuffer[index] = float.MaxValue;
            }
        }


        public void Present()
        {
            /*unsafe
            {
                byte* backBuffer = (byte*)bmp.BackBuffer;
                bmp.BackBuffer = backBuffer;
            }*/          
            bmp.WritePixels(new System.Windows.Int32Rect(0, 0, renderWidth , renderHeight), backBuffer, renderWidth  * 4, 0);
        }

        public void PutPixelWireframe(int x, int y, Color4 color)
        {
            var index = (x + y * renderWidth ) * 4;//index on back buffer
            backBuffer[index] = (byte)(color.Blue * 255);
            backBuffer[index + 1] = (byte)(color.Green * 255);
            backBuffer[index + 2] = (byte)(color.Red * 255);
            backBuffer[index + 3] = (byte)(color.Alpha * 255);
        }

        public void PutPixel(int x, int y, float z, Color4 color)
        {
            var index = (x + y * renderWidth);
            var index4 = index * 4;

            lock (lockBuffer[index])
            {
                if (depthBuffer[index] < z)
                {
                    return; // Discard
                }
                depthBuffer[index] = z;
                backBuffer[index4] = (byte)(color.Blue * 255);
                backBuffer[index4 + 1] = (byte)(color.Green * 255);
                backBuffer[index4 + 2] = (byte)(color.Red * 255);
                backBuffer[index4 + 3] = (byte)(color.Alpha * 255);
            }               
        }


        public Vector2 ProjectWireframe(Vector3 coordinates, Matrix transMat)
        {
            Vector3 point = Vector3.TransformCoordinate(coordinates, transMat);
            float x = point.X * renderWidth  + renderWidth  / 2.0f;
            float y = -point.Y * renderHeight + renderHeight / 2.0f;
            return (new Vector2(x, y));
        }


        public Vertex Project(Vertex vertex, Matrix transMat, Matrix world)
        {
            Vector3 point2d = Vector3.TransformCoordinate(vertex.Coordinates, transMat);//2d space
            Vector3 point3dWorld = Vector3.TransformCoordinate(vertex.Coordinates, world);//3d world
            Vector3 normal3dWorld = Vector3.TransformNormal(vertex.Normal, world);//3d world
            //normal3dWorld.Normalize();
            float x = point2d.X * renderWidth + renderWidth / 2.0f;//transforming coordinate system from center to top left
            float y = -point2d.Y * renderHeight + renderHeight / 2.0f;

            return new Vertex
            {
                Coordinates = new Vector3(x, y, point2d.Z),
                Normal = normal3dWorld,
                WorldCoordinates = point3dWorld,
                TextureCoordinates = vertex.TextureCoordinates
            };
        }

        public void DrawPointWireframe(Vector2 point, Color4 color)
        {
            if (point.X >= 0 && point.Y >= 0 && point.X < renderWidth  && point.Y < renderHeight)//clip
            {
                PutPixelWireframe((int)point.X, (int)point.Y, color);
            }
        }

        public void DrawPoint(Vector3 point, Color4 color)
        {
            if (point.X >= 0 && point.Y >= 0 && point.X < renderWidth  && point.Y < renderHeight)//clip
            {
                PutPixel((int)point.X, (int)point.Y, point.Z, color);
            }
        }

        public void DrawLine(Vector2 point0, Vector2 point1)
        {
            float dist = (point1 - point0).Length();
            if (dist < 2)
                return;
            Vector2 middlePoint = point0 + (point1 - point0) / 2;
            DrawPointWireframe(middlePoint, new Color4(1.0f, 1.0f, 0.0f, 1.0f));
            DrawLine(point0, middlePoint);
            DrawLine(middlePoint, point1);
        }

        public void DrawBline(Vector2 point0, Vector2 point1, Color4 color)
        {
            int x0 = (int)point0.X;
            int y0 = (int)point0.Y;
            int x1 = (int)point1.X;
            int y1 = (int)point1.Y;

            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);
            int sx = (x0 < x1) ? 1 : -1;
            int sy = (y0 < y1) ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                DrawPointWireframe(new Vector2(x0, y0), color);

                if ((x0 == x1) && (y0 == y1)) break;
                var e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 < dx) { err += dx; y0 += sy; }
            }
        }


        Matrix viewMatrixInv;
        Matrix viewMatrix;
        public void Render(Vector3 up)
        {
            viewMatrix = Matrix.LookAtLH(camera.Position, camera.Target, up);//z
            viewMatrixInv = Matrix.LookAtLH(camera.Position, camera.Target, up);
            viewMatrixInv.Invert();
            Matrix projectionMatrix;
            if (!ortho)
                projectionMatrix = Matrix.PerspectiveFovLH(fov, (float)renderWidth  / renderHeight, 0.01f, 1.0f);
            else
            {
                projectionMatrix = Matrix.OrthoLH(600/10, 480/10, 0.01f, 1.0f); 
            }
            
            foreach (Mesh mesh in scene.meshes)
            {
                Matrix worldMatrix = Matrix.RotationYawPitchRoll(mesh.Rotation.Y, mesh.Rotation.X, mesh.Rotation.Z) * Matrix.Translation(mesh.Position);
                Matrix transformMatrix = worldMatrix * viewMatrix * projectionMatrix;

                if (ortho)
                {
                    Vector2 point;
                    foreach (PointLight light in scene.pointLights)
                    {
                        transformMatrix = viewMatrix * projectionMatrix;
                        point = ProjectWireframe(light.position, transformMatrix);
                        DrawBline(new Vector2(point.X - 3, point.Y - 3), new Vector2(point.X - 3, point.Y + 3), light.diffuse);
                        DrawBline(new Vector2(point.X + 3, point.Y - 3), new Vector2(point.X + 3, point.Y + 3), light.diffuse);
                        DrawBline(new Vector2(point.X - 3, point.Y - 3), new Vector2(point.X + 3, point.Y - 3), light.diffuse);
                        DrawBline(new Vector2(point.X - 3, point.Y + 3), new Vector2(point.X + 3, point.Y + 3), light.diffuse);
                        DrawPointWireframe(point, light.diffuse);
                    }
                    DrawCameraCone(projectionMatrix);
                }

                if (renderMode == RenderMode.points)
                {
                    foreach (var vertex in mesh.Vertices)
                    {
                        var point = ProjectWireframe(vertex.Coordinates, transformMatrix);
                        DrawPointWireframe(point, new Color4(1.0f, 1.0f, 0.0f, 1.0f));
                    }
                }
                else
                {
                    Parallel.For(0, mesh.Faces.Length, faceIndex =>
                    {
                        Face face = mesh.Faces[faceIndex];
                        Vertex vertexA = mesh.Vertices[face.A];
                        Vertex vertexB = mesh.Vertices[face.B];
                        Vertex vertexC = mesh.Vertices[face.C];

                        if (renderMode == RenderMode.solid || renderMode == RenderMode.textured)
                        {
                            Vertex pixelA = Project(vertexA, transformMatrix, worldMatrix);
                            Vertex pixelB = Project(vertexB, transformMatrix, worldMatrix);
                            Vertex pixelC = Project(vertexC, transformMatrix, worldMatrix);

                            if ((pixelA.Coordinates.X > 0 && pixelA.Coordinates.X < renderWidth && pixelA.Coordinates.Y > 0 && pixelA.Coordinates.Y < renderHeight) ||
                            (pixelB.Coordinates.X > 0 && pixelB.Coordinates.X < renderWidth && pixelB.Coordinates.Y > 0 && pixelB.Coordinates.Y < renderHeight) ||
                            (pixelC.Coordinates.X > 0 && pixelC.Coordinates.X < renderWidth && pixelC.Coordinates.Y > 0 && pixelC.Coordinates.Y < renderHeight))
                            {
                                if(MathUtilities.AngleBetween(camera.Target - camera.Position, vertexA.Coordinates - camera.Position, true) < 100)
                                {
                                    var ax = pixelA.Coordinates.X - pixelB.Coordinates.X;
                                    var ay = pixelA.Coordinates.Y - pixelB.Coordinates.Y;
                                    var bx = pixelA.Coordinates.X - pixelC.Coordinates.X;
                                    var by = pixelA.Coordinates.Y - pixelC.Coordinates.Y;
                                    var cz = ax * by - ay * bx;
                                    if (!reverseBackFace)
                                    {
                                        if (cz < 0)
                                        {
                                            Color4 color = new Color4(1f, 1f, 0, 1);
                                            DrawTriangle(pixelA, pixelB, pixelC, color, mesh.material, mesh.Texture);
                                        }
                                    }
                                    else if (cz >= 0)
                                    {
                                        Color4 color = new Color4(1f, 1f, 0, 1);
                                        DrawTriangle(pixelA, pixelB, pixelC, color, mesh.material, mesh.Texture);
                                    }
                                }                               
                            }
                        }
                        else
                        {
                            Vector2 pixelA = ProjectWireframe(vertexA.Coordinates, transformMatrix);
                            Vector2 pixelB = ProjectWireframe(vertexB.Coordinates, transformMatrix);
                            Vector2 pixelC = ProjectWireframe(vertexC.Coordinates, transformMatrix);

                            if((pixelA.X > 0 && pixelA.X < renderWidth && pixelA.Y > 0 && pixelA.Y < renderHeight) ||
                            (pixelB.X > 0 && pixelB.X < renderWidth && pixelB.Y > 0 && pixelB.Y < renderHeight) ||
                            (pixelC.X > 0 && pixelC.X < renderWidth && pixelC.Y > 0 && pixelC.Y < renderHeight))
                            {
                                if (MathUtilities.AngleBetween(camera.Target - camera.Position, vertexA.Coordinates - camera.Position, true) < 100)
                                {
                                    DrawBline(pixelA, pixelB, mesh.material.diffuse);
                                    DrawBline(pixelB, pixelC, mesh.material.diffuse);
                                    DrawBline(pixelC, pixelA, mesh.material.diffuse);
                                }                                   
                            }                           
                        }
                        faceIndex++;
                    });
                }
            }
        }


        void DrawCameraCone(Matrix projectionMatrix)
        {
            Matrix worldMatrix = Matrix.Identity;
            var transformMatrix = worldMatrix * viewMatrix * projectionMatrix;
            Vector2 target = ProjectWireframe(scene.mainCamera.Target, transformMatrix);
            DrawBline(new Vector2(target.X - 3, target.Y - 3), new Vector2(target.X - 3, target.Y + 3), new Color4(1, 0, 0, 1.0f));
            DrawBline(new Vector2(target.X + 3, target.Y - 3), new Vector2(target.X + 3, target.Y + 3), new Color4(1, 0, 0, 1.0f));
            DrawBline(new Vector2(target.X - 3, target.Y - 3), new Vector2(target.X + 3, target.Y - 3), new Color4(1, 0, 0, 1.0f));
            DrawBline(new Vector2(target.X - 3, target.Y + 3), new Vector2(target.X + 3, target.Y + 3), new Color4(1, 0, 0, 1.0f));

            transformMatrix = worldMatrix * viewMatrix * projectionMatrix;
            Vector2 eye = ProjectWireframe(scene.mainCamera.Position, transformMatrix);
            DrawBline(new Vector2(eye.X - 3, eye.Y - 3), new Vector2(eye.X - 3, eye.Y + 3), new Color4(1, 0, 0, 1.0f));
            DrawBline(new Vector2(eye.X + 3, eye.Y - 3), new Vector2(eye.X + 3, eye.Y + 3), new Color4(1, 0, 0, 1.0f));
            DrawBline(new Vector2(eye.X - 3, eye.Y - 3), new Vector2(eye.X + 3, eye.Y - 3), new Color4(1, 0, 0, 1.0f));
            DrawBline(new Vector2(eye.X - 3, eye.Y + 3), new Vector2(eye.X + 3, eye.Y + 3), new Color4(1, 0, 0, 1.0f));

            Vector3[] corners = new Vector3[4];
            MathUtilities.GetCorners(corners, Matrix.PerspectiveFovLH(scene.mainCamera.fov/1.82f, (float)renderWidth / renderHeight, 0.1f, (scene.mainCamera.Target - scene.mainCamera.Position).Length()));

            Quaternion rot = Quaternion.LookAtLH(scene.mainCamera.Position, scene.mainCamera.Target, Vector3.UnitY);
            rot.Normalize();
            rot.Conjugate();

            worldMatrix = Matrix.RotationQuaternion(rot) * Matrix.Translation(scene.mainCamera.Position);
            transformMatrix = worldMatrix * viewMatrix * projectionMatrix;
            Vector2 v1 = ProjectWireframe(corners[0], transformMatrix);
            Vector2 v2 = ProjectWireframe(corners[1], transformMatrix);
            Vector2 v3 = ProjectWireframe(corners[2], transformMatrix);
            Vector2 v4 = ProjectWireframe(corners[3], transformMatrix);

            DrawBline(eye, v1, new Color4(1, 0, 0, 1));
            DrawBline(eye, v2, new Color4(1, 0, 0, 1));
            DrawBline(eye, v3, new Color4(1, 0, 0, 1));
            DrawBline(eye, v4, new Color4(1, 0, 0, 1));

            DrawBline(v1, v2, new Color4(1, 0, 0, 1));
            DrawBline(v3, v4, new Color4(1, 0, 0, 1));
            DrawBline(v1, v4, new Color4(1, 0, 0, 1));
            DrawBline(v2, v3, new Color4(1, 0, 0, 1));

            DrawBline(target, eye, new Color4(1, 0, 0, 1));
            DrawPointWireframe(eye, new Color4(0, 0, 1, 1.0f));
        }


        public static async Task<Mesh[]> LoadJSONFileAsync(string fileName)
        {
            var meshes = new List<Mesh>();
            var materials = new Dictionary<String, MaterialTexture>();
            string data;
            using (FileStream file = new FileStream(fileName, FileMode.Open))
            {
                using (StreamReader stream = new StreamReader(file))
                {
                    data = await stream.ReadToEndAsync();
                }
            }
            dynamic jsonObject = Newtonsoft.Json.JsonConvert.DeserializeObject(data);

            for (int materialIndex = 0; materialIndex < jsonObject.materials.Count; materialIndex++)
            {
                MaterialTexture material = new MaterialTexture();
                material.Name = jsonObject.materials[materialIndex].name.Value;
                material.ID = jsonObject.materials[materialIndex].id.Value;
                if (jsonObject.materials[materialIndex].diffuseTexture != null)
                    material.DiffuseTextureName = jsonObject.materials[materialIndex].diffuseTexture.name.Value;

                materials.Add(material.ID, material);
            }

            for (var meshIndex = 0; meshIndex < jsonObject.meshes.Count; meshIndex++)
            {
                var verticesArray = jsonObject.meshes[meshIndex].vertices;
                var indicesArray = jsonObject.meshes[meshIndex].indices;

                var uvCount = jsonObject.meshes[meshIndex].uvCount.Value;
                var verticesStep = 1;

                switch ((int)uvCount)
                {
                    case 0:
                        verticesStep = 6;
                        break;
                    case 1:
                        verticesStep = 8;
                        break;
                    case 2:
                        verticesStep = 10;
                        break;
                }

                dynamic verticesCount = verticesArray.Count / verticesStep;
                dynamic facesCount = indicesArray.Count / 3;
                Mesh mesh = new Mesh(jsonObject.meshes[meshIndex].name.Value, verticesCount, facesCount);

                for (var index = 0; index < verticesCount; index++)
                {
                    float x = (float)verticesArray[index * verticesStep].Value;
                    float y = (float)verticesArray[index * verticesStep + 1].Value;
                    float z = (float)verticesArray[index * verticesStep + 2].Value;
                    float nx = (float)verticesArray[index * verticesStep + 3].Value;
                    float ny = (float)verticesArray[index * verticesStep + 4].Value;
                    float nz = (float)verticesArray[index * verticesStep + 5].Value;

                    mesh.Vertices[index] = new Vertex
                    {
                        Coordinates = new Vector3(x, y, z),
                        Normal = new Vector3(nx, ny, nz)
                    };

                    if (uvCount > 0)
                    {
                        float u = (float)verticesArray[index * verticesStep + 6].Value;
                        float v = (float)verticesArray[index * verticesStep + 7].Value;
                        mesh.Vertices[index].TextureCoordinates = new Vector2(u, v);
                    }
                }

                for (var index = 0; index < facesCount; index++)//filling faces arr
                {
                    int a = (int)indicesArray[index * 3].Value;
                    int b = (int)indicesArray[index * 3 + 1].Value;
                    int c = (int)indicesArray[index * 3 + 2].Value;
                    mesh.Faces[index] = new Face { A = a, B = b, C = c };
                }

                // Getting the position you've set in Blender
                dynamic position = jsonObject.meshes[meshIndex].position;
                mesh.Position = new Vector3((float)position[0].Value, (float)position[1].Value, (float)position[2].Value);

                if (uvCount > 0)
                {
                    // Texture
                    dynamic meshTextureID = jsonObject.meshes[meshIndex].materialId.Value;
                    dynamic meshTextureName = materials[meshTextureID].DiffuseTextureName;
                    mesh.Texture = new Texture(meshTextureName, 512, 512);
                }

                meshes.Add(mesh);
            }
            return meshes.ToArray();
        }

        void DrawScanLine(ScanLineData data, Vertex va, Vertex vb, Vertex vc, Vertex vd, Vector3 surfaceNormalNorm, bool fromStart, Material material, Texture texture)
        {
            Vertex v1,v2,v3;
            if (fromStart)
            {
                v1 = va;
                v2 = vb;
                v3 = vc;
            }
            else
            {
                v1 = vd;
                v2 = vb;
                v3 = vc;
            }

            float gradient1 = va.Coordinates.Y != vb.Coordinates.Y ? (data.currentY - va.Coordinates.Y) / (vb.Coordinates.Y - va.Coordinates.Y) : 1;// Thanks to current Y, we can compute the gradient to compute others values like the starting X (sx) and ending X (ex) to draw between if pa.Y == pb.Y or pc.Y == pd.Y, gradient is forced to 1
            float gradient2 = vc.Coordinates.Y != vd.Coordinates.Y ? (data.currentY - vc.Coordinates.Y) / (vd.Coordinates.Y - vc.Coordinates.Y) : 1;

            int sx = (int)MathUtilities.Interpolate(va.Coordinates.X, vb.Coordinates.X, gradient1);
            int ex = (int)MathUtilities.Interpolate(vc.Coordinates.X, vd.Coordinates.X, gradient2);

            // starting Z & ending Z
            float z1 = MathUtilities.Interpolate(va.Coordinates.Z, vb.Coordinates.Z, gradient1);
            float z2 = MathUtilities.Interpolate(vc.Coordinates.Z, vd.Coordinates.Z, gradient2);

            //float snl = MathUtilities.Interpolate(data.ndotla, data.ndotlb, gradient1);
            //float enl = MathUtilities.Interpolate(data.ndotlc, data.ndotld, gradient2);

            //float gradient1World = va.Coordinates.Y != vb.WorldCoordinates.Y ? (data.currentYWorld - va.WorldCoordinates.Y) / (vb.WorldCoordinates.Y - va.WorldCoordinates.Y) : 1;
            //float gradient2World = vc.Coordinates.Y != vd.WorldCoordinates.Y ? (data.currentYWorld - vc.WorldCoordinates.Y) / (vd.WorldCoordinates.Y - vc.WorldCoordinates.Y) : 1;
            float z1World = MathUtilities.Interpolate(va.WorldCoordinates.Z, vb.WorldCoordinates.Z, gradient1);
            float z2World = MathUtilities.Interpolate(vc.WorldCoordinates.Z, vd.WorldCoordinates.Z, gradient2);

            int sxWorld = (int)MathUtilities.Interpolate(va.WorldCoordinates.X, vb.WorldCoordinates.X, gradient1);
            int exWorld = (int)MathUtilities.Interpolate(vc.WorldCoordinates.X, vd.WorldCoordinates.X, gradient2);

            var su = MathUtilities.Interpolate(data.ua, data.ub, gradient1);
            var eu = MathUtilities.Interpolate(data.uc, data.ud, gradient2);
            var sv = MathUtilities.Interpolate(data.va, data.vb, gradient1);
            var ev = MathUtilities.Interpolate(data.vc, data.vd, gradient2);


            // drawing a line from left (sx) to right (ex) 
            for (int x = sx; x < ex; x++)
            {              
                float gradient = (x - sx) / (float)(ex - sx);
                float z = MathUtilities.Interpolate(z1, z2, gradient);

                float xWorld = MathUtilities.Interpolate(sxWorld, exWorld, gradient);
                float zWorld = MathUtilities.Interpolate(z1World, z2World, gradient);
                Vector3 worldPos = new Vector3(xWorld, data.currentYWorld, zWorld);

                //var ndotl = MathUtilities.Interpolate(snl, enl, gradient);// changing the color value using the cosine of the angle between the light vector and the normal vector
                var u = MathUtilities.Interpolate(su, eu, gradient);
                var v = MathUtilities.Interpolate(sv, ev, gradient);

                if (texture != null && renderMode == RenderMode.textured)
                {
                    material.diffuse = texture.Map(u, v);
                    material.specular = texture.Map(u, v);
                }

                if (shading == Shading.phong)
                {
                    float w1 = 0, w2 = 0, w3 = 0;//normals interpolation
                    MathUtilities.Barycentric(new Vector3(x, data.currentY, z), v1.Coordinates, v2.Coordinates, v3.Coordinates, ref w1, ref w2, ref w3);//MathUtilities.Barycentric(worldPos, n1.Coordinates, n2.Coordinates, n3.Coordinates, ref w1, ref w2, ref w3);
                    surfaceNormalNorm = w1 * v1.Normal + w2 * v2.Normal + w3 * v3.Normal;
                    surfaceNormalNorm.Normalize();
                    
                }

                //phong phong lightning model  
                //diffuse
                float reflection = 0;
                Color3 diffuse = new Color3(0,0,0);
                foreach(PointLight light in scene.pointLights)
                {
                    reflection = MathUtilities.ComputeNDotL(worldPos, surfaceNormalNorm, light.position);                   
                    diffuse.Red += light.diffuse.Red * reflection; diffuse.Green += light.diffuse.Green * reflection; diffuse.Blue += light.diffuse.Blue * reflection;
                    
                }
                diffuse.Red = material.diffuse.Red * diffuse.Red; diffuse.Green = material.diffuse.Green * diffuse.Green; diffuse.Blue = material.diffuse.Blue * diffuse.Blue;                                            
                //ambient
                Color4 ambient = scene.ambientlight;
                //specular        
                float materialShiness = 200f;
                Color3 specular = new Color3(0, 0, 0);
                foreach(PointLight light in scene.pointLights)
                {
                    Vector3 lightDir = worldPos - light.position;
                    lightDir.Normalize();
                    if (Vector3.Dot(surfaceNormalNorm, -lightDir) >= 0f)
                    {
                        Vector3 reflectionDir = Vector3.Reflect(lightDir, surfaceNormalNorm);
                        Vector3 inViewDir;
                        inViewDir = Vector3.TransformCoordinate(Vector3.Zero, viewMatrixInv) - worldPos;
                        inViewDir.Normalize();
                        float shineFactor = Vector3.Dot(reflectionDir, inViewDir);
                        float shineFactorSqr = (float)Math.Pow(Math.Max(0.0f, shineFactor), materialShiness);
                        specular.Red += light.specular.Red * material.specular.Red * shineFactorSqr; specular.Green += light.specular.Green * material.specular.Green * shineFactorSqr; specular.Blue += light.specular.Blue * material.specular.Blue * shineFactorSqr;
                    }
                }
                                
                Color4 resultColor = new Color4(diffuse.Red + specular.Red + ambient.Red, diffuse.Green + specular.Green + ambient.Green, diffuse.Blue + specular.Blue + ambient.Blue, 1);
                resultColor.Red = MathUtilities.Clamp(resultColor.Red);
                resultColor.Green = MathUtilities.Clamp(resultColor.Green);
                resultColor.Blue = MathUtilities.Clamp(resultColor.Blue);

                DrawPoint(new Vector3(x, data.currentY, z), resultColor);
            }
        }

        public void DrawTriangle(Vertex v1, Vertex v2, Vertex v3, Color4 color, Material material, Texture texture)
        {
            if (v1.Coordinates.Y > v2.Coordinates.Y)// Sorting the points from top
            {
                Vertex temp = v2;
                v2 = v1;
                v1 = temp;
            }
            if (v2.Coordinates.Y > v3.Coordinates.Y)
            {
                Vertex temp = v2;
                v2 = v3;
                v3 = temp;
            }
            if (v1.Coordinates.Y > v2.Coordinates.Y)
            {
                Vertex temp = v2;
                v2 = v1;
                v1 = temp;
            }
          
            // cos of the angle between the light vector and the normal vector
            // intensity of the color
            /*float nl1 = MathUtilities.ComputeNDotL(v1.WorldCoordinates, v1.Normal, lightPos);
            float nl2 = MathUtilities.ComputeNDotL(v2.WorldCoordinates, v2.Normal, lightPos);
            float nl3 = MathUtilities.ComputeNDotL(v3.WorldCoordinates, v3.Normal, lightPos);*/

            var data = new ScanLineData { };

            Vector3 surfaceNormalNorm = (v1.Normal + v2.Normal + v3.Normal) / 3;
            surfaceNormalNorm.Normalize();

  

            float dP1P2, dP1P3;//slopes  http://en.wikipedia.org/wiki/Slope
            if (v2.Coordinates.Y - v1.Coordinates.Y > 0)
                dP1P2 = (v2.Coordinates.X - v1.Coordinates.X) / (v2.Coordinates.Y - v1.Coordinates.Y);
            else
                dP1P2 = 0;
            if (v3.Coordinates.Y - v1.Coordinates.Y > 0)
                dP1P3 = (v3.Coordinates.X - v1.Coordinates.X) / (v3.Coordinates.Y - v1.Coordinates.Y);
            else
                dP1P3 = 0;

            // P1
            //      P2
            // P3
            if (dP1P2 > dP1P3)
            {
                for (int y = (int)v1.Coordinates.Y; y <= (int)v3.Coordinates.Y; y++)
                {
                    float gradient = (y - v1.Coordinates.Y) / (v3.Coordinates.Y - v1.Coordinates.Y);
                    data.currentY = y;
                    data.currentYWorld = (int)MathUtilities.Interpolate(v1.WorldCoordinates.Y, v3.WorldCoordinates.Y, gradient);
                    if (y < v2.Coordinates.Y)
                    {
                        /*data.ndotla = nl1;
                        data.ndotlb = nl3;
                        data.ndotlc = nl1;
                        data.ndotld = nl2;*/
                        data.ua = v1.TextureCoordinates.X;
                        data.ub = v3.TextureCoordinates.X;
                        data.uc = v1.TextureCoordinates.X;
                        data.ud = v2.TextureCoordinates.X;

                        data.va = v1.TextureCoordinates.Y;
                        data.vb = v3.TextureCoordinates.Y;
                        data.vc = v1.TextureCoordinates.Y;
                        data.vd = v2.TextureCoordinates.Y;
                        DrawScanLine(data, v1, v3, v1, v2, surfaceNormalNorm, false, material, texture);
                    }
                    else
                    {
                        /*data.ndotla = nl1;
                        data.ndotlb = nl3;
                        data.ndotlc = nl2;
                        data.ndotld = nl3;*/
                        data.ua = v1.TextureCoordinates.X;
                        data.ub = v3.TextureCoordinates.X;
                        data.uc = v2.TextureCoordinates.X;
                        data.ud = v3.TextureCoordinates.X;

                        data.va = v1.TextureCoordinates.Y;
                        data.vb = v3.TextureCoordinates.Y;
                        data.vc = v2.TextureCoordinates.Y;
                        data.vd = v3.TextureCoordinates.Y;

                        DrawScanLine(data, v1, v3, v2, v3, surfaceNormalNorm, true, material, texture);
                    }
                }
            }
            //      P1
            // P2
            //      P3
            else
            {
                for (int y = (int)v1.Coordinates.Y; y <= (int)v3.Coordinates.Y; y++)
                {
                    float gradient = (y - v1.Coordinates.Y) / (v3.Coordinates.Y - v1.Coordinates.Y);
                    data.currentY = y;
                    data.currentYWorld = (int)MathUtilities.Interpolate(v1.WorldCoordinates.Y, v3.WorldCoordinates.Y, gradient);
                    if (y < v2.Coordinates.Y)
                    {
                        /*data.ndotla = nl1;
                        data.ndotlb = nl2;
                        data.ndotlc = nl1;
                        data.ndotld = nl3;*/
                        data.ua = v1.TextureCoordinates.X;
                        data.ub = v2.TextureCoordinates.X;
                        data.uc = v1.TextureCoordinates.X;
                        data.ud = v3.TextureCoordinates.X;

                        data.va = v1.TextureCoordinates.Y;
                        data.vb = v2.TextureCoordinates.Y;
                        data.vc = v1.TextureCoordinates.Y;
                        data.vd = v3.TextureCoordinates.Y;
                        DrawScanLine(data, v1, v2, v1, v3, surfaceNormalNorm, false, material, texture);
                    }
                    else
                    {
                        /*data.ndotla = nl2;
                        data.ndotlb = nl3;
                        data.ndotlc = nl1;
                        data.ndotld = nl3;*/
                        data.ua = v2.TextureCoordinates.X;
                        data.ub = v3.TextureCoordinates.X;
                        data.uc = v1.TextureCoordinates.X;
                        data.ud = v3.TextureCoordinates.X;

                        data.va = v2.TextureCoordinates.Y;
                        data.vb = v3.TextureCoordinates.Y;
                        data.vc = v1.TextureCoordinates.Y;
                        data.vd = v3.TextureCoordinates.Y;
                        DrawScanLine(data, v2, v3, v1, v3, surfaceNormalNorm, true, material, texture);
                    }
                }
            }
        }
    }
}

public enum Shading { none, flat, gouraud, phong }
public enum RenderMode { points, wireframe, solid, textured }