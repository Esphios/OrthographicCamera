using System.Numerics;
using System.Xml;
using Assimp;

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
            float Xmax = 0;
            float Xmin = 0;
            float Ymax = 0;
            float Ymin = 0;
            float Zmax = 0;
            float Zmin = 0;

            List<Vector3> Vertices3D = new List<Vector3>();
            foreach (string Vertice in Vertices)
            {
                // remove o "_" para que as coordenadas sejam separadas
                string[] Coords = Vertice.Replace('_', ' ').Replace('.', ',').Split(' ');

                // considera somente os vertices tridimencionais
                if (Coords.Length == 3)
                {
                    // recupera o vertice
                    float X = (float)Convert.ToDouble(Coords[0]);
                    float Y = (float)Convert.ToDouble(Coords[1]);
                    float Z = (float)Convert.ToDouble(Coords[2]);

                    Vertices3D.Add(new Vector3(X,Y,Z));

                    // verifica se são considerados para identificar os extremos do objeto
                    if ((Xmax == 0) || (X > Xmax)) Xmax = X;
                    if ((Xmin == 0) || (X < Xmin)) Xmin = X;
                    if ((Ymax == 0) || (Y > Ymax)) Ymax = Y;
                    if ((Ymin == 0) || (Y < Ymin)) Ymin = Y;
                    if ((Zmax == 0) || (Z > Zmax)) Zmax = Z;
                    if ((Zmin == 0) || (Z < Zmin)) Zmin = Z;
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
        }
    }
}
