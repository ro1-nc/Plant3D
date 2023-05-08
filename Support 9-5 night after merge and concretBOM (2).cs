using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project1.Support2D
{
    public class Pt3D
    {
        public double X { get; set; }

        public double Y { get; set; }

        public double Z { get; set; }

        public Pt3D()
        {
        }
        public Pt3D(Pt3D Pt)
        {
            X = Pt.X;
            Y = Pt.Y;
            Z = Pt.Z;
        }
    }

    public class MinPtDist
    {
        public Pt3D MinPt { get; set; }
        public double  Mindist { get; set; }
        public double Angle { get; set; }
        public MinPtDist()
        {
            MinPt = new Pt3D();
            MinPt.X = 0;
            MinPt.Y = 0;
            MinPt.Z = 0;

            Mindist = 0.0;

            Angle = 0;
        }
    }

    public class SupportData
    {
        public SupportData()
        {
            Quantity = 0;
        }
        public string SupportType { get; set; }
        public int Quantity { get; set; }
        public string Name { get; set; }

        public List<SupporSpecData> ListConcreteData = new List<SupporSpecData>();
        public List<SupporSpecData> ListPrimarySuppo = new List<SupporSpecData>();
        public List<SupporSpecData> ListSecondrySuppo = new List<SupporSpecData>();
    }


    public enum AngleUnit
    {
        Radiant,
        Degrees
    }
    public enum AxisSequence
    {
        ZYX,
        ZYZ,
        XYZ
    }

    public class CalculationMaths
    {
        public double[] RotM2Eul(double[,] R, AxisSequence sequence = AxisSequence.ZYX, AngleUnit angleUnit = AngleUnit.Radiant)
        {
            if (R.GetLength(0) != 3 && R.GetLength(1) != 3)
                throw new ArgumentOutOfRangeException("The rotation matrix R must have 3x3 elements.");
            double[] eul = new double[3];
            int firstAxis = 0;
            bool repetition = false;
            int parity = 0;
            int i = 0;
            int j = 0;
            int k = 0;
            int[] nextAxis = { 2, 3, 1, 2 };
            switch (sequence)
            {
                case AxisSequence.ZYX:
                    firstAxis = 1;
                    repetition = false;
                    parity = 0;
                    break;
                case AxisSequence.XYZ:
                    firstAxis = 3;
                    repetition = false;
                    parity = 1;
                    break;
                case AxisSequence.ZYZ:
                    firstAxis = 3;
                    repetition = true;
                    parity = 1;
                    break;
                default:
                    break;
            }
            i = firstAxis - 1;
            j = nextAxis[i + parity] - 1;
            k = nextAxis[i - parity + 1] - 1;
            if (repetition)
            {
                double sy = Math.Sqrt(R[i, j] * R[i, j] + R[i, k] * R[i, k]);
                bool singular = sy < 10 * Double.Epsilon;
                eul[0] = Math.Atan2(R[i, j], R[i, k]);
                eul[1] = Math.Atan2(sy, R[i, i]);
                eul[2] = Math.Atan2(R[j, i], -R[k, i]);
                if (singular)
                {
                    eul[0] = Math.Atan2(-R[j, k], R[j, j]);
                    eul[1] = Math.Atan2(sy, R[i, i]);
                    eul[2] = 0;
                }
            }
            else
            {
                double sy = Math.Sqrt(R[i, i] * R[i, i] + R[j, i] * R[j, i]);
                bool singular = sy < 10 * double.Epsilon;
                eul[0] = Math.Atan2(R[k, j], R[k, k]);
                eul[1] = Math.Atan2(-R[k, i], sy);
                eul[2] = Math.Atan2(R[j, i], R[i, i]);
                if (singular)
                {
                    eul[0] = Math.Atan2(-R[j, k], R[j, j]);
                    eul[1] = Math.Atan2(-R[k, i], sy);
                    eul[2] = 0;
                }
            }
            if (parity == 1)
            {
                eul[0] = -eul[0];
                eul[1] = -eul[1];
                eul[2] = -eul[2];
            }

            double value0 = eul[0];
            double value2 = eul[2];
            eul[0] = value2;
            eul[2] = value0;
            if (angleUnit == AngleUnit.Degrees)
            {
                eul[0] *= (180 / Math.PI);
                eul[1] *= (180 / Math.PI);
                eul[2] *= (180 / Math.PI);
            }
            return eul;
        }

        public double DistPoint(Pt3D Pt1, Pt3D Pt2)
        {
            return Math.Sqrt(((Pt2.X - Pt1.X) * (Pt2.X - Pt1.X)) + ((Pt2.Y - Pt1.Y) * (Pt2.Y - Pt1.Y)) + ((Pt2.Z - Pt1.Z) * (Pt2.Z - Pt1.Z)));
        }


        public double GetSignedRotation(System.Windows.Media.Media3D.Vector3D Vec1, System.Windows.Media.Media3D.Vector3D Vec2, System.Windows.Media.Media3D.Vector3D Vec3)
        {
            if (Vec1 == null || Vec2 == null)
                return 0;
            if (Vec1.X == 0 && Vec1.Y == 0 && Vec1.Z == 0)
                return 0;
            if (Vec2.X == 0 && Vec2.Y == 0 && Vec2.Z == 0)
                return 0;

            Vec1.Normalize();
            Vec2.Normalize();

            double dot = System.Windows.Media.Media3D.Vector3D.DotProduct(Vec1, Vec2);

            System.Windows.Media.Media3D.Vector3D cross = System.Windows.Media.Media3D.Vector3D.CrossProduct(Vec1, Vec2);

            Vec3 = new System.Windows.Media.Media3D.Vector3D(1, 0, 0);


            double angle = Math.Atan2(cross.Length * Math.Sign(System.Windows.Media.Media3D.Vector3D.DotProduct(cross, Vec3)), dot);

            if (angle.Equals(0))
            {
                Vec3 = new System.Windows.Media.Media3D.Vector3D(0, 1, 0);


                angle = Math.Atan2(cross.Length * Math.Sign(System.Windows.Media.Media3D.Vector3D.DotProduct(cross, Vec3)), dot);

                if (angle.Equals(0))
                {
                    Vec3 = new System.Windows.Media.Media3D.Vector3D(0, 0, 1);


                    angle = Math.Atan2(cross.Length * Math.Sign(System.Windows.Media.Media3D.Vector3D.DotProduct(cross, Vec3)), dot);
                }
            }

            return angle;
        }


        public double ConvertRadiansToDegrees(double radians)
        {
            double degrees = (180 / Math.PI) * radians;
            return (degrees);
        }
    }

    public class SupporSpecData
    {
        public List<string> ListtouchingParts { get; set; }
        public Pt3D GlobalPos { get; set; }

        public Pt3D Centroid { get; set; }

        public Pt3D Boundingboxmin { get; set; }

        public Pt3D Boundingboxmax { get; set; }

        public DirectionVec Directionvec { get; set; }

        public double Volume { get; set; }

        public Pt3D BoxData { get; set; }

        // in SuppoId first Charater is its type and rest is their No P= Primary , S=Secondary, C= Concreate.
        public string SuppoId { get; set; }

        // Storing the size of Secondary support
        public string Size { get; set; }

        public Angles Angle { get; set; }

        /// <summary>
        ///  Most Probably the Below variable used to store and get Primary support Name
        /// </summary>
        public string SupportName { get; set; }

        //It is used in 2d support Mostaly for Secondary Sypport 
        // Ver = vertical , Hor= Horizontal
        public string PartDirection { get; set; }

        public double[] StPt = new double[3];

        public double[] EndPt = new double[3];

        public Pt3D Midpoint = new Pt3D();

        //This used to solid3d plate data
        public List<FaceData> ListfaceData = new List<FaceData>();

        public Pt3D BottomPrim { get; set; }

        public bool IsSupportNB { get; set; }

        public Angles FaceLocalAngle { get; set; }

        public Autodesk.ProcessPower.PartsRepository.NominalDiameter NomianalDia { get; set; }

        public double PrimaryZhtNB { get; set; }

        public string TouchingPartid { get; set; }

        // Used to detect GRP FLG Part;
        public Pt3D NoramlDir { get; set; }

        /// <summary>
        ///  Only used for Plate to store the Postion 
        /// </summary>
        public Pt3D Position { get; set; }

        public SupporSpecData()
        {
            //Temporarily writing for the 
            Boundingboxmax = InitializetoZero();
            Boundingboxmin = InitializetoZero();
            IsSupportNB = false;
            IsAnchor = false;
            IsGussetplate = false;
            Size = "";
        }

        public Pt3D InitializetoZero()
        {
            Pt3D Data = new Pt3D();

            Data.X = 0;
            Data.Y = 0;
            Data.Z = 0;
            return Data;
        }

        public SupporSpecData(SupporSpecData SpData)
        {
            Angle = SpData.Angle;
            Boundingboxmax = SpData.Boundingboxmax;
            Boundingboxmin = SpData.Boundingboxmin;
            BottomPrim = SpData.BottomPrim;
            IsSupportNB = SpData.IsSupportNB;
        }
        //Call This method when you have Bounding Box
        // This method gets the bounding box length, width, height and stores in box data of same object
        public void CalculateDist()
        {
            BoxData = new Pt3D();
            BoxData.X = Math.Abs(Boundingboxmax.X - Boundingboxmin.X);
            BoxData.Y = Math.Abs(Boundingboxmax.Y - Boundingboxmin.Y);
            BoxData.Z = Math.Abs(Boundingboxmax.Z - Boundingboxmin.Z);
        }


        //Call This method when you have Bounding Box
        // This method gets the Centroid by using the bounding box and stores in  Centroid property of same object
        public void CalculateCentroid()
        {

            Centroid = new Pt3D();
            Centroid.X = /*Math.Abs*/(Boundingboxmax.X + Boundingboxmin.X) / 2;
            Centroid.Y = /*Math.Abs*/(Boundingboxmax.Y + Boundingboxmin.Y) / 2;
            Centroid.Z = /*Math.Abs*/(Boundingboxmax.Z + Boundingboxmin.Z) / 2;
        }

        //Call This method when you have Boxdata
        // This methode calculates volume by using BoxData call this mehode after calling box data
        public void CalculateVolume()
        {
            Volume = BoxData.X * BoxData.Y * BoxData.Z;
        }

        public double DistCenter { get; set; }

        public bool IsAnchor { get; set; }

        public int NoOfAnchoreHole { get; set; }

        public bool IsGussetplate { get; set; }
    }

    public class CircleData
    {
        public Pt3D Vector { get; set; }

        public Pt3D Center { get; set; }

        public Autodesk.AutoCAD.Geometry.Plane AcadPlane { get; set; }

        public CircleData()
        {
            Vector = new Pt3D();

            Center = new Pt3D();
        }

    }

    public class BodyData
    {
        public double Volume { get; set; }

        public List<FaceData> ListFaceData = new List<FaceData>();
    }

    public class FaceData
    {

        public Autodesk.AutoCAD.BoundaryRepresentation.Face AcadFace { get; set; }

        public double SurfaceArea { get; set; }

        public Autodesk.AutoCAD.Geometry.Vector3d FaceNormal { get; set; }

        public Autodesk.AutoCAD.Geometry.Point3d PtonPlane { get; set; }

        public bool IsPlannar { get; set; }

        public string IdLocal { get; set; }

        public bool IsMatching { get; set; }

        public Angles AngleData { get; set; }

        public DirectionVec Directionvecface { get; set; }

        public List<Edgeinfo> ListlinearEdge = new List<Edgeinfo>();


    }

    public class Edgeinfo
    {
        public double EdgeLength { get; set; }
        public Pt3D DirectionEdge { get; set; }
        public Pt3D StPt { get; set; }

        public Pt3D EndPt { get; set; }

        public Pt3D MidPoint { get; set; }

    }

    public class Angles
    {
        public double XinRadian { get; set; }

        public double YinRadian { get; set; }

        public double ZinRadian { get; set; }

        public double XinDegree { get; set; }

        public double YinDegree { get; set; }

        public double ZinDegree { get; set; }
    }


    public class DirectionVec
    {
        public Pt3D XDirVec { get; set; }

        public Pt3D YDirVec { get; set; }

        public Pt3D ZDirVec { get; set; }

    }

    public class SupportProcessedData
    {
        bool HasBottomSupport { get; set; }
        int BottomSupportCount { get; set; }

        SupportProcessedData()
        {
            HasBottomSupport = false;
            BottomSupportCount = 0;
        }
    }
}
