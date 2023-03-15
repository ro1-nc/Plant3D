using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.ProcessPower.PnP3dObjects;
using Autodesk.ProcessPower.PlantInstance;
using Autodesk.ProcessPower.ProjectManager;
using Autodesk.ProcessPower.DataLinks;
using Autodesk.ProcessPower.DataObjects;
using Autodesk.ProcessPower.PnP3dDataLinks;
using Autodesk.ProcessPower.PartsRepository;
using Autodesk.ProcessPower.PnP3dPipeSupport;
using Autodesk.ProcessPower.P3dUI;
using Autodesk.ProcessPower.ACPUtils;
using System.IO;
using System.Reflection;

namespace Project1.Support2D
{
    public class SupportC
    {
        List<SupportData> ListCentalSuppoData = new List<SupportData>();

        public double spaceX = 81971.6112;
        public double spaceY = 71075.0829;

        public double tempX = 101659.6570;
        public double tempY = 71694.2039;
        public void ReadSupportData()
        {
            Document AcadDoc = null;
            Transaction AcadTransaction = null;
            BlockTable AcadBlockTable = null;
            BlockTableRecord AcadBlockTableRecord = null;
            Database AcadDatabase = null;
            Editor AcadEditor = null;
            PromptSelectionResult selectionRes;
            ObjectIdCollection ents;

            AcadDoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;

            if (AcadDoc != null)
            {
                using (AcadDoc.LockDocument())
                {
                    AcadDatabase = AcadDoc.Database;

                    if (AcadDatabase != null)
                    {
                        AcadTransaction = AcadDatabase.TransactionManager.StartTransaction();

                        if (AcadTransaction != null)
                        {
                            AcadBlockTable = AcadTransaction.GetObject(AcadDatabase.BlockTableId,
                                                                          OpenMode.ForWrite) as BlockTable;
                            if (AcadBlockTable != null)
                            {
                                AcadBlockTableRecord = AcadTransaction.GetObject(AcadBlockTable[BlockTableRecord.ModelSpace],
                                                               OpenMode.ForWrite) as BlockTableRecord;
                                AcadEditor = AcadDoc.Editor;

                                if (AcadBlockTableRecord != null && AcadEditor != null)
                                {
                                    // Selecting All in the autocad Doc
                                    selectionRes = AcadEditor.SelectAll();

                                    //Getting Object ID of the each selected entiry
                                    ents = new ObjectIdCollection(selectionRes.Value.GetObjectIds());

                                    foreach (ObjectId id in ents)
                                    {
                                        try
                                        {
                                            var enty = (Entity)AcadTransaction.GetObject(id, OpenMode.ForRead);

                                            // to explode block of text collect them
                                            if (enty.GetType() == typeof(BlockReference))
                                            {
                                                BlockReference blkRef = enty as BlockReference;

                                                if (blkRef.Name == "Simple Support")
                                                {
                                                    SupportData SupData = new SupportData();

                                                    DBObjectCollection ExpObjs = new DBObjectCollection();
                                                    blkRef.Explode(ExpObjs);

                                                    foreach (Entity AcEnt in ExpObjs)
                                                    {

                                                        EntityColor COlor = AcEnt.EntityColor;

                                                        if (AcEnt.GetType() == typeof(Solid3d))
                                                        {
                                                            SupporSpecData SuppoSpecdata = new SupporSpecData();
                                                            Solid3d SLD = AcEnt as Solid3d;

                                                            SuppoSpecdata.Centroid = GetPt3DFromPoint3D(SLD.MassProperties.Centroid);
                                                            FillBoundingBox(AcEnt, ref SuppoSpecdata);
                                                            FillDirVec(AcEnt, ref SuppoSpecdata);
                                                            SuppoSpecdata.Volume = SLD.MassProperties.Volume;
                                                            SuppoSpecdata.CalculateDist();

                                                            SupData.ListBottomPart.Add(SuppoSpecdata);
                                                            continue;
                                                        }

                                                        Type t = AcEnt.GetType();
                                                        string Name = t.Name;
                                                        if (AcEnt.GetType().Name.Contains("BlockReference") || AcEnt.GetType().Name.Contains("ImpCurve"))
                                                        {

                                                            SupporSpecData SuppoSpecdata = new SupporSpecData();
                                                            // Solid3d SLD = AcEnt as Solid3d;

                                                            FillBoundingBox(AcEnt, ref SuppoSpecdata);
                                                            SuppoSpecdata.CalculateCentroid();
                                                            FillDirVec(AcEnt, ref SuppoSpecdata);
                                                            SuppoSpecdata.CalculateDist();
                                                            SuppoSpecdata.CalculateVolume();

                                                            if (AcEnt.GetType().Name.Contains("BlockReference"))
                                                            {
                                                                SupData.ListSecondrySuppo.Add(SuppoSpecdata);
                                                            }
                                                            else
                                                            {
                                                                SupData.ListPrimarySuppo.Add(SuppoSpecdata);
                                                            }
                                                        }
                                                    }

                                                    ListCentalSuppoData.Add(SupData);
                                                }
                                            }
                                        }
                                        catch (Exception)
                                        {

                                        }
                                    }
                                }

                            }

                            AcadTransaction.Commit();
                        }
                    }
                }
            }

            void FillBoundingBox(Entity AcEnt, ref SupporSpecData SuppoSpecdata)
            {
                Extents3d? Ext = AcEnt.Bounds;
                SuppoSpecdata.Boundingboxmin = GetPt3DFromPoint3D(Ext.Value.MinPoint);
                SuppoSpecdata.Boundingboxmax = GetPt3DFromPoint3D(Ext.Value.MaxPoint);
            }

            Pt3D GetPt3DFromPoint3D(Point3d Pt)
            {
                Pt3D LocalPoint3d = new Pt3D();
                LocalPoint3d.X = Pt.X;
                LocalPoint3d.Y = Pt.Y;
                LocalPoint3d.Z = Pt.Z;

                return LocalPoint3d;
            }

            void FillDirVec(Entity AcEnt, ref SupporSpecData SuppoSpecdata)
            {
                DirectionVec DirVec = new DirectionVec();
                DirVec.XDirVec = GetPt3DFromVecData(AcEnt.Ecs.CoordinateSystem3d.Xaxis);
                DirVec.YDirVec = GetPt3DFromVecData(AcEnt.Ecs.CoordinateSystem3d.Yaxis);
                DirVec.ZDirVec = GetPt3DFromVecData(AcEnt.Ecs.CoordinateSystem3d.Zaxis);

                SuppoSpecdata.Directionvec = DirVec;
            }

            Pt3D GetPt3DFromVecData(Vector3d Vec)
            {
                Pt3D LocalPoint3d = new Pt3D();
                LocalPoint3d.X = Vec.X;
                LocalPoint3d.Y = Vec.Y;
                LocalPoint3d.Z = Vec.Z;

                return LocalPoint3d;
            }
        }

        public void ProcessSupportData()
        {
            ProcessSuppoerD();
        }

        public void ProcessSuppoerD()
        {
            foreach (SupportData Data in ListCentalSuppoData)
            {
                bool HasBottom = false;
                int PrimarySuppoCnt = 0;
                int SecondrySuppoCnt = 0;


                if (Data.ListBottomPart.Count > 0)
                {
                    HasBottom = true;
                }

                PrimarySuppoCnt = Data.ListPrimarySuppo.Count;
                SecondrySuppoCnt = Data.ListSecondrySuppo.Count;

                int SupportCount = Data.ListBottomPart.Count + Data.ListPrimarySuppo.Count + Data.ListSecondrySuppo.Count;

                if (SupportCount <= 2)
                {

                }
                else
                {
                    if (SupportCount == 5 && Data.ListBottomPart.Count == 2 && Data.ListPrimarySuppo.Count == 2)
                    {
                        //string 
                        //if (AreCentroidsinLine(Data))
                        //{
                        //    CheckRotationtTogetType(Data);
                        //}
                    }
                }


            }
        }



        bool AreCentroidsinLine(SupportData SData)
        {
            List<Pt3D> BPartCentroids = new List<Pt3D>();
            List<Pt3D> PPartCentroids = new List<Pt3D>();
            List<Pt3D> SPartCentroids = new List<Pt3D>();

            BPartCentroids = GetDicCentroidBottomPart(SData);
            PPartCentroids = GetDicCentroidPrimaryPart(SData);
            SPartCentroids = GetDicCentroidSPart(SData);

            return CheckCentroidInLine(BPartCentroids, PPartCentroids, SPartCentroids);
        }

        bool CheckCentroidInLine(List<Pt3D> BPartCentroids, List<Pt3D> PPartCentroids, List<Pt3D> SPartCentroids)
        {
            bool AllXAreinLine = true;
            bool AllYAreinLine = true;
            bool AllZAreinLine = true;
            List<double> ListXPt = new List<double>();
            List<double> ListYPt = new List<double>();
            List<double> ListZPt = new List<double>();

            ListXPt = GetAllPtList(BPartCentroids, PPartCentroids, SPartCentroids, "X");
            ListYPt = GetAllPtList(BPartCentroids, PPartCentroids, SPartCentroids, "Y");
            ListZPt = GetAllPtList(BPartCentroids, PPartCentroids, SPartCentroids, "Z");

            for (int inx = 0; inx < ListXPt.Count; inx++)
            {
                if (Math.Abs(ListXPt[0] - ListXPt[inx]) > 0.01)
                {
                    AllXAreinLine = false;
                    break;
                }
            }

            for (int inx = 0; inx < ListYPt.Count; inx++)
            {
                if (Math.Abs(ListYPt[0] - ListYPt[inx]) > 0.01)
                {
                    AllYAreinLine = false;
                    break;
                }
            }

            for (int inx = 0; inx < ListZPt.Count; inx++)
            {
                if (Math.Abs(ListZPt[0] - ListZPt[inx]) > 0.01)
                {
                    AllZAreinLine = false;
                    break;
                }
            }

            if (AllXAreinLine && AllYAreinLine)
            {
                return true;
            }
            else if (AllXAreinLine && AllZAreinLine)
            {
                return true;
            }
            else if (AllYAreinLine && AllZAreinLine)
            {
                return true;
            }

            return false;
        }

        List<double> GetAllPtList(List<Pt3D> BPartCentroids, List<Pt3D> PPartCentroids, List<Pt3D> SPartCentroids, string Coordinate)
        {
            List<double> ListCordinate = new List<double>();
            if (BPartCentroids.Count > 0)
            {
                foreach (Pt3D Pnt in BPartCentroids)
                {
                    if (Coordinate == "X")
                    {
                        ListCordinate.Add(Pnt.X);
                    }
                    else if (Coordinate == "Y")
                    {
                        ListCordinate.Add(Pnt.Y);
                    }
                    else if (Coordinate == "Z")
                    {
                        ListCordinate.Add(Pnt.Z);
                    }
                }
            }

            if (PPartCentroids.Count > 0)
            {
                foreach (Pt3D Pnt in PPartCentroids)
                {
                    if (Coordinate == "X")
                    {
                        ListCordinate.Add(Pnt.X);
                    }
                    else if (Coordinate == "Y")
                    {
                        ListCordinate.Add(Pnt.Y);
                    }
                    else if (Coordinate == "Z")
                    {
                        ListCordinate.Add(Pnt.Z);
                    }
                }
            }

            if (SPartCentroids.Count > 0)
            {
                foreach (Pt3D Pnt in SPartCentroids)
                {
                    if (Coordinate == "X")
                    {
                        ListCordinate.Add(Pnt.X);
                    }
                    else if (Coordinate == "Y")
                    {
                        ListCordinate.Add(Pnt.Y);
                    }
                    else if (Coordinate == "Z")
                    {
                        ListCordinate.Add(Pnt.Z);
                    }
                }
            }

            return ListCordinate;
        }

        List<Pt3D> GetDicCentroidPrimaryPart(SupportData SData)
        {
            List<Pt3D> Centroids = new List<Pt3D>();
            if (SData.ListPrimarySuppo.Count > 0)
            {
                foreach (var BPart in SData.ListPrimarySuppo)
                {
                    Centroids.Add(BPart.Centroid);
                }

                return Centroids;
            }

            return null;
        }

        List<Pt3D> GetDicCentroidSPart(SupportData SData)
        {
            List<Pt3D> Centroids = new List<Pt3D>();
            if (SData.ListSecondrySuppo.Count > 0)
            {
                foreach (var BPart in SData.ListSecondrySuppo)
                {
                    Centroids.Add(BPart.Centroid);
                }

                return Centroids;
            }

            return null;
        }

        List<Pt3D> GetDicCentroidBottomPart(SupportData SData)
        {
            List<Pt3D> Centroids = new List<Pt3D>();
            if (SData.ListBottomPart.Count > 0)
            {
                foreach (var BPart in SData.ListBottomPart)
                {
                    Centroids.Add(BPart.Centroid);
                }
                return Centroids;
            }

            return null;
        }

        [Obsolete]
        public void Create2D()
        {
            Document AcadDoc = null;
            Database AcadDatabase = null;
            Document Document2D = null;

            AcadDoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            string Filename = AcadDoc.Name;
            AcadDatabase = AcadDoc.Database;

            using (AcadDoc.LockDocument())
            {
                using (Transaction AcadTransaction = AcadDatabase.TransactionManager.StartTransaction())
                {
                    try
                    {
                        DocumentCollection AcadDocumentCollection = Application.DocumentManager;
                        Document2D = AcadDocumentCollection.Add("acad.dwt");
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            //Creates 2D View
            //Create2DView(Document2D);
            Create2DViewWithTemp(Document2D);
            Database newDb = AcadDatabase.Wblock();
            string Path = System.IO.Path.GetDirectoryName(Filename);
            newDb.SaveAs(Path + "2d.dwg", DwgVersion.Current);
            Document2D.CloseAndSave(Path + "2d.dwg");
        }

        [Obsolete]
        void Create2DView(Document Document2D)
        {
            Database AcadDatabase = Document2D.Database;
            Transaction AcadTransaction = null;
            BlockTable AcadBlockTable = null;
            BlockTableRecord AcadBlockTableRecord = null;

            using (Document2D.LockDocument())
            {
                AcadTransaction = AcadDatabase.TransactionManager.StartTransaction();

                AcadBlockTable = AcadTransaction.GetObject(AcadDatabase.BlockTableId,
                                                               OpenMode.ForRead) as BlockTable;

                AcadBlockTableRecord = AcadTransaction.GetObject(AcadBlockTable[BlockTableRecord.ModelSpace],
                                                OpenMode.ForWrite) as BlockTableRecord;

                SupportData firstSupport = ListCentalSuppoData.FirstOrDefault();

                List<SupporSpecData> PrimarySupport = firstSupport.ListPrimarySuppo;
                SupporSpecData prmsupport = PrimarySupport.FirstOrDefault();
                double height1 = GetHeight(prmsupport);
                CreateSecondarySupportBottom(prmsupport, height1, AcadTransaction, AcadBlockTableRecord, AcadDatabase);

                SupporSpecData prmsupport1 = PrimarySupport[1];
                double newheight = prmsupport1.Boundingboxmax.Z - prmsupport1.Boundingboxmin.Z;
                CreateSecondarySupportTop(prmsupport1, newheight, height1, AcadTransaction, AcadDatabase, AcadBlockTableRecord);

                //CreateSecondarySupportTopOuter(prmsupport1, newheight, height1, AcadTransaction, AcadDatabase, AcadBlockTableRecord);

                //Create Pedestial support
                //CreateBottomSupportTop(prmsupport1, newheight, 0, AcadTransaction, AcadDatabase, AcadBlockTableRecord);
                CreateBottomSupportTopType2(prmsupport1, newheight, 0, AcadTransaction, AcadDatabase, AcadBlockTableRecord);

                //to create primary support
                List<SupporSpecData> SecondSupport = firstSupport.ListSecondrySuppo;
                double add = height1 + newheight;
                //CreatePrimarySupport(SecondSupport, add, AcadBlockTableRecord, AcadTransaction, AcadDatabase);
                CreatePrimarySupportwithvertex(SecondSupport, add, AcadBlockTableRecord, AcadTransaction, AcadDatabase, prmsupport1);

                CreateLeaderfromfile(AcadBlockTableRecord, AcadTransaction, AcadDatabase);

                CreateSingleLeadrwidtxt(AcadBlockTableRecord, AcadTransaction, AcadDatabase);

                //SupporSpecData Sesupport = SecondSupport.FirstOrDefault();
                AcadTransaction.Commit();
            }
        }

        [Obsolete]
        void Create2DViewWithTemp(Document Document2D)
        {
            Database AcadDatabase = Document2D.Database;
            Transaction AcadTransaction = null;
            BlockTable AcadBlockTable = null;
            BlockTableRecord AcadBlockTableRecord = null;

            using (Document2D.LockDocument())
            {
                AcadTransaction = AcadDatabase.TransactionManager.StartTransaction();

                AcadBlockTable = AcadTransaction.GetObject(AcadDatabase.BlockTableId,
                                                               OpenMode.ForRead) as BlockTable;

                AcadBlockTableRecord = AcadTransaction.GetObject(AcadBlockTable[BlockTableRecord.ModelSpace],
                                                OpenMode.ForWrite) as BlockTableRecord;


                SupportData firstSupport = ListCentalSuppoData.FirstOrDefault();

                // gets the template
                GetTemplate(AcadBlockTableRecord, AcadTransaction, AcadDatabase,0);

                //logic for adding block

                //9869.9480;
                //67542.6980;
                //adding blocks here

                double boxlen = 17299.3016;
                double tracex = 619.1209;
                double boxht = 12734.3388;

                CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY);

                CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY);
                CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY);
                CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY);
                CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY);
                CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY);
                CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY);
                CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY);
                CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY);
                CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY);
                CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY);
                CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY);
                CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY);
                CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY);
                CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY);
                CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY);
                CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY);
                CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY);
                CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY);
                CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY);
                CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY);
                CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY);
                CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY);
                CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY);
                CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY);
                CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY);
                CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY);
                CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY);
                CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY);
                CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY);
                CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY);

                List<SupporSpecData> PrimarySupport = firstSupport.ListPrimarySuppo;
                SupporSpecData prmsupport = PrimarySupport.FirstOrDefault();
                double height1 = GetHeight(prmsupport);
                CreateSecondarySupportBottom(prmsupport, height1, AcadTransaction, AcadBlockTableRecord, AcadDatabase);

                SupporSpecData prmsupport1 = PrimarySupport[1];
                double newheight = prmsupport1.Boundingboxmax.Z - prmsupport1.Boundingboxmin.Z;
                CreateSecondarySupportTop(prmsupport1, newheight, height1, AcadTransaction, AcadDatabase, AcadBlockTableRecord);

                //CreateSecondarySupportTopOuter(prmsupport1, newheight, height1, AcadTransaction, AcadDatabase, AcadBlockTableRecord);

                //Create Pedestial support
                //CreateBottomSupportTop(prmsupport1, newheight, 0, AcadTransaction, AcadDatabase, AcadBlockTableRecord);
                CreateBottomSupportTopType2(prmsupport1, newheight, 0, AcadTransaction, AcadDatabase, AcadBlockTableRecord);

                //to create primary support
                List<SupporSpecData> SecondSupport = firstSupport.ListSecondrySuppo;
                double add = height1 + newheight;
                //CreatePrimarySupport(SecondSupport, add, AcadBlockTableRecord, AcadTransaction, AcadDatabase);
                CreatePrimarySupportwithvertex(SecondSupport, add, AcadBlockTableRecord, AcadTransaction, AcadDatabase, prmsupport1);

                CreateLeaderfromfile(AcadBlockTableRecord, AcadTransaction, AcadDatabase);


                CreateSingleLeadrwidtxt(AcadBlockTableRecord, AcadTransaction, AcadDatabase);

                //SupporSpecData Sesupport = SecondSupport.FirstOrDefault();
                AcadTransaction.Commit();
            }
        }

        [Obsolete]
        public void CreateFullBlock(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, ref double tracex, double boxlen, double boxht, ref double spaceY)
        {
            if (tracex >= spaceX - boxlen)
            {
                spaceY -= boxht;
                tracex = tempX - 101659.6570 + 619.1209;
            }
            if (spaceY > boxht)
            {
                double upperYgap = 3500;
                
                double centerX = tracex + boxlen / 2;  // 9869.9480;
                double centerY = spaceY - upperYgap;
                //box boundaries
                //vertical line
                Point3d pt1 = new Point3d(tracex+boxlen,spaceY + 619.1209, 0);
                Point3d Pt2 = new Point3d(tracex + boxlen, spaceY-boxht + 619.1209, 0);
                Line line = new Line(pt1, Pt2);
                AcadBlockTableRecord.AppendEntity(line);
                line.Color = Color.FromColorIndex(ColorMethod.ByAci, 171);
                AcadTransaction.AddNewlyCreatedDBObject(line, true);

                
                //hori line
                Point3d pt11 = new Point3d(tracex , spaceY-boxht + 619.1209, 0);
                Point3d Pt21 = new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0);
                Line line1 = new Line(pt11, Pt21);
                AcadBlockTableRecord.AppendEntity(line1);
                line1.Color = Color.FromColorIndex(ColorMethod.ByAci, 171);
                AcadTransaction.AddNewlyCreatedDBObject(line1, true);

                FixCreatePrimarySupportwithvertex(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY);
                FixCreateSecondarySupportTop(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY);
                FixCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY);
                FixCreateBottomSupportTopType2(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY);
                tracex += boxlen;
            }
            else
            {

                spaceY = 71075.0829;
                tracex = tempX + 10000 + 619.1209;//gaps
                // gets the template
                GetTemplate(AcadBlockTableRecord, AcadTransaction, AcadDatabase,tracex- 619.1209);
                tempX += 101659.6570 + 10000;
                spaceX = tempX - 19068.9248;
            }
        }




        private void CreateSecondarySupportTopOuter(SupporSpecData prmsupport1, double newheight, double height1, Transaction acadTransaction, Database acadDatabase, BlockTableRecord acadBlockTableRecord)
        {
            var newline = new Polyline();
            prmsupport1.Boundingboxmin.X = prmsupport1.Boundingboxmin.X;
            prmsupport1.Boundingboxmin.Y = prmsupport1.Boundingboxmin.Y + 30;
            prmsupport1.Boundingboxmax.X = prmsupport1.Boundingboxmax.X;
            prmsupport1.Boundingboxmax.Y = prmsupport1.Boundingboxmax.Y - 30;

            Point2d Pt2D1 = new Point2d(prmsupport1.Boundingboxmin.X, prmsupport1.Boundingboxmin.Y + height1);
            newline.AddVertexAt(0, Pt2D1, 0, 0, 0);
            Pt2D1 = new Point2d(prmsupport1.Boundingboxmax.X, prmsupport1.Boundingboxmin.Y + height1);
            newline.AddVertexAt(1, Pt2D1, 0, 0, 0);
            Pt2D1 = new Point2d(prmsupport1.Boundingboxmax.X, prmsupport1.Boundingboxmin.Y + newheight + height1);
            newline.AddVertexAt(2, Pt2D1, 0, 0, 0);
            Pt2D1 = new Point2d(prmsupport1.Boundingboxmin.X, prmsupport1.Boundingboxmin.Y + height1 + newheight);
            newline.AddVertexAt(3, Pt2D1, 0, 0, 0);
            Pt2D1 = new Point2d(prmsupport1.Boundingboxmin.X, prmsupport1.Boundingboxmin.Y + height1);
            newline.AddVertexAt(4, Pt2D1, 0, 0, 0);
            newline.Color = Color.FromColorIndex(ColorMethod.ByAci, 171);
            acadBlockTableRecord.AppendEntity(newline);
            acadTransaction.AddNewlyCreatedDBObject(newline, true);
        }

        private void CreateBottomSupportTop(SupporSpecData prmsupport1, double newheight, int v, Transaction acadTransaction, Database acadDatabase, BlockTableRecord acadBlockTableRecord)
        {
            var newline = new Polyline();
            Point2d Pt2D1 = new Point2d(prmsupport1.Boundingboxmin.X - 120, prmsupport1.Boundingboxmin.Y - newheight - 100);
            newline.AddVertexAt(0, Pt2D1, 0, 0, 0);
            Pt2D1 = new Point2d(prmsupport1.Boundingboxmax.X + 120, prmsupport1.Boundingboxmin.Y - newheight - 100);
            newline.AddVertexAt(1, Pt2D1, 0, 0, 0);
            Pt2D1 = new Point2d(prmsupport1.Boundingboxmax.X + 120, prmsupport1.Boundingboxmin.Y);
            newline.AddVertexAt(2, Pt2D1, 0, 0, 0);
            Pt2D1 = new Point2d(prmsupport1.Boundingboxmin.X - 120, prmsupport1.Boundingboxmin.Y);
            newline.AddVertexAt(3, Pt2D1, 0, 0, 0);
            Pt2D1 = new Point2d(prmsupport1.Boundingboxmin.X - 120, prmsupport1.Boundingboxmin.Y - newheight - 100);
            newline.AddVertexAt(4, Pt2D1, 0, 0, 0);
            newline.Color = Color.FromColorIndex(ColorMethod.ByAci, 8);
            acadBlockTableRecord.AppendEntity(newline);
            acadTransaction.AddNewlyCreatedDBObject(newline, true);
        }

        private void CreateBottomSupportTopType2(SupporSpecData prmsupport1, double newheight, int v, Transaction acadTransaction, Database acadDatabase, BlockTableRecord acadBlockTableRecord)
        {
            var newline = new Polyline();
            Point2d Pt2D1 = new Point2d(prmsupport1.Boundingboxmin.X - 120, prmsupport1.Boundingboxmin.Y - newheight - 100);
            newline.AddVertexAt(0, Pt2D1, 0, 0, 0);
            Pt2D1 = new Point2d(prmsupport1.Boundingboxmax.X + 120, prmsupport1.Boundingboxmin.Y - newheight - 100);
            newline.AddVertexAt(1, Pt2D1, 0, 0, 0);
            Pt2D1 = new Point2d(prmsupport1.Boundingboxmax.X + 120, prmsupport1.Boundingboxmin.Y);
            newline.AddVertexAt(2, Pt2D1, 0, 0, 0);
            Pt2D1 = new Point2d(prmsupport1.Boundingboxmin.X - 120, prmsupport1.Boundingboxmin.Y);
            newline.AddVertexAt(3, Pt2D1, 0, 0, 0);
            Pt2D1 = new Point2d(prmsupport1.Boundingboxmin.X - 120, prmsupport1.Boundingboxmin.Y - newheight - 100);
            newline.AddVertexAt(4, Pt2D1, 0, 0, 0);
            newline.Color = Color.FromColorIndex(ColorMethod.ByAci, 8);

            LinetypeTable acLineTypTbl;
            acLineTypTbl = acadTransaction.GetObject(acadDatabase.LinetypeTableId,
                                                   OpenMode.ForRead) as LinetypeTable;
            string sLineTypName = "DASHED";
            if (acLineTypTbl.Has(sLineTypName) == false)
            {
                acadDatabase.LoadLineTypeFile(sLineTypName, "acad.lin");
                newline.Linetype = sLineTypName;

            }
            acadBlockTableRecord.AppendEntity(newline);
            acadTransaction.AddNewlyCreatedDBObject(newline, true);

            //inside upper block
            double offsetht = 50;
            double offsetinside = 150;
            var insideupperblock = new Polyline();
            Point2d Pt2D2 = new Point2d(prmsupport1.Boundingboxmin.X - 120 + offsetinside, prmsupport1.Boundingboxmin.Y - offsetht);
            insideupperblock.AddVertexAt(0, Pt2D2, 0, 0, 0);
            Pt2D2 = new Point2d(prmsupport1.Boundingboxmax.X + 120 - offsetinside, prmsupport1.Boundingboxmin.Y - offsetht);
            insideupperblock.AddVertexAt(1, Pt2D2, 0, 0, 0);
            Pt2D2 = new Point2d(prmsupport1.Boundingboxmax.X + 120 - offsetinside, prmsupport1.Boundingboxmin.Y);
            insideupperblock.AddVertexAt(2, Pt2D2, 0, 0, 0);
            Pt2D2 = new Point2d(prmsupport1.Boundingboxmin.X - 120 + offsetinside, prmsupport1.Boundingboxmin.Y);
            insideupperblock.AddVertexAt(3, Pt2D2, 0, 0, 0);
            Pt2D2 = new Point2d(prmsupport1.Boundingboxmin.X - 120 + offsetinside, prmsupport1.Boundingboxmin.Y - offsetht);
            insideupperblock.AddVertexAt(4, Pt2D2, 0, 0, 0);
            insideupperblock.Color = Color.FromColorIndex(ColorMethod.ByAci, 8);


            if (acLineTypTbl.Has(sLineTypName) == true)
            {
                //acadDatabase.LoadLineTypeFile(sLineTypName, "acad.lin");
                insideupperblock.Linetype = sLineTypName;
            }
            acadBlockTableRecord.AppendEntity(insideupperblock);
            acadTransaction.AddNewlyCreatedDBObject(insideupperblock, true);

            //vertical side lines 
            Point3d pt1 = new Point3d(prmsupport1.Boundingboxmin.X - 120 + 2 * offsetinside, prmsupport1.Boundingboxmin.Y - offsetht, 0);
            Point3d Pt2 = new Point3d(prmsupport1.Boundingboxmin.X - 120 + 2 * offsetinside, prmsupport1.Boundingboxmin.Y - offsetht - offsetht, 0);
            Line line = new Line(pt1, Pt2);
            acadBlockTableRecord.AppendEntity(line);
            line.Color = Color.FromColorIndex(ColorMethod.ByAci, 171);
            acadTransaction.AddNewlyCreatedDBObject(line, true);


            Point3d pt12 = new Point3d(prmsupport1.Boundingboxmax.X + 120 - 2 * offsetinside, prmsupport1.Boundingboxmin.Y - offsetht, 0);
            Point3d Pt22 = new Point3d(prmsupport1.Boundingboxmax.X + 120 - 2 * offsetinside, prmsupport1.Boundingboxmin.Y - offsetht - offsetht, 0);
            Line line2 = new Line(pt12, Pt22);
            acadBlockTableRecord.AppendEntity(line2);
            line2.Color = Color.FromColorIndex(ColorMethod.ByAci, 171);
            acadTransaction.AddNewlyCreatedDBObject(line2, true);

            //horizontal side lines
            Point3d pt3 = new Point3d(prmsupport1.Boundingboxmin.X - 120 + 2 * offsetinside, prmsupport1.Boundingboxmin.Y - offsetht - offsetht, 0);
            Point3d Pt4 = new Point3d(prmsupport1.Boundingboxmin.X - 120, prmsupport1.Boundingboxmin.Y - offsetht - offsetht, 0);
            Line lineh1 = new Line(pt3, Pt4);
            acadBlockTableRecord.AppendEntity(lineh1);
            lineh1.Color = Color.FromColorIndex(ColorMethod.ByAci, 83);
            acadTransaction.AddNewlyCreatedDBObject(lineh1, true);

            //horizontal side lines
            Point3d pt5 = new Point3d(prmsupport1.Boundingboxmax.X + 120 - 2 * offsetinside, prmsupport1.Boundingboxmin.Y - offsetht - offsetht, 0);
            Point3d Pt6 = new Point3d(prmsupport1.Boundingboxmax.X + 120, prmsupport1.Boundingboxmin.Y - offsetht - offsetht, 0);
            Line lineh2 = new Line(pt5, Pt6);
            acadBlockTableRecord.AppendEntity(lineh2);
            lineh2.Color = Color.FromColorIndex(ColorMethod.ByAci, 83);
            acadTransaction.AddNewlyCreatedDBObject(lineh2, true);

        }
        private void CreateSecondarySupportTop(SupporSpecData prmsupport1, double newheight, double height1, Transaction acadTransaction, Database acadDatabase, BlockTableRecord acadBlockTableRecord)
        {
            var newline = new Polyline();
            //rectangular polyline
            Point2d Pt2D1 = new Point2d(prmsupport1.Boundingboxmin.X, prmsupport1.Boundingboxmin.Y + height1);
            newline.AddVertexAt(0, Pt2D1, 0, 0, 0);
            Pt2D1 = new Point2d(prmsupport1.Boundingboxmax.X, prmsupport1.Boundingboxmin.Y + height1);
            newline.AddVertexAt(1, Pt2D1, 0, 0, 0);
            Pt2D1 = new Point2d(prmsupport1.Boundingboxmax.X, prmsupport1.Boundingboxmin.Y + newheight + height1);
            newline.AddVertexAt(2, Pt2D1, 0, 0, 0);
            Pt2D1 = new Point2d(prmsupport1.Boundingboxmin.X, prmsupport1.Boundingboxmin.Y + height1 + newheight);
            newline.AddVertexAt(3, Pt2D1, 0, 0, 0);
            Pt2D1 = new Point2d(prmsupport1.Boundingboxmin.X, prmsupport1.Boundingboxmin.Y + height1);
            newline.AddVertexAt(4, Pt2D1, 0, 0, 0);
            newline.Color = Color.FromColorIndex(ColorMethod.ByAci, 171);
            acadBlockTableRecord.AppendEntity(newline);
            acadTransaction.AddNewlyCreatedDBObject(newline, true);

            //other two lines in rectangle

            Point3d pt1 = new Point3d(prmsupport1.Boundingboxmin.X, prmsupport1.Boundingboxmin.Y + 20 + height1, 0);
            Point3d Pt2 = new Point3d(prmsupport1.Boundingboxmax.X, prmsupport1.Boundingboxmin.Y + 20 + height1, 0);
            Line line = new Line(pt1, Pt2);
            acadBlockTableRecord.AppendEntity(line);
            line.Color = Color.FromColorIndex(ColorMethod.ByAci, 171);
            acadTransaction.AddNewlyCreatedDBObject(line, true);

            Point3d lpt1 = new Point3d(prmsupport1.Boundingboxmin.X, prmsupport1.Boundingboxmin.Y - 20 + height1 + newheight, 0);
            Point3d lpt2 = new Point3d(prmsupport1.Boundingboxmax.X, prmsupport1.Boundingboxmin.Y - 20 + height1 + newheight, 0);
            Line line1 = new Line(lpt1, lpt2);
            acadBlockTableRecord.AppendEntity(line1);
            line1.Color = Color.FromColorIndex(ColorMethod.ByAci, 171);
            acadTransaction.AddNewlyCreatedDBObject(line1, true);


        }


        private void CreateSecondarySupportBottom(SupporSpecData prmsupport, double height1, Transaction AcadTransaction, BlockTableRecord acadBlockTableRecord, Database acadDatabase)
        {
            var line = new Polyline();
            //double height1 = prmsupport.Boundingboxmax.Z - prmsupport.Boundingboxmin.Z;
            Point2d Pt2D1 = new Point2d(prmsupport.Boundingboxmin.X, prmsupport.Boundingboxmin.Y);
            line.AddVertexAt(0, Pt2D1, 0, 0, 0);
            Pt2D1 = new Point2d(prmsupport.Boundingboxmax.X, prmsupport.Boundingboxmin.Y);
            line.AddVertexAt(1, Pt2D1, 0, 0, 0);
            Pt2D1 = new Point2d(prmsupport.Boundingboxmax.X, prmsupport.Boundingboxmin.Y + height1);
            line.AddVertexAt(2, Pt2D1, 0, 0, 0);
            Pt2D1 = new Point2d(prmsupport.Boundingboxmin.X, prmsupport.Boundingboxmin.Y + height1);
            line.AddVertexAt(3, Pt2D1, 0, 0, 0);
            Pt2D1 = new Point2d(prmsupport.Boundingboxmin.X, prmsupport.Boundingboxmin.Y);
            line.AddVertexAt(4, Pt2D1, 0, 0, 0);
            line.Color = Color.FromColorIndex(ColorMethod.ByAci, 171);
            acadBlockTableRecord.AppendEntity(line);
            AcadTransaction.AddNewlyCreatedDBObject(line, true);

            Point3d pt1 = new Point3d(prmsupport.Boundingboxmin.X + 20, prmsupport.Boundingboxmin.Y, 0);
            Point3d Pt2 = new Point3d(prmsupport.Boundingboxmin.X + 20, prmsupport.Boundingboxmin.Y + height1, 0);
            Line innerLine = new Line(pt1, Pt2);
            acadBlockTableRecord.AppendEntity(innerLine);
            innerLine.Color = Color.FromColorIndex(ColorMethod.ByAci, 171);
            AcadTransaction.AddNewlyCreatedDBObject(innerLine, true);

            Point3d lpt1 = new Point3d(prmsupport.Boundingboxmax.X - 20, prmsupport.Boundingboxmin.Y, 0);
            Point3d lpt2 = new Point3d(prmsupport.Boundingboxmax.X - 20, prmsupport.Boundingboxmin.Y + height1, 0);
            Line line1 = new Line(lpt1, lpt2);
            acadBlockTableRecord.AppendEntity(line1);
            line1.Color = Color.FromColorIndex(ColorMethod.ByAci, 171);
            AcadTransaction.AddNewlyCreatedDBObject(line1, true);

            CreateSideSecondarySupportBottom(prmsupport, height1, AcadTransaction, acadBlockTableRecord, acadDatabase, "Left");

        }

        private void CreateSideSecondarySupportBottom(SupporSpecData prmsupport, double height1, Transaction AcadTransaction, BlockTableRecord acadBlockTableRecord, Database acadDatabase, string side)
        {
            double horilen = 1000;
            double vertilen = 300;
            double gap = 400;
            var line = new Polyline();

            //double height1 = prmsupport.Boundingboxmax.Z - prmsupport.Boundingboxmin.Z;
            if (side == "Left")
            {
                Point2d Pt2D1 = new Point2d(prmsupport.Boundingboxmin.X, prmsupport.Boundingboxmin.Y + gap);
                line.AddVertexAt(0, Pt2D1, 0, 0, 0);
                Pt2D1 = new Point2d(prmsupport.Boundingboxmin.X - horilen, prmsupport.Boundingboxmin.Y + gap);
                line.AddVertexAt(1, Pt2D1, 0, 0, 0);
                Pt2D1 = new Point2d(prmsupport.Boundingboxmin.X - horilen, prmsupport.Boundingboxmin.Y + vertilen + gap);
                line.AddVertexAt(2, Pt2D1, 0, 0, 0);
                Pt2D1 = new Point2d(prmsupport.Boundingboxmin.X, prmsupport.Boundingboxmin.Y + vertilen + gap);
                line.AddVertexAt(3, Pt2D1, 0, 0, 0);
                Pt2D1 = new Point2d(prmsupport.Boundingboxmin.X, prmsupport.Boundingboxmin.Y + gap);
                line.AddVertexAt(4, Pt2D1, 0, 0, 0);
                line.Color = Color.FromColorIndex(ColorMethod.ByAci, 171);
                acadBlockTableRecord.AppendEntity(line);
                AcadTransaction.AddNewlyCreatedDBObject(line, true);

                //offset line
                Point3d pt1 = new Point3d(prmsupport.Boundingboxmin.X - horilen, prmsupport.Boundingboxmin.Y + vertilen * 0.75 + gap, 0);
                Point3d Pt2 = new Point3d(prmsupport.Boundingboxmin.X, prmsupport.Boundingboxmin.Y + vertilen * 0.75 + gap, 0);
                Line innerLine = new Line(pt1, Pt2);
                acadBlockTableRecord.AppendEntity(innerLine);
                innerLine.Color = Color.FromColorIndex(ColorMethod.ByAci, 171);
                AcadTransaction.AddNewlyCreatedDBObject(innerLine, true);
            }
            else if (side == "Right")
            {
                Point2d Pt2D1 = new Point2d(prmsupport.Boundingboxmax.X, prmsupport.Boundingboxmin.Y + gap);
                line.AddVertexAt(0, Pt2D1, 0, 0, 0);
                Pt2D1 = new Point2d(prmsupport.Boundingboxmax.X + horilen, prmsupport.Boundingboxmin.Y + gap);
                line.AddVertexAt(1, Pt2D1, 0, 0, 0);
                Pt2D1 = new Point2d(prmsupport.Boundingboxmax.X + horilen, prmsupport.Boundingboxmin.Y + gap + vertilen);
                line.AddVertexAt(2, Pt2D1, 0, 0, 0);
                Pt2D1 = new Point2d(prmsupport.Boundingboxmax.X, prmsupport.Boundingboxmin.Y + gap + vertilen);
                line.AddVertexAt(3, Pt2D1, 0, 0, 0);
                Pt2D1 = new Point2d(prmsupport.Boundingboxmax.X, prmsupport.Boundingboxmin.Y + gap);
                line.AddVertexAt(4, Pt2D1, 0, 0, 0);
                line.Color = Color.FromColorIndex(ColorMethod.ByAci, 171);
                acadBlockTableRecord.AppendEntity(line);
                AcadTransaction.AddNewlyCreatedDBObject(line, true);

                //offset line
                Point3d pt1 = new Point3d(prmsupport.Boundingboxmax.X + horilen, prmsupport.Boundingboxmin.Y + gap + vertilen * 0.75, 0);
                Point3d Pt2 = new Point3d(prmsupport.Boundingboxmax.X, prmsupport.Boundingboxmin.Y + gap + vertilen * 0.75, 0);
                Line innerLine = new Line(pt1, Pt2);
                acadBlockTableRecord.AppendEntity(innerLine);
                innerLine.Color = Color.FromColorIndex(ColorMethod.ByAci, 171);
                AcadTransaction.AddNewlyCreatedDBObject(innerLine, true);
            }
            else
            {

            }



        }

        private double GetHeight(SupporSpecData prmsupport)
        {
            double height1 = prmsupport.Boundingboxmax.Z - prmsupport.Boundingboxmin.Z;
            return height1;
        }

        private void CreatePrimarySupport(List<SupporSpecData> SecondSupport, double add, BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase)
        {
            foreach (SupporSpecData Sesupport in SecondSupport)
            {
                double seheight1 = Sesupport.Boundingboxmax.Z - Sesupport.Boundingboxmin.Z;
                //Point3d center = new Point3d(Sesupport.Boundingboxmin.X + Sesupport.Boundingboxmax.X / 2, Sesupport.Boundingboxmin.Y + Sesupport.Boundingboxmax.Y / 2+add,0);
                Point3d center = new Point3d(Sesupport.Centroid.X, Sesupport.Centroid.Y + add + 200, 0);
                Point2d min = new Point2d(Sesupport.Boundingboxmin.X, Sesupport.Boundingboxmin.Y);
                Point2d max = new Point2d(Sesupport.Boundingboxmax.X, Sesupport.Boundingboxmax.Y);
                double radius = max.GetDistanceTo(min) / 4;
                LinetypeTable acLineTypTbl;
                acLineTypTbl = AcadTransaction.GetObject(AcadDatabase.LinetypeTableId,
                                                       OpenMode.ForRead) as LinetypeTable;
                string sLineTypName = "PHANTOM2";
                if (acLineTypTbl.Has(sLineTypName) == false)
                {
                    AcadDatabase.LoadLineTypeFile(sLineTypName, "acad.lin");
                }
                //double radius = height1/2;
                Circle circle = new Circle();
                Circle secCircle = new Circle();
                circle.Radius = radius;
                circle.Center = center;
                circle.Linetype = sLineTypName;
                circle.LinetypeScale = 0.2;
                circle.Color = Color.FromColorIndex(ColorMethod.ByAci, 8);

                AcadBlockTableRecord.AppendEntity(circle);
                AcadTransaction.AddNewlyCreatedDBObject(circle, true);

                secCircle.Radius = radius - 5;
                secCircle.Center = center;
                secCircle.Linetype = sLineTypName;
                secCircle.Color = Color.FromColorIndex(ColorMethod.ByAci, 8);
                secCircle.LinetypeScale = 0.2;
                AcadBlockTableRecord.AppendEntity(secCircle);
                AcadTransaction.AddNewlyCreatedDBObject(secCircle, true);
                //Adds the arc and line to an object id collection
                ObjectIdCollection acObjIdColl = new ObjectIdCollection();
                acObjIdColl.Add(circle.ObjectId);
                ObjectIdCollection acObjectColl1 = new ObjectIdCollection();
                acObjectColl1.Add(secCircle.ObjectId);
                // Create the hatch object and append it to the block table record
                Hatch acHatch = new Hatch();
                AcadBlockTableRecord.AppendEntity(acHatch);
                AcadTransaction.AddNewlyCreatedDBObject(acHatch, true);

                // Set the properties of the hatch object
                // Associative must be set after the hatch object is appended to the 
                // block table record and before AppendLoop
                acHatch.SetDatabaseDefaults();
                acHatch.SetHatchPattern(HatchPatternType.PreDefined, "ANSI31");
                acHatch.Associative = true;
                acHatch.AppendLoop(HatchLoopTypes.Outermost, acObjIdColl);
                acHatch.AppendLoop(HatchLoopTypes.Outermost, acObjectColl1);
                acHatch.HatchStyle = HatchStyle.Normal;
                acHatch.Linetype = sLineTypName;
                acHatch.LinetypeScale = 0.2;
                acHatch.Color = Color.FromColorIndex(ColorMethod.ByAci, 8);
                // Evaluate the hatch
                acHatch.EvaluateHatch(true);

                // Increase the pattern scale by 2 and re-evaluate the hatch
                acHatch.PatternScale = acHatch.PatternScale + 69.23;
                acHatch.SetHatchPattern(acHatch.PatternType, acHatch.PatternName);
                acHatch.EvaluateHatch(true);
            }
        }

        private void CreatePrimarySupportwithvertex(List<SupporSpecData> SecondSupport, double add, BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, SupporSpecData prmsupport1)
        {
            foreach (SupporSpecData Sesupport in SecondSupport)
            {
                double seheight1 = Sesupport.Boundingboxmax.Z - Sesupport.Boundingboxmin.Z;
                //Point3d center = new Point3d(Sesupport.Boundingboxmin.X + Sesupport.Boundingboxmax.X / 2, Sesupport.Boundingboxmin.Y + Sesupport.Boundingboxmax.Y / 2+add,0);
                Point3d center = new Point3d(Sesupport.Centroid.X, Sesupport.Centroid.Y + add + 200, 0);
                Point2d min = new Point2d(Sesupport.Boundingboxmin.X, Sesupport.Boundingboxmin.Y);
                Point2d max = new Point2d(Sesupport.Boundingboxmax.X, Sesupport.Boundingboxmax.Y);
                double radius = max.GetDistanceTo(min) / 4;
                LinetypeTable acLineTypTbl;
                acLineTypTbl = AcadTransaction.GetObject(AcadDatabase.LinetypeTableId,
                                                       OpenMode.ForRead) as LinetypeTable;
                string sLineTypName = "PHANTOM2";
                if (acLineTypTbl.Has(sLineTypName) == false)
                {
                    AcadDatabase.LoadLineTypeFile(sLineTypName, "acad.lin");
                }
                //double radius = height1/2;
                Circle circle = new Circle();
                Circle secCircle = new Circle();
                circle.Radius = radius;
                circle.Center = center;
                circle.Linetype = sLineTypName;
                circle.LinetypeScale = 0.2;
                circle.Color = Color.FromColorIndex(ColorMethod.ByAci, 8);

                AcadBlockTableRecord.AppendEntity(circle);
                AcadTransaction.AddNewlyCreatedDBObject(circle, true);

                secCircle.Radius = radius - 5;
                secCircle.Center = center;
                secCircle.Linetype = sLineTypName;
                secCircle.Color = Color.FromColorIndex(ColorMethod.ByAci, 8);
                secCircle.LinetypeScale = 0.2;
                AcadBlockTableRecord.AppendEntity(secCircle);
                AcadTransaction.AddNewlyCreatedDBObject(secCircle, true);
                //Adds the arc and line to an object id collection
                ObjectIdCollection acObjIdColl = new ObjectIdCollection();
                acObjIdColl.Add(circle.ObjectId);
                ObjectIdCollection acObjectColl1 = new ObjectIdCollection();
                acObjectColl1.Add(secCircle.ObjectId);
                // Create the hatch object and append it to the block table record
                Hatch acHatch = new Hatch();
                AcadBlockTableRecord.AppendEntity(acHatch);
                AcadTransaction.AddNewlyCreatedDBObject(acHatch, true);

                // Set the properties of the hatch object
                // Associative must be set after the hatch object is appended to the 
                // block table record and before AppendLoop
                acHatch.SetDatabaseDefaults();
                acHatch.SetHatchPattern(HatchPatternType.PreDefined, "ANSI31");
                acHatch.Associative = true;
                acHatch.AppendLoop(HatchLoopTypes.Outermost, acObjIdColl);
                acHatch.AppendLoop(HatchLoopTypes.Outermost, acObjectColl1);
                acHatch.HatchStyle = HatchStyle.Normal;
                acHatch.Linetype = sLineTypName;
                acHatch.LinetypeScale = 0.2;
                acHatch.Color = Color.FromColorIndex(ColorMethod.ByAci, 8);
                // Evaluate the hatch
                acHatch.EvaluateHatch(true);

                // Increase the pattern scale by 2 and re-evaluate the hatch
                acHatch.PatternScale = acHatch.PatternScale + 69.23;
                acHatch.SetHatchPattern(acHatch.PatternType, acHatch.PatternName);
                acHatch.EvaluateHatch(true);

                //lines below circle
                //Pt2D1 = new Point2d(prmsupport1.Boundingboxmax.X, prmsupport1.Boundingboxmin.Y + newheight + height1);

                //Pt2D1 = new Point2d(prmsupport1.Boundingboxmin.X, prmsupport1.Boundingboxmin.Y + height1 + newheight);
                Point3d lpt1 = new Point3d(Sesupport.Centroid.X - radius * 0.75, prmsupport1.Boundingboxmin.Y + add, 0);
                Point3d lpt2 = new Point3d(Sesupport.Centroid.X - radius * 0.75, Sesupport.Centroid.Y + add + 200, 0);
                Line line1 = new Line(lpt1, lpt2);
                Point3dCollection intersectionPoints = new Point3dCollection();
                line1.IntersectWith(circle, Intersect.OnBothOperands, intersectionPoints, IntPtr.Zero, IntPtr.Zero);
                if (intersectionPoints.Count > 0)
                {
                    line1.EndPoint = intersectionPoints[0];
                }
                AcadBlockTableRecord.AppendEntity(line1);
                line1.Color = Color.FromColorIndex(ColorMethod.ByAci, 171);
                AcadTransaction.AddNewlyCreatedDBObject(line1, true);

                Point3d lpt3 = new Point3d(Sesupport.Centroid.X + radius * 0.75, prmsupport1.Boundingboxmin.Y + add, 0);
                Point3d lpt4 = new Point3d(Sesupport.Centroid.X + radius * 0.75, Sesupport.Centroid.Y + add + 200, 0);
                Line line2 = new Line(lpt3, lpt4);
                Point3dCollection intersectionPoints2 = new Point3dCollection();
                line2.IntersectWith(circle, Intersect.OnBothOperands, intersectionPoints2, IntPtr.Zero, IntPtr.Zero);
                if (intersectionPoints2.Count > 0)
                {
                    line2.EndPoint = intersectionPoints2[0];
                }
                AcadBlockTableRecord.AppendEntity(line2);
                line2.Color = Color.FromColorIndex(ColorMethod.ByAci, 171);
                AcadTransaction.AddNewlyCreatedDBObject(line2, true);



            }
        }


        void CreateBottomBox(Document Document2D)
        {
            Database AcadDatabase = Document2D.Database;
            Transaction AcadTransaction = null;
            BlockTable AcadBlockTable = null;
            BlockTableRecord AcadBlockTableRecord = null;

            using (Document2D.LockDocument())
            {
                AcadTransaction = AcadDatabase.TransactionManager.StartTransaction();

                AcadBlockTable = AcadTransaction.GetObject(AcadDatabase.BlockTableId,
                                                               OpenMode.ForRead) as BlockTable;

                AcadBlockTableRecord = AcadTransaction.GetObject(AcadBlockTable[BlockTableRecord.ModelSpace],
                                                OpenMode.ForWrite) as BlockTableRecord;

                var SupportLine = new Polyline();

                Point2d Pt2D = new Point2d(0, 0);

                SupportLine.AddVertexAt(0, Pt2D, 0, 0, 0);
                Pt2D = new Point2d(10, 0);
                SupportLine.AddVertexAt(1, Pt2D, 0, 0, 0);
                Pt2D = new Point2d(10, 10);
                SupportLine.AddVertexAt(2, Pt2D, 0, 0, 0);
                Pt2D = new Point2d(0, 10);
                SupportLine.AddVertexAt(3, Pt2D, 0, 0, 0);
                Pt2D = new Point2d(0, 0);
                SupportLine.AddVertexAt(4, Pt2D, 0, 0, 0);

                SupportLine.Color = Autodesk.AutoCAD.Colors.Color.FromColor(System.Drawing.Color.Red);

                AcadBlockTableRecord.AppendEntity(SupportLine);
                AcadTransaction.AddNewlyCreatedDBObject(SupportLine, true);

                AcadTransaction.Commit();
            }
        }

        void CreateLeader(Document Document2D, Database AcadDatabase)
        {
            // Start a transaction
            using (Transaction tr = AcadDatabase.TransactionManager.StartTransaction())
            {

                // Open the Block table for read
                BlockTable acBlkTbl = tr.GetObject(AcadDatabase.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Get the current space
                BlockTableRecord space = tr.GetObject(AcadDatabase.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;

                // Create a new leader object
                Leader leader = new Leader();

                // Set the leader's properties
                leader.SetDatabaseDefaults();
                //leader.ContentType = ContentType.MTextContent;
                //leader. TextHeight = 2.5;
                //leader.ArrowSize = 2.0;

                // Create a new MText object and set its contents
                MText mtext = new MText();
                mtext.SetDatabaseDefaults();
                mtext.Contents = "Sample leader text";
                mtext.Location = new Point3d(5, 5, 0);

                // Add the MText object to the leader
                //leader.MText = mtext;

                // Set the leader's vertices
                Point3d startPt = new Point3d(0, 0, 0);
                Point3d midPt = new Point3d(4, 4, 0);
                Point3d endPt = new Point3d(5, 4, 0);
                leader.AppendVertex(startPt);
                leader.AppendVertex(midPt);
                leader.AppendVertex(endPt);
                leader.HasArrowHead = true;

                // Add the leader to the current space
                space.AppendEntity(leader);
                tr.AddNewlyCreatedDBObject(leader, true);

                space.AppendEntity(mtext);
                tr.AddNewlyCreatedDBObject(mtext, true);

                // Attach the annotation after the leader object is added
                //leader.Annotation = mtext.ObjectId;
                //leader.EvaluateLeader();

                // Commit the transaction
                tr.Commit();
            }




        }

        [Obsolete]
        void CreateLeaderfromfile(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase)
        {


            // Get the assembly that contains the code that is currently executing
            Assembly currentAssembly = Assembly.GetExecutingAssembly();

            // Get the location of the assembly file
            string assemblyPath = currentAssembly.Location;

            // Get the current working directory
            string workingDirectory = Directory.GetCurrentDirectory();

            // Get the project file path by searching for the .csproj file in the working directory
            string projectFilePath = Directory.GetFiles(workingDirectory, "LeaderBlock.dwg").FirstOrDefault();

            //string dwgPath = "D:\\Projects\\Plant 3D\\LeaderBlock.dwg";


            // Create a new database object
            Database db = new Database(true, true);
            db.ReadDwgFile(projectFilePath, FileOpenMode.OpenForReadAndWriteNoShare, true, "");

            Transaction tr2 = db.TransactionManager.StartTransaction();
            // Open the Block table for read
            BlockTable btsrc = tr2.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

            // Get the current space
            BlockTableRecord btrsrc = tr2.GetObject(btsrc[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

            //Create a matrix and move the block using vector from (0,0,0) to (153334.4278,117162.4417,0)
            //Point3d strpt = new Point3d(0, 0, 0);
            //Vector3d destvect = strpt.GetVectorTo(new Point3d(153334.4278, 117162.4417, 0));



            // get the model space object ids for both dbs
            ObjectId sourceMsId = SymbolUtilityServices.GetBlockModelSpaceId(db);
            ObjectId destDbMsId = SymbolUtilityServices.GetBlockModelSpaceId(AcadDatabase);

            // now create an array of object ids to hold the source objects to copy
            ObjectIdCollection sourceIds = new ObjectIdCollection();

            // open the sourceDb ModelSpace (current autocad dwg)

            BlockTableRecord ms = sourceMsId.Open(OpenMode.ForRead) as BlockTableRecord;

            // loop all the entities and record their ids


            foreach (ObjectId id in ms)
            {
                Point3d strpt = new Point3d(0, 0, 0);
                Vector3d destvect = strpt.GetVectorTo(new Point3d(153334.4278, 117162.4417, 0));

                //BlockReference blkRef = tr2.GetObject(id, OpenMode.ForRead) as BlockReference;
                //Point3d pos = blkRef.Position.Value;



                var ent = (Entity)tr2.GetObject(id, OpenMode.ForWrite);
                var ent2 = (Entity)tr2.GetObject(id, OpenMode.ForRead);
                ent.TransformBy(Matrix3d.Displacement(destvect));
                // Point3d pos=ent2.Position
                //ent.TransformBy(Matrix3d.);
                sourceIds.Add(id);
            }





            // next prepare to deepclone the recorded ids to the destdb

            IdMapping mapping = new IdMapping();

            // now clone the objects into the destdb

            db.WblockCloneObjects(sourceIds, destDbMsId, mapping, DuplicateRecordCloning.Replace, false);

            //AcadDatabase.SaveAs("c:\\temp\\dwgs\\CopyTest.dwg", DwgVersion.Current);

            //AcadDatabase.Save();

            tr2.Commit();
            db.Dispose();
            //// Read the DWG file into the database object
            //string dwgPath = "D:\\Projects\\Plant 3D\\Testmod.dwg";
            //db.ReadDwgFile(dwgPath, FileOpenMode.OpenForReadAndWriteNoShare, true, "");

            //List<Entity> entitiesToReturn = new List<Entity>(); //Blocks that will be returned
            //Transaction tr = db.TransactionManager.StartTransaction();
            ////DocumentLock docLock = _activeDocument.LockDocument();

            //using (tr)
            ////using (docLock)
            //{
            //    BlockTableRecord blockTableRecord = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForRead);
            //    foreach (ObjectId id in blockTableRecord)
            //    {
            //        try
            //        {
            //            Entity ent = (Entity)tr.GetObject(id, OpenMode.ForWrite);
            //            entitiesToReturn.Add(ent);
            //        }
            //        catch (InvalidCastException)
            //        {
            //            continue;
            //        }
            //    }
            //}



            //foreach(Entity ent in entitiesToReturn)
            //{
            //    AcadBlockTableRecord.AppendEntity(ent);
            //    tr.AddNewlyCreatedDBObject(ent, true);
            //}
        }

        [Obsolete]
        void GetTemplate(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase,double inserptX)
        {

            // Get the current working directory
            string workingDirectory = Directory.GetCurrentDirectory();

            // Get the project file path by searching for the .csproj file in the working directory
            string projectFilePath = Directory.GetFiles(workingDirectory, "Template.dwg").FirstOrDefault();

            //string dwgPath = "D:\\Projects\\Plant 3D\\LeaderBlock.dwg";


            // Create a new database object
            Database db = new Database(true, true);
            db.ReadDwgFile(projectFilePath, FileOpenMode.OpenForReadAndWriteNoShare, true, "");

            Transaction tr2 = db.TransactionManager.StartTransaction();
            // Open the Block table for read
            BlockTable btsrc = tr2.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

            // Get the current space
            BlockTableRecord btrsrc = tr2.GetObject(btsrc[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

            // get the model space object ids for both dbs
            ObjectId sourceMsId = SymbolUtilityServices.GetBlockModelSpaceId(db);
            ObjectId destDbMsId = SymbolUtilityServices.GetBlockModelSpaceId(AcadDatabase);

            // now create an array of object ids to hold the source objects to copy
            ObjectIdCollection sourceIds = new ObjectIdCollection();

            // open the sourceDb ModelSpace (current autocad dwg)

            BlockTableRecord ms = sourceMsId.Open(OpenMode.ForRead) as BlockTableRecord;

            // loop all the entities and record their ids


            foreach (ObjectId id in ms)
            {
                Point3d strpt = new Point3d(0, 0, 0);
                Vector3d destvect = strpt.GetVectorTo(new Point3d(inserptX, 0, 0));
                var ent = (Entity)tr2.GetObject(id, OpenMode.ForWrite);
                ent.TransformBy(Matrix3d.Displacement(destvect));
                //ent.UpgradeOpen();
                if (ent.IsWriteEnabled)
                {
                    // Object is not a proxy, clone it as-is
                    sourceIds.Add(id);
                }
                else
                {
                    // Object is a proxy, decompose and clone the copy
                    //DBObject copy = ent.DecomposeForSave();
                    //destBtr.AppendEntity(copy as Entity);
                    //tr.AddNewlyCreatedDBObject(copy, true);
                }
                // Make modifications to the object as needed
                //ent.Color = Color.FromRgb(255, 0, 0);


                // Save changes and dispose of the object
                //ent.DowngradeOpen();

            }

            // next prepare to deepclone the recorded ids to the destdb

            IdMapping mapping = new IdMapping();

            // now clone the objects into the destdb

            db.WblockCloneObjects(sourceIds, destDbMsId, mapping, DuplicateRecordCloning.Replace, false);
            tr2.Commit();
            db.Dispose();

        }
        void CreateSingleLeadrwidtxt(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase)
        {
            // Create the MText annotation
            MText acMText = new MText();
            acMText.SetDatabaseDefaults();
            acMText.Contents = "DECK PANEL";
            acMText.Location = new Point3d(153334.4278 + 1500, 117162.4417, 0);
            //acMText.Width = 2;
            acMText.TextHeight = 80;
            // Add the new object to Model space and the transaction
            AcadBlockTableRecord.AppendEntity(acMText);
            AcadTransaction.AddNewlyCreatedDBObject(acMText, true);
            //ObjectId objectIds1 = SymbolUtilityServices.GetBlockModelSpaceId(AcadDatabase);

            // objectIds1.Add(acMText.Id); 
            Leader acLdr = new Leader();
            acLdr.SetDatabaseDefaults();
            acLdr.AppendVertex(new Point3d(153334.4278, 117162.4417, 0));
            acLdr.AppendVertex(new Point3d(153334.4278 + 1500, 117162.4417, 0));
            acLdr.HasArrowHead = true;
            //acLd.

            TextStyleTable acTStyleTbl = AcadTransaction.GetObject(AcadDatabase.TextStyleTableId, OpenMode.ForRead) as TextStyleTable;

            DimStyleTable acDimStyleTbl = AcadTransaction.GetObject(AcadDatabase.DimStyleTableId, OpenMode.ForRead) as DimStyleTable;

            // Get the index of the dimension style named "ISO-25"
            //int iso25Index = acDimStyleTbl.FindIndex("ISO-25");

            // Get the dimension style at the specified index
            //ObjectId acDimStyleId = acDimStyleTbl[iso25Index];

            ObjectId acDimStyleId = acDimStyleTbl["ISO-25"];

            acLdr.TextStyleId = acDimStyleId;



            acLdr.Color = Autodesk.AutoCAD.Colors.Color.FromColor(System.Drawing.Color.Cyan);
            //acLdr.Dimclrd = Autodesk.AutoCAD.Colors.Color.FromColor(System.Drawing.Color.Green);                     //Add the new object to Model space and the transaction
            AcadBlockTableRecord.AppendEntity(acLdr);
            AcadTransaction.AddNewlyCreatedDBObject(acLdr, true);
            acLdr.Annotation = acMText.ObjectId;
            acLdr.EvaluateLeader();
        }

        //all fix functions

        //fix new function for primary support
        private void FixCreatePrimarySupportwithvertex(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, double centerX, double centerY)
        {
            //centerX = 9869.9480;
            //centerY = 67542.6980;
            // double seheight1 = Sesupport.Boundingboxmax.Z - Sesupport.Boundingboxmin.Z;
            //Point3d center = new Point3d(Sesupport.Boundingboxmin.X + Sesupport.Boundingboxmax.X / 2, Sesupport.Boundingboxmin.Y + Sesupport.Boundingboxmax.Y / 2+add,0);
            Point3d center = new Point3d(centerX, centerY, 0);
            //Point2d min = new Point2d(Sesupport.Boundingboxmin.X, Sesupport.Boundingboxmin.Y);
            //Point2d max = new Point2d(Sesupport.Boundingboxmax.X, Sesupport.Boundingboxmax.Y);
            //double radius = max.GetDistanceTo(min) / 4;
            double radius = 801.5625;
            LinetypeTable acLineTypTbl;
            acLineTypTbl = AcadTransaction.GetObject(AcadDatabase.LinetypeTableId,
                                                   OpenMode.ForRead) as LinetypeTable;
            string sLineTypName = "PHANTOM2";
            if (acLineTypTbl.Has(sLineTypName) == false)
            {
                AcadDatabase.LoadLineTypeFile(sLineTypName, "acad.lin");
            }
            //double radius = height1/2;
            Circle circle = new Circle();
            Circle secCircle = new Circle();
            circle.Radius = radius;
            circle.Center = center;
            circle.Linetype = sLineTypName;
            circle.LinetypeScale = 0.2;
            circle.Color = Color.FromColorIndex(ColorMethod.ByAci, 8);



            AcadBlockTableRecord.AppendEntity(circle);
            AcadTransaction.AddNewlyCreatedDBObject(circle, true);

            secCircle.Radius = radius - 5;
            secCircle.Center = center;
            secCircle.Linetype = sLineTypName;
            secCircle.Color = Color.FromColorIndex(ColorMethod.ByAci, 8);
            secCircle.LinetypeScale = 0.2;
            AcadBlockTableRecord.AppendEntity(secCircle);
            AcadTransaction.AddNewlyCreatedDBObject(secCircle, true);
            //Adds the arc and line to an object id collection
            ObjectIdCollection acObjIdColl = new ObjectIdCollection();
            acObjIdColl.Add(circle.ObjectId);
            ObjectIdCollection acObjectColl1 = new ObjectIdCollection();
            acObjectColl1.Add(secCircle.ObjectId);
            // Create the hatch object and append it to the block table record
            Hatch acHatch = new Hatch();
            AcadBlockTableRecord.AppendEntity(acHatch);
            AcadTransaction.AddNewlyCreatedDBObject(acHatch, true);

            // Set the properties of the hatch object
            // Associative must be set after the hatch object is appended to the 
            // block table record and before AppendLoop
            acHatch.SetDatabaseDefaults();
            acHatch.SetHatchPattern(HatchPatternType.PreDefined, "ANSI31");
            acHatch.Associative = true;
            acHatch.AppendLoop(HatchLoopTypes.Outermost, acObjIdColl);
            acHatch.AppendLoop(HatchLoopTypes.Outermost, acObjectColl1);
            acHatch.HatchStyle = HatchStyle.Normal;
            acHatch.Linetype = sLineTypName;
            acHatch.LinetypeScale = 0.2;
            acHatch.Color = Color.FromColorIndex(ColorMethod.ByAci, 8);
            // Evaluate the hatch
            acHatch.EvaluateHatch(true);

            // Increase the pattern scale by 2 and re-evaluate the hatch
            acHatch.PatternScale = acHatch.PatternScale + 69.23;
            acHatch.SetHatchPattern(acHatch.PatternType, acHatch.PatternName);
            acHatch.EvaluateHatch(true);

            //lines below circle
            //Pt2D1 = new Point2d(prmsupport1.Boundingboxmax.X, prmsupport1.Boundingboxmin.Y + newheight + height1);
            double ht_frm_cen = 1220.7383;
            //Pt2D1 = new Point2d(prmsupport1.Boundingboxmin.X, prmsupport1.Boundingboxmin.Y + height1 + newheight);
            Point3d lpt1 = new Point3d(centerX - radius * 0.75, centerY - ht_frm_cen, 0);
            Point3d lpt2 = new Point3d(centerX - radius * 0.75, centerY, 0);
            Line line1 = new Line(lpt1, lpt2);
            Point3dCollection intersectionPoints = new Point3dCollection();
            line1.IntersectWith(circle, Intersect.OnBothOperands, intersectionPoints, IntPtr.Zero, IntPtr.Zero);
            if (intersectionPoints.Count > 0)
            {
                line1.EndPoint = intersectionPoints[0];
            }
            AcadBlockTableRecord.AppendEntity(line1);
            line1.Color = Color.FromColorIndex(ColorMethod.ByAci, 171);
            AcadTransaction.AddNewlyCreatedDBObject(line1, true);

            Point3d lpt3 = new Point3d(centerX + radius * 0.75, centerY - ht_frm_cen, 0);
            Point3d lpt4 = new Point3d(centerX + radius * 0.75, centerY, 0);
            Line line2 = new Line(lpt3, lpt4);
            Point3dCollection intersectionPoints2 = new Point3dCollection();
            line2.IntersectWith(circle, Intersect.OnBothOperands, intersectionPoints2, IntPtr.Zero, IntPtr.Zero);
            if (intersectionPoints2.Count > 0)
            {
                line2.EndPoint = intersectionPoints2[0];
            }
            AcadBlockTableRecord.AppendEntity(line2);
            line2.Color = Color.FromColorIndex(ColorMethod.ByAci, 171);
            AcadTransaction.AddNewlyCreatedDBObject(line2, true);

        }

        //function for center mark



        //fix new function for Secondary Support Top
        private void FixCreateSecondarySupportTop(BlockTableRecord acadBlockTableRecord, Transaction acadTransaction, Database acadDatabase, double centerX, double centerY)
        {
            double height = 1000.0000;
            double length = 2666.6667;
            double ht_frm_cen = 1220.7383;
            var newline = new Polyline();
            //rectangular polyline
            Point2d Pt2D1 = new Point2d(centerX - length / 2, centerY - ht_frm_cen);
            newline.AddVertexAt(0, Pt2D1, 0, 0, 0);
            Pt2D1 = new Point2d(centerX + length / 2, centerY - ht_frm_cen);
            newline.AddVertexAt(1, Pt2D1, 0, 0, 0);
            Pt2D1 = new Point2d(centerX + length / 2, centerY - ht_frm_cen - height);
            newline.AddVertexAt(2, Pt2D1, 0, 0, 0);
            Pt2D1 = new Point2d(centerX - length / 2, centerY - ht_frm_cen - height);
            newline.AddVertexAt(3, Pt2D1, 0, 0, 0);
            Pt2D1 = new Point2d(centerX - length / 2, centerY - ht_frm_cen);
            newline.AddVertexAt(4, Pt2D1, 0, 0, 0);
            newline.Color = Color.FromColorIndex(ColorMethod.ByAci, 171);
            acadBlockTableRecord.AppendEntity(newline);
            acadTransaction.AddNewlyCreatedDBObject(newline, true);

            //other two lines in rectangle
            double gap = 100;
            Point3d pt1 = new Point3d(centerX - length / 2, centerY - ht_frm_cen - gap, 0);
            Point3d Pt2 = new Point3d(centerX + length / 2, centerY - ht_frm_cen - gap, 0);
            Line line = new Line(pt1, Pt2);
            acadBlockTableRecord.AppendEntity(line);
            line.Color = Color.FromColorIndex(ColorMethod.ByAci, 171);
            acadTransaction.AddNewlyCreatedDBObject(line, true);

            Point3d lpt1 = new Point3d(centerX - length / 2, centerY - ht_frm_cen - height + gap, 0);
            Point3d lpt2 = new Point3d(centerX + length / 2, centerY - ht_frm_cen - height + gap, 0);
            Line line1 = new Line(lpt1, lpt2);
            acadBlockTableRecord.AppendEntity(line1);
            line1.Color = Color.FromColorIndex(ColorMethod.ByAci, 171);
            acadTransaction.AddNewlyCreatedDBObject(line1, true);
        }

        //fix new function for Secondary Support Bottom
        private void FixCreateSecondarySupportBottom(BlockTableRecord acadBlockTableRecord, Transaction AcadTransaction, Database acadDatabase, double centerX, double centerY)
        {
            double height = 3066.5059;
            double length = 1000.0000;
            double ht_frm_cen = 1220.7383 + 1000;
            var line = new Polyline();
            //double height1 = prmsupport.Boundingboxmax.Z - prmsupport.Boundingboxmin.Z;
            Point2d Pt2D1 = new Point2d(centerX - length / 2, centerY - ht_frm_cen);
            line.AddVertexAt(0, Pt2D1, 0, 0, 0);
            Pt2D1 = new Point2d(centerX + length / 2, centerY - ht_frm_cen);
            line.AddVertexAt(1, Pt2D1, 0, 0, 0);
            Pt2D1 = new Point2d(centerX + length / 2, centerY - ht_frm_cen - height);
            line.AddVertexAt(2, Pt2D1, 0, 0, 0);
            Pt2D1 = new Point2d(centerX - length / 2, centerY - ht_frm_cen - height);
            line.AddVertexAt(3, Pt2D1, 0, 0, 0);
            Pt2D1 = new Point2d(centerX - length / 2, centerY - ht_frm_cen);
            line.AddVertexAt(4, Pt2D1, 0, 0, 0);
            line.Color = Color.FromColorIndex(ColorMethod.ByAci, 171);
            acadBlockTableRecord.AppendEntity(line);
            AcadTransaction.AddNewlyCreatedDBObject(line, true);

            double gap = 100;
            Point3d pt1 = new Point3d(centerX - length / 2 + gap, centerY - ht_frm_cen, 0);
            Point3d Pt2 = new Point3d(centerX - length / 2 + gap, centerY - ht_frm_cen - height, 0);
            Line innerLine = new Line(pt1, Pt2);
            acadBlockTableRecord.AppendEntity(innerLine);
            innerLine.Color = Color.FromColorIndex(ColorMethod.ByAci, 171);
            AcadTransaction.AddNewlyCreatedDBObject(innerLine, true);

            Point3d lpt1 = new Point3d(centerX + length / 2 - gap, centerY - ht_frm_cen, 0);
            Point3d lpt2 = new Point3d(centerX + length / 2 - gap, centerY - ht_frm_cen - height, 0);
            Line line1 = new Line(lpt1, lpt2);
            acadBlockTableRecord.AppendEntity(line1);
            line1.Color = Color.FromColorIndex(ColorMethod.ByAci, 171);
            AcadTransaction.AddNewlyCreatedDBObject(line1, true);

            FixCreateSideSecondarySupportBottom(AcadTransaction, acadBlockTableRecord, acadDatabase, "Left", centerX, centerY);

        }

        //fix new function for Side Secondary Support Bottom
        private void FixCreateSideSecondarySupportBottom(Transaction AcadTransaction, BlockTableRecord acadBlockTableRecord, Database acadDatabase, string side, double centerX, double centerY)
        {
            double gap_support = 1000 / 2;
            double height = 568.9965;
            double length = 2666.6667;
            double ht_frm_cen = 1220.7383 + 1000 + 2083;


            var line = new Polyline();

            //double height1 = prmsupport.Boundingboxmax.Z - prmsupport.Boundingboxmin.Z;
            if (side == "Left")
            {
                Point2d Pt2D1 = new Point2d(centerX - gap_support, centerY - ht_frm_cen);
                line.AddVertexAt(0, Pt2D1, 0, 0, 0);
                Pt2D1 = new Point2d(centerX - gap_support - length / 2, centerY - ht_frm_cen);
                line.AddVertexAt(1, Pt2D1, 0, 0, 0);
                Pt2D1 = new Point2d(centerX - gap_support - length / 2, centerY - ht_frm_cen - height);
                line.AddVertexAt(2, Pt2D1, 0, 0, 0);
                Pt2D1 = new Point2d(centerX - gap_support, centerY - ht_frm_cen - height);
                line.AddVertexAt(3, Pt2D1, 0, 0, 0);
                Pt2D1 = new Point2d(centerX - gap_support, centerY - ht_frm_cen);
                line.AddVertexAt(4, Pt2D1, 0, 0, 0);
                line.Color = Color.FromColorIndex(ColorMethod.ByAci, 171);
                acadBlockTableRecord.AppendEntity(line);
                AcadTransaction.AddNewlyCreatedDBObject(line, true);

                //offset line
                double gap = 100;
                Point3d pt1 = new Point3d(centerX - gap_support, centerY - ht_frm_cen - gap, 0);
                Point3d Pt2 = new Point3d(centerX - gap_support - length / 2, centerY - ht_frm_cen - gap, 0);
                Line innerLine = new Line(pt1, Pt2);
                acadBlockTableRecord.AppendEntity(innerLine);
                innerLine.Color = Color.FromColorIndex(ColorMethod.ByAci, 171);
                AcadTransaction.AddNewlyCreatedDBObject(innerLine, true);
            }
            else if (side == "Right")
            {
                Point2d Pt2D1 = new Point2d(centerX + gap_support, centerY - ht_frm_cen);
                line.AddVertexAt(0, Pt2D1, 0, 0, 0);
                Pt2D1 = new Point2d(centerX + gap_support + length / 2, centerY - ht_frm_cen);
                line.AddVertexAt(1, Pt2D1, 0, 0, 0);
                Pt2D1 = new Point2d(centerX + gap_support + length / 2, centerY - ht_frm_cen - height);
                line.AddVertexAt(2, Pt2D1, 0, 0, 0);
                Pt2D1 = new Point2d(centerX + gap_support, centerY - ht_frm_cen - height);
                line.AddVertexAt(3, Pt2D1, 0, 0, 0);
                Pt2D1 = new Point2d(centerX + gap_support, centerY - ht_frm_cen);
                line.AddVertexAt(4, Pt2D1, 0, 0, 0);
                line.Color = Color.FromColorIndex(ColorMethod.ByAci, 171);
                acadBlockTableRecord.AppendEntity(line);
                AcadTransaction.AddNewlyCreatedDBObject(line, true);

                //offset line
                double gap = 100;
                Point3d pt1 = new Point3d(centerX + gap_support, centerY - ht_frm_cen - gap, 0);
                Point3d Pt2 = new Point3d(centerX + gap_support + length / 2, centerY - ht_frm_cen - gap, 0);
                Line innerLine = new Line(pt1, Pt2);
                acadBlockTableRecord.AppendEntity(innerLine);
                innerLine.Color = Color.FromColorIndex(ColorMethod.ByAci, 171);
                AcadTransaction.AddNewlyCreatedDBObject(innerLine, true);
            }
            else
            {

            }



        }

        //fix new function for Bottom Support Top
        private void FixCreateBottomSupportTopType2(BlockTableRecord acadBlockTableRecord, Transaction acadTransaction, Database acadDatabase, double centerX, double centerY)
        {

            double height = 1204.4116;
            double length = 2952.5332;
            double ht_frm_cen = 1220.7383 + 1000 + 3066.5059;

            var newline = new Polyline();
            Point2d Pt2D1 = new Point2d(centerX - length / 2, centerY - ht_frm_cen);
            newline.AddVertexAt(0, Pt2D1, 0, 0, 0);
            Pt2D1 = new Point2d(centerX + length / 2, centerY - ht_frm_cen);
            newline.AddVertexAt(1, Pt2D1, 0, 0, 0);
            Pt2D1 = new Point2d(centerX + length / 2, centerY - ht_frm_cen - height);
            newline.AddVertexAt(2, Pt2D1, 0, 0, 0);
            Pt2D1 = new Point2d(centerX - length / 2, centerY - ht_frm_cen - height);
            newline.AddVertexAt(3, Pt2D1, 0, 0, 0);
            Pt2D1 = new Point2d(centerX - length / 2, centerY - ht_frm_cen);
            newline.AddVertexAt(4, Pt2D1, 0, 0, 0);
            newline.Color = Color.FromColorIndex(ColorMethod.ByAci, 8);

            LinetypeTable acLineTypTbl;
            acLineTypTbl = acadTransaction.GetObject(acadDatabase.LinetypeTableId,
                                                   OpenMode.ForRead) as LinetypeTable;
            string sLineTypName = "DASHED";
            if (acLineTypTbl.Has(sLineTypName) == false)
            {
                acadDatabase.LoadLineTypeFile(sLineTypName, "acad.lin");
                newline.Linetype = sLineTypName;

            }
            acadBlockTableRecord.AppendEntity(newline);
            acadTransaction.AddNewlyCreatedDBObject(newline, true);

            //inside upper block
            double offsetht = 162.6694;
            double offsetinside = 433.7815;
            var insideupperblock = new Polyline();
            Point2d Pt2D2 = new Point2d(centerX - length / 2 + offsetinside, centerY - ht_frm_cen);
            insideupperblock.AddVertexAt(0, Pt2D2, 0, 0, 0);
            Pt2D2 = new Point2d(centerX + length / 2 - offsetinside, centerY - ht_frm_cen);
            insideupperblock.AddVertexAt(1, Pt2D2, 0, 0, 0);
            Pt2D2 = new Point2d(centerX + length / 2 - offsetinside, centerY - ht_frm_cen - offsetht);
            insideupperblock.AddVertexAt(2, Pt2D2, 0, 0, 0);
            Pt2D2 = new Point2d(centerX - length / 2 + offsetinside, centerY - ht_frm_cen - offsetht);
            insideupperblock.AddVertexAt(3, Pt2D2, 0, 0, 0);
            Pt2D2 = new Point2d(centerX - length / 2 + offsetinside, centerY - ht_frm_cen);
            insideupperblock.AddVertexAt(4, Pt2D2, 0, 0, 0);
            insideupperblock.AddVertexAt(4, Pt2D2, 0, 0, 0);
            insideupperblock.Color = Color.FromColorIndex(ColorMethod.ByAci, 8);


            if (acLineTypTbl.Has(sLineTypName) == true)
            {
                //acadDatabase.LoadLineTypeFile(sLineTypName, "acad.lin");
                insideupperblock.Linetype = sLineTypName;
            }
            acadBlockTableRecord.AppendEntity(insideupperblock);
            acadTransaction.AddNewlyCreatedDBObject(insideupperblock, true);

            //vertical side lines 
            double lineHt = 500;
            Point3d pt1 = new Point3d(centerX - length / 2 + 2 * offsetinside, centerY - ht_frm_cen - offsetht, 0);
            Point3d Pt2 = new Point3d(centerX - length / 2 + 2 * offsetinside, centerY - ht_frm_cen - offsetht - lineHt, 0);
            Line line = new Line(pt1, Pt2);
            acadBlockTableRecord.AppendEntity(line);
            line.Color = Color.FromColorIndex(ColorMethod.ByAci, 171);
            acadTransaction.AddNewlyCreatedDBObject(line, true);


            Point3d pt12 = new Point3d(centerX + length / 2 - 2 * offsetinside, centerY - ht_frm_cen - offsetht, 0);
            Point3d Pt22 = new Point3d(centerX + length / 2 - 2 * offsetinside, centerY - ht_frm_cen - offsetht - lineHt, 0);
            Line line2 = new Line(pt12, Pt22);
            acadBlockTableRecord.AppendEntity(line2);
            line2.Color = Color.FromColorIndex(ColorMethod.ByAci, 171);
            acadTransaction.AddNewlyCreatedDBObject(line2, true);

            //horizontal side lines
            Point3d pt3 = new Point3d(centerX - length / 2 + 2 * offsetinside, centerY - ht_frm_cen - offsetht - lineHt, 0);
            Point3d Pt4 = new Point3d(centerX - length / 2, centerY - ht_frm_cen - offsetht - lineHt, 0);
            Line lineh1 = new Line(pt3, Pt4);
            acadBlockTableRecord.AppendEntity(lineh1);
            lineh1.Color = Color.FromColorIndex(ColorMethod.ByAci, 83);
            acadTransaction.AddNewlyCreatedDBObject(lineh1, true);

            //horizontal side lines
            Point3d pt5 = new Point3d(centerX + length / 2 - 2 * offsetinside, centerY - ht_frm_cen - offsetht - lineHt, 0);
            Point3d Pt6 = new Point3d(centerX + length / 2, centerY - ht_frm_cen - offsetht - lineHt, 0);
            Line lineh2 = new Line(pt5, Pt6);
            acadBlockTableRecord.AppendEntity(lineh2);
            lineh2.Color = Color.FromColorIndex(ColorMethod.ByAci, 83);
            acadTransaction.AddNewlyCreatedDBObject(lineh2, true);

        }

    }
}
