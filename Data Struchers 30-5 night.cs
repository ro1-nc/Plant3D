using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;

namespace Project1.Support2D
{


    public class BoundingBox
    {
        public double MinX { get; set; }
        public double MinY { get; set; }
        public double MaxX { get; set; }
        public double MaxY { get; set; }
    }






    public class EntityData
    {
        public GeometRi.Segment3d Segment { get; set; }
        public string IDSegment { get; set; }
    }

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
        public double Mindist { get; set; }
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


    public class Projection2DData
    {


        public List<FaceData> ListfaceData = new List<FaceData>();

        public List<string> TouchingPart = new List<string>();

        public string SectionDir { get; set; }
        public string SectionID { get; set; }



    }

    public class AngleDt
    {
        public double AngleSize { get; set; }

        public double AngleThck { get; set; }

        public double Length { get; set; }

        public AngleDt()
        {
            AngleSize = 0;
            AngleThck = 0;
            Length = 0;
        }
    }

    public class BoundingBoxFace
    {
        public List<BoundingBoxEdge> Edges { get; set; }
        public Point3D FacePoint { get; set; }
        public Vector3D Normal { get; set; }
    }

    public class BoundingBoxEdge
    {
        public Point3D StartPoint { get; set; }
        public Point3D EndPoint { get; set; }
    }

    public class CalculationMaths
    {
        public double[] FindMinMaxCoordinates(List<GeometRi.Segment3d> segments, out double minX, out double minY, out double maxX, out double maxY)
        {
            minX = double.MaxValue;
            minY = double.MaxValue;
            maxX = double.MinValue;
            maxY = double.MinValue;

            foreach (var segment in segments)
            {
                minX = Math.Min(minX, Math.Min(segment.P1.X, segment.P2.X));
                minY = Math.Min(minY, Math.Min(segment.P1.Y, segment.P2.Y));
                maxX = Math.Max(maxX, Math.Max(segment.P1.X, segment.P2.X));
                maxY = Math.Max(maxY, Math.Max(segment.P1.Y, segment.P2.Y));
            }
            double[] result = new double[] { minX, minY, maxX, maxY };
            return result;
        }
        public bool BoundingBoxIntersectsList(List<GeometRi.Segment3d> PartToCheck, List<List<GeometRi.Segment3d>> AllParts)
        {
            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double maxX = double.MinValue;
            double maxY = double.MinValue;
            double[] MinMaxLOngSecCoord = FindMinMaxCoordinates(PartToCheck, out minX, out minY, out maxX, out maxY);

            BoundingBox boundingBox = new BoundingBox();
            boundingBox.MinX = MinMaxLOngSecCoord[0];
            boundingBox.MinY = MinMaxLOngSecCoord[1];
            boundingBox.MaxX = MinMaxLOngSecCoord[2];
            boundingBox.MaxY = MinMaxLOngSecCoord[3];

            List<BoundingBox> boundingBoxes = new List<BoundingBox>();
            foreach (var part in AllParts)
            {
                minX = double.MaxValue;
                minY = double.MaxValue;
                maxX = double.MinValue;
                maxY = double.MinValue;
                double[] MinMaxCoord = FindMinMaxCoordinates(part, out minX, out minY, out maxX, out maxY);

                BoundingBox boundingBoxPart = new BoundingBox();
                boundingBoxPart.MinX = MinMaxLOngSecCoord[0];
                boundingBoxPart.MinY = MinMaxLOngSecCoord[1];
                boundingBoxPart.MaxX = MinMaxLOngSecCoord[2];
                boundingBoxPart.MaxY = MinMaxLOngSecCoord[3];
                boundingBoxes.Add(boundingBoxPart);
            }

            foreach (var box in boundingBoxes)
            {
                if (BoundingBoxesIntersect(boundingBox, box))
                    return true;
            }

            return false;
        }

        public bool BoundingBoxesIntersect(BoundingBox box1, BoundingBox box2)
        {
            if (box1.MaxX < box2.MinX || box1.MinX > box2.MaxX)
                return false;

            if (box1.MaxY < box2.MinY || box1.MinY > box2.MaxY)
                return false;

            return true;
        }



        public List<BoundingBoxFace> GetBoundingBoxFaces(Point3D topPoint, Point3D lowPoint)
        {
            List<BoundingBoxFace> faces = new List<BoundingBoxFace>();

            // Calculate the face points and normals

            // Bottom face
            BoundingBoxFace bottomFace = new BoundingBoxFace();
            bottomFace.Edges = new List<BoundingBoxEdge>
        {
            new BoundingBoxEdge { StartPoint = new Point3D { X = lowPoint.X, Y = lowPoint.Y, Z = lowPoint.Z }, EndPoint = new Point3D { X = topPoint.X, Y = lowPoint.Y, Z = lowPoint.Z } },
            new BoundingBoxEdge { StartPoint = new Point3D { X = topPoint.X, Y = lowPoint.Y, Z = lowPoint.Z }, EndPoint = new Point3D { X = topPoint.X, Y = topPoint.Y, Z = lowPoint.Z } },
            new BoundingBoxEdge { StartPoint = new Point3D { X = topPoint.X, Y = topPoint.Y, Z = lowPoint.Z }, EndPoint = new Point3D { X = lowPoint.X, Y = topPoint.Y, Z = lowPoint.Z } },
            new BoundingBoxEdge { StartPoint = new Point3D { X = lowPoint.X, Y = topPoint.Y, Z = lowPoint.Z }, EndPoint = new Point3D { X = lowPoint.X, Y = lowPoint.Y, Z = lowPoint.Z } }
        };
            bottomFace.FacePoint = new Point3D { X = (lowPoint.X + topPoint.X) / 2, Y = (lowPoint.Y + topPoint.Y) / 2, Z = lowPoint.Z };
            bottomFace.Normal = new Vector3D { X = 0, Y = 0, Z = -1 };
            faces.Add(bottomFace);

            // Top face
            BoundingBoxFace topFace = new BoundingBoxFace();
            topFace.Edges = new List<BoundingBoxEdge>
        {
            new BoundingBoxEdge { StartPoint = new Point3D { X = lowPoint.X, Y = lowPoint.Y, Z = topPoint.Z }, EndPoint = new Point3D { X = topPoint.X, Y = lowPoint.Y, Z = topPoint.Z } },
            new BoundingBoxEdge { StartPoint = new Point3D { X = topPoint.X, Y = lowPoint.Y, Z = topPoint.Z }, EndPoint = new Point3D { X = topPoint.X, Y = topPoint.Y, Z = topPoint.Z } },
            new BoundingBoxEdge { StartPoint = new Point3D { X = topPoint.X, Y = topPoint.Y, Z = topPoint.Z }, EndPoint = new Point3D { X = lowPoint.X, Y = topPoint.Y, Z = topPoint.Z } },
            new BoundingBoxEdge { StartPoint = new Point3D { X = lowPoint.X, Y = topPoint.Y, Z = topPoint.Z }, EndPoint = new Point3D { X = lowPoint.X, Y = lowPoint.Y, Z = topPoint.Z } }
        };
            topFace.FacePoint = new Point3D { X = (lowPoint.X + topPoint.X) / 2, Y = (lowPoint.Y + topPoint.Y) / 2, Z = topPoint.Z };
            topFace.Normal = new Vector3D { X = 0, Y = 0, Z = 1 };
            faces.Add(topFace);

            // Front face
            BoundingBoxFace frontFace = new BoundingBoxFace();
            frontFace.Edges = new List<BoundingBoxEdge>
        {
            new BoundingBoxEdge { StartPoint = new Point3D { X = lowPoint.X, Y = lowPoint.Y, Z = topPoint.Z }, EndPoint = new Point3D { X = topPoint.X, Y = lowPoint.Y, Z = topPoint.Z } },
            new BoundingBoxEdge { StartPoint = new Point3D { X = topPoint.X, Y = lowPoint.Y, Z = topPoint.Z }, EndPoint = new Point3D { X = topPoint.X, Y = lowPoint.Y, Z = lowPoint.Z } },
            new BoundingBoxEdge { StartPoint = new Point3D { X = topPoint.X, Y = lowPoint.Y, Z = lowPoint.Z }, EndPoint = new Point3D { X = lowPoint.X, Y = lowPoint.Y, Z = lowPoint.Z } },
            new BoundingBoxEdge { StartPoint = new Point3D { X = lowPoint.X, Y = lowPoint.Y, Z = lowPoint.Z }, EndPoint = new Point3D { X = lowPoint.X, Y = lowPoint.Y, Z = topPoint.Z } }
        };
            frontFace.FacePoint = new Point3D { X = (lowPoint.X + topPoint.X) / 2, Y = lowPoint.Y, Z = (lowPoint.Z + topPoint.Z) / 2 };
            frontFace.Normal = new Vector3D { X = 0, Y = -1, Z = 0 };
            faces.Add(frontFace);

            // Back face
            BoundingBoxFace backFace = new BoundingBoxFace();
            backFace.Edges = new List<BoundingBoxEdge>
        {
            new BoundingBoxEdge { StartPoint = new Point3D { X = lowPoint.X, Y = topPoint.Y, Z = topPoint.Z }, EndPoint = new Point3D { X = topPoint.X, Y = topPoint.Y, Z = topPoint.Z } },
            new BoundingBoxEdge { StartPoint = new Point3D { X = topPoint.X, Y = topPoint.Y, Z = topPoint.Z }, EndPoint = new Point3D { X = topPoint.X, Y = topPoint.Y, Z = lowPoint.Z } },
            new BoundingBoxEdge { StartPoint = new Point3D { X = topPoint.X, Y = topPoint.Y, Z = lowPoint.Z }, EndPoint = new Point3D { X = lowPoint.X, Y = topPoint.Y, Z = lowPoint.Z } },
            new BoundingBoxEdge { StartPoint = new Point3D { X = lowPoint.X, Y = topPoint.Y, Z = lowPoint.Z }, EndPoint = new Point3D { X = lowPoint.X, Y = topPoint.Y, Z = topPoint.Z } }
        };
            backFace.FacePoint = new Point3D { X = (lowPoint.X + topPoint.X) / 2, Y = lowPoint.Y, Z = (lowPoint.Z + topPoint.Z) / 2 };
            backFace.Normal = new Vector3D { X = 0, Y = 1, Z = 0 };
            faces.Add(backFace);

            // Left face
            BoundingBoxFace leftFace = new BoundingBoxFace();
            leftFace.Edges = new List<BoundingBoxEdge>
        {
            new BoundingBoxEdge { StartPoint = new Point3D { X = topPoint.X, Y = lowPoint.Y, Z = lowPoint.Z }, EndPoint = new Point3D { X = topPoint.X, Y = lowPoint.Y, Z = topPoint.Z } },
            new BoundingBoxEdge { StartPoint = new Point3D { X = topPoint.X, Y = lowPoint.Y, Z = topPoint.Z }, EndPoint = new Point3D { X = topPoint.X, Y = topPoint.Y, Z = topPoint.Z } },
            new BoundingBoxEdge { StartPoint = new Point3D { X = topPoint.X, Y = topPoint.Y, Z = topPoint.Z }, EndPoint = new Point3D { X = topPoint.X, Y = topPoint.Y, Z = lowPoint.Z } },
            new BoundingBoxEdge { StartPoint = new Point3D { X = topPoint.X, Y = topPoint.Y, Z = lowPoint.Z }, EndPoint = new Point3D { X = topPoint.X, Y = lowPoint.Y, Z = lowPoint.Z } }
        };
            leftFace.FacePoint = new Point3D { X = lowPoint.X, Y = (lowPoint.Y + topPoint.Y) / 2, Z = (lowPoint.Z + topPoint.Z) / 2 };
            leftFace.Normal = new Vector3D { X = -1, Y = 0, Z = 0 };
            faces.Add(leftFace);

            // Right face
            BoundingBoxFace rightFace = new BoundingBoxFace();
            rightFace.Edges = new List<BoundingBoxEdge>
        {
            new BoundingBoxEdge { StartPoint = new Point3D { X = lowPoint.X, Y = lowPoint.Y, Z = lowPoint.Z }, EndPoint = new Point3D { X = lowPoint.X, Y = lowPoint.Y, Z = topPoint.Z } },
            new BoundingBoxEdge { StartPoint = new Point3D { X = lowPoint.X, Y = lowPoint.Y, Z = topPoint.Z }, EndPoint = new Point3D { X = lowPoint.X, Y = topPoint.Y, Z = topPoint.Z } },
            new BoundingBoxEdge { StartPoint = new Point3D { X = lowPoint.X, Y = topPoint.Y, Z = topPoint.Z }, EndPoint = new Point3D { X = lowPoint.X, Y = topPoint.Y, Z = lowPoint.Z } },
            new BoundingBoxEdge { StartPoint = new Point3D { X = lowPoint.X, Y = topPoint.Y, Z = lowPoint.Z }, EndPoint = new Point3D { X = lowPoint.X, Y = lowPoint.Y, Z = lowPoint.Z } }
        };
            rightFace.FacePoint = new Point3D { X = topPoint.X, Y = (lowPoint.Y + topPoint.Y) / 2, Z = (lowPoint.Z + topPoint.Z) / 2 };
            rightFace.Normal = new Vector3D { X = 1, Y = 0, Z = 0 };
            faces.Add(rightFace);

            return faces;
        }




        public double[] CalculateMaxHeightAndWidth(List<GeometRi.Segment3d> lines, out double maxHeight, out double maxWidth)
        {
            maxHeight = 0.0;
            maxWidth = 0.0;

            double MaxY = 0, MinY = 0, MinX = 0, MaxX = 0;

            int count = 0;
            foreach (var line in lines)
            {
                double height = Math.Abs(line.P2.Y - line.P1.Y);
                double width = Math.Abs(line.P2.X - line.P1.X);

                //if (height > maxHeight)
                //  maxHeight = height;

                //   if (width > maxWidth)
                //  maxWidth = width;

                count++;
            }

            maxHeight = MaxY - MinY;
            maxWidth = MaxX - MinX;
            return new double[] { maxHeight, maxWidth };
        }
        public List<EntityData> FitLinesInRectangle(List<EntityData> lines, double boxlen, double boxht, double locx, double locy)
        {
            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double maxX = double.MinValue;
            double maxY = double.MinValue;

            // Calculate the minimum and maximum coordinates of the lines
            foreach (var ent in lines)

            {
                GeometRi.Segment3d line = ent.Segment;

                minX = Math.Min(minX, Math.Min(line.P1.X, line.P2.X));
                minY = Math.Min(minY, Math.Min(line.P1.Y, line.P2.Y));
                maxX = Math.Max(maxX, Math.Max(line.P1.X, line.P2.X));
                maxY = Math.Max(maxY, Math.Max(line.P1.Y, line.P2.Y));
            }
            //double width = 0.0;
            //double height = 0.0;

            double width = maxX - minX;
            double height = maxY - minY;
            // var Rectmax = CalculateMaxHeightAndWidth(lines, out height, out width);

            // Calculate the width and height of the rectangle


            // Calculate the scale factors to fit the lines in the rectangle
            double scaleX = boxlen / width;//Rectmax[1];
            double scaleY = boxht / height; //Rectmax[0];
            double scale = Math.Min(scaleX, scaleY);

            // Calculate the translation values to position the lines in the desired location
            double translationX = locx - (minX + maxX) / 2 * scale;
            double translationY = locy - (minY + maxY) / 2 * scale;

            List<EntityData> result = new List<EntityData>();
            // Apply the scale factors and translation values to each line
            foreach (var ent in lines)
            {
                GeometRi.Segment3d line = ent.Segment;

                GeometRi.Segment3d line2 = new GeometRi.Segment3d(new GeometRi.Point3d(line.P1.X * scale + translationX, line.P1.Y * scale + translationY, 0), new GeometRi.Point3d(line.P2.X * scale + translationX, line.P2.Y * scale + translationY, 0));

                EntityData entd = new EntityData();
                entd.Segment = line2;
                entd.IDSegment = ent.IDSegment;
                result.Add(entd);
            }



            return result;
        }


        public Tuple<GeometRi.Point3d, GeometRi.Point3d> ProjectLineOntoXYPlane(GeometRi.Point3d startPoint, GeometRi.Point3d endPoint, Vector3D planeNormal)
        {
            // Determine the direction of the normal
            Vector3D normal = planeNormal;
            Vector3D xAxis = new Vector3D(1, 0, 0);
            Vector3D yAxis = new Vector3D(0, 1, 0);

            // Calculate the dot product between the normal and the X and Y axes
            double dotX = Vector3D.DotProduct(normal, xAxis);
            double dotY = Vector3D.DotProduct(normal, yAxis);

            // Determine the primary axis based on the dot products
            Vector3D primaryAxis = (Math.Abs(dotX) > Math.Abs(dotY)) ? xAxis : yAxis;

            // Project the line onto the XY plane based on the primary axis
            Point2D projectedStartPoint = GetProjectedPoint(new Point3D(startPoint.X, startPoint.Y, startPoint.Z), primaryAxis);
            Point2D projectedEndPoint = GetProjectedPoint(new Point3D(endPoint.X, endPoint.Y, endPoint.Z), primaryAxis);

            //return new Line2D(projectedStartPoint, projectedEndPoint);

            Tuple<GeometRi.Point3d, GeometRi.Point3d> resultLine = new Tuple<GeometRi.Point3d, GeometRi.Point3d>(new GeometRi.Point3d(projectedStartPoint.X, projectedStartPoint.Y, 0), new GeometRi.Point3d(projectedEndPoint.X, projectedEndPoint.Y, 0));

            return resultLine;
        }

        private static Point2D GetProjectedPoint(Point3D point, Vector3D primaryAxis)
        {
            double coordinate = (primaryAxis == new Vector3D(1, 0, 0)) ? point.Y : point.X;
            return new Point2D(coordinate, point.Z);
        }

        public class Line2D
        {
            public Point2D StartPoint { get; set; }
            public Point2D EndPoint { get; set; }

            public Line2D(Point2D startPoint, Point2D endPoint)
            {
                StartPoint = startPoint;
                EndPoint = endPoint;
            }
        }
        public class Point2D
        {
            public double X { get; set; }
            public double Y { get; set; }

            public Point2D(double x, double y)
            {
                X = x;
                Y = y;
            }
        }
















        //public Tuple<GeometRi.Point3d, GeometRi.Point3d> RotateLineTo2D(GeometRi.Point3d startPoint, GeometRi.Point3d endPoint, Vector3D planeNormal)
        //    {
        //        // Determine two axes orthogonal to the plane normal
        //        Vector3D axis1 = GetOrthogonalAxis(planeNormal);
        //        Vector3D axis2 = Vector3D.CrossProduct(planeNormal, axis1);

        //        // Create a transformation matrix using the orthogonal axes
        //        Matrix3D transformationMatrix = new Matrix3D(
        //            axis1.X, axis2.X, planeNormal.X, 0,
        //            axis1.Y, axis2.Y, planeNormal.Y, 0,
        //            axis1.Z, axis2.Z, planeNormal.Z, 0,
        //            0, 0, 0, 1
        //        );


        //        // Check if the plane normal is aligned with the X or Y axis
        //        if (Math.Abs(planeNormal.X) > Math.Abs(planeNormal.Y) && Math.Abs(planeNormal.X) > Math.Abs(planeNormal.Z))
        //        {
        //            // Plane normal is aligned with the X axis, rotate by 90 or 270 degrees
        //            double angle = planeNormal.X > 0 ? Math.PI / 2 : 3 * Math.PI / 2;
        //            transformationMatrix.Rotate(new System.Windows.Media.Media3D.Quaternion(planeNormal, angle));
        //        }
        //        else if (Math.Abs(planeNormal.Y) > Math.Abs(planeNormal.X) && Math.Abs(planeNormal.Y) > Math.Abs(planeNormal.Z))
        //        {
        //            // Plane normal is aligned with the Y axis, rotate by 180 degrees
        //            transformationMatrix.Rotate(new System.Windows.Media.Media3D.Quaternion(planeNormal, Math.PI));
        //        }

        //        // Transform the line to the 2D plane
        //        Point3D startPoint2D = transformationMatrix.Transform(new Point3D(startPoint.X, startPoint.Y, startPoint.Z));
        //        Point3D endPoint2D = transformationMatrix.Transform(new Point3D(endPoint.X, endPoint.Y, endPoint.Z));

        //        // Create the resulting line in 2D
        //        Tuple<GeometRi.Point3d, GeometRi.Point3d> resultLine = new Tuple<GeometRi.Point3d, GeometRi.Point3d>(new GeometRi.Point3d(startPoint2D.X, startPoint2D.Y, 0), new GeometRi.Point3d(endPoint2D.X, endPoint2D.Y, 0));

        //        return resultLine;
        //    }







        //public Tuple<GeometRi.Point3d, GeometRi.Point3d> RotateLineTo2D3(GeometRi.Point3d startPoint, GeometRi.Point3d endPoint, Vector3D planeNormal)
        //    {
        //        // Determine the axis to project onto based on the plane normal
        //        Vector3D projectionAxis;
        //        if (Math.Abs(planeNormal.Y) < Math.Abs(planeNormal.X) && Math.Abs(planeNormal.Y) < Math.Abs(planeNormal.Z))
        //        {
        //            projectionAxis = new Vector3D(0, 1, 0); // Project onto the Y-axis
        //        }
        //        else if (Math.Abs(planeNormal.Z) < Math.Abs(planeNormal.X) && Math.Abs(planeNormal.Z) < Math.Abs(planeNormal.Y))
        //        {
        //            projectionAxis = new Vector3D(0, 0, 1); // Project onto the Z-axis
        //        }
        //        else
        //        {
        //            projectionAxis = new Vector3D(1, 0, 0); // Project onto the X-axis
        //        }

        //        // Calculate the rotation angle around the projection axis
        //        double angle = Math.Atan2(projectionAxis.Z, projectionAxis.X);

        //        // Rotate the line around the projection axis
        //        Matrix3D rotationMatrix = new Matrix3D();
        //        rotationMatrix.Rotate(new System.Windows.Media.Media3D.Quaternion(projectionAxis, -angle));

        //        // Transform the line to the 2D plane
        //        Point3D startPoint2D = rotationMatrix.Transform(new Point3D(startPoint.X, startPoint.Y, startPoint.Z));
        //        Point3D endPoint2D = rotationMatrix.Transform(new Point3D(endPoint.X, endPoint.Y, endPoint.Z));

        //        // Swap the appropriate coordinates based on the projection axis
        //        if (projectionAxis == new Vector3D(0, 1, 0))
        //        {
        //            startPoint2D = new Point3D(startPoint2D.X, startPoint2D.Z, startPoint2D.Y);
        //            endPoint2D = new Point3D(endPoint2D.X, endPoint2D.Z, endPoint2D.Y);
        //        }
        //        else if (projectionAxis == new Vector3D(0, 0, 1))
        //        {
        //            startPoint2D = new Point3D(startPoint2D.Z, startPoint2D.X, startPoint2D.Y);
        //            endPoint2D = new Point3D(endPoint2D.Z, endPoint2D.X, endPoint2D.Y);
        //        }
        //        else
        //        {
        //            startPoint2D = new Point3D(startPoint2D.X, startPoint2D.Y, startPoint2D.Z);
        //            endPoint2D = new Point3D(endPoint2D.X, endPoint2D.Y, endPoint2D.Z);
        //        }

        //        // Create the resulting line in 2D
        //        //Line resultLine = new Line(startPoint2D.X, startPoint2D.Y, endPoint2D.X, endPoint2D.Y);
        //        // Create the resulting line in 2D
        //        Tuple<GeometRi.Point3d, GeometRi.Point3d> resultLine = new Tuple<GeometRi.Point3d, GeometRi.Point3d>(new GeometRi.Point3d(startPoint2D.X, startPoint2D.Y, 0), new GeometRi.Point3d(endPoint2D.X, endPoint2D.Y, 0));

        //        return resultLine;

        //    }





        /// <summary>
        /// ///////////////////////////////////
        /// </summary>
        /// <param name="startPoint"></param>
        /// <param name="endPoint"></param>
        /// <param name="planeNormal"></param>
        /// <returns></returns>
        //public Tuple<GeometRi.Point3d, GeometRi.Point3d> RotateLineTo2D2(GeometRi.Point3d startPoint, GeometRi.Point3d endPoint, Vector3D planeNormal)
        //{
        //    // Calculate the rotation angle around the plane normal
        //    double angle = Math.Atan2(planeNormal.Z, planeNormal.X);

        //    // Rotate the line around the plane normal
        //    Matrix3D rotationMatrix = new Matrix3D();
        //    rotationMatrix.Rotate(new System.Windows.Media.Media3D.Quaternion(planeNormal, -angle));

        //    // Transform the line to the 2D plane
        //    Point3D startPoint2D = rotationMatrix.Transform(new Point3D(startPoint.X, startPoint.Y, startPoint.Z));
        //    Point3D endPoint2D = rotationMatrix.Transform(new Point3D(endPoint.X, endPoint.Y, endPoint.Z));

        //    // Swap the Y and Z coordinates
        //    double temp = startPoint2D.Y;
        //    startPoint2D.Y = startPoint2D.Z;
        //    startPoint2D.Z = temp;

        //    temp = endPoint2D.Y;
        //    endPoint2D.Y = endPoint2D.Z;
        //    endPoint2D.Z = temp;

        //    // Create the resulting line in 2D
        //    Tuple<GeometRi.Point3d, GeometRi.Point3d> resultLine = new Tuple<GeometRi.Point3d, GeometRi.Point3d>(new GeometRi.Point3d(startPoint2D.X, startPoint2D.Y, 0), new GeometRi.Point3d(endPoint2D.X, endPoint2D.Y, 0));

        //    return resultLine;
        //}




        //public Tuple<GeometRi.Point3d, GeometRi.Point3d> RotateLineToZeroZ(GeometRi.Point3d startPoint, GeometRi.Point3d endPoint)
        //{
        //    // Step 1: Calculate the rotation angle
        //    double angle = Math.Atan2(endPoint.Y - startPoint.Y, endPoint.X - startPoint.X);

        //    // Step 2: Create the rotation transformation
        //    Matrix3D rotationMatrix = new Matrix3D();
        //    rotationMatrix.Rotate(new System.Windows.Media.Media3D.Quaternion(new Vector3D(0, 0, 1), -angle));

        //    // Step 3: Apply the rotation to the line's start and end points
        //    Point3D rotatedStartPoint = rotationMatrix.Transform(new Point3D(startPoint.X, startPoint.Y, startPoint.Z));
        //    Point3D rotatedEndPoint = rotationMatrix.Transform(new Point3D(endPoint.X, endPoint.Y, endPoint.Z));

        //    rotatedStartPoint.Y = rotatedStartPoint.Z;
        //    rotatedEndPoint.Y = rotatedEndPoint.Z;

        //    // Step 4: Update the z-coordinate to zero
        //    rotatedStartPoint.Z = 0;
        //    rotatedEndPoint.Z = 0;

        //    // Step 5: Return the updated start and end points
        //    return new Tuple<GeometRi.Point3d, GeometRi.Point3d>(new GeometRi.Point3d(rotatedStartPoint.X, rotatedStartPoint.Y, rotatedStartPoint.Z), new GeometRi.Point3d(rotatedEndPoint.X, rotatedEndPoint.Y, rotatedEndPoint.Z));
        //}



        //public  Vector3 RotateLinesToXYPlane(Vector3 lines)
        //{
        //    // Step 1: Determine the rotation angle
        //    float angle = (float)Math.Atan2(lines.Y, lines.X);

        //    // Step 2: Rotate each line to align with the XY plane
        //    Vector3 transformedLine = new Vector3();

        //    Matrix4x4 rotationMatrix = Matrix4x4.CreateRotationZ(-angle);
        //     transformedLine = Vector3.Transform(lines, rotationMatrix);

        //    // Step 3: Return the transformed lines
        //    return transformedLine;
        //}






        //public static double[] CalculateRotationVector(double[] points)
        //{
        //    double[] centroid = CalculateCentroid(points);
        //    double[] rotationVector = new double[] { -centroid[0], -centroid[1], -centroid[2] };
        //    return rotationVector;
        //}
        //public static double[] CalculateCentroid(double[] points)
        //{
        //    double sumX = 0, sumY = 0, sumZ = 0;
        //    int count = points.Length;


        //    sumX += points[0];
        //    sumY += points[1];
        //    sumZ += points[2];


        //    return new double[] { sumX / count, sumY / count, sumZ / count };
        //}
        //public double[] TransformPointsToXYPlane(double[] points, double[] normal)
        //{

        //    double[] transformedPoints = new double[3];
        //    double[] rotationVector = CalculateRotationVector(points);

        //    double[] xAxis = GenerateXAxis(normal);
        //    double[] yAxis = GenerateYAxis(normal);

        //    //transformedPoints = TransformPointToXYPlane(points, normal, xAxis, yAxis);
        //    transformedPoints = TransformPointToXYPlane(points, rotationVector);


        //    return transformedPoints;
        //}

        //public static double[] GenerateYAxis(double[] normal)
        //{
        //    double[] xAxis = GenerateXAxis(normal);
        //    double[] yAxis = new double[] { normal[2] * xAxis[1] - normal[1] * xAxis[2], normal[0] * xAxis[2] - normal[2] * xAxis[0], normal[1] * xAxis[0] - normal[0] * xAxis[1] };
        //    return yAxis;
        //}

        //public static double[] GenerateXAxis(double[] normal)
        //{
        //    double[] xAxis = new double[3];
        //    if (Math.Abs(normal[0]) < Math.Abs(normal[1]))
        //    {
        //        if (Math.Abs(normal[0]) < Math.Abs(normal[2]))
        //            xAxis[0] = 1;
        //        else
        //            xAxis[2] = 1;
        //    }
        //    else
        //    {
        //        if (Math.Abs(normal[1]) < Math.Abs(normal[2]))
        //            xAxis[1] = 1;
        //        else
        //            xAxis[2] = 1;
        //    }
        //    return xAxis;
        //}

        //public static double[] TransformPointToXYPlane(double[] point, double[] rotationVector)
        //{
        //    double[] transformedPoint = new double[3];

        //    transformedPoint[0] = point[0] + rotationVector[0];
        //    transformedPoint[1] = point[1] + rotationVector[1];
        //    transformedPoint[2] = point[2] + rotationVector[2];

        //    return new double[] { transformedPoint[0], transformedPoint[1] };
        //}
        //public static double[] TransformPointToXYPlane(double[] point, double[] normal, double[] xAxis, double[] yAxis)
        //{
        //    double x = point[0] * xAxis[0] + point[1] * xAxis[1] + point[2] * xAxis[2];
        //    double y = point[0] * yAxis[0] + point[1] * yAxis[1] + point[2] * yAxis[2];
        //    return new double[] { x, y };
        //}

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
        public List<BoundingBoxFace> PrimBoundingBoxData { get; set; }
        public Autodesk.AutoCAD.DatabaseServices.ObjectId AcadObjID { get; set; }
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

        public AngleDt LSecData { get; set; }

        // Used to detect GRP FLG Part;
        public Pt3D NoramlDir { get; set; }

        public CustomPlane ProjectionPlane { get; set; }

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


    public class PedastalData
    {
        public double L { get; set; }

        public double W { get; set; }

        public double H { get; set; }

        public PedastalData()
        {
            L = 0;
            W = 0;
            H = 0;

        }
    }


    public enum EdgeType
    {
        LineSegment = 0,
        ClosedCircularEdge,
        OpenCircularEdge,
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

        public Vector3d FaceNormal { get; set; }

        public Autodesk.AutoCAD.Geometry.Point3d PtonPlane { get; set; }

        public bool IsPlannar { get; set; }

        public string IdLocal { get; set; }
        public string FaceIdLocal { get; set; }

        public bool IsMatching { get; set; }

        public Angles AngleData { get; set; }

        public DirectionVec Directionvecface { get; set; }

        public List<Edgeinfo> ListlinearEdge = new List<Edgeinfo>();

        public List<Edgeinfo> ListAllEdges = new List<Edgeinfo>();

        public List<Edgeinfo> ListPrimEdges = new List<Edgeinfo>();

    }

    public class Edgeinfo
    {
        public double EdgeLength { get; set; }
        public Pt3D DirectionEdge { get; set; }
        public Pt3D StPt { get; set; }

        public EdgeType TypeEdge { get; set; }
        public Pt3D EndPt { get; set; }

        public Pt3D MidPoint { get; set; }
        public Pt3D Center { get; set; }
        public double Radius { get; set; }
        public string EdgeId { get; set; }

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

    public class CustomPlane
    {
        public Pt3D PointOnPlane { get; set; }
        public Pt3D Normal { get; set; }
        public Vector3d NormalPipe { get; set; }
        public Point3d StptPipe { get; set; }
        public Point3d EndptPipe { get; set; }


    }
}
