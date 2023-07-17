using System.IO;
using System.Xml;

using Assimp;

using System.Drawing;
using Matrix4x4 = System.Numerics.Matrix4x4;
using System.Numerics;

namespace ConsoleApp_DAE
{
    internal class Program
    {
        static private byte SigToByte(float n)
        {
            return n == 0 ? (byte) 0 : (byte) (n * 256 - 1);
        }
        static private byte[] RgbaToArgb(Color4D RGBA)
        {
            return new[] { SigToByte(RGBA.A), SigToByte(RGBA.R), SigToByte(RGBA.G), SigToByte(RGBA.B)};
        }
        static void Main(string[] args)
        {
            #pragma warning disable CS8602 // Desreferência de uma referência possivelmente nula.
            #pragma warning disable CA1416 // Validar a compatibilidade da plataforma
            string path = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;

            // lê o arquivo DAE
            XmlDocument doc = new XmlDocument();
            doc.Load($"{path}\\assets\\Car_Sedan_White_v4.dae");

            // Separa as Tags de Interesse
            XmlNodeList nodeList = doc.GetElementsByTagName("float_array");
            List<string> stringList = new List<string>();
            foreach (XmlNode node in nodeList)
            {
                stringList.Add(node.InnerText);
            }

            // Separa os vertices
            List<string> Vertices = new List<string>();
            foreach (string floatArray in stringList)
            {
                // o replace colocado "_" para que os vertices não seja separados pelo Split
                string[] linhasXYZ = floatArray.Replace(' ', '_').Split('\n');
                foreach (string linhaXYZ in linhasXYZ)
                {
                    Vertices.Add(linhaXYZ);
                }
            }

            // identifica as dimenções do objeto
            Double Xmax = 0;
            Double Xmin = 0;
            Double Ymax = 0;
            Double Ymin = 0;
            Double Zmax = 0;
            Double Zmin = 0;

            List<string> Vertices3D = new List<string>();
            foreach (string Vertice in Vertices)
            {
                // remove o "_" para que as coordenadas sejam separadas
                string[] Coords = Vertice.Replace('_', ' ').Replace('.', ',').Split(' ');

                // considera somente os vertices tridimencionais
                if (Coords.Length == 3)
                {
                    // recupera o vertice
                    Double X = Convert.ToDouble(Coords[0]);
                    Double Y = Convert.ToDouble(Coords[1]);
                    Double Z = Convert.ToDouble(Coords[2]);

                    // verifica se são considerados para identificar os extremos do objeto
                    if ((Xmax == 0) || (X > Xmax)) Xmax = X;
                    if ((Xmin == 0) || (X < Xmin)) Xmin = X;
                    if ((Ymax == 0) || (Y > Ymax)) Ymax = Y;
                    if ((Ymin == 0) || (Y < Ymin)) Ymin = Y;
                    if ((Zmax == 0) || (Z > Zmax)) Zmax = Z;
                    if ((Zmin == 0) || (Z > Zmin)) Zmin = Z;
                }
            }

            // de posse dos extremos, calcula-se o centro, que será a coordenada para que todas as cameras serão apontadas
            Double Xcenter = (Xmin + Xmax) / 2;
            Double Ycenter = (Ymin + Ymax) / 2;
            Double Zcenter = (Zmin + Zmax) / 2;


            // este valor precisa ser testado e define a distancia da camera do objeto, como um fator multiplicador do raio plano (XY) do objeto
            Double Fator = 3.0;

            //calculo do raio já aplicando o fator
            Double Lx = Xmax - Xmin;
            Double Ly = Ymax - Ymin;
            Double Base = Math.Sqrt(Lx * Lx + Ly * Ly);
            Double Raio = Base * Fator;

            Double Inclinacao = 30;
            Double AlturaMaxima = Raio * Math.Sin(Inclinacao * (Math.PI / 180)); // altura máxima no ângulo de 30 graus

            List<Tuple<int, double, double, double>> coordenadas = new List<Tuple<int, double, double, double>>();
            for (int anguloGraus = 0; anguloGraus < 360; anguloGraus += 15)
            {
                double anguloRadianos = anguloGraus * (Math.PI / 180);

                // Oscilar a altura com base no cosseno do ângulo, variando de 0 (no ângulo 90 e 270) a alturaMaxima (no ângulo 0 e 180)
                double z = AlturaMaxima * Math.Abs(Math.Cos(anguloRadianos)) + Zcenter;

                // Calcular o raio projetado no plano XY usando o teorema de Pitágoras
                double raioProjetado = Math.Sqrt(Math.Pow(Raio, 2) - Math.Pow(z, 2));

                double x = raioProjetado * Math.Cos(anguloRadianos) + Xcenter;
                double y = raioProjetado * Math.Sin(anguloRadianos) + Ycenter;

                coordenadas.Add(new Tuple<int, double, double, double>(anguloGraus, x, y, z));
            }
            coordenadas.Add(new Tuple<int, double, double, double>(360, Raio + Xcenter, Ycenter, Zcenter));
            coordenadas.Add(new Tuple<int, double, double, double>(361, Raio + Xcenter, Ycenter, Zcenter));

            // Imprimir as coordenadas
            string Centro = String.Format(" X: {0,10:F2} Y: {1,10:F2} Z: {2,10:F2}", Xcenter, Ycenter, Zcenter);
            Console.WriteLine("Centro da Figura: " + Centro);
            foreach (var coordenada in coordenadas)
            {
                string coordenadaFormatada = String.Format("Angulo: {0,10} X: {1,10:F2} Y: {2,10:F2} Z: {3,10:F2}", coordenada.Item1, coordenada.Item2, coordenada.Item3, coordenada.Item4);
                Console.WriteLine(coordenadaFormatada);
            }


            var importer = new AssimpContext();
            var scene = importer.ImportFile($"{path}\\assets\\Car_Sedan_White_v4.dae", PostProcessPreset.TargetRealTimeMaximumQuality);

            int width = 800; // Set the desired width of the output image
            int height = 600; // Set the desired height of the output image
            Bitmap image = new Bitmap(width, height);
            Graphics graphics = Graphics.FromImage(image);

            // Set up the orthographic camera parameters
            double left = Ymin; // Set the left boundary of the camera viewport
            double right = Ymax; // Set the right boundary of the camera viewport
            double bottom = Xmin; // Set the bottom boundary of the camera viewport
            double top = Xmax; // Set the top boundary of the camera viewport
            double near = 0.1; // Set the near clipping plane distance
            double far = 10000; // Set the far clipping plane distance

            var coord = coordenadas[3];


            Vector3 cameraPosition = new Vector3((float)coord.Item2, (float)coord.Item3, (float)coord.Item4);
            Vector3 cameraTarget = new Vector3((float)Xcenter, (float)Ycenter, (float)Zcenter);
            Vector3 up = new Vector3(0.0f, -1.0f, 0.0f);

            Matrix4x4 projectionMatrix = Matrix4x4.CreateOrthographicOffCenter((float)left, (float)right, (float)bottom, (float)top, (float)near, (float)far);
            Matrix4x4 view = Matrix4x4.CreateLookAt(cameraPosition, cameraTarget, up);
            var scale = Matrix4x4.CreateScale(1.5f,1f,1f);
            scale.Translation = new Vector3(width/2, 450f, 1f);

            Matrix4x4 TMatrix = projectionMatrix * view * scale;


            // Apply the projection matrix to the Graphics object
            graphics.Transform = new System.Drawing.Drawing2D.Matrix(TMatrix.M11, TMatrix.M12,
                                                                    TMatrix.M21, TMatrix.M22,
                                                                    TMatrix.M41, TMatrix.M42);


            foreach (var mesh in scene.Meshes)
            {
                foreach (var face in mesh.Faces)
                {
                    if (face.IndexCount == 3)
                    {
                        // Get the vertices for the current face
                        Vector3D vertex1 = mesh.Vertices[face.Indices[0]];
                        Vector3D vertex2 = mesh.Vertices[face.Indices[1]];
                        Vector3D vertex3 = mesh.Vertices[face.Indices[2]];

                        //// Use the Graphics object to draw the face onto the rendering surface
                        //graphics.DrawPolygon(Pens.Black, new[] {
                        //    new System.Drawing.PointF((vertex1.X) * 200,(vertex1.Y) * 200),
                        //    new System.Drawing.PointF((vertex2.X) * 200,(vertex2.Y) * 200),
                        //    new System.Drawing.PointF((vertex3.X) * 200,(vertex3.Y) * 200),
                        //});

                        if (mesh.MaterialIndex == 3)
                        {
                            // 3 -> Lataria (Branco)
                            // 0 -> Azul
                            // 4 -> Preto
                            mesh.MaterialIndex = 0;
                        }

                        Color4D rgba = scene.Materials[mesh.MaterialIndex].ColorDiffuse;
                        byte[] argb = RgbaToArgb(rgba);

                        // Use the Graphics object to draw the face onto the rendering surface
                        graphics.FillPolygon(new SolidBrush(System.Drawing.Color.FromArgb(argb[0], argb[1], argb[2], argb[3])), new[] {
                            new System.Drawing.PointF((vertex1.X) * 200,(vertex1.Y) * 200),
                            new System.Drawing.PointF((vertex2.X) * 200,(vertex2.Y) * 200),
                            new System.Drawing.PointF((vertex3.X) * 200,(vertex3.Y) * 200),
                        });

                    }
                }
            }

            image.Save($"{path}\\output.jpg");
        }
    }
}
