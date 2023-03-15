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
    }

    public class SupportData
    {
        public string SupportType { get; set; }
        public List<SupporSpecData> ListBottomPart = new List<SupporSpecData>();
        public List<SupporSpecData> ListPrimarySuppo= new List<SupporSpecData>();
        public List<SupporSpecData> ListSecondrySuppo = new List<SupporSpecData>();
    }


    public class SupporSpecData
    {
        public Pt3D GlobalPos { get; set; }

        public Pt3D Centroid { get; set; }

        public Pt3D Boundingboxmin { get; set; }

        public Pt3D Boundingboxmax { get; set; }

        public DirectionVec Directionvec { get; set; }

        public double Volume { get; set; }

        public Pt3D BoxData { get; set; }

        public SupporSpecData()
        {
            //Temporarily writing for the 
            Boundingboxmax = InitializetoZero();
            Boundingboxmin = InitializetoZero();
        }

        public Pt3D InitializetoZero()
        {
            Pt3D Data= new Pt3D();

            Data.X = 0;
            Data.Y = 0;
            Data.Z = 0;
            return Data;
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
            Centroid.X = Math.Abs(Boundingboxmax.X + Boundingboxmin.X)/2;
            Centroid.Y = Math.Abs(Boundingboxmax.Y + Boundingboxmin.Y)/2;
            Centroid.Z = Math.Abs(Boundingboxmax.Z + Boundingboxmin.Z)/2;
        }

        //Call This method when you have Boxdata
        // This methode calculates volume by using BoxData call this mehode after calling box data
        public void CalculateVolume()
        {
            Volume = BoxData.X * BoxData.Y * BoxData.Z;
        }
    }

    public class DirectionVec
    {
        public Pt3D XDirVec { get; set;}

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
