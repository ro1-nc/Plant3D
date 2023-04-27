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
using Autodesk.ProcessPower.P3dProjectParts;
using System.Xaml;
using System.Linq.Expressions;
using System.IO;
using System.Runtime.InteropServices;
using System.Reflection;

using MyCol = System.Drawing.Color;
using Autodesk.AutoCAD.BoundaryRepresentation;
using System.Numerics;
using Plane = Autodesk.AutoCAD.Geometry.Plane;
using Exception = System.Exception;
using Autodesk.AutoCAD.Runtime;

namespace Project1.Support2D
{
    public class SupportC
    {
        List<SupportData> ListCentalSuppoData = new List<SupportData>();
        CalculationMaths Calculate = new CalculationMaths();

        public double spaceX = 81971.6112;
        public double spaceY = 71075.0829;

        public double tempX = 101659.6570;
        public double tempY = 71694.2039;

        //datum level
        public double datum_level = 0.0;

        //for collecting information
        Dictionary<Defination, double> info = new Dictionary<Defination, double>();

        Dictionary<Defination, Point3d> pointsinfo = new Dictionary<Defination, Point3d>();

        //for collecting extra info
        Dictionary<string, double> extrainfo = new Dictionary<string, double>();

        Dictionary<string, Point3d> pointsextrainfo = new Dictionary<string, Point3d>();

        //dictionary for determining parts
        public Dictionary<string, string> Csectiondetails = new Dictionary<string, string>();

        public Dictionary<string, string> Lsectiondetails = new Dictionary<string, string>();


        //get doc
        public Document CurDoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
        public void ReadSupportData()
        {
            Document AcadDoc = null;
            Transaction AcadTransaction = null;
            BlockTable AcadBlockTable = null;
            BlockTableRecord AcadBlockTableRecord = null;
            Database AcadDatabase = null;
            Editor AcadEditor = null;
            //PromptSelectionResult selectionRes;

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

                                string[] LayersName = new string[3];

                                List<SupportData> RawSupportData = new List<SupportData>();

                                List<SupporSpecData> ListPSuppoData = new List<SupporSpecData>();
                                List<SupporSpecData> ListSecondarySuppoData = new List<SupporSpecData>();
                                List<SupporSpecData> ListConcreteSupportData = new List<SupporSpecData>();

                                LayersName = GetNamesOfSupportEnteredbyUser(AcadTransaction, AcadDatabase);

                                if (LayersName[0] != null && LayersName[0].Length > 0)
                                {
                                    ListPSuppoData = GetAllPrimarySupportData(AcadEditor, AcadTransaction, LayersName[0]);
                                }

                                if (LayersName[1] != null && LayersName[1].Length > 0)
                                {
                                    ListSecondarySuppoData = GetAllSecondarySupportData(AcadEditor, AcadTransaction, LayersName[1]);
                                }

                                if (LayersName[2] != null && LayersName[2].Length > 0)
                                {
                                    ListConcreteSupportData = GetAllConcreteSupportData(AcadEditor, AcadTransaction, LayersName[2]);
                                }

                                Dictionary<string, Point3d> DicTextPos = new Dictionary<string, Point3d>();

                                DicTextPos = GetAllTexts(AcadTransaction, AcadBlockTable);

                                SaparateSupports(ref RawSupportData, ListPSuppoData, ListSecondarySuppoData, ListConcreteSupportData, AcadTransaction, AcadBlockTable);

                                ListCentalSuppoData = RawSupportData;

                                /*
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

                                                    //blkRef.Rotation
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
                                */
                            }
                            AcadTransaction.Commit();
                        }
                    }
                }
            }
        }

        Dictionary<string, Point3d> GetAllTexts(Transaction AcadTransaction, BlockTable AcadBlockTable)
        {

            var AcadBlockTableRecord = AcadTransaction.GetObject(AcadBlockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

            Dictionary<string, Point3d> DicTextLoc = new Dictionary<string, Point3d>();

            foreach (var id in AcadBlockTableRecord)
            {
                if (id.ObjectClass.DxfName == "TEXT")
                {
                    DBText text = (DBText)AcadTransaction.GetObject(id, OpenMode.ForRead);
                    //text.Position
                    // text.TextString
                    DicTextLoc[text.TextString] = text.Position;
                }
                else if (id.ObjectClass.DxfName == "TEXT")
                {
                    MText mtext = (MText)AcadTransaction.GetObject(id, OpenMode.ForWrite);

                    Extents3d? Ext3d = mtext.Bounds;

                    DicTextLoc[mtext.Text] = Ext3d.Value.MinPoint;
                }

            }
            return DicTextLoc;
        }

        string GetTextInsidetheBBOx(Transaction AcadTransaction, BlockTable AcadBlockTable, Pt3D MinPoint, Pt3D MaxPoint)
        {

            var AcadBlockTableRecord = AcadTransaction.GetObject(AcadBlockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

            Dictionary<string, Point3d> DicTextLoc = new Dictionary<string, Point3d>();

            foreach (var id in AcadBlockTableRecord)
            {
                /*if (id.ObjectClass.DxfName == "TEXT")
                {
                    DBText text = (DBText)AcadTransaction.GetObject(id, OpenMode.ForRead);
                    //text.Position
                    // text.TextString
                    if((Math.Round(MinPoint.X)< Math.Round(text.Position.X) && Math.Round(text.Position.X) <= Math.Round(MaxPoint.X)&& Math.Round(MinPoint.Y) <= Math.Round(text.Position.Y) && Math.Round(text.Position.Y) <= Math.Round(MaxPoint.Y) && Math.Round(MinPoint.Z) <= Math.Round(text.Position.Z) && Math.Round(text.Position.Z) <= Math.Round(MaxPoint.Z))|| (Math.Round(MaxPoint.X) <= Math.Round(text.Position.X) && Math.Round(text.Position.X) <= Math.Round(MinPoint.X) && Math.Round(MaxPoint.Y) <= Math.Round(text.Position.Y) && Math.Round(text.Position.Y) <= Math.Round(MinPoint.Y) && Math.Round(MaxPoint.Z) <= Math.Round(text.Position.Z) && Math.Round(text.Position.Z) <= Math.Round(MinPoint.Z)))
                    {
                        return text.TextString;
                    }
                }
                else*/
                if (id.ObjectClass.DxfName.ToUpper().Contains("TEXT"))
                {
                    MText mtext = (MText)AcadTransaction.GetObject(id, OpenMode.ForWrite);

                    Extents3d? Ext3d = mtext.Bounds;


                    if ((Math.Round(MinPoint.X) < Math.Round(Ext3d.Value.MinPoint.X) && Math.Round(Ext3d.Value.MinPoint.X) <= Math.Round(MaxPoint.X) && Math.Round(MinPoint.Y) <= Math.Round(Ext3d.Value.MinPoint.Y) && Math.Round(Ext3d.Value.MinPoint.Y) <= Math.Round(MaxPoint.Y) && Math.Round(MinPoint.Z) <= Math.Round(Ext3d.Value.MinPoint.Z) && Math.Round(Ext3d.Value.MinPoint.Z) <= Math.Round(MaxPoint.Z)) || (Math.Round(MaxPoint.X) <= Math.Round(Ext3d.Value.MinPoint.X) && Math.Round(Ext3d.Value.MinPoint.X) <= Math.Round(MinPoint.X) && Math.Round(MaxPoint.Y) <= Math.Round(Ext3d.Value.MinPoint.Y) && Math.Round(Ext3d.Value.MinPoint.Y) <= Math.Round(MinPoint.Y) && Math.Round(MaxPoint.Z) <= Math.Round(Ext3d.Value.MinPoint.Z) && Math.Round(Ext3d.Value.MinPoint.Z) <= Math.Round(MinPoint.Z)))
                    {
                        return mtext.Text;
                    }
                }

            }
            return "";
        }

        void SaparateSupports(ref List<SupportData> RawSupportData, List<SupporSpecData> ListPSuppoData, List<SupporSpecData> ListSecondarySuppoData, List<SupporSpecData> ListConcreteSupportData, Transaction AcadTransaction, BlockTable AcadBlockTable)
        {
            //We will iterate over the Primary Support Get all the touching Support to it
            List<SupporSpecData> ListAllSuppoData = new List<SupporSpecData>();
            CombineSupportData(ref ListAllSuppoData, ListPSuppoData, ListSecondarySuppoData, ListConcreteSupportData);

            List<string> ListProcessedSupData = new List<string>();

            List<string> ListProcessedDataIds = new List<string>();

            foreach (SupporSpecData PSuppoData in ListPSuppoData)
            {

                SupportData SupData = new SupportData();
                List<SupporSpecData> ListSuppoData = new List<SupporSpecData>();


                string Text = ""; //GetTextInsidetheBBOx(AcadTransaction, AcadBlockTable, PSuppoData.Boundingboxmin, PSuppoData.Boundingboxmax);

                //if (RawSupportData.Exists(x => x.Name.Equals(Text)))
                //{
                //    RawSupportData.Find(x => x.Name.Equals(Text)).Quantity++;

                //  continue;
                // }

                if (ListProcessedDataIds != null && ListProcessedDataIds.Count > 0 && ListProcessedDataIds.Contains(PSuppoData.SuppoId))
                {
                    continue;
                }

                GetAllTouchingSecondarySupport(PSuppoData, ref ListSuppoData, ref ListProcessedDataIds, ListSecondarySuppoData);

                SupporSpecData TempSuppoData = new SupporSpecData();
                //Temporary Adding this  need to Modify This
                foreach (SupporSpecData Sup in ListSuppoData)
                {
                    if (Sup.SuppoId.Contains("S"))
                    {
                        TempSuppoData = Sup;
                    }
                }

                GetllTouchingParts(TempSuppoData, ref ListSuppoData, ref ListProcessedDataIds, ListAllSuppoData);

                SeparateAndFillSupport(ref ListSuppoData, ref RawSupportData, Text, AcadTransaction, AcadBlockTable);

            }
        }

        void SeparateAndFillSupport(ref List<SupporSpecData> ListSuppoData, ref List<SupportData> RawSupportData, string Name, Transaction AcadTransaction, BlockTable AcadBlockTable)
        {
            SupportData FullSuppo = new SupportData();
            List<SupporSpecData> ListPSuppoData = new List<SupporSpecData>();
            List<SupporSpecData> ListSecondarySuppoData = new List<SupporSpecData>();
            List<SupporSpecData> ListConcreteSupportData = new List<SupporSpecData>();
            foreach (SupporSpecData SupData in ListSuppoData)
            {
                if (SupData.SuppoId.Contains("P"))
                {
                    ListPSuppoData.Add(SupData);
                }
                else if (SupData.SuppoId.Contains("S"))
                {
                    ListSecondarySuppoData.Add(SupData);
                }
                else if (SupData.SuppoId.Contains("C"))
                {
                    ListConcreteSupportData.Add(SupData);
                }

                string text = GetTextInsidetheBBOx(AcadTransaction, AcadBlockTable, SupData.Boundingboxmin, SupData.Boundingboxmax);
                if (text.Length > 0)
                {
                    Name = GetTextInsidetheBBOx(AcadTransaction, AcadBlockTable, SupData.Boundingboxmin, SupData.Boundingboxmax);
                }
            }

            FullSuppo.ListPrimarySuppo = ListPSuppoData;
            FullSuppo.ListSecondrySuppo = ListSecondarySuppoData;
            FullSuppo.ListConcreteData = ListConcreteSupportData;
            FullSuppo.Name = Name;
            FullSuppo.Quantity++;

            RawSupportData.Add(FullSuppo);
        }

        void CombineSupportData(ref List<SupporSpecData> ListAllSuppoData, List<SupporSpecData> ListPSuppoData, List<SupporSpecData> ListSecondarySuppoData, List<SupporSpecData> ListConcreteSupportData)
        {
            foreach (SupporSpecData Data in ListPSuppoData)
            {
                ListAllSuppoData.Add(Data);
            }

            foreach (SupporSpecData Data in ListSecondarySuppoData)
            {
                ListAllSuppoData.Add(Data);
            }

            foreach (SupporSpecData Data in ListConcreteSupportData)
            {
                ListAllSuppoData.Add(Data);
            }
        }

        void GetllTouchingParts(SupporSpecData SupData, ref List<SupporSpecData> ListSuppoData, ref List<string> ListProcessedDataIds, List<SupporSpecData> ListAllSuppoData)
        {
            List<SupporSpecData> ListTouchingSuppo = new List<SupporSpecData>();

            foreach (SupporSpecData SSupportData in ListAllSuppoData)
            {
                if (ListProcessedDataIds.Contains(SSupportData.SuppoId))
                {
                    continue;
                }

                bool IsXRange = false;
                bool IsYRange = false;
                bool IsZRange = false;

                if (Math.Round(SSupportData.Boundingboxmin.X) <= Math.Round(SupData.Boundingboxmin.X) && Math.Round(SupData.Boundingboxmax.X) <= Math.Round(SSupportData.Boundingboxmax.X))
                {
                    IsXRange = true;
                }
                else if (Math.Round(SSupportData.Boundingboxmin.X) <= Math.Round(SupData.Boundingboxmin.X) && Math.Round(SupData.Boundingboxmin.X) <= Math.Round(SSupportData.Boundingboxmax.X))
                {
                    IsXRange = true;
                }
                else if (Math.Round(SSupportData.Boundingboxmin.X) <= Math.Round(SupData.Boundingboxmax.X) && Math.Round(SupData.Boundingboxmax.X) <= Math.Round(SSupportData.Boundingboxmax.X))
                {
                    IsXRange = true;
                }
                else if (Math.Round(SupData.Boundingboxmin.X) <= Math.Round(SSupportData.Boundingboxmin.X) && Math.Round(SSupportData.Boundingboxmax.X) <= Math.Round(SupData.Boundingboxmax.X))
                {
                    IsXRange = true;
                }
                /*   else if (Math.Round(SupData.Boundingboxmin.X) <= Math.Round(SSupportData.Boundingboxmin.X) && Math.Round(SSupportData.Boundingboxmin.X) <= Math.Round(SupData.Boundingboxmax.X))
                   {
                       IsXRange = true;
                   }
                   else if (Math.Round(SSupportData.Boundingboxmin.X) <= Math.Round(SSupportData.Boundingboxmax.X) && Math.Round(SSupportData.Boundingboxmax.X) <= Math.Round(SupData.Boundingboxmax.X))
                   {
                       IsXRange = true;
                   }*/

                if (Math.Round(SSupportData.Boundingboxmin.Y) <= Math.Round(SupData.Boundingboxmin.Y) && Math.Round(SupData.Boundingboxmax.Y) <= Math.Round(SSupportData.Boundingboxmax.Y))
                {
                    IsYRange = true;
                }
                else if (Math.Round(SSupportData.Boundingboxmin.Y) <= Math.Round(SupData.Boundingboxmin.Y) && Math.Round(SupData.Boundingboxmin.Y) <= Math.Round(SSupportData.Boundingboxmax.Y))
                {
                    IsYRange = true;
                }
                else if (Math.Round(SSupportData.Boundingboxmin.Y) <= Math.Round(SupData.Boundingboxmax.Y) && Math.Round(SupData.Boundingboxmax.Y) <= Math.Round(SSupportData.Boundingboxmax.Y))
                {
                    IsYRange = true;
                }
                else if (Math.Round(SupData.Boundingboxmin.Y) <= Math.Round(SSupportData.Boundingboxmin.Y) && Math.Round(SSupportData.Boundingboxmax.Y) <= Math.Round(SupData.Boundingboxmax.Y))
                {
                    IsYRange = true;
                }
                /*else if (Math.Round(SupData.Boundingboxmin.Y) <= Math.Round(SSupportData.Boundingboxmin.Y) && Math.Round(SSupportData.Boundingboxmin.Y) <= Math.Round(SupData.Boundingboxmax.Y))
                {
                    IsYRange = true;
                }
                else if (Math.Round(SSupportData.Boundingboxmin.Y) <= Math.Round(SSupportData.Boundingboxmax.Y) && Math.Round(SSupportData.Boundingboxmax.Y) <= Math.Round(SupData.Boundingboxmax.Y))
                {
                    IsYRange = true;
                }
                */

                if (Math.Round(SSupportData.Boundingboxmin.Z) <= Math.Round(SupData.Boundingboxmin.Z) && Math.Round(SupData.Boundingboxmax.Z) <= Math.Round(SSupportData.Boundingboxmax.Z))
                {
                    IsZRange = true;
                }
                else if (Math.Round(SSupportData.Boundingboxmin.Z) <= Math.Round(SupData.Boundingboxmin.Z) && Math.Round(SupData.Boundingboxmin.Z) <= Math.Round(SSupportData.Boundingboxmax.Z))
                {
                    IsZRange = true;
                }
                else if (Math.Round(SSupportData.Boundingboxmin.Z) <= Math.Round(SupData.Boundingboxmax.Z) && Math.Round(SupData.Boundingboxmax.Z) <= Math.Round(SSupportData.Boundingboxmax.Z))
                {
                    IsZRange = true;
                }
                else if (Math.Round(SupData.Boundingboxmin.Z) <= Math.Round(SSupportData.Boundingboxmin.Z) && Math.Round(SSupportData.Boundingboxmax.Z) <= Math.Round(SupData.Boundingboxmax.Z))
                {
                    IsZRange = true;
                }
                /*  else if (Math.Round(SupData.Boundingboxmin.Z) <= Math.Round(SSupportData.Boundingboxmin.Z) && Math.Round(SSupportData.Boundingboxmin.Z) <= Math.Round(SupData.Boundingboxmax.Z))
                  {
                      IsZRange = true;
                  }
                  else if (Math.Round(SSupportData.Boundingboxmin.Z) <= Math.Round(SSupportData.Boundingboxmax.Z) && Math.Round(SSupportData.Boundingboxmax.Z) <= Math.Round(SupData.Boundingboxmax.Z))
                  {
                      IsZRange = true;
                  }*/

                if (IsXRange && IsYRange && IsZRange)
                {
                    ListTouchingSuppo.Add(SSupportData);
                    ListSuppoData.Add(SSupportData);

                    if (SupData.SuppoId.ToUpper().Contains("P"))
                    {
                        SupData.TouchingPartid = SSupportData.SuppoId;
                        //SSupportData.TouchingPartid = SupData.SuppoId;
                    }

                    if (!ListProcessedDataIds.Contains(SSupportData.SuppoId))
                    {
                        ListProcessedDataIds.Add(SSupportData.SuppoId);
                    }
                }
            }

            foreach (SupporSpecData SuppoData in ListTouchingSuppo)
            {
                GetllTouchingParts(SuppoData, ref ListSuppoData, ref ListProcessedDataIds, ListAllSuppoData);
            }
        }

        void GetAllTouchingSecondarySupport(SupporSpecData PSuppoData, ref List<SupporSpecData> ListPartsRange, ref List<string> ListProcessedDataIds, List<SupporSpecData> ListSecondarySuppoData)
        {

            //Need to be Refine the Logic later
            GetAllSondaryPartinRange(PSuppoData, ref ListPartsRange, ref ListProcessedDataIds, ListSecondarySuppoData);
            ListPartsRange.Add(PSuppoData);

            if (!ListProcessedDataIds.Contains(PSuppoData.SuppoId))
            {
                ListProcessedDataIds.Add(PSuppoData.SuppoId);
            }
            //int CountTouchingParts = 1;
            // while(CountTouchingParts!=0)
            // {
            // }

            //GetPartsTochingXminconst(PSuppoData, ref Data, ListSecondarySuppoData);
            //GetPartsTochingXmaxconst(PSuppoData, ref Data, ListSecondarySuppoData);
            //GetPartsTochingYminconst(PSuppoData, ref Data, ListSecondarySuppoData);
            //GetPartsTochingYmaxconst(PSuppoData, ref Data, ListSecondarySuppoData);
            //GetPartsTochingZminconst(PSuppoData, ref Data, ListSecondarySuppoData);
            //GetPartsTochingZmaxconst(PSuppoData, ref Data, ListSecondarySuppoData); 
        }

        void ModifyBoundigBox(ref SupporSpecData PSuppoData)
        {
            if (Calculate.DistPoint(PSuppoData.Boundingboxmin, PSuppoData.BottomPrim) > Calculate.DistPoint(PSuppoData.Boundingboxmax, PSuppoData.BottomPrim))
            {
                PSuppoData.Boundingboxmax = new Pt3D(PSuppoData.BottomPrim);
            }
            else
            {
                PSuppoData.Boundingboxmin = new Pt3D(PSuppoData.BottomPrim);
            }
        }
        void GetAllSondaryPartinRange(SupporSpecData PRSuppoData, ref List<SupporSpecData> ListPartsRange, ref List<string> ListProcessedDataIds, List<SupporSpecData> ListSecondarySuppoData)
        {
            SupporSpecData PSuppoData = new SupporSpecData(PRSuppoData);

            if (PSuppoData.IsSupportNB)
            {
                ModifyBoundigBox(ref PSuppoData);
            }

            foreach (SupporSpecData SSupportData in ListSecondarySuppoData)
            {
                bool IsXRange = false;
                bool IsYRange = false;
                bool IsZRange = false;

                /* if (SSupportData.Boundingboxmin.X <= PSuppoData.Boundingboxmin.X && PSuppoData.Boundingboxmax.X <= SSupportData.Boundingboxmax.X)
                 {
                     IsXRange = true;
                 }
                 else if (SSupportData.Boundingboxmin.X <= PSuppoData.Boundingboxmin.X && PSuppoData.Boundingboxmin.X <= SSupportData.Boundingboxmax.X)
                 {
                     IsXRange = true;
                 }
                 else if (SSupportData.Boundingboxmin.X <= PSuppoData.Boundingboxmax.X && PSuppoData.Boundingboxmax.X <= SSupportData.Boundingboxmax.X)
                 {
                     IsXRange = true;
                 }

                 if (SSupportData.Boundingboxmin.Y <= PSuppoData.Boundingboxmin.Y && PSuppoData.Boundingboxmax.Y <= SSupportData.Boundingboxmax.Y)
                 {
                     IsYRange = true;
                 }
                 else if (SSupportData.Boundingboxmin.Y <= PSuppoData.Boundingboxmin.Y && PSuppoData.Boundingboxmin.Y <= SSupportData.Boundingboxmax.Y)
                 {
                     IsYRange = true;
                 }
                 else if (SSupportData.Boundingboxmin.Y <= PSuppoData.Boundingboxmax.Y && PSuppoData.Boundingboxmax.Y <= SSupportData.Boundingboxmax.Y)
                 {
                     IsYRange = true;
                 }

                 if (SSupportData.Boundingboxmin.Z <= PSuppoData.Boundingboxmin.Z && PSuppoData.Boundingboxmax.Z <= SSupportData.Boundingboxmax.Z)
                 {
                     IsZRange = true;
                 }
                 else if (SSupportData.Boundingboxmin.Z <= PSuppoData.Boundingboxmin.Z && PSuppoData.Boundingboxmin.Z <= SSupportData.Boundingboxmax.Z)
                 {
                     IsZRange = true;
                 }
                 else if (SSupportData.Boundingboxmin.Z <= PSuppoData.Boundingboxmax.Z && PSuppoData.Boundingboxmax.Z <= SSupportData.Boundingboxmax.Z)
                 {
                     IsZRange = true;
                 }
                 */
                if (SSupportData.SupportName != null && SSupportData.SupportName == "PLATE")

                {
                    if (Math.Round(SSupportData.Boundingboxmin.X) <= Math.Round(PSuppoData.Boundingboxmin.X) && Math.Round(PSuppoData.Boundingboxmax.X) <= Math.Round(SSupportData.Boundingboxmax.X))
                    {
                        IsXRange = true;
                    }
                    else if (Math.Round(SSupportData.Boundingboxmin.X) <= Math.Round(PSuppoData.Boundingboxmin.X) && Math.Round(PSuppoData.Boundingboxmin.X) <= Math.Round(SSupportData.Boundingboxmax.X))
                    {
                        IsXRange = true;
                    }
                    else if (Math.Round(SSupportData.Boundingboxmin.X) <= Math.Round(PSuppoData.Boundingboxmax.X) && Math.Round(PSuppoData.Boundingboxmax.X) <= Math.Round(SSupportData.Boundingboxmax.X))
                    {
                        IsXRange = true;
                    }
                    else if (Math.Round(PSuppoData.Boundingboxmin.X) <= Math.Round(SSupportData.Boundingboxmin.X) && Math.Round(SSupportData.Boundingboxmax.X) <= Math.Round(PSuppoData.Boundingboxmax.X))
                    {
                        IsXRange = true;
                    }


                    if (Math.Round(SSupportData.Boundingboxmin.Y) <= Math.Round(PSuppoData.Boundingboxmin.Y) && Math.Round(PSuppoData.Boundingboxmax.Y) <= Math.Round(SSupportData.Boundingboxmax.Y))
                    {
                        IsYRange = true;
                    }
                    else if (Math.Round(SSupportData.Boundingboxmin.Y) <= Math.Round(PSuppoData.Boundingboxmin.Y) && Math.Round(PSuppoData.Boundingboxmin.Y) <= Math.Round(SSupportData.Boundingboxmax.Y))
                    {
                        IsYRange = true;
                    }
                    else if (Math.Round(SSupportData.Boundingboxmin.Y) <= Math.Round(PSuppoData.Boundingboxmax.Y) && Math.Round(PSuppoData.Boundingboxmax.Y) <= Math.Round(SSupportData.Boundingboxmax.Y))
                    {
                        IsYRange = true;
                    }
                    else if (Math.Round(PSuppoData.Boundingboxmin.Y) <= Math.Round(SSupportData.Boundingboxmin.Y) && Math.Round(SSupportData.Boundingboxmax.Y) <= Math.Round(PSuppoData.Boundingboxmax.Y))
                    {
                        IsYRange = true;
                    }


                    if (Math.Round(SSupportData.Boundingboxmin.Z) <= Math.Round(PSuppoData.Boundingboxmin.Z) && Math.Round(PSuppoData.Boundingboxmax.Z) <= Math.Round(SSupportData.Boundingboxmax.Z))
                    {
                        IsZRange = true;
                    }
                    else if (Math.Round(SSupportData.Boundingboxmin.Z) <= Math.Round(PSuppoData.Boundingboxmin.Z) && Math.Round(PSuppoData.Boundingboxmin.Z) <= Math.Round(SSupportData.Boundingboxmax.Z))
                    {
                        IsZRange = true;
                    }
                    else if (Math.Round(SSupportData.Boundingboxmin.Z) <= Math.Round(PSuppoData.Boundingboxmax.Z) && Math.Round(PSuppoData.Boundingboxmax.Z) <= Math.Round(SSupportData.Boundingboxmax.Z))
                    {
                        IsZRange = true;
                    }
                    else if (Math.Round(PSuppoData.Boundingboxmin.Z) <= Math.Round(SSupportData.Boundingboxmin.Z) && Math.Round(SSupportData.Boundingboxmax.Z) <= Math.Round(PSuppoData.Boundingboxmax.Z))
                    {
                        IsZRange = true;
                    }
                }
                else
                {
                    if (Math.Round(SSupportData.Boundingboxmin.X) <= Math.Round(PRSuppoData.Boundingboxmin.X) && Math.Round(PRSuppoData.Boundingboxmax.X) <= Math.Round(SSupportData.Boundingboxmax.X))
                    {
                        IsXRange = true;
                    }
                    else if (Math.Round(SSupportData.Boundingboxmin.X) <= Math.Round(PRSuppoData.Boundingboxmin.X) && Math.Round(PRSuppoData.Boundingboxmin.X) <= Math.Round(SSupportData.Boundingboxmax.X))
                    {
                        IsXRange = true;
                    }
                    else if (Math.Round(SSupportData.Boundingboxmin.X) <= Math.Round(PRSuppoData.Boundingboxmax.X) && Math.Round(PRSuppoData.Boundingboxmax.X) <= Math.Round(SSupportData.Boundingboxmax.X))
                    {
                        IsXRange = true;
                    }
                    else if (Math.Round(PRSuppoData.Boundingboxmin.X) <= Math.Round(SSupportData.Boundingboxmin.X) && Math.Round(SSupportData.Boundingboxmax.X) <= Math.Round(PRSuppoData.Boundingboxmax.X))
                    {
                        IsXRange = true;
                    }


                    if (Math.Round(SSupportData.Boundingboxmin.Y) <= Math.Round(PRSuppoData.Boundingboxmin.Y) && Math.Round(PRSuppoData.Boundingboxmax.Y) <= Math.Round(SSupportData.Boundingboxmax.Y))
                    {
                        IsYRange = true;
                    }
                    else if (Math.Round(SSupportData.Boundingboxmin.Y) <= Math.Round(PRSuppoData.Boundingboxmin.Y) && Math.Round(PRSuppoData.Boundingboxmin.Y) <= Math.Round(SSupportData.Boundingboxmax.Y))
                    {
                        IsYRange = true;
                    }
                    else if (Math.Round(SSupportData.Boundingboxmin.Y) <= Math.Round(PRSuppoData.Boundingboxmax.Y) && Math.Round(PRSuppoData.Boundingboxmax.Y) <= Math.Round(SSupportData.Boundingboxmax.Y))
                    {
                        IsYRange = true;
                    }
                    else if (Math.Round(PRSuppoData.Boundingboxmin.Y) <= Math.Round(SSupportData.Boundingboxmin.Y) && Math.Round(SSupportData.Boundingboxmax.Y) <= Math.Round(PRSuppoData.Boundingboxmax.Y))
                    {
                        IsYRange = true;
                    }


                    if (Math.Round(SSupportData.Boundingboxmin.Z) <= Math.Round(PRSuppoData.Boundingboxmin.Z) && Math.Round(PRSuppoData.Boundingboxmax.Z) <= Math.Round(SSupportData.Boundingboxmax.Z))
                    {
                        IsZRange = true;
                    }
                    else if (Math.Round(SSupportData.Boundingboxmin.Z) <= Math.Round(PRSuppoData.Boundingboxmin.Z) && Math.Round(PRSuppoData.Boundingboxmin.Z) <= Math.Round(SSupportData.Boundingboxmax.Z))
                    {
                        IsZRange = true;
                    }
                    else if (Math.Round(SSupportData.Boundingboxmin.Z) <= Math.Round(PRSuppoData.Boundingboxmax.Z) && Math.Round(PRSuppoData.Boundingboxmax.Z) <= Math.Round(SSupportData.Boundingboxmax.Z))
                    {
                        IsZRange = true;
                    }
                    else if (Math.Round(PRSuppoData.Boundingboxmin.Z) <= Math.Round(SSupportData.Boundingboxmin.Z) && Math.Round(SSupportData.Boundingboxmax.Z) <= Math.Round(PRSuppoData.Boundingboxmax.Z))
                    {
                        IsZRange = true;
                    }
                }


                if (IsXRange && IsYRange && IsZRange)
                {
                    PRSuppoData.TouchingPartid = SSupportData.SuppoId;
                    SSupportData.TouchingPartid = PRSuppoData.SuppoId;
                    ListPartsRange.Add(SSupportData);

                    if (!ListProcessedDataIds.Contains(SSupportData.SuppoId))
                    {
                        ListProcessedDataIds.Add(SSupportData.SuppoId);
                    }
                }
            }
        }

        void GetPartsTochingXminconst(SupporSpecData PSuppoData, ref SupportData Data, List<SupporSpecData> ListSecondarySuppoData)
        {
            foreach (SupporSpecData SSuppoData in ListSecondarySuppoData)
            {
                //Considring only the support in the positive Z direction
                if (SSuppoData.Boundingboxmin.X <= PSuppoData.Boundingboxmin.X)
                {

                }


            }
        }

        void GetPartsTochingXmaxconst(SupporSpecData PSuppoData, ref SupportData Data, List<SupporSpecData> ListSecondarySuppoData)
        {

        }

        void GetPartsTochingYminconst(SupporSpecData PSuppoData, ref SupportData Data, List<SupporSpecData> ListSecondarySuppoData)
        {

        }

        void GetPartsTochingYmaxconst(SupporSpecData PSuppoData, ref SupportData Data, List<SupporSpecData> ListSecondarySuppoData)
        {

        }

        void GetPartsTochingZminconst(SupporSpecData PSuppoData, ref SupportData Data, List<SupporSpecData> ListSecondarySuppoData)
        {

        }
        void GetPartsTochingZmaxconst(SupporSpecData PSuppoData, ref SupportData Data, List<SupporSpecData> ListSecondarySuppoData)
        {

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

        Pt3D GetPt3DFromArray(double[] Pt)
        {
            Pt3D LocalPoint3d = new Pt3D();
            LocalPoint3d.X = Pt[0];
            LocalPoint3d.Y = Pt[1];
            LocalPoint3d.Z = Pt[2];

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

        string[] GetNamesOfSupportEnteredbyUser(Transaction AcadTransaction, Database AcadDatabase)
        {
            string[] LayersName = new string[3];
            //LayerTable AcadLyrTbl;
            LayerTable AcadLyrTbl = AcadTransaction.GetObject(AcadDatabase.LayerTableId, OpenMode.ForRead) as LayerTable;

            if (AcadLyrTbl != null)
            {
                foreach (ObjectId AcadObjId in AcadLyrTbl)
                {
                    LayerTableRecord AcadLyrTblRec;
                    AcadLyrTblRec = AcadTransaction.GetObject(AcadObjId,
                                                    OpenMode.ForRead) as LayerTableRecord;
                    if (AcadLyrTblRec.Name.ToLower().Equals("primary support"))
                    {
                        LayersName[0] = AcadLyrTblRec.Name;
                    }
                    else if (AcadLyrTblRec.Name.ToLower().Equals("secondary support"))
                    {
                        LayersName[1] = AcadLyrTblRec.Name;
                    }
                    else if (AcadLyrTblRec.Name.ToLower().Equals("concrete support"))
                    {
                        LayersName[2] = AcadLyrTblRec.Name;
                    }
                }
            }

            return LayersName;
        }
        List<SupporSpecData> GetAllPrimarySupportData(Editor AcadEditor, Transaction AcadTransaction, string LayerName)
        {
            PromptSelectionResult SelectionRes;
            ObjectIdCollection Ents;
            TypedValue[] SelFilterName = new TypedValue[1] { new TypedValue((int)DxfCode.LayerName, LayerName) };
            SelectionFilter SelFilter = new SelectionFilter(SelFilterName);


            SelectionRes = AcadEditor.SelectAll(SelFilter);

            //Getting Object ID of the each selected entiry
            Ents = new ObjectIdCollection(SelectionRes.Value.GetObjectIds());

            List<SupporSpecData> ListPSuppoData = new List<SupporSpecData>();

            int Count = 1;
            foreach (ObjectId Id in Ents)
            {
                try
                {
                    var AcEnt = (Entity)AcadTransaction.GetObject(Id, OpenMode.ForRead);
                    SupporSpecData SuppoSpecdata = new SupporSpecData();
                    // to explode block of text collect them
                    if (AcEnt.GetType() == typeof(BlockReference))
                    {
                        if (AcEnt.GetType().Name.Contains("BlockReference"))
                        {
                            BlockReference BlockRef = AcEnt as BlockReference;

                            FillBoundingBox(AcEnt, ref SuppoSpecdata);
                            SuppoSpecdata.CalculateCentroid();
                            FillDirVec(AcEnt, ref SuppoSpecdata);
                            SuppoSpecdata.CalculateDist();
                            SuppoSpecdata.CalculateVolume();
                            SuppoSpecdata.SupportName = BlockRef.Name;
                            SuppoSpecdata.SuppoId = "P" + Count.ToString();
                            // BlockRef.ScaleFactors

                            if (BlockRef.Name.ToUpper().Contains("GRP FLG"))
                            {
                                GetRotationFlgGrp(BlockRef, ref SuppoSpecdata);
                            }
                            else if (BlockRef.Name.ToUpper().Contains("NB"))
                            {
                                GetRotationNBParts(BlockRef, ref SuppoSpecdata);
                            }
                            else
                            {
                                //SuppoSpecdata.IsSupportNB = true;
                                DBObjectCollection ExpObjs = new DBObjectCollection();
                                BlockRef.Explode(ExpObjs);
                                GetZDir(ref SuppoSpecdata, ExpObjs);
                            }
                        }
                    }
                    else if (AcEnt.GetType() == typeof(Autodesk.ProcessPower.PnP3dObjects.Support))
                    {
                        Autodesk.ProcessPower.PnP3dObjects.Support Suppo = AcEnt as Autodesk.ProcessPower.PnP3dObjects.Support;

                        dynamic Obj = Suppo.AcadObject;

                        if (Obj != null)
                        {
                            SuppoSpecdata.SupportName = Obj.PartFamilyLongDesc;
                            string Tag = Obj.LineNumberTag;
                            string Siz = Obj.Size;
                        }

                        Autodesk.ProcessPower.PnP3dObjects.PartSizeProperties PartProp = Suppo.PartSizeProperties;

                        SuppoSpecdata.NomianalDia = PartProp.NominalDiameter;

                        FillBoundingBox(AcEnt, ref SuppoSpecdata);
                        SuppoSpecdata.CalculateCentroid();
                        FillDirVec(AcEnt, ref SuppoSpecdata);
                        SuppoSpecdata.CalculateDist();
                        SuppoSpecdata.CalculateVolume();

                        SuppoSpecdata.SuppoId = "P" + Count.ToString();
                    }

                    Count++;
                    ListPSuppoData.Add(SuppoSpecdata);
                }
                catch (Exception)
                {
                }
            }

            return ListPSuppoData;
        }


        void GetRotationFlgGrp(BlockReference BlockRef, ref SupporSpecData SuppoSpecdata)
        {
            DBObjectCollection ExpObjs = new DBObjectCollection();
            BlockRef.Explode(ExpObjs);

            List<BodyData> ListSeleBodyNB = new List<BodyData>();
            BodyData BodyBottomPart = new BodyData();

            CircleData CirDataS = new CircleData();

            //Fun1 730 to 828
            foreach (Entity AcEnt1 in ExpObjs)
            {
                if (AcEnt1.GetType() == typeof(Circle))
                {
                    Circle CirData = AcEnt1 as Circle;

                    CirDataS.Vector.X = CirData.Normal.X;
                    CirDataS.Vector.Y = CirData.Normal.Y;
                    CirDataS.Vector.Z = CirData.Normal.Z;

                    CirDataS.Center.X = CirData.Center.X;
                    CirDataS.AcadPlane = CirData.GetPlane();
                }
            }

            foreach (Entity AcEnt1 in ExpObjs)
            {
                if (AcEnt1.GetType() == typeof(Solid3d))
                {
                    Solid3d solid3D = AcEnt1 as Solid3d;
                    using (var Breps = new Autodesk.AutoCAD.BoundaryRepresentation.Brep(solid3D))
                    {
                        Autodesk.AutoCAD.BoundaryRepresentation.BrepFaceCollection FaceColl = Breps.Faces;

                        int Count = FaceColl.Count<Autodesk.AutoCAD.BoundaryRepresentation.Face>();

                        // Here we are getting the cuboidal bodies from which we can get diection of the Primary support so we are checking for Faces
                        if (Count == 8)
                        {
                            foreach (var FacE in FaceColl)
                            {
                                FaceData FaceLoc = new FaceData();

                                FaceLoc.AcadFace = FacE;
                                BoundBlock3d Block3d = FacE.BoundBlock;

                                FaceLoc.SurfaceArea = FacE.GetSurfaceArea();

                                ExternalBoundedSurface ExtBSurf = FacE.Surface as ExternalBoundedSurface;

                                if (ExtBSurf != null)
                                {
                                    if (ExtBSurf.IsPlane)
                                    {
                                        Plane PlaneSurf = ExtBSurf.BaseSurface as Plane;

                                        if (PlaneSurf.IsCoplanarTo(CirDataS.AcadPlane))
                                        {

                                            FillDirVecData(PlaneSurf.GetCoordinateSystem(), ref SuppoSpecdata);

                                            DirectionVec DirVec = new DirectionVec();
                                            DirVec.XDirVec = GetPt3DFromVecData(FacE.BoundBlock.Direction1);
                                            DirVec.YDirVec = GetPt3DFromVecData(FacE.BoundBlock.Direction2);
                                            DirVec.ZDirVec = GetPt3DFromVecData(FacE.BoundBlock.Direction3);

                                            System.Windows.Media.Media3D.Vector3D Vec1 = new System.Windows.Media.Media3D.Vector3D(CirDataS.AcadPlane.Normal.X, CirDataS.AcadPlane.Normal.Y, CirDataS.AcadPlane.Normal.Z);
                                            System.Windows.Media.Media3D.Vector3D Vec2 = new System.Windows.Media.Media3D.Vector3D(1, 0, 0);


                                            //  System.Windows.Media.Media3D.Vector3D.
                                            double angle1 = System.Windows.Media.Media3D.Vector3D.AngleBetween(Vec1, Vec2);
                                            double angle2 = System.Windows.Media.Media3D.Vector3D.AngleBetween(Vec1, new System.Windows.Media.Media3D.Vector3D(0, 1, 0));
                                            double angle3 = System.Windows.Media.Media3D.Vector3D.AngleBetween(Vec1, new System.Windows.Media.Media3D.Vector3D(0, 0, 1));

                                            double anglePto = System.Windows.Media.Media3D.Vector3D.DotProduct(Vec1, new System.Windows.Media.Media3D.Vector3D(0, 0, 1));
                                            //System.Windows.Media.Media3D.Vector3D Crossp = System.Windows.Media.Media3D.Vector3D.CrossProduct(Vec1, Vec2);

                                            SuppoSpecdata.Directionvec = DirVec;
                                            //  PlaneSurf.TransformBy

                                            SuppoSpecdata.DistCenter = GetDistanceCenterCirclefromBase(FaceLoc.AcadFace, CirDataS.Center);

                                            Vec1.Normalize();

                                            double X = Calculate.GetSignedRotation(Vec1, new System.Windows.Media.Media3D.Vector3D(1, 0, 0), new System.Windows.Media.Media3D.Vector3D(0, 0, 1));
                                            double Y = Calculate.GetSignedRotation(Vec1, new System.Windows.Media.Media3D.Vector3D(0, 1, 0), new System.Windows.Media.Media3D.Vector3D(0, 0, 1));
                                            double Z = Calculate.GetSignedRotation(Vec1, new System.Windows.Media.Media3D.Vector3D(0, 0, 1), new System.Windows.Media.Media3D.Vector3D(1, 0, 0));

                                            SuppoSpecdata.FaceLocalAngle = new Angles();
                                            SuppoSpecdata.FaceLocalAngle.XinRadian = X;
                                            SuppoSpecdata.FaceLocalAngle.YinRadian = Y;
                                            SuppoSpecdata.FaceLocalAngle.ZinRadian = Z;
                                            SuppoSpecdata.FaceLocalAngle.XinDegree = Calculate.ConvertRadiansToDegrees(X);
                                            SuppoSpecdata.FaceLocalAngle.YinDegree = Calculate.ConvertRadiansToDegrees(Y);
                                            SuppoSpecdata.FaceLocalAngle.ZinDegree = Calculate.ConvertRadiansToDegrees(Z);

                                            SuppoSpecdata.NoramlDir = new Pt3D();
                                            SuppoSpecdata.NoramlDir.X = CirDataS.AcadPlane.Normal.X;
                                            SuppoSpecdata.NoramlDir.Y = CirDataS.AcadPlane.Normal.Y;
                                            SuppoSpecdata.NoramlDir.Z = CirDataS.AcadPlane.Normal.Z;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        double GetDistanceCenterCirclefromBase(Autodesk.AutoCAD.BoundaryRepresentation.Face AcadFace, Pt3D CenterCir)
        {
            List<double> DistList = new List<double>();

            // double Lenght = 0;
            double Dist = 0;
            foreach (var Loop in AcadFace.Loops)
            {
                if (Loop.LoopType == LoopType.LoopExterior)
                {
                    foreach (Edge AcadEdge in Loop.Edges)
                    {
                        ExternalCurve3d Curve = AcadEdge.Curve as ExternalCurve3d;

                        if (Curve.IsLineSegment)
                        {
                            LineSegment3d LineSeg = Curve.NativeCurve as LineSegment3d;


                            //if (Math.Round(LineSeg.Length) > Math.Round(Lenght))
                            // Lenght = LineSeg.Length;
                            ///LineSeg.
                            Dist = Calculate.DistPoint(GetCenterofLineSeg(LineSeg), CenterCir);
                            DistList.Add(Calculate.DistPoint(GetCenterofLineSeg(LineSeg), CenterCir));
                        }
                    }
                }
            }


            DistList.Sort();

            return DistList[DistList.Count - 1];
        }

        Pt3D GetCenterofLineSeg(LineSegment3d LineSegd)
        {
            Pt3D CenterPt = new Pt3D();

            CenterPt.X = (LineSegd.StartPoint.X + LineSegd.EndPoint.X) / 2;
            CenterPt.Y = (LineSegd.StartPoint.Y + LineSegd.EndPoint.Y) / 2;
            CenterPt.Z = (LineSegd.StartPoint.Z + LineSegd.EndPoint.Z) / 2;

            return CenterPt;
        }

        void GetZDir(ref SupporSpecData SuppoSpecdata, DBObjectCollection ExpObjs)
        {
            double z = 0.0;
            double Vol = 0.0;
            foreach (Entity AcEnt1 in ExpObjs)
            {
                Pt3D BoxData = new Pt3D();
                var Boundingboxmax = AcEnt1.Bounds.Value.MaxPoint;
                var Boundingboxmin = AcEnt1.Bounds.Value.MinPoint;
                BoxData.X = Math.Abs(Boundingboxmax.X - Boundingboxmin.X);
                BoxData.Y = Math.Abs(Boundingboxmax.Y - Boundingboxmin.Y);
                BoxData.Z = Math.Abs(Boundingboxmax.Z - Boundingboxmin.Z);

                if (Vol < BoxData.X * BoxData.Y * BoxData.Z)
                {
                    Vol = BoxData.X * BoxData.Y * BoxData.Z;
                    z = Boundingboxmin.Z + (Math.Abs(Boundingboxmax.Z - Boundingboxmin.Z) / 2);
                }
            }

            SuppoSpecdata.PrimaryZhtNB = z;
        }

        // This will Calculate NBParts All Required Data 
        // Please Make Smaller Functions in the NB Parts


        // This will Calculate NBParts All Required Data 
        // Please Make Smaller Functions in the NB Parts
        void GetRotationNBParts(BlockReference BlockRef, ref SupporSpecData SuppoSpecdata)
        {
            SuppoSpecdata.IsSupportNB = true;
            DBObjectCollection ExpObjs = new DBObjectCollection();
            BlockRef.Explode(ExpObjs);

            List<BodyData> ListSeleBodyNB = new List<BodyData>();
            BodyData BodyBottomPart = new BodyData();

            GetZDir(ref SuppoSpecdata, ExpObjs);

            //Fun1 730 to 828
            foreach (Entity AcEnt1 in ExpObjs)
            {
                if (AcEnt1.GetType() == typeof(Solid3d))
                {
                    Solid3d solid3D = AcEnt1 as Solid3d;
                    using (var Breps = new Autodesk.AutoCAD.BoundaryRepresentation.Brep(solid3D))
                    {
                        Autodesk.AutoCAD.BoundaryRepresentation.BrepFaceCollection FaceColl = Breps.Faces;

                        int Count = FaceColl.Count<Autodesk.AutoCAD.BoundaryRepresentation.Face>();

                        // Here we are getting the cuboidal bodies from which we can get diection of the Primary support so we are checking for Faces
                        if (Count == 6 || Count == 8)
                        {
                            BodyData BodyLoc = new BodyData();
                            BodyLoc.Volume = solid3D.MassProperties.Volume;

                            List<FaceData> ListFacesLoc = new List<FaceData>();

                            int Cntr = 1;
                            foreach (var FacE in FaceColl)
                            {
                                FaceData FaceLoc = new FaceData();

                                FaceLoc.AcadFace = FacE;
                                BoundBlock3d Block3d = FacE.BoundBlock;

                                FaceLoc.SurfaceArea = FacE.GetSurfaceArea();

                                ExternalBoundedSurface ExtBSurf = FacE.Surface as ExternalBoundedSurface;

                                if (ExtBSurf != null)
                                {
                                    if (ExtBSurf.IsPlane)
                                    {
                                        FaceLoc.IsPlannar = true;
                                        Plane PlaneSurf = ExtBSurf.BaseSurface as Plane;

                                        FaceLoc.FaceNormal = PlaneSurf.Normal;

                                        FaceLoc.PtonPlane = PlaneSurf.PointOnPlane;

                                        FaceLoc.IdLocal = "F" + Cntr.ToString();
                                        ListFacesLoc.Add(FaceLoc);
                                        Cntr++;
                                    }
                                }


                                /* Point3d Pt = Block3d.BasePoint;

                                 PointOnSurface ptOnSurf = FacE.Surface.GetClosestPointTo(Pt);

                                 Point2d param = ptOnSurf.Parameter;

                                 Vector3d normal = ptOnSurf.GetNormal(param);

                                 // It seems we cannot trust the

                                 // face.Surface.IsNormalReversed property,

                                 // so I am applying a small offset to the point on surface

                                 // and check if the offset point is inside the solid

                                 // in case it is inside (or on boundary),

                                 // the normal must be reversed

                                 Vector3d normalTest = normal.MultiplyBy(1E-6 / normal.Length);

                                 Point3d ptTest = Pt.Add(normalTest);

                                 Autodesk.AutoCAD.BoundaryRepresentation.PointContainment ptContainment = new PointContainment();

                                 bool reverse = false;

                                 using (BrepEntity brepEnt = FacE.Brep.GetPointContainment(ptTest, out ptContainment))
                                 {

                                     if (ptContainment != PointContainment.Outside)
                                     {
                                         reverse = true;
                                     }
                                 }

                                 normal.MultiplyBy(reverse ? -1.0 : 1.0);

                                 */
                            }


                            if (Count == 6)
                            {
                                BodyLoc.ListFaceData = ListFacesLoc;
                                ListSeleBodyNB.Add(BodyLoc);
                            }
                            else if (Count == 8)
                            {
                                BodyLoc.ListFaceData = ListFacesLoc;
                                BodyBottomPart = BodyLoc;
                            }
                        }
                        else if (Count > 8)
                        {
                            BodyData BodyLoc = new BodyData();
                            BodyLoc.Volume = solid3D.MassProperties.Volume;

                            List<FaceData> ListFacesLoc = new List<FaceData>();

                            int Cntr = 1;
                            foreach (var FacE in FaceColl)
                            {
                                FaceData FaceLoc = new FaceData();

                                FaceLoc.AcadFace = FacE;
                                BoundBlock3d Block3d = FacE.BoundBlock;

                                FaceLoc.SurfaceArea = FacE.GetSurfaceArea();

                                ExternalBoundedSurface ExtBSurf = FacE.Surface as ExternalBoundedSurface;

                                if (ExtBSurf != null)
                                {
                                    if (ExtBSurf.IsPlane)
                                    {
                                        FaceLoc.IsPlannar = true;
                                        Plane PlaneSurf = ExtBSurf.BaseSurface as Plane;

                                        FaceLoc.FaceNormal = PlaneSurf.Normal;

                                        FaceLoc.PtonPlane = PlaneSurf.PointOnPlane;

                                        FaceLoc.IdLocal = "F" + Cntr.ToString();
                                        ListFacesLoc.Add(FaceLoc);

                                    }
                                }
                            }
                        }
                    }
                }
            }

            //Fun1 730 to 828

            // Getting the smallest and the biggest volumes
            // Please Don't change order of fill data to list ListbodiesToCompare Sequence is used to later to caompare data
            //First add the body i the list whoes volume is smaller 

            List<BodyData> ListbodiesToCompare = new List<BodyData>();
            if (ListSeleBodyNB != null && ListSeleBodyNB.Count > 0)
            {
                if (ListSeleBodyNB.Exists(e => Math.Round(e.Volume) > Math.Round(ListSeleBodyNB[0].Volume)))
                {
                    ListbodiesToCompare.Add(ListSeleBodyNB[0]);
                    ListbodiesToCompare.Add(ListSeleBodyNB.Find(e => e.Volume > ListSeleBodyNB[0].Volume));
                }
                else if (ListSeleBodyNB.Exists(e => Math.Round(e.Volume) < Math.Round(ListSeleBodyNB[0].Volume)))
                {
                    ListbodiesToCompare.Add(ListSeleBodyNB.Find(e => e.Volume < ListSeleBodyNB[0].Volume));
                    ListbodiesToCompare.Add(ListSeleBodyNB[0]);
                }
            }

            // Get here on Coplanar face of smallest body Face  
            FaceData FaceDataToProcess = new FaceData();
            if (ListbodiesToCompare != null && ListbodiesToCompare.Count == 2)
            {
                foreach (FaceData FaceB in ListbodiesToCompare[0].ListFaceData)
                {
                    ExternalBoundedSurface ExtBnSurf = FaceB.AcadFace.Surface as ExternalBoundedSurface;
                    Plane PlaneSurfB = ExtBnSurf.BaseSurface as Plane;

                    foreach (FaceData FaceB1 in ListbodiesToCompare[1].ListFaceData)
                    {
                        ExternalBoundedSurface ExtBnSurf1 = FaceB1.AcadFace.Surface as ExternalBoundedSurface;
                        Plane PlaneSurfB1 = ExtBnSurf.BaseSurface as Plane;

                        if (PlaneSurfB.IsCoplanarTo(PlaneSurfB1))
                        {
                            FaceDataToProcess = FaceB;
                            break;
                        }
                    }
                }
            }

            GetDirfromface(FaceDataToProcess, ref SuppoSpecdata);

            ExternalBoundedSurface ExtBSurf1 = FaceDataToProcess.AcadFace.Surface as ExternalBoundedSurface;

            if (ExtBSurf1 != null)
            {
                if (ExtBSurf1.IsPlane)
                {
                    Plane PlaneSurf = ExtBSurf1.BaseSurface as Plane;

                    SuppoSpecdata.NoramlDir = new Pt3D();
                    SuppoSpecdata.NoramlDir.X = PlaneSurf.Normal.X;
                    SuppoSpecdata.NoramlDir.Y = PlaneSurf.Normal.Y;
                    SuppoSpecdata.NoramlDir.Z = PlaneSurf.Normal.Z;
                }
            }


            //recent change
            SuppoSpecdata.Midpoint = GetSmallerMidPt(FaceDataToProcess);

            SuppoSpecdata.DistCenter = GetLargerEdgeFromPlane(FaceDataToProcess) + GetBottomPartHeight(FaceDataToProcess, BodyBottomPart);

            double SurfArea = 0;
            foreach (var Face in BodyBottomPart.ListFaceData)
            {
                if (SurfArea < Face.SurfaceArea)
                {
                    SurfArea = Face.SurfaceArea;
                }
            }

            List<FaceData> faceDt = new List<FaceData>();

            foreach (var Face in BodyBottomPart.ListFaceData)
            {
                if (Math.Round(SurfArea).Equals(Math.Round(Face.SurfaceArea)))
                {
                    faceDt.Add(Face);
                }
            }

            double Dist = 0;

            foreach (var Face in faceDt)
            {
                if (Dist < Calculate.DistPoint(GetPt3DFromPoint3D(FaceDataToProcess.PtonPlane), GetPt3DFromPoint3D(Face.PtonPlane)))
                {
                    Dist = Calculate.DistPoint(GetPt3DFromPoint3D(FaceDataToProcess.PtonPlane), GetPt3DFromPoint3D(Face.PtonPlane));

                    SuppoSpecdata.BottomPrim = GetPt3DFromPoint3D(Face.PtonPlane);
                }
            }
        }

        void GetDirfromface(FaceData Face, ref SupporSpecData SuppoSpecdata)
        {
            ExternalBoundedSurface ExtBSurf = Face.AcadFace.Surface as ExternalBoundedSurface;

            if (ExtBSurf != null)
            {
                if (ExtBSurf.IsPlane)
                {
                    Plane PlaneSurf = ExtBSurf.BaseSurface as Plane;

                    FillDirVecData(PlaneSurf.GetCoordinateSystem(), ref SuppoSpecdata);
                }
            }
        }

        void FillDirVecData(CoordinateSystem3d CordSys, ref SupporSpecData SuppoSpecdata)
        {
            DirectionVec DirVec = new DirectionVec();
            DirVec.XDirVec = GetPt3DFromVecData(CordSys.Xaxis);
            DirVec.YDirVec = GetPt3DFromVecData(CordSys.Yaxis);
            DirVec.ZDirVec = GetPt3DFromVecData(CordSys.Zaxis);

            SuppoSpecdata.Directionvec = DirVec;

        }

        double GetBottomPartHeight(FaceData FaceDataToProcess, BodyData BodyBottomPart)
        {
            if (BodyBottomPart != null)
            {
                ExternalBoundedSurface ExtBnSurf = FaceDataToProcess.AcadFace.Surface as ExternalBoundedSurface;
                Plane PlaneSurfB = ExtBnSurf.BaseSurface as Plane;
                foreach (FaceData FaceB1 in BodyBottomPart.ListFaceData)
                {
                    ExternalBoundedSurface ExtBnSurf1 = FaceB1.AcadFace.Surface as ExternalBoundedSurface;
                    Plane PlaneSurfB1 = ExtBnSurf1.BaseSurface as Plane;

                    if (PlaneSurfB1.IsParallelTo(PlaneSurfB))
                    {
                        return GetSmallerEdgeFromPlane(FaceB1);
                    }
                }
            }

            return 0;
        }


        Pt3D GetSmallerMidPt(FaceData FaceB1)
        {
            double LenghtCurveold = 0;
            bool IsFirst = true;
            double LengthofVer = 0;
            Pt3D Midpt = new Pt3D();

            foreach (var Loop in FaceB1.AcadFace.Loops)
            {
                if (Loop.LoopType == LoopType.LoopExterior)
                {
                    foreach (Edge AcadEdge in Loop.Edges)
                    {
                        ExternalCurve3d Curve = AcadEdge.Curve as ExternalCurve3d;

                        if (Curve.IsLineSegment)
                        {
                            LineSegment3d LineSeg = Curve.NativeCurve as LineSegment3d;
                            if (IsFirst)
                            {
                                LenghtCurveold = LineSeg.Length;

                                Midpt = GetPt3DFromPoint3D(LineSeg.MidPoint);
                                IsFirst = false;
                            }
                            else if (LenghtCurveold > LineSeg.Length)
                            {
                                LengthofVer = LineSeg.Length;

                                Midpt = GetPt3DFromPoint3D(LineSeg.MidPoint);
                                break;
                            }
                            else if (LenghtCurveold < LineSeg.Length)
                            {
                                LengthofVer = LenghtCurveold;
                                break;
                            }
                        }
                    }
                }
            }

            return Midpt;
        }

        double GetSmallerEdgeFromPlane(FaceData FaceB1)
        {
            double LenghtCurveold = 0;
            bool IsFirst = true;
            double LengthofVer = 0;

            foreach (var Loop in FaceB1.AcadFace.Loops)
            {
                if (Loop.LoopType == LoopType.LoopExterior)
                {
                    foreach (Edge AcadEdge in Loop.Edges)
                    {
                        ExternalCurve3d Curve = AcadEdge.Curve as ExternalCurve3d;

                        if (Curve.IsLineSegment)
                        {
                            LineSegment3d LineSeg = Curve.NativeCurve as LineSegment3d;
                            if (IsFirst)
                            {
                                LenghtCurveold = LineSeg.Length;
                                IsFirst = false;
                            }
                            else if (LenghtCurveold > LineSeg.Length)
                            {
                                LengthofVer = LineSeg.Length;

                                break;
                            }
                            else if (LenghtCurveold < LineSeg.Length)
                            {
                                LengthofVer = LenghtCurveold;
                                break;
                            }
                        }
                    }
                }
            }

            return LengthofVer;
        }

        double GetLargerEdgeFromPlane(FaceData FaceB1)
        {
            double LenghtCurveold = 0;
            bool IsFirst = true;
            double LengthofVer = 0;

            foreach (var Loop in FaceB1.AcadFace.Loops)
            {
                if (Loop.LoopType == LoopType.LoopExterior)
                {
                    foreach (Edge AcadEdge in Loop.Edges)
                    {
                        ExternalCurve3d Curve = AcadEdge.Curve as ExternalCurve3d;

                        if (Curve.IsLineSegment)
                        {
                            LineSegment3d LineSeg = Curve.NativeCurve as LineSegment3d;

                            if (IsFirst)
                            {
                                LenghtCurveold = LineSeg.Length;
                                IsFirst = false;
                            }
                            else if (LenghtCurveold > LineSeg.Length)
                            {
                                LengthofVer = LenghtCurveold;

                                break;
                            }
                            else if (LenghtCurveold < LineSeg.Length)
                            {
                                LengthofVer = LineSeg.Length;

                                break;
                            }
                        }
                    }
                }
            }
            return LengthofVer;
        }

        List<SupporSpecData> GetAllSecondarySupportData(Editor AcadEditor, Transaction AcadTransaction, string LayerName)
        {
            PromptSelectionResult SelectionRes;
            ObjectIdCollection Ents;
            TypedValue[] SelFilterName = new TypedValue[1] { new TypedValue((int)DxfCode.LayerName, LayerName) };
            SelectionFilter SelFilter = new SelectionFilter(SelFilterName);

            SelectionRes = AcadEditor.SelectAll(SelFilter);

            //Getting Object ID of the each selected entiry
            Ents = new ObjectIdCollection(SelectionRes.Value.GetObjectIds());

            List<SupporSpecData> ListSecondarySuppoData = new List<SupporSpecData>();
            int Count = 1;
            foreach (ObjectId Id in Ents)
            {
                try
                {
                    var AcEnt = (Entity)AcadTransaction.GetObject(Id, OpenMode.ForRead);
                    dynamic Structureobj = AcEnt.AcadObject;

                    // to explode block of text collect them
                    if (AcEnt.GetType().Name.Contains("ImpCurve"))
                    {
                        SupporSpecData SuppoSpecdata = new SupporSpecData();
                        // Solid3d SLD = AcEnt as Solid3d;

                        FillBoundingBox(AcEnt, ref SuppoSpecdata);
                        SuppoSpecdata.CalculateCentroid();
                        FillDirVec(AcEnt, ref SuppoSpecdata);
                        SuppoSpecdata.CalculateDist();
                        SuppoSpecdata.CalculateVolume();

                        try
                        {
                            if (Structureobj.PnPClassName == "StructurePlate")
                            {
                                SuppoSpecdata.Position = new Pt3D(GetPt3DFromArray(Structureobj.PositionPoint));
                                SuppoSpecdata.SupportName = "PLATE";
                            }
                            else
                            {
                                double[] StPt = new double[3];
                                double[] EndPt = new double[3];
                                SuppoSpecdata.Size = Structureobj.Size;
                                SuppoSpecdata.StPt = Structureobj.StartPoint;
                                SuppoSpecdata.EndPt = Structureobj.EndPoint;
                                SuppoSpecdata.SupportName = Structureobj.PnPClassName;
                            }
                            //  Structureobj.
                        }
                        catch (System.Exception)
                        {
                        }

                        SuppoSpecdata.SuppoId = "S" + Count.ToString();

                        ListSecondarySuppoData.Add(SuppoSpecdata);
                    }
                    else if (AcEnt.GetType() == typeof(Solid3d))
                    {
                        SupporSpecData SuppoSpecdata = new SupporSpecData();
                        Solid3d SLD = AcEnt as Solid3d;

                        SuppoSpecdata.ListfaceData = GetFacesData(SLD);

                        GetSmallestEdge(ref SuppoSpecdata);

                        Pt3D Cntrd = new Pt3D();
                        Cntrd.X = Math.Abs(SLD.MassProperties.Centroid[0]);
                        Cntrd.Y = Math.Abs(SLD.MassProperties.Centroid[1]);
                        Cntrd.Z = Math.Abs(SLD.MassProperties.Centroid[2]);
                        SuppoSpecdata.Centroid = Cntrd;
                        FillBoundingBox(AcEnt, ref SuppoSpecdata);
                        FillDirVec(AcEnt, ref SuppoSpecdata);
                        SuppoSpecdata.Volume = SLD.MassProperties.Volume;
                        SuppoSpecdata.CalculateDist();
                        SuppoSpecdata.SupportName = "PLATE";
                        SuppoSpecdata.SuppoId = "S" + Count.ToString();

                        ListSecondarySuppoData.Add(SuppoSpecdata);
                    }

                    Count++;
                }
                catch (System.Exception)
                {
                }
            }

            return ListSecondarySuppoData;
        }

        void GetSmallestEdge(ref SupporSpecData SuppoSpecdata)
        {
            double EdgeLen = 0.0;
            int inx = 0;
            foreach (FaceData FaCe in SuppoSpecdata.ListfaceData)
            {
                foreach (Edgeinfo Edge in FaCe.ListlinearEdge)
                {
                    if (inx == 0)
                    {
                        EdgeLen = Edge.EdgeLength;
                    }
                    else
                    {
                        if (EdgeLen > Edge.EdgeLength)
                        {
                            EdgeLen = Edge.EdgeLength;
                        }
                    }

                    inx++;
                }
            }

        }

        List<Edgeinfo> GetAllEdgeInfo(Autodesk.AutoCAD.BoundaryRepresentation.Face AcadFace)
        {
            List<Edgeinfo> ListlinearEdge = new List<Edgeinfo>();
            foreach (var Loop in AcadFace.Loops)
            {

                if (Loop.LoopType == LoopType.LoopExterior)
                {
                    foreach (Edge AcadEdge in Loop.Edges)
                    {
                        Edgeinfo EdgeData = new Edgeinfo();
                        ExternalCurve3d Curve = AcadEdge.Curve as ExternalCurve3d;

                        if (Curve.IsLineSegment)
                        {
                            LineSegment3d LineSeg = Curve.NativeCurve as LineSegment3d;

                            EdgeData.DirectionEdge = GetPt3DFromVecData(LineSeg.Direction);
                            EdgeData.StPt = GetPt3DFromPoint3D(LineSeg.StartPoint);
                            EdgeData.EndPt = GetPt3DFromPoint3D(LineSeg.EndPoint);
                            EdgeData.MidPoint = GetPt3DFromPoint3D(LineSeg.MidPoint);
                            EdgeData.EdgeLength = LineSeg.Length;

                            ListlinearEdge.Add(EdgeData);
                        }
                    }
                }
            }

            return ListlinearEdge;
        }

        List<FaceData> GetFacesData(Solid3d solid3D)
        {
            List<FaceData> AllFaceData = new List<FaceData>();
            using (var Breps = new Autodesk.AutoCAD.BoundaryRepresentation.Brep(solid3D))
            {
                Autodesk.AutoCAD.BoundaryRepresentation.BrepFaceCollection FaceColl = Breps.Faces;

                int Count = FaceColl.Count<Autodesk.AutoCAD.BoundaryRepresentation.Face>();

                // Here we are getting the cuboidal bodies from which we can get diection of the Primary support so we are checking for Faces

                foreach (var FacE in FaceColl)
                {
                    FaceData FaceLoc = new FaceData();

                    FaceLoc.AcadFace = FacE;
                    BoundBlock3d Block3d = FacE.BoundBlock;

                    FaceLoc.SurfaceArea = FacE.GetSurfaceArea();

                    ExternalBoundedSurface ExtBSurf = FacE.Surface as ExternalBoundedSurface;

                    if (ExtBSurf != null)
                    {
                        if (ExtBSurf.IsPlane)
                        {
                            Plane PlaneSurf = ExtBSurf.BaseSurface as Plane;
                            // CoordinateSystem3d CoSym = PlaneSurf.GetCoordinateSystem();
                            GetDirectionFace(ref FaceLoc, PlaneSurf.GetCoordinateSystem());
                            FaceLoc.FaceNormal = PlaneSurf.Normal;
                            FaceLoc.ListlinearEdge = GetAllEdgeInfo(FaceLoc.AcadFace);

                        }
                    }

                    AllFaceData.Add(FaceLoc);
                }
            }

            return AllFaceData;
        }

        void GetDirectionFace(ref FaceData FaceLoc, CoordinateSystem3d CoSym)
        {
            DirectionVec DirVec = new DirectionVec();
            DirVec.XDirVec = GetPt3DFromVecData(CoSym.Xaxis);
            DirVec.YDirVec = GetPt3DFromVecData(CoSym.Yaxis);
            DirVec.ZDirVec = GetPt3DFromVecData(CoSym.Zaxis);

            FaceLoc.Directionvecface = DirVec;
            FaceLoc.AngleData = GetRotationFromVec(DirVec);
        }

        List<SupporSpecData> GetAllConcreteSupportData(Editor AcadEditor, Transaction AcadTransaction, string LayerName)
        {
            PromptSelectionResult SelectionRes;
            ObjectIdCollection Ents;
            TypedValue[] SelFilterName = new TypedValue[1] { new TypedValue((int)DxfCode.LayerName, LayerName) };
            SelectionFilter SelFilter = new SelectionFilter(SelFilterName);

            SelectionRes = AcadEditor.SelectAll(SelFilter);

            //Getting Object ID of the each selected entiry
            Ents = new ObjectIdCollection(SelectionRes.Value.GetObjectIds());

            List<SupporSpecData> ListConcreteSupportData = new List<SupporSpecData>();
            int Count = 1;
            foreach (ObjectId Id in Ents)
            {
                try
                {
                    var AcEnt = (Entity)AcadTransaction.GetObject(Id, OpenMode.ForRead);

                    // to explode block of text collect them
                    if (AcEnt.GetType() == typeof(Solid3d))
                    {
                        SupporSpecData SuppoSpecdata = new SupporSpecData();
                        Solid3d SLD = AcEnt as Solid3d;
                        Pt3D Cntrd = new Pt3D();
                        Cntrd.X = Math.Abs(SLD.MassProperties.Centroid[0]);
                        Cntrd.Y = Math.Abs(SLD.MassProperties.Centroid[1]);
                        Cntrd.Z = Math.Abs(SLD.MassProperties.Centroid[2]);
                        SuppoSpecdata.Centroid = Cntrd;
                        FillBoundingBox(AcEnt, ref SuppoSpecdata);
                        FillDirVec(AcEnt, ref SuppoSpecdata);
                        SuppoSpecdata.Volume = SLD.MassProperties.Volume;
                        SuppoSpecdata.CalculateDist();
                        SuppoSpecdata.SuppoId = "C" + Count.ToString();

                        ListConcreteSupportData.Add(SuppoSpecdata);
                    }
                    else if (AcEnt.GetType().Name.Contains("ImpCurve"))
                    {
                        SupporSpecData SuppoSpecdata = new SupporSpecData();
                        // Solid3d SLD = AcEnt as Solid3d;

                        dynamic Structureobj = AcEnt.AcadObject;
                        FillBoundingBox(AcEnt, ref SuppoSpecdata);
                        SuppoSpecdata.CalculateCentroid();
                        FillDirVec(AcEnt, ref SuppoSpecdata);
                        SuppoSpecdata.CalculateDist();
                        SuppoSpecdata.CalculateVolume();

                        try
                        {
                            if (Structureobj.PnPClassName == "StructurePlate")
                            {
                                SuppoSpecdata.Position = new Pt3D(GetPt3DFromArray(Structureobj.PositionPoint));
                                SuppoSpecdata.SupportName = "PLATE";
                            }
                            else
                            {
                                double[] StPt = new double[3];
                                double[] EndPt = new double[3];
                                SuppoSpecdata.Size = Structureobj.Size;
                                SuppoSpecdata.StPt = Structureobj.StartPoint;
                                SuppoSpecdata.EndPt = Structureobj.EndPoint;
                            }
                            //  Structureobj.
                        }
                        catch (System.Exception)
                        {
                        }

                        SuppoSpecdata.SuppoId = "C" + Count.ToString();

                        ListConcreteSupportData.Add(SuppoSpecdata);
                    }

                    Count++;
                }
                catch (Exception)
                {
                }
            }

            return ListConcreteSupportData;
        }

        void CompareSupport()
        {
            //First we need to compare supports types are matching

            IsSupportSizematching();
        }

        bool IsSupportSizematching()
        {
            //Temporarly using Support to compare 

            return false;
        }
        public void ProcessSupportData()
        {
            ProcessSuppoerD();
        }

        public void ProcessSuppoerD()
        {
            foreach (SupportData Data in ListCentalSuppoData)
            {
                CheckRotationtTogetType(Data);

            }
        }

        void CheckRotationtTogetType(SupportData SData)
        {
            SupportData SupportData = GetRotationofParts(SData);

            ProcessDicDataToGetType(SupportData);
        }

        void ProcessDicDataToGetType(SupportData SupportData)
        {
            int CSupportCount = SupportData.ListConcreteData.Count;
            int PSupportCount = SupportData.ListPrimarySuppo.Count;
            int SSupportCount = SupportData.ListSecondrySuppo.Count;


            if (CSupportCount == 0 && PSupportCount == 1 && SSupportCount == 0)
            {
                SupportData.SupportType = "Support13";
            }
            else if (CSupportCount == 0 && PSupportCount == 1 && SSupportCount == 1)
            {
                CheckforTypeSupport14(ref SupportData);
            }
            else if (CSupportCount == 1 && PSupportCount == 1 && SSupportCount == 1)
            {
                CheckforTypeSupport14(ref SupportData);
            }
            else if (CSupportCount == 0 && PSupportCount == 1 && SSupportCount == 2)
            {
                if (!CheckforTypeSupport3(ref SupportData))
                {
                    if (!CheckforTypeSupport2(ref SupportData))
                    {
                        if (!CheckforTypeSupport4(ref SupportData))
                        {
                            if (SupportData.ListSecondrySuppo.Any(x => x.SupportName == "PLATE"))
                            {
                                if (CheckforTypeSupport8(ref SupportData))
                                {

                                }
                            }
                        }
                    }
                }
            }
            else if (CSupportCount == 1 && PSupportCount == 1 && SSupportCount == 2)
            {
                if (!CheckforTypeSupport2(ref SupportData))
                {
                    if (!CheckforTypeSupport4(ref SupportData))
                    {
                        if (SupportData.ListSecondrySuppo.Any(x => x.SupportName == "PLATE"))
                        {
                            if (CheckforTypeSupport8(ref SupportData))
                            {

                            }
                        }
                    }
                }
            }
            else if (CSupportCount == 2 && PSupportCount == 1 && SSupportCount == 2)
            {
                if (!CheckforTypeSupport2(ref SupportData))
                {
                    if (!CheckforTypeSupport4(ref SupportData))
                    {
                        if (SupportData.ListSecondrySuppo.Any(x => x.SupportName == "PLATE"))
                        {
                            if (CheckforTypeSupport8(ref SupportData))
                            {

                            }
                        }
                    }
                }
            }
            else if (CSupportCount == 2 && PSupportCount == 2 && SSupportCount == 3)
            {
                if (!CheckforTypeSupport22(ref SupportData))
                {
                    CheckforTypeSupport6(ref SupportData);
                }
            }
            else if (CSupportCount == 2 && PSupportCount == 2 && SSupportCount == 2)
            {
                CheckforTypeSupport7(ref SupportData);
            }
            else if (CSupportCount == 2 && PSupportCount == 1 && SSupportCount == 3)
            {
                CheckforTypeSupport31(ref SupportData);
            }
            else if (CSupportCount == 1 && PSupportCount == 2 && SSupportCount == 3)
            {
                if (!CheckforTypeSupport22(ref SupportData))
                {
                    if (!CheckforTypeSupport6(ref SupportData))
                    {
                        CheckforTypeSupport30(ref SupportData);
                    }
                }
            }
            else if (CSupportCount == 2 && PSupportCount == 3 && SSupportCount == 4)
            {
                if (CheckforTypeSupport32(ref SupportData))
                {

                }
            }
            else if (CSupportCount == 1 && PSupportCount == 2 && SSupportCount == 2)
            {
                CheckforTypeSupport7(ref SupportData);
            }
            else if (CSupportCount == 1 && PSupportCount == 2 && SSupportCount == 4)
            {
                if (SupportData.ListSecondrySuppo.Exists(x => x.Size.Contains("Angle")))
                {
                    CheckforTypeSupport20(ref SupportData);
                }
            }
            else if (CSupportCount == 1 && PSupportCount == 3 && SSupportCount == 4)
            {

                CheckforTypeSupport18(ref SupportData);
            }
            else if (CSupportCount == 1 && PSupportCount == 3 && SSupportCount == 5)
            {
                CheckforTypeSupport19(ref SupportData);
            }
            else if (CSupportCount == 2 && PSupportCount == 1 && SSupportCount == 7)
            {
                if (HasWord(SupportData.ListSecondrySuppo, "NB"))
                {

                    //CheckforTypeSupport28(ref SupportData);
                    //Need to Add Check for Primary Sup
                    CheckforTypeSupport25(ref SupportData);
                }
            }
            else if (CSupportCount == 2 && PSupportCount == 2 && SSupportCount == 8)
            {
                if (HasWord(SupportData.ListSecondrySuppo, "NB"))
                {
                    //Need to Add Check for Primary Sup
                    CheckforTypeSupport27(ref SupportData);
                }
            }
        }

        // bool CheckforTypeSupport28(ref SupportData SupData)
        //{
        //}
        bool HasWord(List<SupporSpecData> SecondarySupData, string Word)
        {
            foreach (var Data in SecondarySupData)
            {
                if (Data.Size != null)
                {
                    if (Data.Size.ToUpper().Contains(Word))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        bool CheckforTypeSupport31(ref SupportData SupportData)
        {
            if (Math.Round(SupportData.ListSecondrySuppo[0].Angle.ZinDegree).Equals(Math.Round(SupportData.ListSecondrySuppo[1].Angle.ZinDegree)) && Math.Round(SupportData.ListSecondrySuppo[1].Angle.ZinDegree).Equals(Math.Round(SupportData.ListSecondrySuppo[2].Angle.ZinDegree)) && Math.Round(SupportData.ListSecondrySuppo[0].Angle.YinDegree).Equals(Math.Round(SupportData.ListSecondrySuppo[1].Angle.YinDegree)) && Math.Round(SupportData.ListSecondrySuppo[1].Angle.YinDegree).Equals(Math.Round(SupportData.ListSecondrySuppo[2].Angle.YinDegree)))
            {
                return CheckAnyPartSecInclined(ref SupportData);
            }

            return false;
        }

        bool CheckAnyPartSecInclined(ref SupportData SupportData)
        {

            bool Condition1 = false;
            bool Condition2 = false;
            bool Condition3 = false;
            SupporSpecData HorPart = new SupporSpecData();
            SupporSpecData VerPart = new SupporSpecData();
            foreach (var SecSup in SupportData.ListSecondrySuppo)
            {
                string InclinedId = "";
                if (Math.Round(SecSup.Angle.XinDegree).Equals(45) || Math.Round(SecSup.Angle.XinDegree).Equals(135))
                {
                    InclinedId = SecSup.SuppoId;

                    SecSup.PartDirection = "45Inclined";
                    Condition1 = true;
                }
                else if (Math.Round(SecSup.Angle.XinDegree).Equals(0))
                {
                    SecSup.PartDirection = "Ver";
                    VerPart = SecSup;
                    Condition2 = true;
                }
                else if (Math.Round(SecSup.Angle.XinDegree).Equals(90))
                {
                    SecSup.PartDirection = "Hor";
                    HorPart = SecSup;
                    Condition3 = true;
                }
            }

            if (Condition1 && Condition2 && Condition3)
            {

                if (Math.Round(VerPart.Boundingboxmax.Z) > Math.Round(HorPart.Centroid.Z))
                {
                    System.Windows.Media.Media3D.Vector3D Vec1 = new System.Windows.Media.Media3D.Vector3D(1, 0, 0);
                    System.Windows.Media.Media3D.Vector3D Vec2 = new System.Windows.Media.Media3D.Vector3D(0, 1, 0);
                    System.Windows.Media.Media3D.Vector3D Vec3 = new System.Windows.Media.Media3D.Vector3D(1, 0, 0);

                    double[] Closestpt = new double[3];
                    if (Calculate.DistPoint(GetPt3DFromArray(VerPart.EndPt), GetPt3DFromArray(HorPart.EndPt)) > Calculate.DistPoint(GetPt3DFromArray(VerPart.EndPt), GetPt3DFromArray(HorPart.StPt)))
                    {
                        Vec1.X = HorPart.EndPt[0] - HorPart.StPt[0];
                        Vec1.Y = HorPart.EndPt[1] - HorPart.StPt[1];
                        Vec1.Z = HorPart.EndPt[2] - HorPart.StPt[2];

                        Closestpt = HorPart.StPt;
                    }
                    else
                    {
                        Vec1.X = HorPart.StPt[0] - HorPart.EndPt[0];
                        Vec1.Y = HorPart.StPt[1] - HorPart.EndPt[1];
                        Vec1.Z = HorPart.StPt[2] - HorPart.EndPt[2];

                        Closestpt = HorPart.EndPt;
                    }

                    Vec2.X = VerPart.EndPt[0] - VerPart.StPt[0];
                    Vec2.Y = VerPart.EndPt[1] - VerPart.StPt[1];
                    Vec2.Z = VerPart.EndPt[2] - VerPart.StPt[2];

                    if (Math.Round(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(Vec1, Vec2, Vec3))).Equals(90))
                    {
                        if (Calculate.DistPoint(SupportData.ListPrimarySuppo[0].Centroid, GetPt3DFromArray(VerPart.EndPt)) > Calculate.DistPoint(GetPt3DFromArray(VerPart.EndPt), GetPt3DFromArray(Closestpt)))
                        {
                            SupportData.SupportType = "Support31";
                            return true;
                        }
                    }
                }

            }

            return false;
        }
        //Need to Add the some code for the some conditon for the Support30
        bool CheckforTypeSupport30(ref SupportData SupData)
        {
            if (SupData.ListPrimarySuppo[0].SupportName.Contains("GRP FLG") && SupData.ListPrimarySuppo[1].SupportName.Contains("GRP FLG"))
            {
                System.Windows.Media.Media3D.Vector3D Vec1 = new System.Windows.Media.Media3D.Vector3D(1, 0, 0);
                System.Windows.Media.Media3D.Vector3D Vec2 = new System.Windows.Media.Media3D.Vector3D(0, 1, 0);
                System.Windows.Media.Media3D.Vector3D Vec3 = new System.Windows.Media.Media3D.Vector3D(0, 0, 1);

                Vec1.X = SupData.ListPrimarySuppo[0].NoramlDir.X;
                Vec1.Y = SupData.ListPrimarySuppo[0].NoramlDir.Y;
                Vec1.Z = SupData.ListPrimarySuppo[0].NoramlDir.Z;
                Vec2.X = SupData.ListPrimarySuppo[1].NoramlDir.X;
                Vec2.Y = SupData.ListPrimarySuppo[1].NoramlDir.Y;
                Vec2.Z = SupData.ListPrimarySuppo[1].NoramlDir.Z;

                if (Math.Round(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(Vec1, Vec2, Vec3))).Equals(180))
                {
                    if (SupData.ListSecondrySuppo.Any(x => x.SupportName == "PLATE"))
                    {

                    }
                }
            }

            return false;
        }

        bool CheckforTypeSupport32(ref SupportData SupData)
        {
            foreach (var Prim in SupData.ListPrimarySuppo)
            {

            }


            return false;
        }

        bool CheckforTypeSupport27(ref SupportData SupData)
        {
            int plateCount = 0;
            SupporSpecData NBPart = new SupporSpecData();
            List<SupporSpecData> ChannelC = new List<SupporSpecData>();
            foreach (var Data in SupData.ListSecondrySuppo)
            {
                if (Data.SupportName.ToUpper().Equals("PLATE"))
                {
                    plateCount++;
                }
                else if (Data.Size.ToUpper().Contains("NB"))
                {
                    NBPart = Data;
                }
                else if (Data.Size.ToUpper().Contains("ISMC"))
                {
                    ChannelC.Add(Data);
                }
            }

            if (plateCount == 5 && NBPart != null && ChannelC != null && ChannelC.Count == 2)
            {
                if (Math.Round(Math.Abs(ChannelC[0].Angle.XinDegree - NBPart.Angle.XinDegree)).Equals(90) && Math.Round(Math.Abs(ChannelC[1].Angle.XinDegree - NBPart.Angle.XinDegree)).Equals(90))
                {
                    System.Windows.Media.Media3D.Vector3D Vec1 = new System.Windows.Media.Media3D.Vector3D(1, 0, 0);
                    System.Windows.Media.Media3D.Vector3D Vec2 = new System.Windows.Media.Media3D.Vector3D(0, 1, 0);
                    System.Windows.Media.Media3D.Vector3D Vec3 = new System.Windows.Media.Media3D.Vector3D(1, 0, 0);

                    System.Windows.Media.Media3D.Vector3D Vec4 = new System.Windows.Media.Media3D.Vector3D(1, 0, 0);

                    if (Calculate.DistPoint(GetPt3DFromArray(NBPart.EndPt), GetPt3DFromArray(ChannelC[0].EndPt)) > Calculate.DistPoint(GetPt3DFromArray(NBPart.EndPt), GetPt3DFromArray(ChannelC[0].StPt)))
                    {

                        Vec1.X = ChannelC[0].EndPt[0] - ChannelC[0].StPt[0];
                        Vec1.Y = ChannelC[0].EndPt[1] - ChannelC[0].StPt[1];
                        Vec1.Z = ChannelC[0].EndPt[2] - ChannelC[0].StPt[2];
                    }
                    else
                    {
                        Vec1.X = ChannelC[0].StPt[0] - ChannelC[0].EndPt[0];
                        Vec1.Y = ChannelC[0].StPt[1] - ChannelC[0].EndPt[1];
                        Vec1.Z = ChannelC[0].StPt[2] - ChannelC[0].EndPt[2];
                    }

                    if (Calculate.DistPoint(GetPt3DFromArray(NBPart.EndPt), GetPt3DFromArray(ChannelC[1].EndPt)) > Calculate.DistPoint(GetPt3DFromArray(NBPart.EndPt), GetPt3DFromArray(ChannelC[1].StPt)))
                    {
                        Vec4.X = ChannelC[1].EndPt[0] - ChannelC[1].StPt[0];
                        Vec4.Y = ChannelC[1].EndPt[1] - ChannelC[1].StPt[1];
                        Vec4.Z = ChannelC[1].EndPt[2] - ChannelC[1].StPt[2];
                    }
                    else
                    {
                        Vec4.X = ChannelC[1].StPt[0] - ChannelC[1].EndPt[0];
                        Vec4.Y = ChannelC[1].StPt[1] - ChannelC[1].EndPt[1];
                        Vec4.Z = ChannelC[1].StPt[2] - ChannelC[1].EndPt[2];
                    }


                    Vec2.X = NBPart.EndPt[0] - NBPart.StPt[0];
                    Vec2.Y = NBPart.EndPt[1] - NBPart.StPt[1];
                    Vec2.Z = NBPart.EndPt[2] - NBPart.StPt[2];

                    if ((Math.Round(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(Vec1, Vec2, Vec3))).Equals(90)
                       && Math.Round(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(Vec4, Vec2, Vec3))).Equals(-90)) ||
                       (Math.Round(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(Vec1, Vec2, Vec3))).Equals(-90)
                       && Math.Round(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(Vec4, Vec2, Vec3))).Equals(90)))
                    {
                        if (CheckforCollinear(ChannelC[0], NBPart) && CheckforCollinear(ChannelC[1], NBPart))
                        {
                            if (CheckTwoSuppoCollinear(ChannelC[0], ChannelC[1]))
                            {
                                SupData.SupportType = "Support27";
                                return true;
                            }
                        }
                    }

                }
            }
            return false;
        }

        bool CheckTwoSuppoCollinear(SupporSpecData ChannelC, SupporSpecData NBPart)
        {
            double dist1 = 0, dist2 = 0, dist3 = 0, dist4 = 0, dist5 = 0, dist6 = 0;
            dist1 = Calculate.DistPoint(GetPt3DFromArray(ChannelC.StPt), GetPt3DFromArray(NBPart.StPt));
            dist2 = Calculate.DistPoint(GetPt3DFromArray(ChannelC.EndPt), GetPt3DFromArray(NBPart.StPt));
            dist3 = Calculate.DistPoint(GetPt3DFromArray(ChannelC.EndPt), GetPt3DFromArray(NBPart.EndPt));
            dist4 = Calculate.DistPoint(GetPt3DFromArray(ChannelC.StPt), GetPt3DFromArray(NBPart.EndPt));

            dist5 = Calculate.DistPoint(GetPt3DFromArray(NBPart.StPt), GetPt3DFromArray(NBPart.EndPt));
            dist6 = Calculate.DistPoint(GetPt3DFromArray(ChannelC.StPt), GetPt3DFromArray(ChannelC.EndPt));
            if (dist2 < dist1 && dist3 < dist1 && dist4 < dist1)
            {
                if (Math.Abs(dist1 - (dist6 + dist5)) < 5)
                {
                    return true;
                }
            }
            else if (dist3 < dist2 && dist1 < dist2 && dist4 < dist2)
            {
                if (Math.Abs(dist2 - (dist6 + dist5)) < 5)
                {
                    return true;
                }
            }
            else if (dist2 < dist3 && dist1 < dist3 && dist4 < dist3)
            {
                if (Math.Abs(dist3 - (dist6 + dist5)) < 5)
                {
                    return true;
                }
            }
            else if (dist2 < dist4 && dist1 < dist4 && dist3 < dist4)
            {
                if (Math.Abs(dist4 - (dist6 + dist5)) < 5)
                {
                    return true;
                }
            }

            return false;
        }
        bool CheckforTypeSupport25(ref SupportData SupData)
        {
            int plateCount = 0;
            SupporSpecData NBPart = new SupporSpecData();
            SupporSpecData ChannelC = new SupporSpecData();
            foreach (var Data in SupData.ListSecondrySuppo)
            {
                if (Data.SupportName.ToUpper().Equals("PLATE"))
                {
                    plateCount++;
                }
                else if (Data.Size.ToUpper().Contains("NB"))
                {
                    NBPart = Data;
                }
                else if (Data.Size.ToUpper().Contains("ISMC"))
                {
                    ChannelC = Data;
                }
            }

            if (plateCount == 5 && NBPart != null && ChannelC != null)
            {
                if (Math.Round(Math.Abs(ChannelC.Angle.XinDegree - NBPart.Angle.XinDegree)).Equals(90))
                {
                    System.Windows.Media.Media3D.Vector3D Vec1 = new System.Windows.Media.Media3D.Vector3D(1, 0, 0);
                    System.Windows.Media.Media3D.Vector3D Vec2 = new System.Windows.Media.Media3D.Vector3D(0, 1, 0);
                    System.Windows.Media.Media3D.Vector3D Vec3 = new System.Windows.Media.Media3D.Vector3D(1, 0, 0);

                    Vec1.X = ChannelC.EndPt[0] - ChannelC.StPt[0];
                    Vec1.Y = ChannelC.EndPt[1] - ChannelC.StPt[1];
                    Vec1.Z = ChannelC.EndPt[2] - ChannelC.StPt[2];

                    Vec2.X = NBPart.EndPt[0] - NBPart.StPt[0];
                    Vec2.Y = NBPart.EndPt[1] - NBPart.StPt[1];
                    Vec2.Z = NBPart.EndPt[2] - NBPart.StPt[2];

                    List<Pt3D> BPartCentroids = new List<Pt3D>();
                    List<Pt3D> PPartCentroids = new List<Pt3D>();
                    List<Pt3D> SPartCentroids = new List<Pt3D>();
                    string Orin = "";

                    BPartCentroids = GetDicCentroidBottomPart(SupData);
                    SPartCentroids.Add(NBPart.Centroid);
                    SPartCentroids.Add(ChannelC.Centroid);
                    PPartCentroids = GetDicCentroidPrimaryPart(SupData);

                    if (CheckCentroidInLine(BPartCentroids, PPartCentroids, SPartCentroids, ref Orin))
                    {
                        SupData.SupportType = "Support28";
                        return true;
                    }
                    else if (Math.Round(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(Vec1, Vec2, Vec3))).Equals(90))
                    {
                        if (CheckforCollinear(ChannelC, NBPart))
                        {
                            SupData.SupportType = "Support25";
                            return true;
                        }
                    }
                    else if (Math.Round(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(Vec1, Vec2, Vec3))).Equals(-90))
                    {
                        if (CheckforCollinear(ChannelC, NBPart))
                        {
                            SupData.SupportType = "Support26";
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Here we will check the closest distancefrom first support is collinear with other or not
        /// </summary>
        /// <param name="ChannelC"></param>
        /// <param name="NBPart"></param>
        /// <returns></returns>
        bool CheckforCollinear(SupporSpecData ChannelC, SupporSpecData NBPart)
        {
            double dist1 = 0, dist2 = 0, dist3 = 0, dist4 = 0;
            dist1 = Calculate.DistPoint(GetPt3DFromArray(ChannelC.StPt), GetPt3DFromArray(NBPart.StPt));
            dist2 = Calculate.DistPoint(GetPt3DFromArray(ChannelC.EndPt), GetPt3DFromArray(NBPart.StPt));

            if (dist2 < dist1)
            {
                dist3 = Calculate.DistPoint(GetPt3DFromArray(ChannelC.EndPt), GetPt3DFromArray(NBPart.EndPt));
                dist4 = Calculate.DistPoint(GetPt3DFromArray(NBPart.StPt), GetPt3DFromArray(NBPart.EndPt));

                if (Math.Abs(dist4 - (dist3 + dist2)) < 5)
                {
                    return true;
                }
            }
            else
            {
                dist3 = Calculate.DistPoint(GetPt3DFromArray(ChannelC.StPt), GetPt3DFromArray(NBPart.EndPt));
                dist4 = Calculate.DistPoint(GetPt3DFromArray(NBPart.StPt), GetPt3DFromArray(NBPart.EndPt));

                if (Math.Abs(dist4 - (dist3 + dist1)) < 5)
                {
                    return true;
                }
            }


            return false;
        }
        bool CheckforTypeSupport20(ref SupportData SupData)
        {

            return false;
        }

        System.Windows.Media.Media3D.Vector3D GetVecStartfrompnts(SupporSpecData VerSuppo, SupporSpecData SecData)
        {
            System.Windows.Media.Media3D.Vector3D Vec1 = new System.Windows.Media.Media3D.Vector3D(1, 0, 0);
            if (Calculate.DistPoint(GetPt3DFromArray(VerSuppo.StPt), GetPt3DFromArray(SecData.StPt)) > Calculate.DistPoint(GetPt3DFromArray(VerSuppo.StPt), GetPt3DFromArray(SecData.EndPt)))
            {
                Vec1.X = SecData.StPt[0] - SecData.EndPt[0];
                Vec1.Y = SecData.StPt[1] - SecData.EndPt[1];
                Vec1.Z = SecData.StPt[2] - SecData.EndPt[2];
            }
            else
            {
                Vec1.X = SecData.EndPt[0] - SecData.StPt[0];
                Vec1.Y = SecData.EndPt[1] - SecData.StPt[1];
                Vec1.Z = SecData.EndPt[2] - SecData.StPt[2];
            }

            return Vec1;
        }

        bool CheckforTypeSupport18(ref SupportData SupData)
        {
            List<string> ListParSup = new List<string>();
            List<List<string>> CombinedParSupp = new List<List<string>>();
            //AnycentroidSecondaryMaching(ref SupData) // will use this later 
            ListParSup = Checkandgetparllelsupports(ref SupData, ref CombinedParSupp);

            List<System.Windows.Media.Media3D.Vector3D> ListVec = new List<System.Windows.Media.Media3D.Vector3D>();


            System.Windows.Media.Media3D.Vector3D Vec1 = new System.Windows.Media.Media3D.Vector3D(1, 0, 0);
            System.Windows.Media.Media3D.Vector3D Vec2 = new System.Windows.Media.Media3D.Vector3D(0, 1, 0);
            System.Windows.Media.Media3D.Vector3D Vec3 = new System.Windows.Media.Media3D.Vector3D(1, 0, 0);

            int Count1 = 0;
            int Count2 = 0;

            if (CombinedParSupp.Count == 2)
            {
                if (CombinedParSupp[0].Count == 3 || CombinedParSupp[0].Count == 1 || CombinedParSupp[1].Count == 3 || CombinedParSupp[1].Count == 1)
                {
                    SupporSpecData VerSuppo = new SupporSpecData();

                    if (CombinedParSupp[0].Count == 3)
                    {
                        foreach (var SecData in SupData.ListSecondrySuppo)
                        {
                            if (SecData.SuppoId.Equals(CombinedParSupp[1][0]))
                            {
                                VerSuppo = SecData;
                            }
                        }
                    }
                    else if (CombinedParSupp[0].Count == 1)
                    {
                        foreach (var SecData in SupData.ListSecondrySuppo)
                        {
                            if (SecData.SuppoId.Equals(CombinedParSupp[0][0]))
                            {
                                VerSuppo = SecData;
                            }
                        }
                    }

                    Vec2.X = (VerSuppo.EndPt[0] - VerSuppo.StPt[0]);
                    Vec2.Y = (VerSuppo.EndPt[1] - VerSuppo.StPt[1]);
                    Vec2.Z = (VerSuppo.EndPt[2] - VerSuppo.StPt[2]);

                    Dictionary<string, string> DicIdDir = new Dictionary<string, string>();
                    foreach (var SecData in SupData.ListSecondrySuppo)
                    {
                        if (!SecData.SuppoId.Equals(VerSuppo.SuppoId))
                        {
                            System.Windows.Media.Media3D.Vector3D VecLoc = GetVecStartfrompnts(VerSuppo, SecData);

                            if (Math.Round(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(VecLoc, Vec2, Vec3))).Equals(90))
                            {
                                Count1++;

                                DicIdDir[SecData.SuppoId] = "LeftHor";
                            }
                            else if (Math.Round(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(VecLoc, Vec2, Vec3))).Equals(-90))
                            {
                                Count2++;

                                DicIdDir[SecData.SuppoId] = "RightHor";
                            }
                        }
                        {
                            DicIdDir[SecData.SuppoId] = "Ver";
                        }
                    }

                    List<string> ProecessedPids = new List<string>();

                    if (Count1 == 2 && Count2 == 1)
                    {
                        foreach (var SecData in SupData.ListSecondrySuppo)
                        {
                            foreach (var PsuppoData in SupData.ListPrimarySuppo)
                            {

                                if (ProecessedPids != null && PsuppoData.TouchingPartid != null)
                                {
                                    if ((!ProecessedPids.Contains(PsuppoData.SuppoId)) && PsuppoData.TouchingPartid.Equals(SecData.SuppoId))
                                    {
                                        ProecessedPids.Add(PsuppoData.SuppoId);
                                    }
                                }
                            }
                            SecData.PartDirection = DicIdDir[SecData.SuppoId];
                        }

                        if (ProecessedPids.Count == 3)
                        {
                            SupData.SupportType = "Support18";
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        bool CheckforTypeSupport19(ref SupportData SupData)
        {
            List<string> ListParSup = new List<string>();
            List<List<string>> CombinedParSupp = new List<List<string>>();
            //AnycentroidSecondaryMaching(ref SupData) // will use this later 
            ListParSup = Checkandgetparllelsupports(ref SupData, ref CombinedParSupp);

            List<System.Windows.Media.Media3D.Vector3D> ListVec = new List<System.Windows.Media.Media3D.Vector3D>();


            System.Windows.Media.Media3D.Vector3D Vec1 = new System.Windows.Media.Media3D.Vector3D(1, 0, 0);
            System.Windows.Media.Media3D.Vector3D Vec2 = new System.Windows.Media.Media3D.Vector3D(0, 1, 0);
            System.Windows.Media.Media3D.Vector3D Vec3 = new System.Windows.Media.Media3D.Vector3D(1, 0, 0);

            int Count1 = 0;
            int Count2 = 0;

            if (CombinedParSupp.Count == 2)
            {
                if (CombinedParSupp[0].Count == 4 || CombinedParSupp[0].Count == 1 || CombinedParSupp[1].Count == 4 || CombinedParSupp[1].Count == 1)
                {
                    SupporSpecData VerSuppo = new SupporSpecData();

                    if (CombinedParSupp[0].Count == 4)
                    {
                        foreach (var SecData in SupData.ListSecondrySuppo)
                        {
                            if (SecData.SuppoId.Equals(CombinedParSupp[1][0]))
                            {
                                VerSuppo = SecData;
                            }
                        }
                    }
                    else if (CombinedParSupp[0].Count == 1)
                    {
                        foreach (var SecData in SupData.ListSecondrySuppo)
                        {
                            if (SecData.SuppoId.Equals(CombinedParSupp[0][0]))
                            {
                                VerSuppo = SecData;
                            }
                        }
                    }

                    Vec2.X = (VerSuppo.EndPt[0] - VerSuppo.StPt[0]);
                    Vec2.Y = (VerSuppo.EndPt[1] - VerSuppo.StPt[1]);
                    Vec2.Z = (VerSuppo.EndPt[2] - VerSuppo.StPt[2]);

                    Dictionary<string, string> DicIdDir = new Dictionary<string, string>();
                    foreach (var SecData in SupData.ListSecondrySuppo)
                    {
                        if (!SecData.SuppoId.Equals(VerSuppo.SuppoId))
                        {
                            System.Windows.Media.Media3D.Vector3D VecLoc = GetVecStartfrompnts(VerSuppo, SecData);

                            if (Math.Round(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(VecLoc, Vec2, Vec3))).Equals(90))
                            {
                                Count1++;

                                DicIdDir[SecData.SuppoId] = "LeftHor";
                            }
                            else if (Math.Round(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(VecLoc, Vec2, Vec3))).Equals(-90))
                            {
                                Count2++;

                                DicIdDir[SecData.SuppoId] = "RightHor";
                            }
                        }
                        {
                            DicIdDir[SecData.SuppoId] = "Ver";
                        }
                    }

                    List<string> ProecessedPids = new List<string>();

                    if (Count1 == 2 && Count2 == 2)
                    {
                        foreach (var SecData in SupData.ListSecondrySuppo)
                        {
                            foreach (var PsuppoData in SupData.ListPrimarySuppo)
                            {
                                if ((!ProecessedPids.Contains(PsuppoData.SuppoId)) && PsuppoData.TouchingPartid.Equals(SecData.SuppoId))
                                {
                                    ProecessedPids.Add(PsuppoData.SuppoId);
                                }
                            }
                            SecData.PartDirection = DicIdDir[SecData.SuppoId];
                        }

                        if (ProecessedPids.Count == 4)
                        {
                            SupData.SupportType = "Support19";
                            return true;
                        }
                    }
                }
            }

            return false;
        }
        bool CheckforTypeSupport14(ref SupportData SupportData)
        {
            if (SupportData.ListPrimarySuppo[0].IsSupportNB)
            {
                string Orientation = "";
                if (!AreCentroidsinLine(SupportData, ref Orientation))
                {
                    System.Windows.Media.Media3D.Vector3D Vec1 = new System.Windows.Media.Media3D.Vector3D(1, 0, 0);
                    System.Windows.Media.Media3D.Vector3D Vec2 = new System.Windows.Media.Media3D.Vector3D(0, 1, 0);
                    System.Windows.Media.Media3D.Vector3D Vec3 = new System.Windows.Media.Media3D.Vector3D(0, 0, 1);

                    Vec1.X = SupportData.ListPrimarySuppo[0].NoramlDir.X;
                    Vec1.Y = SupportData.ListPrimarySuppo[0].NoramlDir.Y;
                    Vec1.Z = SupportData.ListPrimarySuppo[0].NoramlDir.Z;

                    Vec2.X = (SupportData.ListSecondrySuppo[0].EndPt[0] - SupportData.ListSecondrySuppo[0].StPt[0]);
                    Vec2.Y = (SupportData.ListSecondrySuppo[0].EndPt[1] - SupportData.ListSecondrySuppo[0].StPt[1]);
                    Vec2.Z = (SupportData.ListSecondrySuppo[0].EndPt[2] - SupportData.ListSecondrySuppo[0].StPt[2]);

                    if (Math.Round(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(Vec1, Vec2, Vec3))).Equals(-90))
                    {
                        SupportData.SupportType = "Support14";
                        return true;
                    }

                }
                else
                {
                    SupportData.SupportType = "Support17";
                    return true;
                }
            }
            return false;
        }

        // Temporary Hard Coading the support Compare LAter we Check All
        List<string> Checkandgetparllelsupports(ref SupportData SupData, ref List<List<string>> CombinedParSupp)
        {
            CombinedParSupp = new List<List<string>>();
            List<string> PallellpartID = new List<string>();
            List<string> ProcessedIds = new List<string>();

            System.Windows.Media.Media3D.Vector3D Vec1 = new System.Windows.Media.Media3D.Vector3D(1, 0, 0);
            System.Windows.Media.Media3D.Vector3D Vec2 = new System.Windows.Media.Media3D.Vector3D(0, 1, 0);
            System.Windows.Media.Media3D.Vector3D Vec3 = new System.Windows.Media.Media3D.Vector3D(1, 0, 0);

            List<string> ListFirstLineParl = new List<string>();
            List<string> ListSecondLineParl = new List<string>();
            List<string> Listthirlinparl = new List<string>();

            Dictionary<string, System.Windows.Media.Media3D.Vector3D> ListVecData = new Dictionary<string, System.Windows.Media.Media3D.Vector3D>();

            foreach (var Data in SupData.ListSecondrySuppo)
            {
                System.Windows.Media.Media3D.Vector3D Vecr = new System.Windows.Media.Media3D.Vector3D();

                Vecr.X = Data.EndPt[0] - Data.StPt[0];
                Vecr.Y = Data.EndPt[1] - Data.StPt[1];
                Vecr.Z = Data.EndPt[2] - Data.StPt[2];
                Vecr.Normalize();
                ListVecData[Data.SuppoId] = Vecr;
            }

            while (ProcessedIds.Count < ListVecData.Count)
            {
                bool IsStart = true;
                List<string> listParllelId = new List<string>();

                foreach (var data in ListVecData)
                {
                    if (!ProcessedIds.Contains(data.Key))
                    {
                        if (IsStart)
                        {
                            Vec1 = data.Value;
                            listParllelId.Add(data.Key);
                            ProcessedIds.Add(data.Key);
                            IsStart = false;
                        }
                        else
                        {
                            double Angle = System.Windows.Media.Media3D.Vector3D.AngleBetween(Vec1, data.Value);

                            if (Math.Round(Angle).Equals(0) || Math.Round(Angle).Equals(180))
                            {
                                listParllelId.Add(data.Key);
                                ProcessedIds.Add(data.Key);
                            }
                        }
                    }
                }

                CombinedParSupp.Add(listParllelId);
            }

            if (ListVecData.Count > 2)
            {
                for (int inx = 0; inx < ListVecData.Count; inx++)
                {
                    if (inx > 0)
                    {
                        double Angle = System.Windows.Media.Media3D.Vector3D.AngleBetween(ListVecData.ElementAt(0).Value, ListVecData.ElementAt(inx).Value);

                        if (Math.Round(Angle).Equals(0) || Math.Round(Angle).Equals(180))
                        {
                            ListFirstLineParl.Add(ListVecData.ElementAt(inx).Key);
                        }
                        else
                        {
                            ListSecondLineParl.Add(ListVecData.ElementAt(inx).Key);
                        }
                    }
                    else
                    {
                        ListFirstLineParl.Add(ListVecData.ElementAt(0).Key);
                    }
                }
            }

            if (ListSecondLineParl.Count > 1)
            {
                for (int inx = 0; inx < ListSecondLineParl.Count; inx++)
                {
                    if (inx > 0)
                    {
                        double Angle = System.Windows.Media.Media3D.Vector3D.AngleBetween(ListVecData[ListSecondLineParl[0]], ListVecData[ListSecondLineParl[inx]]);

                        if (Math.Round(Angle).Equals(0) || Math.Round(Angle).Equals(180))
                        {
                            Listthirlinparl.Add(ListSecondLineParl[inx]);
                        }
                    }
                    else
                    {
                        Listthirlinparl.Add(ListSecondLineParl[0]);
                    }
                }
            }

            // List<string> ListParSup = new List<string>();

            /* if (Math.Round(SupData.ListSecondrySuppo[0].Angle.XinDegree).Equals(Math.Round(SupData.ListSecondrySuppo[1].Angle.XinDegree)) && Math.Round(SupData.ListSecondrySuppo[0].Angle.YinDegree).Equals(Math.Round(SupData.ListSecondrySuppo[1].Angle.YinDegree)) && Math.Round(SupData.ListSecondrySuppo[0].Angle.ZinDegree).Equals(Math.Round(SupData.ListSecondrySuppo[1].Angle.ZinDegree)))
             {
                 ListParSup.Add(SupData.ListSecondrySuppo[0].SuppoId);
                 ListParSup.Add(SupData.ListSecondrySuppo[1].SuppoId);
             }
             else if (Math.Round(SupData.ListSecondrySuppo[0].Angle.XinDegree).Equals(Math.Round(SupData.ListSecondrySuppo[2].Angle.XinDegree)) && Math.Round(SupData.ListSecondrySuppo[0].Angle.YinDegree).Equals(Math.Round(SupData.ListSecondrySuppo[2].Angle.YinDegree)) && Math.Round(SupData.ListSecondrySuppo[0].Angle.ZinDegree).Equals(Math.Round(SupData.ListSecondrySuppo[2].Angle.ZinDegree)))
             {
                 ListParSup.Add(SupData.ListSecondrySuppo[0].SuppoId);
                 ListParSup.Add(SupData.ListSecondrySuppo[2].SuppoId);
             }
             else if (Math.Round(SupData.ListSecondrySuppo[1].Angle.XinDegree).Equals(Math.Round(SupData.ListSecondrySuppo[2].Angle.XinDegree)) && Math.Round(SupData.ListSecondrySuppo[1].Angle.YinDegree).Equals(Math.Round(SupData.ListSecondrySuppo[2].Angle.YinDegree)) && Math.Round(SupData.ListSecondrySuppo[1].Angle.ZinDegree).Equals(Math.Round(SupData.ListSecondrySuppo[2].Angle.ZinDegree)))
             {
                 ListParSup.Add(SupData.ListSecondrySuppo[1].SuppoId);
                 ListParSup.Add(SupData.ListSecondrySuppo[2].SuppoId);
             }*/

            return ProcessedIds;
        }

        bool CheckforTypeSupport22(ref SupportData SupData)
        {
            List<List<string>> CombinedParSupp = new List<List<string>>();
            List<string> ListProcessedParts = new List<string>();
            ListProcessedParts = Checkandgetparllelsupports(ref SupData, ref CombinedParSupp);

            if (CombinedParSupp.Count == 2)
            {
                try
                {

                    if (CombinedParSupp[0].Count == 2)
                    {
                        List<Pt3D> BPartCentroids = new List<Pt3D>();
                        List<Pt3D> PPartCentroids = new List<Pt3D>();
                        List<Pt3D> SPartCentroids = new List<Pt3D>();

                        PPartCentroids.Add(SupData.ListPrimarySuppo.Find(X => X.TouchingPartid.Equals(CombinedParSupp[0][0])).Centroid);

                        SPartCentroids.Add(SupData.ListSecondrySuppo.Find(x => x.SuppoId == CombinedParSupp[0][0]).Centroid);

                        SPartCentroids.Add(SupData.ListSecondrySuppo.Find(x => x.SuppoId == CombinedParSupp[1][0]).Centroid);

                        if (SupData.ListConcreteData.Count > 0)
                        {
                            BPartCentroids = BPartCentroids = GetDicCentroidBottomPart(SupData);
                        }

                        string Orientation = "";

                        if (!CheckCentroidInLine(BPartCentroids, PPartCentroids, SPartCentroids, ref Orientation))
                        {
                            BPartCentroids.Clear();
                            PPartCentroids.Clear();
                            SPartCentroids.Clear();

                            PPartCentroids.Add(SupData.ListPrimarySuppo.Find(X => X.TouchingPartid.Equals(CombinedParSupp[0][1])).Centroid);

                            SPartCentroids.Add(SupData.ListSecondrySuppo.Find(x => x.SuppoId == CombinedParSupp[0][1]).Centroid);

                            SPartCentroids.Add(SupData.ListSecondrySuppo.Find(x => x.SuppoId == CombinedParSupp[1][0]).Centroid);

                            if (SupData.ListConcreteData.Count > 0)
                            {
                                BPartCentroids = BPartCentroids = GetDicCentroidBottomPart(SupData);
                            }

                            Orientation = "";

                            if (CheckCentroidInLine(BPartCentroids, PPartCentroids, SPartCentroids, ref Orientation))
                            {
                                System.Windows.Media.Media3D.Vector3D Vec1 = new System.Windows.Media.Media3D.Vector3D(1, 0, 0);
                                System.Windows.Media.Media3D.Vector3D Vec2 = new System.Windows.Media.Media3D.Vector3D(0, 1, 0);
                                System.Windows.Media.Media3D.Vector3D Vec3 = new System.Windows.Media.Media3D.Vector3D(1, 0, 0);

                                Vec1.X = (SupData.ListSecondrySuppo.Find(x => x.SuppoId == CombinedParSupp[0][0]).EndPt[0] - SupData.ListSecondrySuppo.Find(x => x.SuppoId == CombinedParSupp[0][0]).StPt[0]);
                                Vec1.Y = (SupData.ListSecondrySuppo.Find(x => x.SuppoId == CombinedParSupp[0][0]).EndPt[1] - SupData.ListSecondrySuppo.Find(x => x.SuppoId == CombinedParSupp[0][0]).StPt[1]);
                                Vec1.Z = (SupData.ListSecondrySuppo.Find(x => x.SuppoId == CombinedParSupp[0][0]).EndPt[2] - SupData.ListSecondrySuppo.Find(x => x.SuppoId == CombinedParSupp[0][0]).StPt[2]);

                                Vec2.X = (SupData.ListSecondrySuppo.Find(x => x.SuppoId == CombinedParSupp[1][0]).EndPt[0] - SupData.ListSecondrySuppo.Find(x => x.SuppoId == CombinedParSupp[1][0]).StPt[0]);
                                Vec2.Y = (SupData.ListSecondrySuppo.Find(x => x.SuppoId == CombinedParSupp[1][0]).EndPt[1] - SupData.ListSecondrySuppo.Find(x => x.SuppoId == CombinedParSupp[1][0]).StPt[1]);
                                Vec2.Z = (SupData.ListSecondrySuppo.Find(x => x.SuppoId == CombinedParSupp[1][0]).EndPt[2] - SupData.ListSecondrySuppo.Find(x => x.SuppoId == CombinedParSupp[1][0]).StPt[2]);

                                if (Math.Round(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(Vec1, Vec2, Vec3))).Equals(-90))
                                {
                                    SupData.SupportType = "Support12";
                                    return true;
                                }
                                else if (Math.Round(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(Vec1, Vec2, Vec3))).Equals(90))
                                {
                                    SupData.SupportType = "Support1";
                                    return true;
                                }
                                else
                                {
                                    return false;
                                }
                            }
                            else
                            {
                                return false;
                            }
                        }
                        else
                        {
                            System.Windows.Media.Media3D.Vector3D Vec1 = new System.Windows.Media.Media3D.Vector3D(1, 0, 0);
                            System.Windows.Media.Media3D.Vector3D Vec2 = new System.Windows.Media.Media3D.Vector3D(0, 1, 0);
                            System.Windows.Media.Media3D.Vector3D Vec3 = new System.Windows.Media.Media3D.Vector3D(1, 0, 0);

                            Vec1.X = (SupData.ListSecondrySuppo.Find(x => x.SuppoId == CombinedParSupp[0][1]).EndPt[0] - SupData.ListSecondrySuppo.Find(x => x.SuppoId == CombinedParSupp[0][1]).StPt[0]);
                            Vec1.Y = (SupData.ListSecondrySuppo.Find(x => x.SuppoId == CombinedParSupp[0][1]).EndPt[1] - SupData.ListSecondrySuppo.Find(x => x.SuppoId == CombinedParSupp[0][1]).StPt[1]);
                            Vec1.Z = (SupData.ListSecondrySuppo.Find(x => x.SuppoId == CombinedParSupp[0][1]).EndPt[2] - SupData.ListSecondrySuppo.Find(x => x.SuppoId == CombinedParSupp[0][1]).StPt[2]);

                            Vec2.X = (SupData.ListSecondrySuppo.Find(x => x.SuppoId == CombinedParSupp[1][0]).EndPt[0] - SupData.ListSecondrySuppo.Find(x => x.SuppoId == CombinedParSupp[1][0]).StPt[0]);
                            Vec2.Y = (SupData.ListSecondrySuppo.Find(x => x.SuppoId == CombinedParSupp[1][0]).EndPt[1] - SupData.ListSecondrySuppo.Find(x => x.SuppoId == CombinedParSupp[1][0]).StPt[1]);
                            Vec2.Z = (SupData.ListSecondrySuppo.Find(x => x.SuppoId == CombinedParSupp[1][0]).EndPt[2] - SupData.ListSecondrySuppo.Find(x => x.SuppoId == CombinedParSupp[1][0]).StPt[2]);

                            if (Math.Round(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(Vec1, Vec2, Vec3))).Equals(-90))
                            {
                                SupData.SupportType = "Support12";
                                return true;
                            }
                            else if (Math.Round(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(Vec1, Vec2, Vec3))).Equals(90))
                            {
                                SupData.SupportType = "Support1";
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                    else if (CombinedParSupp[1].Count == 2)
                    {
                        List<Pt3D> BPartCentroids = new List<Pt3D>();
                        List<Pt3D> PPartCentroids = new List<Pt3D>();
                        List<Pt3D> SPartCentroids = new List<Pt3D>();

                        PPartCentroids.Add(SupData.ListPrimarySuppo.Find(X => X.TouchingPartid.Equals(CombinedParSupp[1][0])).Centroid);

                        SPartCentroids.Add(SupData.ListSecondrySuppo.Find(x => x.SuppoId == CombinedParSupp[1][0]).Centroid);

                        SPartCentroids.Add(SupData.ListSecondrySuppo.Find(x => x.SuppoId == CombinedParSupp[0][0]).Centroid);

                        if (SupData.ListConcreteData.Count > 0)
                        {
                            BPartCentroids = BPartCentroids = GetDicCentroidBottomPart(SupData);
                        }

                        string Orientation = "";

                        if (!CheckCentroidInLine(BPartCentroids, PPartCentroids, SPartCentroids, ref Orientation))
                        {
                            BPartCentroids.Clear();
                            PPartCentroids.Clear();
                            SPartCentroids.Clear();

                            PPartCentroids.Add(SupData.ListPrimarySuppo.Find(X => X.TouchingPartid.Equals(CombinedParSupp[1][1])).Centroid);

                            SPartCentroids.Add(SupData.ListSecondrySuppo.Find(x => x.SuppoId == CombinedParSupp[1][1]).Centroid);

                            SPartCentroids.Add(SupData.ListSecondrySuppo.Find(x => x.SuppoId == CombinedParSupp[0][0]).Centroid);

                            if (SupData.ListConcreteData.Count > 0)
                            {
                                BPartCentroids = BPartCentroids = GetDicCentroidBottomPart(SupData);
                            }

                            Orientation = "";

                            if (CheckCentroidInLine(BPartCentroids, PPartCentroids, SPartCentroids, ref Orientation))
                            {
                                System.Windows.Media.Media3D.Vector3D Vec1 = new System.Windows.Media.Media3D.Vector3D(1, 0, 0);
                                System.Windows.Media.Media3D.Vector3D Vec2 = new System.Windows.Media.Media3D.Vector3D(0, 1, 0);
                                System.Windows.Media.Media3D.Vector3D Vec3 = new System.Windows.Media.Media3D.Vector3D(1, 0, 0);

                                Vec1.X = (SupData.ListSecondrySuppo.Find(x => x.SuppoId == CombinedParSupp[1][0]).EndPt[0] - SupData.ListSecondrySuppo.Find(x => x.SuppoId == CombinedParSupp[1][0]).StPt[0]);
                                Vec1.Y = (SupData.ListSecondrySuppo.Find(x => x.SuppoId == CombinedParSupp[1][0]).EndPt[1] - SupData.ListSecondrySuppo.Find(x => x.SuppoId == CombinedParSupp[1][0]).StPt[1]);
                                Vec1.Z = (SupData.ListSecondrySuppo.Find(x => x.SuppoId == CombinedParSupp[1][0]).EndPt[2] - SupData.ListSecondrySuppo.Find(x => x.SuppoId == CombinedParSupp[1][0]).StPt[2]);

                                Vec2.X = (SupData.ListSecondrySuppo.Find(x => x.SuppoId == CombinedParSupp[0][0]).EndPt[0] - SupData.ListSecondrySuppo.Find(x => x.SuppoId == CombinedParSupp[0][0]).StPt[0]);
                                Vec2.Y = (SupData.ListSecondrySuppo.Find(x => x.SuppoId == CombinedParSupp[0][0]).EndPt[1] - SupData.ListSecondrySuppo.Find(x => x.SuppoId == CombinedParSupp[0][0]).StPt[1]);
                                Vec2.Z = (SupData.ListSecondrySuppo.Find(x => x.SuppoId == CombinedParSupp[0][0]).EndPt[2] - SupData.ListSecondrySuppo.Find(x => x.SuppoId == CombinedParSupp[0][0]).StPt[2]);

                                if (Math.Round(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(Vec1, Vec2, Vec3))).Equals(-90))
                                {
                                    SupData.SupportType = "Support12";
                                    return true;
                                }
                                else if (Math.Round(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(Vec1, Vec2, Vec3))).Equals(90))
                                {
                                    SupData.SupportType = "Support1";
                                    return true;
                                }
                                else
                                {
                                    return false;
                                }

                            }
                            else
                            {
                                return false;
                            }
                        }
                        else
                        {
                            System.Windows.Media.Media3D.Vector3D Vec1 = new System.Windows.Media.Media3D.Vector3D(1, 0, 0);
                            System.Windows.Media.Media3D.Vector3D Vec2 = new System.Windows.Media.Media3D.Vector3D(0, 1, 0);
                            System.Windows.Media.Media3D.Vector3D Vec3 = new System.Windows.Media.Media3D.Vector3D(1, 0, 0);

                            Vec1.X = (SupData.ListSecondrySuppo.Find(x => x.SuppoId == CombinedParSupp[1][1]).EndPt[0] - SupData.ListSecondrySuppo.Find(x => x.SuppoId == CombinedParSupp[1][1]).StPt[0]);
                            Vec1.Y = (SupData.ListSecondrySuppo.Find(x => x.SuppoId == CombinedParSupp[1][1]).EndPt[1] - SupData.ListSecondrySuppo.Find(x => x.SuppoId == CombinedParSupp[1][1]).StPt[1]);
                            Vec1.Z = (SupData.ListSecondrySuppo.Find(x => x.SuppoId == CombinedParSupp[1][1]).EndPt[2] - SupData.ListSecondrySuppo.Find(x => x.SuppoId == CombinedParSupp[1][1]).StPt[2]);

                            Vec2.X = (SupData.ListSecondrySuppo.Find(x => x.SuppoId == CombinedParSupp[0][0]).EndPt[0] - SupData.ListSecondrySuppo.Find(x => x.SuppoId == CombinedParSupp[0][0]).StPt[0]);
                            Vec2.Y = (SupData.ListSecondrySuppo.Find(x => x.SuppoId == CombinedParSupp[0][0]).EndPt[1] - SupData.ListSecondrySuppo.Find(x => x.SuppoId == CombinedParSupp[0][0]).StPt[1]);
                            Vec2.Z = (SupData.ListSecondrySuppo.Find(x => x.SuppoId == CombinedParSupp[0][0]).EndPt[2] - SupData.ListSecondrySuppo.Find(x => x.SuppoId == CombinedParSupp[0][0]).StPt[2]);

                            if (Math.Round(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(Vec1, Vec2, Vec3))).Equals(-90))
                            {
                                SupData.SupportType = "Support12";
                                return true;
                            }
                            else if (Math.Round(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(Vec1, Vec2, Vec3))).Equals(90))
                            {
                                SupData.SupportType = "Support1";
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                }
                catch (Exception)
                {
                }
            }

            return false;
        }

        bool CheckforTypeSupport6(ref SupportData SupData)
        {
            List<string> ListParSup = new List<string>();
            List<List<string>> CombinedParSupp = new List<List<string>>();
            ListParSup = Checkandgetparllelsupports(ref SupData, ref CombinedParSupp);

            System.Windows.Media.Media3D.Vector3D Vec1 = new System.Windows.Media.Media3D.Vector3D(1, 0, 0);
            System.Windows.Media.Media3D.Vector3D Vec2 = new System.Windows.Media.Media3D.Vector3D(0, 1, 0);
            System.Windows.Media.Media3D.Vector3D Vec3 = new System.Windows.Media.Media3D.Vector3D(1, 0, 0);


            if (ListParSup.Count > 0)
            {
                foreach (var Sup in SupData.ListSecondrySuppo)
                {
                    if (ListParSup.Contains(Sup.SuppoId))
                    {
                        Vec1.X = (Sup.EndPt[0] - Sup.StPt[0]);
                        Vec1.Y = (Sup.EndPt[1] - Sup.StPt[1]);
                        Vec1.Z = (Sup.EndPt[2] - Sup.StPt[2]);

                        //GetTouchingPrim
                    }
                    else
                    {
                        Vec2.X = (Sup.EndPt[0] - Sup.StPt[0]);
                        Vec2.Y = (Sup.EndPt[1] - Sup.StPt[1]);
                        Vec2.Z = (Sup.EndPt[2] - Sup.StPt[2]);
                    }
                }
            }

            if (Math.Round(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(Vec1, Vec2, Vec3))).Equals(90))
            {
                foreach (var Sup in SupData.ListSecondrySuppo)
                {
                    if (ListParSup.Contains(Sup.SuppoId))
                    {
                        Sup.PartDirection = "Hor";
                    }
                    else
                    {
                        Sup.PartDirection = "Ver";
                    }
                }

                SupData.SupportType = "Support6";

                return true;
            }

            return false;
        }

        bool CheckforTypeSupport7(ref SupportData SupData)
        {
            if (SupData.ListPrimarySuppo[0].SupportName.Contains("GRP FLG") && SupData.ListPrimarySuppo[1].SupportName.Contains("GRP FLG"))
            {
                System.Windows.Media.Media3D.Vector3D Vec1 = new System.Windows.Media.Media3D.Vector3D(1, 0, 0);
                System.Windows.Media.Media3D.Vector3D Vec2 = new System.Windows.Media.Media3D.Vector3D(0, 1, 0);
                System.Windows.Media.Media3D.Vector3D Vec3 = new System.Windows.Media.Media3D.Vector3D(0, 0, 1);

                Vec1.X = SupData.ListPrimarySuppo[0].NoramlDir.X;
                Vec1.Y = SupData.ListPrimarySuppo[0].NoramlDir.Y;
                Vec1.Z = SupData.ListPrimarySuppo[0].NoramlDir.Z;
                Vec2.X = SupData.ListPrimarySuppo[1].NoramlDir.X;
                Vec2.Y = SupData.ListPrimarySuppo[1].NoramlDir.Y;
                Vec2.Z = SupData.ListPrimarySuppo[1].NoramlDir.Z;

                if (Math.Round(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(Vec1, Vec2, Vec3))).Equals(180))
                {
                    if (SupData.ListSecondrySuppo.Any(x => x.SupportName == "PLATE"))
                    {
                        SupData.SupportType = "Support7";
                        return true;
                    }
                }
            }

            return false;
        }

        // we are considering this support int the Positive Z Direction 
        bool CheckforTypeSupport8(ref SupportData SupData)
        {
            string Orientation = "";
            if (AreCentroidsinLine(SupData, ref Orientation))
            {
                if (Orientation.Equals("XY"))
                {
                    if (SupData.ListSecondrySuppo.Any(x => x.Size != null && x.Size.ToUpper().Contains("SLOPING CHANNEL")))
                    {
                        if (SupData.ListPrimarySuppo[0].Centroid.Z > SupData.ListSecondrySuppo.Find(x => x.SupportName == "PLATE").Centroid.Z && SupData.ListSecondrySuppo.Find(x => x.SupportName == "PLATE").Centroid.Z > SupData.ListSecondrySuppo.Find(x => x.Size != null && x.Size.ToUpper().Contains("SLOPING CHANNEL")).Centroid.Z)
                        {

                            if (SupData.ListPrimarySuppo[0].SupportName.Contains("GRP FLG"))
                            {
                                if (Math.Round(Math.Abs(SupData.ListPrimarySuppo[0].FaceLocalAngle.XinDegree - ((SupData.ListSecondrySuppo.Find(x => x.Size != null && x.Size.ToUpper().Contains("SLOPING CHANNEL")).Angle.ZinDegree)))).Equals(180) && Math.Round(Math.Abs((SupData.ListPrimarySuppo[0].FaceLocalAngle.ZinDegree) - (SupData.ListSecondrySuppo.Find(x => x.Size != null && x.Size.ToUpper().Contains("SLOPING CHANNEL")).Angle.YinDegree))).Equals(90))
                                {
                                    SupData.SupportType = "Support10";
                                    return true;
                                }
                                else if (Math.Round(Math.Abs(SupData.ListPrimarySuppo[0].FaceLocalAngle.XinDegree)).Equals((SupData.ListSecondrySuppo.Find(x => x.Size != null && x.Size.ToUpper().Contains("SLOPING CHANNEL")).Angle.ZinDegree)) && Math.Round(Math.Abs((SupData.ListPrimarySuppo[0].FaceLocalAngle.ZinDegree) - (SupData.ListSecondrySuppo.Find(x => x.Size != null && x.Size.ToUpper().Contains("SLOPING CHANNEL")).Angle.YinDegree))).Equals(90))
                                {
                                    //this is Support12
                                    SupData.SupportType = "Support10";
                                    return true;
                                }
                            }
                            else if (SupData.ListPrimarySuppo[0].SupportName.Contains("NB"))
                            {
                                if (Math.Round(Math.Abs((SupData.ListPrimarySuppo[0].Angle.XinDegree) - (SupData.ListSecondrySuppo.Find(x => x.Size != null && x.Size.ToUpper().Contains("SLOPING CHANNEL")).Angle.XinDegree))).Equals(90) && Math.Round(Math.Abs((SupData.ListPrimarySuppo[0].Angle.ZinDegree) - (SupData.ListSecondrySuppo.Find(x => x.Size != null && x.Size.ToUpper().Contains("SLOPING CHANNEL")).Angle.ZinDegree))).Equals(90))
                                {
                                    SupData.SupportType = "Support8";
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                return false;
            }

            return false;
        }


        bool CheckforTypeSupport4(ref SupportData SupportData)
        {
            if (Math.Round(SupportData.ListSecondrySuppo[0].Angle.ZinDegree).Equals(Math.Round(SupportData.ListSecondrySuppo[1].Angle.ZinDegree)))
            {
                if (Math.Abs(SupportData.ListSecondrySuppo[0].Angle.XinDegree) > Math.Abs(SupportData.ListSecondrySuppo[1].Angle.XinDegree))
                {
                    if (Math.Abs(SupportData.ListSecondrySuppo[0].Angle.XinDegree).Equals(90))
                    {
                        if (SupportData.ListSecondrySuppo[0].Centroid.Z > SupportData.ListSecondrySuppo[1].Centroid.Z)
                        {
                            SupporSpecData HorPart = new SupporSpecData();
                            SupporSpecData VerPart = new SupporSpecData();

                            HorPart = SupportData.ListSecondrySuppo[0];
                            VerPart = SupportData.ListSecondrySuppo[1];
                            System.Windows.Media.Media3D.Vector3D Vec1 = new System.Windows.Media.Media3D.Vector3D(1, 0, 0);
                            System.Windows.Media.Media3D.Vector3D Vec2 = new System.Windows.Media.Media3D.Vector3D(0, 1, 0);
                            System.Windows.Media.Media3D.Vector3D Vec3 = new System.Windows.Media.Media3D.Vector3D(1, 0, 0);

                            double[] Closestpt = new double[3];
                            if (Calculate.DistPoint(GetPt3DFromArray(VerPart.EndPt), GetPt3DFromArray(HorPart.EndPt)) > Calculate.DistPoint(GetPt3DFromArray(VerPart.EndPt), GetPt3DFromArray(HorPart.StPt)))
                            {
                                Vec1.X = HorPart.EndPt[0] - HorPart.StPt[0];
                                Vec1.Y = HorPart.EndPt[1] - HorPart.StPt[1];
                                Vec1.Z = HorPart.EndPt[2] - HorPart.StPt[2];

                                Closestpt = HorPart.StPt;
                            }
                            else
                            {
                                Vec1.X = HorPart.StPt[0] - HorPart.EndPt[0];
                                Vec1.Y = HorPart.StPt[1] - HorPart.EndPt[1];
                                Vec1.Z = HorPart.StPt[2] - HorPart.EndPt[2];

                                Closestpt = HorPart.EndPt;
                            }

                            Vec2.X = VerPart.EndPt[0] - VerPart.StPt[0];
                            Vec2.Y = VerPart.EndPt[1] - VerPart.StPt[1];
                            Vec2.Z = VerPart.EndPt[2] - VerPart.StPt[2];

                            if (Math.Abs(Math.Round(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(Vec1, Vec2, Vec3)))).Equals(90))
                            {
                                if (Calculate.DistPoint(SupportData.ListPrimarySuppo[0].Centroid, GetPt3DFromArray(VerPart.EndPt)) > Calculate.DistPoint(GetPt3DFromArray(VerPart.EndPt), GetPt3DFromArray(Closestpt)))
                                {
                                    if (Math.Round(SupportData.ListSecondrySuppo[1].Boundingboxmax.Z) > Math.Round(SupportData.ListSecondrySuppo[0].Centroid.Z))
                                    {
                                        SupportData.SupportType = "Support5";
                                    }
                                    else
                                    {
                                        SupportData.SupportType = "Support4";
                                    }
                                    SupportData.ListSecondrySuppo[0].PartDirection = "Hor";
                                    SupportData.ListSecondrySuppo[1].PartDirection = "Ver";

                                    return true;
                                }
                            }

                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    if (Math.Abs(SupportData.ListSecondrySuppo[1].Angle.XinDegree).Equals(90))
                    {
                        if (SupportData.ListSecondrySuppo[0].Centroid.Z < SupportData.ListSecondrySuppo[1].Centroid.Z)
                        {
                            SupporSpecData HorPart = new SupporSpecData();
                            SupporSpecData VerPart = new SupporSpecData();

                            HorPart = SupportData.ListSecondrySuppo[1];
                            VerPart = SupportData.ListSecondrySuppo[0];
                            System.Windows.Media.Media3D.Vector3D Vec1 = new System.Windows.Media.Media3D.Vector3D(1, 0, 0);
                            System.Windows.Media.Media3D.Vector3D Vec2 = new System.Windows.Media.Media3D.Vector3D(0, 1, 0);
                            System.Windows.Media.Media3D.Vector3D Vec3 = new System.Windows.Media.Media3D.Vector3D(1, 0, 0);

                            double[] Closestpt = new double[3];
                            if (Calculate.DistPoint(GetPt3DFromArray(VerPart.EndPt), GetPt3DFromArray(HorPart.EndPt)) > Calculate.DistPoint(GetPt3DFromArray(VerPart.EndPt), GetPt3DFromArray(HorPart.StPt)))
                            {
                                Vec1.X = HorPart.EndPt[0] - HorPart.StPt[0];
                                Vec1.Y = HorPart.EndPt[1] - HorPart.StPt[1];
                                Vec1.Z = HorPart.EndPt[2] - HorPart.StPt[2];

                                Closestpt = HorPart.StPt;
                            }
                            else
                            {
                                Vec1.X = HorPart.StPt[0] - HorPart.EndPt[0];
                                Vec1.Y = HorPart.StPt[1] - HorPart.EndPt[1];
                                Vec1.Z = HorPart.StPt[2] - HorPart.EndPt[2];

                                Closestpt = HorPart.EndPt;
                            }

                            Vec2.X = VerPart.EndPt[0] - VerPart.StPt[0];
                            Vec2.Y = VerPart.EndPt[1] - VerPart.StPt[1];
                            Vec2.Z = VerPart.EndPt[2] - VerPart.StPt[2];

                            if (Math.Abs(Math.Round(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(Vec1, Vec2, Vec3)))).Equals(90))
                            {
                                if (Calculate.DistPoint(SupportData.ListPrimarySuppo[0].Centroid, GetPt3DFromArray(VerPart.EndPt)) > Calculate.DistPoint(GetPt3DFromArray(VerPart.EndPt), GetPt3DFromArray(Closestpt)))
                                {
                                    if (Math.Round(SupportData.ListSecondrySuppo[0].Boundingboxmax.Z) > Math.Round(SupportData.ListSecondrySuppo[1].Centroid.Z))
                                    {
                                        SupportData.SupportType = "Support5";
                                    }
                                    else
                                    {
                                        SupportData.SupportType = "Support4";
                                    }
                                    SupportData.ListSecondrySuppo[1].PartDirection = "Hor";
                                    SupportData.ListSecondrySuppo[0].PartDirection = "Ver";

                                    return true;
                                }
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            return false;
        }

        bool CheckforTypeSupport3(ref SupportData SupportData)
        {
            if (SupportData.ListSecondrySuppo[0].Angle.YinDegree.Equals(0) && SupportData.ListSecondrySuppo[1].Angle.YinDegree.Equals(0))
            {
                if (SupportData.ListSecondrySuppo[0].Angle.ZinDegree.Equals(SupportData.ListSecondrySuppo[1].Angle.ZinDegree))
                {
                    if (Math.Abs(SupportData.ListSecondrySuppo[0].Angle.XinDegree) > Math.Abs(SupportData.ListSecondrySuppo[1].Angle.XinDegree))
                    {
                        if (Math.Abs(Math.Round(SupportData.ListSecondrySuppo[0].Angle.XinDegree)).Equals(90) && Math.Abs(Math.Round(SupportData.ListSecondrySuppo[1].Angle.XinDegree)).Equals(45))
                        {
                            if (Math.Round(SupportData.ListSecondrySuppo[0].StPt[0]).Equals(Math.Round(SupportData.ListSecondrySuppo[1].StPt[0])) || Math.Round(SupportData.ListSecondrySuppo[0].EndPt[0]).Equals(Math.Round(SupportData.ListSecondrySuppo[1].EndPt[0])) || (Math.Round(SupportData.ListSecondrySuppo[0].StPt[1]).Equals(Math.Round(SupportData.ListSecondrySuppo[1].StPt[1])) || Math.Round(SupportData.ListSecondrySuppo[0].EndPt[1]).Equals(Math.Round(SupportData.ListSecondrySuppo[1].EndPt[1]))) || (Math.Round(SupportData.ListSecondrySuppo[0].StPt[2]).Equals(Math.Round(SupportData.ListSecondrySuppo[1].StPt[2])) || Math.Round(SupportData.ListSecondrySuppo[0].EndPt[2]).Equals(Math.Round(SupportData.ListSecondrySuppo[1].EndPt[2]))))
                            {
                                SupportData.ListSecondrySuppo[1].PartDirection = "45Inclined";
                                SupportData.ListSecondrySuppo[0].PartDirection = "Hor";
                                SupportData.SupportType = "Support3";
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (Math.Abs(Math.Round(SupportData.ListSecondrySuppo[1].Angle.XinDegree)).Equals(90) && Math.Abs(Math.Round(SupportData.ListSecondrySuppo[0].Angle.XinDegree)).Equals(45))
                        {
                            if (Math.Round(SupportData.ListSecondrySuppo[0].StPt[0]).Equals(Math.Round(SupportData.ListSecondrySuppo[1].StPt[0])) || Math.Round(SupportData.ListSecondrySuppo[0].EndPt[0]).Equals(Math.Round(SupportData.ListSecondrySuppo[1].EndPt[0])) || (Math.Round(SupportData.ListSecondrySuppo[0].StPt[1]).Equals(Math.Round(SupportData.ListSecondrySuppo[1].StPt[1])) || Math.Round(SupportData.ListSecondrySuppo[0].EndPt[1]).Equals(Math.Round(SupportData.ListSecondrySuppo[1].EndPt[1]))) || (Math.Round(SupportData.ListSecondrySuppo[0].StPt[2]).Equals(Math.Round(SupportData.ListSecondrySuppo[1].StPt[2])) || Math.Round(SupportData.ListSecondrySuppo[0].EndPt[2]).Equals(Math.Round(SupportData.ListSecondrySuppo[1].EndPt[2]))))
                            {
                                SupportData.ListSecondrySuppo[0].PartDirection = "45Inclined";
                                SupportData.ListSecondrySuppo[1].PartDirection = "Hor";
                                SupportData.SupportType = "Support3";
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }
            return false;
        }

        bool CheckforTypeSupport2(ref SupportData SupportData)
        {
            if (SupportData.ListSecondrySuppo[0].Angle.YinDegree.Equals(0) && SupportData.ListSecondrySuppo[1].Angle.YinDegree.Equals(0))
            {
                if (SupportData.ListSecondrySuppo[0].Angle.ZinDegree.Equals(SupportData.ListSecondrySuppo[1].Angle.ZinDegree))
                {
                    if (Math.Abs(SupportData.ListSecondrySuppo[0].Angle.XinDegree) > Math.Abs(SupportData.ListSecondrySuppo[1].Angle.XinDegree))
                    {
                        if (Math.Abs(SupportData.ListSecondrySuppo[0].Angle.XinDegree).Equals(90))
                        {
                            if (Math.Round(SupportData.ListSecondrySuppo[0].Centroid.X) == Math.Round(SupportData.ListSecondrySuppo[1].Centroid.X) && SupportData.ListSecondrySuppo[0].Centroid.Z > SupportData.ListSecondrySuppo[1].Centroid.Z)
                            {

                                string Orientation = "";
                                if (!AreCentroidsinLine(SupportData, ref Orientation))
                                {

                                    System.Windows.Media.Media3D.Vector3D Vec1 = new System.Windows.Media.Media3D.Vector3D(1, 0, 0);
                                    System.Windows.Media.Media3D.Vector3D Vec2 = new System.Windows.Media.Media3D.Vector3D(0, 1, 0);
                                    System.Windows.Media.Media3D.Vector3D Vec3 = new System.Windows.Media.Media3D.Vector3D(0, 1, 0);
                                    Vec1.X = (SupportData.ListPrimarySuppo[0].Centroid.X - SupportData.ListSecondrySuppo[0].Centroid.X);
                                    Vec1.Y = (SupportData.ListPrimarySuppo[0].Centroid.Y - SupportData.ListSecondrySuppo[0].Centroid.Y);
                                    Vec1.Z = 0;

                                    Vec2.X = (SupportData.ListSecondrySuppo[1].EndPt[0] - SupportData.ListSecondrySuppo[1].StPt[0]);
                                    Vec2.Y = (SupportData.ListSecondrySuppo[1].EndPt[1] - SupportData.ListSecondrySuppo[1].StPt[1]);
                                    Vec2.Z = (SupportData.ListSecondrySuppo[1].EndPt[2] - SupportData.ListSecondrySuppo[1].StPt[2]);

                                    if (Math.Round(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(Vec2, Vec1, Vec3))).Equals(-90))
                                    {
                                        SupportData.SupportType = "Support16";
                                        SupportData.ListSecondrySuppo[0].PartDirection = "Hor";
                                        SupportData.ListSecondrySuppo[1].PartDirection = "Ver";
                                        return true;
                                    }
                                    if (Math.Round(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(Vec2, Vec1, Vec3))).Equals(90))
                                    {
                                        SupportData.SupportType = "Support15";
                                        SupportData.ListSecondrySuppo[0].PartDirection = "Hor";
                                        SupportData.ListSecondrySuppo[1].PartDirection = "Ver";
                                        return true;
                                    }
                                }
                                else
                                {

                                    SupportData.SupportType = "Support2";
                                    SupportData.ListSecondrySuppo[0].PartDirection = "Hor";
                                    SupportData.ListSecondrySuppo[1].PartDirection = "Ver";
                                    return true;
                                }
                            }
                            else
                            {
                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (Math.Abs(SupportData.ListSecondrySuppo[1].Angle.XinDegree).Equals(90))
                        {
                            if (Math.Round(SupportData.ListSecondrySuppo[0].Centroid.X) == Math.Round(SupportData.ListSecondrySuppo[1].Centroid.X) && SupportData.ListSecondrySuppo[0].Centroid.Z < SupportData.ListSecondrySuppo[1].Centroid.Z)
                            {
                                string Orientation = "";
                                if (!AreCentroidsinLine(SupportData, ref Orientation))
                                {

                                    System.Windows.Media.Media3D.Vector3D Vec1 = new System.Windows.Media.Media3D.Vector3D(1, 0, 0);
                                    System.Windows.Media.Media3D.Vector3D Vec2 = new System.Windows.Media.Media3D.Vector3D(0, 1, 0);
                                    System.Windows.Media.Media3D.Vector3D Vec3 = new System.Windows.Media.Media3D.Vector3D(0, 1, 0);
                                    Vec1.X = (SupportData.ListPrimarySuppo[0].Centroid.X - SupportData.ListSecondrySuppo[0].Centroid.X);
                                    Vec1.Y = (SupportData.ListPrimarySuppo[0].Centroid.Y - SupportData.ListSecondrySuppo[0].Centroid.Y);
                                    Vec1.Z = 0;

                                    Vec2.X = (SupportData.ListSecondrySuppo[0].EndPt[0] - SupportData.ListSecondrySuppo[0].StPt[0]);
                                    Vec2.Y = (SupportData.ListSecondrySuppo[0].EndPt[1] - SupportData.ListSecondrySuppo[0].StPt[1]);
                                    Vec2.Z = (SupportData.ListSecondrySuppo[0].EndPt[2] - SupportData.ListSecondrySuppo[0].StPt[2]);

                                    if (Math.Round(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(Vec2, Vec1, Vec3))).Equals(-90))
                                    {
                                        SupportData.SupportType = "Support16";
                                        SupportData.ListSecondrySuppo[1].PartDirection = "Hor";
                                        SupportData.ListSecondrySuppo[0].PartDirection = "Ver";
                                        return true;
                                    }
                                    if (Math.Round(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(Vec2, Vec1, Vec3))).Equals(90))
                                    {
                                        SupportData.SupportType = "Support15";
                                        SupportData.ListSecondrySuppo[1].PartDirection = "Hor";
                                        SupportData.ListSecondrySuppo[0].PartDirection = "Ver";
                                        return true;
                                    }
                                }
                                else
                                {

                                    SupportData.SupportType = "Support2";
                                    SupportData.ListSecondrySuppo[1].PartDirection = "Hor";
                                    SupportData.ListSecondrySuppo[0].PartDirection = "Ver";
                                    return true;
                                }


                            }
                            else
                            {
                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
                else if (Math.Round(Math.Abs((SupportData.ListSecondrySuppo[0].Angle.ZinDegree) - (SupportData.ListSecondrySuppo[1].Angle.ZinDegree))).Equals(180))
                {
                    if (Math.Abs(SupportData.ListSecondrySuppo[0].Angle.XinDegree) > Math.Abs(SupportData.ListSecondrySuppo[1].Angle.XinDegree))
                    {
                        if (Math.Abs(SupportData.ListSecondrySuppo[0].Angle.XinDegree).Equals(90))
                        {
                            if (Math.Round(SupportData.ListSecondrySuppo[0].Centroid.X) == Math.Round(SupportData.ListSecondrySuppo[1].Centroid.X) && SupportData.ListSecondrySuppo[0].Centroid.Z > SupportData.ListSecondrySuppo[1].Centroid.Z)
                            {

                                string Orientation = "";
                                if (!AreCentroidsinLine(SupportData, ref Orientation))
                                {

                                    System.Windows.Media.Media3D.Vector3D Vec1 = new System.Windows.Media.Media3D.Vector3D(1, 0, 0);
                                    System.Windows.Media.Media3D.Vector3D Vec2 = new System.Windows.Media.Media3D.Vector3D(0, 1, 0);
                                    System.Windows.Media.Media3D.Vector3D Vec3 = new System.Windows.Media.Media3D.Vector3D(0, 1, 0);
                                    Vec1.X = (SupportData.ListPrimarySuppo[0].Centroid.X - SupportData.ListSecondrySuppo[0].Centroid.X);
                                    Vec1.Y = (SupportData.ListPrimarySuppo[0].Centroid.Y - SupportData.ListSecondrySuppo[0].Centroid.Y);
                                    Vec1.Z = 0;

                                    Vec2.X = (SupportData.ListSecondrySuppo[1].EndPt[0] - SupportData.ListSecondrySuppo[1].StPt[0]);
                                    Vec2.Y = (SupportData.ListSecondrySuppo[1].EndPt[1] - SupportData.ListSecondrySuppo[1].StPt[1]);
                                    Vec2.Z = (SupportData.ListSecondrySuppo[1].EndPt[2] - SupportData.ListSecondrySuppo[1].StPt[2]);

                                    if (Math.Round(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(Vec2, Vec1, Vec3))).Equals(-90))
                                    {
                                        SupportData.SupportType = "Support16";
                                        SupportData.ListSecondrySuppo[0].PartDirection = "Hor";
                                        SupportData.ListSecondrySuppo[1].PartDirection = "Ver";
                                        return true;
                                    }
                                    if (Math.Round(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(Vec2, Vec1, Vec3))).Equals(90))
                                    {
                                        SupportData.SupportType = "Support15";
                                        SupportData.ListSecondrySuppo[0].PartDirection = "Hor";
                                        SupportData.ListSecondrySuppo[1].PartDirection = "Ver";
                                        return true;
                                    }
                                }
                                else
                                {

                                    SupportData.SupportType = "Support2";
                                    SupportData.ListSecondrySuppo[0].PartDirection = "Hor";
                                    SupportData.ListSecondrySuppo[1].PartDirection = "Ver";
                                    return true;
                                }


                            }
                            else
                            {

                                var MidPt = FindPerpendicularFoot(GetPt3DFromArray(SupportData.ListSecondrySuppo[1].EndPt), SupportData.ListSecondrySuppo[0].StPt, SupportData.ListSecondrySuppo[0].EndPt);

                                double Dist1 = Calculate.DistPoint(GetPt3DFromArray(SupportData.ListSecondrySuppo[0].StPt), GetPt3DFromPoint3D(MidPt));

                                double Dist2 = Calculate.DistPoint(GetPt3DFromArray(SupportData.ListSecondrySuppo[0].EndPt), GetPt3DFromPoint3D(MidPt));

                                double Dist3 = Calculate.DistPoint(GetPt3DFromArray(SupportData.ListSecondrySuppo[0].StPt), GetPt3DFromArray(SupportData.ListSecondrySuppo[0].EndPt));

                                if (Math.Round(Dist3).Equals(Math.Round(Dist2 + Dist1)))
                                {

                                    SupportData.SupportType = "Support24";
                                    SupportData.ListSecondrySuppo[0].PartDirection = "Hor";
                                    SupportData.ListSecondrySuppo[1].PartDirection = "Ver";
                                    return true;
                                }

                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (Math.Abs(SupportData.ListSecondrySuppo[1].Angle.XinDegree).Equals(90))
                        {
                            if (Math.Round(SupportData.ListSecondrySuppo[0].Centroid.X) == Math.Round(SupportData.ListSecondrySuppo[1].Centroid.X) && SupportData.ListSecondrySuppo[0].Centroid.Z < SupportData.ListSecondrySuppo[1].Centroid.Z)
                            {
                                string Orientation = "";
                                if (!AreCentroidsinLine(SupportData, ref Orientation))
                                {

                                    System.Windows.Media.Media3D.Vector3D Vec1 = new System.Windows.Media.Media3D.Vector3D(1, 0, 0);
                                    System.Windows.Media.Media3D.Vector3D Vec2 = new System.Windows.Media.Media3D.Vector3D(0, 1, 0);
                                    System.Windows.Media.Media3D.Vector3D Vec3 = new System.Windows.Media.Media3D.Vector3D(0, 1, 0);
                                    Vec1.X = (SupportData.ListPrimarySuppo[0].Centroid.X - SupportData.ListSecondrySuppo[0].Centroid.X);
                                    Vec1.Y = (SupportData.ListPrimarySuppo[0].Centroid.Y - SupportData.ListSecondrySuppo[0].Centroid.Y);
                                    Vec1.Z = 0;

                                    Vec2.X = (SupportData.ListSecondrySuppo[0].EndPt[0] - SupportData.ListSecondrySuppo[0].StPt[0]);
                                    Vec2.Y = (SupportData.ListSecondrySuppo[0].EndPt[1] - SupportData.ListSecondrySuppo[0].StPt[1]);
                                    Vec2.Z = (SupportData.ListSecondrySuppo[0].EndPt[2] - SupportData.ListSecondrySuppo[0].StPt[2]);

                                    if (Math.Round(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(Vec2, Vec1, Vec3))).Equals(-90))
                                    {
                                        SupportData.SupportType = "Support16";
                                        SupportData.ListSecondrySuppo[1].PartDirection = "Hor";
                                        SupportData.ListSecondrySuppo[0].PartDirection = "Ver";
                                        return true;
                                    }
                                    if (Math.Round(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(Vec2, Vec1, Vec3))).Equals(90))
                                    {
                                        SupportData.SupportType = "Support15";
                                        SupportData.ListSecondrySuppo[1].PartDirection = "Hor";
                                        SupportData.ListSecondrySuppo[0].PartDirection = "Ver";
                                        return true;
                                    }
                                }
                                else
                                {

                                    SupportData.SupportType = "Support2";
                                    SupportData.ListSecondrySuppo[1].PartDirection = "Hor";
                                    SupportData.ListSecondrySuppo[0].PartDirection = "Ver";
                                    return true;
                                }


                            }
                            else
                            {

                                var MidPt = FindPerpendicularFoot(GetPt3DFromArray(SupportData.ListSecondrySuppo[0].EndPt), SupportData.ListSecondrySuppo[1].StPt, SupportData.ListSecondrySuppo[1].EndPt);

                                double Dist1 = Calculate.DistPoint(GetPt3DFromArray(SupportData.ListSecondrySuppo[1].StPt), GetPt3DFromPoint3D(MidPt));

                                double Dist2 = Calculate.DistPoint(GetPt3DFromArray(SupportData.ListSecondrySuppo[1].EndPt), GetPt3DFromPoint3D(MidPt));

                                double Dist3 = Calculate.DistPoint(GetPt3DFromArray(SupportData.ListSecondrySuppo[1].StPt), GetPt3DFromArray(SupportData.ListSecondrySuppo[1].EndPt));

                                if (Math.Round(Dist3).Equals(Math.Round(Dist2 + Dist1)))
                                {

                                    SupportData.SupportType = "Support24";
                                    SupportData.ListSecondrySuppo[0].PartDirection = "Hor";
                                    SupportData.ListSecondrySuppo[1].PartDirection = "Ver";
                                    return true;
                                }

                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
                else if (Math.Round(Math.Abs((SupportData.ListSecondrySuppo[0].Angle.ZinDegree) - (SupportData.ListSecondrySuppo[1].Angle.ZinDegree))).Equals(90))
                {
                    if (Math.Abs(SupportData.ListSecondrySuppo[0].Angle.XinDegree) < Math.Abs(SupportData.ListSecondrySuppo[1].Angle.XinDegree))
                    {
                        if (Math.Abs(SupportData.ListSecondrySuppo[0].Angle.XinDegree).Equals(90))
                        {
                            if (Math.Round(SupportData.ListSecondrySuppo[0].Centroid.Y) == Math.Round(SupportData.ListSecondrySuppo[1].Centroid.Y) && SupportData.ListSecondrySuppo[0].Centroid.Z < SupportData.ListSecondrySuppo[1].Centroid.Z)
                            {

                                string Orientation = "";
                                if (!AreCentroidsinLine(SupportData, ref Orientation))
                                {

                                    System.Windows.Media.Media3D.Vector3D Vec1 = new System.Windows.Media.Media3D.Vector3D(1, 0, 0);
                                    System.Windows.Media.Media3D.Vector3D Vec2 = new System.Windows.Media.Media3D.Vector3D(0, 1, 0);
                                    System.Windows.Media.Media3D.Vector3D Vec3 = new System.Windows.Media.Media3D.Vector3D(0, 1, 0);
                                    Vec1.X = (SupportData.ListPrimarySuppo[0].Centroid.X - SupportData.ListSecondrySuppo[0].Centroid.X);
                                    Vec1.Y = (SupportData.ListPrimarySuppo[0].Centroid.Y - SupportData.ListSecondrySuppo[0].Centroid.Y);
                                    Vec1.Z = 0;

                                    Vec2.X = (SupportData.ListSecondrySuppo[1].EndPt[0] - SupportData.ListSecondrySuppo[1].StPt[0]);
                                    Vec2.Y = (SupportData.ListSecondrySuppo[1].EndPt[1] - SupportData.ListSecondrySuppo[1].StPt[1]);
                                    Vec2.Z = (SupportData.ListSecondrySuppo[1].EndPt[2] - SupportData.ListSecondrySuppo[1].StPt[2]);

                                    if (Math.Round(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(Vec2, Vec1, Vec3))).Equals(-90))
                                    {
                                        SupportData.SupportType = "Support16";
                                        SupportData.ListSecondrySuppo[0].PartDirection = "Hor";
                                        SupportData.ListSecondrySuppo[1].PartDirection = "Ver";
                                        return true;
                                    }
                                    if (Math.Round(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(Vec2, Vec1, Vec3))).Equals(90))
                                    {
                                        SupportData.SupportType = "Support23";
                                        SupportData.ListSecondrySuppo[0].PartDirection = "Hor";
                                        SupportData.ListSecondrySuppo[1].PartDirection = "Ver";
                                        return true;
                                    }
                                }
                                else
                                {

                                    SupportData.SupportType = "Support2";
                                    SupportData.ListSecondrySuppo[0].PartDirection = "Hor";
                                    SupportData.ListSecondrySuppo[1].PartDirection = "Ver";
                                    return true;
                                }


                            }
                            else
                            {

                                var MidPt = FindPerpendicularFoot(GetPt3DFromArray(SupportData.ListSecondrySuppo[1].EndPt), SupportData.ListSecondrySuppo[0].StPt, SupportData.ListSecondrySuppo[0].EndPt);

                                double Dist1 = Calculate.DistPoint(GetPt3DFromArray(SupportData.ListSecondrySuppo[0].StPt), GetPt3DFromPoint3D(MidPt));

                                double Dist2 = Calculate.DistPoint(GetPt3DFromArray(SupportData.ListSecondrySuppo[0].EndPt), GetPt3DFromPoint3D(MidPt));

                                double Dist3 = Calculate.DistPoint(GetPt3DFromArray(SupportData.ListSecondrySuppo[0].StPt), GetPt3DFromArray(SupportData.ListSecondrySuppo[0].EndPt));

                                if (Math.Round(Dist3).Equals(Math.Round(Dist2 + Dist1)))
                                {

                                    SupportData.SupportType = "Support24";
                                    SupportData.ListSecondrySuppo[0].PartDirection = "Hor";
                                    SupportData.ListSecondrySuppo[1].PartDirection = "Ver";
                                    return true;
                                }

                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (Math.Abs(SupportData.ListSecondrySuppo[1].Angle.XinDegree).Equals(90))
                        {
                            if (Math.Round(SupportData.ListSecondrySuppo[0].Centroid.Y) == Math.Round(SupportData.ListSecondrySuppo[1].Centroid.Y) && SupportData.ListSecondrySuppo[0].Centroid.Z > SupportData.ListSecondrySuppo[1].Centroid.Z)
                            {
                                string Orientation = "";
                                if (!AreCentroidsinLine(SupportData, ref Orientation))
                                {

                                    System.Windows.Media.Media3D.Vector3D Vec1 = new System.Windows.Media.Media3D.Vector3D(1, 0, 0);
                                    System.Windows.Media.Media3D.Vector3D Vec2 = new System.Windows.Media.Media3D.Vector3D(0, 1, 0);
                                    System.Windows.Media.Media3D.Vector3D Vec3 = new System.Windows.Media.Media3D.Vector3D(0, 1, 0);
                                    Vec1.X = (SupportData.ListPrimarySuppo[0].Centroid.X - SupportData.ListSecondrySuppo[0].Centroid.X);
                                    Vec1.Y = (SupportData.ListPrimarySuppo[0].Centroid.Y - SupportData.ListSecondrySuppo[0].Centroid.Y);
                                    Vec1.Z = 0;

                                    Vec2.X = (SupportData.ListSecondrySuppo[0].EndPt[0] - SupportData.ListSecondrySuppo[0].StPt[0]);
                                    Vec2.Y = (SupportData.ListSecondrySuppo[0].EndPt[1] - SupportData.ListSecondrySuppo[0].StPt[1]);
                                    Vec2.Z = (SupportData.ListSecondrySuppo[0].EndPt[2] - SupportData.ListSecondrySuppo[0].StPt[2]);

                                    if (Math.Round(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(Vec2, Vec1, Vec3))).Equals(-180))
                                    {
                                        SupportData.SupportType = "Support16";
                                        SupportData.ListSecondrySuppo[1].PartDirection = "Hor";
                                        SupportData.ListSecondrySuppo[0].PartDirection = "Ver";
                                        return true;
                                    }
                                    if (Math.Round(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(Vec2, Vec1, Vec3))).Equals(180))
                                    {
                                        SupportData.SupportType = "Support23";
                                        SupportData.ListSecondrySuppo[1].PartDirection = "Hor";
                                        SupportData.ListSecondrySuppo[0].PartDirection = "Ver";
                                        return true;
                                    }
                                }
                                else
                                {

                                    SupportData.SupportType = "Support2";
                                    SupportData.ListSecondrySuppo[1].PartDirection = "Hor";
                                    SupportData.ListSecondrySuppo[0].PartDirection = "Ver";
                                    return true;
                                }


                            }
                            else
                            {

                                var MidPt = FindPerpendicularFoot(GetPt3DFromArray(SupportData.ListSecondrySuppo[0].EndPt), SupportData.ListSecondrySuppo[1].StPt, SupportData.ListSecondrySuppo[1].EndPt);

                                double Dist1 = Calculate.DistPoint(GetPt3DFromArray(SupportData.ListSecondrySuppo[1].StPt), GetPt3DFromPoint3D(MidPt));

                                double Dist2 = Calculate.DistPoint(GetPt3DFromArray(SupportData.ListSecondrySuppo[1].EndPt), GetPt3DFromPoint3D(MidPt));

                                double Dist3 = Calculate.DistPoint(GetPt3DFromArray(SupportData.ListSecondrySuppo[1].StPt), GetPt3DFromArray(SupportData.ListSecondrySuppo[1].EndPt));

                                if (Math.Round(Dist3).Equals(Math.Round(Dist2 + Dist1)))
                                {

                                    SupportData.SupportType = "Support24";
                                    SupportData.ListSecondrySuppo[0].PartDirection = "Hor";
                                    SupportData.ListSecondrySuppo[1].PartDirection = "Ver";
                                    return true;
                                }

                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }

            if (SupportData.ListSecondrySuppo.Any(Y => Y.Size.ToLower().Contains("angle")))
            {
                string Orientation = "";
                if (AreCentroidsinLine(SupportData, ref Orientation))
                {

                }


            }

            return false;
        }
        /// <summary> 
        /// Here we will add the Key for dictionary 
        /// the key will contain first letter i.e prefix of support type 
        /// Next letters to the prefix will be Number of that support
        /// </summary>
        /// 
        SupportData GetRotationofParts(SupportData SData)
        {
            SupportData SupportAnglesData = new SupportData();

            if (SData.ListConcreteData.Count > 0)
            {
                for (int inx = 0; inx < SData.ListConcreteData.Count; inx++)
                {
                    SData.ListConcreteData[inx].Angle =
                    GetRotationFromVec(SData.ListConcreteData[inx].Directionvec);

                    //SupportAnglesData.ListConcreteData.Add(Data.ListConcreteData[inx]);
                }
            }

            if (SData.ListPrimarySuppo.Count > 0)
            {
                //Will b modified depending upon the reading 
                for (int inx = 0; inx < SData.ListPrimarySuppo.Count; inx++)
                {
                    // DicPartAngles[SData.ListPrimarySuppo[inx].SuppoId] = GetRotationFromVec(SData.ListPrimarySuppo[inx].Directionvec);

                    SData.ListPrimarySuppo[inx].Angle =
                       GetRotationFromVec(SData.ListPrimarySuppo[inx].Directionvec);

                    // DicPartAngles[SData.ListPrimarySuppo[inx].SuppoId] = SData.ListPrimarySuppo[inx];
                }
            }

            if (SData.ListSecondrySuppo.Count > 0)
            {
                for (int inx = 0; inx < SData.ListSecondrySuppo.Count; inx++)
                {
                    // DicPartAngles[SData.ListSecondrySuppo[inx].SuppoId] = GetRotationFromVec(SData.ListSecondrySuppo[inx].Directionvec);

                    SData.ListSecondrySuppo[inx].Angle =
                           GetRotationFromVec(SData.ListSecondrySuppo[inx].Directionvec);

                    //DicPartAngles[SData.ListSecondrySuppo[inx].SuppoId] = SData.ListSecondrySuppo[inx];
                }
            }

            return SData;
        }
        Angles GetRotationFromVec(DirectionVec Dirvec)
        {
            Angles AngleData = new Angles();

            double[,] MatrixArr = GetArrayFromVec(Dirvec);

            double[] AngleinRadian = Calculate.RotM2Eul(MatrixArr, AxisSequence.XYZ, AngleUnit.Radiant);

            // we need to change some logic here because we can use formula directly here of convert degree to work faster
            double[] AngleinDegree = Calculate.RotM2Eul(MatrixArr, AxisSequence.XYZ, AngleUnit.Degrees);

            if (AngleinRadian.GetLength(0) == 3)
            {
                AngleData.XinRadian = AngleinRadian[0];
                AngleData.YinRadian = AngleinRadian[1];
                AngleData.ZinRadian = AngleinRadian[2];
            }

            if (AngleinDegree.GetLength(0) == 3)
            {
                AngleData.XinDegree = AngleinDegree[0];
                AngleData.YinDegree = AngleinDegree[1];
                AngleData.ZinDegree = AngleinDegree[2];
            }

            return AngleData;
        }

        double[,] GetArrayFromVec(DirectionVec Dirvec)
        {
            //Dirvec.
            double[,] ArrayVec = new double[3, 3];

            ArrayVec[0, 0] = Dirvec.XDirVec.X;
            ArrayVec[0, 1] = Dirvec.XDirVec.Y;
            ArrayVec[0, 2] = Dirvec.XDirVec.Z;
            ArrayVec[1, 0] = Dirvec.YDirVec.X;
            ArrayVec[1, 1] = Dirvec.YDirVec.Y;
            ArrayVec[1, 2] = Dirvec.YDirVec.Z;
            ArrayVec[2, 0] = Dirvec.ZDirVec.X;
            ArrayVec[2, 1] = Dirvec.ZDirVec.Y;
            ArrayVec[2, 2] = Dirvec.ZDirVec.Z;

            return ArrayVec;
        }

        bool AreCentroidsinLine(SupportData SData, ref string Orientation)
        {
            List<Pt3D> BPartCentroids = new List<Pt3D>();
            List<Pt3D> PPartCentroids = new List<Pt3D>();
            List<Pt3D> SPartCentroids = new List<Pt3D>();

            BPartCentroids = GetDicCentroidBottomPart(SData);
            PPartCentroids = GetDicCentroidPrimaryPart(SData);
            SPartCentroids = GetDicCentroidSPart(SData);

            return CheckCentroidInLine(BPartCentroids, PPartCentroids, SPartCentroids, ref Orientation);
        }

        bool CheckCentroidInLine(List<Pt3D> BPartCentroids, List<Pt3D> PPartCentroids, List<Pt3D> SPartCentroids, ref string Orientation)
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
                if (Math.Abs(ListXPt[0] - ListXPt[inx]) > 1)
                {
                    AllXAreinLine = false;
                    break;
                }
            }

            for (int inx = 0; inx < ListYPt.Count; inx++)
            {
                if (Math.Abs(ListYPt[0] - ListYPt[inx]) > 1)
                {
                    AllYAreinLine = false;
                    break;
                }
            }

            for (int inx = 0; inx < ListZPt.Count; inx++)
            {
                if (Math.Abs(ListZPt[0] - ListZPt[inx]) > 1)
                {
                    AllZAreinLine = false;
                    break;
                }
            }

            if (AllXAreinLine && AllYAreinLine)
            {
                Orientation = "XY";
                return true;
            }
            else if (AllXAreinLine && AllZAreinLine)
            {
                Orientation = "XZ";
                return true;
            }
            else if (AllYAreinLine && AllZAreinLine)
            {
                Orientation = "YZ";
                return true;
            }

            return false;
        }

        List<double> GetAllPtList(List<Pt3D> BPartCentroids, List<Pt3D> PPartCentroids, List<Pt3D> SPartCentroids, string Coordinate)
        {
            List<double> ListCordinate = new List<double>();
            if (BPartCentroids != null && BPartCentroids.Count > 0)
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

            if (PPartCentroids != null && PPartCentroids.Count > 0)
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

            if (PPartCentroids != null && SPartCentroids.Count > 0)
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
            if (SData.ListConcreteData.Count > 0)
            {
                foreach (var BPart in SData.ListConcreteData)
                {
                    Centroids.Add(BPart.Centroid);
                }
                return Centroids;
            }

            return null;
        }

        double GetQuantityofParts(List<double> ListLenght)
        {
            double sum = 0.0;
            foreach (double value in ListLenght)
            {
                sum += value;
            }

            return sum / 1000;
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
            Create2DViewWithTemp(Document2D);
            //Create2DView(Document2D);
            Database newDb = AcadDatabase.Wblock();
            string Path = System.IO.Path.GetDirectoryName(Filename);
            newDb.SaveAs(Path + "2d.dwg", DwgVersion.Current);
            Document2D.CloseAndSave(Path + "2d.dwg");
        }
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
                // CopyPasteTemplateFile("Temp1", Document2D, 0);

                CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Mtemp", 0, 0, 1);

                //GetTemplate(AcadBlockTableRecord, AcadTransaction, AcadDatabase,0);

                //adding values to c section dictionary
                Csectiondetails.Add("75x40 ", "ISMC 75");
                Csectiondetails.Add("100x50", "ISMC 100 ");
                Csectiondetails.Add("125x65 ", "ISMC 125");
                Csectiondetails.Add("125x66 ", "ISMC 125*");
                Csectiondetails.Add("150x75 ", "ISMC 150");
                Csectiondetails.Add("150x76 ", "ISMC 150*");
                Csectiondetails.Add("175x75 ", "ISMC 175");
                Csectiondetails.Add("175x76 ", "ISMC 175*");
                Csectiondetails.Add("200x75 ", "ISMC 200");
                Csectiondetails.Add("200x76 ", "ISMC 200*");
                Csectiondetails.Add("225x80 ", "ISMC 225");
                Csectiondetails.Add("225x82 ", "ISMC 225*");
                Csectiondetails.Add("250x80 ", "ISMC 250");
                Csectiondetails.Add("250x82 ", "ISMC 250*");
                Csectiondetails.Add("250x83 ", "ISMC 250*");
                Csectiondetails.Add("300x90 ", "ISMC 300");
                Csectiondetails.Add("300x92 ", "ISMC 300*");
                Csectiondetails.Add("300x93 ", "ISMC 300*");
                Csectiondetails.Add("350x100", "ISMC 350");
                Csectiondetails.Add("400x100", "ISMC 400");



                //adding values to L section dictionary


                /*  Lsectiondetails.Add(" 75 40", "ISLC 75");
                  Lsectiondetails.Add(" 100 50", "ISLC 100 ");
                  Lsectiondetails.Add("125 65 ", "ISLC 125");
                  Lsectiondetails.Add("125 65 ", "ISLC(P) 125");
                  Lsectiondetails.Add("150 75 ", "ISLC 150");
                  Lsectiondetails.Add("150 75 ", "ISLC(P) 150");
                  Lsectiondetails.Add("175 75 ", "ISLC 175");
                  Lsectiondetails.Add("200 75 ", "ISLC 200");
                  Lsectiondetails.Add("200 75 ", "ISLC(P) 200");
                  Lsectiondetails.Add("225 90 ", "ISLC 225");
                  Lsectiondetails.Add("250 100", "ISLC 250");
                  Lsectiondetails.Add("300 100", "ISLC 300");
                  Lsectiondetails.Add("300 90 ", "ISLC(P) 300");
                  Lsectiondetails.Add("350 100", "ISLC 350");
                  Lsectiondetails.Add("400 100", "ISLC 400");*/

                ListDimStylesCommand();
                //logic for adding block      

                //9869.9480;                  
                //67542.6980;
                //adding blocks here

                double boxlen = 17299.3016;
                double boxht = 12734.3388;

                double tracex = 619.1209;

                Dictionary<string, double> DicWtData = new Dictionary<string, double>();
                DicWtData["ISMC75"] = 7.14;
                DicWtData["ISMC100"] = 9.56;
                DicWtData["ISMC125"] = 13.1;
                DicWtData["ISMC125*"] = 13.7;
                DicWtData["ISMC150"] = 16.8;
                DicWtData["ISMC150*"] = 17.7;
                DicWtData["ISMC175"] = 19.6;
                DicWtData["ISMC175*"] = 22.7;
                DicWtData["ISMC200"] = 22.3;
                DicWtData["ISMC200*"] = 24.3;
                DicWtData["ISMC225"] = 26.1;
                DicWtData["ISMC225*"] = 30.7;
                DicWtData["ISMC250"] = 30.6;
                DicWtData["ISMC250*"] = 34.2;
                DicWtData["ISMC250**"] = 36.1;
                DicWtData["ISMC300"] = 36.3;
                DicWtData["ISMC300*"] = 41.5;
                DicWtData["ISMC300**"] = 46.2;
                DicWtData["ISMC350"] = 42.7;
                DicWtData["ISMC400"] = 50.1;
                Dictionary<string, double> DicSurfaceAData = new Dictionary<string, double>();


                DicSurfaceAData["ISMC75"] = 0.31;
                DicSurfaceAData["ISMC100"] = 0.40;
                DicSurfaceAData["ISMC125"] = 0.51;
                DicSurfaceAData["ISMC150"] = 0.60;
                DicSurfaceAData["ISMC200"] = 0.70;
                DicSurfaceAData["ISMC225"] = 0.77;
                DicSurfaceAData["ISMC250"] = 0.82;
                DicSurfaceAData["ISMC300"] = 0.96;
                DicSurfaceAData["ISMC400"] = 1.20;


                //for valcuating datum level
                List<double> datum = new List<double>();
                //string suptype = ListCentalSuppoData[0].SupportType;
                for (int i = 0; i < ListCentalSuppoData.Count; i++)
                {
                    foreach (SupporSpecData sp in ListCentalSuppoData[i].ListConcreteData)
                    {
                        datum.Add(sp.Boundingboxmin.Z);
                    }

                    //datum.Add(ListCentalSuppoData[i].ListConcreteData[0].Boundingboxmin.Z);
                }

                if (datum.Count > 0)
                {
                    datum_level = MillimetersToMeters(Math.Round(datum.Min()));
                }



                //for (int j = 0; j <= 1; j++)
                //{
                for (int i = 0; i < ListCentalSuppoData.Count; i++)
                {
                    CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                }


                //comment before given for release
                CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, "Support2");
                CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, "Support3");
                CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, "Support4");
                CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, "Support5");
                CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, "Support6");
                CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, "Support7");
                CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, "Support8");
                CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, "Support9");
                CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, "Support10");
                CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, "Support11");
                CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, "Support13");
                CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, "Support14");
                CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, "Support15");
                CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, "Support16");
                CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, "Support17");
                CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, "Support18");
                CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, "Support19");
                CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, "Support20");
                CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, "Support21");
                CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, "Support22");
                CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, "Support23");
                CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, "Support24");
                CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, "Support25");
                CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, "Support26");
                CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, "Support27");
                CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, "Support28");
                CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, "Support29");



                Dictionary<string, List<double>> ListBomData = new Dictionary<string, List<double>>();

                foreach (var SData in ListCentalSuppoData)
                {
                    foreach (var SType in SData.ListSecondrySuppo)
                    {
                        if (SType.SupportName != null && SType.SupportName.ToLower().Equals("plate"))
                        {
                            //GetLBHFromFace(SType.ListfaceData);
                        }
                        else if (SType.Size != null && SType.Size.ToLower().Contains("web"))
                        {
                            string Size = GettheISMC(SType.Size);
                            if (Size == "")
                            {
                                Size = "ISMC" + CSECTIONSIZE(SType.Size);
                            }

                            List<double> Temp = new List<double>();
                            double Dist = Calculate.DistPoint(GetPt3DFromArray(SType.StPt), GetPt3DFromArray(SType.EndPt));

                            if (ListBomData.ContainsKey(Size))
                            {
                                Temp = ListBomData[Size];
                                Temp.Add(Dist);

                                ListBomData[Size] = Temp;
                            }
                            else
                            {
                                Temp.Add(Dist);

                                ListBomData[Size] = Temp;
                            }
                        }
                    }
                }

                Table BomTable = new Table();

                BomTable.SetSize(ListBomData.Count + 3, 8);
                string[,] str = new string[ListBomData.Count + 3, 8];

                str[0, 0] = "BOM";
                str[1, 0] = "SR. No.";
                str[1, 1] = "ITEM DESCRIPTION";
                str[1, 2] = "MATERIAL";
                str[1, 3] = "QTY";
                str[1, 4] = "WEIGHT(KG)";
                str[1, 5] = "PAINTING AREA (METER sq.)";
                str[1, 6] = "Remark";

                Autodesk.AutoCAD.DatabaseServices.CellRange rowFirst = Autodesk.AutoCAD.DatabaseServices.CellRange.Create(BomTable, 0, 0, 0, 7);
                BomTable.MergeCells(rowFirst);

                for (int iCo1 = 0; iCo1 < BomTable.Rows.Count; iCo1++)
                    BomTable.Rows[iCo1].Height = 400;

                for (int iCo1 = 0; iCo1 < 8; iCo1++)
                {
                    if (iCo1 == 0)
                    {
                        BomTable.Columns[iCo1].Width = 1000;
                    }
                    else if (iCo1 == 1 || iCo1 == 2)
                    {
                        BomTable.Columns[iCo1].Width = 2500;
                    }
                    else if (iCo1 == 3 || iCo1 == 4 || iCo1 == 5)
                    {
                        BomTable.Columns[iCo1].Width = 1500;
                    }
                    else if (iCo1 == 7)
                    {
                        BomTable.Columns[iCo1].Width = 1;
                    }
                    else
                    {
                        BomTable.Columns[iCo1].Width = 1000;
                    }
                }

                double Weight = 0.0;
                double SurfaceArea = 0.0;

                BomTable.Cells[0, 0].ContentColor = Autodesk.AutoCAD.Colors.Color.FromColor(System.Drawing.Color.Black);
                BomTable.Cells[0, 0].BackgroundColor = Autodesk.AutoCAD.Colors.Color.FromColor(System.Drawing.Color.Yellow);
                BomTable.SetContentColor(Autodesk.AutoCAD.Colors.Color.FromColor(System.Drawing.Color.LightGray), 1);

                int iNo = 2;

                for (int inx = 0; inx < ListBomData.Count; inx++)
                {
                    for (int iCount1 = 0; iCount1 < 7; iCount1++)
                    {
                        if (iCount1 == 0)
                            str[iNo, iCount1] = (inx + 1).ToString();
                        if (iCount1 == 1)
                            str[iNo, iCount1] = ListBomData.ElementAt(inx).Key;
                        if (iCount1 == 2)
                            str[iNo, iCount1] = "IS:2062 Gr.E250A";
                        if (iCount1 == 3)
                            str[iNo, iCount1] = (Math.Round(GetQuantityofParts(ListBomData.ElementAt(inx).Value), 1)).ToString() + " M";
                        if (iCount1 == 4)
                            str[iNo, iCount1] = (Math.Round(GetQuantityofParts(ListBomData.ElementAt(inx).Value) * DicWtData[ListBomData.ElementAt(inx).Key], 2)).ToString() + " Kg";
                        if (iCount1 == 5)
                            str[iNo, iCount1] = (Math.Round((DicSurfaceAData.ContainsKey(ListBomData.ElementAt(inx).Key) ? DicSurfaceAData[ListBomData.ElementAt(inx).Key] : 0) * GetQuantityofParts(ListBomData.ElementAt(inx).Value), 1)).ToString() + "  m2/m";

                        if (iCount1 == 6)
                            str[iNo, iCount1] = " ";
                    }
                    Weight = Weight + Math.Round(GetQuantityofParts(ListBomData.ElementAt(inx).Value) * DicWtData[ListBomData.ElementAt(inx).Key], 2);

                    SurfaceArea = SurfaceArea + Math.Round((DicSurfaceAData.ContainsKey(ListBomData.ElementAt(inx).Key) ? DicSurfaceAData[ListBomData.ElementAt(inx).Key] : 0) * GetQuantityofParts(ListBomData.ElementAt(inx).Value), 1);
                    iNo++;
                }

                for (int i = 0; i < BomTable.Rows.Count; i++)
                {
                    if (i == BomTable.Rows.Count - 1)
                    {
                        BomTable.Cells[i, 0].TextHeight = 140;
                        BomTable.Cells[i, 0].Contents[0].TextString = "{\\fArial;" + "Total" + "}";
                        BomTable.Cells[i, 0].Alignment = CellAlignment.MiddleCenter;

                        BomTable.Cells[i, 4].TextHeight = 140;
                        BomTable.Cells[i, 4].Contents[0].TextString = "{\\fArial;" + Weight.ToString() + "}";
                        BomTable.Cells[i, 4].Alignment = CellAlignment.MiddleCenter;

                        BomTable.Cells[i, 5].TextHeight = 140;
                        BomTable.Cells[i, 5].Contents[0].TextString = "{\\fArial;" + SurfaceArea.ToString() + "}";
                        BomTable.Cells[i, 5].Alignment = CellAlignment.MiddleCenter;
                    }
                    else
                    {
                        for (int j = 0; j < 8; j++)
                        {
                            BomTable.Cells[i, j].TextHeight = 140;
                            BomTable.Cells[i, j].Contents[0].TextString = "{\\fArial;" + str[i, j] + "}";
                            BomTable.Cells[i, j].Alignment = CellAlignment.MiddleCenter;
                        }
                    }
                }

                BomTable.DeleteColumns(BomTable.Columns.Count - 1, 1);
                BomTable.Position = new Point3d(tracex, -BomTable.Height + spaceY, 0);

                BomTable.GenerateLayout();
                AcadBlockTableRecord.AppendEntity(BomTable);
                AcadTransaction.AddNewlyCreatedDBObject(BomTable, true);


                //Point3d startPoint = new Point3d(0, 0, 0);
                //Point3d endPoint = new Point3d(50, 50, 0);
                //Point3d landingPoint = new Point3d(100, 50, 0);
                //DrawLeader(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Defalut text", startPoint, endPoint, landingPoint);
                //startPoint = new Point3d(0, 0, 0);
                //endPoint = new Point3d(500, 500, 0);
                //DrawLeader(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Defalut text", startPoint, endPoint);

                //}


                /*List<SupporSpecData> PrimarySupport = firstSupport.ListPrimarySuppo;
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

                //CreateLeaderfromfile(AcadBlockTableRecord, AcadTransaction, AcadDatabase);


                CreateSingleLeadrwidtxt(AcadBlockTableRecord, AcadTransaction, AcadDatabase);

                //SupporSpecData Sesupport = SecondSupport.FirstOrDefault();*/
                AddDimStyleToDimensions();
                AcadTransaction.Commit();
            }
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


            }
            newline.Linetype = sLineTypName;
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


        [Obsolete]
        public void CreateFullBlock(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, ref double tracex, double boxlen, double boxht, ref double spaceY, Document Document2D, string SupType, [Optional] int i)
        {



            info.Clear();
            info.Add(Defination.Prim_Radius, 0);
            info[Defination.Concrete_l] = 3000;
            info[Defination.Concrete_b] = 1000;

            if (SupType == SupportType.Support2.ToString())
            {
                boxlen = 17299.3016;
                boxht = 12734.3388;
                if (tracex >= spaceX - boxlen)
                {
                    spaceY -= boxht;
                    tracex = tempX - 101659.6570 + 619.1209;
                }
                if (spaceY > boxht)
                {
                    DRS2(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                }
                else
                {

                    spaceY = 71075.0829;
                    tracex = tempX + 10000 + 619.1209;//gaps
                                                      // gets the template
                                                      // GetTemplate(AcadBlockTableRecord, AcadTransaction, AcadDatabase,tracex- 619.1209);

                    // CopyPasteTemplateFile("Temp1", Document2D, tracex - 619.1209);
                    CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Mtemp", tracex - 619.1209, 0, 1);

                    tempX += 101659.6570 + 10000;
                    spaceX = tempX - 19068.9248;
                    DRS2(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                }
            }

            else if (SupType == SupportType.SL_Tyep.ToString())
            {
                boxlen = 17299.3016;
                boxht = 12734.3388;
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
                    Point3d pt1 = new Point3d(tracex + boxlen, spaceY + 619.1209, 0);
                    Point3d Pt2 = new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0);
                    Line line = new Line(pt1, Pt2);
                    AcadBlockTableRecord.AppendEntity(line);
                    line.Color = Color.FromColorIndex(ColorMethod.ByAci, 171);
                    AcadTransaction.AddNewlyCreatedDBObject(line, true);


                    //hori line
                    Point3d pt11 = new Point3d(tracex, spaceY - boxht + 619.1209, 0);
                    Point3d Pt21 = new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0);
                    Line line1 = new Line(pt11, Pt21);
                    AcadBlockTableRecord.AppendEntity(line1);
                    line1.Color = Color.FromColorIndex(ColorMethod.ByAci, 171);
                    AcadTransaction.AddNewlyCreatedDBObject(line1, true);

                    //height = 3000;
                    //length = 1000;

                    //  FixCreateBottomSupportTopType2(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY, height, length);
                    FixCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY, "Left");
                    FixCreateSecondarySupportTop(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY);
                    FixCreatePrimarySupportwithvertex(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY, 801.5625);

                    tracex += boxlen;
                }
                else
                {

                    spaceY = 71075.0829;
                    tracex = tempX + 10000 + 619.1209;//gaps
                                                      // gets the template
                                                      // GetTemplate(AcadBlockTableRecord, AcadTransaction, AcadDatabase,tracex- 619.1209);

                    //CopyPasteTemplateFile("Temp1", Document2D, tracex - 619.1209);
                    CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Mtemp", tracex - 619.1209, 0, 1);
                    tempX += 101659.6570 + 10000;
                    spaceX = tempX - 19068.9248;
                }
            }

            else if (SupType == SupportType.SR_Tyep.ToString())
            {
                boxlen = 17299.3016;
                boxht = 12734.3388;
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
                    Point3d pt1 = new Point3d(tracex + boxlen, spaceY + 619.1209, 0);
                    Point3d Pt2 = new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0);
                    Line line = new Line(pt1, Pt2);
                    AcadBlockTableRecord.AppendEntity(line);
                    line.Color = Color.FromColorIndex(ColorMethod.ByAci, 171);
                    AcadTransaction.AddNewlyCreatedDBObject(line, true);


                    //hori line
                    Point3d pt11 = new Point3d(tracex, spaceY - boxht + 619.1209, 0);
                    Point3d Pt21 = new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0);
                    Line line1 = new Line(pt11, Pt21);
                    AcadBlockTableRecord.AppendEntity(line1);
                    line1.Color = Color.FromColorIndex(ColorMethod.ByAci, 171);
                    AcadTransaction.AddNewlyCreatedDBObject(line1, true);

                    // height = 3000;
                    // length = 1000;

                    // FixCreateBottomSupportTopType2(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY, height, length);
                    FixCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY, "Right");
                    FixCreateSecondarySupportTop(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY);
                    FixCreatePrimarySupportwithvertex(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY, 801.5625);

                    tracex += boxlen;
                }
                else
                {

                    spaceY = 71075.0829;
                    tracex = tempX + 10000 + 619.1209;//gaps
                                                      // gets the template
                                                      // GetTemplate(AcadBlockTableRecord, AcadTransaction, AcadDatabase,tracex- 619.1209);


                    // CopyPasteTemplateFile("Temp1", Document2D, tracex - 619.1209);
                    CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Mtemp", tracex - 619.1209, 0, 1);
                    tempX += 101659.6570 + 10000;
                    spaceX = tempX - 19068.9248;
                }
            }

            else if (SupType == SupportType.Support3.ToString())
            {
                boxlen = 17299.3016;
                boxht = 12734.3388;
                if (tracex >= spaceX - boxlen)
                {
                    spaceY -= boxht;
                    tracex = tempX - 101659.6570 + 619.1209;
                }
                if (spaceY > boxht)
                {
                    DRS3(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                }
                else
                {

                    spaceY = 71075.0829;
                    tracex = tempX + 10000 + 619.1209;//gaps
                                                      // gets the template
                                                      // GetTemplate(AcadBlockTableRecord, AcadTransaction, AcadDatabase,tracex- 619.1209);


                    //CopyPasteTemplateFile("Temp1", Document2D, tracex - 619.1209);
                    CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Mtemp", tracex - 619.1209, 0, 1);
                    tempX += 101659.6570 + 10000;
                    spaceX = tempX - 19068.9248;
                    DRS3(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                }
            }

            else if (SupType == SupportType.Support4.ToString())
            {
                boxlen = 17299.3016;
                boxht = 12734.3388;
                if (tracex >= spaceX - boxlen)
                {
                    spaceY -= boxht;
                    tracex = tempX - 101659.6570 + 619.1209;
                }
                if (spaceY > boxht)
                {
                    DRS4(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                }
                else
                {

                    spaceY = 71075.0829;
                    tracex = tempX + 10000 + 619.1209;//gaps
                                                      // gets the template
                                                      // GetTemplate(AcadBlockTableRecord, AcadTransaction, AcadDatabase,tracex- 619.1209);


                    //CopyPasteTemplateFile("Temp1", Document2D, tracex - 619.1209);
                    CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Mtemp", tracex - 619.1209, 0, 1);
                    tempX += 101659.6570 + 10000;
                    spaceX = tempX - 19068.9248;
                    DRS4(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                }
            }

            else if (SupType == SupportType.Support5.ToString())
            {
                boxlen = 17299.3016;
                boxht = 12734.3388;
                if (tracex >= spaceX - boxlen)
                {
                    spaceY -= boxht;
                    tracex = tempX - 101659.6570 + 619.1209;
                }
                if (spaceY > boxht)
                {
                    DRS5(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                }
                else
                {

                    spaceY = 71075.0829;
                    tracex = tempX + 10000 + 619.1209;//gaps
                                                      // gets the template
                                                      // GetTemplate(AcadBlockTableRecord, AcadTransaction, AcadDatabase,tracex- 619.1209);

                    // CopyPasteTemplateFile("Temp1", Document2D, tracex - 619.1209);
                    CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Mtemp", tracex - 619.1209, 0, 1);
                    tempX += 101659.6570 + 10000;
                    spaceX = tempX - 19068.9248;
                    DRS5(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                }
            }

            else if (SupType == SupportType.Support6.ToString())
            {
                boxlen = 17299.3016;
                boxht = 12734.3388;
                if (tracex >= spaceX - boxlen)
                {
                    spaceY -= boxht;
                    tracex = tempX - 101659.6570 + 619.1209;
                }
                if (spaceY > boxht)
                {
                    DRS6(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                }
                else
                {

                    spaceY = 71075.0829;
                    tracex = tempX + 10000 + 619.1209;//gaps
                                                      // gets the template
                                                      // GetTemplate(AcadBlockTableRecord, AcadTransaction, AcadDatabase,tracex- 619.1209);

                    // CopyPasteTemplateFile("Temp1", Document2D, tracex - 619.1209);
                    CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Mtemp", tracex - 619.1209, 0, 1);
                    tempX += 101659.6570 + 10000;
                    spaceX = tempX - 19068.9248;
                    DRS6(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                }
            }

            else if (SupType == SupportType.Support7.ToString())
            {
                boxlen = 17299.3016;
                boxht = 12734.3388;
                if (tracex >= spaceX - boxlen)
                {
                    spaceY -= boxht;
                    tracex = tempX - 101659.6570 + 619.1209;
                }
                if (spaceY > boxht)
                {
                    DRS7(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                }
                else
                {

                    spaceY = 71075.0829;
                    tracex = tempX + 10000 + 619.1209;//gaps
                                                      // gets the template
                                                      // GetTemplate(AcadBlockTableRecord, AcadTransaction, AcadDatabase,tracex- 619.1209);

                    //CopyPasteTemplateFile("Temp1", Document2D, tracex - 619.1209);
                    CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Mtemp", tracex - 619.1209, 0, 1);
                    tempX += 101659.6570 + 10000;
                    spaceX = tempX - 19068.9248;
                    DRS7(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                }
            }

            else if (SupType == SupportType.Support8.ToString())
            {
                boxlen = 17299.3016;
                boxht = 12734.3388;
                if (tracex >= spaceX - boxlen)
                {
                    spaceY -= boxht;
                    tracex = tempX - 101659.6570 + 619.1209;
                }
                if (spaceY > boxht)
                {
                    DRS8(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                }
                else
                {

                    spaceY = 71075.0829;
                    tracex = tempX + 10000 + 619.1209;//gaps
                                                      // gets the template
                                                      // GetTemplate(AcadBlockTableRecord, AcadTransaction, AcadDatabase,tracex- 619.1209);

                    // CopyPasteTemplateFile("Temp1", Document2D, tracex - 619.1209);
                    CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Mtemp", tracex - 619.1209, 0, 1);
                    tempX += 101659.6570 + 10000;
                    spaceX = tempX - 19068.9248;
                    DRS8(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                }
            }


            else if (SupType == SupportType.Support9.ToString())
            {
                boxlen = 17299.3016;
                boxht = 12734.3388;
                if (tracex >= spaceX - boxlen)
                {
                    spaceY -= boxht;
                    tracex = tempX - 101659.6570 + 619.1209;
                }
                if (spaceY > boxht)
                {
                    DRS9(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                }
                else
                {

                    spaceY = 71075.0829;
                    tracex = tempX + 10000 + 619.1209;//gaps
                                                      // gets the template
                                                      // GetTemplate(AcadBlockTableRecord, AcadTransaction, AcadDatabase,tracex- 619.1209);

                    //CopyPasteTemplateFile("Temp1", Document2D, tracex - 619.1209);
                    CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Mtemp", tracex - 619.1209, 0, 1);
                    tempX += 101659.6570 + 10000;
                    spaceX = tempX - 19068.9248;
                    DRS9(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                }
            }

            else if (SupType == SupportType.Support10.ToString())
            {
                boxlen = 17299.3016;
                boxht = 12734.3388;
                if (tracex >= spaceX - boxlen)
                {
                    spaceY -= boxht;
                    tracex = tempX - 101659.6570 + 619.1209;
                }
                if (spaceY > boxht)
                {
                    DRS10(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                }
                else
                {

                    spaceY = 71075.0829;
                    tracex = tempX + 10000 + 619.1209;//gaps
                                                      // gets the template
                                                      // GetTemplate(AcadBlockTableRecord, AcadTransaction, AcadDatabase,tracex- 619.1209);

                    //CopyPasteTemplateFile("Temp1", Document2D, tracex - 619.1209);
                    CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Mtemp", tracex - 619.1209, 0, 1);
                    tempX += 101659.6570 + 10000;
                    spaceX = tempX - 19068.9248;
                    DRS10(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                }
            }


            else if (SupType == SupportType.Support11.ToString())
            {
                boxlen = 17299.3016;
                boxht = 12734.3388;
                if (tracex >= spaceX - boxlen)
                {
                    spaceY -= boxht;
                    tracex = tempX - 101659.6570 + 619.1209;
                }
                if (spaceY > boxht)
                {
                    DRS11(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                }
                else
                {

                    spaceY = 71075.0829;
                    tracex = tempX + 10000 + 619.1209;//gaps
                                                      // gets the template
                                                      // GetTemplate(AcadBlockTableRecord, AcadTransaction, AcadDatabase,tracex- 619.1209);

                    //CopyPasteTemplateFile("Temp1", Document2D, tracex - 619.1209);
                    CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Mtemp", tracex - 619.1209, 0, 1);
                    tempX += 101659.6570 + 10000;
                    spaceX = tempX - 19068.9248;
                    DRS11(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                }
            }

            else if (SupType == SupportType.Support13.ToString())
            {
                boxlen = 17299.3016;
                boxht = 12734.3388;
                if (tracex >= spaceX - boxlen)
                {
                    spaceY -= boxht;
                    tracex = tempX - 101659.6570 + 619.1209;
                }
                if (spaceY > boxht)
                {
                    try
                    {
                        DRS13(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                    }
                    catch (Exception)
                    {

                    }
                }
                else
                {

                    spaceY = 71075.0829;
                    tracex = tempX + 10000 + 619.1209;//gaps
                                                      // gets the template
                                                      // GetTemplate(AcadBlockTableRecord, AcadTransaction, AcadDatabase,tracex- 619.1209);

                    //CopyPasteTemplateFile("Temp1", Document2D, tracex - 619.1209);
                    CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Mtemp", tracex - 619.1209, 0, 1);
                    tempX += 101659.6570 + 10000;
                    spaceX = tempX - 19068.9248;
                    try
                    {
                        DRS13(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                    }
                    catch (Exception)
                    {

                    }
                }
            }
            else if (SupType == SupportType.Support14.ToString())
            {
                boxlen = 17299.3016;
                boxht = 12734.3388;
                if (tracex >= spaceX - boxlen)
                {
                    spaceY -= boxht;
                    tracex = tempX - 101659.6570 + 619.1209;
                }
                if (spaceY > boxht)
                {
                    DRS14(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                }
                else
                {

                    spaceY = 71075.0829;
                    tracex = tempX + 10000 + 619.1209;//gaps
                                                      // gets the template
                                                      // GetTemplate(AcadBlockTableRecord, AcadTransaction, AcadDatabase,tracex- 619.1209);

                    //CopyPasteTemplateFile("Temp1", Document2D, tracex - 619.1209);
                    CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Mtemp", tracex - 619.1209, 0, 1);
                    tempX += 101659.6570 + 10000;
                    spaceX = tempX - 19068.9248;
                    DRS14(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                }
            }

            else if (SupType == SupportType.Support15.ToString())
            {
                boxlen = 17299.3016;
                boxht = 12734.3388;
                if (tracex >= spaceX - boxlen)
                {
                    spaceY -= boxht;
                    tracex = tempX - 101659.6570 + 619.1209;
                }
                if (spaceY > boxht)
                {
                    DRS15(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                }
                else
                {

                    spaceY = 71075.0829;
                    tracex = tempX + 10000 + 619.1209;//gaps
                                                      // gets the template
                                                      // GetTemplate(AcadBlockTableRecord, AcadTransaction, AcadDatabase,tracex- 619.1209);

                    //CopyPasteTemplateFile("Temp1", Document2D, tracex - 619.1209);
                    CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Mtemp", tracex - 619.1209, 0, 1);
                    tempX += 101659.6570 + 10000;
                    spaceX = tempX - 19068.9248;
                    DRS15(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                }
            }

            else if (SupType == SupportType.Support16.ToString())
            {
                boxlen = 17299.3016;
                boxht = 12734.3388;
                if (tracex >= spaceX - boxlen)
                {
                    spaceY -= boxht;
                    tracex = tempX - 101659.6570 + 619.1209;
                }
                if (spaceY > boxht)
                {
                    DRS16(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                }
                else
                {

                    spaceY = 71075.0829;
                    tracex = tempX + 10000 + 619.1209;//gaps
                                                      // gets the template
                                                      // GetTemplate(AcadBlockTableRecord, AcadTransaction, AcadDatabase,tracex- 619.1209);

                    //CopyPasteTemplateFile("Temp1", Document2D, tracex - 619.1209);
                    CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Mtemp", tracex - 619.1209, 0, 1);
                    tempX += 101659.6570 + 10000;
                    spaceX = tempX - 19068.9248;
                    DRS16(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                }
            }

            else if (SupType == SupportType.Support17.ToString())
            {
                boxlen = 17299.3016;
                boxht = 12734.3388;
                if (tracex >= spaceX - boxlen)
                {
                    spaceY -= boxht;
                    tracex = tempX - 101659.6570 + 619.1209;
                }
                if (spaceY > boxht)
                {
                    DRS17(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                }
                else
                {

                    spaceY = 71075.0829;
                    tracex = tempX + 10000 + 619.1209;//gaps
                                                      // gets the template
                                                      // GetTemplate(AcadBlockTableRecord, AcadTransaction, AcadDatabase,tracex- 619.1209);

                    //CopyPasteTemplateFile("Temp1", Document2D, tracex - 619.1209);
                    CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Mtemp", tracex - 619.1209, 0, 1);
                    tempX += 101659.6570 + 10000;
                    spaceX = tempX - 19068.9248;
                    DRS17(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                }
            }

            else if (SupType == SupportType.Support18.ToString())
            {
                boxlen = 17299.3016;
                boxht = 12734.3388;
                if (tracex >= spaceX - boxlen)
                {
                    spaceY -= boxht;
                    tracex = tempX - 101659.6570 + 619.1209;
                }
                if (spaceY > boxht)
                {
                    DRS18(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                }
                else
                {

                    spaceY = 71075.0829;
                    tracex = tempX + 10000 + 619.1209;//gaps
                                                      // gets the template
                                                      // GetTemplate(AcadBlockTableRecord, AcadTransaction, AcadDatabase,tracex- 619.1209);

                    //CopyPasteTemplateFile("Temp1", Document2D, tracex - 619.1209);
                    CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Mtemp", tracex - 619.1209, 0, 1);
                    tempX += 101659.6570 + 10000;
                    spaceX = tempX - 19068.9248;
                    DRS18(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                }
            }

            else if (SupType == SupportType.Support19.ToString())
            {
                boxlen = 17299.3016;
                boxht = 12734.3388;
                if (tracex >= spaceX - boxlen)
                {
                    spaceY -= boxht;
                    tracex = tempX - 101659.6570 + 619.1209;
                }
                if (spaceY > boxht)
                {
                    DRS19(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                }
                else
                {

                    spaceY = 71075.0829;
                    tracex = tempX + 10000 + 619.1209;//gaps
                                                      // gets the template
                                                      // GetTemplate(AcadBlockTableRecord, AcadTransaction, AcadDatabase,tracex- 619.1209);

                    //CopyPasteTemplateFile("Temp1", Document2D, tracex - 619.1209);
                    CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Mtemp", tracex - 619.1209, 0, 1);
                    tempX += 101659.6570 + 10000;
                    spaceX = tempX - 19068.9248;
                    DRS19(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                }
            }

            else if (SupType == SupportType.Support20.ToString())
            {
                boxlen = 17299.3016;
                boxht = 12734.3388;
                if (tracex >= spaceX - boxlen)
                {
                    spaceY -= boxht;
                    tracex = tempX - 101659.6570 + 619.1209;
                }
                if (spaceY > boxht)
                {
                    DRS20(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                }
                else
                {

                    spaceY = 71075.0829;
                    tracex = tempX + 10000 + 619.1209;//gaps
                                                      // gets the template
                                                      // GetTemplate(AcadBlockTableRecord, AcadTransaction, AcadDatabase,tracex- 619.1209);

                    //CopyPasteTemplateFile("Temp1", Document2D, tracex - 619.1209);
                    CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Mtemp", tracex - 619.1209, 0, 1);
                    tempX += 101659.6570 + 10000;
                    spaceX = tempX - 19068.9248;
                    DRS20(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                }
            }

            else if (SupType == SupportType.Support21.ToString())
            {
                boxlen = 17299.3016;
                boxht = 12734.3388;
                if (tracex >= spaceX - boxlen)
                {
                    spaceY -= boxht;
                    tracex = tempX - 101659.6570 + 619.1209;
                }
                if (spaceY > boxht)
                {
                    DRS21(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                }
                else
                {

                    spaceY = 71075.0829;
                    tracex = tempX + 10000 + 619.1209;//gaps
                                                      // gets the template
                                                      // GetTemplate(AcadBlockTableRecord, AcadTransaction, AcadDatabase,tracex- 619.1209);

                    //CopyPasteTemplateFile("Temp1", Document2D, tracex - 619.1209);
                    CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Mtemp", tracex - 619.1209, 0, 1);
                    tempX += 101659.6570 + 10000;
                    spaceX = tempX - 19068.9248;
                    DRS21(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                }
            }

            else if (SupType == SupportType.Support22.ToString())
            {
                boxlen = 17299.3016;
                boxht = 12734.3388;
                if (tracex >= spaceX - boxlen)
                {
                    spaceY -= boxht;
                    tracex = tempX - 101659.6570 + 619.1209;
                }
                if (spaceY > boxht)
                {
                    DRS22(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                }
                else
                {

                    spaceY = 71075.0829;
                    tracex = tempX + 10000 + 619.1209;//gaps
                                                      // gets the template
                                                      // GetTemplate(AcadBlockTableRecord, AcadTransaction, AcadDatabase,tracex- 619.1209);

                    //CopyPasteTemplateFile("Temp1", Document2D, tracex - 619.1209);
                    CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Mtemp", tracex - 619.1209, 0, 1);
                    tempX += 101659.6570 + 10000;
                    spaceX = tempX - 19068.9248;
                    DRS22(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                }
            }

            else if (SupType == SupportType.Support23.ToString())
            {
                boxlen = 17299.3016;
                boxht = 12734.3388;
                if (tracex >= spaceX - boxlen)
                {
                    spaceY -= boxht;
                    tracex = tempX - 101659.6570 + 619.1209;
                }
                if (spaceY > boxht)
                {
                    DRS23(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                }
                else
                {

                    spaceY = 71075.0829;
                    tracex = tempX + 10000 + 619.1209;//gaps
                                                      // gets the template
                                                      // GetTemplate(AcadBlockTableRecord, AcadTransaction, AcadDatabase,tracex- 619.1209);

                    //CopyPasteTemplateFile("Temp1", Document2D, tracex - 619.1209);
                    CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Mtemp", tracex - 619.1209, 0, 1);
                    tempX += 101659.6570 + 10000;
                    spaceX = tempX - 19068.9248;
                    DRS23(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                }
            }

            else if (SupType == SupportType.Support24.ToString())
            {
                boxlen = 17299.3016;
                boxht = 12734.3388;
                if (tracex >= spaceX - boxlen)
                {
                    spaceY -= boxht;
                    tracex = tempX - 101659.6570 + 619.1209;
                }
                if (spaceY > boxht)
                {
                    DRS24(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                }
                else
                {

                    spaceY = 71075.0829;
                    tracex = tempX + 10000 + 619.1209;//gaps
                                                      // gets the template
                                                      // GetTemplate(AcadBlockTableRecord, AcadTransaction, AcadDatabase,tracex- 619.1209);

                    //CopyPasteTemplateFile("Temp1", Document2D, tracex - 619.1209);
                    CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Mtemp", tracex - 619.1209, 0, 1);
                    tempX += 101659.6570 + 10000;
                    spaceX = tempX - 19068.9248;
                    DRS24(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                }
            }

            else if (SupType == SupportType.Support25.ToString())
            {
                boxlen = 17299.3016;
                boxht = 12734.3388;
                if (tracex >= spaceX - boxlen)
                {
                    spaceY -= boxht;
                    tracex = tempX - 101659.6570 + 619.1209;
                }
                if (spaceY > boxht)
                {
                    DRS25(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                }
                else
                {

                    spaceY = 71075.0829;
                    tracex = tempX + 10000 + 619.1209;//gaps
                                                      // gets the template
                                                      // GetTemplate(AcadBlockTableRecord, AcadTransaction, AcadDatabase,tracex- 619.1209);

                    //CopyPasteTemplateFile("Temp1", Document2D, tracex - 619.1209);
                    CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Mtemp", tracex - 619.1209, 0, 1);
                    tempX += 101659.6570 + 10000;
                    spaceX = tempX - 19068.9248;
                    DRS25(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                }
            }
            else if (SupType == SupportType.Support26.ToString())
            {
                boxlen = 17299.3016;
                boxht = 12734.3388;
                if (tracex >= spaceX - boxlen)
                {
                    spaceY -= boxht;
                    tracex = tempX - 101659.6570 + 619.1209;
                }
                if (spaceY > boxht)
                {
                    DRS26(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                }
                else
                {

                    spaceY = 71075.0829;
                    tracex = tempX + 10000 + 619.1209;//gaps
                                                      // gets the template
                                                      // GetTemplate(AcadBlockTableRecord, AcadTransaction, AcadDatabase,tracex- 619.1209);

                    //CopyPasteTemplateFile("Temp1", Document2D, tracex - 619.1209);
                    CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Mtemp", tracex - 619.1209, 0, 1);
                    tempX += 101659.6570 + 10000;
                    spaceX = tempX - 19068.9248;
                    DRS26(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                }
            }

            else if (SupType == SupportType.Support27.ToString())
            {
                boxlen = 17299.3016;
                boxht = 12734.3388;
                if (tracex >= spaceX - boxlen)
                {
                    spaceY -= boxht;
                    tracex = tempX - 101659.6570 + 619.1209;
                }
                if (spaceY > boxht)
                {
                    DRS27(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                }
                else
                {

                    spaceY = 71075.0829;
                    tracex = tempX + 10000 + 619.1209;//gaps
                                                      // gets the template
                                                      // GetTemplate(AcadBlockTableRecord, AcadTransaction, AcadDatabase,tracex- 619.1209);

                    //CopyPasteTemplateFile("Temp1", Document2D, tracex - 619.1209);
                    CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Mtemp", tracex - 619.1209, 0, 1);
                    tempX += 101659.6570 + 10000;
                    spaceX = tempX - 19068.9248;
                    DRS27(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                }
            }
            else if (SupType == SupportType.Support28.ToString())
            {
                boxlen = 17299.3016;
                boxht = 12734.3388;
                if (tracex >= spaceX - boxlen)
                {
                    spaceY -= boxht;
                    tracex = tempX - 101659.6570 + 619.1209;
                }
                if (spaceY > boxht)
                {
                    DRS28(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                }
                else
                {

                    spaceY = 71075.0829;
                    tracex = tempX + 10000 + 619.1209;//gaps
                                                      // gets the template
                                                      // GetTemplate(AcadBlockTableRecord, AcadTransaction, AcadDatabase,tracex- 619.1209);

                    //CopyPasteTemplateFile("Temp1", Document2D, tracex - 619.1209);
                    CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Mtemp", tracex - 619.1209, 0, 1);
                    tempX += 101659.6570 + 10000;
                    spaceX = tempX - 19068.9248;
                    DRS28(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                }
            }

            else if (SupType == SupportType.Support29.ToString())
            {
                boxlen = 17299.3016;
                boxht = 12734.3388;
                if (tracex >= spaceX - boxlen)
                {
                    spaceY -= boxht;
                    tracex = tempX - 101659.6570 + 619.1209;
                }
                if (spaceY > boxht)
                {
                    DRS29(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                }
                else
                {

                    spaceY = 71075.0829;
                    tracex = tempX + 10000 + 619.1209;//gaps
                                                      // gets the template
                                                      // GetTemplate(AcadBlockTableRecord, AcadTransaction, AcadDatabase,tracex- 619.1209);

                    //CopyPasteTemplateFile("Temp1", Document2D, tracex - 619.1209);
                    CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Mtemp", tracex - 619.1209, 0, 1);
                    tempX += 101659.6570 + 10000;
                    spaceX = tempX - 19068.9248;
                    DRS29(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                }
            }
            else if (SupType == SupportType.Support31.ToString())
            {
                boxlen = 17299.3016;
                boxht = 12734.3388;
                if (tracex >= spaceX - boxlen)
                {
                    spaceY -= boxht;
                    tracex = tempX - 101659.6570 + 619.1209;
                }
                if (spaceY > boxht)
                {
                    DRS31(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                }
                else
                {

                    spaceY = 71075.0829;
                    tracex = tempX + 10000 + 619.1209;//gaps
                                                      // gets the template
                                                      // GetTemplate(AcadBlockTableRecord, AcadTransaction, AcadDatabase,tracex- 619.1209);

                    //CopyPasteTemplateFile("Temp1", Document2D, tracex - 619.1209);
                    CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Mtemp", tracex - 619.1209, 0, 1);
                    tempX += 101659.6570 + 10000;
                    spaceX = tempX - 19068.9248;
                    DRS31(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                }
            }
            else if (SupType == SupportType.Support33.ToString())
            {
                boxlen = 17299.3016;
                boxht = 12734.3388;
                if (tracex >= spaceX - boxlen)
                {
                    spaceY -= boxht;
                    tracex = tempX - 101659.6570 + 619.1209;
                }
                if (spaceY > boxht)
                {
                    DRS33(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                }
                else
                {

                    spaceY = 71075.0829;
                    tracex = tempX + 10000 + 619.1209;//gaps
                                                      // gets the template
                                                      // GetTemplate(AcadBlockTableRecord, AcadTransaction, AcadDatabase,tracex- 619.1209);

                    //CopyPasteTemplateFile("Temp1", Document2D, tracex - 619.1209);
                    CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Mtemp", tracex - 619.1209, 0, 1);
                    tempX += 101659.6570 + 10000;
                    spaceX = tempX - 19068.9248;
                    DRS33(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                }
            }
            //if (SupportType == SupportType.Elevation)
            //{
            //    boxlen = 30162;
            //    boxht = 12734.3388;

            //    if (tracex >= spaceX - boxlen)
            //    {
            //        spaceY -= boxht;
            //        tracex = tempX - 101659.6570 + 619.1209;
            //    }
            //    if (spaceY > boxht)
            //    {
            //        double upperYgap = 2812;

            //        double centerX = tracex + 5636;  // boxlen / 2;  // 9869.9480;
            //        double centerY = spaceY - upperYgap;
            //        //box boundaries
            //        //vertical line
            //        Point3d pt1 = new Point3d(tracex + boxlen, spaceY + 619.1209, 0);
            //        Point3d Pt2 = new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0);
            //        Line line = new Line(pt1, Pt2);
            //        AcadBlockTableRecord.AppendEntity(line);
            //        line.Color = Color.FromColorIndex(ColorMethod.ByAci, 171);
            //        AcadTransaction.AddNewlyCreatedDBObject(line, true);


            //        //hori line
            //        Point3d pt11 = new Point3d(tracex, spaceY - boxht + 619.1209, 0);
            //        Point3d Pt21 = new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0);
            //        Line line1 = new Line(pt11, Pt21);
            //        AcadBlockTableRecord.AppendEntity(line1);
            //        line1.Color = Color.FromColorIndex(ColorMethod.ByAci, 171);
            //        AcadTransaction.AddNewlyCreatedDBObject(line1, true);

            //        height = 3000;
            //        length = 1000;

            //        FixCreateBottomSupportTopType2(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY, height, length);
            //        FixCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY, "Left");
            //        FixCreateSecondarySupportTop(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY);
            //        //FixCreatePrimarySupportwithvertex(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY);
            //        FixPrim_Elevation(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY, Document2D);

            //        //side view
            //        centerX = centerX + 17000;
            //        InsertBlockOnDocument("Side_Primary_Elevation", Document2D, centerX, centerY);
            //        InsertBlockOnDocument("Side_Top_Secondary", Document2D, centerX, centerY - 2226);
            //        ElevFixCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY);
            //        height = 3000;
            //        length = 1000;

            //        FixCreateBottomSupportTopType2(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY, height, length);
            //        tracex += boxlen;
            //    }
            //    else
            //    {

            //        spaceY = 71075.0829;
            //        tracex = tempX + 10000 + 619.1209;//gaps
            //                                          // gets the template
            //                                          // GetTemplate(AcadBlockTableRecord, AcadTransaction, AcadDatabase,tracex- 619.1209);
            //        CopyPasteTemplateFile("Temp1", Document2D, tracex - 619.1209);
            //        tempX += 101659.6570 + 10000;
            //        spaceX = tempX - 19068.9248;
            //    }
            //}

        }

        [Obsolete]
        void CreateLeaderfromfile(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, double insertptX, double insertptY)
        {


            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);

            string workingDirectory = Path.GetDirectoryName(path);

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
                Vector3d destvect = strpt.GetVectorTo(new Point3d(insertptX, insertptY, 0));

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
            tr2.Commit();
            db.Dispose();

        }



        [Obsolete]
        void InsertblockScale(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, string blockname, double insertptX, double insertptY, double scaleFactor = 1)
        {


            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);

            string workingDirectory = Path.GetDirectoryName(path);

            // Get the project file path by searching for the .csproj file in the working directory
            string projectFilePath = Directory.GetFiles(workingDirectory, blockname + ".dwg").FirstOrDefault();

            //string dwgPath = "D:\\Projects\\Plant 3D\\LeaderBlock.dwg";


            // Create a new database object
            Database db = new Database(true, true);
            db.ReadDwgFile(projectFilePath, FileOpenMode.OpenForReadAndWriteNoShare, true, "");

            Transaction tr2 = db.TransactionManager.StartTransaction();
            // Open the Block table for read
            BlockTable btsrc = tr2.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;


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
                Vector3d destvect = strpt.GetVectorTo(new Point3d(insertptX, insertptY, 0));

                var ent = (Entity)tr2.GetObject(id, OpenMode.ForWrite);
                // var ent2 = (Entity)tr2.GetObject(id, OpenMode.ForRead);

                Matrix3d scaleMatrix = Matrix3d.Scaling(scaleFactor, new Point3d(0, 0, 0));

                // Apply the scaling matrix to the entity
                ent.TransformBy(scaleMatrix);

                ent.TransformBy(Matrix3d.Displacement(destvect));
                // Point3d pos=ent2.Position
                //ent.TransformBy(Matrix3d.);
                sourceIds.Add(id);
            }
            tr2.Commit();
            // next prepare to deepclone the recorded ids to the destdb
            IdMapping mapping = new IdMapping();
            // now clone the objects into the destdb

            db.WblockCloneObjects(sourceIds, destDbMsId, mapping, DuplicateRecordCloning.Replace, false);

            db.Dispose();

        }



        public void CopyAndModifyEntities(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, string sourceDwg, double insertptX, double insertptY, double scaleFactor)
        {

            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);

            string workingDirectory = Path.GetDirectoryName(path);

            // Get the project file path by searching for the .csproj file in the working directory
            string sourceDwgPath = Directory.GetFiles(workingDirectory, sourceDwg + ".dwg").FirstOrDefault();

            // Start a transaction

            // Get the BlockTableRecord of the model space in the destination database
            //BlockTableRecord destModelSpace = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(destDb), OpenMode.ForWrite);

            // Create a new Database object for the source DWG file
            Database sourceDb = new Database(false, true);
            sourceDb.ReadDwgFile(sourceDwgPath, FileOpenMode.OpenForReadAndWriteNoShare, true, "");


            Transaction tr2 = sourceDb.TransactionManager.StartTransaction();

            // Open the BlockTableRecord of the model space in the source database
            BlockTableRecord sourceModelSpace = (BlockTableRecord)sourceDb.TransactionManager.GetObject(sourceDb.CurrentSpaceId, OpenMode.ForWrite);

            // Create an ObjectIdCollection to hold the source object IDs
            ObjectIdCollection sourceObjectIds = new ObjectIdCollection();

            // Loop through the entities in the source model space
            foreach (ObjectId sourceObjectId in sourceModelSpace)
            {
                // Add the source ObjectId to the collection
                sourceObjectIds.Add(sourceObjectId);

                // Modify the entity as needed
                Entity sourceEntity = (Entity)tr2.GetObject(sourceObjectId, OpenMode.ForWrite);
                ColorMethod colorMethod = ColorMethod.ByLayer;
                int colorMethodCode = (int)colorMethod;
                sourceEntity.ColorIndex = colorMethodCode;

                Point3d strpt = new Point3d(0, 0, 0);
                Vector3d destvect = strpt.GetVectorTo(new Point3d(insertptX, insertptY, 0));

                // var ent = (Entity)AcadTransaction.GetObject(sourceObjectId, OpenMode.ForWrite);
                //var ent2 = (Entity)AcadTransaction.GetObject(sourceObjectId, OpenMode.ForRead);

                Matrix3d scaleMatrix = Matrix3d.Scaling(scaleFactor, new Point3d(0, 0, 0));

                // Apply the scaling matrix to the entity
                sourceEntity.TransformBy(scaleMatrix);

                sourceEntity.TransformBy(Matrix3d.Displacement(destvect));

                // For example

                // Add the modified entity to the destination database
                Entity destEntity = sourceEntity.Clone() as Entity;

                //AcadBlockTableRecord.AppendEntity(destEntity);
                //tr2.AddNewlyCreatedDBObject(destEntity, true);
            }

            // Create a new IdMapping object
            IdMapping mapping = new IdMapping();

            // Clone the source objects to the destination database
            AcadDatabase.WblockCloneObjects(sourceObjectIds, AcadBlockTableRecord.ObjectId, mapping, DuplicateRecordCloning.Replace, false);

            // Commit the transaction and dispose of the source database
            tr2.Commit();
            sourceDb.Dispose();


            // Save and close the destination database
            // destDb.SaveAs(destDwgPath, DwgVersion.Current);
            // destDb.Dispose();

        }



        //fix new function for Bottom Support Top

        [Obsolete]
        private void FixCreateBottomSupportTopType2(Document Document2D, BlockTableRecord acadBlockTableRecord, Transaction acadTransaction, Database acadDatabase, double centerX, double centerY, double height, double length, [Optional] int i)
        {

            double ht_frm_cen = info[Defination.Sec_ht_bot];

            var newline = new Polyline();
            Point2d Pt2D1 = new Point2d(centerX - length / 2, centerY - ht_frm_cen);
            newline.AddVertexAt(0, Pt2D1, 0, 0, 0);
            Pt2D1 = new Point2d(centerX + length / 2, centerY - ht_frm_cen);
            newline.AddVertexAt(1, Pt2D1, 0, 0, 0);
            Pt2D1 = new Point2d(centerX + length / 2, centerY - ht_frm_cen - height);

            //dimensioning
            RotatedDimension dim = new RotatedDimension(Math.PI / 2, new Point3d(centerX + length / 2, centerY - ht_frm_cen, 0), new Point3d(centerX + length / 2, centerY - ht_frm_cen - height, 0), new Point3d(centerX + length / 2 + 1400, centerY - ht_frm_cen - height, 0), "", ObjectId.Null);
            dim.Dimtxt = 100;
            dim.Dimasz = 150;



            //if (ListCentalSuppoData[0].ListConcreteData[0].BoxData.Z > ListCentalSuppoData[0].ListConcreteData[1].BoxData.Z)
            //{
            //    dim.DimensionText = (ListCentalSuppoData[0].ListConcreteData[0].BoxData.Z).ToString();
            //}
            //else
            //{
            //    dim.DimensionText = (ListCentalSuppoData[0].ListConcreteData[1].BoxData.Z).ToString();
            //}
            try
            {
                if (ListCentalSuppoData[i].ListConcreteData.Count > 1)
                {
                    dim.DimensionText = Math.Max(ListCentalSuppoData[i].ListConcreteData[0].BoxData.Z, ListCentalSuppoData[i].ListConcreteData[1].BoxData.Z).ToString();
                }
                else
                {
                    dim.DimensionText = ListCentalSuppoData[i].ListConcreteData[0].BoxData.Z.ToString();
                }


            }
            catch (Exception)
            {

            }


            dim.Dimclre = Color.FromColor(System.Drawing.Color.Cyan);
            dim.Dimclrt = Color.FromColor(System.Drawing.Color.Yellow);
            dim.Dimclrd = Color.FromColor(System.Drawing.Color.Cyan);

            var chk = dim.GetDimstyleData();



            dim.Dimtih = true;
            // dim.Dimtoh = false;

            acadBlockTableRecord.AppendEntity(dim);
            acadTransaction.AddNewlyCreatedDBObject(dim, true);

            newline.AddVertexAt(2, Pt2D1, 0, 0, 0);
            Pt2D1 = new Point2d(centerX - length / 2, centerY - ht_frm_cen - height);
            newline.AddVertexAt(3, Pt2D1, 0, 0, 0);
            Pt2D1 = new Point2d(centerX - length / 2, centerY - ht_frm_cen);
            newline.AddVertexAt(4, Pt2D1, 0, 0, 0);
            newline.Color = Color.FromColorIndex(ColorMethod.ByAci, 8);

            LinetypeTable acLineTypTbl;
            acLineTypTbl = acadTransaction.GetObject(acadDatabase.LinetypeTableId,
                                                   OpenMode.ForRead) as LinetypeTable;

            string sLineTypName = "HIDDEN2";
            if (acLineTypTbl.Has(sLineTypName) == false)
            {
                acadDatabase.LoadLineTypeFile(sLineTypName, "acad.lin");
            }
            newline.Linetype = sLineTypName;
            newline.LinetypeScale = 100;
            acadBlockTableRecord.AppendEntity(newline);
            acadTransaction.AddNewlyCreatedDBObject(newline, true);

            info[Defination.Prim_ht] = ht_frm_cen;

            //detail line
            //detail line
            Point3d dpt1 = new Point3d(centerX + length / 2, centerY - ht_frm_cen - height, 0);
            Point3d dPt2 = new Point3d(centerX + length / 2 + 4000, centerY - ht_frm_cen - height, 0);
            Line dline = new Line(dpt1, dPt2);
            dline.Linetype = sLineTypName;
            dline.LinetypeScale = 100;
            acadBlockTableRecord.AppendEntity(dline);
            dline.Color = Color.FromColorIndex(ColorMethod.ByAci, 8);
            acadTransaction.AddNewlyCreatedDBObject(dline, true);

            //mtext
            CreateMtextfunc(acadBlockTableRecord, acadTransaction, acadDatabase, new Point3d(centerX + length / 2 + 2500, centerY - ht_frm_cen - height + 300, 0), "HPP.(+)" + datum_level);


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
            insideupperblock.Linetype = sLineTypName;
            insideupperblock.LinetypeScale = 100;
            insideupperblock.Color = Color.FromColorIndex(ColorMethod.ByAci, 8);

            CreateLeaderfromfile(acadBlockTableRecord, acadTransaction, acadDatabase, centerX + length / 2 - offsetinside - 450, centerY - ht_frm_cen);

            //InsertBlockOnDocument("LeaderBlock", Document2D, centerX + length / 2 - offsetinside - 450, centerY - ht_frm_cen);

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
            line.Linetype = sLineTypName;
            line.LinetypeScale = 100;
            line.Color = Color.FromColorIndex(ColorMethod.ByAci, 8);
            acadBlockTableRecord.AppendEntity(line);

            acadTransaction.AddNewlyCreatedDBObject(line, true);


            Point3d pt12 = new Point3d(centerX + length / 2 - 2 * offsetinside, centerY - ht_frm_cen - offsetht, 0);
            Point3d Pt22 = new Point3d(centerX + length / 2 - 2 * offsetinside, centerY - ht_frm_cen - offsetht - lineHt, 0);
            Line line2 = new Line(pt12, Pt22);
            line2.Linetype = sLineTypName;
            line2.LinetypeScale = 100;
            line2.Color = Color.FromColorIndex(ColorMethod.ByAci, 8);
            acadBlockTableRecord.AppendEntity(line2);

            acadTransaction.AddNewlyCreatedDBObject(line2, true);

            //horizontal side lines
            Point3d pt3 = new Point3d(centerX - length / 2 + 2 * offsetinside, centerY - ht_frm_cen - offsetht - lineHt, 0);
            Point3d Pt4 = new Point3d(centerX - length / 2, centerY - ht_frm_cen - offsetht - lineHt, 0);
            Line lineh1 = new Line(pt3, Pt4);
            lineh1.Linetype = sLineTypName;
            lineh1.LinetypeScale = 100;
            lineh1.Color = Color.FromColorIndex(ColorMethod.ByAci, 8);
            acadBlockTableRecord.AppendEntity(lineh1);

            acadTransaction.AddNewlyCreatedDBObject(lineh1, true);

            //horizontal side lines
            Point3d pt5 = new Point3d(centerX + length / 2 - 2 * offsetinside, centerY - ht_frm_cen - offsetht - lineHt, 0);
            Point3d Pt6 = new Point3d(centerX + length / 2, centerY - ht_frm_cen - offsetht - lineHt, 0);
            Line lineh2 = new Line(pt5, Pt6);
            lineh2.Linetype = sLineTypName;
            lineh2.LinetypeScale = 100;
            lineh2.Color = Color.FromColorIndex(ColorMethod.ByAci, 8);
            acadBlockTableRecord.AppendEntity(lineh2);

            acadTransaction.AddNewlyCreatedDBObject(lineh2, true);

        }

        private void FixCreateSecondarySupportTop(BlockTableRecord acadBlockTableRecord, Transaction acadTransaction, Database acadDatabase, double centerX, double centerY)
        {
            double height = 1000.0000;
            double length = 2666.6667;
            double ht_frm_cen = 1220.7383;
            var newline = new Polyline();
            //rectangular polyline
            Point2d Pt2D1 = new Point2d(centerX - length / 2, centerY - ht_frm_cen);

            CreateDimension(new Point3d(centerX - length / 2, centerY - ht_frm_cen, 0), new Point3d(centerX, centerY, 0));

            newline.AddVertexAt(0, Pt2D1, 0, 0, 0);
            Pt2D1 = new Point2d(centerX + length / 2, centerY - ht_frm_cen);

            CreateDimension(new Point3d(centerX + length / 2, centerY - ht_frm_cen, 0), new Point3d(centerX, centerY, 0));

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

            //info
            info[Defination.Sec_ht_top] = info[Defination.Prim_ht] - ht_frm_cen;

            //detail line
            Point3d dpt1 = new Point3d(centerX + length / 2, centerY - ht_frm_cen, 0);
            Point3d dPt2 = new Point3d(centerX + length / 2 + 4000, centerY - ht_frm_cen, 0);
            Line dline = new Line(dpt1, dPt2);
            acadBlockTableRecord.AppendEntity(dline);
            dline.Color = Color.FromColorIndex(ColorMethod.ByAci, 171);
            acadTransaction.AddNewlyCreatedDBObject(dline, true);

            string TotalHt = "";
            try
            {
                if (ListCentalSuppoData[0].ListConcreteData[0].BoxData.Z > ListCentalSuppoData[0].ListConcreteData[1].BoxData.Z)
                {
                    TotalHt = (ListCentalSuppoData[0].ListConcreteData[0].BoxData.Z + ListCentalSuppoData[0].ListSecondrySuppo[0].BoxData.Z + ListCentalSuppoData[0].ListSecondrySuppo[1].BoxData.Z).ToString();
                }
                else
                {
                    TotalHt = (ListCentalSuppoData[0].ListConcreteData[1].BoxData.Z + ListCentalSuppoData[0].ListSecondrySuppo[0].BoxData.Z + ListCentalSuppoData[0].ListSecondrySuppo[1].BoxData.Z).ToString();
                }
            }
            catch (Exception)
            {

            }


            //mtext
            try
            {
                //TotalHt = MillimetersToMeters(Convert.ToDouble(TotalHt)).ToString();
            }
            catch (Exception)
            {

            }
            CreateMtextfunc(acadBlockTableRecord, acadTransaction, acadDatabase, new Point3d(centerX + length / 2 + 1200, centerY - ht_frm_cen + 300, 0), "TOS EL.(+)" + TotalHt /*info[Defination.Sec_ht].ToString()*/);

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

            //dimensioning
            RotatedDimension dim = new RotatedDimension(Math.PI / 2, new Point3d(centerX + length / 2, centerY - ht_frm_cen, 0), new Point3d(centerX + length / 2, centerY - ht_frm_cen - height, 0), new Point3d(centerX + length / 2 + 2500, centerY - ht_frm_cen - height, 0), "", ObjectId.Null);
            dim.Dimtxt = 100;
            dim.Dimasz = 150;

            if (ListCentalSuppoData[0].ListSecondrySuppo[0].BoxData.Z > ListCentalSuppoData[0].ListSecondrySuppo[1].BoxData.Z)
            {
                dim.DimensionText = (ListCentalSuppoData[0].ListSecondrySuppo[0].BoxData.Z).ToString();
            }
            else
            {
                dim.DimensionText = (ListCentalSuppoData[0].ListSecondrySuppo[1].BoxData.Z).ToString();
            }
            dim.Dimclre = Color.FromColor(System.Drawing.Color.Cyan);
            dim.Dimclrt = Color.FromColor(System.Drawing.Color.Yellow);
            dim.Dimclrd = Color.FromColor(System.Drawing.Color.Cyan);
            dim.Dimtih = true;
            //  dim.Dimtoh = false;


            acadBlockTableRecord.AppendEntity(dim);
            AcadTransaction.AddNewlyCreatedDBObject(dim, true);

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

            // FixCreateSideSecondarySupportBottom(AcadTransaction, acadBlockTableRecord, acadDatabase, "Left", centerX, centerY);

        }

        public void CreateDimension(Point3d strpt, Point3d endpt, [Optional] string topsec, double vertical_offset = 1200)
        {


            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            //dimension line point

            // Create the dimension
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForWrite);
                RotatedDimension dim = new RotatedDimension(0, strpt, endpt, new Point3d(endpt.X, endpt.Y + vertical_offset, 0), "", ObjectId.Null);

                dim.Dimtxt = 100;
                dim.Dimasz = 150;

                dim.Dimdec = 2;
                try
                {
                    dim.DimensionText = Math.Round(Convert.ToDouble(topsec)).ToString();
                }
                catch (Exception)
                {
                    dim.DimensionText = topsec;
                }


                dim.Dimclre = Color.FromColor(System.Drawing.Color.Cyan);
                dim.Dimclrt = Color.FromColor(System.Drawing.Color.Yellow);
                dim.Dimclrd = Color.FromColor(System.Drawing.Color.Cyan);
                dim.Dimtih = true;
                // dim.Dimtoh = false;


                btr.AppendEntity(dim);
                tr.AddNewlyCreatedDBObject(dim, true);
                tr.Commit();
            }

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


        private static string _DBText(ObjectId id)
        {
            return id.ObjectClass.Name + " " + id.Handle.ToString();
        }
        private void FixCreatePrimarySupportwithvertex(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, double centerX, double centerY, double radius, string suppnameside = "r")
        {


            Point3d center = new Point3d(centerX, centerY, 0);
            // radius = 801.5625;

            //stores radius value
            info[Defination.Prim_Radius] = radius;

            double ht_frm_cen = radius * 1.5;

            info[Defination.Prim_ht] = ht_frm_cen;

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


            // Create a new diametric dimension entity
            Point3d chordPoint = new Point3d(center.X + radius, center.Y, center.Z);
            Point3d farChordPoint = new Point3d(center.X - radius, center.Y, center.Z);
            double leaderLength = 1.0; // The length of the dimension leader line
            string dimensionText = ""; // The dimension text, which is automatically generated
            ObjectId dimStyleId = AcadDatabase.Dimstyle; // The object ID of the dimension style to use
            RadialDimension dim = new RadialDimension(circle.Center, chordPoint, leaderLength, dimensionText, dimStyleId);
            dim.Dimcen = radius;
            dim.LinetypeScale = 50;
            string centerlinetype = "CENTERX2";
            if (acLineTypTbl.Has(centerlinetype) == false)
            {
                AcadDatabase.LoadLineTypeFile(centerlinetype, "acad.lin");
            }
            dim.Linetype = centerlinetype;
            dim.Dimclre = Color.FromColor(MyCol.Red);

            //// Create a new dimension style with the desired center mark type
            //DimensionStyle newDimStyle = dimStyle.Clone() as DimensionStyle;
            //newDimStyle.CenterMarkType = CenterMarkStyle.Arc;

            //var abc = dim.CenterMarkSize;linety
            //dim.CenterMarkSize=
            // Add the dimension to the drawing database
            AcadBlockTableRecord.AppendEntity(dim);
            AcadTransaction.AddNewlyCreatedDBObject(dim, true);

            AcadBlockTableRecord.AppendEntity(circle);
            AcadTransaction.AddNewlyCreatedDBObject(circle, true);



            //mtext
            if (suppnameside == "l" || suppnameside == "L")
            {
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - radius - 1500, centerY + 300, 0), ListCentalSuppoData[0].ListPrimarySuppo[0].SupportName);

                //detail line
                Point3d dpt1 = new Point3d(centerX - radius, centerY, 0);
                Point3d dPt2 = new Point3d(centerX - radius - 4000, centerY, 0);
                Line dline = new Line(dpt1, dPt2);
                AcadBlockTableRecord.AppendEntity(dline);
                dline.Color = Color.FromColorIndex(ColorMethod.ByAci, 171);
                AcadTransaction.AddNewlyCreatedDBObject(dline, true);

            }
            else
            {
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + radius + 1500, centerY + 300, 0), ListCentalSuppoData[0].ListPrimarySuppo[0].SupportName);

                //detail line
                Point3d dpt1 = new Point3d(centerX + radius, centerY, 0);
                Point3d dPt2 = new Point3d(centerX + radius + 4000, centerY, 0);
                Line dline = new Line(dpt1, dPt2);
                AcadBlockTableRecord.AppendEntity(dline);
                dline.Color = Color.FromColorIndex(ColorMethod.ByAci, 171);
                AcadTransaction.AddNewlyCreatedDBObject(dline, true);
            }


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


        public void CreateMtextfunc(BlockTableRecord acadBlockTableRecord, Transaction acadTransaction, Database acadDatabase, Point3d location, string text, [Optional] double textheight, [Optional] MyCol color)
        {

            // Create a new MText object with some text
            MText mtext = new MText();

            //try
            //{
            //    text = RoundUp(Convert.ToInt32(text)).ToString();
            //}
            //catch (Exception)
            //{

            //}

            mtext.Contents = text;

            // Set the position of the MText object
            mtext.Location = location;

            mtext.TextHeight = textheight == 0 ? 150 : textheight;

            mtext.Color = color.IsEmpty == true ? Color.FromColor(MyCol.Yellow) : Color.FromColor(color);

            // Add the MText object to the drawing
            acadBlockTableRecord.AppendEntity(mtext);
            acadTransaction.AddNewlyCreatedDBObject(mtext, true);

        }

        //for elevation view
        public void FixPrim_Elevation(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, double centerX, double centerY, Document Document2D)
        {
            Point3d center = new Point3d(centerX, centerY, 0);
            double radius = 801.5625;

            //insert block
            InsertBlockOnDocument("Front_Primary_Elevation", Document2D, centerX, centerY);

            //stores radius value
            info[Defination.Prim_Radius] = radius;


            //detail line
            Point3d dpt1 = new Point3d(centerX + radius, centerY, 0);
            Point3d dPt2 = new Point3d(centerX + radius + 4000, centerY, 0);
            Line dline = new Line(dpt1, dPt2);
            AcadBlockTableRecord.AppendEntity(dline);
            dline.Color = Color.FromColorIndex(ColorMethod.ByAci, 171);
            AcadTransaction.AddNewlyCreatedDBObject(dline, true);

            //mtext
            CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + radius + 1500, centerY + 300, 0), "300NB");

            CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + radius + 1500, centerY - 100, 0), "CL.EL.(+)" + info[Defination.Prim_ht].ToString());

            //center mark
            //centerline
            Point3d cpt1 = new Point3d(centerX, centerY + radius + 250, 0);
            Point3d cPt2 = new Point3d(centerX, centerY - radius - 250, 0);
            Line cline = new Line(cpt1, cPt2);
            cline.Linetype = "Dashed";
            AcadBlockTableRecord.AppendEntity(cline);
            cline.Color = Color.FromColorIndex(ColorMethod.ByAci, 171);
            AcadTransaction.AddNewlyCreatedDBObject(cline, true);

            Point3d cpt11 = new Point3d(centerX + radius + 250, centerY, 0);
            Point3d cPt21 = new Point3d(centerX - radius - 250, centerY, 0);
            Line cline1 = new Line(cpt11, cPt21);
            cline1.Linetype = "Dashed";
            AcadBlockTableRecord.AppendEntity(cline1);
            cline1.Color = Color.FromColorIndex(ColorMethod.ByAci, 171);
            AcadTransaction.AddNewlyCreatedDBObject(cline1, true);

            //centerline
            Point3d lcpt1 = new Point3d(centerX, centerY + radius + 250, 0);
            Point3d lcPt2 = new Point3d(centerX, centerY - info[Defination.Prim_ht] - 250, 0);
            Line lcline = new Line(lcpt1, lcPt2);
            lcline.Linetype = "Dashed";
            AcadBlockTableRecord.AppendEntity(lcline);
            lcline.Color = Color.FromColorIndex(ColorMethod.ByAci, 171);
            AcadTransaction.AddNewlyCreatedDBObject(lcline, true);

        }

        //insert any block
        public void InsertBlockOnDocument(string fileName, Document finalDocument, double inserptX, double insertY)
        {

            ObjectIdCollection ids = new ObjectIdCollection();
            DocumentCollection documentCollection = Application.DocumentManager;

            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);

            string workingDirectory = Path.GetDirectoryName(path);

            // Get the project file path by searching for the .csproj file in the working directory
            string projectFilePath = Directory.GetFiles(workingDirectory, fileName + ".dwg").FirstOrDefault();

            var tempDocument = documentCollection.Open(projectFilePath, false);
            using (tempDocument.LockDocument())
            {
                var openDb = tempDocument.Database; using (Transaction trans = openDb.TransactionManager.StartTransaction())
                {
                    BlockTable bt = (BlockTable)trans.GetObject(openDb.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord btrsrc = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;


                    foreach (ObjectId id in btrsrc)
                    {
                        Point3d strpt = new Point3d(0, 0, 0);
                        Vector3d destvect = strpt.GetVectorTo(new Point3d(inserptX, insertY, 0));
                        var ent = (Entity)trans.GetObject(id, OpenMode.ForWrite);
                        ent.TransformBy(Matrix3d.Displacement(destvect));
                        //ent.UpgradeOpen();
                        if (ent.IsWriteEnabled)
                        {
                            // Object is not a proxy, clone it as-is
                            ids.Add(id);
                        }
                        else
                        {

                        }
                    }

                    trans.Commit();
                }
            }
            documentCollection.MdiActiveDocument = finalDocument;
            if (ids.Count != 0)
            {
                using (finalDocument.LockDocument())
                {
                    Database destdb = finalDocument.Database;
                    using (Transaction trans = destdb.TransactionManager.StartTransaction())
                    {
                        IdMapping iMap = new IdMapping();
                        //logger.Debug("Entering Copying of Template File");
                        destdb.WblockCloneObjects(ids, destdb.CurrentSpaceId, iMap, DuplicateRecordCloning.Replace, false);
                        trans.Commit();
                        //logger.Debug("Copied the Template File");
                    }
                }
            }
            tempDocument.CloseAndDiscard();

        }

        private void CopyPasteTemplateFile(string fileName, Document finalDocument, double inserptX)
        {
            ObjectIdCollection ids = new ObjectIdCollection();
            DocumentCollection documentCollection = Application.DocumentManager;

            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);

            string workingDirectory = Path.GetDirectoryName(path);

            // Get the project file path by searching for the .csproj file in the working directory
            string projectFilePath = Directory.GetFiles(workingDirectory, "Mtemp.dwg").FirstOrDefault();

            var tempDocument = documentCollection.Open(projectFilePath, false);
            using (tempDocument.LockDocument())
            {
                var openDb = tempDocument.Database; using (Transaction trans = openDb.TransactionManager.StartTransaction())
                {
                    BlockTable bt = (BlockTable)trans.GetObject(openDb.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord btrsrc = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;


                    foreach (ObjectId id in btrsrc)
                    {
                        Point3d strpt = new Point3d(0, 0, 0);
                        Vector3d destvect = strpt.GetVectorTo(new Point3d(inserptX, 0, 0));
                        var ent = (Entity)trans.GetObject(id, OpenMode.ForWrite);
                        ent.TransformBy(Matrix3d.Displacement(destvect));
                        //ent.UpgradeOpen();
                        if (ent.IsWriteEnabled)
                        {
                            // Object is not a proxy, clone it as-is
                            ids.Add(id);
                        }
                        else
                        {

                        }
                    }

                    trans.Commit();
                }
            }
            documentCollection.MdiActiveDocument = finalDocument;
            if (ids.Count != 0)
            {
                using (finalDocument.LockDocument())
                {
                    Database destdb = finalDocument.Database;
                    using (Transaction trans = destdb.TransactionManager.StartTransaction())
                    {
                        IdMapping iMap = new IdMapping();
                        //logger.Debug("Entering Copying of Template File");
                        destdb.WblockCloneObjects(ids, destdb.CurrentSpaceId, iMap, DuplicateRecordCloning.Replace, false);
                        trans.Commit();
                        //logger.Debug("Copied the Template File");
                    }
                }
            }
            tempDocument.CloseAndDiscard();
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

        //changes from other cs -extra functions
        //fix new function for Secondary Support Bottom

        private void ElevFixCreateSecondarySupportBottom(BlockTableRecord acadBlockTableRecord, Transaction AcadTransaction, Database acadDatabase, double centerX, double centerY)
        {
            double height = 3066.5059;
            double length = 416;
            double ht_frm_cen = 1220.7383 + 1000;
            var line = new Polyline();
            //double height1 = prmsupport.Boundingboxmax.Z - prmsupport.Boundingboxmin.Z;
            Point2d Pt2D1 = new Point2d(centerX - length / 2, centerY - ht_frm_cen);
            line.AddVertexAt(0, Pt2D1, 0, 0, 0);
            Pt2D1 = new Point2d(centerX + length / 2, centerY - ht_frm_cen);
            line.AddVertexAt(1, Pt2D1, 0, 0, 0);
            Pt2D1 = new Point2d(centerX + length / 2, centerY - ht_frm_cen - height);
            line.AddVertexAt(2, Pt2D1, 0, 0, 0);

            //dimensioning
            RotatedDimension dim = new RotatedDimension(Math.PI / 2, new Point3d(centerX + length / 2, centerY - ht_frm_cen, 0), new Point3d(centerX + length / 2, centerY - ht_frm_cen - height, 0), new Point3d(centerX + length / 2 + 2500, centerY - ht_frm_cen - height, 0), "", ObjectId.Null);
            dim.Dimtxt = 100;
            dim.Dimasz = 150;

            dim.Dimclre = Color.FromColor(System.Drawing.Color.Cyan);
            dim.Dimclrt = Color.FromColor(System.Drawing.Color.Yellow);
            dim.Dimclrd = Color.FromColor(System.Drawing.Color.Cyan);

            dim.Dimtih = true;



            acadBlockTableRecord.AppendEntity(dim);
            AcadTransaction.AddNewlyCreatedDBObject(dim, true);

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
            //LineDraw(,)
        }
        private void FixCreateSecondarySupportBottom(BlockTableRecord acadBlockTableRecord, Transaction AcadTransaction, Database acadDatabase, double centerX, double centerY, string side)
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

            //dimensioning
            RotatedDimension dim = new RotatedDimension(Math.PI / 2, new Point3d(centerX + length / 2, centerY - ht_frm_cen, 0), new Point3d(centerX + length / 2, centerY - ht_frm_cen - height, 0), new Point3d(centerX + length / 2 + 2500, centerY - ht_frm_cen - height, 0), "", ObjectId.Null);
            dim.Dimtxt = 100;
            dim.Dimasz = 150;

            dim.Dimclre = Color.FromColor(System.Drawing.Color.Cyan);
            dim.Dimclrt = Color.FromColor(System.Drawing.Color.Yellow);
            dim.Dimclrd = Color.FromColor(System.Drawing.Color.Cyan);

            dim.Dimtih = true;



            acadBlockTableRecord.AppendEntity(dim);
            AcadTransaction.AddNewlyCreatedDBObject(dim, true);

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
            FixCreateSideSecondarySupportBottom(AcadTransaction, acadBlockTableRecord, acadDatabase, side, centerX, centerY);

        }

        //linedraw

        /// <param name="AcadBlockTableRecord">My number parameter</param>
        public void LineDraw(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database acadDatabase, Point3d startpt, Point3d endpt, MyCol color, [Optional] string linetype, [Optional] double linetypeScale)
        {
            Point3d cpt1 = startpt;//new Point3d(centerX, centerY + radius + 250, 0);
            Point3d cPt2 = endpt;// new Point3d(centerX, centerY - radius - 250, 0);
            Line cline = new Line(cpt1, cPt2);


            try
            {
                LinetypeTable acLineTypTbl;
                acLineTypTbl = AcadTransaction.GetObject(acadDatabase.LinetypeTableId,
                                                       OpenMode.ForRead) as LinetypeTable;

                if (acLineTypTbl.Has(linetype) == false)
                {
                    acadDatabase.LoadLineTypeFile(linetype, "acad.lin");
                    // cline.Linetype = Linetype;

                }
                cline.Linetype = linetype;
            }
            catch (Exception)
            {
            }
            if (color == null)
            {
                color = MyCol.LightBlue;
            }
            cline.Color = Color.FromColor(color);
            if (linetypeScale != 0)
            {
                cline.LinetypeScale = linetypeScale;
            }

            AcadBlockTableRecord.AppendEntity(cline);
            //cline.Color = Color.FromColorIndex(ColorMethod.ByAci, 171);
            AcadTransaction.AddNewlyCreatedDBObject(cline, true);
        }

        //generic secondary support 
        private void GenCreateSecondarySupportBottom(BlockTableRecord acadBlockTableRecord, Transaction AcadTransaction, Database acadDatabase, double centerX, double centerY, double length, double height, double ht_frm_cen, SecThick secthik, double thickness = 100)
        {
            var line = new Polyline();
            //double height1 = prmsupport.Boundingboxmax.Z - prmsupport.Boundingboxmin.Z;
            Point2d Pt2D1 = new Point2d(centerX - length / 2, centerY - ht_frm_cen);
            line.AddVertexAt(0, Pt2D1, 0, 0, 0);
            Pt2D1 = new Point2d(centerX + length / 2, centerY - ht_frm_cen);
            line.AddVertexAt(1, Pt2D1, 0, 0, 0);
            Pt2D1 = new Point2d(centerX + length / 2, centerY - ht_frm_cen - height);
            line.AddVertexAt(2, Pt2D1, 0, 0, 0);

            //dimensioning
            RotatedDimension dim = new RotatedDimension(Math.PI / 2, new Point3d(centerX + length / 2, centerY - ht_frm_cen, 0), new Point3d(centerX + length / 2, centerY - ht_frm_cen - height, 0), new Point3d(centerX + length / 2 + 2500, centerY - ht_frm_cen - height, 0), "", ObjectId.Null);
            dim.Dimtxt = 100;
            dim.Dimasz = 150;
            dim.Dimclre = Color.FromColor(System.Drawing.Color.Cyan);
            dim.Dimclrt = Color.FromColor(System.Drawing.Color.Yellow);
            dim.Dimclrd = Color.FromColor(System.Drawing.Color.Cyan);
            dim.Dimtih = true;

            acadBlockTableRecord.AppendEntity(dim);
            AcadTransaction.AddNewlyCreatedDBObject(dim, true);

            Pt2D1 = new Point2d(centerX - length / 2, centerY - ht_frm_cen - height);
            line.AddVertexAt(3, Pt2D1, 0, 0, 0);
            Pt2D1 = new Point2d(centerX - length / 2, centerY - ht_frm_cen);
            line.AddVertexAt(4, Pt2D1, 0, 0, 0);
            line.Color = Color.FromColorIndex(ColorMethod.ByAci, 171);
            acadBlockTableRecord.AppendEntity(line);
            AcadTransaction.AddNewlyCreatedDBObject(line, true);

            thickness = 100;
            //if(secthik==SecThick.Both)
            //{
            //    LineDraw(acadBlockTableRecord, AcadTransaction,acadDatabase, new Point3d(centerX - length / 2 + thickness, centerY - ht_frm_cen, 0), new Point3d(centerX - length / 2 + thickness, centerY - ht_frm_cen - height, 0), MyCol.LightBlue);
            //    LineDraw(acadBlockTableRecord, AcadTransaction,acadDatabase, new Point3d(centerX + length / 2 - thickness, centerY - ht_frm_cen, 0), new Point3d(centerX + length / 2 - thickness, centerY - ht_frm_cen - height, 0), MyCol.LightBlue);
            //}

            switch (secthik)
            {
                case SecThick.HBoth:
                    // code block
                    LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, new Point3d(centerX - length / 2 + thickness, centerY - ht_frm_cen, 0), new Point3d(centerX - length / 2 + thickness, centerY - ht_frm_cen - height, 0), MyCol.LightBlue);
                    LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, new Point3d(centerX + length / 2 - thickness, centerY - ht_frm_cen, 0), new Point3d(centerX + length / 2 - thickness, centerY - ht_frm_cen - height, 0), MyCol.LightBlue);
                    break;
                case SecThick.HHidBoth:
                    // code block
                    LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, new Point3d(centerX - length / 2 + thickness, centerY - ht_frm_cen, 0), new Point3d(centerX - length / 2 + thickness, centerY - ht_frm_cen - height, 0), MyCol.Yellow, "DASHED");
                    LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, new Point3d(centerX + length / 2 - thickness, centerY - ht_frm_cen, 0), new Point3d(centerX + length / 2 - thickness, centerY - ht_frm_cen - height, 0), MyCol.Yellow, "DASHED");
                    break;
                case SecThick.Left:
                    // code block
                    LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, new Point3d(centerX - length / 2 + thickness, centerY - ht_frm_cen, 0), new Point3d(centerX - length / 2 + thickness, centerY - ht_frm_cen - height, 0), MyCol.LightBlue);
                    break;
                case SecThick.HidLeft:
                    // code block
                    LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, new Point3d(centerX - length / 2 + thickness, centerY - ht_frm_cen, 0), new Point3d(centerX - length / 2 + thickness, centerY - ht_frm_cen - height, 0), MyCol.Yellow, "DASHED");
                    break;
                case SecThick.Right:
                    // code block
                    LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, new Point3d(centerX + length / 2 - thickness, centerY - ht_frm_cen, 0), new Point3d(centerX + length / 2 - thickness, centerY - ht_frm_cen - height, 0), MyCol.LightBlue);
                    break;
                case SecThick.HidRight:
                    // code block
                    LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, new Point3d(centerX + length / 2 - thickness, centerY - ht_frm_cen, 0), new Point3d(centerX + length / 2 - thickness, centerY - ht_frm_cen - height, 0), MyCol.Yellow, "DASHED");
                    break;



                case SecThick.VBoth:
                    // code block
                    LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, new Point3d(centerX - length / 2, centerY - ht_frm_cen - thickness, 0), new Point3d(centerX - length / 2 + thickness, centerY - ht_frm_cen - height, 0), MyCol.LightBlue);
                    LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, new Point3d(centerX + length / 2 - thickness, centerY - ht_frm_cen, 0), new Point3d(centerX + length / 2 - thickness, centerY - ht_frm_cen - height, 0), MyCol.LightBlue);
                    break;
                case SecThick.VHidBoth:
                    // code block
                    LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, new Point3d(centerX - length / 2 + thickness, centerY - ht_frm_cen, 0), new Point3d(centerX - length / 2 + thickness, centerY - ht_frm_cen - height, 0), MyCol.Yellow, "DASHED");
                    LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, new Point3d(centerX + length / 2 - thickness, centerY - ht_frm_cen, 0), new Point3d(centerX + length / 2 - thickness, centerY - ht_frm_cen - height, 0), MyCol.Yellow, "DASHED");
                    break;
                case SecThick.Top:
                    // code block
                    LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, new Point3d(centerX - length / 2 + thickness, centerY - ht_frm_cen, 0), new Point3d(centerX - length / 2 + thickness, centerY - ht_frm_cen - height, 0), MyCol.LightBlue);
                    break;
                case SecThick.HidTop:
                    // code block
                    LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, new Point3d(centerX - length / 2 + thickness, centerY - ht_frm_cen, 0), new Point3d(centerX - length / 2 + thickness, centerY - ht_frm_cen - height, 0), MyCol.Yellow, "DASHED");
                    break;
                case SecThick.Bottom:
                    // code block
                    LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, new Point3d(centerX + length / 2 - thickness, centerY - ht_frm_cen, 0), new Point3d(centerX + length / 2 - thickness, centerY - ht_frm_cen - height, 0), MyCol.LightBlue);
                    break;
                case SecThick.HidBottom:
                    // code block
                    LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, new Point3d(centerX + length / 2 - thickness, centerY - ht_frm_cen, 0), new Point3d(centerX + length / 2 - thickness, centerY - ht_frm_cen - height, 0), MyCol.Yellow, "DASHED");
                    break;


            }

        }

        //generic secondary support(for both top bottom)
        private void BoxGenCreateSecondarySupportBottom(BlockTableRecord acadBlockTableRecord, Transaction AcadTransaction, Database acadDatabase, Point3d lefttop, Point3d righttop, Point3d rightbot, Point3d leftbot, SecThick secthik, [Optional] string ISMCTAGDir, double verti_tag_offset = 0, double sectionsize = 100)
        {

            LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, lefttop, righttop, MyCol.PaleTurquoise);
            LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, righttop, rightbot, MyCol.PaleTurquoise);
            LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, rightbot, leftbot, MyCol.PaleTurquoise);
            LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, leftbot, lefttop, MyCol.PaleTurquoise);

            //for ismc supp
            if (ISMCTAGDir == "L" || ISMCTAGDir == "l")
            {
                DrawLeader(acadBlockTableRecord, AcadTransaction, acadDatabase, "ISMC " + sectionsize, new Point3d(lefttop.X, (lefttop.Y + leftbot.Y) / 2 + verti_tag_offset, 0), new Point3d(lefttop.X - 2000, (lefttop.Y + leftbot.Y) / 2 + verti_tag_offset, 0));
            }
            else if (ISMCTAGDir == "R" || ISMCTAGDir == "r")
            {
                DrawLeader(acadBlockTableRecord, AcadTransaction, acadDatabase, "ISMC " + sectionsize, new Point3d(righttop.X, (righttop.Y + rightbot.Y) / 2 + verti_tag_offset, 0), new Point3d(righttop.X + 2000, (righttop.Y + rightbot.Y) / 2 + verti_tag_offset, 0));
            }
            //for circular supp
            else if (ISMCTAGDir == "NBL" || ISMCTAGDir == "nbl")
            {
                DrawLeader(acadBlockTableRecord, AcadTransaction, acadDatabase, sectionsize + " NB", new Point3d(lefttop.X, (lefttop.Y + leftbot.Y) / 2 + verti_tag_offset, 0), new Point3d(lefttop.X - 2000, (lefttop.Y + leftbot.Y) / 2 + verti_tag_offset, 0));
            }
            else if (ISMCTAGDir == "NBR" || ISMCTAGDir == "nbr")
            {
                DrawLeader(acadBlockTableRecord, AcadTransaction, acadDatabase, sectionsize + " NB", new Point3d(righttop.X, (righttop.Y + rightbot.Y) / 2 + verti_tag_offset, 0), new Point3d(righttop.X + 2000, (righttop.Y + rightbot.Y) / 2 + verti_tag_offset, 0));
            }
            //for plate
            else if (ISMCTAGDir == "PL" || ISMCTAGDir == "pl")
            {
                DrawLeader(acadBlockTableRecord, AcadTransaction, acadDatabase, sectionsize + " NB", new Point3d(lefttop.X, (lefttop.Y + leftbot.Y) / 2 + verti_tag_offset, 0), new Point3d(lefttop.X - 2000, (lefttop.Y + leftbot.Y) / 2 + verti_tag_offset, 0));
            }
            else if (ISMCTAGDir == "PR" || ISMCTAGDir == "pr")
            {
                DrawLeader(acadBlockTableRecord, AcadTransaction, acadDatabase, sectionsize + " NB", new Point3d(righttop.X, (righttop.Y + rightbot.Y) / 2 + verti_tag_offset, 0), new Point3d(righttop.X + 2000, (righttop.Y + rightbot.Y) / 2 + verti_tag_offset, 0));
            }


            double thickness = 100;
            //if(secthik==SecThick.Both)
            //{
            //    LineDraw(acadBlockTableRecord, AcadTransaction,acadDatabase, new Point3d(centerX - length / 2 + thickness, centerY - ht_frm_cen, 0), new Point3d(centerX - length / 2 + thickness, centerY - ht_frm_cen - height, 0), MyCol.LightBlue);
            //    LineDraw(acadBlockTableRecord, AcadTransaction,acadDatabase, new Point3d(centerX + length / 2 - thickness, centerY - ht_frm_cen, 0), new Point3d(centerX + length / 2 - thickness, centerY - ht_frm_cen - height, 0), MyCol.LightBlue);
            //}

            switch (secthik)
            {
                case SecThick.VBoth:
                    // code block
                    LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, new Point3d(lefttop.X + thickness, lefttop.Y, 0), new Point3d(leftbot.X + thickness, leftbot.Y, 0), MyCol.LightBlue);
                    LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, new Point3d(righttop.X - thickness, righttop.Y, 0), new Point3d(rightbot.X - thickness, rightbot.Y, 0), MyCol.LightBlue);
                    break;
                case SecThick.VHidBoth:
                    // code block
                    LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, new Point3d(lefttop.X + thickness, lefttop.Y, 0), new Point3d(leftbot.X + thickness, leftbot.Y, 0), MyCol.LightBlue, "DASHED");
                    LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, new Point3d(righttop.X - thickness, righttop.Y, 0), new Point3d(rightbot.X - thickness, rightbot.Y, 0), MyCol.LightBlue, "DASHED");
                    break;
                case SecThick.Left:
                    // code block
                    LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, new Point3d(lefttop.X + thickness, lefttop.Y, 0), new Point3d(leftbot.X + thickness, leftbot.Y, 0), MyCol.LightBlue);
                    break;
                case SecThick.HidLeft:
                    // code block
                    LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, new Point3d(lefttop.X + thickness, lefttop.Y, 0), new Point3d(leftbot.X + thickness, leftbot.Y, 0), MyCol.LightBlue, "DASHED");
                    break;
                case SecThick.Right:
                    // code block
                    LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, new Point3d(righttop.X - thickness, righttop.Y, 0), new Point3d(rightbot.X - thickness, rightbot.Y, 0), MyCol.LightBlue);
                    break;
                case SecThick.HidRight:
                    // code block
                    LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, new Point3d(righttop.X - thickness, righttop.Y, 0), new Point3d(rightbot.X - thickness, rightbot.Y, 0), MyCol.LightBlue, "DASHED");
                    break;



                case SecThick.HBoth:
                    // code block
                    LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, new Point3d(lefttop.X, lefttop.Y - thickness, 0), new Point3d(righttop.X, righttop.Y - thickness, 0), MyCol.LightBlue);
                    LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, new Point3d(leftbot.X, leftbot.Y + thickness, 0), new Point3d(rightbot.X, rightbot.Y + thickness, 0), MyCol.LightBlue);
                    break;
                case SecThick.HHidBoth:
                    // code block
                    LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, new Point3d(lefttop.X, lefttop.Y - thickness, 0), new Point3d(righttop.X, righttop.Y - thickness, 0), MyCol.LightBlue, "DASHED");
                    LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, new Point3d(leftbot.X, leftbot.Y + thickness, 0), new Point3d(rightbot.X, rightbot.Y + thickness, 0), MyCol.LightBlue, "DASHED");
                    break;
                case SecThick.Top:
                    // code block
                    LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, new Point3d(lefttop.X, lefttop.Y - thickness, 0), new Point3d(righttop.X, righttop.Y - thickness, 0), MyCol.LightBlue);
                    break;
                case SecThick.HidTop:
                    // code block
                    LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, new Point3d(lefttop.X, lefttop.Y - thickness, 0), new Point3d(righttop.X, righttop.Y - thickness, 0), MyCol.LightBlue, "DASHED");
                    break;
                case SecThick.Bottom:
                    // code block
                    LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, new Point3d(leftbot.X, leftbot.Y + thickness, 0), new Point3d(rightbot.X, rightbot.Y + thickness, 0), MyCol.LightBlue);
                    break;
                case SecThick.HidBottom:
                    // code block
                    LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, new Point3d(leftbot.X, leftbot.Y + thickness, 0), new Point3d(rightbot.X, rightbot.Y + thickness, 0), MyCol.LightBlue, "DASHED");
                    break;


            }

        }

        //aligh dimensioning
        public void CreateAlighDimen(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Point3d startpt, Point3d endpt, [Optional] string dimtxt, double offsetdist = -2000)
        {

            Line parallelline = CreateParallelLine(startpt, endpt, offsetdist);
            //dimensioning
            AlignedDimension align = new AlignedDimension(startpt, endpt, parallelline.EndPoint, "", ObjectId.Null);

            align.Dimtxt = 100;
            align.Dimasz = 150;

            try
            {
                align.DimensionText = Math.Round(Convert.ToDouble(dimtxt)).ToString();
            }
            catch (Exception)
            {
                align.DimensionText = dimtxt;
            }

            align.Dimdec = 2;
            align.Dimclre = Color.FromColor(System.Drawing.Color.Cyan);
            align.Dimclrt = Color.FromColor(System.Drawing.Color.Yellow);
            align.Dimclrd = Color.FromColor(System.Drawing.Color.Cyan);
            align.Dimtih = true;

            AcadBlockTableRecord.AppendEntity(align);
            AcadTransaction.AddNewlyCreatedDBObject(align, true);
        }

        //create parallel lines
        public Line CreateParallelLine(Point3d startpt, Point3d endpt, double offset)
        {

            var xDifference = startpt.X - endpt.X;
            var yDifference = startpt.Y - endpt.Y;
            var length = Math.Sqrt(Math.Pow(xDifference, 2) + Math.Pow(yDifference, 2));


            var X1 = (float)(startpt.X - offset * yDifference / length);
            var X2 = (float)(endpt.X - offset * yDifference / length);
            var Y1 = (float)(startpt.Y + offset * xDifference / length);
            var Y2 = (float)(endpt.Y + offset * xDifference / length);

            Line parallelLine = new Line(new Point3d(X1, Y1, 0), new Point3d(X2, Y2, 0));

            return parallelLine;
        }

        //enum for secondary thickness logic
        public enum SecThick
        {
            Nothing,

            VBoth,
            VHidBoth,
            Left,
            HidLeft,
            Right,
            HidRight,

            HBoth,
            HHidBoth,
            Top,
            HidTop,
            Bottom,
            HidBottom
        }

        //for distance
        public double GetDist(Point3d strpt, Point3d endpt)
        {
            double dist = Math.Sqrt(Math.Pow(endpt.X - strpt.X, 2) + Math.Pow(endpt.Y - strpt.Y, 2) + Math.Pow(endpt.Z - strpt.Z, 2));
            return dist;
        }


        //enum for information
        public enum Defination
        {
            Prim_Radius,
            Prim_ht,
            Sec_ht_top,
            Sec_ht_bot,

            Sec_top_l,
            Sec_top_b,
            Sec_bot_l,
            Sec_bot_b,
            Concrete_l,
            Concrete_b,
            Plate_l,
            Plate_b,

            SecTopLT,
            SecTopRT,
            SecTopRB,
            SecTopLB,

            SecBotLT,
            SecBotRT,
            SecBotRB,
            SecBotLB



        }

        //for support name and quantity
        public void CreateSupportName(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, ref double tracex, double boxlen, double boxht, ref double spaceY, int i)
        {
            //support name and quantity
            double centerX = tracex + boxlen / 2;

            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - 1500, spaceY - boxht + 700 + 1100, 0), new Point3d(centerX + 1500, spaceY - boxht + 700 + 1100, 0), new Point3d(centerX + 1500, spaceY - boxht + 700, 0), new Point3d(centerX - 1500, spaceY - boxht + 700, 0), SecThick.Nothing);

            CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - 1500 + 1000, spaceY - boxht + 700 + 1100 - 100, 0), ListCentalSuppoData[i].Name, 350, MyCol.Red);

            //support quant
            int quant = ListCentalSuppoData[i].Quantity;

            CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - 1500 + 800, spaceY - boxht + 700 + 1100 - 600, 0), "QTY. " + quant + " NOS.", 250, MyCol.Green);
        }

        //for reference bock
        private void CreateRefBlock(BlockTableRecord acadBlockTableRecord, Transaction AcadTransaction, Database acadDatabase, Point3d lefttop, Point3d righttop, Point3d rightbot, Point3d leftbot, string boxorientation = "ver")
        {

            if (boxorientation.ToLower() == "hor")
            {
                LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, lefttop, new Point3d(lefttop.X, (lefttop.Y + righttop.Y) / 2 - 250, lefttop.Z), MyCol.PaleTurquoise);
                LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, new Point3d(lefttop.X, (lefttop.Y + righttop.Y) / 2 - 250, lefttop.Z), new Point3d(lefttop.X, (lefttop.Y + righttop.Y) / 2 + 250, lefttop.Z), MyCol.PaleTurquoise, "ZIGZAG", 500);
                LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, new Point3d(lefttop.X, (lefttop.Y + righttop.Y) / 2 + 250, lefttop.Z), righttop, MyCol.PaleTurquoise);
            }
            else
            {
                LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, lefttop, new Point3d((lefttop.X + righttop.X) / 2 - 250, lefttop.Y, lefttop.Z), MyCol.PaleTurquoise);
                LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, new Point3d((lefttop.X + righttop.X) / 2 - 250, lefttop.Y, lefttop.Z), new Point3d((lefttop.X + righttop.X) / 2 + 250, lefttop.Y, lefttop.Z), MyCol.PaleTurquoise, "ZIGZAG", 500);
                LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, new Point3d((lefttop.X + righttop.X) / 2 + 250, lefttop.Y, lefttop.Z), righttop, MyCol.PaleTurquoise);
            }



            LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, righttop, rightbot, MyCol.PaleTurquoise, "DASHED", 500);


            if (boxorientation.ToLower() == "hor")
            {
                LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, rightbot, new Point3d(leftbot.X
                    , (leftbot.Y + rightbot.Y) / 2 + 250, leftbot.Z), MyCol.PaleTurquoise);
                LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, new Point3d(leftbot.X
                    , (leftbot.Y + rightbot.Y) / 2 + 250, leftbot.Z), new Point3d(leftbot.X
                    , (leftbot.Y + rightbot.Y) / 2 - 250, leftbot.Z), MyCol.PaleTurquoise, "ZIGZAG", 500);
                LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, new Point3d(leftbot.X
                    , (leftbot.Y + rightbot.Y) / 2 - 250, leftbot.Z), leftbot, MyCol.PaleTurquoise);

            }
            else
            {
                LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, rightbot, new Point3d((lefttop.X + righttop.X) / 2 + 250, leftbot.Y, leftbot.Z), MyCol.PaleTurquoise);
                LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, new Point3d((lefttop.X + righttop.X) / 2 + 250, leftbot.Y, leftbot.Z), new Point3d((lefttop.X + righttop.X) / 2 - 250, leftbot.Y, leftbot.Z), MyCol.PaleTurquoise, "ZIGZAG", 500);
                LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, new Point3d((lefttop.X + righttop.X) / 2 - 250, leftbot.Y, leftbot.Z), leftbot, MyCol.PaleTurquoise);
            }

            LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, leftbot, lefttop, MyCol.PaleTurquoise, "DASHED", 500);
        }

        //create leader

        public void DrawLeader(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, string text, Point3d firstPoint, Point3d secondPoint, Point3d? thirdPoint = null)
        {

            // Create the MText annotation
            MText acMText = new MText();
            acMText.SetDatabaseDefaults();
            acMText.Contents = text;
            acMText.Location = secondPoint;

            if (thirdPoint.HasValue)
            {
                acMText.Location = thirdPoint.Value;
            }

            //acMText.Width = 2;
            acMText.TextHeight = 200;
            // Set text position to above
            acMText.Attachment = AttachmentPoint.TopCenter;
            //acMText.
            // Add the new object to Model space and the transaction
            AcadBlockTableRecord.AppendEntity(acMText);
            AcadTransaction.AddNewlyCreatedDBObject(acMText, true);
            //ObjectId objectIds1 = SymbolUtilityServices.GetBlockModelSpaceId(AcadDatabase);

            // objectIds1.Add(acMText.Id); 
            Leader acLdr = new Leader();
            acLdr.SetDatabaseDefaults();
            acLdr.AppendVertex(firstPoint);
            acLdr.AppendVertex(secondPoint);
            if (thirdPoint.HasValue)
            {
                acLdr.AppendVertex(thirdPoint.Value);
            }
            acLdr.HasArrowHead = true;
            acLdr.Dimasz = 150;


            TextStyleTable acTStyleTbl = AcadTransaction.GetObject(AcadDatabase.TextStyleTableId, OpenMode.ForRead) as TextStyleTable;

            DimStyleTable acDimStyleTbl = AcadTransaction.GetObject(AcadDatabase.DimStyleTableId, OpenMode.ForRead) as DimStyleTable;

            // Get the index of the dimension style named "ISO-25"
            //int iso25Index = acDimStyleTbl.FindIndex("ISO-25");

            // Get the dimension style at the specified index
            //ObjectId acDimStyleId = acDimStyleTbl[iso25Index];

            //ObjectId acDimStyleId = acDimStyleTbl["ISO-25"];

            //acLdr.TextStyleId = acDimStyleId;

            acLdr.Color = Autodesk.AutoCAD.Colors.Color.FromColor(System.Drawing.Color.Cyan);
            //acLdr.Dimclrd = Autodesk.AutoCAD.Colors.Color.FromColor(System.Drawing.Color.Green);                     //Add the new object to Model space and the transaction
            AcadBlockTableRecord.AppendEntity(acLdr);
            AcadTransaction.AddNewlyCreatedDBObject(acLdr, true);
            acLdr.Annotation = acMText.ObjectId;
            acLdr.EvaluateLeader();

        }



        //foot of perpendicular
        public static Point3d FindPerpendicularFoot(Pt3D destpt, double[] strpt, double[] endpt)
        {

            Vector3 segmentStart = new Vector3(Convert.ToSingle(strpt[0]), Convert.ToSingle(strpt[1]), Convert.ToSingle(strpt[2]));

            Vector3 segmentEnd = new Vector3(Convert.ToSingle(endpt[0]), Convert.ToSingle(endpt[1]), Convert.ToSingle(endpt[2]));

            Vector3 point = new Vector3(Convert.ToSingle(destpt.X), Convert.ToSingle(destpt.Y), Convert.ToSingle(destpt.Z));

            // Calculate the direction vector of the segment
            Vector3 segmentDirection = segmentEnd - segmentStart;

            // Calculate the vector from the start of the segment to the point
            Vector3 pointVector = point - segmentStart;

            // Calculate the projection of the point vector onto the segment direction vector
            float projection = Vector3.Dot(pointVector, segmentDirection) / segmentDirection.LengthSquared();

            // Clamp the projection to the range [0,1] to ensure the foot is on the segment
            //projection = Math. Clamp(projection, 0, 1);

            // Calculate the foot of the perpendicular
            Vector3 foot = segmentStart + projection * segmentDirection;

            Point3d result = new Point3d(foot.X, foot.Y, foot.Z);

            return result;
        }

        public static void ListDimStylesCommand()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                DimStyleTable dst = tr.GetObject(db.DimStyleTableId, OpenMode.ForRead) as DimStyleTable;

                if (dst == null)
                {
                    ed.WriteMessage("\nCould not get the dimension style table.");
                    return;
                }

                ed.WriteMessage("\nList of dimension styles:");
                foreach (ObjectId id in dst)
                {
                    DimStyleTableRecord ds = (DimStyleTableRecord)tr.GetObject(id, OpenMode.ForRead);
                    if (ds != null)
                    {
                        ed.WriteMessage("\n- {0}", ds.Name);
                    }
                }

                tr.Commit();
            }
        }


        //adding dimstyle iso-25
        public void AddDimStyleToDimensions()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            // Create an empty list to store dimensions
            ObjectIdCollection dimIds = new ObjectIdCollection();

            // Get all the dimensions in the drawing
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;

                foreach (ObjectId objId in btr)
                {
                    if ((objId.ObjectClass == RXClass.GetClass(typeof(AlignedDimension))) || (objId.ObjectClass == RXClass.GetClass(typeof(RotatedDimension))))
                    {
                        dimIds.Add(objId);
                    }
                }

                tr.Commit();
            }

            // Add the dimensions to the AllDimension list
            List<Dimension> AllDimension = new List<Dimension>();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (ObjectId dimId in dimIds)
                {
                    Dimension dim = tr.GetObject(dimId, OpenMode.ForWrite) as Dimension;
                    AllDimension.Add(dim);
                }
                tr.Commit();
            }

            // Add the "ISO-25" dimstyle to each dimension in the AllDimension list
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                // Get the "ISO-25" dimstyle to be applied
                DimStyleTable dst = tr.GetObject(db.DimStyleTableId, OpenMode.ForRead) as DimStyleTable;
                if (!dst.Has("ISO-25"))
                {

                    ed.WriteMessage("Dimstyle 'ISO-25' does not exist in the drawing. Please create it before running this command.");
                    return;
                }

                ObjectId iso25DimStyleId = dst["ISO-25"];


                // Apply the "ISO-25" dimstyle to each dimension in the AllDimension list
                foreach (Dimension dim in AllDimension)
                {
                    dim.DimensionStyle = iso25DimStyleId;
                    dim.Dimtxt = 1.5;
                    dim.Dimasz = 2.5;
                }
                tr.Commit();
            }

            ed.WriteMessage("Dimstyle 'ISO-25' added to all dimensions.");
        }

        public static int RoundUp(int value)
        {
            return 10 * ((value + 9) / 10);
        }

        public static double Roundnear5(double value)
        {
            return (int)(Math.Round(value / 5) * 5);
        }

        public double MillimetersToMeters(double millimeters)
        {
            return millimeters *= 0.001; ;
        }

        public string GettheISMC(string str)
        {
            Dictionary<string, string> DicISMCcode = new Dictionary<string, string>();
            DicISMCcode["ISMC75"] = "75X40";
            DicISMCcode["ISMC100"] = "100X50";
            DicISMCcode["ISMC125"] = "125X66";
            DicISMCcode["ISMC150"] = "150X75";
            DicISMCcode["ISMC150*"] = "150X76";
            DicISMCcode["ISMC175"] = "175X75";
            DicISMCcode["ISMC175*"] = "175X76";
            DicISMCcode["ISMC200"] = "200X75";
            DicISMCcode["ISMC200*"] = "200X76";
            DicISMCcode["ISMC225"] = "225X80";
            DicISMCcode["ISMC225*"] = "225X82";
            DicISMCcode["ISMC250"] = "250X80";
            DicISMCcode["ISMC250*"] = "250X82";
            DicISMCcode["ISMC250**"] = "250X83";
            DicISMCcode["ISMC300"] = "300X90";
            DicISMCcode["ISMC300*"] = "300X92";
            DicISMCcode["ISMC300**"] = "300X93";
            DicISMCcode["ISMC350"] = "350X100";
            DicISMCcode["ISMC400"] = "400X100";

            foreach (var Item in DicISMCcode)
            {
                if (str.ToUpper().Contains(Item.Value))
                {
                    return Item.Key;
                }
            }

            return "";
        }
        public double CSECTIONSIZE(string str)
        {
            if (str == null)
            {
                return 0;
            }
            char[] chars = { };
            StringBuilder sb = new StringBuilder();


            foreach (char ch in str)
            {
                if (ch == 'x' || ch == 'X')
                {
                    break;
                }
                if (char.IsDigit(ch))
                {
                    sb.Append(ch);
                }
            }


            string result = string.Join("", sb);
            return Convert.ToDouble(result);
        }

        public static double InchesToMm(double inches)
        {
            return inches * 25.4;
        }


        public void TOSLOC(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, double centerX, double centerY, string TotalHt, string position = "r", string HTtype = "TOS EL.(+)")
        {

            if (position.ToLower() == "l")
            {
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - 3500, centerY + 300, 0), HTtype + TotalHt);
                LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX, centerY, 0), new Point3d(centerX - 4000, centerY, 0), MyCol.LightBlue);
            }
            else
            {
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + 3500, centerY + 300, 0), HTtype + TotalHt);
                LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX, centerY, 0), new Point3d(centerX + 4000, centerY, 0), MyCol.LightBlue);
            }


        }

        Edgeinfo GetEdgeforplate(SupporSpecData PrimarySupport, SupporSpecData PlateSuppo)
        {
            Edgeinfo Edgeinfo = new Edgeinfo();
            double edgelenght = 0.0;
            System.Windows.Media.Media3D.Vector3D Vec1 = new System.Windows.Media.Media3D.Vector3D(1, 0, 0);
            System.Windows.Media.Media3D.Vector3D Vec2 = new System.Windows.Media.Media3D.Vector3D(0, 1, 0);
            System.Windows.Media.Media3D.Vector3D Vec3 = new System.Windows.Media.Media3D.Vector3D(1, 0, 0);



            Vec1.X = PrimarySupport.NoramlDir.X;
            Vec1.Y = PrimarySupport.NoramlDir.Y;
            Vec1.Z = PrimarySupport.NoramlDir.Z;



            foreach (var Face in PlateSuppo.ListfaceData)
            {
                Vec2.X = Face.FaceNormal.X;
                Vec2.Y = Face.FaceNormal.Y;
                Vec2.Z = Face.FaceNormal.Z;



                if (Math.Abs(Math.Round(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(Vec1, Vec2, Vec3)))).Equals(0) || Math.Abs(Math.Round(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(Vec1, Vec2, Vec3)))).Equals(180))
                {
                    foreach (var Edge in Face.ListlinearEdge)
                    {
                        if (Edge.EdgeLength > edgelenght)
                        {
                            edgelenght = Edge.EdgeLength;
                            Edgeinfo = Edge;
                        }
                    }



                    return Edgeinfo;
                }
            }



            return Edgeinfo;
        }

        public static double[] GetArrayAbsoluteValues(double[] arr)
        {
            double[] absArr = new double[arr.Length];

            for (int i = 0; i < arr.Length; i++)
            {
                absArr[i] = Math.Abs(arr[i]);
            }

            return absArr;
        }

        public static Pt3D Get3dAbsoluteValues(Pt3D arr)
        {
            arr.X = Math.Abs(arr.X);
            arr.Y = Math.Abs(arr.Y);
            arr.Z = Math.Abs(arr.Z);

            return arr;
        }


        public static double[] FindClosestPoint(double[] strpt, double[] endpt, Pt3D closestTO)
        {

            Vector3 pointA = new Vector3(Convert.ToSingle(strpt[0]), Convert.ToSingle(strpt[1]), Convert.ToSingle(strpt[2]));
            Vector3 pointB = new Vector3(Convert.ToSingle(endpt[0]), Convert.ToSingle(endpt[1]), Convert.ToSingle(endpt[2]));
            Vector3 targetPoint = new Vector3(Convert.ToSingle(closestTO.X), Convert.ToSingle(closestTO.Y), Convert.ToSingle(closestTO.Z));

            // Calculate the distances between each point and the target point
            float distanceToA = Vector3.Distance(pointA, targetPoint);
            float distanceToB = Vector3.Distance(pointB, targetPoint);

            // Determine which point is closer
            if (distanceToA < distanceToB)
            {
                double[] result = new double[] { pointA.X, pointA.Y, pointA.Z };
                return result;
            }
            else
            {
                double[] result = new double[] { pointB.X, pointB.Y, pointB.Z };
                return result;
            }
        }


        //support details
        [Obsolete]
        public void DRS2(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, ref double tracex, double boxlen, double boxht, ref double spaceY, Document Document2D, string SupType, [Optional] int i)
        {

            double upperYgap = 3500;
            double centerX = tracex + boxlen / 2;  // 9869.9480;
            double centerY = spaceY - upperYgap;
            //box boundaries
            //vertical line
            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex + boxlen, spaceY + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);

            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex, spaceY - boxht + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);

            var primsup_des = ListCentalSuppoData[i].ListPrimarySuppo.OrderByDescending(e => e.Centroid.Z);
            var secsupp_des = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection.ToLower() == "hor").OrderByDescending(e => e.Boundingboxmax.Z);


            string prim_height = "";
            try
            {
                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
                {
                    //height of prim from bottom

                    prim_height = Math.Round((MillimetersToMeters(ListCentalSuppoData[i].ListPrimarySuppo[0].PrimaryZhtNB)), 2).ToString();
                }

                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
                {
                    prim_height = Math.Round((MillimetersToMeters(ListCentalSuppoData[i].ListPrimarySuppo[0].Centroid.Z)), 2).ToString();
                }
            }
            catch (Exception)
            {

            }

            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
            {
                FixCreatePrimarySupportwithvertex(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY, 801.5625);

                //try
                //{
                //     //    prim_height = MillimetersToMeters(Convert.ToDouble(prim_height)).ToString();
                //}
                //catch (Exception)
                //{

                //}



                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] + 1500, centerY - 100, 0), "CL.EL.(+)" + prim_height);

            }

            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
            {
                double radius = 500;
                info[Defination.Prim_Radius] = radius;
                info[Defination.Prim_ht] = radius;

                CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Prim_block_U", centerX, centerY, radius);
                //height of prim from bottom
                // CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] - 1500, centerY - 100, 0), "CL.EL.(+)" + info[Defination.Prim_ht].ToString());

                //string clampname = ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName;
                //DrawLeader(AcadBlockTableRecord, AcadTransaction, AcadDatabase, clampname, new Point3d(centerX, centerY + 1.4 * radius, 0), new Point3d(centerX - 4 * radius, centerY + 1.4 * radius, 0));

                try
                {
                    //    prim_height = MillimetersToMeters(Convert.ToDouble(prim_height)).ToString();
                }
                catch (Exception)
                {

                }

                //CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] - 1500, centerY - 100, 0), "CL.EL.(+)" + prim_height);
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - info[Defination.Prim_Radius] - 2500, centerY - 100, 0), "CL.EL.(+)" + prim_height);
                LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - info[Defination.Prim_Radius], centerY, 0), new Point3d(centerX - info[Defination.Prim_Radius] - 3000, centerY, 0), MyCol.LightBlue);
                string clampname = ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName;
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - info[Defination.Prim_Radius] - 2500, centerY + 300, 0), clampname);

            }



            double height = 1000;
            double length = 3000;
            info[Defination.Sec_top_l] = length;
            info[Defination.Sec_top_b] = height;

            //upper secondary supp

            var section = secsupp_des.ElementAt(0).Size;

            var sectionsize = Convert.ToDouble(CSECTIONSIZE(section));

            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht] - height, 0), new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht] - height, 0), SecThick.HBoth, "R", 0, sectionsize);

            //info for future
            pointsinfo[Defination.SecTopLT] = new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht], 0);
            pointsinfo[Defination.SecTopRT] = new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht], 0);
            pointsinfo[Defination.SecTopRB] = new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht] - height, 0);
            pointsinfo[Defination.SecTopLB] = new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht] - height, 0);

            double l_dist_frm_centre = 0;
            double r_dist_frm_centre = 0;
            //dimensioning
            try
            {
                var strpt = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection.ToLower() == "hor").First().StPt;

                var endpt = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection.ToLower() == "hor").First().EndPt;

                // var midpt = ListCentalSuppoData[i].ListPrimarySuppo[0].Midpoint;
                var midpt = ListCentalSuppoData[i].ListPrimarySuppo[0].Centroid;

                Point3d projectedpt = FindPerpendicularFoot(midpt, strpt, endpt);

                l_dist_frm_centre = Math.Round(GetDist(new Point3d(strpt), projectedpt));
                r_dist_frm_centre = Math.Round(GetDist(new Point3d(endpt), projectedpt));
            }
            catch (Exception)
            {

            }


            //double strtmiddist=GetDist(ldist)

            CreateDimension(new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX, centerY + info[Defination.Prim_Radius], 0), l_dist_frm_centre.ToString());
            CreateDimension(new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX, centerY + info[Defination.Prim_Radius], 0), r_dist_frm_centre.ToString());


            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length / 2 + 4000, centerY - info[Defination.Prim_ht], 0), MyCol.LightBlue);

            // vertical dimensioning
            string TotalHt = "";
            //mtext


            try
            {
                TotalHt = Math.Round(MillimetersToMeters(ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection.ToLower() == "hor").First().Boundingboxmax.Z), 2).ToString();
            }
            catch (Exception)
            {
            }


            try
            {
                //TotalHt = MillimetersToMeters(Convert.ToDouble(TotalHt)).ToString();
            }
            catch (Exception)
            {

            }
            CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + 2500, centerY - info[Defination.Prim_ht] + 300, 0), "TOS EL.(+)" + TotalHt);

            info[Defination.Sec_ht_top] = info[Defination.Prim_ht] + height;

            height = 3000;
            length = 1000;

            //sec bot
            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length / 2, centerY - info[Defination.Sec_ht_top], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top] - height, 0), new Point3d(centerX - length / 2, centerY - info[Defination.Sec_ht_top] - height, 0), SecThick.VBoth, "l");

            //info for future
            pointsinfo[Defination.SecBotLT] = new Point3d(centerX - length / 2, centerY - info[Defination.Sec_ht_top], 0);
            pointsinfo[Defination.SecBotRT] = new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top], 0);
            pointsinfo[Defination.SecBotRB] = new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top] - height, 0);
            pointsinfo[Defination.SecBotLB] = new Point3d(centerX - length / 2, centerY - info[Defination.Sec_ht_top] - height, 0);

            //dimensioning
            var botsec = "";
            try
            {
                botsec = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.BoxData.Z == ListCentalSuppoData[i].ListSecondrySuppo.Max(s => s.BoxData.Z)).First().BoxData.Z.ToString();
            }
            catch (Exception)
            {

            }


            CreateAlighDimen(AcadBlockTableRecord, AcadTransaction, new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top] - height, 0), botsec);

            info[Defination.Sec_ht_bot] = info[Defination.Sec_ht_top] + height;

            height = 1000;
            length = 3000;
            info[Defination.Concrete_l] = length;
            info[Defination.Concrete_b] = height;

            if (ListCentalSuppoData[i].ListConcreteData.Count > 0)
            {
                FixCreateBottomSupportTopType2(Document2D, AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY, height, length, i);

            }



            //support name and quantity
            CreateSupportName(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, i);


            tracex += boxlen;

        }

        public void DRS3(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, ref double tracex, double boxlen, double boxht, ref double spaceY, Document Document2D, string SupType, [Optional] int i)
        {
            double upperYgap = 3500;

            double centerX = tracex + boxlen / 2;  // 9869.9480;
            double centerY = spaceY - upperYgap;
            //box boundaries
            //vertical line
            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex + boxlen, spaceY + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);


            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex, spaceY - boxht + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);

            var primsup_des = ListCentalSuppoData[i].ListPrimarySuppo.OrderByDescending(e => e.Centroid.Z);
            var secsupp_des = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection.ToLower() == "hor").OrderByDescending(e => e.Boundingboxmax.Z);

            string prim_height = "";
            try
            {
                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
                {
                    //height of prim from bottom

                    prim_height = Math.Round((MillimetersToMeters(ListCentalSuppoData[i].ListPrimarySuppo[0].PrimaryZhtNB)), 2).ToString();
                }

                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
                {
                    prim_height = Math.Round((MillimetersToMeters(ListCentalSuppoData[i].ListPrimarySuppo[0].Centroid.Z)), 2).ToString();
                }
            }
            catch (Exception)
            {

            }


            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
            {
                FixCreatePrimarySupportwithvertex(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY, 801.5625);
                //height of prim from bottom
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] + 1500, centerY - 100, 0), "CL.EL.(+)" + info[Defination.Prim_ht].ToString());

            }

            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
            {
                double radius = 500;
                info[Defination.Prim_Radius] = radius;
                info[Defination.Prim_ht] = radius;

                CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Prim_block_U", centerX, centerY, radius);
                //height of prim from bottom
                // CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] - 1500, centerY - 100, 0), "CL.EL.(+)" + info[Defination.Prim_ht].ToString());

                //string clampname = ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName;
                //DrawLeader(AcadBlockTableRecord, AcadTransaction, AcadDatabase, clampname, new Point3d(centerX, centerY + 1.4 * radius, 0), new Point3d(centerX - 4 * radius, centerY + 1.4 * radius, 0));
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - info[Defination.Prim_Radius] - 2500, centerY - 100, 0), "CL.EL.(+)" + prim_height);
                LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - info[Defination.Prim_Radius], centerY, 0), new Point3d(centerX - info[Defination.Prim_Radius] - 3000, centerY, 0), MyCol.LightBlue);
                string clampname = ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName;
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - info[Defination.Prim_Radius] - 2500, centerY + 300, 0), clampname);

            }



            //double ht_frm_cen = 1220.7383;

            double height = 1000;
            double length = 3000;
            info[Defination.Sec_top_l] = length;
            info[Defination.Sec_top_b] = height;

            var section = secsupp_des.ElementAt(0).Size;

            var sectionsize = Convert.ToDouble(CSECTIONSIZE(section));

            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length * 0.66, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length * 0.34, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length * 0.34, centerY - info[Defination.Prim_ht] - height, 0), new Point3d(centerX - length * 0.66, centerY - info[Defination.Prim_ht] - height, 0), SecThick.HBoth, "R", 0, sectionsize);

            //info for future
            pointsinfo[Defination.SecTopLT] = new Point3d(centerX - length * 0.66, centerY - info[Defination.Prim_ht], 0);
            pointsinfo[Defination.SecTopRT] = new Point3d(centerX + length * 0.34, centerY - info[Defination.Prim_ht], 0);
            pointsinfo[Defination.SecTopRB] = new Point3d(centerX + length * 0.34, centerY - info[Defination.Prim_ht] - height, 0);
            pointsinfo[Defination.SecTopLB] = new Point3d(centerX - length * 0.66, centerY - info[Defination.Prim_ht] - height, 0);

            double l_dist_frm_centre = 0;
            double r_dist_frm_centre = 0;
            //dimensioning
            try
            {
                var strpt = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection.ToLower() == "hor").First().StPt;

                var endpt = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection.ToLower() == "hor").First().EndPt;

                // var midpt = ListCentalSuppoData[i].ListPrimarySuppo[0].Midpoint;
                var midpt = ListCentalSuppoData[i].ListPrimarySuppo[0].Centroid;

                Point3d projectedpt = FindPerpendicularFoot(midpt, strpt, endpt);

                l_dist_frm_centre = Math.Round(GetDist(new Point3d(strpt), projectedpt));
                r_dist_frm_centre = Math.Round(GetDist(new Point3d(endpt), projectedpt));
            }
            catch (Exception)
            {

            }

            //dimensioning
            CreateDimension(new Point3d(centerX - length * 0.66, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX, centerY + info[Defination.Prim_Radius], 0), l_dist_frm_centre.ToString());
            CreateDimension(new Point3d(centerX + length * 0.34, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX, centerY + info[Defination.Prim_Radius], 0), r_dist_frm_centre.ToString());

            // vertical dimensioning
            string TotalHt = "";
            //mtext


            try
            {
                TotalHt = Math.Round(MillimetersToMeters(ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection.ToLower() == "hor").First().Boundingboxmax.Z), 2).ToString();
            }
            catch (Exception)
            {
            }


            try
            {
                //TotalHt = MillimetersToMeters(Convert.ToDouble(TotalHt)).ToString();
            }
            catch (Exception)
            {

            }
            CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + 2500, centerY - info[Defination.Prim_ht] + 300, 0), "TOS EL.(+)" + TotalHt);

            info[Defination.Sec_ht_top] = info[Defination.Prim_ht] + height;
            //slant sec support
            height = 1000;
            length = 3000;
            info[Defination.Sec_bot_l] = length;
            info[Defination.Sec_bot_b] = height;
            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length * 0.66, centerY - info[Defination.Sec_ht_top] - 1000, 0), new Point3d(centerX - length * 0.66 + 1737.7464, centerY - info[Defination.Sec_ht_top], 0), new Point3d(centerX - length * 0.66 + 1737.7464 + 1000, centerY - info[Defination.Sec_ht_top], 0), new Point3d(centerX - length * 0.66, centerY - info[Defination.Sec_ht_top] - 1000 - 571.1660, 0), SecThick.Nothing, "L");

            //info for future
            pointsinfo[Defination.SecBotLT] = new Point3d(centerX - length * 0.66, centerY - info[Defination.Sec_ht_top] - 1000, 0);
            pointsinfo[Defination.SecBotRT] = new Point3d(centerX - length * 0.66 + 1737.7464, centerY - info[Defination.Sec_ht_top], 0);
            pointsinfo[Defination.SecBotRB] = new Point3d(centerX - length * 0.66 + 1737.7464 + 1000, centerY - info[Defination.Sec_ht_top], 0);
            pointsinfo[Defination.SecBotLB] = new Point3d(centerX - length * 0.66, centerY - info[Defination.Sec_ht_top] - 1000 - 571.1660, 0);

            //dimensioning
            var botsec = "";
            try
            {
                botsec = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection.ToLower().Contains("incline")).First().BoxData.Z.ToString();
            }
            catch (Exception)
            {

            }



            CreateAlighDimen(AcadBlockTableRecord, AcadTransaction, new Point3d(centerX - length * 0.66 + 1737.7464 + 1000, centerY - info[Defination.Sec_ht_top], 0), new Point3d(centerX - length * 0.66, centerY - info[Defination.Sec_ht_top] - 1000 - 571.1660, 0), botsec, -1000);

            //hori small dimen
            var botsec2 = "";
            try
            {
                botsec2 = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.BoxData.Z == ListCentalSuppoData[i].ListSecondrySuppo.Max(s => s.BoxData.Z)).First().BoxData.Z.ToString();

                var toppart = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection.ToLower() == "hor").First().EndPt;
                var anglepart = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection == "45Inclined").First().EndPt;

                sectionsize = CSECTIONSIZE(ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection == "45Inclined").First().Size);

                botsec2 = (toppart[0] - anglepart[0] - sectionsize / 2).ToString();
            }
            catch (Exception)
            {

            }


            CreateAlighDimen(AcadBlockTableRecord, AcadTransaction, new Point3d(centerX + length * 0.34, centerY - info[Defination.Prim_ht] - height, 0), new Point3d(centerX - length * 0.66 + 1737.7464 + 1000, centerY - info[Defination.Sec_ht_top], 0), botsec2, -700);

            //inside lines
            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length * 0.66, centerY - info[Defination.Sec_ht_top] - 1000 - 115, 0), new Point3d(centerX - length * 0.66 + 1737.7464 + 200, centerY - info[Defination.Sec_ht_top], 0), MyCol.Yellow, "Dashed");

            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length * 0.66 + 1737.7464 + 1000 - 200, centerY - info[Defination.Sec_ht_top], 0), new Point3d(centerX - length * 0.66, centerY - info[Defination.Sec_ht_top] - 1000 - 571.1660 + 115, 0), MyCol.Yellow, "Dashed");

            //support name and quantity
            CreateSupportName(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, i);

            //ref block
            pointsextrainfo["RefLT"] = new Point3d(pointsinfo[Defination.SecTopLT].X - 1500, pointsinfo[Defination.SecTopLT].Y + 1000, 0);
            pointsextrainfo["RefRT"] = new Point3d(pointsinfo[Defination.SecTopLT].X, pointsinfo[Defination.SecTopLT].Y + 1000, 0);
            pointsextrainfo["RefRB"] = new Point3d(pointsinfo[Defination.SecBotLB].X, pointsinfo[Defination.SecBotLB].Y - 1000, 0);
            pointsextrainfo["RefLB"] = new Point3d(pointsinfo[Defination.SecBotLB].X - 1500, pointsinfo[Defination.SecBotLB].Y - 1000, 0);

            CreateRefBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, pointsextrainfo["RefLT"], pointsextrainfo["RefRT"], pointsextrainfo["RefRB"], pointsextrainfo["RefLB"]);
            DrawLeader(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Structure/Block", new Point3d(pointsextrainfo["RefLT"].X, (pointsextrainfo["RefLT"].Y + pointsextrainfo["RefLB"].Y) / 2, 0), new Point3d(pointsextrainfo["RefLT"].X - 1000, (pointsextrainfo["RefLT"].Y + pointsextrainfo["RefLB"].Y) / 2, 0));

            tracex += boxlen;

        }

        [Obsolete]
        public void DRS4(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, ref double tracex, double boxlen, double boxht, ref double spaceY, Document Document2D, string SupType, [Optional] int i)
        {
            double upperYgap = 3500;

            double centerX = tracex + boxlen / 2;  // 9869.9480;
            double centerY = spaceY - upperYgap;
            //box boundaries
            //vertical line
            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex + boxlen, spaceY + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);

            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex, spaceY - boxht + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);

            var primsup_des = ListCentalSuppoData[i].ListPrimarySuppo.OrderByDescending(e => e.Centroid.Z);
            var secsupp_des = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection.ToLower() == "hor").OrderByDescending(e => e.Boundingboxmax.Z);

            string prim_height = "";
            try
            {
                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
                {
                    //height of prim from bottom

                    prim_height = Math.Round((MillimetersToMeters(ListCentalSuppoData[i].ListPrimarySuppo[0].PrimaryZhtNB)), 2).ToString();
                }

                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
                {
                    prim_height = Math.Round((MillimetersToMeters(ListCentalSuppoData[i].ListPrimarySuppo[0].Centroid.Z)), 2).ToString();
                }
            }
            catch (Exception)
            {

            }
            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
            {
                FixCreatePrimarySupportwithvertex(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY, 801.5625);
                //height of prim from bottom
                try
                {
                    //    prim_height = MillimetersToMeters(Convert.ToDouble(prim_height)).ToString();
                }
                catch (Exception)
                {

                }
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] + 1500, centerY - 100, 0), "CL.EL.(+)" + prim_height);

            }

            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
            {
                double radius = 500;
                info[Defination.Prim_Radius] = radius;
                info[Defination.Prim_ht] = radius;

                CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Prim_block_U", centerX, centerY, radius);

                // string clampname = ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName;
                // DrawLeader(AcadBlockTableRecord, AcadTransaction, AcadDatabase, clampname, new Point3d(centerX, centerY + 1.4 * radius, 0), new Point3d(centerX - 4 * radius, centerY + 1.4 * radius, 0));

                //height of prim from bottom
                try
                {
                    //    prim_height = MillimetersToMeters(Convert.ToDouble(prim_height)).ToString();
                }
                catch (Exception)
                {

                }
                //CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] - 1500, centerY - 100, 0), "CL.EL.(+)" + prim_height);
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - info[Defination.Prim_Radius] - 2500, centerY - 100, 0), "CL.EL.(+)" + prim_height);
                LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - info[Defination.Prim_Radius], centerY, 0), new Point3d(centerX - info[Defination.Prim_Radius] - 3000, centerY, 0), MyCol.LightBlue);
                string clampname = ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName;
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - info[Defination.Prim_Radius] - 2500, centerY + 300, 0), clampname);

            }


            double height = 1000;
            double length = 3000;
            //double ht_frm_cen = 1220.7383;

            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length * 0.66, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length * 0.34, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length * 0.34, centerY - info[Defination.Prim_ht] - height, 0), new Point3d(centerX - length * 0.66, centerY - info[Defination.Prim_ht] - height, 0), SecThick.HBoth, "R");

            double l_dist_frm_centre = 0;
            double r_dist_frm_centre = 0;
            //dimensioning
            try
            {
                var strpt = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection == "Hor").First().StPt;

                var endpt = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection == "Hor").First().EndPt;

                // var midpt = ListCentalSuppoData[i].ListPrimarySuppo[0].Midpoint;
                var midpt = ListCentalSuppoData[i].ListPrimarySuppo[0].Centroid;

                Point3d projectedpt = FindPerpendicularFoot(midpt, strpt, endpt);

                l_dist_frm_centre = Math.Round(GetDist(new Point3d(strpt), projectedpt));
                r_dist_frm_centre = Math.Round(GetDist(new Point3d(endpt), projectedpt));
            }
            catch (Exception)
            {

            }


            //dimensioning
            CreateDimension(new Point3d(centerX - length * 0.66, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX, centerY + info[Defination.Prim_Radius], 0), l_dist_frm_centre.ToString());
            CreateDimension(new Point3d(centerX + length * 0.34, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX, centerY + info[Defination.Prim_Radius], 0), r_dist_frm_centre.ToString());


            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + length * 0.34, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length * 0.34 + 4000, centerY - info[Defination.Prim_ht], 0), MyCol.LightBlue);


            string TotalHt = "";
            //mtext
            try
            {
                TotalHt = Math.Round(MillimetersToMeters(ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection.ToLower() == "hor").First().Boundingboxmax.Z), 2).ToString();
            }
            catch (Exception)
            {
            }

            try
            {
                //TotalHt = MillimetersToMeters(Convert.ToDouble(TotalHt)).ToString();
            }
            catch (Exception)
            {

            }

            CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + 2500, centerY - info[Defination.Prim_ht] + 300, 0), "TOS EL.(+)" + TotalHt);

            info[Defination.Sec_ht_top] = info[Defination.Prim_ht] + height;

            height = 3000;
            length = 1000;
            info[Defination.Sec_ht_bot] = info[Defination.Sec_ht_top] + height;
            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - height * 0.66, centerY - info[Defination.Sec_ht_top], 0), new Point3d(centerX - height * 0.66 + length, centerY - info[Defination.Sec_ht_top], 0), new Point3d(centerX - height * 0.66 + length, centerY - info[Defination.Sec_ht_top] - height, 0), new Point3d(centerX - height * 0.66, centerY - info[Defination.Sec_ht_top] - height, 0), SecThick.VBoth, "l");

            //dimensioning
            var botsec = "";
            try
            {
                botsec = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.BoxData.Z == ListCentalSuppoData[i].ListSecondrySuppo.Max(s => s.BoxData.Z)).First().BoxData.Z.ToString();
            }
            catch (Exception)
            {

            }



            CreateAlighDimen(AcadBlockTableRecord, AcadTransaction, new Point3d(centerX - height * 0.66 + length, centerY - info[Defination.Sec_ht_top], 0), new Point3d(centerX - height * 0.66 + length, centerY - info[Defination.Sec_ht_top] - height, 0), botsec);

            height = 1000;
            length = 3000;
            info[Defination.Concrete_l] = length;
            info[Defination.Concrete_b] = height;
            //FixCreateSecondarySupportTop(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY);
            FixCreateBottomSupportTopType2(Document2D, AcadBlockTableRecord, AcadTransaction, AcadDatabase, (centerX - length * 0.66 + centerX - length * 0.66 + height) / 2, centerY, height, length, i);


            ////hori small dimen
            //var botsec2 = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.BoxData.Z == ListCentalSuppoData[i].ListSecondrySuppo.Max(s => s.BoxData.Z)).First().BoxData.Z.ToString();

            //CreateAlighDimen(AcadBlockTableRecord, AcadTransaction, new Point3d(centerX + length * 0.34, centerY - info[Defination.Prim_ht] - height, 0), new Point3d(centerX - length * 0.66 + 1737.7464 + 1000, centerY - info[Defination.Sec_ht_top], 0), botsec2);

            //LineDraw(acadBlockTableRecord, AcadTransaction,acadDatabase, new Point3d(centerX - length * 0.66, centerY - info[Defination.Sec_ht_top] - 1000 - 115, 0), new Point3d(centerX - length * 0.66 + 1737.7464 + 200, centerY - info[Defination.Sec_ht_top], 0), MyCol.Yellow, "Dashed");

            //LineDraw(acadBlockTableRecord, AcadTransaction,acadDatabase, new Point3d(centerX - length * 0.66 + 1737.7464 + 1000 - 200, centerY - info[Defination.Sec_ht_top], 0), new Point3d(centerX - length * 0.66, centerY - info[Defination.Sec_ht_top] - 1000 - 571.1660 + 115, 0), MyCol.Yellow, "Dashed");

            //support name and quantity
            CreateSupportName(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, i);

            tracex += boxlen;
        }

        [Obsolete]
        public void DRS5(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, ref double tracex, double boxlen, double boxht, ref double spaceY, Document Document2D, string SupType, [Optional] int i)
        {
            double upperYgap = 3500;

            double centerX = tracex + boxlen / 2;  // 9869.9480;
            double centerY = spaceY - upperYgap;
            //box boundaries
            //vertical line
            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex + boxlen, spaceY + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);

            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex, spaceY - boxht + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);

            var primsup_des = ListCentalSuppoData[i].ListPrimarySuppo.OrderByDescending(e => e.Centroid.Z);
            var secsupp_des = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection.ToLower() == "hor").OrderByDescending(e => e.Boundingboxmax.Z);

            string prim_height = "";
            try
            {
                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
                {
                    //height of prim from bottom

                    prim_height = Math.Round((MillimetersToMeters(ListCentalSuppoData[i].ListPrimarySuppo[0].PrimaryZhtNB)), 2).ToString();
                }

                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
                {
                    prim_height = Math.Round((MillimetersToMeters(ListCentalSuppoData[i].ListPrimarySuppo[0].Centroid.Z)), 2).ToString();
                }
            }
            catch (Exception)
            {

            }

            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
            {
                FixCreatePrimarySupportwithvertex(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY, 801.5625);
                //height of prim from bottom
                try
                {
                    //    prim_height = MillimetersToMeters(Convert.ToDouble(prim_height)).ToString();
                }
                catch (Exception)
                {

                }
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] + 1500, centerY - 100, 0), "CL.EL.(+)" + prim_height);


            }

            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
            {
                double radius = 500;
                info[Defination.Prim_Radius] = radius;
                info[Defination.Prim_ht] = radius;

                CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Prim_block_U", centerX, centerY, radius);


                // DrawLeader(AcadBlockTableRecord, AcadTransaction, AcadDatabase, clampname, new Point3d(centerX, centerY + 1.4 * radius, 0), new Point3d(centerX - 4 * radius, centerY + 1.4 * radius, 0));

                //height of prim from bottom
                try
                {
                    //    prim_height = MillimetersToMeters(Convert.ToDouble(prim_height)).ToString();
                }
                catch (Exception)
                {

                }
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - info[Defination.Prim_Radius] - 2500, centerY - 100, 0), "CL.EL.(+)" + prim_height);
                LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - info[Defination.Prim_Radius], centerY, 0), new Point3d(centerX - info[Defination.Prim_Radius] - 3000, centerY, 0), MyCol.LightBlue);
                string clampname = ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName;
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - info[Defination.Prim_Radius] - 2500, centerY + 300, 0), clampname);

            }


            double height = 1000;
            double length = 3000;
            //double ht_frm_cen = 1220.7383;

            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length * 0.66 + 1000, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length * 0.34, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length * 0.34, centerY - info[Defination.Prim_ht] - height, 0), new Point3d(centerX - length * 0.66 + 1000, centerY - info[Defination.Prim_ht] - height, 0), SecThick.HBoth, "R");

            double l_dist_frm_centre = 0;
            double r_dist_frm_centre = 0;
            //dimensioning
            try
            {
                var strpt = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection.ToLower() == "hor").First().StPt;

                var endpt = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection.ToLower() == "hor").First().EndPt;

                // var midpt = ListCentalSuppoData[i].ListPrimarySuppo[0].Midpoint;
                var midpt = ListCentalSuppoData[i].ListPrimarySuppo[0].Centroid;

                Point3d projectedpt = FindPerpendicularFoot(midpt, strpt, endpt);

                l_dist_frm_centre = Math.Round(GetDist(new Point3d(strpt), projectedpt));
                r_dist_frm_centre = Math.Round(GetDist(new Point3d(endpt), projectedpt));
            }
            catch (Exception)
            {

            }


            //dimensioning
            CreateDimension(new Point3d(centerX - length * 0.66 + 1000, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX, centerY + info[Defination.Prim_Radius], 0), l_dist_frm_centre.ToString());
            CreateDimension(new Point3d(centerX + length * 0.34, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX, centerY + info[Defination.Prim_Radius], 0), r_dist_frm_centre.ToString());


            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + length * 0.34, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length * 0.34 + 4000, centerY - info[Defination.Prim_ht], 0), MyCol.LightBlue);

            string TotalHt = "";

            //mtext
            try
            {
                TotalHt = Math.Round(MillimetersToMeters(ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection.ToLower() == "hor").First().Boundingboxmax.Z), 2).ToString();
            }
            catch (Exception)
            {
            }

            try
            {
                //TotalHt = MillimetersToMeters(Convert.ToDouble(TotalHt)).ToString();
            }
            catch (Exception)
            {

            }

            CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + 2500, centerY - info[Defination.Prim_ht] + 300, 0), "TOS EL.(+)" + TotalHt);

            info[Defination.Sec_ht_top] = info[Defination.Prim_ht] + height;
            //bot sec supp
            height = 3000;
            length = 1000;
            info[Defination.Sec_ht_bot] = info[Defination.Sec_ht_top] + height;
            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - height * 0.66, centerY - info[Defination.Sec_ht_top] + 1000, 0), new Point3d(centerX - height * 0.66 + length, centerY - info[Defination.Sec_ht_top] + 1000, 0), new Point3d(centerX - height * 0.66 + length, centerY - info[Defination.Sec_ht_top] - height, 0), new Point3d(centerX - height * 0.66, centerY - info[Defination.Sec_ht_top] - height, 0), SecThick.VBoth, "R");

            //dimensioning
            var botsec = "";
            try
            {
                botsec = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.BoxData.Z == ListCentalSuppoData[i].ListSecondrySuppo.Max(s => s.BoxData.Z)).First().BoxData.Z.ToString();
            }
            catch (Exception)
            {

            }


            CreateAlighDimen(AcadBlockTableRecord, AcadTransaction, new Point3d(centerX - height * 0.66, centerY - info[Defination.Sec_ht_top] + 1000, 0), new Point3d(centerX - height * 0.66, centerY - info[Defination.Sec_ht_top] - height, 0), botsec, 1000);

            //concrete
            height = 1000;
            length = 3000;
            info[Defination.Concrete_l] = length;
            info[Defination.Concrete_b] = height;
            //FixCreateSecondarySupportTop(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY);
            FixCreateBottomSupportTopType2(Document2D, AcadBlockTableRecord, AcadTransaction, AcadDatabase, (centerX - length * 0.66 + centerX - length * 0.66 + height) / 2, centerY, height, length, i);


            //LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + length * 0.34, centerY - info[Defination.Sec_ht_bot] - length, 0), new Point3d(centerX + length * 0.34 + 4000, centerY - info[Defination.Sec_ht_bot] - length, 0), MyCol.LightBlue);

            ////hori small dimen
            //var botsec2 = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.BoxData.Z == ListCentalSuppoData[i].ListSecondrySuppo.Max(s => s.BoxData.Z)).First().BoxData.Z.ToString();

            //CreateAlighDimen(AcadBlockTableRecord, AcadTransaction, new Point3d(centerX + length * 0.34, centerY - info[Defination.Prim_ht] - height, 0), new Point3d(centerX - length * 0.66 + 1737.7464 + 1000, centerY - info[Defination.Sec_ht_top], 0), botsec2);

            //LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length * 0.66, centerY - info[Defination.Sec_ht_top] - 1000 - 115, 0), new Point3d(centerX - length * 0.66 + 1737.7464 + 200, centerY - info[Defination.Sec_ht_top], 0), MyCol.Yellow, "Dashed");

            //LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length * 0.66 + 1737.7464 + 1000 - 200, centerY - info[Defination.Sec_ht_top], 0), new Point3d(centerX - length * 0.66, centerY - info[Defination.Sec_ht_top] - 1000 - 571.1660 + 115, 0), MyCol.Yellow, "Dashed");

            //support name and quantity
            CreateSupportName(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, i);

            tracex += boxlen;


        }

        [Obsolete]
        public void DRS6(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, ref double tracex, double boxlen, double boxht, ref double spaceY, Document Document2D, string SupType, [Optional] int i)
        {
            double upperYgap = 2500;

            double centerX = tracex + boxlen / 2;  // 9869.9480;
            double centerY = spaceY - upperYgap;
            //box boundaries
            //vertical line
            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex + boxlen, spaceY + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);

            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex, spaceY - boxht + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);

            var primsup_des = ListCentalSuppoData[i].ListPrimarySuppo.OrderByDescending(e => e.Centroid.Z);
            var secsupp_des = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection.ToLower() == "hor").OrderByDescending(e => e.Boundingboxmax.Z);

            string prim_height = "";
            try
            {
                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
                {
                    //height of prim from bottom

                    prim_height = Math.Round((MillimetersToMeters(primsup_des.ElementAt(0).PrimaryZhtNB)), 2).ToString();
                }

                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
                {
                    prim_height = Math.Round((MillimetersToMeters(primsup_des.ElementAt(0).Centroid.Z)), 2).ToString();
                }
            }
            catch (Exception)
            {

            }

            //upper primary
            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
            {
                double radius = 801.5625;
                FixCreatePrimarySupportwithvertex(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX - 500, centerY, radius);
                //height of prim from bottom
                try
                {
                    //    prim_height = MillimetersToMeters(Convert.ToDouble(prim_height)).ToString();
                }
                catch (Exception)
                {

                }
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - 500 + info[Defination.Prim_Radius] + 1500, centerY - 100, 0), "CL.EL.(+)" + prim_height);

            }

            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
            {
                double radius = 500;
                info[Defination.Prim_Radius] = radius;
                info[Defination.Prim_ht] = radius;

                CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Prim_block_U", centerX - 500, centerY, radius);

                //DrawLeader(AcadBlockTableRecord, AcadTransaction, AcadDatabase, clampname, new Point3d(centerX - 500, centerY + 1.4 * radius, 0), new Point3d(centerX - 500 - 4 * radius, centerY + 1.4 * radius, 0));

                //height of prim from bottom
                try
                {
                    //    prim_height = MillimetersToMeters(Convert.ToDouble(prim_height)).ToString();
                }
                catch (Exception)
                {

                }
                //CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - 500 + info[Defination.Prim_Radius] - 1500, centerY - 100, 0), "CL.EL.(+)" + prim_height);
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - 500 + info[Defination.Prim_Radius] + 2500, centerY - 100, 0), "CL.EL.(+)" + prim_height);
                LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - 500 + info[Defination.Prim_Radius], centerY, 0), new Point3d(centerX - 500 + info[Defination.Prim_Radius] + 3000, centerY, 0), MyCol.LightBlue);
                string clampname = ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName;
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - 500 + info[Defination.Prim_Radius] + 2500, centerY + 300, 0), clampname);

            }


            double height = 1000;
            double length = 4000;

            info[Defination.Sec_top_l] = length;
            info[Defination.Sec_top_b] = height;
            //double ht_frm_cen = 1220.7383;
            var sectionsize = 0.0;
            try
            {
                sectionsize = Convert.ToDouble(CSECTIONSIZE(secsupp_des.ElementAt(0).Size));
            }
            catch (Exception)
            {

            }

            //upper top secondary
            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length * 0.66 + 1000, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length * 0.34, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length * 0.34, centerY - info[Defination.Prim_ht] - height, 0), new Point3d(centerX - length * 0.66 + 1000, centerY - info[Defination.Prim_ht] - height, 0), SecThick.HBoth, "L", 0, sectionsize);


            double l_dist_frm_centre = 0;
            double r_dist_frm_centre = 0;
            //dimensioning
            try
            {

                var maxz = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.Boundingboxmax.Z == ListCentalSuppoData[i].ListSecondrySuppo.Max(s => s.Boundingboxmax.Z));

                var strpt = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => (e.PartDirection.ToLower() == "hor") && (e.Boundingboxmax.Z == ListCentalSuppoData[i].ListSecondrySuppo.Max(s => s.Boundingboxmax.Z))).First().StPt;

                var endpt = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => (e.PartDirection.ToLower() == "hor") && (e.Boundingboxmax.Z == ListCentalSuppoData[i].ListSecondrySuppo.Max(s => s.Boundingboxmax.Z))).First().EndPt;

                // var midpt = ListCentalSuppoData[i].ListPrimarySuppo[0].Midpoint;
                var midpt = ListCentalSuppoData[i].ListPrimarySuppo[0].Centroid;

                Point3d projectedpt = FindPerpendicularFoot(midpt, strpt, endpt);

                l_dist_frm_centre = Math.Round(GetDist(new Point3d(strpt), projectedpt));
                r_dist_frm_centre = Math.Round(GetDist(new Point3d(endpt), projectedpt));
            }
            catch (Exception)
            {

            }


            //dimensioning
            CreateDimension(new Point3d(centerX - length * 0.66 + 1000, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX - 500, centerY + info[Defination.Prim_Radius], 0));
            CreateDimension(new Point3d(centerX + length * 0.34, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX - 500, centerY + info[Defination.Prim_Radius], 0));


            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + length * 0.34, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length * 0.34 + 4000, centerY - info[Defination.Prim_ht], 0), MyCol.LightBlue);




            string TotalHt = "";
            //mtext
            try
            {
                TotalHt = Math.Round(MillimetersToMeters(secsupp_des.ElementAt(0).Boundingboxmax.Z), 2).ToString();
            }
            catch (Exception)
            {
            }

            try
            {
                //TotalHt = MillimetersToMeters(Convert.ToDouble(TotalHt)).ToString();
            }
            catch (Exception)
            {

            }

            CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + 3500, centerY - info[Defination.Prim_ht] + 300, 0), "TOS EL.(+)" + TotalHt);

            info[Defination.Sec_ht_top] = info[Defination.Prim_ht] + height;

            height = 6000;
            length = 1000;
            info[Defination.Sec_bot_l] = length;
            info[Defination.Sec_bot_b] = height;

            info[Defination.Sec_ht_bot] = info[Defination.Sec_ht_top] + height - 1000;

            try
            {
                sectionsize = Convert.ToDouble(CSECTIONSIZE(ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection.ToLower() == "ver").First().Size));
            }
            catch (Exception)
            {

            }

            //secondary bottom

            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Sec_top_l] * 0.34, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + info[Defination.Sec_top_l] * 0.34 + info[Defination.Sec_bot_l], centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + info[Defination.Sec_top_l] * 0.34 + info[Defination.Sec_bot_l], centerY - info[Defination.Prim_ht] - info[Defination.Sec_bot_b], 0), new Point3d(centerX + info[Defination.Sec_top_l] * 0.34, centerY - info[Defination.Prim_ht] - info[Defination.Sec_bot_b], 0), SecThick.VBoth, "r", 2000);

            //dimensioning
            var botsec = "";
            try
            {
                botsec = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.BoxData.Z == ListCentalSuppoData[i].ListSecondrySuppo.Max(s => s.BoxData.Z)).First().BoxData.Z.ToString();
            }
            catch (Exception)
            {

            }



            CreateAlighDimen(AcadBlockTableRecord, AcadTransaction, new Point3d(centerX + info[Defination.Sec_top_l] * 0.34 + info[Defination.Sec_bot_l], centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + info[Defination.Sec_top_l] * 0.34 + info[Defination.Sec_bot_l], centerY - info[Defination.Prim_ht] - info[Defination.Sec_bot_b], 0), botsec);


            //lower sec top supp
            height = 1000;
            length = 4000;
            extrainfo["Sec_low_b"] = height;
            extrainfo["Sec_low_l"] = length;
            extrainfo["Sec_low_l"] = length + 500;


            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Sec_top_l] * 0.34 - extrainfo["Sec_low_l"], centerY - info[Defination.Prim_ht] - info[Defination.Sec_bot_b] + extrainfo["Sec_low_b"] + 500, 0), new Point3d(centerX + info[Defination.Sec_top_l] * 0.34, centerY - info[Defination.Prim_ht] - info[Defination.Sec_bot_b] + extrainfo["Sec_low_b"] + 500, 0), new Point3d(centerX + info[Defination.Sec_top_l] * 0.34, centerY - info[Defination.Prim_ht] - info[Defination.Sec_bot_b] + 500, 0), new Point3d(centerX + info[Defination.Sec_top_l] * 0.34 - extrainfo["Sec_low_l"], centerY - info[Defination.Prim_ht] - info[Defination.Sec_bot_b] + 500, 0), SecThick.HBoth, "L");

            //info for future
            pointsextrainfo["Sec_low_LT"] = new Point3d(centerX + info[Defination.Sec_top_l] * 0.34 - extrainfo["Sec_low_l"], centerY - info[Defination.Prim_ht] - info[Defination.Sec_bot_b] + extrainfo["Sec_low_b"] + 500, 0);
            pointsextrainfo["Sec_low_RT"] = new Point3d(centerX + info[Defination.Sec_top_l] * 0.34, centerY - info[Defination.Prim_ht] - info[Defination.Sec_bot_b] + extrainfo["Sec_low_b"] + 500, 0);
            pointsextrainfo["Sec_low_RB"] = new Point3d(centerX + info[Defination.Sec_top_l] * 0.34, centerY - info[Defination.Prim_ht] - info[Defination.Sec_bot_b] + 500, 0);
            pointsextrainfo["Sec_low_LB"] = new Point3d(centerX + info[Defination.Sec_top_l] * 0.34 - extrainfo["Sec_low_l"], centerY - info[Defination.Prim_ht] - info[Defination.Sec_bot_b] + 500, 0);

            //vertical sec dimensioning
            try
            {
                TotalHt = Math.Round(MillimetersToMeters(secsupp_des.ElementAt(1).Boundingboxmax.Z), 2).ToString();
            }
            catch (Exception)
            {
            }
            CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Sec_top_l] * 0.34 - extrainfo["Sec_low_l"] - 3500, centerY - info[Defination.Prim_ht] - info[Defination.Sec_bot_b] + extrainfo["Sec_low_b"] + 500 + 300, 0), "TOS EL.(+)" + TotalHt);


            string lower_prim_height = "";
            try
            {
                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
                {
                    //height of prim from bottom

                    lower_prim_height = Math.Round((MillimetersToMeters(primsup_des.ElementAt(1).PrimaryZhtNB)), 2).ToString();
                }

                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
                {
                    lower_prim_height = Math.Round((MillimetersToMeters(primsup_des.ElementAt(1).Centroid.Z)), 2).ToString();
                }
            }
            catch (Exception)
            {

            }


            ////prim of lower sec top supp
            //FixCreatePrimarySupportwithvertex(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX - 500, centerY - info[Defination.Prim_ht] - info[Defination.Sec_bot_b] + extrainfo["Sec_low_b"] + 500 + (1200 * 1.5), 1200);

            //CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - 500 + info[Defination.Prim_Radius] + 1500, centerY - 100, 0), "CL.EL.(+)" + info[Defination.Prim_ht].ToString());


            if (ListCentalSuppoData[i].ListPrimarySuppo.Where(e => e.StPt[2] == ListCentalSuppoData[i].ListPrimarySuppo.Min(s => s.StPt[2])).First().SupportName.ToLower().Contains("nb"))
            {
                double radius = 1200;
                FixCreatePrimarySupportwithvertex(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX - 500, centerY - info[Defination.Prim_ht] - info[Defination.Sec_bot_b] + extrainfo["Sec_low_b"] + 500 + (radius * 1.5), radius, "l");
                try
                {
                    lower_prim_height = MillimetersToMeters(Convert.ToDouble(lower_prim_height)).ToString();
                }
                catch (Exception)
                {

                }
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - 500 - info[Defination.Prim_Radius] - 1500, centerY - info[Defination.Prim_ht] - info[Defination.Sec_bot_b] + extrainfo["Sec_low_b"] + 500 + (radius * 1.5) - 100, 0), "CL.EL.(+)" + lower_prim_height);

            }

            if (ListCentalSuppoData[i].ListPrimarySuppo.Where(e => e.StPt[2] == ListCentalSuppoData[i].ListPrimarySuppo.Min(s => s.StPt[2])).First().SupportName.ToLower().Contains("clamp"))
            {
                double radius = 500;
                info[Defination.Prim_Radius] = radius;
                info[Defination.Prim_ht] = radius;

                CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Prim_block_U", centerX - 500, centerY - info[Defination.Prim_ht] - info[Defination.Sec_bot_b] + extrainfo["Sec_low_b"] + 500 + (radius * 1.5), radius);

                // string clampname = ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName;
                //DrawLeader(AcadBlockTableRecord, AcadTransaction, AcadDatabase, clampname, new Point3d(centerX - 500, centerY - info[Defination.Prim_ht] - info[Defination.Sec_bot_b] + extrainfo["Sec_low_b"] + 500 + (radius * 1.5) + 1.4 * radius, 0), new Point3d(centerX - 500 - 4 * radius, centerY - info[Defination.Prim_ht] - info[Defination.Sec_bot_b] + extrainfo["Sec_low_b"] + 500 + (radius * 1.5) + 1.4 * radius, 0));

                //height of prim from bottom
                try
                {
                    lower_prim_height = MillimetersToMeters(Convert.ToDouble(lower_prim_height)).ToString();
                }
                catch (Exception)
                {

                }
                // CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - 500 + info[Defination.Prim_Radius] - 1500, centerY - info[Defination.Prim_ht] - info[Defination.Sec_bot_b] + extrainfo["Sec_low_b"] + 500 + (radius * 1.5) - 100, 0), "CL.EL.(+)" + lower_prim_height);

                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - 500 - info[Defination.Prim_Radius] - 2500, centerY - info[Defination.Prim_ht] - info[Defination.Sec_bot_b] + extrainfo["Sec_low_b"] + 500 + (radius * 1.5) - 100, 0), "CL.EL.(+)" + prim_height);
                LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - 500 - info[Defination.Prim_Radius], centerY - info[Defination.Prim_ht] - info[Defination.Sec_bot_b] + extrainfo["Sec_low_b"] + 500 + (radius * 1.5), 0), new Point3d(centerX - 500 - info[Defination.Prim_Radius] - 3000, centerY - info[Defination.Prim_ht] - info[Defination.Sec_bot_b] + extrainfo["Sec_low_b"] + 500 + (radius * 1.5), 0), MyCol.LightBlue);
                string clampname = ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName;
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - 500 - info[Defination.Prim_Radius] - 2500, centerY - info[Defination.Prim_ht] - info[Defination.Sec_bot_b] + extrainfo["Sec_low_b"] + 500 + (radius * 1.5) + 300, 0), clampname);

            }


            //note nfo[prim.ht changes here

            //dimensioning

            l_dist_frm_centre = 0;
            r_dist_frm_centre = 0;
            //dimensioning
            try
            {
                var strpt = secsupp_des.ElementAt(1).StPt;
                var endpt = secsupp_des.ElementAt(1).EndPt;
                var midpt = primsup_des.ElementAt(1).Centroid;
                Point3d projectedpt = FindPerpendicularFoot(midpt, strpt, endpt);
                l_dist_frm_centre = Math.Round(GetDist(new Point3d(strpt), projectedpt));
                r_dist_frm_centre = Math.Round(GetDist(new Point3d(endpt), projectedpt));
            }
            catch (Exception)
            {

            }


            //CreateDimension(pointsextrainfo["Sec_low_LT"], new Point3d(centerX - 500, centerY - info[Defination.Prim_ht] - info[Defination.Sec_bot_b] + extrainfo["Sec_low_b"] + 500 + (1200 * 1.5)+1200, 0));
            CreateDimension(pointsextrainfo["Sec_low_LT"], new Point3d(centerX - 500, pointsextrainfo["Sec_low_LT"].Y + info[Defination.Prim_ht] + info[Defination.Prim_Radius], 0), l_dist_frm_centre.ToString(), 200);
            CreateDimension(pointsextrainfo["Sec_low_RT"], new Point3d(centerX - 500, pointsextrainfo["Sec_low_LT"].Y + info[Defination.Prim_ht] + info[Defination.Prim_Radius], 0), r_dist_frm_centre.ToString(), 200);


            height = 1000;
            length = 3000;
            info[Defination.Concrete_l] = length;
            info[Defination.Concrete_b] = height;
            //FixCreateSecondarySupportTop(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY);
            FixCreateBottomSupportTopType2(Document2D, AcadBlockTableRecord, AcadTransaction, AcadDatabase, (centerX + info[Defination.Sec_top_l] * 0.34 + info[Defination.Sec_bot_l] + centerX + info[Defination.Sec_top_l] * 0.34) / 2, centerY, height, length, i);

            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + length * 0.34, centerY - info[Defination.Sec_ht_bot] - length, 0), new Point3d(centerX + length * 0.34 + 4000, centerY - info[Defination.Sec_ht_bot] - length, 0), MyCol.LightBlue);

            ////hori small dimen
            //var botsec2 = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.BoxData.Z == ListCentalSuppoData[i].ListSecondrySuppo.Max(s => s.BoxData.Z)).First().BoxData.Z.ToString();

            //CreateAlighDimen(AcadBlockTableRecord, AcadTransaction, new Point3d(centerX + length * 0.34, centerY - info[Defination.Prim_ht] - height, 0), new Point3d(centerX - length * 0.66 + 1737.7464 + 1000, centerY - info[Defination.Sec_ht_top], 0), botsec2);

            //LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length * 0.66, centerY - info[Defination.Sec_ht_top] - 1000 - 115, 0), new Point3d(centerX - length * 0.66 + 1737.7464 + 200, centerY - info[Defination.Sec_ht_top], 0), MyCol.Yellow, "Dashed");

            //LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length * 0.66 + 1737.7464 + 1000 - 200, centerY - info[Defination.Sec_ht_top], 0), new Point3d(centerX - length * 0.66, centerY - info[Defination.Sec_ht_top] - 1000 - 571.1660 + 115, 0), MyCol.Yellow, "Dashed");

            //support name and quantity
            CreateSupportName(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, i);

            tracex += boxlen;


        }

        [Obsolete]
        public void DRS7(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, ref double tracex, double boxlen, double boxht, ref double spaceY, Document Document2D, string SupType, [Optional] int i)
        {

            double upperYgap = 3500;
            double centerX = tracex + boxlen / 2;  // 9869.9480;
            double centerY = spaceY - upperYgap;
            //box boundaries
            //vertical line
            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex + boxlen, spaceY + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);

            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex, spaceY - boxht + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);

            var primsup_des = ListCentalSuppoData[i].ListPrimarySuppo.OrderByDescending(e => e.Centroid.Z);
            var secsupp_des = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection.ToLower() == "hor").OrderByDescending(e => e.Boundingboxmax.Z);

            InsertblockScale(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Side_Primary_Elevation", centerX, centerY, 801.5625);

            double radius = 801.5625;
            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + radius, centerY, 0), new Point3d(centerX + radius + 4000, centerY, 0), MyCol.LightBlue);

            // string clampname = ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName;
            // DrawLeader(AcadBlockTableRecord, AcadTransaction, AcadDatabase, clampname, new Point3d(centerX, centerY + 1.4 * radius, 0), new Point3d(centerX - 4 * radius, centerY + 1.4 * radius, 0));

            info[Defination.Prim_Radius] = radius;
            info[Defination.Prim_ht] = 1.5 * radius;

            // FixCreatePrimarySupportwithvertex(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY, 801.5625);

            string prim_height = "";
            try
            {
                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
                {
                    //height of prim from bottom

                    prim_height = Math.Round((MillimetersToMeters(ListCentalSuppoData[i].ListPrimarySuppo[0].PrimaryZhtNB)), 2).ToString();
                }

                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
                {
                    prim_height = Math.Round((MillimetersToMeters(ListCentalSuppoData[i].ListPrimarySuppo[0].Centroid.Z)), 2).ToString();
                }
            }
            catch (Exception)
            {

            }
            try
            {
                //    prim_height = MillimetersToMeters(Convert.ToDouble(prim_height)).ToString();
            }
            catch (Exception)
            {

            }
            CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] + 1500, centerY - 100, 0), "CL.EL.(+)" + prim_height);

            CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] + 1500, centerY + 300, 0), ListCentalSuppoData[0].ListPrimarySuppo[0].SupportName);

            double height = 100;
            double length = 3000;
            info[Defination.Sec_top_l] = length;
            info[Defination.Sec_top_b] = height;

            //plate
            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht] - height, 0), new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht] - height, 0), SecThick.HBoth);

            //info for future
            pointsinfo[Defination.SecTopLT] = new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht], 0);
            pointsinfo[Defination.SecTopRT] = new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht], 0);
            pointsinfo[Defination.SecTopRB] = new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht] - height, 0);
            pointsinfo[Defination.SecTopLB] = new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht] - height, 0);

            //dimensioning

            double l_dist_frm_centre = 0;
            double r_dist_frm_centre = 0;
            //dimensioning
            try
            {
                var prims = ListCentalSuppoData[i].ListPrimarySuppo[0];
                var secs = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.SupportName.ToLower() == "plate").First();
                var edge = GetEdgeforplate(prims, secs);
                var strpt1 = edge.StPt;
                var endpt1 = edge.EndPt;
                var strpt = new double[] { strpt1.X, strpt1.Y, strpt1.Z };
                var endpt = new double[] { endpt1.X, endpt1.Y, endpt1.Z };

                // var midpt = ListCentalSuppoData[i].ListPrimarySuppo[0].Midpoint;
                var midpt = ListCentalSuppoData[i].ListPrimarySuppo[0].Centroid;

                Point3d projectedpt = FindPerpendicularFoot(midpt, strpt, endpt);

                l_dist_frm_centre = Math.Round(GetDist(new Point3d(strpt), projectedpt));
                r_dist_frm_centre = Math.Round(GetDist(new Point3d(endpt), projectedpt));
            }
            catch (Exception)
            {

            }



            CreateDimension(new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX, centerY + info[Defination.Prim_Radius], 0), l_dist_frm_centre.ToString());
            CreateDimension(new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX, centerY + info[Defination.Prim_Radius], 0), r_dist_frm_centre.ToString());


            //LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length / 2 + 4000, centerY - info[Defination.Prim_ht], 0), MyCol.LightBlue);

            string TotalHt = "";
            //mtext
            //sec ht
            try
            {
                TotalHt = Math.Round(MillimetersToMeters(ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.SupportName.ToLower() == "plate").First().Boundingboxmax.Z), 2).ToString();
            }
            catch (Exception)
            {
            }
            TOSLOC(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY, TotalHt);

            try
            {
                //TotalHt = MillimetersToMeters(Convert.ToDouble(TotalHt)).ToString();
            }
            catch (Exception)
            {

            }



            info[Defination.Sec_ht_top] = info[Defination.Prim_ht] + height;

            height = 2000;
            length = 1000;

            var sectionsize = 0.0;
            try
            {
                sectionsize = Convert.ToDouble(CSECTIONSIZE(ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection.ToLower() == "ver").First().Size));
            }
            catch (Exception)
            {

            }
            //secondary bot
            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length / 2, centerY - info[Defination.Sec_ht_top], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top] - height, 0), new Point3d(centerX - length / 2, centerY - info[Defination.Sec_ht_top] - height, 0), SecThick.VBoth, "l");


            //info for future
            pointsinfo[Defination.SecBotLT] = new Point3d(centerX - length / 2, centerY - info[Defination.Sec_ht_top], 0);
            pointsinfo[Defination.SecBotRT] = new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top], 0);
            pointsinfo[Defination.SecBotRB] = new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top] - height, 0);
            pointsinfo[Defination.SecBotLB] = new Point3d(centerX - length / 2, centerY - info[Defination.Sec_ht_top] - height, 0);

            //dimensioning
            var botsec = "";
            try
            {
                botsec = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.SupportName.ToLower().Contains("structure")).First().BoxData.Z.ToString();
            }
            catch (Exception)
            {

            }



            CreateAlighDimen(AcadBlockTableRecord, AcadTransaction, new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top] - height, 0), botsec);

            //concrete sup
            info[Defination.Sec_ht_bot] = info[Defination.Sec_ht_top] + height;

            height = 1000;
            length = 3000;
            info[Defination.Concrete_l] = length;
            info[Defination.Concrete_b] = height;

            FixCreateBottomSupportTopType2(Document2D, AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY, height, length, i);


            //support name and quantity
            CreateSupportName(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, i);


            tracex += boxlen;

        }

        [Obsolete]
        public void DRS8(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, ref double tracex, double boxlen, double boxht, ref double spaceY, Document Document2D, string SupType, [Optional] int i)
        {

            double upperYgap = 3500;
            double centerX = tracex + boxlen / 2;  // 9869.9480;
            double centerY = spaceY - upperYgap;
            //box boundaries
            //vertical line
            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex + boxlen, spaceY + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);

            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex, spaceY - boxht + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);

            var primsup_des = ListCentalSuppoData[i].ListPrimarySuppo.OrderByDescending(e => e.Centroid.Z);
            var secsupp_des = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection.ToLower() == "hor").OrderByDescending(e => e.Boundingboxmax.Z);

            FixCreatePrimarySupportwithvertex(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY, 801.5625);

            string prim_height = "";
            try
            {
                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
                {
                    //height of prim from bottom

                    prim_height = Math.Round((MillimetersToMeters(ListCentalSuppoData[i].ListPrimarySuppo[0].PrimaryZhtNB)), 2).ToString();
                }

                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
                {
                    prim_height = Math.Round((MillimetersToMeters(ListCentalSuppoData[i].ListPrimarySuppo[0].Centroid.Z)), 2).ToString();
                }
            }
            catch (Exception)
            {

            }
            try
            {
                //    prim_height = MillimetersToMeters(Convert.ToDouble(prim_height)).ToString();
            }
            catch (Exception)
            {

            }
            CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] + 1500, centerY - 100, 0), "CL.EL.(+)" + prim_height);

            double height = 100;
            double length = 3000;
            info[Defination.Sec_top_l] = length;
            info[Defination.Sec_top_b] = height;

            //plate
            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht] - height, 0), new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht] - height, 0), SecThick.HBoth);

            //info for future
            pointsinfo[Defination.SecTopLT] = new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht], 0);
            pointsinfo[Defination.SecTopRT] = new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht], 0);
            pointsinfo[Defination.SecTopRB] = new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht] - height, 0);
            pointsinfo[Defination.SecTopLB] = new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht] - height, 0);

            //dimensioning

            double l_dist_frm_centre = 0;
            double r_dist_frm_centre = 0;
            //dimensioning
            try
            {

                var prims = ListCentalSuppoData[i].ListPrimarySuppo[0];
                var secs = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.SupportName.ToLower() == "plate").First();
                var edge = GetEdgeforplate(prims, secs);
                var strpt1 = edge.StPt;
                var endpt1 = edge.EndPt;
                var strpt = new double[] { strpt1.X, strpt1.Y, strpt1.Z };
                var endpt = new double[] { endpt1.X, endpt1.Y, endpt1.Z };


                // var midpt = ListCentalSuppoData[i].ListPrimarySuppo[0].Midpoint;
                var midpt = ListCentalSuppoData[i].ListPrimarySuppo[0].Centroid;

                Point3d projectedpt = FindPerpendicularFoot(midpt, strpt, endpt);

                l_dist_frm_centre = Math.Round(GetDist(new Point3d(strpt), projectedpt));
                r_dist_frm_centre = Math.Round(GetDist(new Point3d(endpt), projectedpt));
            }
            catch (Exception)
            {

            }

            //var ldist = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection == "Hor");

            CreateDimension(new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX, centerY + info[Defination.Prim_Radius], 0), l_dist_frm_centre.ToString());
            CreateDimension(new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX, centerY + info[Defination.Prim_Radius], 0), r_dist_frm_centre.ToString());


            //LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length / 2 + 4000, centerY - info[Defination.Prim_ht], 0), MyCol.LightBlue);


            //veerti dime top sec datum
            string TotalHt = "";
            // top sec ht
            try
            {
                TotalHt = Math.Round(MillimetersToMeters(ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.SupportName.ToLower() == "plate").First().Boundingboxmax.Z), 2).ToString();
            }
            catch (Exception)
            {
            }
            TOSLOC(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY, TotalHt);



            // CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + 2500, centerY - info[Defination.Prim_ht] + 300, 0), "TOS EL.(+)100." + TotalHt);
            DrawLeader(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Plate", pointsinfo[Defination.SecTopLT], new Point3d(pointsinfo[Defination.SecTopLT].X - 1000, pointsinfo[Defination.SecTopLT].Y, 0));

            info[Defination.Sec_ht_top] = info[Defination.Prim_ht] + height;

            height = 2000;
            length = 1000;
            var sectionsize = 0.0;
            try
            {
                sectionsize = Convert.ToDouble(CSECTIONSIZE(ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.SupportName.ToLower().Contains("structure")).First().Size));
            }
            catch (Exception)
            {

            }
            //secondary bot
            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length / 2, centerY - info[Defination.Sec_ht_top], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top] - height, 0), new Point3d(centerX - length / 2, centerY - info[Defination.Sec_ht_top] - height, 0), SecThick.VBoth, "l");


            //info for future
            pointsinfo[Defination.SecBotLT] = new Point3d(centerX - length / 2, centerY - info[Defination.Sec_ht_top], 0);
            pointsinfo[Defination.SecBotRT] = new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top], 0);
            pointsinfo[Defination.SecBotRB] = new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top] - height, 0);
            pointsinfo[Defination.SecBotLB] = new Point3d(centerX - length / 2, centerY - info[Defination.Sec_ht_top] - height, 0);

            //dimensioning
            var botsec = "";
            try
            {
                botsec = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.SupportName.ToLower().Contains("structure")).First().BoxData.Z.ToString();
            }
            catch (Exception)
            {

            }



            CreateAlighDimen(AcadBlockTableRecord, AcadTransaction, new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top] - height, 0), botsec);

            info[Defination.Sec_ht_bot] = info[Defination.Sec_ht_top] + height;

            height = 1000;
            length = 3000;
            info[Defination.Concrete_l] = length;
            info[Defination.Concrete_b] = height;

            FixCreateBottomSupportTopType2(Document2D, AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY, height, length, i);


            //support name and quantity
            CreateSupportName(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, i);


            tracex += boxlen;

        }


        [Obsolete]
        public void DRS9(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, ref double tracex, double boxlen, double boxht, ref double spaceY, Document Document2D, string SupType, [Optional] int i)
        {
            double upperYgap = 3500;

            double centerX = tracex + boxlen / 2;  // 9869.9480;
            double centerY = spaceY - upperYgap;
            //box boundaries
            //vertical line
            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex + boxlen, spaceY + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);

            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex, spaceY - boxht + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);

            //CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Prim_block_U", centerX, centerY, radius);
            var primsup_des = ListCentalSuppoData[i].ListPrimarySuppo.OrderByDescending(e => e.Centroid.Z);
            var secsupp_des = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection.ToLower() == "hor").OrderByDescending(e => e.Boundingboxmax.Z);

            string prim_height = "";
            try
            {
                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
                {
                    //height of prim from bottom

                    prim_height = Math.Round((MillimetersToMeters(ListCentalSuppoData[i].ListPrimarySuppo[0].PrimaryZhtNB)), 2).ToString();
                }

                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
                {
                    prim_height = Math.Round((MillimetersToMeters(ListCentalSuppoData[i].ListPrimarySuppo[0].Centroid.Z)), 2).ToString();
                }
            }
            catch (Exception)
            {

            }

            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
            {
                FixCreatePrimarySupportwithvertex(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY, 801.5625);
                //height of prim from bottom
                try
                {
                    //    prim_height = MillimetersToMeters(Convert.ToDouble(prim_height)).ToString();
                }
                catch (Exception)
                {

                }
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] + 1500, centerY - 100, 0), "CL.EL.(+)" + prim_height);

            }

            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
            {
                double radius = 500;
                info[Defination.Prim_Radius] = radius;
                info[Defination.Prim_ht] = radius;

                CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Prim_block_U", centerX, centerY, radius);


                // string clampname = ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName;
                // DrawLeader(AcadBlockTableRecord, AcadTransaction, AcadDatabase, clampname, new Point3d(centerX, centerY + 1.4 * radius, 0), new Point3d(centerX - 4 * radius, centerY + 1.4 * radius, 0));

                //height of prim from bottom
                try
                {
                    //    prim_height = MillimetersToMeters(Convert.ToDouble(prim_height)).ToString();
                }
                catch (Exception)
                {

                }
                //CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] - 1500, centerY - 100, 0), "CL.EL.(+)" + prim_height);

                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - info[Defination.Prim_Radius] - 2500, centerY - 100, 0), "CL.EL.(+)" + prim_height);
                LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - info[Defination.Prim_Radius], centerY, 0), new Point3d(centerX - info[Defination.Prim_Radius] - 3000, centerY, 0), MyCol.LightBlue);
                string clampname = ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName;
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - info[Defination.Prim_Radius] - 2500, centerY + 300, 0), clampname);

            }



            //upper top sec
            double height = 1000;
            double length = 4000;
            info[Defination.Sec_top_l] = length;
            info[Defination.Sec_top_b] = height;
            //double ht_frm_cen = 1220.7383;
            var sectionsize = 0.0;
            try
            {
                var section = secsupp_des.ElementAt(0).Size;

                sectionsize = Convert.ToDouble(CSECTIONSIZE(section));
            }
            catch (Exception)
            {

            }

            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length * 0.66, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length * 0.34, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length * 0.34, centerY - info[Defination.Prim_ht] - height, 0), new Point3d(centerX - length * 0.66, centerY - info[Defination.Prim_ht] - height, 0), SecThick.HBoth, "R", 0, sectionsize);


            //info for future
            pointsinfo[Defination.SecTopLT] = new Point3d(centerX - length * 0.66, centerY - info[Defination.Prim_ht], 0);
            pointsinfo[Defination.SecTopRT] = new Point3d(centerX + length * 0.34, centerY - info[Defination.Prim_ht], 0);
            pointsinfo[Defination.SecTopRB] = new Point3d(centerX + length * 0.34, centerY - info[Defination.Prim_ht] - height, 0);
            pointsinfo[Defination.SecTopLB] = new Point3d(centerX - length * 0.66, centerY - info[Defination.Prim_ht] - height, 0);


            //hori dimen prim and sec sup
            double l_dist_frm_centre = 0;
            double r_dist_frm_centre = 0;
            //dimensioning
            try
            {
                var strpt = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection == "Hor").First().StPt;
                var endpt = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection == "Hor").First().EndPt;
                var midpt = ListCentalSuppoData[i].ListPrimarySuppo[0].Centroid;
                Point3d projectedpt = FindPerpendicularFoot(midpt, strpt, endpt);

                l_dist_frm_centre = Math.Round(GetDist(new Point3d(strpt), projectedpt));
                r_dist_frm_centre = Math.Round(GetDist(new Point3d(endpt), projectedpt));

                l_dist_frm_centre = Math.Max(l_dist_frm_centre, r_dist_frm_centre);
                r_dist_frm_centre = Math.Min(l_dist_frm_centre, r_dist_frm_centre);

            }
            catch (Exception)
            {

            }

            //dimensioning
            CreateDimension(new Point3d(centerX - length * 0.66, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX, centerY + info[Defination.Prim_Radius], 0));
            CreateDimension(new Point3d(centerX + length * 0.34, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX, centerY + info[Defination.Prim_Radius], 0));


            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + length * 0.34, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length * 0.34 + 4000, centerY - info[Defination.Prim_ht], 0), MyCol.LightBlue);


            string TotalHt = "";

            //mtext

            try
            {
                TotalHt = Math.Round(MillimetersToMeters(ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection.ToLower() == "hor").First().Boundingboxmax.Z), 2).ToString();
            }
            catch (Exception)
            {
            }

            try
            {
                //TotalHt = MillimetersToMeters(Convert.ToDouble(TotalHt)).ToString();
            }
            catch (Exception)
            {

            }


            CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + 2500, centerY - info[Defination.Prim_ht] + 300, 0), "TOS EL.(+)" + TotalHt);

            info[Defination.Sec_ht_top] = info[Defination.Prim_ht] + height;

            height = 4000;
            length = 1000;
            info[Defination.Sec_bot_l] = length;
            info[Defination.Sec_bot_b] = height;

            info[Defination.Sec_ht_bot] = info[Defination.Sec_ht_top] + height;
            //var sectionsize = 0.0;
            try
            {
                sectionsize = Convert.ToDouble(CSECTIONSIZE(ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection.ToLower() == "ver").First().Size));
            }
            catch (Exception)
            {

            }
            //sec bot
            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, pointsinfo[Defination.SecTopLB], new Point3d(pointsinfo[Defination.SecTopLB].X + info[Defination.Sec_bot_l], pointsinfo[Defination.SecTopLB].Y, pointsinfo[Defination.SecTopLB].Z), new Point3d(pointsinfo[Defination.SecTopLB].X + info[Defination.Sec_bot_l], pointsinfo[Defination.SecTopLB].Y - info[Defination.Sec_bot_b], pointsinfo[Defination.SecTopLB].Z), new Point3d(pointsinfo[Defination.SecTopLB].X, pointsinfo[Defination.SecTopLB].Y - info[Defination.Sec_bot_b], pointsinfo[Defination.SecTopLB].Z), SecThick.VBoth, "l");

            //info for future
            pointsinfo[Defination.SecBotLT] = pointsinfo[Defination.SecTopLB];
            pointsinfo[Defination.SecBotRT] = new Point3d(pointsinfo[Defination.SecTopLB].X + info[Defination.Sec_bot_l], pointsinfo[Defination.SecTopLB].Y, pointsinfo[Defination.SecTopLB].Z);
            pointsinfo[Defination.SecBotRB] = new Point3d(pointsinfo[Defination.SecTopLB].X + info[Defination.Sec_bot_l], pointsinfo[Defination.SecTopLB].Y - info[Defination.Sec_bot_b], pointsinfo[Defination.SecTopLB].Z);
            pointsinfo[Defination.SecBotLB] = new Point3d(pointsinfo[Defination.SecTopLB].X - info[Defination.Sec_bot_l], pointsinfo[Defination.SecTopLB].Y - info[Defination.Sec_bot_b], pointsinfo[Defination.SecTopLB].Z);
            //dimensioning

            var botsec = "";
            try
            {
                botsec = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.BoxData.Z == ListCentalSuppoData[i].ListSecondrySuppo.Max(s => s.BoxData.Z)).First().BoxData.Z.ToString();
            }
            catch (Exception)
            {

            }

            CreateAlighDimen(AcadBlockTableRecord, AcadTransaction, new Point3d(centerX - height * 0.66 + length, centerY - info[Defination.Sec_ht_top], 0), new Point3d(centerX - height * 0.66 + length, centerY - info[Defination.Sec_ht_top] - height, 0), botsec);


            //draw plate 
            height = 100;
            length = 3000;
            info[Defination.Plate_l] = length;
            info[Defination.Plate_b] = height;


            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d((pointsinfo[Defination.SecBotRB].X + pointsinfo[Defination.SecBotLB].X) / 2 - info[Defination.Plate_l] / 2, pointsinfo[Defination.SecBotLB].Y, 0), new Point3d((pointsinfo[Defination.SecBotRB].X + pointsinfo[Defination.SecBotLB].X) / 2 + info[Defination.Plate_l] / 2, pointsinfo[Defination.SecBotLB].Y, 0), new Point3d((pointsinfo[Defination.SecBotRB].X + pointsinfo[Defination.SecBotLB].X) / 2 + info[Defination.Plate_l] / 2, pointsinfo[Defination.SecBotLB].Y - info[Defination.Plate_b], 0), new Point3d((pointsinfo[Defination.SecBotRB].X + pointsinfo[Defination.SecBotLB].X) / 2 - info[Defination.Plate_l] / 2, pointsinfo[Defination.SecBotLB].Y - info[Defination.Plate_b], 0), SecThick.Nothing);


            //support name and quantity
            CreateSupportName(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, i);

            tracex += boxlen;
        }


        [Obsolete]
        public void DRS10(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, ref double tracex, double boxlen, double boxht, ref double spaceY, Document Document2D, string SupType, [Optional] int i)
        {

            double upperYgap = 3500;
            double centerX = tracex + boxlen / 2;  // 9869.9480;
            double centerY = spaceY - upperYgap;
            //box boundaries
            //vertical line
            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex + boxlen, spaceY + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);

            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex, spaceY - boxht + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);


            // InsertblockScale(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Side_Primary_Elevation", centerX, centerY, 801.5625);
            var primsup_des = ListCentalSuppoData[i].ListPrimarySuppo.OrderByDescending(e => e.Centroid.Z);
            //var secsupp_des = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection.ToLower() == "hor").OrderByDescending(e => e.Boundingboxmax.Z);
            string prim_height = "";
            try
            {
                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
                {
                    //height of prim from bottom

                    prim_height = Math.Round((MillimetersToMeters(ListCentalSuppoData[i].ListPrimarySuppo[0].PrimaryZhtNB)), 2).ToString();
                }

                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
                {
                    prim_height = Math.Round((MillimetersToMeters(ListCentalSuppoData[i].ListPrimarySuppo[0].Centroid.Z)), 2).ToString();
                }
            }
            catch (Exception)
            {

            }

            CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Side_Primary_Elevation", centerX, centerY, 801.5625);

            info[Defination.Prim_Radius] = 801.5625;
            info[Defination.Prim_ht] = 1.5 * 801.5625;


            CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] + 2500, centerY - 100, 0), "CL.EL.(+)" + prim_height);
            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius], centerY, 0), new Point3d(centerX + info[Defination.Prim_Radius] + 3000, centerY, 0), MyCol.LightBlue);
            string clampname = ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName;
            CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] + 2500, centerY + 300, 0), clampname);

            //CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] + 1500, centerY - 100, 0), "CL.EL.(+)" + info[Defination.Prim_ht].ToString());

            //plate sec top
            double height = 100;
            double length = 3000;
            info[Defination.Sec_top_l] = length;
            info[Defination.Sec_top_b] = height;

            //plate
            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht] - height, 0), new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht] - height, 0), SecThick.HBoth);

            //info for future
            pointsinfo[Defination.SecTopLT] = new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht], 0);
            pointsinfo[Defination.SecTopRT] = new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht], 0);
            pointsinfo[Defination.SecTopRB] = new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht] - height, 0);
            pointsinfo[Defination.SecTopLB] = new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht] - height, 0);

            //dimensioning
            double l_dist_frm_centre = 0;
            double r_dist_frm_centre = 0;
            //dimensioning
            try
            {

                var prims = ListCentalSuppoData[i].ListPrimarySuppo[0];
                var secs = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.SupportName.ToLower() == "plate").First();
                var edge = GetEdgeforplate(prims, secs);
                var strpt1 = edge.StPt;
                var endpt1 = edge.EndPt;
                var strpt = new double[] { strpt1.X, strpt1.Y, strpt1.Z };
                var endpt = new double[] { endpt1.X, endpt1.Y, endpt1.Z };


                // var midpt = ListCentalSuppoData[i].ListPrimarySuppo[0].Midpoint;
                var midpt = ListCentalSuppoData[i].ListPrimarySuppo[0].Centroid;

                Point3d projectedpt = FindPerpendicularFoot(midpt, strpt, endpt);

                l_dist_frm_centre = Math.Round(GetDist(new Point3d(strpt), projectedpt));
                r_dist_frm_centre = Math.Round(GetDist(new Point3d(endpt), projectedpt));
            }
            catch (Exception)
            {

            }

            CreateDimension(new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX, centerY + info[Defination.Prim_Radius], 0), l_dist_frm_centre.ToString());
            CreateDimension(new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX, centerY + info[Defination.Prim_Radius], 0), r_dist_frm_centre.ToString());


            // LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length / 2 + 4000, centerY - info[Defination.Prim_ht], 0), MyCol.LightBlue);

            //veerti dime top sec datum
            string TotalHt = "";
            // top sec ht
            try
            {
                TotalHt = Math.Round(MillimetersToMeters(ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.SupportName.ToLower() == "plate").First().Boundingboxmax.Z), 2).ToString();
            }
            catch (Exception)
            {
            }
            TOSLOC(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY, TotalHt);

            info[Defination.Sec_ht_top] = info[Defination.Prim_ht] + height;

            //bot sec
            height = 2000;
            length = 1000;
            var sectionsize = 0.0;
            try
            {
                sectionsize = Convert.ToDouble(CSECTIONSIZE(ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.SupportName.ToLower().Contains("structure")).First().Size));
            }
            catch (Exception)
            {

            }
            //secondary bot
            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length / 2, centerY - info[Defination.Sec_ht_top], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top] - height, 0), new Point3d(centerX - length / 2, centerY - info[Defination.Sec_ht_top] - height, 0), SecThick.VBoth, "l", 0, sectionsize);


            //info for future
            pointsinfo[Defination.SecBotLT] = new Point3d(centerX - length / 2, centerY - info[Defination.Sec_ht_top], 0);
            pointsinfo[Defination.SecBotRT] = new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top], 0);
            pointsinfo[Defination.SecBotRB] = new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top] - height, 0);
            pointsinfo[Defination.SecBotLB] = new Point3d(centerX - length / 2, centerY - info[Defination.Sec_ht_top] - height, 0);

            //dimensioning
            var botsec = "";
            try
            {
                botsec = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.SupportName.ToLower().Contains("structure")).First().BoxData.Z.ToString();
            }
            catch (Exception)
            {

            }

            CreateAlighDimen(AcadBlockTableRecord, AcadTransaction, new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top] - height, 0), botsec);

            info[Defination.Sec_ht_bot] = info[Defination.Sec_ht_top] + height;

            height = 1000;
            length = 3000;
            info[Defination.Concrete_l] = length;
            info[Defination.Concrete_b] = height;

            FixCreateBottomSupportTopType2(Document2D, AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY, height, length, i);


            //support name and quantity
            CreateSupportName(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, i);


            tracex += boxlen;

        }

        [Obsolete]
        public void DRS11(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, ref double tracex, double boxlen, double boxht, ref double spaceY, Document Document2D, string SupType, [Optional] int i)
        {

            double upperYgap = 3500;
            double centerX = tracex + boxlen / 2;  // 9869.9480;
            double centerY = spaceY - upperYgap;

            //for taking whole figure up by 1000
            centerY += 1000;

            //box boundaries
            //vertical line
            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex + boxlen, spaceY + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);

            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex, spaceY - boxht + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);

            //does not have primary support
            //var primsup_des = ListCentalSuppoData[i].ListPrimarySuppo.OrderByDescending(e => e.Centroid.Z);
            var secsupp_des = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection.ToLower() == "hor").OrderByDescending(e => e.Boundingboxmax.Z);
            // FixCreatePrimarySupportwithvertex(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY, 801.5625);

            double radius = 801.5625;
            info[Defination.Prim_Radius] = radius;
            info[Defination.Prim_ht] = radius * 1.5;
            //height of prim from bottom

            //CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] + 1500, centerY - 100, 0), "CL.EL.(+)" + info[Defination.Prim_ht].ToString());

            double height = 1000;
            double length = 5000;
            info[Defination.Sec_top_l] = length;
            info[Defination.Sec_top_b] = height;
            var sectionsize = 0.0;
            try
            {
                var section = secsupp_des.ElementAt(0).Size;

                sectionsize = Convert.ToDouble(CSECTIONSIZE(section));
            }
            catch (Exception)
            {

            }

            //upper secondary supp
            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht] - height, 0), new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht] - height, 0), SecThick.HBoth, "R", 0, sectionsize);

            //info for future
            pointsinfo[Defination.SecTopLT] = new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht], 0);
            pointsinfo[Defination.SecTopRT] = new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht], 0);
            pointsinfo[Defination.SecTopRB] = new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht] - height, 0);
            pointsinfo[Defination.SecTopLB] = new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht] - height, 0);

            //double l_dist_frm_centre = 0;
            //double r_dist_frm_centre = 0;
            //dimensioning
            //try
            //{
            //    var strpt = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection == "Hor").First().StPt;

            //    var endpt = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection == "Hor").First().EndPt;

            //    var midpt = ListCentalSuppoData[i].ListPrimarySuppo[0].Midpoint;

            //    Point3d projectedpt = FindPerpendicularFoot(midpt, strpt, endpt);

            //    l_dist_frm_centre = GetDist(new Point3d(strpt), projectedpt);
            //    r_dist_frm_centre = GetDist(new Point3d(endpt), projectedpt);
            //}
            //catch (Exception)
            //{

            //}
            double horilength = 0;
            try
            {
                horilength = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection == "Hor").First().BoxData.X;
            }
            catch (Exception)
            {

            }

            //double strtmiddist=GetDist(ldist)

            //CreateDimension(new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX, centerY + info[Defination.Prim_Radius], 0), horilength.ToString());
            // CreateDimension(new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX, centerY + info[Defination.Prim_Radius], 0), r_dist_frm_centre.ToString());


            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length / 2 + 4000, centerY - info[Defination.Prim_ht], 0), MyCol.LightBlue);


            string TotalHt = "";
            //mtext
            try
            {
                TotalHt = Math.Round(MillimetersToMeters(ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection.ToLower() == "hor").First().Boundingboxmax.Z), 2).ToString();
            }
            catch (Exception)
            {
            }
            try
            {
                //TotalHt = MillimetersToMeters(Convert.ToDouble(TotalHt)).ToString();
            }
            catch (Exception)
            {

            }

            CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + 2500, centerY - info[Defination.Prim_ht] + 300, 0), "TOS EL.(+)" + TotalHt);

            info[Defination.Sec_ht_top] = info[Defination.Prim_ht] + height;

            height = 4000;
            length = 1000;

            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length / 2, centerY - info[Defination.Sec_ht_top], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top] - height, 0), new Point3d(centerX - length / 2, centerY - info[Defination.Sec_ht_top] - height, 0), SecThick.Left, "l");

            //info for future
            pointsinfo[Defination.SecBotLT] = new Point3d(centerX - length / 2, centerY - info[Defination.Sec_ht_top], 0);
            pointsinfo[Defination.SecBotRT] = new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top], 0);
            pointsinfo[Defination.SecBotRB] = new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top] - height, 0);
            pointsinfo[Defination.SecBotLB] = new Point3d(centerX - length / 2, centerY - info[Defination.Sec_ht_top] - height, 0);

            //dimensioning
            var botsec = "";
            try
            {
                botsec = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.BoxData.Z == ListCentalSuppoData[i].ListSecondrySuppo.Max(s => s.BoxData.Z)).First().BoxData.Z.ToString();
            }
            catch (Exception)
            {

            }

            CreateAlighDimen(AcadBlockTableRecord, AcadTransaction, new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top] - height, 0), botsec);

            info[Defination.Sec_ht_bot] = info[Defination.Sec_ht_top] + height;

            height = 1000;
            length = 3000;
            info[Defination.Concrete_l] = length;
            info[Defination.Concrete_b] = height;

            FixCreateBottomSupportTopType2(Document2D, AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY, height, length, i);


            //support name and quantity
            CreateSupportName(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, i);


            tracex += boxlen;

        }

        [Obsolete]
        public void DRS13(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, ref double tracex, double boxlen, double boxht, ref double spaceY, Document Document2D, string SupType, [Optional] int i)
        {

            double upperYgap = 3500;
            double centerX = tracex + boxlen / 2;  // 9869.9480;
            double centerY = spaceY - upperYgap;
            //box boundaries
            //vertical line
            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex + boxlen, spaceY + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);

            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex, spaceY - boxht + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);

            //prim ht
            string prim_height = "";
            try
            {
                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
                {
                    //height of prim from bottom

                    prim_height = Math.Round((MillimetersToMeters(ListCentalSuppoData[i].ListPrimarySuppo[0].PrimaryZhtNB)), 2).ToString();
                }

                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
                {
                    prim_height = Math.Round((MillimetersToMeters(ListCentalSuppoData[i].ListPrimarySuppo[0].Centroid.Z)), 2).ToString();
                }
            }
            catch (Exception)
            {

            }

            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
            {
                FixCreatePrimarySupportwithvertex(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY, 801.5625);
                //height of prim from bottom

                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] + 1500, centerY - 100, 0), "CL.EL.(+)" + info[Defination.Prim_ht].ToString());

            }

            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
            {
                double radius = 500;
                info[Defination.Prim_Radius] = radius;
                info[Defination.Prim_ht] = radius;

                CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Prim_block_U", centerX, centerY, radius);
                //height of prim from bottom
                // CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] - 1500, centerY - 100, 0), "CL.EL.(+)" + info[Defination.Prim_ht].ToString());

                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - info[Defination.Prim_Radius] - 2500, centerY - 100, 0), "CL.EL.(+)");
                LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius], centerY, 0), new Point3d(centerX + info[Defination.Prim_Radius] - 3000, centerY, 0), MyCol.LightBlue);
                string clampname = ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName;
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - info[Defination.Prim_Radius] - 2500, centerY + 300, 0), clampname);

            }

            double height = 1500;
            double length = 3000;
            info[Defination.Sec_top_l] = length;
            info[Defination.Sec_top_b] = height;

            try
            {
                CreateRefBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht] - height, 0), new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht] - height, 0), "hor");
            }
            catch (Exception)
            {

            }

            //ref block
            pointsextrainfo["RefLT"] = new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht] - height, 0);
            pointsextrainfo["RefRT"] = new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht], 0);
            pointsextrainfo["RefRB"] = new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht], 0);
            pointsextrainfo["RefLB"] = new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht] - height, 0);

            DrawLeader(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Structure/Block", pointsextrainfo["RefLT"], new Point3d(pointsextrainfo["RefLT"].X - 1500, (pointsextrainfo["RefLT"].Y + pointsextrainfo["RefLB"].Y) / 2, 0));


            //upper secondary supp
            // BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht] - height, 0), new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht] - height, 0), SecThick.HBoth, "R");


            //support name and quantity
            CreateSupportName(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, i);


            tracex += boxlen;

        }


        public void DRS14(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, ref double tracex, double boxlen, double boxht, ref double spaceY, Document Document2D, string SupType, [Optional] int i)
        {
            double upperYgap = 3500;

            double centerX = tracex + boxlen / 2;  // 9869.9480;
            double centerY = spaceY - upperYgap;
            //box boundaries
            //vertical line
            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex + boxlen, spaceY + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);


            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex, spaceY - boxht + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);

            //FixCreatePrimarySupportwithvertex(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY, 801.5625);

            //CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] + 1500, centerY - 100, 0), "CL.EL.(+)" + info[Defination.Prim_ht].ToString());
            var secsupp_des = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection.ToLower() == "hor").OrderByDescending(e => e.Boundingboxmax.Z);
            string prim_height = "";
            try
            {
                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
                {
                    //height of prim from bottom

                    prim_height = Math.Round((MillimetersToMeters(ListCentalSuppoData[i].ListPrimarySuppo[0].PrimaryZhtNB)), 2).ToString();
                }

                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
                {
                    prim_height = Math.Round((MillimetersToMeters(ListCentalSuppoData[i].ListPrimarySuppo[0].Centroid.Z)), 2).ToString();
                }
            }
            catch (Exception)
            {

            }


            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
            {
                FixCreatePrimarySupportwithvertex(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY, 801.5625);
                //height of prim from bottom
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] + 1500, centerY - 100, 0), "CL.EL.(+)" + prim_height);

            }

            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
            {
                double radius = 500;
                info[Defination.Prim_Radius] = radius;
                info[Defination.Prim_ht] = radius;

                CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Prim_block_U", centerX, centerY, radius);
                //height of prim from bottom
                // CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] - 1500, centerY - 100, 0), "CL.EL.(+)" + info[Defination.Prim_ht].ToString());

                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - info[Defination.Prim_Radius] - 2500, centerY - 100, 0), "CL.EL.(+)" + prim_height);
                LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - info[Defination.Prim_Radius], centerY, 0), new Point3d(centerX - info[Defination.Prim_Radius] - 3000, centerY, 0), MyCol.LightBlue);
                string clampname = ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName;
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - info[Defination.Prim_Radius] - 2500, centerY + 300, 0), clampname);

            }



            //double ht_frm_cen = 1220.7383;

            double height = 1000;
            double length = 3000;
            info[Defination.Sec_top_l] = length;
            info[Defination.Sec_top_b] = height;
            var sectionsize = 0.0;
            try
            {
                var section = secsupp_des.ElementAt(0).Size;

                sectionsize = Convert.ToDouble(CSECTIONSIZE(section));
            }
            catch (Exception)
            {

            }

            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length * 0.66, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length * 0.34, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length * 0.34, centerY - info[Defination.Prim_ht] - height, 0), new Point3d(centerX - length * 0.66, centerY - info[Defination.Prim_ht] - height, 0), SecThick.HBoth, "R", 0, sectionsize);

            //info for future
            pointsinfo[Defination.SecTopLT] = new Point3d(centerX - length * 0.66, centerY - info[Defination.Prim_ht], 0);
            pointsinfo[Defination.SecTopRT] = new Point3d(centerX + length * 0.34, centerY - info[Defination.Prim_ht], 0);
            pointsinfo[Defination.SecTopRB] = new Point3d(centerX + length * 0.34, centerY - info[Defination.Prim_ht] - height, 0);
            pointsinfo[Defination.SecTopLB] = new Point3d(centerX - length * 0.66, centerY - info[Defination.Prim_ht] - height, 0);

            double l_dist_frm_centre = 0;
            double r_dist_frm_centre = 0;
            //dimensioning
            try
            {
                var strpt = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection == "Hor").First().StPt;

                var endpt = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection == "Hor").First().EndPt;

                // var midpt = ListCentalSuppoData[i].ListPrimarySuppo[0].Midpoint;
                var midpt = ListCentalSuppoData[i].ListPrimarySuppo[0].Centroid;

                Point3d projectedpt = FindPerpendicularFoot(midpt, strpt, endpt);

                l_dist_frm_centre = Math.Round(GetDist(new Point3d(strpt), projectedpt));
                r_dist_frm_centre = Math.Round(GetDist(new Point3d(endpt), projectedpt));

                l_dist_frm_centre = Math.Max(l_dist_frm_centre, r_dist_frm_centre);
                r_dist_frm_centre = Math.Min(l_dist_frm_centre, r_dist_frm_centre);
            }
            catch (Exception)
            {

            }

            //dimensioning
            CreateDimension(new Point3d(centerX - length * 0.66, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX, centerY + info[Defination.Prim_Radius], 0), l_dist_frm_centre.ToString());
            CreateDimension(new Point3d(centerX + length * 0.34, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX, centerY + info[Defination.Prim_Radius], 0), r_dist_frm_centre.ToString());

            //mtext
            string TotalHt = "";

            try
            {
                TotalHt = Math.Round(MillimetersToMeters(ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection.ToLower() == "hor").First().Boundingboxmax.Z), 2).ToString();
            }
            catch (Exception)
            {
            }
            try
            {
                //TotalHt = MillimetersToMeters(Convert.ToDouble(TotalHt)).ToString();
            }
            catch (Exception)
            {

            }
            CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + 2500, centerY - info[Defination.Prim_ht] + 300, 0), "TOS EL.(+)" + TotalHt);
            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, pointsinfo[Defination.SecTopRT], new Point3d(centerX + length / 2 + 4000, centerY - info[Defination.Prim_ht], 0), MyCol.LightBlue);

            //support name and quantity
            CreateSupportName(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, i);

            //ref block
            pointsextrainfo["RefLT"] = new Point3d(pointsinfo[Defination.SecTopLT].X - 1500, pointsinfo[Defination.SecTopLT].Y + 1000, 0);
            pointsextrainfo["RefRT"] = new Point3d(pointsinfo[Defination.SecTopLT].X, pointsinfo[Defination.SecTopLT].Y + 1000, 0);
            pointsextrainfo["RefRB"] = new Point3d(pointsinfo[Defination.SecTopLT].X, pointsinfo[Defination.SecTopLT].Y - 2000, 0);
            pointsextrainfo["RefLB"] = new Point3d(pointsinfo[Defination.SecTopLT].X - 1500, pointsinfo[Defination.SecTopLT].Y - 2000, 0);

            CreateRefBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, pointsextrainfo["RefLT"], pointsextrainfo["RefRT"], pointsextrainfo["RefRB"], pointsextrainfo["RefLB"]);
            DrawLeader(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Structure/Block", new Point3d(pointsextrainfo["RefLT"].X, (pointsextrainfo["RefLT"].Y + pointsextrainfo["RefLB"].Y) / 2, 0), new Point3d(pointsextrainfo["RefLT"].X - 1000, (pointsextrainfo["RefLT"].Y + pointsextrainfo["RefLB"].Y) / 2, 0));

            tracex += boxlen;

        }

        [Obsolete]
        public void DRS15(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, ref double tracex, double boxlen, double boxht, ref double spaceY, Document Document2D, string SupType, [Optional] int i)
        {

            double upperYgap = 3500;
            double centerX = tracex + boxlen / 2;  // 9869.9480;
            double centerY = spaceY - upperYgap;
            //box boundaries
            //vertical line
            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex + boxlen, spaceY + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);

            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex, spaceY - boxht + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);
            var secsupp_des = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection.ToLower() == "hor").OrderByDescending(e => e.Boundingboxmax.Z);

            string prim_height = "";
            try
            {
                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
                {
                    //height of prim from bottom

                    prim_height = Math.Round((MillimetersToMeters(ListCentalSuppoData[i].ListPrimarySuppo[0].PrimaryZhtNB)), 2).ToString();
                }

                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
                {
                    prim_height = Math.Round((MillimetersToMeters(ListCentalSuppoData[i].ListPrimarySuppo[0].Centroid.Z)), 2).ToString();
                }
            }
            catch (Exception)
            {

            }

            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
            {
                FixCreatePrimarySupportwithvertex(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX - 1000, centerY, 801.5625);
                //height of prim from bottom
                try
                {
                    //    prim_height = MillimetersToMeters(Convert.ToDouble(prim_height)).ToString();
                }
                catch (Exception)
                {

                }
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - 1000 + info[Defination.Prim_Radius] + 1500, centerY - 100, 0), "CL.EL.(+)" + prim_height);

            }

            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
            {
                double radius = 500;
                info[Defination.Prim_Radius] = radius;
                info[Defination.Prim_ht] = radius;

                CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Prim_block_U", centerX - 1000, centerY, radius);
                //height of prim from bottom
                // CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] - 1500, centerY - 100, 0), "CL.EL.(+)" + info[Defination.Prim_ht].ToString());

                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - 1000 - info[Defination.Prim_Radius] - 2500, centerY - 100, 0), "CL.EL.(+)" + prim_height);
                LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - 1000 - info[Defination.Prim_Radius], centerY, 0), new Point3d(centerX - 1000 - info[Defination.Prim_Radius] - 3000, centerY, 0), MyCol.LightBlue);
                string clampname = ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName;
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - 1000 - info[Defination.Prim_Radius] - 2500, centerY + 300, 0), clampname);

            }



            double height = 1000;
            double length = 5000;
            info[Defination.Sec_top_l] = length;
            info[Defination.Sec_top_b] = height;
            var sectionsize = 0.0;
            try
            {
                var section = secsupp_des.ElementAt(0).Size;

                sectionsize = Convert.ToDouble(CSECTIONSIZE(section));
            }
            catch (Exception)
            {

            }

            //upper secondary supp
            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht] - height, 0), new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht] - height, 0), SecThick.HBoth, "R", 0, sectionsize);

            //info for future
            pointsinfo[Defination.SecTopLT] = new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht], 0);
            pointsinfo[Defination.SecTopRT] = new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht], 0);
            pointsinfo[Defination.SecTopRB] = new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht] - height, 0);
            pointsinfo[Defination.SecTopLB] = new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht] - height, 0);

            double l_dist_frm_centre = 0;
            double r_dist_frm_centre = 0;
            //dimensioning
            try
            {
                var strpt = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection == "Hor").First().StPt;
                var endpt = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection == "Hor").First().EndPt;
                var midpt = ListCentalSuppoData[i].ListPrimarySuppo[0].Centroid;
                Point3d projectedpt = FindPerpendicularFoot(midpt, strpt, endpt);

                l_dist_frm_centre = Math.Round(GetDist(new Point3d(strpt), projectedpt));
                r_dist_frm_centre = Math.Round(GetDist(new Point3d(endpt), projectedpt));

                l_dist_frm_centre = Math.Min(l_dist_frm_centre, r_dist_frm_centre);
                r_dist_frm_centre = Math.Max(l_dist_frm_centre, r_dist_frm_centre);
            }
            catch (Exception)
            {

            }


            //double strtmiddist=GetDist(ldist)

            CreateDimension(pointsinfo[Defination.SecTopLT], new Point3d(centerX - 1000, centerY + info[Defination.Prim_Radius], 0), l_dist_frm_centre.ToString());
            CreateDimension(pointsinfo[Defination.SecTopRT], new Point3d(centerX - 1000, centerY + info[Defination.Prim_Radius], 0), r_dist_frm_centre.ToString());


            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length / 2 + 4000, centerY - info[Defination.Prim_ht], 0), MyCol.LightBlue);


            string TotalHt = "";
            //mtext

            try
            {
                TotalHt = Math.Round(MillimetersToMeters(ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection.ToLower() == "hor").First().Boundingboxmax.Z), 2).ToString();
            }
            catch (Exception)
            {
            }

            try
            {
                //TotalHt = MillimetersToMeters(Convert.ToDouble(TotalHt)).ToString();
            }
            catch (Exception)
            {

            }

            CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + 3500, centerY - info[Defination.Prim_ht] + 300, 0), "TOS EL.(+)" + TotalHt);

            info[Defination.Sec_ht_top] = info[Defination.Prim_ht] + height;

            height = 3000;
            length = 1000;
            // var sectionsize = 0.0;
            try
            {
                sectionsize = Convert.ToDouble(CSECTIONSIZE(ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection.ToLower() == "ver").First().Size));
            }
            catch (Exception)
            {

            }
            //sec bot
            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length / 2, centerY - info[Defination.Sec_ht_top], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top] - height, 0), new Point3d(centerX - length / 2, centerY - info[Defination.Sec_ht_top] - height, 0), SecThick.VBoth, "l");

            //info for future
            pointsinfo[Defination.SecBotLT] = new Point3d(centerX - length / 2, centerY - info[Defination.Sec_ht_top], 0);
            pointsinfo[Defination.SecBotRT] = new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top], 0);
            pointsinfo[Defination.SecBotRB] = new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top] - height, 0);
            pointsinfo[Defination.SecBotLB] = new Point3d(centerX - length / 2, centerY - info[Defination.Sec_ht_top] - height, 0);

            //dimensioning
            var botsec = "";
            try
            {
                botsec = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.BoxData.Z == ListCentalSuppoData[i].ListSecondrySuppo.Max(s => s.BoxData.Z)).First().BoxData.Z.ToString();
            }
            catch (Exception)
            {

            }

            CreateAlighDimen(AcadBlockTableRecord, AcadTransaction, new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top] - height, 0), botsec);

            info[Defination.Sec_ht_bot] = info[Defination.Sec_ht_top] + height;

            height = 1000;
            length = 3000;
            info[Defination.Concrete_l] = length;
            info[Defination.Concrete_b] = height;

            if (ListCentalSuppoData[i].ListConcreteData.Count > 0)
            {
                FixCreateBottomSupportTopType2(Document2D, AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY, height, length, i);
            }

            //support name and quantity
            CreateSupportName(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, i);


            tracex += boxlen;

        }

        [Obsolete]
        public void DRS16(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, ref double tracex, double boxlen, double boxht, ref double spaceY, Document Document2D, string SupType, [Optional] int i)
        {

            double upperYgap = 3500;
            double centerX = tracex + boxlen / 2;  // 9869.9480;
            double centerY = spaceY - upperYgap;
            //box boundaries
            //vertical line
            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex + boxlen, spaceY + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);

            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex, spaceY - boxht + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);
            var secsupp_des = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection.ToLower() == "hor").OrderByDescending(e => e.Boundingboxmax.Z);

            string prim_height = "";
            try
            {
                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
                {
                    //height of prim from bottom

                    prim_height = Math.Round((MillimetersToMeters(ListCentalSuppoData[i].ListPrimarySuppo[0].PrimaryZhtNB)), 2).ToString();
                }

                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
                {
                    prim_height = Math.Round((MillimetersToMeters(ListCentalSuppoData[i].ListPrimarySuppo[0].Centroid.Z)), 2).ToString();
                }
            }
            catch (Exception)
            {

            }

            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
            {
                FixCreatePrimarySupportwithvertex(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX + 1000, centerY, 801.5625);
                //height of prim from bottom
                try
                {
                    //    prim_height = MillimetersToMeters(Convert.ToDouble(prim_height)).ToString();
                }
                catch (Exception)
                {

                }
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + 1000 + info[Defination.Prim_Radius] + 1500, centerY - 100, 0), "CL.EL.(+)" + prim_height);

            }

            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
            {
                double radius = 500;
                info[Defination.Prim_Radius] = radius;
                info[Defination.Prim_ht] = radius;

                CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Prim_block_U", centerX + 1000, centerY, radius);


                //string clampname = ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName;
                // DrawLeader(AcadBlockTableRecord, AcadTransaction, AcadDatabase, clampname, new Point3d(centerX + 1000, centerY + 1.4 * radius, 0), new Point3d(centerX + 1000 - 4 * radius, centerY + 1.4 * radius, 0));
                //height of prim from bottom
                try
                {
                    //    prim_height = MillimetersToMeters(Convert.ToDouble(prim_height)).ToString();
                }
                catch (Exception)
                {

                }


                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + 1000 - info[Defination.Prim_Radius] - 2500, centerY - 100, 0), "CL.EL.(+)" + prim_height);
                LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + 1000 - info[Defination.Prim_Radius], centerY, 0), new Point3d(centerX + 1000 - info[Defination.Prim_Radius] - 3000, centerY, 0), MyCol.LightBlue);
                string clampname = ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName;
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + 1000 - info[Defination.Prim_Radius] - 2500, centerY + 300, 0), clampname);
            }



            double height = 1000;
            double length = 5000;
            info[Defination.Sec_top_l] = length;
            info[Defination.Sec_top_b] = height;
            var sectionsize = 0.0;
            try
            {
                var section = secsupp_des.ElementAt(0).Size;

                sectionsize = Convert.ToDouble(CSECTIONSIZE(section));
            }
            catch (Exception)
            {

            }

            //upper secondary supp
            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht] - height, 0), new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht] - height, 0), SecThick.HBoth, "R", 0, sectionsize);

            //info for future
            pointsinfo[Defination.SecTopLT] = new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht], 0);
            pointsinfo[Defination.SecTopRT] = new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht], 0);
            pointsinfo[Defination.SecTopRB] = new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht] - height, 0);
            pointsinfo[Defination.SecTopLB] = new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht] - height, 0);

            double l_dist_frm_centre = 0;
            double r_dist_frm_centre = 0;
            //dimensioning
            try
            {
                var strpt = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection == "Hor").First().StPt;
                var endpt = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection == "Hor").First().EndPt;
                var midpt = ListCentalSuppoData[i].ListPrimarySuppo[0].Centroid;
                Point3d projectedpt = FindPerpendicularFoot(midpt, strpt, endpt);

                l_dist_frm_centre = Math.Round(GetDist(new Point3d(strpt), projectedpt));
                r_dist_frm_centre = Math.Round(GetDist(new Point3d(endpt), projectedpt));

                l_dist_frm_centre = Math.Max(l_dist_frm_centre, r_dist_frm_centre);
                r_dist_frm_centre = Math.Min(l_dist_frm_centre, r_dist_frm_centre);
            }
            catch (Exception)
            {

            }


            //double strtmiddist=GetDist(ldist)

            CreateDimension(pointsinfo[Defination.SecTopLT], new Point3d(centerX + 1000, centerY + info[Defination.Prim_Radius], 0), l_dist_frm_centre.ToString());
            CreateDimension(pointsinfo[Defination.SecTopRT], new Point3d(centerX + 1000, centerY + info[Defination.Prim_Radius], 0), r_dist_frm_centre.ToString());


            string TotalHt = "";
            //mtext
            try
            {
                TotalHt = Math.Round(MillimetersToMeters(ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection.ToLower() == "hor").First().Boundingboxmax.Z), 2).ToString();
            }
            catch (Exception)
            {
            }


            try
            {
                //TotalHt = MillimetersToMeters(Convert.ToDouble(TotalHt)).ToString();
            }
            catch (Exception)
            {

            }
            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + 1000, centerY - info[Defination.Prim_ht], 0), pointsinfo[Defination.SecTopRT], MyCol.LightBlue);

            CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + 3500, centerY - info[Defination.Prim_ht] + 300, 0), "TOS EL.(+)" + TotalHt);

            info[Defination.Sec_ht_top] = info[Defination.Prim_ht] + height;

            // var sectionsize = 0.0;
            try
            {
                sectionsize = Convert.ToDouble(CSECTIONSIZE(ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection.ToLower() == "ver").First().Size));
            }
            catch (Exception)
            {

            }
            //sec bot
            height = 3000;
            length = 1000;
            //sec bot
            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length / 2, centerY - info[Defination.Sec_ht_top], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top] - height, 0), new Point3d(centerX - length / 2, centerY - info[Defination.Sec_ht_top] - height, 0), SecThick.VBoth, "l");

            //info for future
            pointsinfo[Defination.SecBotLT] = new Point3d(centerX - length / 2, centerY - info[Defination.Sec_ht_top], 0);
            pointsinfo[Defination.SecBotRT] = new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top], 0);
            pointsinfo[Defination.SecBotRB] = new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top] - height, 0);
            pointsinfo[Defination.SecBotLB] = new Point3d(centerX - length / 2, centerY - info[Defination.Sec_ht_top] - height, 0);

            //dimensioning
            var botsec = "";
            try
            {
                botsec = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.BoxData.Z == ListCentalSuppoData[i].ListSecondrySuppo.Max(s => s.BoxData.Z)).First().BoxData.Z.ToString();
            }
            catch (Exception)
            {

            }

            CreateAlighDimen(AcadBlockTableRecord, AcadTransaction, new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top] - height, 0), botsec);

            info[Defination.Sec_ht_bot] = info[Defination.Sec_ht_top] + height;

            height = 1000;
            length = 3000;
            info[Defination.Concrete_l] = length;
            info[Defination.Concrete_b] = height;

            if (ListCentalSuppoData[i].ListConcreteData.Count > 0)
            {
                FixCreateBottomSupportTopType2(Document2D, AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY, height, length, i);
            }

            //support name and quantity
            CreateSupportName(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, i);


            tracex += boxlen;

        }

        [Obsolete]
        public void DRS17(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, ref double tracex, double boxlen, double boxht, ref double spaceY, Document Document2D, string SupType, [Optional] int i)
        {

            double upperYgap = 3500;
            double centerX = tracex + boxlen / 2;  // 9869.9480;
            double centerY = spaceY - upperYgap;
            //box boundaries
            //vertical line
            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex + boxlen, spaceY + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);

            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex, spaceY - boxht + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);
            var secsupp_des = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection.ToLower() == "hor").OrderByDescending(e => e.Boundingboxmax.Z);

            string prim_height = "";
            try
            {
                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
                {
                    //height of prim from bottom

                    prim_height = Math.Round((MillimetersToMeters(ListCentalSuppoData[i].ListPrimarySuppo[0].PrimaryZhtNB)), 2).ToString();
                }

                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
                {
                    prim_height = Math.Round((MillimetersToMeters(ListCentalSuppoData[i].ListPrimarySuppo[0].Centroid.Z)), 2).ToString();
                }
            }
            catch (Exception)
            {

            }

            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
            {
                FixCreatePrimarySupportwithvertex(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY, 801.5625);
                //height of prim from bottom
                try
                {
                    //    prim_height = MillimetersToMeters(Convert.ToDouble(prim_height)).ToString();
                }
                catch (Exception)
                {

                }
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] + 1500, centerY - 100, 0), "CL.EL.(+)" + prim_height);

            }

            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
            {
                double radius = 500;
                info[Defination.Prim_Radius] = radius;
                info[Defination.Prim_ht] = radius;

                CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Prim_block_U", centerX, centerY, radius);

                //string clampname = ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName;
                //DrawLeader(AcadBlockTableRecord, AcadTransaction, AcadDatabase, clampname, new Point3d(centerX, centerY + 1.4 * radius, 0), new Point3d(centerX - 4 * radius, centerY + 1.4 * radius, 0));

                //height of prim from bottom
                try
                {
                    //    prim_height = MillimetersToMeters(Convert.ToDouble(prim_height)).ToString();
                }
                catch (Exception)
                {

                }
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - info[Defination.Prim_Radius] - 2500, centerY - 100, 0), "CL.EL.(+)" + prim_height);
                LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - info[Defination.Prim_Radius], centerY, 0), new Point3d(centerX - info[Defination.Prim_Radius] - 3000, centerY, 0), MyCol.LightBlue);
                string clampname = ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName;
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - info[Defination.Prim_Radius] - 2500, centerY + 300, 0), clampname);

            }



            double height = 1000;
            double length = 2000;
            info[Defination.Sec_top_l] = length;
            info[Defination.Sec_top_b] = height;
            var sectionsize = 0.0;
            try
            {
                var section = secsupp_des.ElementAt(0).Size;

                sectionsize = Convert.ToDouble(CSECTIONSIZE(section));
            }
            catch (Exception)
            {

            }
            //upper secondary supp
            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht] - height, 0), new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht] - height, 0), SecThick.HBoth, "l", 0, sectionsize);

            //info for future
            pointsinfo[Defination.SecTopLT] = new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht], 0);
            pointsinfo[Defination.SecTopRT] = new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht], 0);
            pointsinfo[Defination.SecTopRB] = new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht] - height, 0);
            pointsinfo[Defination.SecTopLB] = new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht] - height, 0);

            double l_dist_frm_centre = 0;
            double r_dist_frm_centre = 0;
            //dimensioning
            try
            {
                var strpt = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection == "Hor").First().StPt;
                var endpt = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection == "Hor").First().EndPt;
                var midpt = ListCentalSuppoData[i].ListPrimarySuppo[0].Centroid;
                Point3d projectedpt = FindPerpendicularFoot(midpt, strpt, endpt);

                l_dist_frm_centre = Math.Round(GetDist(new Point3d(strpt), projectedpt));
                r_dist_frm_centre = Math.Round(GetDist(new Point3d(endpt), projectedpt));
            }
            catch (Exception)
            {

            }


            //double strtmiddist=GetDist(ldist)

            CreateDimension(new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX, centerY + info[Defination.Prim_Radius], 0), l_dist_frm_centre.ToString());
            CreateDimension(new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX, centerY + info[Defination.Prim_Radius], 0), r_dist_frm_centre.ToString());


            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length / 2 + 4000, centerY - info[Defination.Prim_ht], 0), MyCol.LightBlue);


            string TotalHt = "";
            //mtext
            try
            {
                TotalHt = Math.Round(MillimetersToMeters(ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection.ToLower() == "hor").First().Boundingboxmax.Z), 2).ToString();
            }
            catch (Exception)
            {
            }

            try
            {
                //TotalHt = MillimetersToMeters(Convert.ToDouble(TotalHt)).ToString();
            }
            catch (Exception)
            {

            }
            CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + 2500, centerY - info[Defination.Prim_ht] + 300, 0), "TOS EL.(+)" + TotalHt);

            info[Defination.Sec_ht_top] = info[Defination.Prim_ht] + height;

            info[Defination.Sec_ht_bot] = info[Defination.Sec_ht_top];

            height = 1000;
            length = 3000;
            info[Defination.Concrete_l] = length;
            info[Defination.Concrete_b] = height;

            if (ListCentalSuppoData[i].ListConcreteData.Count > 0)
            {
                FixCreateBottomSupportTopType2(Document2D, AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY, height, length, i);
            }

            //support name and quantity
            CreateSupportName(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, i);


            tracex += boxlen;

        }


        [Obsolete]
        public void DRS18(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, ref double tracex, double boxlen, double boxht, ref double spaceY, Document Document2D, string SupType, [Optional] int i)
        {
            double upperYgap = 3500;

            double centerX = tracex + boxlen / 2;  // 9869.9480;
            double centerY = spaceY - upperYgap;

            //for support18 only
            centerY += 1000;
            centerX -= 1500;

            //box boundaries
            //vertical line
            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex + boxlen, spaceY + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);

            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex, spaceY - boxht + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);

            //CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Prim_block_U", centerX, centerY, radius);
            var primsup_des = ListCentalSuppoData[i].ListPrimarySuppo.OrderByDescending(e => e.Centroid.Z);
            var secsupp_des = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection.ToLower() == "hor").OrderByDescending(e => e.Boundingboxmax.Z);
            string prim_height = "";
            try
            {
                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
                {
                    //height of prim from bottom

                    prim_height = Math.Round((MillimetersToMeters(primsup_des.ElementAt(0).PrimaryZhtNB)), 2).ToString();
                }

                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
                {
                    prim_height = Math.Round((MillimetersToMeters(primsup_des.ElementAt(0).Centroid.Z)), 2).ToString();
                }
            }
            catch (Exception)
            {

            }

            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
            {
                FixCreatePrimarySupportwithvertex(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY, 801.5625);
                //height of prim from bottom
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] + 1500, centerY - 100, 0), "CL.EL.(+)" + info[Defination.Prim_ht].ToString());

            }

            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
            {
                double radius = 500;
                info[Defination.Prim_Radius] = radius;
                info[Defination.Prim_ht] = radius;

                CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Prim_block_U", centerX, centerY, radius);
                //height of prim from bottom
                // CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] - 1500, centerY - 100, 0), "CL.EL.(+)" + info[Defination.Prim_ht].ToString());

                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - info[Defination.Prim_Radius] - 2500, centerY - 100, 0), "CL.EL.(+)" + prim_height);
                LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - info[Defination.Prim_Radius], centerY, 0), new Point3d(centerX - info[Defination.Prim_Radius] - 3000, centerY, 0), MyCol.LightBlue);
                string clampname = ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName;
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - info[Defination.Prim_Radius] - 2500, centerY + 300, 0), clampname);

            }



            //upper top sec
            double height = 1000;
            double length = 4000;
            info[Defination.Sec_top_l] = length;
            info[Defination.Sec_top_b] = height;
            var sectionsize = 0.0;
            try
            {
                var section = secsupp_des.ElementAt(0).Size;

                sectionsize = Convert.ToDouble(CSECTIONSIZE(section));
            }
            catch (Exception)
            {

            }
            //double ht_frm_cen = 1220.7383;0.33,0.66
            //upper top secondary supp
            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length * 0.34, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length * 0.66, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length * 0.66, centerY - info[Defination.Prim_ht] - height, 0), new Point3d(centerX - length * 0.34, centerY - info[Defination.Prim_ht] - height, 0), SecThick.HBoth, "l", 0, sectionsize);


            //info for future
            pointsinfo[Defination.SecTopLT] = new Point3d(centerX - length * 0.34, centerY - info[Defination.Prim_ht], 0);
            pointsinfo[Defination.SecTopRT] = new Point3d(centerX + length * 0.66, centerY - info[Defination.Prim_ht], 0);
            pointsinfo[Defination.SecTopRB] = new Point3d(centerX + length * 0.66, centerY - info[Defination.Prim_ht] - height, 0);
            pointsinfo[Defination.SecTopLB] = new Point3d(centerX - length * 0.34, centerY - info[Defination.Prim_ht] - height, 0);

            double l_dist_frm_centre = 0;
            double r_dist_frm_centre = 0;
            //dimensioning
            try
            {
                var strpt = secsupp_des.ElementAt(0).StPt;
                var endpt = secsupp_des.ElementAt(0).EndPt;
                var midpt = primsup_des.ElementAt(0).Centroid;
                Point3d projectedpt = FindPerpendicularFoot(midpt, strpt, endpt);

                l_dist_frm_centre = Math.Round(GetDist(new Point3d(strpt), projectedpt));
                r_dist_frm_centre = Math.Round(GetDist(new Point3d(endpt), projectedpt));

                l_dist_frm_centre = Math.Min(l_dist_frm_centre, r_dist_frm_centre);
                r_dist_frm_centre = Math.Max(l_dist_frm_centre, r_dist_frm_centre);
            }
            catch (Exception)
            {

            }
            //dimensioning
            CreateDimension(new Point3d(centerX - length * 0.34, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX, centerY + info[Defination.Prim_Radius], 0), l_dist_frm_centre.ToString());
            CreateDimension(new Point3d(centerX + length * 0.66, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX, centerY + info[Defination.Prim_Radius], 0), r_dist_frm_centre.ToString());





            string TotalHt = "";

            //mtext
            try
            {
                TotalHt = Math.Round(MillimetersToMeters(secsupp_des.ElementAt(0).Boundingboxmax.Z), 2).ToString();
            }
            catch (Exception)
            {
            }


            try
            {
                //TotalHt = MillimetersToMeters(Convert.ToDouble(TotalHt)).ToString();
            }
            catch (Exception)
            {

            }

            CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + 2500, centerY - info[Defination.Prim_ht] + 300, 0), "TOS EL.(+)" + TotalHt);
            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + length * 0.66, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length * 0.66 + 4000, centerY - info[Defination.Prim_ht], 0), MyCol.LightBlue);

            info[Defination.Sec_ht_top] = info[Defination.Prim_ht] + height;

            try
            {
                sectionsize = Convert.ToDouble(CSECTIONSIZE(ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection.ToLower() == "ver").First().Size));
            }
            catch (Exception)
            {

            }
            //bot sec
            height = 5000;
            length = 1000;
            info[Defination.Sec_bot_l] = length;
            info[Defination.Sec_bot_b] = height;

            info[Defination.Sec_ht_bot] = info[Defination.Sec_ht_top] + height;
            //botsec sec supp
            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(pointsinfo[Defination.SecTopRB].X - info[Defination.Sec_bot_l] - 500, pointsinfo[Defination.SecTopRB].Y, 0), new Point3d(pointsinfo[Defination.SecTopRB].X - 500, pointsinfo[Defination.SecTopRB].Y, 0), new Point3d(pointsinfo[Defination.SecTopRB].X - 500, pointsinfo[Defination.SecTopRB].Y - info[Defination.Sec_bot_b], 0), new Point3d(pointsinfo[Defination.SecTopRB].X - info[Defination.Sec_bot_l] - 500, pointsinfo[Defination.SecTopRB].Y - info[Defination.Sec_bot_b], 0), SecThick.VBoth, "r", 1700);

            //info for future
            pointsinfo[Defination.SecBotLT] = new Point3d(pointsinfo[Defination.SecTopRB].X - info[Defination.Sec_bot_l] - 500, pointsinfo[Defination.SecTopRB].Y, 0);
            pointsinfo[Defination.SecBotRT] = new Point3d(pointsinfo[Defination.SecTopRB].X - 500, pointsinfo[Defination.SecTopRB].Y, 0);
            pointsinfo[Defination.SecBotRB] = new Point3d(pointsinfo[Defination.SecTopRB].X - 500, pointsinfo[Defination.SecTopRB].Y - info[Defination.Sec_bot_b], 0);
            pointsinfo[Defination.SecBotLB] = new Point3d(pointsinfo[Defination.SecTopRB].X - info[Defination.Sec_bot_l] - 500, pointsinfo[Defination.SecTopRB].Y - info[Defination.Sec_bot_b], 0);
            //dimensioning
            var botsec = "";
            try
            {
                botsec = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.BoxData.Z == ListCentalSuppoData[i].ListSecondrySuppo.Max(s => s.BoxData.Z)).First().BoxData.Z.ToString();
            }
            catch (Exception)
            {

            }


            CreateAlighDimen(AcadBlockTableRecord, AcadTransaction, pointsinfo[Defination.SecBotRT], pointsinfo[Defination.SecBotRB], botsec);



            //left side down secondary support
            height = 1000;
            length = 2500;
            extrainfo["sec_Left_Bot_L"] = length;
            extrainfo["sec_Left_Bot_B"] = height;

            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(pointsinfo[Defination.SecBotLB].X - extrainfo["sec_Left_Bot_L"], pointsinfo[Defination.SecBotLB].Y + 500 + extrainfo["sec_Left_Bot_B"], 0), new Point3d(pointsinfo[Defination.SecBotLB].X, pointsinfo[Defination.SecBotLB].Y + 500 + extrainfo["sec_Left_Bot_B"], 0), new Point3d(pointsinfo[Defination.SecBotLB].X, pointsinfo[Defination.SecBotLB].Y + 500, 0), new Point3d(pointsinfo[Defination.SecBotLB].X - extrainfo["sec_Left_Bot_L"], pointsinfo[Defination.SecBotLB].Y + 500, 0), SecThick.HBoth, "l");

            //verti dimen left down
            try
            {
                TotalHt = Math.Round(MillimetersToMeters(secsupp_des.ElementAt(2).Boundingboxmax.Z), 2).ToString();
            }
            catch (Exception)
            {
            }
            CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(pointsinfo[Defination.SecBotLB].X - extrainfo["sec_Left_Bot_L"] - 3500, centerY - info[Defination.Prim_ht] - info[Defination.Sec_bot_b] + extrainfo["Sec_low_b"] + 500 + 300, 0), "TOS EL.(+)" + TotalHt);
            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(pointsinfo[Defination.SecBotLB].X - extrainfo["sec_Left_Bot_L"], pointsinfo[Defination.SecBotLB].Y + 500 + extrainfo["sec_Left_Bot_B"], 0), new Point3d(pointsinfo[Defination.SecBotLB].X - extrainfo["sec_Left_Bot_L"] - 4000, pointsinfo[Defination.SecBotLB].Y + 500 + extrainfo["sec_Left_Bot_B"], 0), MyCol.LightBlue);

            //left side down primary support

            try
            {
                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
                {
                    //height of prim from bottom

                    prim_height = Math.Round((MillimetersToMeters(primsup_des.ElementAt(2).PrimaryZhtNB)), 2).ToString();
                }

                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
                {
                    prim_height = Math.Round((MillimetersToMeters(primsup_des.ElementAt(2).Centroid.Z)), 2).ToString();
                }
            }
            catch (Exception)
            {

            }


            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
            {
                double radius = 801.5625;

                FixCreatePrimarySupportwithvertex(AcadBlockTableRecord, AcadTransaction, AcadDatabase, pointsinfo[Defination.SecBotLB].X - extrainfo["sec_Left_Bot_L"] + 1000, pointsinfo[Defination.SecBotLB].Y + 500 + extrainfo["sec_Left_Bot_B"] + (radius * 1.5), radius, "l");
                //height of prim from bottom
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(pointsinfo[Defination.SecBotLB].X - extrainfo["sec_Left_Bot_L"] + 1000 - info[Defination.Prim_Radius] - 2000, pointsinfo[Defination.SecBotLB].Y + 500 + extrainfo["sec_Left_Bot_B"] + (radius * 1.5) - 100, 0), "CL.EL.(+)" + prim_height);

            }

            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
            {
                double radius = 300;
                info[Defination.Prim_Radius] = radius;
                info[Defination.Prim_ht] = radius;

                CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Prim_block_U", pointsinfo[Defination.SecBotLB].X - extrainfo["sec_Left_Bot_L"] + 1000, pointsinfo[Defination.SecBotLB].Y + 500 + extrainfo["sec_Left_Bot_B"] + radius, radius);
                //height of prim from bottom
                // CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] - 1500, centerY - 100, 0), "CL.EL.(+)" + info[Defination.Prim_ht].ToString());


                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(pointsinfo[Defination.SecBotLB].X - extrainfo["sec_Left_Bot_L"] + 1000 - info[Defination.Prim_Radius] - 2500, pointsinfo[Defination.SecBotLB].Y + 500 + extrainfo["sec_Left_Bot_B"] + radius - 100, 0), "CL.EL.(+)" + prim_height);
                LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(pointsinfo[Defination.SecBotLB].X - extrainfo["sec_Left_Bot_L"] + 1000 - info[Defination.Prim_Radius], pointsinfo[Defination.SecBotLB].Y + 500 + extrainfo["sec_Left_Bot_B"] + radius, 0), new Point3d(pointsinfo[Defination.SecBotLB].X - extrainfo["sec_Left_Bot_L"] + 1000 - info[Defination.Prim_Radius] - 3000, pointsinfo[Defination.SecBotLB].Y + 500 + extrainfo["sec_Left_Bot_B"] + radius, 0), MyCol.LightBlue);
                string clampname = ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName;
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(pointsinfo[Defination.SecBotLB].X - extrainfo["sec_Left_Bot_L"] + 1000 - info[Defination.Prim_Radius] - 2500, pointsinfo[Defination.SecBotLB].Y + 500 + extrainfo["sec_Left_Bot_B"] + radius + 300, 0), clampname);

            }

            //left side down dimensioning hori
            l_dist_frm_centre = 0;
            r_dist_frm_centre = 0;
            //dimensioning
            try
            {
                var strpt = secsupp_des.ElementAt(2).StPt;
                var endpt = secsupp_des.ElementAt(2).EndPt;
                var midpt = primsup_des.ElementAt(2).Centroid;

                Point3d projectedpt = FindPerpendicularFoot(midpt, strpt, endpt);

                l_dist_frm_centre = Math.Round(GetDist(new Point3d(strpt), projectedpt));
                r_dist_frm_centre = Math.Round(GetDist(new Point3d(endpt), projectedpt));

                l_dist_frm_centre = Math.Min(l_dist_frm_centre, r_dist_frm_centre);
                r_dist_frm_centre = Math.Max(l_dist_frm_centre, r_dist_frm_centre);
            }
            catch (Exception)
            {

            }
            //dimensioning
            CreateDimension(new Point3d(pointsinfo[Defination.SecBotLB].X - extrainfo["sec_Left_Bot_L"], pointsinfo[Defination.SecBotLB].Y + 500 + extrainfo["sec_Left_Bot_B"], 0), new Point3d(pointsinfo[Defination.SecBotLB].X - extrainfo["sec_Left_Bot_L"] + 1000, pointsinfo[Defination.SecBotLB].Y + 500 + extrainfo["sec_Left_Bot_B"] + (info[Defination.Prim_Radius] * 1.5) + info[Defination.Prim_Radius], 0), l_dist_frm_centre.ToString(), 300);
            CreateDimension(new Point3d(pointsinfo[Defination.SecBotLB].X, pointsinfo[Defination.SecBotLB].Y + 500 + extrainfo["sec_Left_Bot_B"], 0), new Point3d(pointsinfo[Defination.SecBotLB].X - extrainfo["sec_Left_Bot_L"] + 1000, pointsinfo[Defination.SecBotLB].Y + 500 + extrainfo["sec_Left_Bot_B"] + (info[Defination.Prim_Radius] * 1.5) + info[Defination.Prim_Radius], 0), r_dist_frm_centre.ToString(), 300);

            //right side down secondary support
            height = 1000;
            length = 5000;
            extrainfo["sec_Right_Bot_L"] = length;
            extrainfo["sec_Right_Bot_B"] = height;


            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(pointsinfo[Defination.SecBotRB].X, pointsinfo[Defination.SecBotRB].Y + 1500 + extrainfo["sec_Right_Bot_B"], 0), new Point3d(pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Bot_L"], pointsinfo[Defination.SecBotRB].Y + 1500 + extrainfo["sec_Right_Bot_B"], 0), new Point3d(pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Bot_L"], pointsinfo[Defination.SecBotRB].Y + 1500, 0), new Point3d(pointsinfo[Defination.SecBotRB].X, pointsinfo[Defination.SecBotRB].Y + 1500, 0), SecThick.HBoth, "R");

            //verti dimen right up
            try
            {
                TotalHt = Math.Round(MillimetersToMeters(secsupp_des.ElementAt(2).Boundingboxmax.Z), 2).ToString();
            }
            catch (Exception)
            {
            }
            CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(pointsinfo[Defination.SecBotRB].X + 3500, pointsinfo[Defination.SecBotRB].Y + 1500 + extrainfo["sec_Right_Bot_B"], 0), "TOS EL.(+)" + TotalHt);
            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(pointsinfo[Defination.SecBotRB].X, pointsinfo[Defination.SecBotRB].Y + 1500 + extrainfo["sec_Right_Bot_B"], 0), new Point3d(pointsinfo[Defination.SecBotRB].X + 4000, pointsinfo[Defination.SecBotRB].Y + 1500 + extrainfo["sec_Right_Bot_B"], 0), MyCol.LightBlue);


            //right side down primary support
            try
            {
                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
                {
                    //height of prim from bottom

                    prim_height = Math.Round((MillimetersToMeters(primsup_des.ElementAt(1).PrimaryZhtNB)), 2).ToString();
                }

                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
                {
                    prim_height = Math.Round((MillimetersToMeters(primsup_des.ElementAt(1).Centroid.Z)), 2).ToString();
                }
            }
            catch (Exception)
            {

            }
            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
            {
                double radius = 801.5625;

                FixCreatePrimarySupportwithvertex(AcadBlockTableRecord, AcadTransaction, AcadDatabase, pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Bot_L"] - 1500, pointsinfo[Defination.SecBotRB].Y + 1500 + extrainfo["sec_Right_Bot_B"] + (radius * 1.5), radius);
                //height of prim from bottom
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Bot_L"] - 1500 + info[Defination.Prim_Radius] + 1500, pointsinfo[Defination.SecBotRB].Y + 1500 + extrainfo["sec_Right_Bot_B"] + (radius * 1.5) - 100, 0), "CL.EL.(+)" + prim_height);

            }

            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
            {
                double radius = 300;
                info[Defination.Prim_Radius] = radius;
                info[Defination.Prim_ht] = radius;

                CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Prim_block_U", pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Bot_L"] - 1500, pointsinfo[Defination.SecBotRB].Y + 1500 + extrainfo["sec_Right_Bot_B"] + radius, radius);
                //height of prim from bottom
                // CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] - 1500, centerY - 100, 0), "CL.EL.(+)" + info[Defination.Prim_ht].ToString());

                // string clampname = ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName;
                // DrawLeader(AcadBlockTableRecord, AcadTransaction, AcadDatabase, clampname, new Point3d(pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Bot_L"] - 1500, pointsinfo[Defination.SecBotRB].Y + 1500 + extrainfo["sec_Right_Bot_B"] + radius, 0), new Point3d(pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Bot_L"] - 1500 - 4 * radius, pointsinfo[Defination.SecBotRB].Y + 1500 + extrainfo["sec_Right_Bot_B"] + radius + 1.4 * radius, 0));

                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Bot_L"] - 1500 + info[Defination.Prim_Radius] + 2500, pointsinfo[Defination.SecBotRB].Y + 1500 + extrainfo["sec_Right_Bot_B"] + radius - 100, 0), "CL.EL.(+)" + prim_height);
                LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Bot_L"] - 1500 + info[Defination.Prim_Radius], pointsinfo[Defination.SecBotRB].Y + 1500 + extrainfo["sec_Right_Bot_B"] + radius, 0), new Point3d(pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Bot_L"] - 1500 + info[Defination.Prim_Radius] + 3000, pointsinfo[Defination.SecBotRB].Y + 1500 + extrainfo["sec_Right_Bot_B"] + radius, 0), MyCol.LightBlue);
                string clampname = ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName;
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Bot_L"] - 1500 + info[Defination.Prim_Radius] + 2500, pointsinfo[Defination.SecBotRB].Y + 1500 + extrainfo["sec_Right_Bot_B"] + radius + 300, 0), clampname);

            }

            //hori dimesnsioning right sec supp
            l_dist_frm_centre = 0;
            r_dist_frm_centre = 0;
            //dimensioning
            try
            {
                var strpt = secsupp_des.ElementAt(1).StPt;
                var endpt = secsupp_des.ElementAt(1).EndPt;
                var midpt = primsup_des.ElementAt(1).Centroid;

                Point3d projectedpt = FindPerpendicularFoot(midpt, strpt, endpt);

                l_dist_frm_centre = Math.Round(GetDist(new Point3d(strpt), projectedpt));
                r_dist_frm_centre = Math.Round(GetDist(new Point3d(endpt), projectedpt));

                l_dist_frm_centre = Math.Max(l_dist_frm_centre, r_dist_frm_centre);
                r_dist_frm_centre = Math.Min(l_dist_frm_centre, r_dist_frm_centre);
            }
            catch (Exception)
            {

            }
            //dimensioning
            CreateDimension(new Point3d(pointsinfo[Defination.SecBotRB].X, pointsinfo[Defination.SecBotRB].Y + 1500 + extrainfo["sec_Right_Bot_B"], 0), new Point3d(pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Bot_L"] - 1500, pointsinfo[Defination.SecBotRB].Y + 1500 + extrainfo["sec_Right_Bot_B"] + (info[Defination.Prim_Radius] * 1.5) + info[Defination.Prim_Radius], 0), l_dist_frm_centre.ToString(), 500);
            CreateDimension(new Point3d(pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Bot_L"], pointsinfo[Defination.SecBotRB].Y + 1500 + extrainfo["sec_Right_Bot_B"], 0), new Point3d(pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Bot_L"] - 1500, pointsinfo[Defination.SecBotRB].Y + 1500 + extrainfo["sec_Right_Bot_B"] + (info[Defination.Prim_Radius] * 1.5) + info[Defination.Prim_Radius], 0), r_dist_frm_centre.ToString(), 500);


            //draw plate/concrete support 
            height = 1000;
            length = 3000;
            info[Defination.Plate_l] = length;
            info[Defination.Plate_b] = height;


            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d((pointsinfo[Defination.SecBotRB].X + pointsinfo[Defination.SecBotLB].X) / 2 - info[Defination.Plate_l] / 2, pointsinfo[Defination.SecBotLB].Y, 0), new Point3d((pointsinfo[Defination.SecBotRB].X + pointsinfo[Defination.SecBotLB].X) / 2 + info[Defination.Plate_l] / 2, pointsinfo[Defination.SecBotLB].Y, 0), new Point3d((pointsinfo[Defination.SecBotRB].X + pointsinfo[Defination.SecBotLB].X) / 2 + info[Defination.Plate_l] / 2, pointsinfo[Defination.SecBotLB].Y - info[Defination.Plate_b], 0), new Point3d((pointsinfo[Defination.SecBotRB].X + pointsinfo[Defination.SecBotLB].X) / 2 - info[Defination.Plate_l] / 2, pointsinfo[Defination.SecBotLB].Y - info[Defination.Plate_b], 0), SecThick.Nothing);


            height = 1000;
            length = 3000;
            info[Defination.Concrete_l] = length;
            info[Defination.Concrete_b] = height;

            if (ListCentalSuppoData[i].ListConcreteData.Count > 0)
            {
                FixCreateBottomSupportTopType2(Document2D, AcadBlockTableRecord, AcadTransaction, AcadDatabase, (pointsinfo[Defination.SecBotRB].X + pointsinfo[Defination.SecBotLB].X) / 2, centerY, height, length, i);
            }

            //support name and quantity
            CreateSupportName(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, i);

            tracex += boxlen;
        }

        [Obsolete]
        public void DRS19(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, ref double tracex, double boxlen, double boxht, ref double spaceY, Document Document2D, string SupType, [Optional] int i)
        {
            double upperYgap = 3500;

            double centerX = tracex + boxlen / 2;  // 9869.9480;
            double centerY = spaceY - upperYgap;

            //for support18 only
            centerY += 2500;
            centerX -= 1500;

            //box boundaries
            //vertical line
            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex + boxlen, spaceY + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);

            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex, spaceY - boxht + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);


            var primsup_des = ListCentalSuppoData[i].ListPrimarySuppo.OrderByDescending(e => e.Centroid.Z);
            var secsupp_des = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection.ToLower() == "hor").OrderByDescending(e => e.Boundingboxmax.Z);


            string prim_height = "";
            try
            {
                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
                {
                    //height of prim from bottom

                    prim_height = Math.Round((MillimetersToMeters(primsup_des.ElementAt(0).PrimaryZhtNB)), 2).ToString();
                }

                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
                {
                    prim_height = Math.Round((MillimetersToMeters(primsup_des.ElementAt(0).Centroid.Z)), 2).ToString();
                }
            }
            catch (Exception)
            {

            }
            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
            {
                FixCreatePrimarySupportwithvertex(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY, 801.5625);
                //height of prim from bottom
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] + 1500, centerY - 100, 0), "CL.EL.(+)" + prim_height);

            }

            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
            {
                double radius = 300;
                info[Defination.Prim_Radius] = radius;
                info[Defination.Prim_ht] = radius;

                CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Prim_block_U", centerX, centerY, radius);
                //height of prim from bottom
                // CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] - 1500, centerY - 100, 0), "CL.EL.(+)" + info[Defination.Prim_ht].ToString());

                //string clampname = ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName;
                //DrawLeader(AcadBlockTableRecord, AcadTransaction, AcadDatabase, clampname, new Point3d(centerX, centerY + 1.4 * radius, 0), new Point3d(centerX - 4 * radius, centerY + 1.4 * radius, 0));
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - info[Defination.Prim_Radius] - 2500, centerY - 100, 0), "CL.EL.(+)" + prim_height);
                LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - info[Defination.Prim_Radius], centerY, 0), new Point3d(centerX - info[Defination.Prim_Radius] - 3000, centerY, 0), MyCol.LightBlue);
                string clampname = ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName;
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - info[Defination.Prim_Radius] - 2500, centerY + 300, 0), clampname);

            }




            double height = 1000;
            double length = 4000;
            info[Defination.Sec_top_l] = length;
            info[Defination.Sec_top_b] = height;
            var sectionsize = 0.0;
            try
            {
                var section = secsupp_des.ElementAt(0).Size;

                sectionsize = Convert.ToDouble(CSECTIONSIZE(section));
            }
            catch (Exception)
            {

            }
            //double ht_frm_cen = 1220.7383;0.33,0.66

            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length * 0.34, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length * 0.66, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length * 0.66, centerY - info[Defination.Prim_ht] - height, 0), new Point3d(centerX - length * 0.34, centerY - info[Defination.Prim_ht] - height, 0), SecThick.HBoth, "l", 0, sectionsize);


            //info for future
            pointsinfo[Defination.SecTopLT] = new Point3d(centerX - length * 0.34, centerY - info[Defination.Prim_ht], 0);
            pointsinfo[Defination.SecTopRT] = new Point3d(centerX + length * 0.66, centerY - info[Defination.Prim_ht], 0);
            pointsinfo[Defination.SecTopRB] = new Point3d(centerX + length * 0.66, centerY - info[Defination.Prim_ht] - height, 0);
            pointsinfo[Defination.SecTopLB] = new Point3d(centerX - length * 0.34, centerY - info[Defination.Prim_ht] - height, 0);


            double l_dist_frm_centre = 0;
            double r_dist_frm_centre = 0;
            //dimensioning
            try
            {
                var strpt = secsupp_des.ElementAt(0).StPt;
                var endpt = secsupp_des.ElementAt(0).EndPt;
                var midpt = primsup_des.ElementAt(0).Centroid;
                Point3d projectedpt = FindPerpendicularFoot(midpt, strpt, endpt);

                l_dist_frm_centre = Math.Round(GetDist(new Point3d(strpt), projectedpt));
                r_dist_frm_centre = Math.Round(GetDist(new Point3d(endpt), projectedpt));

                l_dist_frm_centre = Math.Min(l_dist_frm_centre, r_dist_frm_centre);
                r_dist_frm_centre = Math.Max(l_dist_frm_centre, r_dist_frm_centre);
            }
            catch (Exception)
            {

            }

            //dimensioning
            CreateDimension(new Point3d(centerX - length * 0.34, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX, centerY + info[Defination.Prim_Radius], 0), l_dist_frm_centre.ToString(), 300);
            CreateDimension(new Point3d(centerX + length * 0.66, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX, centerY + info[Defination.Prim_Radius], 0), r_dist_frm_centre.ToString(), 300);


            string TotalHt = "";

            //mtext
            try
            {
                TotalHt = Math.Round(MillimetersToMeters(secsupp_des.ElementAt(0).Boundingboxmax.Z), 2).ToString();
            }
            catch (Exception)
            {
            }

            CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + 3500, centerY - info[Defination.Prim_ht] + 300, 0), "TOS EL.(+)" + TotalHt);
            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + length * 0.66, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length * 0.66 + 4000, centerY - info[Defination.Prim_ht], 0), MyCol.LightBlue);
            try
            {
                //TotalHt = MillimetersToMeters(Convert.ToDouble(TotalHt)).ToString();
            }
            catch (Exception)
            {

            }



            info[Defination.Sec_ht_top] = info[Defination.Prim_ht] + height;

            try
            {
                sectionsize = Convert.ToDouble(CSECTIONSIZE(ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection.ToLower() == "ver").First().Size));
            }
            catch (Exception)
            {

            }
            //bot sec
            height = 6500;
            length = 1000;
            info[Defination.Sec_bot_l] = length;
            info[Defination.Sec_bot_b] = height;

            info[Defination.Sec_ht_bot] = info[Defination.Sec_ht_top] + height;

            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(pointsinfo[Defination.SecTopRB].X - info[Defination.Sec_bot_l] - 500, pointsinfo[Defination.SecTopRB].Y, 0), new Point3d(pointsinfo[Defination.SecTopRB].X - 500, pointsinfo[Defination.SecTopRB].Y, 0), new Point3d(pointsinfo[Defination.SecTopRB].X - 500, pointsinfo[Defination.SecTopRB].Y - info[Defination.Sec_bot_b], 0), new Point3d(pointsinfo[Defination.SecTopRB].X - info[Defination.Sec_bot_l] - 500, pointsinfo[Defination.SecTopRB].Y - info[Defination.Sec_bot_b], 0), SecThick.VBoth, "l");

            //info for future
            pointsinfo[Defination.SecBotLT] = new Point3d(pointsinfo[Defination.SecTopRB].X - info[Defination.Sec_bot_l] - 500, pointsinfo[Defination.SecTopRB].Y, 0);
            pointsinfo[Defination.SecBotRT] = new Point3d(pointsinfo[Defination.SecTopRB].X - 500, pointsinfo[Defination.SecTopRB].Y, 0);
            pointsinfo[Defination.SecBotRB] = new Point3d(pointsinfo[Defination.SecTopRB].X - 500, pointsinfo[Defination.SecTopRB].Y - info[Defination.Sec_bot_b], 0);
            pointsinfo[Defination.SecBotLB] = new Point3d(pointsinfo[Defination.SecTopRB].X - info[Defination.Sec_bot_l] - 500, pointsinfo[Defination.SecTopRB].Y - info[Defination.Sec_bot_b], 0);
            //dimensioning
            var botsec = "";
            try
            {
                botsec = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.BoxData.Z == ListCentalSuppoData[i].ListSecondrySuppo.Max(s => s.BoxData.Z)).First().BoxData.Z.ToString();
            }
            catch (Exception)
            {

            }


            CreateAlighDimen(AcadBlockTableRecord, AcadTransaction, pointsinfo[Defination.SecBotRT], pointsinfo[Defination.SecBotRB], botsec);



            //left side down secondary support
            height = 1000;
            length = 2500;
            extrainfo["sec_Left_Bot_L"] = length;
            extrainfo["sec_Left_Bot_B"] = height;

            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(pointsinfo[Defination.SecBotLT].X - extrainfo["sec_Left_Bot_L"], pointsinfo[Defination.SecBotLT].Y - 1000, 0), new Point3d(pointsinfo[Defination.SecBotLT].X, pointsinfo[Defination.SecBotLT].Y - 1000, 0), new Point3d(pointsinfo[Defination.SecBotLT].X, pointsinfo[Defination.SecBotLT].Y - 1000 - extrainfo["sec_Left_Bot_B"], 0), new Point3d(pointsinfo[Defination.SecBotLT].X - extrainfo["sec_Left_Bot_L"], pointsinfo[Defination.SecBotLT].Y - 1000 - extrainfo["sec_Left_Bot_B"], 0), SecThick.HBoth, "l");

            //verti dimen secsupp
            try
            {
                TotalHt = Math.Round(MillimetersToMeters(secsupp_des.ElementAt(1).Boundingboxmax.Z), 2).ToString();
            }
            catch (Exception)
            {
            }

            CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(pointsinfo[Defination.SecBotLT].X - extrainfo["sec_Left_Bot_L"] - 3500, pointsinfo[Defination.SecBotLT].Y - 1000 + 300, 0), "TOS EL.(+)" + TotalHt);
            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(pointsinfo[Defination.SecBotLT].X - extrainfo["sec_Left_Bot_L"], pointsinfo[Defination.SecBotLT].Y - 1000, 0), new Point3d(pointsinfo[Defination.SecBotLT].X - extrainfo["sec_Left_Bot_L"] - 4000, pointsinfo[Defination.SecBotLT].Y - 1000, 0), MyCol.LightBlue);

            //left side down primary support

            //verti dimen prim ht
            try
            {
                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
                {
                    //height of prim from bottom

                    prim_height = Math.Round((MillimetersToMeters(primsup_des.ElementAt(1).PrimaryZhtNB)), 2).ToString();
                }

                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
                {
                    prim_height = Math.Round((MillimetersToMeters(primsup_des.ElementAt(1).Centroid.Z)), 2).ToString();
                }
            }
            catch (Exception)
            {

            }

            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
            {
                double radius = 300;

                FixCreatePrimarySupportwithvertex(AcadBlockTableRecord, AcadTransaction, AcadDatabase, pointsinfo[Defination.SecBotLB].X - extrainfo["sec_Left_Bot_L"] + 1000, pointsinfo[Defination.SecBotLT].Y - 1000 + (radius * 1.5), radius, "l");
                //height of prim from bottom
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(pointsinfo[Defination.SecBotLB].X - extrainfo["sec_Left_Bot_L"] + 1000 - info[Defination.Prim_Radius] - 2500, pointsinfo[Defination.SecBotLT].Y - 1000 + (radius * 1.5) - 100, 0), "CL.EL.(+)" + info[Defination.Prim_ht].ToString());

            }

            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
            {
                double radius = 300;
                info[Defination.Prim_Radius] = radius;
                info[Defination.Prim_ht] = radius;

                CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Prim_block_U", pointsinfo[Defination.SecBotLT].X - extrainfo["sec_Left_Bot_L"] + 1000, pointsinfo[Defination.SecBotLT].Y - 1000 + radius, radius);
                //height of prim from bottom
                // CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] - 1500, centerY - 100, 0), "CL.EL.(+)" + info[Defination.Prim_ht].ToString());

                //string clampname = ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName;
                //DrawLeader(AcadBlockTableRecord, AcadTransaction, AcadDatabase, clampname, new Point3d(pointsinfo[Defination.SecBotLT].X - extrainfo["sec_Left_Bot_L"] + 1000, pointsinfo[Defination.SecBotLT].Y - 1000 + radius + 1.4 * radius, 0), new Point3d(pointsinfo[Defination.SecBotLT].X - extrainfo["sec_Left_Bot_L"] + 1000 - 4 * radius, pointsinfo[Defination.SecBotLT].Y - 1000 + radius + 1.4 * radius, 0));
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(pointsinfo[Defination.SecBotLT].X - extrainfo["sec_Left_Bot_L"] + 1000 - info[Defination.Prim_Radius] - 2500, pointsinfo[Defination.SecBotLT].Y - 1000 + radius - 100, 0), "CL.EL.(+)" + prim_height);
                LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(pointsinfo[Defination.SecBotLT].X - extrainfo["sec_Left_Bot_L"] + 1000 - info[Defination.Prim_Radius], pointsinfo[Defination.SecBotLT].Y - 1000 + radius, 0), new Point3d(pointsinfo[Defination.SecBotLT].X - extrainfo["sec_Left_Bot_L"] + 1000 - info[Defination.Prim_Radius] - 3000, pointsinfo[Defination.SecBotLT].Y - 1000 + radius, 0), MyCol.LightBlue);
                string clampname = ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName;
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(pointsinfo[Defination.SecBotLT].X - extrainfo["sec_Left_Bot_L"] + 1000 - info[Defination.Prim_Radius] - 2500, pointsinfo[Defination.SecBotLT].Y - 1000 + radius + 300, 0), clampname);

            }

            //hori dimesion of left side down prim and sec
            l_dist_frm_centre = 0;
            r_dist_frm_centre = 0;
            //dimensioning
            try
            {
                var strpt = secsupp_des.ElementAt(1).StPt;
                var endpt = secsupp_des.ElementAt(1).EndPt;
                var midpt = primsup_des.ElementAt(1).Centroid;
                Point3d projectedpt = FindPerpendicularFoot(midpt, strpt, endpt);

                l_dist_frm_centre = Math.Round(GetDist(new Point3d(strpt), projectedpt));
                r_dist_frm_centre = Math.Round(GetDist(new Point3d(endpt), projectedpt));

                l_dist_frm_centre = Math.Min(l_dist_frm_centre, r_dist_frm_centre);
                r_dist_frm_centre = Math.Max(l_dist_frm_centre, r_dist_frm_centre);
            }
            catch (Exception)
            {

            }

            //dimensioning
            CreateDimension(new Point3d(pointsinfo[Defination.SecBotLT].X - extrainfo["sec_Left_Bot_L"], pointsinfo[Defination.SecBotLT].Y - 1000, 0), new Point3d(pointsinfo[Defination.SecBotLB].X - extrainfo["sec_Left_Bot_L"] + 1000, pointsinfo[Defination.SecBotLT].Y - 1000 + (info[Defination.Prim_Radius] * 1.5) + info[Defination.Prim_Radius], 0), l_dist_frm_centre.ToString(), 10);
            CreateDimension(new Point3d(pointsinfo[Defination.SecBotLT].X, pointsinfo[Defination.SecBotLT].Y - 1000, 0), new Point3d(pointsinfo[Defination.SecBotLB].X - extrainfo["sec_Left_Bot_L"] + 1000, pointsinfo[Defination.SecBotLT].Y - 1000 + (info[Defination.Prim_Radius] * 1.5) + info[Defination.Prim_Radius], 0), r_dist_frm_centre.ToString(), 10);


            //right side upper secondary support
            height = 1000;
            length = 4000;
            extrainfo["sec_Right_Upper_Bot_L"] = length;
            extrainfo["sec_Right_Upper_Bot_B"] = height;


            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(pointsinfo[Defination.SecBotRB].X, pointsinfo[Defination.SecBotRB].Y + 4000 + extrainfo["sec_Right_Upper_Bot_B"], 0), new Point3d(pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Upper_Bot_L"], pointsinfo[Defination.SecBotRB].Y + 4000 + extrainfo["sec_Right_Upper_Bot_B"], 0), new Point3d(pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Upper_Bot_L"], pointsinfo[Defination.SecBotRB].Y + 4000, 0), new Point3d(pointsinfo[Defination.SecBotRB].X, pointsinfo[Defination.SecBotRB].Y + 4000, 0), SecThick.HBoth, "R");

            //right side verti dimen sec sup
            try
            {
                TotalHt = Math.Round(MillimetersToMeters(secsupp_des.ElementAt(2).Boundingboxmax.Z), 2).ToString();
            }
            catch (Exception)
            {
            }

            TOSLOC(AcadBlockTableRecord, AcadTransaction, AcadDatabase, pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Upper_Bot_L"], pointsinfo[Defination.SecBotRB].Y + 4000 + extrainfo["sec_Right_Upper_Bot_B"], TotalHt);

            //right side upper primary support

            //verti dimen prim ht
            try
            {
                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
                {
                    //height of prim from bottom

                    prim_height = Math.Round((MillimetersToMeters(primsup_des.ElementAt(2).PrimaryZhtNB)), 2).ToString();
                }

                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
                {
                    prim_height = Math.Round((MillimetersToMeters(primsup_des.ElementAt(2).Centroid.Z)), 2).ToString();
                }
            }
            catch (Exception)
            {

            }

            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
            {
                double radius = 600;

                FixCreatePrimarySupportwithvertex(AcadBlockTableRecord, AcadTransaction, AcadDatabase, pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Upper_Bot_L"] - 1500, pointsinfo[Defination.SecBotRB].Y + 4000 + extrainfo["sec_Right_Upper_Bot_B"] + (radius * 1.5), radius);
                //height of prim from bottom
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Upper_Bot_L"] - 1500 + 1000, pointsinfo[Defination.SecBotRB].Y + 4000 + extrainfo["sec_Right_Upper_Bot_B"] + (radius * 1.5) - 100, 0), "CL.EL.(+)" + info[Defination.Prim_ht].ToString());

            }

            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
            {
                double radius = 400;
                info[Defination.Prim_Radius] = radius;
                info[Defination.Prim_ht] = radius;

                CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Prim_block_U", pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Upper_Bot_L"] - 1500, pointsinfo[Defination.SecBotRB].Y + 4000 + extrainfo["sec_Right_Upper_Bot_B"] + radius, radius);
                //height of prim from bottom
                // CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] - 1500, centerY - 100, 0), "CL.EL.(+)" + info[Defination.Prim_ht].ToString());

                // string clampname = ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName;
                // DrawLeader(AcadBlockTableRecord, AcadTransaction, AcadDatabase, clampname, new Point3d(pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Upper_Bot_L"] - 1500, pointsinfo[Defination.SecBotRB].Y + 3500 + extrainfo["sec_Right_Upper_Bot_B"] + radius, 0), new Point3d(pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Upper_Bot_L"] - 1500 - 4 * radius, pointsinfo[Defination.SecBotRB].Y + 3500 + extrainfo["sec_Right_Upper_Bot_B"] + radius + 1.4 * radius, 0));

                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Upper_Bot_L"] - 1500 + info[Defination.Prim_Radius] + 2500, pointsinfo[Defination.SecBotRB].Y + 4000 + extrainfo["sec_Right_Upper_Bot_B"] + radius - 100, 0), "CL.EL.(+)" + prim_height);
                LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Upper_Bot_L"] - 1500 + info[Defination.Prim_Radius], pointsinfo[Defination.SecBotRB].Y + 4000 + extrainfo["sec_Right_Upper_Bot_B"] + radius, 0), new Point3d(pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Upper_Bot_L"] - 1500 + info[Defination.Prim_Radius] + 3000, pointsinfo[Defination.SecBotRB].Y + 4000 + extrainfo["sec_Right_Upper_Bot_B"] + radius, 0), MyCol.LightBlue);
                string clampname = ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName;
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Upper_Bot_L"] - 1500 + info[Defination.Prim_Radius] + 2500, pointsinfo[Defination.SecBotRB].Y + 4000 + extrainfo["sec_Right_Upper_Bot_B"] + radius + 300, 0), clampname);

            }

            //right side upper primary hori dimen of prim and sec
            l_dist_frm_centre = 0;
            r_dist_frm_centre = 0;
            //dimensioning
            try
            {
                var strpt = secsupp_des.ElementAt(2).StPt;
                var endpt = secsupp_des.ElementAt(2).EndPt;
                var midpt = primsup_des.ElementAt(2).Centroid;
                Point3d projectedpt = FindPerpendicularFoot(midpt, strpt, endpt);

                var dist1 = Math.Round(GetDist(new Point3d(strpt), projectedpt));
                var dist2 = Math.Round(GetDist(new Point3d(endpt), projectedpt));

                l_dist_frm_centre = Math.Max(dist1, dist2);
                r_dist_frm_centre = Math.Min(dist1, dist2);
            }
            catch (Exception)
            {

            }

            //dimensioning
            CreateDimension(new Point3d(pointsinfo[Defination.SecBotRB].X, pointsinfo[Defination.SecBotRB].Y + 4000 + extrainfo["sec_Right_Upper_Bot_B"], 0), new Point3d(pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Upper_Bot_L"] - 1500, pointsinfo[Defination.SecBotRB].Y + 4000 + extrainfo["sec_Right_Upper_Bot_B"] + (info[Defination.Prim_Radius] * 1.5) + info[Defination.Prim_Radius], 0), l_dist_frm_centre.ToString(), 200);
            CreateDimension(new Point3d(pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Upper_Bot_L"], pointsinfo[Defination.SecBotRB].Y + 4000 + extrainfo["sec_Right_Upper_Bot_B"], 0), new Point3d(pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Upper_Bot_L"] - 1500, pointsinfo[Defination.SecBotRB].Y + 4000 + extrainfo["sec_Right_Upper_Bot_B"] + (info[Defination.Prim_Radius] * 1.5) + info[Defination.Prim_Radius], 0), r_dist_frm_centre.ToString(), 200);



            //right side down secondary support
            height = 1000;
            length = 4000;
            extrainfo["sec_Right_Lower_Bot_L"] = length;
            extrainfo["sec_Right_Lower_Bot_B"] = height;


            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(pointsinfo[Defination.SecBotRB].X, pointsinfo[Defination.SecBotRB].Y + 500 + extrainfo["sec_Right_Lower_Bot_B"], 0), new Point3d(pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Lower_Bot_L"], pointsinfo[Defination.SecBotRB].Y + 500 + extrainfo["sec_Right_Lower_Bot_B"], 0), new Point3d(pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Lower_Bot_L"], pointsinfo[Defination.SecBotRB].Y + 500, 0), new Point3d(pointsinfo[Defination.SecBotRB].X, pointsinfo[Defination.SecBotRB].Y + 500, 0), SecThick.HBoth, "R");

            try
            {
                TotalHt = Math.Round(MillimetersToMeters(secsupp_des.ElementAt(3).Boundingboxmax.Z), 2).ToString();
            }
            catch (Exception)
            {
            }
            TOSLOC(AcadBlockTableRecord, AcadTransaction, AcadDatabase, pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Lower_Bot_L"], pointsinfo[Defination.SecBotRB].Y + 500 + extrainfo["sec_Right_Lower_Bot_B"], TotalHt);


            //right side down primary support

            //verti dime prim ht
            try
            {
                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
                {
                    //height of prim from bottom

                    prim_height = Math.Round((MillimetersToMeters(primsup_des.ElementAt(3).PrimaryZhtNB)), 2).ToString();
                }

                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
                {
                    prim_height = Math.Round((MillimetersToMeters(primsup_des.ElementAt(3).Centroid.Z)), 2).ToString();
                }
            }
            catch (Exception)
            {

            }
            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
            {
                double radius = 600;

                FixCreatePrimarySupportwithvertex(AcadBlockTableRecord, AcadTransaction, AcadDatabase, pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Lower_Bot_L"] - 1500, pointsinfo[Defination.SecBotRB].Y + 500 + extrainfo["sec_Right_Lower_Bot_B"] + (radius * 1.5), radius);
                //height of prim from bottom
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Lower_Bot_L"] - 1500 + 1000, pointsinfo[Defination.SecBotRB].Y + 500 + extrainfo["sec_Right_Lower_Bot_B"] + (radius * 1.5) - 100, 0), "CL.EL.(+)" + info[Defination.Prim_ht].ToString());

            }

            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
            {
                double radius = 400;
                info[Defination.Prim_Radius] = radius;
                info[Defination.Prim_ht] = radius;

                CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Prim_block_U", pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Lower_Bot_L"] - 1500, pointsinfo[Defination.SecBotRB].Y + 500 + extrainfo["sec_Right_Lower_Bot_B"] + radius, radius);
                //height of prim from bottom
                // CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] - 1500, centerY - 100, 0), "CL.EL.(+)" + info[Defination.Prim_ht].ToString());

                //string clampname = ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName;
                // DrawLeader(AcadBlockTableRecord, AcadTransaction, AcadDatabase, clampname, new Point3d(pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Lower_Bot_L"] - 1500, pointsinfo[Defination.SecBotRB].Y + 500 + extrainfo["sec_Right_Lower_Bot_B"] + radius, 0), new Point3d(pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Lower_Bot_L"] - 1500 - 4 * radius, pointsinfo[Defination.SecBotRB].Y + 500 + extrainfo["sec_Right_Lower_Bot_B"] + radius + 1.4 * radius, 0));
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Lower_Bot_L"] - 1500 + info[Defination.Prim_Radius] + 2500, pointsinfo[Defination.SecBotRB].Y + 500 + extrainfo["sec_Right_Lower_Bot_B"] + radius - 100, 0), "CL.EL.(+)" + prim_height);
                LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Lower_Bot_L"] - 1500 + info[Defination.Prim_Radius], pointsinfo[Defination.SecBotRB].Y + 500 + extrainfo["sec_Right_Lower_Bot_B"] + radius, 0), new Point3d(pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Lower_Bot_L"] - 1500 + info[Defination.Prim_Radius] + 3000, pointsinfo[Defination.SecBotRB].Y + 500 + extrainfo["sec_Right_Lower_Bot_B"] + radius, 0), MyCol.LightBlue);
                string clampname = ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName;
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Lower_Bot_L"] - 1500 + info[Defination.Prim_Radius] + 2500, pointsinfo[Defination.SecBotRB].Y + 500 + extrainfo["sec_Right_Lower_Bot_B"] + radius + 300, 0), clampname);
            }

            //hori dimen right side below prim and sec
            l_dist_frm_centre = 0;
            r_dist_frm_centre = 0;
            //dimensioning
            try
            {
                var strpt = secsupp_des.ElementAt(3).StPt;
                var endpt = secsupp_des.ElementAt(3).EndPt;
                var midpt = primsup_des.ElementAt(3).Centroid;
                Point3d projectedpt = FindPerpendicularFoot(midpt, strpt, endpt);

                var dist1 = Math.Round(GetDist(new Point3d(strpt), projectedpt));
                var dist2 = Math.Round(GetDist(new Point3d(endpt), projectedpt));

                l_dist_frm_centre = Math.Max(dist1, dist2);
                r_dist_frm_centre = Math.Min(dist1, dist2);
            }
            catch (Exception)
            {

            }

            //dimensioning
            CreateDimension(new Point3d(pointsinfo[Defination.SecBotRB].X, pointsinfo[Defination.SecBotRB].Y + 500 + extrainfo["sec_Right_Lower_Bot_B"], 0), new Point3d(pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Lower_Bot_L"] - 1500, pointsinfo[Defination.SecBotRB].Y + 500 + extrainfo["sec_Right_Lower_Bot_B"] + (info[Defination.Prim_Radius] * 1.5) + info[Defination.Prim_Radius], 0), l_dist_frm_centre.ToString(), 200);
            CreateDimension(new Point3d(pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Lower_Bot_L"], pointsinfo[Defination.SecBotRB].Y + 500 + extrainfo["sec_Right_Lower_Bot_B"], 0), new Point3d(pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Lower_Bot_L"] - 1500, pointsinfo[Defination.SecBotRB].Y + 500 + extrainfo["sec_Right_Lower_Bot_B"] + (info[Defination.Prim_Radius] * 1.5) + info[Defination.Prim_Radius], 0), r_dist_frm_centre.ToString(), 200);

            //draw plate/concrete support 
            height = 1000;
            length = 3000;
            info[Defination.Plate_l] = length;
            info[Defination.Plate_b] = height;


            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d((pointsinfo[Defination.SecBotRB].X + pointsinfo[Defination.SecBotLB].X) / 2 - info[Defination.Plate_l] / 2, pointsinfo[Defination.SecBotLB].Y, 0), new Point3d((pointsinfo[Defination.SecBotRB].X + pointsinfo[Defination.SecBotLB].X) / 2 + info[Defination.Plate_l] / 2, pointsinfo[Defination.SecBotLB].Y, 0), new Point3d((pointsinfo[Defination.SecBotRB].X + pointsinfo[Defination.SecBotLB].X) / 2 + info[Defination.Plate_l] / 2, pointsinfo[Defination.SecBotLB].Y - info[Defination.Plate_b], 0), new Point3d((pointsinfo[Defination.SecBotRB].X + pointsinfo[Defination.SecBotLB].X) / 2 - info[Defination.Plate_l] / 2, pointsinfo[Defination.SecBotLB].Y - info[Defination.Plate_b], 0), SecThick.Nothing);


            height = 1000;
            length = 3000;
            info[Defination.Concrete_l] = length;
            info[Defination.Concrete_b] = height;

            if (ListCentalSuppoData[i].ListConcreteData.Count > 0)
            {
                FixCreateBottomSupportTopType2(Document2D, AcadBlockTableRecord, AcadTransaction, AcadDatabase, (pointsinfo[Defination.SecBotRB].X + pointsinfo[Defination.SecBotLB].X) / 2, centerY, height, length, i);
            }

            //support name and quantity
            CreateSupportName(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, i);

            tracex += boxlen;
        }

        [Obsolete]
        public void DRS20(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, ref double tracex, double boxlen, double boxht, ref double spaceY, Document Document2D, string SupType, [Optional] int i)
        {
            double upperYgap = 3500;

            double centerX = tracex + boxlen / 2;  // 9869.9480;
            double centerY = spaceY - upperYgap;

            //for support18 only
            centerY += 1500;
            centerX -= 1500;

            //box boundaries
            //vertical line
            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex + boxlen, spaceY + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);

            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex, spaceY - boxht + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);


            //CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Prim_block_U", centerX, centerY, radius);
            var primsup_des = ListCentalSuppoData[i].ListPrimarySuppo.OrderByDescending(e => e.Centroid.Z);
            var secsupp_des = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection.ToLower() == "hor").OrderByDescending(e => e.Boundingboxmax.Z);

            string prim_height = "";
            try
            {
                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
                {
                    //height of prim from bottom

                    prim_height = Math.Round((MillimetersToMeters(ListCentalSuppoData[i].ListPrimarySuppo[0].PrimaryZhtNB)), 2).ToString();
                }

                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
                {
                    prim_height = Math.Round((MillimetersToMeters(ListCentalSuppoData[i].ListPrimarySuppo[0].Centroid.Z)), 2).ToString();
                }
            }
            catch (Exception)
            {

            }
            double radius = 801.5625;
            info[Defination.Prim_Radius] = radius;
            info[Defination.Prim_ht] = radius * 1.5;

            double height = 1000;
            double length = 4000;
            info[Defination.Sec_top_l] = length;
            info[Defination.Sec_top_b] = height;
            //double ht_frm_cen = 1220.7383;0.33,0.66var sectionsize = 0.0;
            var sectionsize = 0.0;
            try
            {
                var section = secsupp_des.ElementAt(0).Size;

                sectionsize = Convert.ToDouble(CSECTIONSIZE(section));
            }
            catch (Exception)
            {

            }

            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length * 0.34, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length * 0.66, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length * 0.66, centerY - info[Defination.Prim_ht] - height, 0), new Point3d(centerX - length * 0.34, centerY - info[Defination.Prim_ht] - height, 0), SecThick.HHidBoth, "l", 0, sectionsize);


            //info for future
            pointsinfo[Defination.SecTopLT] = new Point3d(centerX - length * 0.34, centerY - info[Defination.Prim_ht], 0);
            pointsinfo[Defination.SecTopRT] = new Point3d(centerX + length * 0.66, centerY - info[Defination.Prim_ht], 0);
            pointsinfo[Defination.SecTopRB] = new Point3d(centerX + length * 0.66, centerY - info[Defination.Prim_ht] - height, 0);
            pointsinfo[Defination.SecTopLB] = new Point3d(centerX - length * 0.34, centerY - info[Defination.Prim_ht] - height, 0);


            //dimensioning
            // CreateDimension(new Point3d(centerX - length * 0.34, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX, centerY + info[Defination.Prim_Radius], 0));
            // CreateDimension(new Point3d(centerX + length * 0.66, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX, centerY + info[Defination.Prim_Radius], 0));


            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + length * 0.66 + 1000, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length * 0.66 + 5000, centerY - info[Defination.Prim_ht], 0), MyCol.LightBlue);


            string TotalHt = "";

            //mtext
            try
            {

                if (ListCentalSuppoData[i].ListConcreteData.Count > 1)
                {
                    TotalHt = ListCentalSuppoData[i].ListConcreteData[0].BoxData.Z > ListCentalSuppoData[i].ListConcreteData[1].BoxData.Z
                    ? Math.Round((ListCentalSuppoData[i].ListConcreteData[0].BoxData.Z + ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection == "Hor").First().BoxData.Z + ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.BoxData.Z == ListCentalSuppoData[i].ListSecondrySuppo.Max(s => s.BoxData.Z)).First().BoxData.Z), 2).ToString()
                    : Math.Round((ListCentalSuppoData[i].ListConcreteData[1].BoxData.Z + ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection == "Hor").First().BoxData.Z + ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.BoxData.Z == ListCentalSuppoData[i].ListSecondrySuppo.Max(s => s.BoxData.Z)).First().BoxData.Z), 2).ToString();
                }
                else
                {
                    TotalHt = Math.Round((ListCentalSuppoData[i].ListConcreteData[0].BoxData.Z + ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection.ToLower() == "hor").First().BoxData.Z + ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection.ToLower() == "ver").First().BoxData.Z), 2).ToString();
                }


            }
            catch (Exception)
            {

            }


            try
            {
                //TotalHt = MillimetersToMeters(Convert.ToDouble(TotalHt)).ToString();
            }
            catch (Exception)
            {

            }

            CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + 4500, centerY - info[Defination.Prim_ht] + 300, 0), "TOS EL.(+)" + TotalHt);

            info[Defination.Sec_ht_top] = info[Defination.Prim_ht] + height;
            //bot sec
            height = 5000;
            length = 1000;
            info[Defination.Sec_bot_l] = length;
            info[Defination.Sec_bot_b] = height;

            info[Defination.Sec_ht_bot] = height;

            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(pointsinfo[Defination.SecTopRT].X, pointsinfo[Defination.SecTopRT].Y, 0), new Point3d(pointsinfo[Defination.SecTopRT].X + info[Defination.Sec_bot_l], pointsinfo[Defination.SecTopRT].Y, 0), new Point3d(pointsinfo[Defination.SecTopRT].X + info[Defination.Sec_bot_l], pointsinfo[Defination.SecTopRT].Y - info[Defination.Sec_bot_b], 0), new Point3d(pointsinfo[Defination.SecTopRT].X, pointsinfo[Defination.SecTopRT].Y - info[Defination.Sec_bot_b], 0), SecThick.VHidBoth, "l");

            //info for future
            pointsinfo[Defination.SecBotLT] = new Point3d(pointsinfo[Defination.SecTopRT].X, pointsinfo[Defination.SecTopRT].Y, 0);
            pointsinfo[Defination.SecBotRT] = new Point3d(pointsinfo[Defination.SecTopRT].X + info[Defination.Sec_bot_l], pointsinfo[Defination.SecTopRT].Y, 0);
            pointsinfo[Defination.SecBotRB] = new Point3d(pointsinfo[Defination.SecTopRT].X + info[Defination.Sec_bot_l], pointsinfo[Defination.SecTopRT].Y - info[Defination.Sec_bot_b], 0);
            pointsinfo[Defination.SecBotLB] = new Point3d(pointsinfo[Defination.SecTopRT].X, pointsinfo[Defination.SecTopRT].Y - info[Defination.Sec_bot_b], 0);
            //dimensioning
            var botsec = "";
            try
            {
                botsec = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.BoxData.Z == ListCentalSuppoData[i].ListSecondrySuppo.Max(s => s.BoxData.Z)).First().BoxData.Z.ToString();
            }
            catch (Exception)
            {

            }


            CreateAlighDimen(AcadBlockTableRecord, AcadTransaction, pointsinfo[Defination.SecBotRT], pointsinfo[Defination.SecBotRB], botsec, -3500);



            //left side down secondary support
            height = 1000;
            length = 4500;
            extrainfo["sec_Left_Bot_L"] = length;
            extrainfo["sec_Left_Bot_B"] = height;

            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(pointsinfo[Defination.SecBotLB].X - extrainfo["sec_Left_Bot_L"], pointsinfo[Defination.SecBotLB].Y + 500 + extrainfo["sec_Left_Bot_B"], 0), new Point3d(pointsinfo[Defination.SecBotLB].X, pointsinfo[Defination.SecBotLB].Y + 500 + extrainfo["sec_Left_Bot_B"], 0), new Point3d(pointsinfo[Defination.SecBotLB].X, pointsinfo[Defination.SecBotLB].Y + 500, 0), new Point3d(pointsinfo[Defination.SecBotLB].X - extrainfo["sec_Left_Bot_L"], pointsinfo[Defination.SecBotLB].Y + 500, 0), SecThick.HidTop, "l");


            //left side down primary support
            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
            {
                radius = 801.5625;

                FixCreatePrimarySupportwithvertex(AcadBlockTableRecord, AcadTransaction, AcadDatabase, pointsinfo[Defination.SecBotLB].X - extrainfo["sec_Left_Bot_L"] + 1000, pointsinfo[Defination.SecBotLB].Y + 500 + extrainfo["sec_Left_Bot_B"] + (radius * 1.5), radius, "l");
                //height of prim from bottom
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(pointsinfo[Defination.SecBotLB].X - extrainfo["sec_Left_Bot_L"] + 1000 - info[Defination.Prim_Radius] - 1500, pointsinfo[Defination.SecBotLB].Y + 500 + extrainfo["sec_Left_Bot_B"] + (radius * 1.5) - 100, 0), "CL.EL.(+)" + prim_height);

            }

            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
            {
                radius = 200;
                info[Defination.Prim_Radius] = radius;
                info[Defination.Prim_ht] = radius;

                CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Prim_block_U", pointsinfo[Defination.SecBotLB].X - extrainfo["sec_Left_Bot_L"] + 1000, pointsinfo[Defination.SecBotLB].Y + 500 + extrainfo["sec_Left_Bot_B"] + radius, radius);
                //height of prim from bottom
                // CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] - 1500, centerY - 100, 0), "CL.EL.(+)" + info[Defination.Prim_ht].ToString());


                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(pointsinfo[Defination.SecBotLB].X - extrainfo["sec_Left_Bot_L"] + 1000 - info[Defination.Prim_Radius] - 2500, pointsinfo[Defination.SecBotLB].Y + 500 + extrainfo["sec_Left_Bot_B"] + radius - 100, 0), "CL.EL.(+)" + prim_height);
                LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(pointsinfo[Defination.SecBotLB].X - extrainfo["sec_Left_Bot_L"] + 1000 - info[Defination.Prim_Radius], pointsinfo[Defination.SecBotLB].Y + 500 + extrainfo["sec_Left_Bot_B"] + radius, 0), new Point3d(pointsinfo[Defination.SecBotLB].X - extrainfo["sec_Left_Bot_L"] + 1000 - info[Defination.Prim_Radius] - 3000, pointsinfo[Defination.SecBotLB].Y + 500 + extrainfo["sec_Left_Bot_B"] + radius, 0), MyCol.LightBlue);
                string clampname = ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName;
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(pointsinfo[Defination.SecBotLB].X - extrainfo["sec_Left_Bot_L"] + 1000 - info[Defination.Prim_Radius] - 2500, pointsinfo[Defination.SecBotLB].Y + 500 + extrainfo["sec_Left_Bot_B"] + radius + 300, 0), clampname);

            }

            //dimen hori left side down prim n sec
            double l_dist_frm_centre = 0;
            double r_dist_frm_centre = 0;
            //dimensioning
            try
            {
                var strpt = secsupp_des.ElementAt(1).StPt;
                var endpt = secsupp_des.ElementAt(1).EndPt;
                var midpt = primsup_des.ElementAt(1).Centroid;

                Point3d projectedpt = FindPerpendicularFoot(midpt, strpt, endpt);

                var dist1 = Math.Round(GetDist(new Point3d(strpt), projectedpt));
                var dist2 = Math.Round(GetDist(new Point3d(endpt), projectedpt));

                l_dist_frm_centre = Math.Min(dist1, dist2);
                r_dist_frm_centre = Math.Max(dist1, dist2);
            }
            catch (Exception)
            {

            }

            //dimensioning
            CreateDimension(new Point3d(pointsinfo[Defination.SecBotLB].X - extrainfo["sec_Left_Bot_L"], pointsinfo[Defination.SecBotLB].Y + 500 + extrainfo["sec_Left_Bot_B"], 0), new Point3d(pointsinfo[Defination.SecBotLB].X - extrainfo["sec_Left_Bot_L"] + 1000, pointsinfo[Defination.SecBotLB].Y + 500 + extrainfo["sec_Left_Bot_B"] + (radius * 1.5) + info[Defination.Prim_Radius], 0), l_dist_frm_centre.ToString(), 200);
            CreateDimension(new Point3d(pointsinfo[Defination.SecBotLB].X - extrainfo["sec_Left_Bot_L"] + 1000, pointsinfo[Defination.SecBotLB].Y + 500 + extrainfo["sec_Left_Bot_B"] + (radius * 1.5) + info[Defination.Prim_Radius], 0), new Point3d(pointsinfo[Defination.SecBotLB].X, pointsinfo[Defination.SecBotLB].Y + 500 + extrainfo["sec_Left_Bot_B"], 0), r_dist_frm_centre.ToString(), 200);


            //right side down secondary support
            height = 1000;
            length = 3000;
            extrainfo["sec_Right_Bot_L"] = length;
            extrainfo["sec_Right_Bot_B"] = height;


            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(pointsinfo[Defination.SecBotRB].X, pointsinfo[Defination.SecBotRB].Y + 1500 + extrainfo["sec_Right_Bot_B"], 0), new Point3d(pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Bot_L"], pointsinfo[Defination.SecBotRB].Y + 1500 + extrainfo["sec_Right_Bot_B"], 0), new Point3d(pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Bot_L"], pointsinfo[Defination.SecBotRB].Y + 1500, 0), new Point3d(pointsinfo[Defination.SecBotRB].X, pointsinfo[Defination.SecBotRB].Y + 1500, 0), SecThick.HBoth, "R");


            //right side down primary support
            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
            {
                radius = 801.5625;

                FixCreatePrimarySupportwithvertex(AcadBlockTableRecord, AcadTransaction, AcadDatabase, pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Bot_L"] - 1500, pointsinfo[Defination.SecBotRB].Y + 1500 + extrainfo["sec_Right_Bot_B"] + (radius * 1.5), radius);
                //height of prim from bottom
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Bot_L"] + 1500, pointsinfo[Defination.SecBotRB].Y + 1500 + extrainfo["sec_Right_Bot_B"] + (radius * 1.5) - 100, 0), "CL.EL.(+)" + info[Defination.Prim_ht].ToString());

            }

            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
            {
                radius = 500;
                info[Defination.Prim_Radius] = radius;
                info[Defination.Prim_ht] = radius;

                CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Prim_block_U", pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Bot_L"] - 1500, pointsinfo[Defination.SecBotRB].Y + 1500 + extrainfo["sec_Right_Bot_B"] + radius, radius);
                //height of prim from bottom
                // CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] - 1500, centerY - 100, 0), "CL.EL.(+)" + info[Defination.Prim_ht].ToString());

                // string clampname = ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName;
                // DrawLeader(AcadBlockTableRecord, AcadTransaction, AcadDatabase, clampname, new Point3d(pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Bot_L"] - 1500, pointsinfo[Defination.SecBotRB].Y + 1500 + extrainfo["sec_Right_Bot_B"] + radius, 0), new Point3d(pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Bot_L"] - 1500 - 4 * radius, pointsinfo[Defination.SecBotRB].Y + 1500 + extrainfo["sec_Right_Bot_B"] + radius + 1.4 * radius, 0));

                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Bot_L"] - 1500 - info[Defination.Prim_Radius] - 2500, pointsinfo[Defination.SecBotRB].Y + 1500 + extrainfo["sec_Right_Bot_B"] + radius - 100, 0), "CL.EL.(+)" + prim_height);
                LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Bot_L"] - 1500 - info[Defination.Prim_Radius], pointsinfo[Defination.SecBotRB].Y + 1500 + extrainfo["sec_Right_Bot_B"] + radius, 0), new Point3d(pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Bot_L"] - 1500 - info[Defination.Prim_Radius] - 3000, pointsinfo[Defination.SecBotRB].Y + 1500 + extrainfo["sec_Right_Bot_B"] + radius, 0), MyCol.LightBlue);
                string clampname = ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName;
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Bot_L"] - 1500 - info[Defination.Prim_Radius] - 2500, pointsinfo[Defination.SecBotRB].Y + 1500 + extrainfo["sec_Right_Bot_B"] + radius + 300, 0), clampname);

            }


            //dimens  hori right side prim n sec
            l_dist_frm_centre = 0;
            r_dist_frm_centre = 0;
            //dimensioning
            try
            {
                var strpt = secsupp_des.ElementAt(0).StPt;
                var endpt = secsupp_des.ElementAt(0).EndPt;
                var midpt = primsup_des.ElementAt(0).Centroid;

                Point3d projectedpt = FindPerpendicularFoot(midpt, strpt, endpt);

                var dist1 = Math.Round(GetDist(new Point3d(strpt), projectedpt));
                var dist2 = Math.Round(GetDist(new Point3d(endpt), projectedpt));

                l_dist_frm_centre = Math.Max(dist1, dist2);
                r_dist_frm_centre = Math.Min(dist1, dist2);
            }
            catch (Exception)
            {

            }

            //dimensioning
            CreateDimension(new Point3d(pointsinfo[Defination.SecBotRB].X, pointsinfo[Defination.SecBotRB].Y + 1500 + extrainfo["sec_Right_Bot_B"], 0), new Point3d(pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Bot_L"] - 1500, pointsinfo[Defination.SecBotRB].Y + 1500 + extrainfo["sec_Right_Bot_B"] + (radius * 1.5) + info[Defination.Prim_Radius], 0), l_dist_frm_centre.ToString(), 200);
            CreateDimension(new Point3d(pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Bot_L"], pointsinfo[Defination.SecBotRB].Y + 1500 + extrainfo["sec_Right_Bot_B"], 0), new Point3d(pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Bot_L"] - 1500, pointsinfo[Defination.SecBotRB].Y + 1500 + extrainfo["sec_Right_Bot_B"] + (radius * 1.5) + info[Defination.Prim_Radius], 0), r_dist_frm_centre.ToString(), 200);


            ////draw plate/concrete support 
            //height = 1000;
            //length = 3000;
            //info[Defination.Plate_l] = length;
            //info[Defination.Plate_b] = height;


            //BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d((pointsinfo[Defination.SecBotRB].X + pointsinfo[Defination.SecBotLB].X) / 2 - info[Defination.Plate_l] / 2, pointsinfo[Defination.SecBotLB].Y, 0), new Point3d((pointsinfo[Defination.SecBotRB].X + pointsinfo[Defination.SecBotLB].X) / 2 + info[Defination.Plate_l] / 2, pointsinfo[Defination.SecBotLB].Y, 0), new Point3d((pointsinfo[Defination.SecBotRB].X + pointsinfo[Defination.SecBotLB].X) / 2 + info[Defination.Plate_l] / 2, pointsinfo[Defination.SecBotLB].Y - info[Defination.Plate_b], 0), new Point3d((pointsinfo[Defination.SecBotRB].X + pointsinfo[Defination.SecBotLB].X) / 2 - info[Defination.Plate_l] / 2, pointsinfo[Defination.SecBotLB].Y - info[Defination.Plate_b], 0), SecThick.Nothing);


            height = 1000;
            length = 3000;
            info[Defination.Concrete_l] = length;
            info[Defination.Concrete_b] = height;

            if (ListCentalSuppoData[i].ListConcreteData.Count > 0)
            {
                FixCreateBottomSupportTopType2(Document2D, AcadBlockTableRecord, AcadTransaction, AcadDatabase, (pointsinfo[Defination.SecBotRB].X + pointsinfo[Defination.SecBotLB].X) / 2, centerY, height, length, i);
            }

            //support name and quantity
            CreateSupportName(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, i);

            tracex += boxlen;
        }

        [Obsolete]
        public void DRS21(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, ref double tracex, double boxlen, double boxht, ref double spaceY, Document Document2D, string SupType, [Optional] int i)
        {

            double upperYgap = 3500;
            double centerX = tracex + boxlen / 2;  // 9869.9480;
            double centerY = spaceY - upperYgap;
            //box boundaries
            //vertical line
            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex + boxlen, spaceY + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);

            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex, spaceY - boxht + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);

            //does not have primary support

            // FixCreatePrimarySupportwithvertex(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY, 801.5625);
            var secsupp_des = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection.ToLower() == "hor").OrderByDescending(e => e.Boundingboxmax.Z);

            double radius = 801.5625;
            info[Defination.Prim_Radius] = radius;
            info[Defination.Prim_ht] = radius * 1.5;
            //height of prim from bottom

            //CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] + 1500, centerY - 100, 0), "CL.EL.(+)" + info[Defination.Prim_ht].ToString());

            double height = 1000;
            double length = 5000;
            info[Defination.Sec_top_l] = length;
            info[Defination.Sec_top_b] = height;
            var sectionsize = 0.0;
            try
            {
                var section = secsupp_des.ElementAt(0).Size;

                sectionsize = Convert.ToDouble(CSECTIONSIZE(section));
            }
            catch (Exception)
            {

            }
            //upper secondary supp
            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht] - height, 0), new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht] - height, 0), SecThick.Top, "R", 0, sectionsize);

            //info for future
            pointsinfo[Defination.SecTopLT] = new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht], 0);
            pointsinfo[Defination.SecTopRT] = new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht], 0);
            pointsinfo[Defination.SecTopRB] = new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht] - height, 0);
            pointsinfo[Defination.SecTopLB] = new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht] - height, 0);


            double horilength = 0;
            try
            {
                horilength = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection == "Hor").First().BoxData.X;
            }
            catch (Exception)
            {

            }

            //double strtmiddist=GetDist(ldist)

            CreateDimension(pointsinfo[Defination.SecTopLT], pointsinfo[Defination.SecTopRT], horilength.ToString());
            // CreateDimension(new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX, centerY + info[Defination.Prim_Radius], 0), r_dist_frm_centre.ToString());


            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length / 2 + 4000, centerY - info[Defination.Prim_ht], 0), MyCol.LightBlue);


            string TotalHt = "";
            //mtext
            try
            {

                if (ListCentalSuppoData[i].ListConcreteData.Count > 1)
                {
                    TotalHt = ListCentalSuppoData[i].ListConcreteData[0].BoxData.Z > ListCentalSuppoData[i].ListConcreteData[1].BoxData.Z
                    ? Math.Round((ListCentalSuppoData[i].ListConcreteData[0].BoxData.Z + ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection == "Hor").First().BoxData.Z + ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.BoxData.Z == ListCentalSuppoData[i].ListSecondrySuppo.Max(s => s.BoxData.Z)).First().BoxData.Z), 2).ToString()
                    : Math.Round((ListCentalSuppoData[i].ListConcreteData[1].BoxData.Z + ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection == "Hor").First().BoxData.Z + ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.BoxData.Z == ListCentalSuppoData[i].ListSecondrySuppo.Max(s => s.BoxData.Z)).First().BoxData.Z), 2).ToString();
                }
                else
                {
                    TotalHt = Math.Round((ListCentalSuppoData[i].ListConcreteData[0].BoxData.Z + ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection.ToLower() == "hor").First().BoxData.Z + ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection.ToLower() == "ver").First().BoxData.Z), 2).ToString();
                }


            }
            catch (Exception)
            {

            }

            try
            {
                //TotalHt = MillimetersToMeters(Convert.ToDouble(TotalHt)).ToString();
            }
            catch (Exception)
            {

            }

            CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + 2500, centerY - info[Defination.Prim_ht] + 300, 0), "TOS EL.(+)" + TotalHt);

            info[Defination.Sec_ht_top] = info[Defination.Prim_ht] + height;

            height = 4000;
            length = 1000;

            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length / 2, centerY - info[Defination.Sec_ht_top], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top] - height, 0), new Point3d(centerX - length / 2, centerY - info[Defination.Sec_ht_top] - height, 0), SecThick.HidRight, "l");

            //info for future
            pointsinfo[Defination.SecBotLT] = new Point3d(centerX - length / 2, centerY - info[Defination.Sec_ht_top], 0);
            pointsinfo[Defination.SecBotRT] = new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top], 0);
            pointsinfo[Defination.SecBotRB] = new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top] - height, 0);
            pointsinfo[Defination.SecBotLB] = new Point3d(centerX - length / 2, centerY - info[Defination.Sec_ht_top] - height, 0);

            //dimensioning
            var botsec = "";
            try
            {
                botsec = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.BoxData.Z == ListCentalSuppoData[i].ListSecondrySuppo.Max(s => s.BoxData.Z)).First().BoxData.Z.ToString();
            }
            catch (Exception)
            {

            }

            CreateAlighDimen(AcadBlockTableRecord, AcadTransaction, new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top] - height, 0), botsec);

            info[Defination.Sec_ht_bot] = info[Defination.Sec_ht_top] + height;

            height = 300;
            length = 3000;
            info[Defination.Concrete_l] = length;
            info[Defination.Concrete_b] = height;

            //FixCreateBottomSupportTopType2(Document2D, AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY, height, length, i);

            //plate
            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length / 2, centerY - info[Defination.Sec_ht_bot], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_bot], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_bot] - height, 0), new Point3d(centerX - length / 2, centerY - info[Defination.Sec_ht_bot] - height, 0), SecThick.Nothing);

            //support name and quantity
            CreateSupportName(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, i);


            tracex += boxlen;

        }


        [Obsolete]
        public void DRS22(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, ref double tracex, double boxlen, double boxht, ref double spaceY, Document Document2D, string SupType, [Optional] int i)
        {

            double upperYgap = 3500;
            double centerX = tracex + boxlen / 2;  // 9869.9480;
            double centerY = spaceY - upperYgap;
            //box boundaries
            //vertical line
            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex + boxlen, spaceY + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);

            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex, spaceY - boxht + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);

            var primsup_des = ListCentalSuppoData[i].ListPrimarySuppo.OrderByDescending(e => e.Centroid.Z);
            var secsupp_des = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection.ToLower() == "hor").OrderByDescending(e => e.Boundingboxmax.Z);

            string prim_height = "";
            try
            {
                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
                {
                    //height of prim from bottom

                    prim_height = Math.Round((MillimetersToMeters(primsup_des.ElementAt(0).PrimaryZhtNB)), 2).ToString();
                }

                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
                {
                    prim_height = Math.Round((MillimetersToMeters(primsup_des.ElementAt(0).Centroid.Z)), 2).ToString();
                }
            }
            catch (Exception)
            {

            }

            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
            {
                FixCreatePrimarySupportwithvertex(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY, 801.5625);

                try
                {
                    //    prim_height = MillimetersToMeters(Convert.ToDouble(prim_height)).ToString();
                }
                catch (Exception)
                {

                }
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] + 1500, centerY - 100, 0), "CL.EL.(+)" + prim_height);

            }

            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
            {
                double radius = 300;
                info[Defination.Prim_Radius] = radius;
                info[Defination.Prim_ht] = radius;

                CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Prim_block_U", centerX, centerY, radius);
                //height of prim from bottom
                // CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] - 1500, centerY - 100, 0), "CL.EL.(+)" + info[Defination.Prim_ht].ToString());

                //string clampname = ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName;
                // DrawLeader(AcadBlockTableRecord, AcadTransaction, AcadDatabase, clampname, new Point3d(centerX, centerY + 1.4 * radius, 0), new Point3d(centerX - 4 * radius, centerY + 1.4 * radius, 0));
                try
                {
                    //    prim_height = MillimetersToMeters(Convert.ToDouble(prim_height)).ToString();
                }
                catch (Exception)
                {

                }
                //CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] - 1500, centerY - 100, 0), "CL.EL.(+)" + prim_height);

                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - info[Defination.Prim_Radius] - 2500, centerY - 100, 0), "CL.EL.(+)" + prim_height);
                LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - info[Defination.Prim_Radius], centerY, 0), new Point3d(centerX - info[Defination.Prim_Radius] - 3000, centerY, 0), MyCol.LightBlue);
                string clampname = ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName;
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - info[Defination.Prim_Radius] - 2500, centerY + 300, 0), clampname);

            }



            double height = 1000;
            double length = 2000;
            info[Defination.Sec_top_l] = length;
            info[Defination.Sec_top_b] = height;
            var sectionsize = 0.0;
            try
            {
                var section = secsupp_des.ElementAt(0).Size;

                sectionsize = Convert.ToDouble(CSECTIONSIZE(section));
            }
            catch (Exception)
            {

            }
            //upper top secondary supp 
            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht] - height, 0), new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht] - height, 0), SecThick.HBoth, "l", 0, sectionsize);

            //info for future
            pointsinfo[Defination.SecTopLT] = new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht], 0);
            pointsinfo[Defination.SecTopRT] = new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht], 0);
            pointsinfo[Defination.SecTopRB] = new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht] - height, 0);
            pointsinfo[Defination.SecTopLB] = new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht] - height, 0);

            double l_dist_frm_centre = 0;
            double r_dist_frm_centre = 0;
            //dimensioning
            try
            {
                var strpt = secsupp_des.ElementAt(0).StPt;
                var endpt = secsupp_des.ElementAt(0).EndPt;
                var midpt = primsup_des.ElementAt(0).Centroid;

                Point3d projectedpt = FindPerpendicularFoot(midpt, strpt, endpt);

                var dist1 = Math.Round(GetDist(new Point3d(strpt), projectedpt));
                var dist2 = Math.Round(GetDist(new Point3d(endpt), projectedpt));

                l_dist_frm_centre = Math.Max(dist1, dist2);
                r_dist_frm_centre = Math.Min(dist1, dist2);
            }
            catch (Exception)
            {

            }


            //double strtmiddist=GetDist(ldist)

            CreateDimension(new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX, centerY + info[Defination.Prim_Radius], 0), l_dist_frm_centre.ToString());
            CreateDimension(new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX, centerY + info[Defination.Prim_Radius], 0), r_dist_frm_centre.ToString());


            //LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length / 2 + 4000, centerY - info[Defination.Prim_ht], 0), MyCol.LightBlue);

            // vertical dimensioning
            string TotalHt = "";
            //mtext

            try
            {
                TotalHt = Math.Round(MillimetersToMeters(secsupp_des.ElementAt(0).Boundingboxmax.Z), 2).ToString();
            }
            catch (Exception)
            {
            }
            TOSLOC(AcadBlockTableRecord, AcadTransaction, AcadDatabase, pointsinfo[Defination.SecTopRT].X, pointsinfo[Defination.SecTopRT].Y, TotalHt, "l");

            try
            {
                //TotalHt = MillimetersToMeters(Convert.ToDouble(TotalHt)).ToString();
            }
            catch (Exception)
            {

            }

            info[Defination.Sec_ht_top] = info[Defination.Prim_ht] + height;

            try
            {
                sectionsize = Convert.ToDouble(CSECTIONSIZE(ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection.ToLower() == "ver").First().Size));
            }
            catch (Exception)
            {

            }
            height = 3000;
            length = 1000;

            //sec bot
            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length / 2, centerY - info[Defination.Sec_ht_top], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top] - height, 0), new Point3d(centerX - length / 2, centerY - info[Defination.Sec_ht_top] - height, 0), SecThick.VBoth, "l", 1000);

            //info for future
            pointsinfo[Defination.SecBotLT] = new Point3d(centerX - length / 2, centerY - info[Defination.Sec_ht_top], 0);
            pointsinfo[Defination.SecBotRT] = new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top], 0);
            pointsinfo[Defination.SecBotRB] = new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top] - height, 0);
            pointsinfo[Defination.SecBotLB] = new Point3d(centerX - length / 2, centerY - info[Defination.Sec_ht_top] - height, 0);

            //dimensioning
            var botsec = "";
            try
            {
                botsec = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.BoxData.Z == ListCentalSuppoData[i].ListSecondrySuppo.Max(s => s.BoxData.Z)).First().BoxData.Z.ToString();
            }
            catch (Exception)
            {

            }


            CreateAlighDimen(AcadBlockTableRecord, AcadTransaction, pointsinfo[Defination.SecBotLT], pointsinfo[Defination.SecBotLB], botsec, 500);

            info[Defination.Sec_ht_bot] = info[Defination.Sec_ht_top] + height;


            //right secondary support

            //right side down secondary support
            height = 1000;
            length = 4000;
            extrainfo["sec_Right_Bot_L"] = length;
            extrainfo["sec_Right_Bot_B"] = height;


            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(pointsinfo[Defination.SecBotRB].X, pointsinfo[Defination.SecBotRB].Y + 500 + extrainfo["sec_Right_Bot_B"], 0), new Point3d(pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Bot_L"], pointsinfo[Defination.SecBotRB].Y + 500 + extrainfo["sec_Right_Bot_B"], 0), new Point3d(pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Bot_L"], pointsinfo[Defination.SecBotRB].Y + 500, 0), new Point3d(pointsinfo[Defination.SecBotRB].X, pointsinfo[Defination.SecBotRB].Y + 500, 0), SecThick.HBoth, "R");

            try
            {
                TotalHt = Math.Round(MillimetersToMeters(secsupp_des.ElementAt(1).Boundingboxmax.Z), 2).ToString();
            }
            catch (Exception)
            {
            }
            TOSLOC(AcadBlockTableRecord, AcadTransaction, AcadDatabase, pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Bot_L"], pointsinfo[Defination.SecBotRB].Y + 500 + extrainfo["sec_Right_Bot_B"], TotalHt);


            //right side down primary support

            try
            {
                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
                {
                    //height of prim from bottom

                    prim_height = Math.Round((MillimetersToMeters(primsup_des.ElementAt(1).PrimaryZhtNB)), 2).ToString();
                }

                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
                {
                    prim_height = Math.Round((MillimetersToMeters(primsup_des.ElementAt(1).Centroid.Z)), 2).ToString();
                }
            }
            catch (Exception)
            {

            }

            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
            {
                double radius = 801.5625;

                FixCreatePrimarySupportwithvertex(AcadBlockTableRecord, AcadTransaction, AcadDatabase, pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Bot_L"] - 1500, pointsinfo[Defination.SecBotRB].Y + 500 + extrainfo["sec_Right_Bot_B"] + (radius * 1.5), radius);
                //height of prim from bottom
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Bot_L"] - 1500 + 3000, pointsinfo[Defination.SecBotRB].Y + 500 + extrainfo["sec_Right_Bot_B"] + (radius * 1.5) - 100, 0), "CL.EL.(+)" + prim_height);

            }

            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
            {
                double radius = 400;
                info[Defination.Prim_Radius] = radius;
                info[Defination.Prim_ht] = radius;

                CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Prim_block_U", pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Bot_L"] - 1500, pointsinfo[Defination.SecBotRB].Y + 500 + extrainfo["sec_Right_Bot_B"] + radius, radius);
                //height of prim from bottom
                // CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] - 1500, centerY - 100, 0), "CL.EL.(+)" + info[Defination.Prim_ht].ToString());

                //string clampname = ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName;
                //DrawLeader(AcadBlockTableRecord, AcadTransaction, AcadDatabase, clampname, new Point3d(pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Bot_L"] - 1500, pointsinfo[Defination.SecBotRB].Y + 500 + extrainfo["sec_Right_Bot_B"] + radius, 0), new Point3d(pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Bot_L"] - 1500 - 4 * radius, pointsinfo[Defination.SecBotRB].Y + 500 + extrainfo["sec_Right_Bot_B"] + radius + 1.4 * radius, 0));
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Bot_L"] - 1500 + info[Defination.Prim_Radius] + 2500, pointsinfo[Defination.SecBotRB].Y + 500 + extrainfo["sec_Right_Bot_B"] + radius - 100, 0), "CL.EL.(+)" + prim_height);
                LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Bot_L"] - 1500 + info[Defination.Prim_Radius], pointsinfo[Defination.SecBotRB].Y + 500 + extrainfo["sec_Right_Bot_B"] + radius, 0), new Point3d(pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Bot_L"] - 1500 + info[Defination.Prim_Radius] + 3000, pointsinfo[Defination.SecBotRB].Y + 500 + extrainfo["sec_Right_Bot_B"] + radius, 0), MyCol.LightBlue);
                string clampname = ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName;
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Bot_L"] - 1500 + info[Defination.Prim_Radius] + 2500, pointsinfo[Defination.SecBotRB].Y + 500 + extrainfo["sec_Right_Bot_B"] + radius + 300, 0), clampname);

            }
            //hori dimen right side down primary n sec
            l_dist_frm_centre = 0;
            r_dist_frm_centre = 0;
            //dimensioning
            try
            {
                var strpt = secsupp_des.ElementAt(1).StPt;
                var endpt = secsupp_des.ElementAt(1).EndPt;
                var midpt = primsup_des.ElementAt(1).Centroid;

                Point3d projectedpt = FindPerpendicularFoot(midpt, strpt, endpt);

                var dist1 = Math.Round(GetDist(new Point3d(strpt), projectedpt));
                var dist2 = Math.Round(GetDist(new Point3d(endpt), projectedpt));

                l_dist_frm_centre = Math.Max(dist1, dist2);
                r_dist_frm_centre = Math.Min(dist1, dist2);
            }
            catch (Exception)
            {

            }

            //dimensioning
            CreateDimension(new Point3d(pointsinfo[Defination.SecBotRB].X, pointsinfo[Defination.SecBotRB].Y + 500 + extrainfo["sec_Right_Bot_B"], 0), new Point3d(pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Bot_L"] - 1500, pointsinfo[Defination.SecBotRB].Y + 500 + extrainfo["sec_Right_Bot_B"] + (info[Defination.Prim_Radius] * 1.5) + info[Defination.Prim_Radius], 0), l_dist_frm_centre.ToString(), 300);
            CreateDimension(new Point3d(pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Bot_L"], pointsinfo[Defination.SecBotRB].Y + 500 + extrainfo["sec_Right_Bot_B"], 0), new Point3d(pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Bot_L"] - 1500, pointsinfo[Defination.SecBotRB].Y + 500 + extrainfo["sec_Right_Bot_B"] + (info[Defination.Prim_Radius] * 1.5) + info[Defination.Prim_Radius], 0), r_dist_frm_centre.ToString(), 300);


            //concrete support
            height = 1000;
            length = 3000;
            info[Defination.Concrete_l] = length;
            info[Defination.Concrete_b] = height;

            if (ListCentalSuppoData[i].ListConcreteData.Count > 0)
            {
                FixCreateBottomSupportTopType2(Document2D, AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY, height, length, i);
            }

            //support name and quantity
            CreateSupportName(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, i);


            tracex += boxlen;

        }


        [Obsolete]
        public void DRS23(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, ref double tracex, double boxlen, double boxht, ref double spaceY, Document Document2D, string SupType, [Optional] int i)
        {

            double upperYgap = 3500;
            double centerX = tracex + boxlen / 2;  // 9869.9480;
            double centerY = spaceY - upperYgap;
            //box boundaries
            //vertical line
            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex + boxlen, spaceY + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);

            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex, spaceY - boxht + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);
            var secsupp_des = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection.ToLower() == "hor").OrderByDescending(e => e.Boundingboxmax.Z);

            string prim_height = "";
            try
            {
                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
                {
                    //height of prim from bottom

                    prim_height = Math.Round((MillimetersToMeters(ListCentalSuppoData[i].ListPrimarySuppo[0].PrimaryZhtNB)), 2).ToString();
                }

                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
                {
                    prim_height = Math.Round((MillimetersToMeters(ListCentalSuppoData[i].ListPrimarySuppo[0].Centroid.Z)), 2).ToString();
                }
            }
            catch (Exception)
            {

            }
            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
            {
                FixCreatePrimarySupportwithvertex(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX + 1000, centerY, 801.5625);
                //height of prim from bottom
                try
                {
                    //    prim_height = MillimetersToMeters(Convert.ToDouble(prim_height)).ToString();
                }
                catch (Exception)
                {

                }
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + 1000 + info[Defination.Prim_Radius] + 1500, centerY - 100, 0), "CL.EL.(+)" + prim_height);


            }

            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
            {
                double radius = 200;
                info[Defination.Prim_Radius] = radius;
                info[Defination.Prim_ht] = radius;

                CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Prim_block_U", centerX + 1000, centerY, radius);


                //string clampname = ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName;
                //DrawLeader(AcadBlockTableRecord, AcadTransaction, AcadDatabase, clampname, new Point3d(centerX + 1000, centerY + 1.4 * radius, 0), new Point3d(centerX + 1000 - 4 * radius, centerY + 1.4 * radius, 0));
                //height of prim from bottom
                try
                {
                    //    prim_height = MillimetersToMeters(Convert.ToDouble(prim_height)).ToString();
                }
                catch (Exception)
                {

                }
                // CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] - 3500, centerY - 100, 0), "CL.EL.(+)" + prim_height);
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + 1000 - info[Defination.Prim_Radius] - 2500, centerY - 100, 0), "CL.EL.(+)" + prim_height);
                LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + 1000 - info[Defination.Prim_Radius], centerY, 0), new Point3d(centerX + 1000 - info[Defination.Prim_Radius] - 3000, centerY, 0), MyCol.LightBlue);
                string clampname = ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName;
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + 1000 - info[Defination.Prim_Radius] - 2500, centerY + 300, 0), clampname);


            }



            double height = 300;
            double length = 5000;
            info[Defination.Sec_top_l] = length;
            info[Defination.Sec_top_b] = height;
            var sectionsize = 0.0;
            try
            {
                var section = secsupp_des.ElementAt(0).Size;

                sectionsize = Convert.ToDouble(CSECTIONSIZE(section));
            }
            catch (Exception)
            {

            }
            //upper secondary supp
            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht] - height, 0), new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht] - height, 0), SecThick.Nothing, "l", 0, sectionsize);

            //info for future
            pointsinfo[Defination.SecTopLT] = new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht], 0);
            pointsinfo[Defination.SecTopRT] = new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht], 0);
            pointsinfo[Defination.SecTopRB] = new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht] - height, 0);
            pointsinfo[Defination.SecTopLB] = new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht] - height, 0);

            double l_dist_frm_centre = 0;
            double r_dist_frm_centre = 0;
            //dimensioning
            try
            {
                var strpt = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection == "Hor").First().StPt;

                var endpt = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection == "Hor").First().EndPt;

                // var midpt = ListCentalSuppoData[i].ListPrimarySuppo[0].Midpoint;
                var midpt = ListCentalSuppoData[i].ListPrimarySuppo[0].Centroid;

                Point3d projectedpt = FindPerpendicularFoot(midpt, strpt, endpt);

                l_dist_frm_centre = Math.Round(GetDist(new Point3d(strpt), projectedpt));
                r_dist_frm_centre = Math.Round(GetDist(new Point3d(endpt), projectedpt));
            }
            catch (Exception)
            {

            }


            //double strtmiddist=GetDist(ldist)

            CreateDimension(pointsinfo[Defination.SecTopLT], new Point3d(centerX + 1000, centerY + info[Defination.Prim_Radius], 0), l_dist_frm_centre.ToString());
            CreateDimension(pointsinfo[Defination.SecTopRT], new Point3d(centerX + 1000, centerY + info[Defination.Prim_Radius], 0), r_dist_frm_centre.ToString());


            //LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length / 2 + 4000, centerY - info[Defination.Prim_ht], 0), MyCol.LightBlue);


            string TotalHt = "";
            //mtext
            try
            {
                TotalHt = Math.Round(MillimetersToMeters(ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection.ToLower() == "hor").First().Boundingboxmax.Z), 2).ToString();
            }
            catch (Exception)
            {
            }
            TOSLOC(AcadBlockTableRecord, AcadTransaction, AcadDatabase, pointsinfo[Defination.SecTopRT].X, pointsinfo[Defination.SecTopRT].Y, TotalHt);

            try
            {
                //TotalHt = MillimetersToMeters(Convert.ToDouble(TotalHt)).ToString();
            }
            catch (Exception)
            {

            }
            //CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + 3500, centerY - info[Defination.Prim_ht] + 300, 0), "TOS EL.(+)" + TotalHt);

            info[Defination.Sec_ht_top] = info[Defination.Prim_ht] + height;

            height = 3000;
            length = 300;

            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(pointsinfo[Defination.SecTopLT].X + 1500, pointsinfo[Defination.SecTopLT].Y, 0), new Point3d(pointsinfo[Defination.SecTopLT].X + 1500 + length, pointsinfo[Defination.SecTopLT].Y, 0), new Point3d(pointsinfo[Defination.SecTopLT].X + 1500 + length, pointsinfo[Defination.SecTopLT].Y - height, 0), new Point3d(pointsinfo[Defination.SecTopLT].X + 1500, pointsinfo[Defination.SecTopLT].Y - height, 0), SecThick.Nothing, "l");

            //info for future
            pointsinfo[Defination.SecBotLT] = new Point3d(pointsinfo[Defination.SecTopLT].X + 1500, pointsinfo[Defination.SecTopLT].Y, 0);
            pointsinfo[Defination.SecBotRT] = new Point3d(pointsinfo[Defination.SecTopLT].X + 1500 + length, pointsinfo[Defination.SecTopLT].Y, 0);
            pointsinfo[Defination.SecBotRB] = new Point3d(pointsinfo[Defination.SecTopLT].X + 1500 + length, pointsinfo[Defination.SecTopLT].Y - height, 0);
            pointsinfo[Defination.SecBotLB] = new Point3d(pointsinfo[Defination.SecTopLT].X + 1500 - length, pointsinfo[Defination.SecTopLT].Y - height, 0);

            //dimensioning
            var botsec = "";
            try
            {
                botsec = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.BoxData.Z == ListCentalSuppoData[i].ListSecondrySuppo.Max(s => s.BoxData.Z)).First().BoxData.Z.ToString();
            }
            catch (Exception)
            {

            }

            CreateAlighDimen(AcadBlockTableRecord, AcadTransaction, pointsinfo[Defination.SecBotRT], pointsinfo[Defination.SecBotRB], botsec);

            info[Defination.Sec_ht_bot] = info[Defination.Sec_ht_top] + height;

            //height = 1000;
            //length = 3000;
            //info[Defination.Concrete_l] = length;
            //info[Defination.Concrete_b] = height;

            //if (ListCentalSuppoData[i].ListConcreteData.Count > 0)
            //{
            //    FixCreateBottomSupportTopType2(Document2D, AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY, height, length, i);
            //}

            //support name and quantity
            CreateSupportName(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, i);


            tracex += boxlen;

        }


        [Obsolete]
        public void DRS24(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, ref double tracex, double boxlen, double boxht, ref double spaceY, Document Document2D, string SupType, [Optional] int i)
        {

            double upperYgap = 3500;
            double centerX = tracex + boxlen / 2;  // 9869.9480;
            double centerY = spaceY - upperYgap;
            //box boundaries
            //vertical line
            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex + boxlen, spaceY + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);

            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex, spaceY - boxht + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);
            var secsupp_des = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection.ToLower() == "hor").OrderByDescending(e => e.Boundingboxmax.Z);

            string prim_height = "";
            try
            {
                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
                {
                    //height of prim from bottom

                    prim_height = Math.Round((MillimetersToMeters(ListCentalSuppoData[i].ListPrimarySuppo[0].PrimaryZhtNB)), 2).ToString();
                }

                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
                {
                    prim_height = Math.Round((MillimetersToMeters(ListCentalSuppoData[i].ListPrimarySuppo[0].Centroid.Z)), 2).ToString();
                }
            }
            catch (Exception)
            {

            }

            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
            {
                FixCreatePrimarySupportwithvertex(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY, 801.5625);

                try
                {
                    //prim_height = MillimetersToMeters(Convert.ToDouble(prim_height)).ToString();
                }
                catch (Exception)
                {

                }



                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] + 1500, centerY - 100, 0), "CL.EL.(+)" + prim_height);

            }

            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
            {
                double radius = 500;
                info[Defination.Prim_Radius] = radius;
                info[Defination.Prim_ht] = radius;

                CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Prim_block_U", centerX, centerY, radius);
                //height of prim from bottom
                // CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] - 1500, centerY - 100, 0), "CL.EL.(+)" + info[Defination.Prim_ht].ToString());

                // string clampname = ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName;
                // DrawLeader(AcadBlockTableRecord, AcadTransaction, AcadDatabase, clampname, new Point3d(centerX, centerY + 1.4 * radius, 0), new Point3d(centerX - 4 * radius, centerY + 1.4 * radius, 0));

                try
                {
                    // prim_height = MillimetersToMeters(Convert.ToDouble(prim_height)).ToString();
                }
                catch (Exception)
                {

                }

                // CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] - 1500, centerY - 100, 0), "CL.EL.(+)" + prim_height);
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - info[Defination.Prim_Radius] - 2500, centerY - 100, 0), "CL.EL.(+)" + prim_height);
                LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - info[Defination.Prim_Radius], centerY, 0), new Point3d(centerX - info[Defination.Prim_Radius] - 3000, centerY, 0), MyCol.LightBlue);
                string clampname = ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName;
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - info[Defination.Prim_Radius] - 2500, centerY + 300, 0), clampname);

            }



            double height = 800;
            double length = 3000;
            info[Defination.Sec_top_l] = length;
            info[Defination.Sec_top_b] = height;
            var sectionsize = 0.0;
            try
            {
                var section = secsupp_des.ElementAt(0).Size;

                sectionsize = Convert.ToDouble(CSECTIONSIZE(section));
            }
            catch (Exception)
            {

            }
            //upper secondary supp
            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht] - height, 0), new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht] - height, 0), SecThick.HBoth, "l", 0, sectionsize);

            //info for future
            pointsinfo[Defination.SecTopLT] = new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht], 0);
            pointsinfo[Defination.SecTopRT] = new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht], 0);
            pointsinfo[Defination.SecTopRB] = new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht] - height, 0);
            pointsinfo[Defination.SecTopLB] = new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht] - height, 0);

            double l_dist_frm_centre = 0;
            double r_dist_frm_centre = 0;
            //dimensioning
            try
            {
                var strpt = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection == "Hor").First().StPt;

                var endpt = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection == "Hor").First().EndPt;

                // var midpt = ListCentalSuppoData[i].ListPrimarySuppo[0].Midpoint;
                var midpt = ListCentalSuppoData[i].ListPrimarySuppo[0].Centroid;

                Point3d projectedpt = FindPerpendicularFoot(midpt, strpt, endpt);

                l_dist_frm_centre = Math.Round(GetDist(new Point3d(strpt), projectedpt));
                r_dist_frm_centre = Math.Round(GetDist(new Point3d(endpt), projectedpt));
            }
            catch (Exception)
            {

            }


            //double strtmiddist=GetDist(ldist)

            CreateDimension(new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX, centerY + info[Defination.Prim_Radius], 0), l_dist_frm_centre.ToString());
            CreateDimension(new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX, centerY + info[Defination.Prim_Radius], 0), r_dist_frm_centre.ToString());


            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length / 2 + 4000, centerY - info[Defination.Prim_ht], 0), MyCol.LightBlue);

            // vertical dimensioning
            string TotalHt = "";
            //mtext


            try
            {
                TotalHt = Math.Round(MillimetersToMeters(ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection.ToLower() == "hor").First().Boundingboxmax.Z), 2).ToString();
            }
            catch (Exception)
            {
            }
            TOSLOC(AcadBlockTableRecord, AcadTransaction, AcadDatabase, pointsinfo[Defination.SecTopRT].X, pointsinfo[Defination.SecTopRT].Y, TotalHt);


            try
            {
                //TotalHt = MillimetersToMeters(Convert.ToDouble(TotalHt)).ToString();
            }
            catch (Exception)
            {

            }
            CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + 2500, centerY - info[Defination.Prim_ht] + 300, 0), "TOS EL.(+)" + TotalHt);

            info[Defination.Sec_ht_top] = info[Defination.Prim_ht] + height;

            try
            {
                sectionsize = Convert.ToDouble(CSECTIONSIZE(ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection.ToLower() == "ver").First().Size));
            }
            catch (Exception)
            {

            }
            height = 3000;
            length = 1000;

            //sec bot
            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length / 2, centerY - info[Defination.Sec_ht_top] + info[Defination.Sec_top_b] - 200, 0), new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top] + info[Defination.Sec_top_b] - 200, 0), new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top] - height, 0), new Point3d(centerX - length / 2, centerY - info[Defination.Sec_ht_top] - height, 0), SecThick.VBoth, "l");

            //info for future
            pointsinfo[Defination.SecBotLT] = new Point3d(centerX - length / 2, centerY - info[Defination.Sec_ht_top], 0);
            pointsinfo[Defination.SecBotRT] = new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top], 0);
            pointsinfo[Defination.SecBotRB] = new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top] - height, 0);
            pointsinfo[Defination.SecBotLB] = new Point3d(centerX - length / 2, centerY - info[Defination.Sec_ht_top] - height, 0);

            //dimensioning
            var botsec = "";
            try
            {
                botsec = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.BoxData.Z == ListCentalSuppoData[i].ListSecondrySuppo.Max(s => s.BoxData.Z)).First().BoxData.Z.ToString();
            }
            catch (Exception)
            {

            }


            CreateAlighDimen(AcadBlockTableRecord, AcadTransaction, new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top] - height, 0), botsec);

            info[Defination.Sec_ht_bot] = info[Defination.Sec_ht_top] + height;

            height = 1000;
            length = 3000;
            info[Defination.Concrete_l] = length;
            info[Defination.Concrete_b] = height;

            if (ListCentalSuppoData[i].ListConcreteData.Count > 0)
            {
                FixCreateBottomSupportTopType2(Document2D, AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY, height, length, i);

            }



            //support name and quantity
            CreateSupportName(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, i);


            tracex += boxlen;

        }

        [Obsolete]
        public void DRS25(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, ref double tracex, double boxlen, double boxht, ref double spaceY, Document Document2D, string SupType, [Optional] int i)
        {
            double upperYgap = 3500;

            double centerX = tracex + boxlen / 2;  // 9869.9480;
            double centerY = spaceY - upperYgap;

            //for support18 only
            centerY += 2000;
            centerX -= 1500;

            //box boundaries
            //vertical line
            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex + boxlen, spaceY + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);

            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex, spaceY - boxht + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);

            //CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Prim_block_U", centerX, centerY, radius);
            var secsupp_des = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.SupportName.ToLower().Contains("structure")).OrderByDescending(e => e.Boundingboxmax.Z);

            string prim_height = "";
            try
            {
                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
                {
                    //height of prim from bottom

                    prim_height = Math.Round((MillimetersToMeters(ListCentalSuppoData[i].ListPrimarySuppo[0].PrimaryZhtNB)), 2).ToString();
                }

                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
                {
                    prim_height = Math.Round((MillimetersToMeters(ListCentalSuppoData[i].ListPrimarySuppo[0].Centroid.Z)), 2).ToString();
                }
            }
            catch (Exception)
            {

            }
            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
            {
                FixCreatePrimarySupportwithvertex(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY, 801.5625);
                //height of prim from bottom
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] + 1500, centerY - 100, 0), "CL.EL.(+)" + info[Defination.Prim_ht].ToString());

            }

            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
            {
                double radius = 300;
                info[Defination.Prim_Radius] = radius;
                info[Defination.Prim_ht] = radius;

                CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Prim_block_U", centerX, centerY, radius);
                //height of prim from bottom
                // CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] - 1500, centerY - 100, 0), "CL.EL.(+)" + info[Defination.Prim_ht].ToString());

                //string clampname = ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName;
                //DrawLeader(AcadBlockTableRecord, AcadTransaction, AcadDatabase, clampname, new Point3d(centerX, centerY + 1.4 * radius, 0), new Point3d(centerX - 4 * radius, centerY + 1.4 * radius, 0));
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - info[Defination.Prim_Radius] - 2500, centerY - 100, 0), "CL.EL.(+)" + prim_height);
                LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - info[Defination.Prim_Radius], centerY, 0), new Point3d(centerX - info[Defination.Prim_Radius] - 3000, centerY, 0), MyCol.LightBlue);
                string clampname = ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName;
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - info[Defination.Prim_Radius] - 2500, centerY + 300, 0), clampname);

            }



            //upper top sec supp
            double height = 1000;
            double length = 4000;
            info[Defination.Sec_top_l] = length;
            info[Defination.Sec_top_b] = height;
            //double ht_frm_cen = 1220.7383;0.33,0.66
            var sectionsize = 0.0;
            try
            {
                sectionsize = Convert.ToDouble(CSECTIONSIZE(secsupp_des.ElementAt(1).Size));
            }
            catch (Exception)
            {

            }
            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length * 0.34, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length * 0.66, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length * 0.66, centerY - info[Defination.Prim_ht] - height, 0), new Point3d(centerX - length * 0.34, centerY - info[Defination.Prim_ht] - height, 0), SecThick.HBoth, "l", 0, sectionsize);


            //info for future
            pointsinfo[Defination.SecTopLT] = new Point3d(centerX - length * 0.34, centerY - info[Defination.Prim_ht], 0);
            pointsinfo[Defination.SecTopRT] = new Point3d(centerX + length * 0.66, centerY - info[Defination.Prim_ht], 0);
            pointsinfo[Defination.SecTopRB] = new Point3d(centerX + length * 0.66, centerY - info[Defination.Prim_ht] - height, 0);
            pointsinfo[Defination.SecTopLB] = new Point3d(centerX - length * 0.34, centerY - info[Defination.Prim_ht] - height, 0);


            double l_dist_frm_centre = 0;
            double r_dist_frm_centre = 0;
            //dimensioning hori prim top sec
            try
            {
                var strpt = GetArrayAbsoluteValues(secsupp_des.ElementAt(1).StPt);
                var endpt = GetArrayAbsoluteValues(secsupp_des.ElementAt(1).EndPt);
                var midpt = Get3dAbsoluteValues(ListCentalSuppoData[i].ListPrimarySuppo[0].Centroid);
                Point3d projectedpt = FindPerpendicularFoot(midpt, strpt, endpt);

                var dist1 = Math.Round(GetDist(new Point3d(strpt), projectedpt));
                var dist2 = Math.Round(GetDist(new Point3d(endpt), projectedpt));

                l_dist_frm_centre = Math.Min(dist1, dist2);
                r_dist_frm_centre = Math.Max(dist1, dist2);
            }
            catch (Exception)
            {

            }

            //dimensioning
            CreateDimension(new Point3d(centerX - length * 0.34, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX, centerY + info[Defination.Prim_Radius], 0), l_dist_frm_centre.ToString(), 500);
            CreateDimension(new Point3d(centerX + length * 0.66, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX, centerY + info[Defination.Prim_Radius], 0), r_dist_frm_centre.ToString(), 500);


            //LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + length * 0.66, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length * 0.66 + 4000, centerY - info[Defination.Prim_ht], 0), MyCol.LightBlue);


            string TotalHt = "";

            //mtext
            try
            {
                TotalHt = Math.Round(MillimetersToMeters(secsupp_des.ElementAt(0).Boundingboxmax.Z), 2).ToString();
            }
            catch (Exception)
            {
            }
            TOSLOC(AcadBlockTableRecord, AcadTransaction, AcadDatabase, pointsinfo[Defination.SecTopRT].X, pointsinfo[Defination.SecTopRT].Y, TotalHt);


            try
            {
                //TotalHt = MillimetersToMeters(Convert.ToDouble(TotalHt)).ToString();
            }
            catch (Exception)
            {

            }

            info[Defination.Sec_ht_top] = info[Defination.Prim_ht] + height;
            try
            {
                sectionsize = Convert.ToDouble(CSECTIONSIZE(secsupp_des.ElementAt(0).Size));
            }
            catch (Exception)
            {

            }
            //bot sec supp
            height = 6000;
            length = 1000;
            info[Defination.Sec_bot_l] = length;
            info[Defination.Sec_bot_b] = height;

            info[Defination.Sec_ht_bot] = height + 700;

            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(pointsinfo[Defination.SecTopRT].X - 500, pointsinfo[Defination.SecTopRT].Y + 500, 0), new Point3d(pointsinfo[Defination.SecTopRT].X - 500 + info[Defination.Sec_bot_l], pointsinfo[Defination.SecTopRT].Y + 500, 0), new Point3d(pointsinfo[Defination.SecTopRT].X - 500 + info[Defination.Sec_bot_l], pointsinfo[Defination.SecTopRT].Y + 500 - info[Defination.Sec_bot_b], 0), new Point3d(pointsinfo[Defination.SecTopRT].X - 500, pointsinfo[Defination.SecTopRT].Y + 500 - info[Defination.Sec_bot_b], 0), SecThick.VBoth, "nbl");

            //info for future
            pointsinfo[Defination.SecBotLT] = new Point3d(pointsinfo[Defination.SecTopRT].X - 500, pointsinfo[Defination.SecTopRT].Y + 500, 0);
            pointsinfo[Defination.SecBotRT] = new Point3d(pointsinfo[Defination.SecTopRT].X - 500 + info[Defination.Sec_bot_l], pointsinfo[Defination.SecTopRT].Y + 500, 0);
            pointsinfo[Defination.SecBotRB] = new Point3d(pointsinfo[Defination.SecTopRT].X - 500 + info[Defination.Sec_bot_l], pointsinfo[Defination.SecTopRT].Y + 500 - info[Defination.Sec_bot_b], 0);
            pointsinfo[Defination.SecBotLB] = new Point3d(pointsinfo[Defination.SecTopRT].X - 500, pointsinfo[Defination.SecTopRT].Y + 500 - info[Defination.Sec_bot_b], 0);
            //dimensioning
            var botsec = "";
            try
            {
                botsec = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.BoxData.Z == ListCentalSuppoData[i].ListSecondrySuppo.Max(s => s.BoxData.Z)).First().BoxData.Z.ToString();
            }
            catch (Exception)
            {

            }

            CreateAlighDimen(AcadBlockTableRecord, AcadTransaction, pointsinfo[Defination.SecBotRT], pointsinfo[Defination.SecBotRB], botsec, -1000);


            //draw upper plate 
            height = 200;
            length = 1500;
            info[Defination.Plate_l] = length;
            info[Defination.Plate_b] = height;


            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d((pointsinfo[Defination.SecBotLT].X + pointsinfo[Defination.SecBotRT].X) / 2 - info[Defination.Plate_l] / 2, pointsinfo[Defination.SecBotLT].Y + info[Defination.Plate_b], 0), new Point3d((pointsinfo[Defination.SecBotLT].X + pointsinfo[Defination.SecBotRT].X) / 2 + info[Defination.Plate_l] / 2, pointsinfo[Defination.SecBotLT].Y + info[Defination.Plate_b], 0), new Point3d((pointsinfo[Defination.SecBotLT].X + pointsinfo[Defination.SecBotRT].X) / 2 + info[Defination.Plate_l] / 2, pointsinfo[Defination.SecBotLT].Y, 0), new Point3d((pointsinfo[Defination.SecBotLT].X + pointsinfo[Defination.SecBotRT].X) / 2 - info[Defination.Plate_l] / 2, pointsinfo[Defination.SecBotLT].Y, 0), SecThick.Nothing);



            //height = 1000;
            //length = 3000;
            //info[Defination.Concrete_l] = length;
            //info[Defination.Concrete_b] = height;

            //concrete down
            CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "concrete_patel", (pointsinfo[Defination.SecBotLB].X + pointsinfo[Defination.SecBotRB].X) / 2, pointsinfo[Defination.SecBotLB].Y, 500);

            //concrete verti ht
            try
            {
                TotalHt = ListCentalSuppoData[i].ListConcreteData[0].Boundingboxmax.Z.ToString();
            }
            catch (Exception)
            {

            }
            TOSLOC(AcadBlockTableRecord, AcadTransaction, AcadDatabase, (pointsinfo[Defination.SecBotLB].X + pointsinfo[Defination.SecBotRB].X) / 2, pointsinfo[Defination.SecBotLB].Y, TotalHt, "r", "EL.(+)");
            try
            {
                TotalHt = ListCentalSuppoData[i].ListConcreteData[0].Boundingboxmin.Z.ToString();
            }
            catch (Exception)
            {

            }
            TOSLOC(AcadBlockTableRecord, AcadTransaction, AcadDatabase, (pointsinfo[Defination.SecBotLB].X + pointsinfo[Defination.SecBotRB].X) / 2, pointsinfo[Defination.SecBotLB].Y - (500 * 3.5063), TotalHt, "r", "FGL.EL.(+)");

            //concrete block dimension
            try
            {
                botsec = ListCentalSuppoData[i].ListConcreteData[0].BoxData.Z.ToString();
            }
            catch (Exception)
            {

            }

            CreateAlighDimen(AcadBlockTableRecord, AcadTransaction, new Point3d((pointsinfo[Defination.SecBotLB].X + pointsinfo[Defination.SecBotRB].X) / 2 - (3 * 500), pointsinfo[Defination.SecBotLB].Y, 0), new Point3d((pointsinfo[Defination.SecBotLB].X + pointsinfo[Defination.SecBotRB].X) / 2 - (3 * 500), pointsinfo[Defination.SecBotLB].Y - (500 * 3.5063), 0), botsec, 1000);

            //if (ListCentalSuppoData[i].ListConcreteData.Count > 0)
            //{
            //    FixCreateBottomSupportTopType2(Document2D, AcadBlockTableRecord, AcadTransaction, AcadDatabase, (pointsinfo[Defination.SecBotLT].X + pointsinfo[Defination.SecBotRT].X) / 2, centerY, height, length, i);

            //}

            //support name and quantity
            CreateSupportName(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, i);

            tracex += boxlen;
        }



        [Obsolete]
        public void DRS26(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, ref double tracex, double boxlen, double boxht, ref double spaceY, Document Document2D, string SupType, [Optional] int i)
        {
            double upperYgap = 3500;

            double centerX = tracex + boxlen / 2;  // 9869.9480;
            double centerY = spaceY - upperYgap;
            //box boundaries
            //vertical line
            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex + boxlen, spaceY + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);

            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex, spaceY - boxht + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);

            //CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Prim_block_U", centerX, centerY, radius);
            var secsupp_des = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.SupportName.ToLower().Contains("structure")).OrderByDescending(e => e.Boundingboxmax.Z);

            string prim_height = "";
            try
            {
                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
                {
                    //height of prim from bottom

                    prim_height = Math.Round((MillimetersToMeters(ListCentalSuppoData[i].ListPrimarySuppo[0].PrimaryZhtNB)), 2).ToString();
                }

                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
                {
                    prim_height = Math.Round((MillimetersToMeters(ListCentalSuppoData[i].ListPrimarySuppo[0].Centroid.Z)), 2).ToString();
                }
            }
            catch (Exception)
            {

            }

            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
            {
                FixCreatePrimarySupportwithvertex(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY, 801.5625);
                //height of prim from bottom
                try
                {
                    // prim_height = MillimetersToMeters(Convert.ToDouble(prim_height)).ToString();
                }
                catch (Exception)
                {

                }
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] + 1500, centerY - 100, 0), "CL.EL.(+)" + prim_height);

            }

            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
            {
                double radius = 500;
                info[Defination.Prim_Radius] = radius;
                info[Defination.Prim_ht] = radius;

                CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Prim_block_U", centerX, centerY, radius);


                // string clampname = ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName;
                // DrawLeader(AcadBlockTableRecord, AcadTransaction, AcadDatabase, clampname, new Point3d(centerX, centerY + 1.4 * radius, 0), new Point3d(centerX - 4 * radius, centerY + 1.4 * radius, 0));

                //height of prim from bottom
                try
                {
                    // prim_height = MillimetersToMeters(Convert.ToDouble(prim_height)).ToString();
                }
                catch (Exception)
                {

                }
                //CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] - 1500, centerY - 100, 0), "CL.EL.(+)" + prim_height);

                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] + 2500, centerY - 100, 0), "CL.EL.(+)10" + prim_height);
                LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius], centerY, 0), new Point3d(centerX + info[Defination.Prim_Radius] + 3000, centerY, 0), MyCol.LightBlue);
                string clampname = ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName;
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] + 2500, centerY + 300, 0), clampname);

            }



            //upper top sec
            double height = 1000;
            double length = 4000;
            info[Defination.Sec_top_l] = length;
            info[Defination.Sec_top_b] = height;
            //double ht_frm_cen = 1220.7383;
            var sectionsize = 0.0;
            try
            {
                var section = secsupp_des.ElementAt(1).Size;
                sectionsize = Convert.ToDouble(CSECTIONSIZE(section));
            }
            catch (Exception)
            {

            }
            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length * 0.66, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length * 0.34, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length * 0.34, centerY - info[Defination.Prim_ht] - height, 0), new Point3d(centerX - length * 0.66, centerY - info[Defination.Prim_ht] - height, 0), SecThick.HBoth, "R", 0, sectionsize);


            //info for future
            pointsinfo[Defination.SecTopLT] = new Point3d(centerX - length * 0.66, centerY - info[Defination.Prim_ht], 0);
            pointsinfo[Defination.SecTopRT] = new Point3d(centerX + length * 0.34, centerY - info[Defination.Prim_ht], 0);
            pointsinfo[Defination.SecTopRB] = new Point3d(centerX + length * 0.34, centerY - info[Defination.Prim_ht] - height, 0);
            pointsinfo[Defination.SecTopLB] = new Point3d(centerX - length * 0.66, centerY - info[Defination.Prim_ht] - height, 0);

            //hori dimen prim and sec
            double l_dist_frm_centre = 0;
            double r_dist_frm_centre = 0;
            //dimensioning
            try
            {
                var strpt = GetArrayAbsoluteValues(secsupp_des.ElementAt(1).StPt);
                var endpt = GetArrayAbsoluteValues(secsupp_des.ElementAt(1).EndPt);
                var midpt = Get3dAbsoluteValues(ListCentalSuppoData[i].ListPrimarySuppo[0].Centroid);
                Point3d projectedpt = FindPerpendicularFoot(midpt, strpt, endpt);

                var dist1 = Math.Round(GetDist(new Point3d(strpt), projectedpt));
                var dist2 = Math.Round(GetDist(new Point3d(endpt), projectedpt));

                l_dist_frm_centre = Math.Max(dist1, dist2);
                r_dist_frm_centre = Math.Min(dist1, dist2);
            }
            catch (Exception)
            {

            }
            //dimensioning
            CreateDimension(new Point3d(centerX - length * 0.66, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX, centerY + info[Defination.Prim_Radius], 0), l_dist_frm_centre.ToString());
            CreateDimension(new Point3d(centerX + length * 0.34, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX, centerY + info[Defination.Prim_Radius], 0), r_dist_frm_centre.ToString());


            // LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + length * 0.34, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length * 0.34 + 4000, centerY - info[Defination.Prim_ht], 0), MyCol.LightBlue);


            string TotalHt = "";

            //mtext
            try
            {
                TotalHt = Math.Round(MillimetersToMeters(secsupp_des.ElementAt(0).Boundingboxmax.Z), 2).ToString();
            }
            catch (Exception)
            {
            }
            TOSLOC(AcadBlockTableRecord, AcadTransaction, AcadDatabase, pointsinfo[Defination.SecTopRT].X, pointsinfo[Defination.SecTopRT].Y, TotalHt);

            try
            {
                //TotalHt = MillimetersToMeters(Convert.ToDouble(TotalHt)).ToString();
            }
            catch (Exception)
            {

            }


            //CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + 2500, centerY - info[Defination.Prim_ht] + 300, 0), "BOP(+)" + TotalHt);

            info[Defination.Sec_ht_top] = info[Defination.Prim_ht] + height;
            try
            {
                sectionsize = Convert.ToDouble(CSECTIONSIZE(secsupp_des.ElementAt(0).Size));
            }
            catch (Exception)
            {

            }

            //bot sec
            height = 4000;
            length = 1000;
            info[Defination.Sec_bot_l] = length;
            info[Defination.Sec_bot_b] = height;

            info[Defination.Sec_ht_bot] = height + 700;

            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(pointsinfo[Defination.SecTopLT].X - 500, pointsinfo[Defination.SecTopLT].Y + 500, 0), new Point3d(pointsinfo[Defination.SecTopLT].X - 500 + info[Defination.Sec_bot_l], pointsinfo[Defination.SecTopLT].Y + 500, pointsinfo[Defination.SecTopLT].Z), new Point3d(pointsinfo[Defination.SecTopLT].X - 500 + info[Defination.Sec_bot_l], pointsinfo[Defination.SecTopLT].Y + 500 - info[Defination.Sec_bot_b], pointsinfo[Defination.SecTopLT].Z), new Point3d(pointsinfo[Defination.SecTopLT].X - 500, pointsinfo[Defination.SecTopLT].Y + 500 - info[Defination.Sec_bot_b], pointsinfo[Defination.SecTopLT].Z), SecThick.VBoth, "nbl", -1000);

            //info for future
            pointsinfo[Defination.SecBotLT] = new Point3d(pointsinfo[Defination.SecTopLT].X - 500, pointsinfo[Defination.SecTopLT].Y + 500, 0);
            pointsinfo[Defination.SecBotRT] = new Point3d(pointsinfo[Defination.SecTopLT].X - 500 + info[Defination.Sec_bot_l], pointsinfo[Defination.SecTopLT].Y + 500, pointsinfo[Defination.SecTopLT].Z);
            pointsinfo[Defination.SecBotRB] = new Point3d(pointsinfo[Defination.SecTopLT].X - 500 + info[Defination.Sec_bot_l], pointsinfo[Defination.SecTopLT].Y + 500 - info[Defination.Sec_bot_b], pointsinfo[Defination.SecTopLT].Z);
            pointsinfo[Defination.SecBotLB] = new Point3d(pointsinfo[Defination.SecTopLT].X - 500, pointsinfo[Defination.SecTopLT].Y + 500 - info[Defination.Sec_bot_b], pointsinfo[Defination.SecTopLT].Z);
            //dimensioning

            var botsec = "";
            try
            {
                botsec = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.BoxData.Z == ListCentalSuppoData[i].ListSecondrySuppo.Max(s => s.BoxData.Z)).First().BoxData.Z.ToString();
            }
            catch (Exception)
            {

            }

            CreateAlighDimen(AcadBlockTableRecord, AcadTransaction, pointsinfo[Defination.SecBotLT], pointsinfo[Defination.SecBotLB], botsec, 500);


            //draw upper plate 
            height = 200;
            length = 1500;
            info[Defination.Plate_l] = length;
            info[Defination.Plate_b] = height;


            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d((pointsinfo[Defination.SecBotLT].X + pointsinfo[Defination.SecBotRT].X) / 2 - info[Defination.Plate_l] / 2, pointsinfo[Defination.SecBotLT].Y + info[Defination.Plate_b], 0), new Point3d((pointsinfo[Defination.SecBotLT].X + pointsinfo[Defination.SecBotRT].X) / 2 + info[Defination.Plate_l] / 2, pointsinfo[Defination.SecBotLT].Y + info[Defination.Plate_b], 0), new Point3d((pointsinfo[Defination.SecBotLT].X + pointsinfo[Defination.SecBotRT].X) / 2 + info[Defination.Plate_l] / 2, pointsinfo[Defination.SecBotLT].Y, 0), new Point3d((pointsinfo[Defination.SecBotLT].X + pointsinfo[Defination.SecBotRT].X) / 2 - info[Defination.Plate_l] / 2, pointsinfo[Defination.SecBotLT].Y, 0), SecThick.Nothing);


            //concrete supp
            CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "concrete_patel", (pointsinfo[Defination.SecBotLB].X + pointsinfo[Defination.SecBotRB].X) / 2, pointsinfo[Defination.SecBotLB].Y, 500);

            //concrete verti ht
            try
            {
                TotalHt = ListCentalSuppoData[i].ListConcreteData[0].Boundingboxmax.Z.ToString();
            }
            catch (Exception)
            {

            }
            TOSLOC(AcadBlockTableRecord, AcadTransaction, AcadDatabase, (pointsinfo[Defination.SecBotLB].X + pointsinfo[Defination.SecBotRB].X) / 2, pointsinfo[Defination.SecBotLB].Y, TotalHt, "r", "EL.(+)");
            try
            {
                TotalHt = ListCentalSuppoData[i].ListConcreteData[0].Boundingboxmin.Z.ToString();
            }
            catch (Exception)
            {

            }
            TOSLOC(AcadBlockTableRecord, AcadTransaction, AcadDatabase, (pointsinfo[Defination.SecBotLB].X + pointsinfo[Defination.SecBotRB].X) / 2, pointsinfo[Defination.SecBotLB].Y - (500 * 3.5063), TotalHt, "r", "FGL.EL.(+)");

            //concrete block dimension
            try
            {
                botsec = ListCentalSuppoData[i].ListConcreteData[0].BoxData.Z.ToString();
            }
            catch (Exception)
            {

            }

            CreateAlighDimen(AcadBlockTableRecord, AcadTransaction, new Point3d((pointsinfo[Defination.SecBotLB].X + pointsinfo[Defination.SecBotRB].X) / 2 - (3 * 500), pointsinfo[Defination.SecBotLB].Y, 0), new Point3d((pointsinfo[Defination.SecBotLB].X + pointsinfo[Defination.SecBotRB].X) / 2 - (3 * 500), pointsinfo[Defination.SecBotLB].Y - (500 * 3.5063), 0), botsec, 1000);

            //height = 1000;
            //length = 3000;
            //info[Defination.Concrete_l] = length;
            //info[Defination.Concrete_b] = height;

            //if (ListCentalSuppoData[i].ListConcreteData.Count > 0)
            //{
            //    FixCreateBottomSupportTopType2(Document2D, AcadBlockTableRecord, AcadTransaction, AcadDatabase, (pointsinfo[Defination.SecBotLT].X + pointsinfo[Defination.SecBotRT].X) / 2, centerY, height, length, i);

            //}

            //support name and quantity
            CreateSupportName(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, i);

            tracex += boxlen;
        }


        [Obsolete]
        public void DRS27(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, ref double tracex, double boxlen, double boxht, ref double spaceY, Document Document2D, string SupType, [Optional] int i)
        {

            double upperYgap = 3500;
            double centerX = tracex + boxlen / 2;  // 9869.9480;
            double centerY = spaceY - upperYgap;
            //box boundaries
            //vertical line
            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex + boxlen, spaceY + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);

            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex, spaceY - boxht + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);

            var primsup_des = ListCentalSuppoData[i].ListPrimarySuppo.OrderByDescending(e => e.Centroid.Z);
            var secsupp_des = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.SupportName.ToLower().Contains("structure")).OrderByDescending(e => e.Boundingboxmax.Z);

            string prim_height = "";
            try
            {
                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
                {
                    //height of prim from bottom

                    prim_height = Math.Round((MillimetersToMeters(primsup_des.ElementAt(0).PrimaryZhtNB)), 2).ToString();
                }

                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
                {
                    prim_height = Math.Round((MillimetersToMeters(primsup_des.ElementAt(0).Centroid.Z)), 2).ToString();
                }
            }
            catch (Exception)
            {

            }
            //left prim supp
            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
            {
                FixCreatePrimarySupportwithvertex(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX - 2200, centerY, 801.5625, "l");

                try
                {
                    // prim_height = MillimetersToMeters(Convert.ToDouble(prim_height)).ToString();
                }
                catch (Exception)
                {

                }



                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - 2200 - info[Defination.Prim_Radius] - 1500, centerY - 100, 0), "CL.EL.(+)" + prim_height);

            }

            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
            {
                double radius = 500;
                info[Defination.Prim_Radius] = radius;
                info[Defination.Prim_ht] = radius;

                CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Prim_block_U", centerX - 2200, centerY, radius);
                //height of prim from bottom
                // CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] - 1500, centerY - 100, 0), "CL.EL.(+)" + info[Defination.Prim_ht].ToString());

                //string clampname = ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName;
                //DrawLeader(AcadBlockTableRecord, AcadTransaction, AcadDatabase, clampname, new Point3d(centerX, centerY + 1.4 * radius, 0), new Point3d(centerX - 4 * radius, centerY + 1.4 * radius, 0));

                //CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] - 1500, centerY - 100, 0), "CL.EL.(+)" + prim_height);
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - 2200 - info[Defination.Prim_Radius] - 3500, centerY - 100, 0), "CL.EL.(+)" + prim_height);
                LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - 2200 - info[Defination.Prim_Radius], centerY, 0), new Point3d(centerX - 2200 - info[Defination.Prim_Radius] - 3000, centerY, 0), MyCol.LightBlue);
                string clampname = ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName;
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - 2200 - info[Defination.Prim_Radius] - 3500, centerY + 300, 0), clampname);

            }

            //left side top sec
            double height = 1000;
            double length = 3500;
            info[Defination.Sec_top_l] = length;
            info[Defination.Sec_top_b] = height;

            var sectionsize = 0.0;
            try
            {
                sectionsize = Convert.ToDouble(CSECTIONSIZE(secsupp_des.Where(e => e.TouchingPartid == primsup_des.ElementAt(0).SuppoId).First().Size));
            }
            catch (Exception)
            {

            }

            //upper secondary supp
            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX, centerY - info[Defination.Prim_ht] - height, 0), new Point3d(centerX - length, centerY - info[Defination.Prim_ht] - height, 0), SecThick.HBoth, "l");

            //info for future
            pointsinfo[Defination.SecTopLT] = new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht], 0);
            pointsinfo[Defination.SecTopRT] = new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht], 0);
            pointsinfo[Defination.SecTopRB] = new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht] - height, 0);
            pointsinfo[Defination.SecTopLB] = new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht] - height, 0);

            //here line is drawn to split on sec supp into two


            //dimension hori both prim sec
            double l_dist_frm_centre = 0;
            double r_dist_frm_centre = 0;

            //dimensioning left hori prim top sec
            try
            {
                var strpt = GetArrayAbsoluteValues(secsupp_des.Where(e => e.TouchingPartid == primsup_des.ElementAt(0).SuppoId).First().StPt);
                var endpt = GetArrayAbsoluteValues(secsupp_des.Where(e => e.TouchingPartid == primsup_des.ElementAt(0).SuppoId).First().EndPt);
                var midpt = Get3dAbsoluteValues(primsup_des.ElementAt(0).Centroid);
                Point3d projectedpt = FindPerpendicularFoot(midpt, strpt, endpt);

                var dist1 = Math.Round(GetDist(new Point3d(strpt), projectedpt));
                var dist2 = Math.Round(GetDist(new Point3d(endpt), projectedpt));

                l_dist_frm_centre = Math.Min(dist1, dist2);
                r_dist_frm_centre = Math.Max(dist1, dist2);
            }
            catch (Exception)
            {

            }

            //double strtmiddist=GetDist(ldist)

            CreateDimension(pointsinfo[Defination.SecTopLT], new Point3d(centerX - 2200, centerY + info[Defination.Prim_Radius], 0), l_dist_frm_centre.ToString());
            CreateDimension(pointsinfo[Defination.SecTopRT], new Point3d(centerX - 2200, centerY + info[Defination.Prim_Radius], 0), r_dist_frm_centre.ToString());


            //LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length / 2 + 4000, centerY - info[Defination.Prim_ht], 0), MyCol.LightBlue);


            string TotalHt = "";
            //mtext
            try
            {
                TotalHt = Math.Round(MillimetersToMeters(secsupp_des.Where(e => e.TouchingPartid == primsup_des.ElementAt(0).SuppoId).First().Boundingboxmax.Z), 2).ToString();
            }
            catch (Exception)
            {
            }
            TOSLOC(AcadBlockTableRecord, AcadTransaction, AcadDatabase, pointsinfo[Defination.SecTopRT].X, pointsinfo[Defination.SecTopRT].Y, TotalHt);

            try
            {
                //TotalHt = MillimetersToMeters(Convert.ToDouble(TotalHt)).ToString();
            }
            catch (Exception)
            {

            }

            //CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + 4500, centerY - info[Defination.Prim_ht] + 300, 0), "TOS EL.(+)" + TotalHt);


            //right prim supp
            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
            {
                FixCreatePrimarySupportwithvertex(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX + 2200, centerY, 801.5625);


                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + 2200 + info[Defination.Prim_Radius] + 1500, centerY - 100, 0), "CL.EL.(+)" + prim_height);

            }

            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
            {
                double radius = 400;
                info[Defination.Prim_Radius] = radius;
                info[Defination.Prim_ht] = radius;

                CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Prim_block_U", centerX + 2200, centerY, radius);


                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + 2200 + info[Defination.Prim_Radius] + 2500, centerY - 100, 0), "CL.EL.(+)" + prim_height);
                LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + 2200 + info[Defination.Prim_Radius], centerY, 0), new Point3d(centerX + 2200 + info[Defination.Prim_Radius] + 3000, centerY, 0), MyCol.LightBlue);
                string clampname = ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName;
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + 2200 + info[Defination.Prim_Radius] + 2500, centerY + 300, 0), clampname);

            }

            //right side top sec

            height = 1000;
            length = 3500;
            info[Defination.Sec_top_l] = length;
            info[Defination.Sec_top_b] = height;

            sectionsize = 0.0;
            try
            {
                sectionsize = Convert.ToDouble(CSECTIONSIZE(secsupp_des.Where(e => e.TouchingPartid == primsup_des.ElementAt(1).SuppoId).First().Size));
            }
            catch (Exception)
            {

            }

            //upper secondary supp
            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length, centerY - info[Defination.Prim_ht] - height, 0), new Point3d(centerX, centerY - info[Defination.Prim_ht] - height, 0), SecThick.HBoth, "R");

            ////info for future
            //pointsinfo[Defination.SecTopLT] = new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht], 0);
            //pointsinfo[Defination.SecTopRT] = new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht], 0);
            //pointsinfo[Defination.SecTopRB] = new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht] - height, 0);
            //pointsinfo[Defination.SecTopLB] = new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht] - height, 0);

            //here line is drawn to split on sec supp into two


            //dimension hori both prim sec
            //dimensioning
            l_dist_frm_centre = 0;
            r_dist_frm_centre = 0;
            //dimensioning right hori prim top sec
            try
            {
                var strpt = GetArrayAbsoluteValues(secsupp_des.Where(e => e.TouchingPartid == primsup_des.ElementAt(1).SuppoId).First().StPt);
                var endpt = GetArrayAbsoluteValues(secsupp_des.Where(e => e.TouchingPartid == primsup_des.ElementAt(1).SuppoId).First().EndPt);
                var midpt = Get3dAbsoluteValues(primsup_des.ElementAt(0).Centroid);
                Point3d projectedpt = FindPerpendicularFoot(midpt, strpt, endpt);

                var dist1 = Math.Round(GetDist(new Point3d(strpt), projectedpt));
                var dist2 = Math.Round(GetDist(new Point3d(endpt), projectedpt));

                l_dist_frm_centre = Math.Max(dist1, dist2);
                r_dist_frm_centre = Math.Min(dist1, dist2);
            }
            catch (Exception)
            {

            }

            //double strtmiddist=GetDist(ldist)

            CreateDimension(new Point3d(centerX, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + 2200, centerY + info[Defination.Prim_Radius], 0), l_dist_frm_centre.ToString());
            CreateDimension(new Point3d(centerX + length, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + 2200, centerY + info[Defination.Prim_Radius], 0), r_dist_frm_centre.ToString());


            //LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length / 2 + 4000, centerY - info[Defination.Prim_ht], 0), MyCol.LightBlue);



            //mtext
            try
            {
                TotalHt = Math.Round(MillimetersToMeters(secsupp_des.Where(e => e.TouchingPartid == primsup_des.ElementAt(1).SuppoId).First().Boundingboxmax.Z), 2).ToString();
            }
            catch (Exception)
            {
            }
            TOSLOC(AcadBlockTableRecord, AcadTransaction, AcadDatabase, pointsinfo[Defination.SecTopRT].X, pointsinfo[Defination.SecTopRT].Y, TotalHt);

            try
            {
                //TotalHt = MillimetersToMeters(Convert.ToDouble(TotalHt)).ToString();
            }
            catch (Exception)
            {

            }

            //CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + 4500, centerY - info[Defination.Prim_ht] + 300, 0), "TOS EL.(+)" + TotalHt);



            //bot sec
            info[Defination.Sec_ht_top] = info[Defination.Prim_ht] + height;
            //bot sec
            height = 4000;
            length = 1000;
            info[Defination.Sec_ht_bot] = info[Defination.Sec_ht_top] + height;

            sectionsize = 0.0;
            try
            {
                sectionsize = Convert.ToDouble(CSECTIONSIZE(secsupp_des.ElementAt(0).Size));
            }
            catch (Exception)
            {

            }
            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length / 2, centerY - info[Defination.Sec_ht_top] - 1500, 0), new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top] - 1500, 0), new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top] - height, 0), new Point3d(centerX - length / 2, centerY - info[Defination.Sec_ht_top] - height, 0), SecThick.Left, "nbl");

            //info for future
            pointsinfo[Defination.SecBotLT] = new Point3d(centerX - length / 2, centerY - info[Defination.Sec_ht_top], 0);
            pointsinfo[Defination.SecBotRT] = new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top], 0);
            pointsinfo[Defination.SecBotRB] = new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top] - height, 0);
            pointsinfo[Defination.SecBotLB] = new Point3d(centerX - length / 2, centerY - info[Defination.Sec_ht_top] - height, 0);

            //dimensioning
            var botsec = "";
            try
            {
                botsec = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.BoxData.Z == ListCentalSuppoData[i].ListSecondrySuppo.Max(s => s.BoxData.Z)).First().BoxData.Z.ToString();
            }
            catch (Exception)
            {

            }

            CreateAlighDimen(AcadBlockTableRecord, AcadTransaction, new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top] - height, 0), botsec);




            //draw upper plate 
            height = 200;
            length = 1500;
            info[Defination.Plate_l] = length;
            info[Defination.Plate_b] = height;


            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d((pointsinfo[Defination.SecBotLT].X + pointsinfo[Defination.SecBotRT].X) / 2 - info[Defination.Plate_l] / 2, pointsinfo[Defination.SecBotLT].Y + info[Defination.Plate_b], 0), new Point3d((pointsinfo[Defination.SecBotLT].X + pointsinfo[Defination.SecBotRT].X) / 2 + info[Defination.Plate_l] / 2, pointsinfo[Defination.SecBotLT].Y + info[Defination.Plate_b], 0), new Point3d((pointsinfo[Defination.SecBotLT].X + pointsinfo[Defination.SecBotRT].X) / 2 + info[Defination.Plate_l] / 2, pointsinfo[Defination.SecBotLT].Y, 0), new Point3d((pointsinfo[Defination.SecBotLT].X + pointsinfo[Defination.SecBotRT].X) / 2 - info[Defination.Plate_l] / 2, pointsinfo[Defination.SecBotLT].Y, 0), SecThick.Nothing);


            //concrete supp
            CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "concrete_patel", (pointsinfo[Defination.SecBotLB].X + pointsinfo[Defination.SecBotRB].X) / 2, pointsinfo[Defination.SecBotLB].Y, 500);

            //concrete verti ht
            try
            {
                TotalHt = ListCentalSuppoData[i].ListConcreteData[0].Boundingboxmax.Z.ToString();
            }
            catch (Exception)
            {

            }
            TOSLOC(AcadBlockTableRecord, AcadTransaction, AcadDatabase, (pointsinfo[Defination.SecBotLB].X + pointsinfo[Defination.SecBotRB].X) / 2, pointsinfo[Defination.SecBotLB].Y, TotalHt, "r", "EL.(+)");
            try
            {
                TotalHt = ListCentalSuppoData[i].ListConcreteData[0].Boundingboxmin.Z.ToString();
            }
            catch (Exception)
            {

            }
            TOSLOC(AcadBlockTableRecord, AcadTransaction, AcadDatabase, (pointsinfo[Defination.SecBotLB].X + pointsinfo[Defination.SecBotRB].X) / 2, pointsinfo[Defination.SecBotLB].Y - (500 * 3.5063), TotalHt, "r", "FGL.EL.(+)");

            //concrete block dimension
            try
            {
                botsec = ListCentalSuppoData[i].ListConcreteData[0].BoxData.Z.ToString();
            }
            catch (Exception)
            {

            }

            CreateAlighDimen(AcadBlockTableRecord, AcadTransaction, new Point3d((pointsinfo[Defination.SecBotLB].X + pointsinfo[Defination.SecBotRB].X) / 2 - (3 * 500), pointsinfo[Defination.SecBotLB].Y, 0), new Point3d((pointsinfo[Defination.SecBotLB].X + pointsinfo[Defination.SecBotRB].X) / 2 - (3 * 500), pointsinfo[Defination.SecBotLB].Y - (500 * 3.5063), 0), botsec, 1000);
            //height = 1000;
            //length = 3000;
            //info[Defination.Concrete_l] = length;
            //info[Defination.Concrete_b] = height;

            //if (ListCentalSuppoData[i].ListConcreteData.Count > 0)
            //{
            //    FixCreateBottomSupportTopType2(Document2D, AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY, height, length, i);

            //}


            //support name and quantity
            CreateSupportName(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, i);


            tracex += boxlen;

        }

        [Obsolete]
        public void DRS28(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, ref double tracex, double boxlen, double boxht, ref double spaceY, Document Document2D, string SupType, [Optional] int i)
        {

            double upperYgap = 3500;
            double centerX = tracex + boxlen / 2;  // 9869.9480;
            double centerY = spaceY - upperYgap;
            //box boundaries
            //vertical line
            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex + boxlen, spaceY + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);

            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex, spaceY - boxht + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);

            var secsupp_des = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.SupportName.ToLower().Contains("structure")).OrderByDescending(e => e.Boundingboxmax.Z);

            string prim_height = "";
            try
            {
                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
                {
                    //height of prim from bottom

                    prim_height = Math.Round((MillimetersToMeters(ListCentalSuppoData[i].ListPrimarySuppo[0].PrimaryZhtNB)), 2).ToString();
                }

                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
                {
                    prim_height = Math.Round((MillimetersToMeters(ListCentalSuppoData[i].ListPrimarySuppo[0].Centroid.Z)), 2).ToString();
                }
            }
            catch (Exception)
            {

            }

            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
            {
                FixCreatePrimarySupportwithvertex(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY, 801.5625);

                try
                {
                    //prim_height = MillimetersToMeters(Convert.ToDouble(prim_height)).ToString();
                }
                catch (Exception)
                {

                }



                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] + 1500, centerY - 100, 0), "CL.EL.(+)" + prim_height);

            }

            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
            {
                double radius = 500;
                info[Defination.Prim_Radius] = radius;
                info[Defination.Prim_ht] = radius;

                CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Prim_block_U", centerX, centerY, radius);
                //height of prim from bottom
                // CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] - 1500, centerY - 100, 0), "CL.EL.(+)" + info[Defination.Prim_ht].ToString());

                //string clampname = ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName;
                //DrawLeader(AcadBlockTableRecord, AcadTransaction, AcadDatabase, clampname, new Point3d(centerX, centerY + 1.4 * radius, 0), new Point3d(centerX - 4 * radius, centerY + 1.4 * radius, 0));

                try
                {
                    //    prim_height = MillimetersToMeters(Convert.ToDouble(prim_height)).ToString();
                }
                catch (Exception)
                {

                }

                //CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] - 1500, centerY - 100, 0), "CL.EL.(+)" + prim_height);
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - info[Defination.Prim_Radius] - 2500, centerY - 100, 0), "CL.EL.(+)" + prim_height);
                LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - info[Defination.Prim_Radius], centerY, 0), new Point3d(centerX - info[Defination.Prim_Radius] - 3000, centerY, 0), MyCol.LightBlue);
                string clampname = ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName;
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - info[Defination.Prim_Radius] - 2500, centerY + 300, 0), clampname);

            }



            double height = 1000;
            double length = 2000;
            info[Defination.Sec_top_l] = length;
            info[Defination.Sec_top_b] = height;

            //upper secondary supp
            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht] - height, 0), new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht] - height, 0), SecThick.HBoth, "l");

            //info for future
            pointsinfo[Defination.SecTopLT] = new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht], 0);
            pointsinfo[Defination.SecTopRT] = new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht], 0);
            pointsinfo[Defination.SecTopRB] = new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht] - height, 0);
            pointsinfo[Defination.SecTopLB] = new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht] - height, 0);

            //dimensioning
            double l_dist_frm_centre = 0;
            double r_dist_frm_centre = 0;
            //dimensioning hori prim top sec
            try
            {
                var strpt = GetArrayAbsoluteValues(secsupp_des.ElementAt(0).StPt);
                var endpt = GetArrayAbsoluteValues(secsupp_des.ElementAt(0).EndPt);
                var midpt = Get3dAbsoluteValues(ListCentalSuppoData[i].ListPrimarySuppo[0].Centroid);
                Point3d projectedpt = FindPerpendicularFoot(midpt, strpt, endpt);

                var dist1 = Math.Round(GetDist(new Point3d(strpt), projectedpt));
                var dist2 = Math.Round(GetDist(new Point3d(endpt), projectedpt));

                l_dist_frm_centre = Math.Min(dist1, dist2);
                r_dist_frm_centre = Math.Max(dist1, dist2);
            }
            catch (Exception)
            {

            }

            //double strtmiddist=GetDist(ldist)

            CreateDimension(new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX, centerY + info[Defination.Prim_Radius], 0), l_dist_frm_centre.ToString());
            CreateDimension(new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX, centerY + info[Defination.Prim_Radius], 0), r_dist_frm_centre.ToString());


            // LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length / 2 + 4000, centerY - info[Defination.Prim_ht], 0), MyCol.LightBlue);

            // vertical dimensioning
            string TotalHt = "";
            //mtext


            try
            {
                TotalHt = Math.Round(MillimetersToMeters(ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection.ToLower() == "hor").First().Boundingboxmax.Z), 2).ToString();
            }
            catch (Exception)
            {
            }
            TOSLOC(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY, TotalHt);


            try
            {
                //TotalHt = MillimetersToMeters(Convert.ToDouble(TotalHt)).ToString();
            }
            catch (Exception)
            {

            }


            //upper plate
            height = 200;
            length = 1500;
            info[Defination.Sec_ht_top] = info[Defination.Prim_ht] + height;

            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length / 2, centerY - info[Defination.Sec_ht_top], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top] - height, 0), new Point3d(centerX - length / 2, centerY - info[Defination.Sec_ht_top] - height, 0), SecThick.Nothing, "none");


            info[Defination.Sec_ht_top] = info[Defination.Prim_ht] + height + 200; //plus 200 for plate
            var sectionsize = 0.0;
            try
            {
                sectionsize = Convert.ToDouble(CSECTIONSIZE(ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection.ToLower() == "ver").First().Size));
            }
            catch (Exception)
            {

            }
            height = 3000;
            length = 1000;

            //sec bot
            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length / 2, centerY - info[Defination.Sec_ht_top], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top] - height, 0), new Point3d(centerX - length / 2, centerY - info[Defination.Sec_ht_top] - height, 0), SecThick.VBoth, "nbl");

            //info for future
            pointsinfo[Defination.SecBotLT] = new Point3d(centerX - length / 2, centerY - info[Defination.Sec_ht_top], 0);
            pointsinfo[Defination.SecBotRT] = new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top], 0);
            pointsinfo[Defination.SecBotRB] = new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top] - height, 0);
            pointsinfo[Defination.SecBotLB] = new Point3d(centerX - length / 2, centerY - info[Defination.Sec_ht_top] - height, 0);

            //dimensioning
            var botsec = "";
            try
            {
                botsec = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.BoxData.Z == ListCentalSuppoData[i].ListSecondrySuppo.Max(s => s.BoxData.Z)).First().BoxData.Z.ToString();
            }
            catch (Exception)
            {

            }


            CreateAlighDimen(AcadBlockTableRecord, AcadTransaction, new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top] - height, 0), botsec);

            info[Defination.Sec_ht_bot] = info[Defination.Sec_ht_top] + height;

            CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "concrete_patel", (pointsinfo[Defination.SecBotLB].X + pointsinfo[Defination.SecBotRB].X) / 2, pointsinfo[Defination.SecBotLB].Y, 500);


            //concrete verti ht
            try
            {
                TotalHt = ListCentalSuppoData[i].ListConcreteData[0].Boundingboxmax.Z.ToString();
            }
            catch (Exception)
            {

            }
            TOSLOC(AcadBlockTableRecord, AcadTransaction, AcadDatabase, (pointsinfo[Defination.SecBotLB].X + pointsinfo[Defination.SecBotRB].X) / 2, pointsinfo[Defination.SecBotLB].Y, TotalHt, "r", "EL.(+)");
            try
            {
                TotalHt = ListCentalSuppoData[i].ListConcreteData[0].Boundingboxmin.Z.ToString();
            }
            catch (Exception)
            {

            }
            TOSLOC(AcadBlockTableRecord, AcadTransaction, AcadDatabase, (pointsinfo[Defination.SecBotLB].X + pointsinfo[Defination.SecBotRB].X) / 2, pointsinfo[Defination.SecBotLB].Y - (500 * 3.5063), TotalHt, "r", "FGL.EL.(+)");

            //concrete block dimension
            try
            {
                botsec = ListCentalSuppoData[i].ListConcreteData[0].BoxData.Z.ToString();
            }
            catch (Exception)
            {

            }

            CreateAlighDimen(AcadBlockTableRecord, AcadTransaction, new Point3d((pointsinfo[Defination.SecBotLB].X + pointsinfo[Defination.SecBotRB].X) / 2 - (3 * 500), pointsinfo[Defination.SecBotLB].Y, 0), new Point3d((pointsinfo[Defination.SecBotLB].X + pointsinfo[Defination.SecBotRB].X) / 2 - (3 * 500), pointsinfo[Defination.SecBotLB].Y - (500 * 3.5063), 0), botsec, 1000);
            //height = 1000;
            //length = 3000;
            //info[Defination.Concrete_l] = length;
            //info[Defination.Concrete_b] = height;

            //if (ListCentalSuppoData[i].ListConcreteData.Count > 0)
            //{
            //    FixCreateBottomSupportTopType2(Document2D, AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY, height, length, i);

            //}



            //support name and quantity
            CreateSupportName(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, i);


            tracex += boxlen;

        }


        [Obsolete]
        public void DRS29(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, ref double tracex, double boxlen, double boxht, ref double spaceY, Document Document2D, string SupType, [Optional] int i)
        {
            double upperYgap = 3500;

            double centerX = tracex + boxlen / 2;  // 9869.9480;
            double centerY = spaceY - upperYgap;

            //for support18 only
            centerY += 2000;
            centerX -= 1500;

            //box boundaries
            //vertical line
            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex + boxlen, spaceY + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);

            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex, spaceY - boxht + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);

            var secsupp_des = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.SupportName.ToLower().Contains("structure")).OrderByDescending(e => e.Boundingboxmax.Z);
            //CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Prim_block_U", centerX, centerY, radius);
            string prim_height = "";
            try
            {
                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
                {
                    //height of prim from bottom

                    prim_height = Math.Round((MillimetersToMeters(ListCentalSuppoData[i].ListPrimarySuppo[0].PrimaryZhtNB)), 2).ToString();
                }

                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
                {
                    prim_height = Math.Round((MillimetersToMeters(ListCentalSuppoData[i].ListPrimarySuppo[0].Centroid.Z)), 2).ToString();
                }
            }
            catch (Exception)
            {

            }
            double radius = 801.5625;
            info[Defination.Prim_Radius] = radius;
            info[Defination.Prim_ht] = radius * 1.5;

            //upper top sec
            double height = 1000;
            double length = 4000;
            info[Defination.Sec_top_l] = length;
            info[Defination.Sec_top_b] = height;
            //double ht_frm_cen = 1220.7383;0.33,0.66
            //upper top secondary supp
            // BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length * 0.34, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length * 0.66, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length * 0.66, centerY - info[Defination.Prim_ht] - height, 0), new Point3d(centerX - length * 0.34, centerY - info[Defination.Prim_ht] - height, 0), SecThick.HBoth, "R");


            //info for future
            pointsinfo[Defination.SecTopLT] = new Point3d(centerX - length * 0.34, centerY - info[Defination.Prim_ht], 0);
            pointsinfo[Defination.SecTopRT] = new Point3d(centerX + length * 0.66, centerY - info[Defination.Prim_ht], 0);
            pointsinfo[Defination.SecTopRB] = new Point3d(centerX + length * 0.66, centerY - info[Defination.Prim_ht] - height, 0);
            pointsinfo[Defination.SecTopLB] = new Point3d(centerX - length * 0.34, centerY - info[Defination.Prim_ht] - height, 0);

            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + length * 0.66, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length * 0.66 + 4000, centerY - info[Defination.Prim_ht], 0), MyCol.LightBlue);


            string TotalHt = "";

            //mtext


            try
            {
                //TotalHt = MillimetersToMeters(Convert.ToDouble(TotalHt)).ToString();
            }
            catch (Exception)
            {

            }




            info[Defination.Sec_ht_top] = info[Defination.Prim_ht] + height;

            var sectionsize = 0.0;
            try
            {
                sectionsize = Convert.ToDouble(CSECTIONSIZE(ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection.ToLower() == "ver").First().Size));
            }
            catch (Exception)
            {

            }
            //bot sec
            height = 5000;
            length = 1000;
            info[Defination.Sec_bot_l] = length;
            info[Defination.Sec_bot_b] = height;

            info[Defination.Sec_ht_bot] = height;
            //botsec sec supp
            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(pointsinfo[Defination.SecTopRB].X - info[Defination.Sec_bot_l] - 500, pointsinfo[Defination.SecTopRB].Y, 0), new Point3d(pointsinfo[Defination.SecTopRB].X - 500, pointsinfo[Defination.SecTopRB].Y, 0), new Point3d(pointsinfo[Defination.SecTopRB].X - 500, pointsinfo[Defination.SecTopRB].Y - info[Defination.Sec_bot_b], 0), new Point3d(pointsinfo[Defination.SecTopRB].X - info[Defination.Sec_bot_l] - 500, pointsinfo[Defination.SecTopRB].Y - info[Defination.Sec_bot_b], 0), SecThick.VBoth, "nbl", 1700);

            //info for future
            pointsinfo[Defination.SecBotLT] = new Point3d(pointsinfo[Defination.SecTopRB].X - info[Defination.Sec_bot_l] - 500, pointsinfo[Defination.SecTopRB].Y, 0);
            pointsinfo[Defination.SecBotRT] = new Point3d(pointsinfo[Defination.SecTopRB].X - 500, pointsinfo[Defination.SecTopRB].Y, 0);
            pointsinfo[Defination.SecBotRB] = new Point3d(pointsinfo[Defination.SecTopRB].X - 500, pointsinfo[Defination.SecTopRB].Y - info[Defination.Sec_bot_b], 0);
            pointsinfo[Defination.SecBotLB] = new Point3d(pointsinfo[Defination.SecTopRB].X - info[Defination.Sec_bot_l] - 500, pointsinfo[Defination.SecTopRB].Y - info[Defination.Sec_bot_b], 0);
            //dimensioning
            var botsec = "";
            try
            {
                botsec = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.BoxData.Z == ListCentalSuppoData[i].ListSecondrySuppo.Max(s => s.BoxData.Z)).First().BoxData.Z.ToString();
            }
            catch (Exception)
            {

            }


            CreateAlighDimen(AcadBlockTableRecord, AcadTransaction, pointsinfo[Defination.SecBotRT], pointsinfo[Defination.SecBotRB], botsec);

            //upper plate//upper plate
            height = 200;
            length = 1500;


            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d((pointsinfo[Defination.SecBotLT].X + pointsinfo[Defination.SecBotRT].X) / 2 - length / 2, pointsinfo[Defination.SecBotLT].Y + height, 0), new Point3d((pointsinfo[Defination.SecBotLT].X + pointsinfo[Defination.SecBotRT].X) / 2 + length / 2, pointsinfo[Defination.SecBotLT].Y + height, 0), new Point3d((pointsinfo[Defination.SecBotLT].X + pointsinfo[Defination.SecBotRT].X) / 2 + length / 2, pointsinfo[Defination.SecBotLT].Y, 0), new Point3d((pointsinfo[Defination.SecBotLT].X + pointsinfo[Defination.SecBotRT].X) / 2 - length / 2, pointsinfo[Defination.SecBotLT].Y, 0), SecThick.Nothing, "none");


            //left side down secondary support
            height = 1000;
            length = 4000;
            extrainfo["sec_Left_Bot_L"] = length;
            extrainfo["sec_Left_Bot_B"] = height;

            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(pointsinfo[Defination.SecBotLB].X - extrainfo["sec_Left_Bot_L"], pointsinfo[Defination.SecBotLB].Y + 500 + extrainfo["sec_Left_Bot_B"], 0), new Point3d(pointsinfo[Defination.SecBotLB].X, pointsinfo[Defination.SecBotLB].Y + 500 + extrainfo["sec_Left_Bot_B"], 0), new Point3d(pointsinfo[Defination.SecBotLB].X, pointsinfo[Defination.SecBotLB].Y + 500, 0), new Point3d(pointsinfo[Defination.SecBotLB].X - extrainfo["sec_Left_Bot_L"], pointsinfo[Defination.SecBotLB].Y + 500, 0), SecThick.HBoth, "l");

            //verti dimen sec left down
            try
            {
                TotalHt = Math.Round(MillimetersToMeters(secsupp_des.ElementAt(1).Boundingboxmax.Z), 2).ToString();
            }
            catch (Exception)
            {
            }
            TOSLOC(AcadBlockTableRecord, AcadTransaction, AcadDatabase, pointsinfo[Defination.SecBotLB].X - extrainfo["sec_Left_Bot_L"], pointsinfo[Defination.SecBotLB].Y + 500 + extrainfo["sec_Left_Bot_B"], TotalHt, "l");



            //left side down primary support

            //verti dimes prim ht
            try
            {
                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
                {
                    //height of prim from bottom

                    prim_height = Math.Round((MillimetersToMeters(ListCentalSuppoData[i].ListPrimarySuppo[0].PrimaryZhtNB)), 2).ToString();
                }

                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
                {
                    prim_height = Math.Round((MillimetersToMeters(ListCentalSuppoData[i].ListPrimarySuppo[0].Centroid.Z)), 2).ToString();
                }
            }
            catch (Exception)
            {

            }

            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
            {
                radius = 801.5625;

                FixCreatePrimarySupportwithvertex(AcadBlockTableRecord, AcadTransaction, AcadDatabase, pointsinfo[Defination.SecBotLB].X - extrainfo["sec_Left_Bot_L"] + 1000, pointsinfo[Defination.SecBotLB].Y + 500 + extrainfo["sec_Left_Bot_B"] + (radius * 1.5), radius, "l");
                //height of prim from bottom
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(pointsinfo[Defination.SecBotLB].X - extrainfo["sec_Left_Bot_L"] + 1000 - info[Defination.Prim_Radius] - 2500, pointsinfo[Defination.SecBotLB].Y + 500 + extrainfo["sec_Left_Bot_B"] + (radius * 1.5) - 100, 0), "CL.EL.(+)" + prim_height);

            }

            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
            {
                radius = 300;
                info[Defination.Prim_Radius] = radius;
                info[Defination.Prim_ht] = radius;

                CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Prim_block_U", pointsinfo[Defination.SecBotLB].X - extrainfo["sec_Left_Bot_L"] + 1000, pointsinfo[Defination.SecBotLB].Y + 500 + extrainfo["sec_Left_Bot_B"] + radius, radius);
                //height of prim from bottom
                // CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] - 1500, centerY - 100, 0), "CL.EL.(+)" + info[Defination.Prim_ht].ToString());


                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(pointsinfo[Defination.SecBotLB].X - extrainfo["sec_Left_Bot_L"] + 1000 - info[Defination.Prim_Radius] - 2500, pointsinfo[Defination.SecBotLB].Y + 500 + extrainfo["sec_Left_Bot_B"] + radius - 100, 0), "CL.EL.(+)" + prim_height);
                LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(pointsinfo[Defination.SecBotLB].X - extrainfo["sec_Left_Bot_L"] + 1000 - info[Defination.Prim_Radius], pointsinfo[Defination.SecBotLB].Y + 500 + extrainfo["sec_Left_Bot_B"] + radius, 0), new Point3d(pointsinfo[Defination.SecBotLB].X - extrainfo["sec_Left_Bot_L"] + 1000 - info[Defination.Prim_Radius] - 3000, pointsinfo[Defination.SecBotLB].Y + 500 + extrainfo["sec_Left_Bot_B"] + radius, 0), MyCol.LightBlue);
                string clampname = ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName;
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(pointsinfo[Defination.SecBotLB].X - extrainfo["sec_Left_Bot_L"] + 1000 - info[Defination.Prim_Radius] - 2500, pointsinfo[Defination.SecBotLB].Y + 500 + extrainfo["sec_Left_Bot_B"] + radius + 300, 0), clampname);

            }

            //left side down dimensioning hori
            double l_dist_frm_centre = 0;
            double r_dist_frm_centre = 0;
            //dimensioning hori prim top sec
            try
            {
                var strpt = GetArrayAbsoluteValues(secsupp_des.ElementAt(2).StPt);
                var endpt = GetArrayAbsoluteValues(secsupp_des.ElementAt(2).EndPt);
                var midpt = Get3dAbsoluteValues(ListCentalSuppoData[i].ListPrimarySuppo[0].Centroid);
                Point3d projectedpt = FindPerpendicularFoot(midpt, strpt, endpt);

                var dist1 = Math.Round(GetDist(new Point3d(strpt), projectedpt));
                var dist2 = Math.Round(GetDist(new Point3d(endpt), projectedpt));

                l_dist_frm_centre = Math.Min(dist1, dist2);
                r_dist_frm_centre = Math.Max(dist1, dist2);
            }
            catch (Exception)
            {

            }
            //dimensioning
            CreateDimension(new Point3d(pointsinfo[Defination.SecBotLB].X - extrainfo["sec_Left_Bot_L"], pointsinfo[Defination.SecBotLB].Y + 500 + extrainfo["sec_Left_Bot_B"], 0), new Point3d(pointsinfo[Defination.SecBotLB].X - extrainfo["sec_Left_Bot_L"] + 1000, pointsinfo[Defination.SecBotLB].Y + 500 + extrainfo["sec_Left_Bot_B"] + (info[Defination.Prim_Radius] * 1.5) + info[Defination.Prim_Radius], 0), l_dist_frm_centre.ToString(), 300);
            CreateDimension(new Point3d(pointsinfo[Defination.SecBotLB].X, pointsinfo[Defination.SecBotLB].Y + 500 + extrainfo["sec_Left_Bot_B"], 0), new Point3d(pointsinfo[Defination.SecBotLB].X - extrainfo["sec_Left_Bot_L"] + 1000, pointsinfo[Defination.SecBotLB].Y + 500 + extrainfo["sec_Left_Bot_B"] + (info[Defination.Prim_Radius] * 1.5) + info[Defination.Prim_Radius], 0), r_dist_frm_centre.ToString(), 300);

            //right side up secondary support
            height = 1000;
            length = 5000;
            extrainfo["sec_Right_Bot_L"] = length;
            extrainfo["sec_Right_Bot_B"] = height;


            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(pointsinfo[Defination.SecBotRB].X, pointsinfo[Defination.SecBotRB].Y + 4500 + extrainfo["sec_Right_Bot_B"], 0), new Point3d(pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Bot_L"], pointsinfo[Defination.SecBotRB].Y + 4500 + extrainfo["sec_Right_Bot_B"], 0), new Point3d(pointsinfo[Defination.SecBotRB].X + extrainfo["sec_Right_Bot_L"], pointsinfo[Defination.SecBotRB].Y + 4500, 0), new Point3d(pointsinfo[Defination.SecBotRB].X, pointsinfo[Defination.SecBotRB].Y + 4500, 0), SecThick.HBoth, "R");

            //verti dimen sec ht right up
            try
            {
                TotalHt = Math.Round(MillimetersToMeters(secsupp_des.ElementAt(0).Boundingboxmax.Z), 2).ToString();
            }
            catch (Exception)
            {
            }
            TOSLOC(AcadBlockTableRecord, AcadTransaction, AcadDatabase, pointsinfo[Defination.SecBotRB].X, pointsinfo[Defination.SecBotRB].Y + 4500 + extrainfo["sec_Right_Bot_B"], TotalHt, "l");


            //draw plate/concrete support 
            //height = 1000;
            //length = 3000;
            //info[Defination.Plate_l] = length;
            //info[Defination.Plate_b] = height;


            //BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d((pointsinfo[Defination.SecBotRB].X + pointsinfo[Defination.SecBotLB].X) / 2 - info[Defination.Plate_l] / 2, pointsinfo[Defination.SecBotLB].Y, 0), new Point3d((pointsinfo[Defination.SecBotRB].X + pointsinfo[Defination.SecBotLB].X) / 2 + info[Defination.Plate_l] / 2, pointsinfo[Defination.SecBotLB].Y, 0), new Point3d((pointsinfo[Defination.SecBotRB].X + pointsinfo[Defination.SecBotLB].X) / 2 + info[Defination.Plate_l] / 2, pointsinfo[Defination.SecBotLB].Y - info[Defination.Plate_b], 0), new Point3d((pointsinfo[Defination.SecBotRB].X + pointsinfo[Defination.SecBotLB].X) / 2 - info[Defination.Plate_l] / 2, pointsinfo[Defination.SecBotLB].Y - info[Defination.Plate_b], 0), SecThick.Nothing);


            CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "concrete_patel", (pointsinfo[Defination.SecBotLB].X + pointsinfo[Defination.SecBotRB].X) / 2, pointsinfo[Defination.SecBotLB].Y, 500);


            //concrete verti ht
            try
            {
                TotalHt = ListCentalSuppoData[i].ListConcreteData[0].Boundingboxmax.Z.ToString();
            }
            catch (Exception)
            {

            }
            TOSLOC(AcadBlockTableRecord, AcadTransaction, AcadDatabase, (pointsinfo[Defination.SecBotLB].X + pointsinfo[Defination.SecBotRB].X) / 2, pointsinfo[Defination.SecBotLB].Y, TotalHt, "r", "EL.(+)");
            try
            {
                TotalHt = ListCentalSuppoData[i].ListConcreteData[0].Boundingboxmin.Z.ToString();
            }
            catch (Exception)
            {

            }
            TOSLOC(AcadBlockTableRecord, AcadTransaction, AcadDatabase, (pointsinfo[Defination.SecBotLB].X + pointsinfo[Defination.SecBotRB].X) / 2, pointsinfo[Defination.SecBotLB].Y - (500 * 3.5063), TotalHt, "r", "FGL.EL.(+)");

            //concrete block dimension
            try
            {
                botsec = ListCentalSuppoData[i].ListConcreteData[0].BoxData.Z.ToString();
            }
            catch (Exception)
            {

            }

            CreateAlighDimen(AcadBlockTableRecord, AcadTransaction, new Point3d((pointsinfo[Defination.SecBotLB].X + pointsinfo[Defination.SecBotRB].X) / 2 - (3 * 500), pointsinfo[Defination.SecBotLB].Y, 0), new Point3d((pointsinfo[Defination.SecBotLB].X + pointsinfo[Defination.SecBotRB].X) / 2 - (3 * 500), pointsinfo[Defination.SecBotLB].Y - (500 * 3.5063), 0), botsec, 1000);
            //height = 1000;
            //length = 3000;
            //info[Defination.Concrete_l] = length;
            //info[Defination.Concrete_b] = height;

            //if (ListCentalSuppoData[i].ListConcreteData.Count > 0)
            //{
            //    FixCreateBottomSupportTopType2(Document2D, AcadBlockTableRecord, AcadTransaction, AcadDatabase, (pointsinfo[Defination.SecBotRB].X + pointsinfo[Defination.SecBotLB].X) / 2, centerY, height, length, i);
            //}

            //support name and quantity
            CreateSupportName(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, i);

            tracex += boxlen;
        }

        [Obsolete]
        public void DRS31(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, ref double tracex, double boxlen, double boxht, ref double spaceY, Document Document2D, string SupType, [Optional] int i)
        {
            double upperYgap = 3500;

            double centerX = tracex + boxlen / 2;  // 9869.9480;
            double centerY = spaceY - upperYgap;
            //box boundaries
            //vertical line
            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex + boxlen, spaceY + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);


            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex, spaceY - boxht + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);

            //FixCreatePrimarySupportwithvertex(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY, 801.5625);

            //CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] + 1500, centerY - 100, 0), "CL.EL.(+)" + info[Defination.Prim_ht].ToString());


            var primsup_des = ListCentalSuppoData[i].ListPrimarySuppo.OrderByDescending(e => e.Centroid.Z);
            var secsupp_des = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection.ToLower() == "hor").OrderByDescending(e => e.Boundingboxmax.Z);


            string prim_height = "";
            try
            {
                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
                {
                    //height of prim from bottom

                    prim_height = Math.Round((MillimetersToMeters(ListCentalSuppoData[i].ListPrimarySuppo[0].PrimaryZhtNB)), 2).ToString();
                }

                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
                {
                    prim_height = Math.Round((MillimetersToMeters(ListCentalSuppoData[i].ListPrimarySuppo[0].Centroid.Z)), 2).ToString();
                }
            }
            catch (Exception)
            {

            }


            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
            {
                FixCreatePrimarySupportwithvertex(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY, 801.5625);
                //height of prim from bottom
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] + 1500, centerY - 100, 0), "CL.EL.(+)" + info[Defination.Prim_ht].ToString());

            }

            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
            {
                double radius = 500;
                info[Defination.Prim_Radius] = radius;
                info[Defination.Prim_ht] = radius;

                CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Prim_block_U", centerX, centerY, radius);
                //height of prim from bottom
                // CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] - 1500, centerY - 100, 0), "CL.EL.(+)" + info[Defination.Prim_ht].ToString());

                //string clampname = ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName;
                //DrawLeader(AcadBlockTableRecord, AcadTransaction, AcadDatabase, clampname, new Point3d(centerX, centerY + 1.4 * radius, 0), new Point3d(centerX - 4 * radius, centerY + 1.4 * radius, 0));
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - info[Defination.Prim_Radius] - 2500, centerY - 100, 0), "CL.EL.(+)" + prim_height);
                LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - info[Defination.Prim_Radius], centerY, 0), new Point3d(centerX - info[Defination.Prim_Radius] - 3000, centerY, 0), MyCol.LightBlue);
                string clampname = ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName;
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - info[Defination.Prim_Radius] - 2500, centerY + 300, 0), clampname);

            }



            //double ht_frm_cen = 1220.7383;

            double height = 1000;
            double length = 3000;
            info[Defination.Sec_top_l] = length;
            info[Defination.Sec_top_b] = height;

            var section = secsupp_des.ElementAt(0).Size;

            var sectionsize = Convert.ToDouble(CSECTIONSIZE(section));

            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length * 0.66, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length * 0.34, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length * 0.34, centerY - info[Defination.Prim_ht] - height, 0), new Point3d(centerX - length * 0.66, centerY - info[Defination.Prim_ht] - height, 0), SecThick.HBoth, "R", 0, sectionsize);

            //info for future
            pointsinfo[Defination.SecTopLT] = new Point3d(centerX - length * 0.66, centerY - info[Defination.Prim_ht], 0);
            pointsinfo[Defination.SecTopRT] = new Point3d(centerX + length * 0.34, centerY - info[Defination.Prim_ht], 0);
            pointsinfo[Defination.SecTopRB] = new Point3d(centerX + length * 0.34, centerY - info[Defination.Prim_ht] - height, 0);
            pointsinfo[Defination.SecTopLB] = new Point3d(centerX - length * 0.66, centerY - info[Defination.Prim_ht] - height, 0);

            double l_dist_frm_centre = 0;
            double r_dist_frm_centre = 0;
            //dimensioning
            try
            {
                var strpt = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection == "Hor").First().StPt;

                var endpt = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection == "Hor").First().EndPt;

                // var midpt = ListCentalSuppoData[i].ListPrimarySuppo[0].Midpoint;
                var midpt = ListCentalSuppoData[i].ListPrimarySuppo[0].Centroid;

                Point3d projectedpt = FindPerpendicularFoot(midpt, strpt, endpt);

                l_dist_frm_centre = Math.Round(GetDist(new Point3d(strpt), projectedpt));
                r_dist_frm_centre = Math.Round(GetDist(new Point3d(endpt), projectedpt));
            }
            catch (Exception)
            {

            }

            //dimensioning
            CreateDimension(new Point3d(centerX - length * 0.66, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX, centerY + info[Defination.Prim_Radius], 0), l_dist_frm_centre.ToString());
            CreateDimension(new Point3d(centerX + length * 0.34, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX, centerY + info[Defination.Prim_Radius], 0), r_dist_frm_centre.ToString());

            // vertical dimensioning
            string TotalHt = "";
            //mtext


            try
            {
                TotalHt = Math.Round(MillimetersToMeters(ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection.ToLower() == "hor").First().Boundingboxmax.Z), 2).ToString();
            }
            catch (Exception)
            {
            }


            try
            {
                //TotalHt = MillimetersToMeters(Convert.ToDouble(TotalHt)).ToString();
            }
            catch (Exception)
            {

            }
            CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + 2500, centerY - info[Defination.Prim_ht] + 300, 0), "TOS EL.(+)" + TotalHt);

            info[Defination.Sec_ht_top] = info[Defination.Prim_ht] + height;

            height = 1000;
            length = 3000;
            info[Defination.Sec_bot_l] = length;
            info[Defination.Sec_bot_b] = height;
            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length * 0.66, centerY - info[Defination.Sec_ht_top] - 1000, 0), new Point3d(centerX - length * 0.66 + 1737.7464, centerY - info[Defination.Sec_ht_top], 0), new Point3d(centerX - length * 0.66 + 1737.7464 + 1000, centerY - info[Defination.Sec_ht_top], 0), new Point3d(centerX - length * 0.66, centerY - info[Defination.Sec_ht_top] - 1000 - 571.1660, 0), SecThick.Nothing, "L");

            //info for future
            pointsinfo[Defination.SecBotLT] = new Point3d(centerX - length * 0.66, centerY - info[Defination.Sec_ht_top] - 1000, 0);
            pointsinfo[Defination.SecBotRT] = new Point3d(centerX - length * 0.66 + 1737.7464, centerY - info[Defination.Sec_ht_top], 0);
            pointsinfo[Defination.SecBotRB] = new Point3d(centerX - length * 0.66 + 1737.7464 + 1000, centerY - info[Defination.Sec_ht_top], 0);
            pointsinfo[Defination.SecBotLB] = new Point3d(centerX - length * 0.66, centerY - info[Defination.Sec_ht_top] - 1000 - 571.1660, 0);

            //dimensioning
            var botsec = "";
            try
            {
                botsec = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.BoxData.Z == ListCentalSuppoData[i].ListSecondrySuppo.Max(s => s.BoxData.Z)).First().BoxData.Z.ToString();
            }
            catch (Exception)
            {

            }



            CreateAlighDimen(AcadBlockTableRecord, AcadTransaction, new Point3d(centerX - length * 0.66 + 1737.7464 + 1000, centerY - info[Defination.Sec_ht_top], 0), new Point3d(centerX - length * 0.66, centerY - info[Defination.Sec_ht_top] - 1000 - 571.1660, 0), botsec, -1000);

            //hori small dimen
            var botsec2 = "";
            try
            {
                botsec2 = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.BoxData.Z == ListCentalSuppoData[i].ListSecondrySuppo.Max(s => s.BoxData.Z)).First().BoxData.Z.ToString();

                var toppart = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection == "Hor").First().EndPt;
                var anglepart = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection == "45Inclined").First().EndPt;

                sectionsize = CSECTIONSIZE(ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection == "45Inclined").First().Size);

                botsec2 = (toppart[0] - anglepart[0] - sectionsize / 2).ToString();
            }
            catch (Exception)
            {

            }


            CreateAlighDimen(AcadBlockTableRecord, AcadTransaction, new Point3d(centerX + length * 0.34, centerY - info[Defination.Prim_ht] - height, 0), new Point3d(centerX - length * 0.66 + 1737.7464 + 1000, centerY - info[Defination.Sec_ht_top], 0), botsec2, -700);

            //inside lines
            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length * 0.66, centerY - info[Defination.Sec_ht_top] - 1000 - 115, 0), new Point3d(centerX - length * 0.66 + 1737.7464 + 200, centerY - info[Defination.Sec_ht_top], 0), MyCol.Yellow, "Dashed");

            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length * 0.66 + 1737.7464 + 1000 - 200, centerY - info[Defination.Sec_ht_top], 0), new Point3d(centerX - length * 0.66, centerY - info[Defination.Sec_ht_top] - 1000 - 571.1660 + 115, 0), MyCol.Yellow, "Dashed");


            try
            {
                sectionsize = Convert.ToDouble(CSECTIONSIZE(ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection.ToLower() == "ver").First().Size));
            }
            catch (Exception)
            {

            }
            //bot secondary support
            height = 4000;
            length = 1000;
            info[Defination.Sec_ht_bot] = height + 500;
            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(pointsinfo[Defination.SecTopLT].X - length, pointsinfo[Defination.SecTopLT].Y, 0), new Point3d(pointsinfo[Defination.SecTopLT].X, pointsinfo[Defination.SecTopLT].Y, 0), new Point3d(pointsinfo[Defination.SecTopLT].X, pointsinfo[Defination.SecTopLT].Y - height, 0), new Point3d(pointsinfo[Defination.SecTopLT].X - length, pointsinfo[Defination.SecTopLT].Y - height, 0), SecThick.VBoth, "L");

            pointsinfo[Defination.SecBotLT] = new Point3d(pointsinfo[Defination.SecTopLT].X - length, pointsinfo[Defination.SecTopLT].Y, 0);
            pointsinfo[Defination.SecBotRT] = new Point3d(pointsinfo[Defination.SecTopLT].X, pointsinfo[Defination.SecTopLT].Y, 0);
            pointsinfo[Defination.SecBotRB] = new Point3d(pointsinfo[Defination.SecTopLT].X, pointsinfo[Defination.SecTopLT].Y - height, 0);
            pointsinfo[Defination.SecBotLB] = new Point3d(pointsinfo[Defination.SecTopLT].X - length, pointsinfo[Defination.SecTopLT].Y - height, 0);

            //dimensioning
            botsec = "";
            try
            {
                botsec = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection.ToLower() == "ver").First().BoxData.Z.ToString();
            }
            catch (Exception)
            {

            }


            CreateAlighDimen(AcadBlockTableRecord, AcadTransaction, pointsinfo[Defination.SecBotLT], pointsinfo[Defination.SecBotLB], botsec, 3000);

            //concret supp
            height = 1000;
            length = 3000;
            info[Defination.Concrete_l] = length;
            info[Defination.Concrete_b] = height;

            FixCreateBottomSupportTopType2(Document2D, AcadBlockTableRecord, AcadTransaction, AcadDatabase, (pointsinfo[Defination.SecBotLB].X + pointsinfo[Defination.SecBotRB].X) / 2, centerY, height, length, i);



            //support name and quantity
            CreateSupportName(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, i);



            tracex += boxlen;

        }

        [Obsolete]
        public void DRS33(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, ref double tracex, double boxlen, double boxht, ref double spaceY, Document Document2D, string SupType, [Optional] int i)
        {
            double upperYgap = 3500;

            double centerX = tracex + boxlen / 2;  // 9869.9480;
            double centerY = spaceY - upperYgap;

            //for support18 only
            centerY += 2000;
            centerX -= 1500;

            //box boundaries
            //vertical line
            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex + boxlen, spaceY + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);

            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex, spaceY - boxht + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);

            //CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Prim_block_U", centerX, centerY, radius);
            var primsup_des = ListCentalSuppoData[i].ListPrimarySuppo.OrderByDescending(e => e.Centroid.Z);
            var secsupp_des = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.SupportName.ToLower().Contains("structure")).OrderByDescending(e => e.Boundingboxmax.Z);

            string prim_height = "";
            try
            {
                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
                {
                    //height of prim from bottom

                    prim_height = Math.Round((MillimetersToMeters(primsup_des.ElementAt(1).PrimaryZhtNB)), 2).ToString();
                }

                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
                {
                    prim_height = Math.Round((MillimetersToMeters(primsup_des.ElementAt(1).Centroid.Z)), 2).ToString();
                }
            }
            catch (Exception)
            {

            }
            //prim supp left side left most
            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
            {
                FixCreatePrimarySupportwithvertex(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX - 1500, centerY, 801.5625);
                //height of prim from bottom
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - 1500 + info[Defination.Prim_Radius] + 1500, centerY - 100, 0), "CL.EL.(+)" + prim_height);

            }

            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
            {
                double radius = 300;
                info[Defination.Prim_Radius] = radius;
                info[Defination.Prim_ht] = radius;

                CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Prim_block_U", centerX - 1500, centerY, radius);
                //height of prim from bottom
                // CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] - 1500, centerY - 100, 0), "CL.EL.(+)" + info[Defination.Prim_ht].ToString());

                //string clampname = ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName;
                //DrawLeader(AcadBlockTableRecord, AcadTransaction, AcadDatabase, clampname, new Point3d(centerX, centerY + 1.4 * radius, 0), new Point3d(centerX - 4 * radius, centerY + 1.4 * radius, 0));
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - 1500 - info[Defination.Prim_Radius] - 2500, centerY - 100, 0), "CL.EL.(+)" + prim_height);
                LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - 1500 - info[Defination.Prim_Radius], centerY, 0), new Point3d(centerX - 1500 - info[Defination.Prim_Radius] - 3000, centerY, 0), MyCol.LightBlue);
                string clampname = ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName;
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - 1500 - info[Defination.Prim_Radius] - 2500, centerY + 300, 0), clampname);

            }
            try
            {
                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
                {
                    //height of prim from bottom

                    prim_height = Math.Round((MillimetersToMeters(primsup_des.ElementAt(0).PrimaryZhtNB)), 2).ToString();
                }

                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0 && ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
                {
                    prim_height = Math.Round((MillimetersToMeters(primsup_des.ElementAt(0).Centroid.Z)), 2).ToString();
                }
            }
            catch (Exception)
            {

            }

            //prim supp left side right most
            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
            {
                FixCreatePrimarySupportwithvertex(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY, 801.5625);
                //height of prim from bottom
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] + 1500, centerY - 100, 0), "CL.EL.(+)" + prim_height);

            }

            if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp"))
            {
                double radius = 300;
                info[Defination.Prim_Radius] = radius;
                info[Defination.Prim_ht] = radius;

                CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Prim_block_U", centerX, centerY, radius);
                //height of prim from bottom
                // CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] - 1500, centerY - 100, 0), "CL.EL.(+)" + info[Defination.Prim_ht].ToString());

                //string clampname = ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName;
                //DrawLeader(AcadBlockTableRecord, AcadTransaction, AcadDatabase, clampname, new Point3d(centerX, centerY + 1.4 * radius, 0), new Point3d(centerX - 4 * radius, centerY + 1.4 * radius, 0));
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - info[Defination.Prim_Radius] - 2500, centerY - 100, 0), "CL.EL.(+)" + prim_height);
                LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - info[Defination.Prim_Radius], centerY, 0), new Point3d(centerX - info[Defination.Prim_Radius] - 3000, centerY, 0), MyCol.LightBlue);
                string clampname = ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName;
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - info[Defination.Prim_Radius] - 2500, centerY + 300, 0), clampname);

            }



            //upper top sec supp
            double height = 1000;
            double length = 4000;
            info[Defination.Sec_top_l] = length;
            info[Defination.Sec_top_b] = height;
            //double ht_frm_cen = 1220.7383;0.33,0.66
            var sectionsize = 0.0;
            try
            {
                sectionsize = Convert.ToDouble(CSECTIONSIZE(secsupp_des.ElementAt(1).Size));
            }
            catch (Exception)
            {

            }
            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length * 0.34 - 2000, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length * 0.66, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length * 0.66, centerY - info[Defination.Prim_ht] - height, 0), new Point3d(centerX - length * 0.34 - 2000, centerY - info[Defination.Prim_ht] - height, 0), SecThick.HBoth, "l", 0, sectionsize);


            //info for future
            pointsinfo[Defination.SecTopLT] = new Point3d(centerX - length * 0.34 - 2000, centerY - info[Defination.Prim_ht], 0);
            pointsinfo[Defination.SecTopRT] = new Point3d(centerX + length * 0.66, centerY - info[Defination.Prim_ht], 0);
            pointsinfo[Defination.SecTopRB] = new Point3d(centerX + length * 0.66, centerY - info[Defination.Prim_ht] - height, 0);
            pointsinfo[Defination.SecTopLB] = new Point3d(centerX - length * 0.34 - 2000, centerY - info[Defination.Prim_ht] - height, 0);


            double l_dist_frm_centre = 0;
            double mid_dist_frm_centre = 0;
            double r_dist_frm_centre = 0;
            //dimensioning hori prim top sec
            try
            {
                var strpt1 = GetArrayAbsoluteValues(secsupp_des.ElementAt(1).StPt);
                var endpt1 = GetArrayAbsoluteValues(secsupp_des.ElementAt(1).EndPt);
                var midpt1 = Get3dAbsoluteValues(ListCentalSuppoData[i].ListPrimarySuppo[1].Centroid);
                var midpt2 = Get3dAbsoluteValues(ListCentalSuppoData[i].ListPrimarySuppo[0].Centroid);


                var strpt = FindClosestPoint(strpt1, endpt1, midpt1);
                double[] endpt;
                var dist1 = 0.0;
                var dist2 = 0.0;
                var dist3 = 0.0;
                if (strpt == strpt1)
                {
                    endpt = endpt1;
                    Point3d projectedpt1 = FindPerpendicularFoot(midpt1, strpt, endpt);
                    Point3d projectedpt2 = FindPerpendicularFoot(midpt2, strpt, endpt);

                    dist1 = Math.Round(GetDist(new Point3d(strpt), projectedpt1));
                    dist2 = Math.Round(GetDist(projectedpt1, projectedpt2));
                    dist3 = Math.Round(GetDist(projectedpt2, new Point3d(endpt)));
                }
                else
                {
                    endpt = strpt1;
                    Point3d projectedpt1 = FindPerpendicularFoot(midpt1, strpt, endpt);
                    Point3d projectedpt2 = FindPerpendicularFoot(midpt2, strpt, endpt);

                    dist1 = Math.Round(GetDist(new Point3d(endpt), projectedpt1));
                    dist2 = Math.Round(GetDist(projectedpt1, projectedpt2));
                    dist3 = Math.Round(GetDist(projectedpt2, new Point3d(strpt)));
                }

                l_dist_frm_centre = dist1;
                mid_dist_frm_centre = dist2;
                r_dist_frm_centre = dist3;
            }
            catch (Exception)
            {

            }

            //dimensioning
            CreateDimension(pointsinfo[Defination.SecTopLT], new Point3d(centerX - 1500, centerY + info[Defination.Prim_Radius], 0), l_dist_frm_centre.ToString(), 500);
            CreateDimension(new Point3d(centerX - 1500, centerY + info[Defination.Prim_Radius], 0), new Point3d(centerX, centerY + info[Defination.Prim_Radius], 0), mid_dist_frm_centre.ToString(), 500);
            CreateDimension(pointsinfo[Defination.SecTopRT], new Point3d(centerX, centerY + info[Defination.Prim_Radius], 0), r_dist_frm_centre.ToString(), 500);


            //LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + length * 0.66, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length * 0.66 + 4000, centerY - info[Defination.Prim_ht], 0), MyCol.LightBlue);


            string TotalHt = "";

            //mtext
            try
            {
                TotalHt = Math.Round(MillimetersToMeters(secsupp_des.ElementAt(0).Boundingboxmax.Z), 2).ToString();
            }
            catch (Exception)
            {
            }
            TOSLOC(AcadBlockTableRecord, AcadTransaction, AcadDatabase, pointsinfo[Defination.SecTopRT].X, pointsinfo[Defination.SecTopRT].Y, TotalHt);


            try
            {
                //TotalHt = MillimetersToMeters(Convert.ToDouble(TotalHt)).ToString();
            }
            catch (Exception)
            {

            }

            info[Defination.Sec_ht_top] = info[Defination.Prim_ht] + height;
            try
            {
                sectionsize = Convert.ToDouble(CSECTIONSIZE(secsupp_des.ElementAt(0).Size));
            }
            catch (Exception)
            {

            }
            //bot sec supp
            height = 6000;
            length = 1000;
            info[Defination.Sec_bot_l] = length;
            info[Defination.Sec_bot_b] = height;

            info[Defination.Sec_ht_bot] = height + 700;

            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(pointsinfo[Defination.SecTopRT].X - 500, pointsinfo[Defination.SecTopRT].Y + 500, 0), new Point3d(pointsinfo[Defination.SecTopRT].X - 500 + info[Defination.Sec_bot_l], pointsinfo[Defination.SecTopRT].Y + 500, 0), new Point3d(pointsinfo[Defination.SecTopRT].X - 500 + info[Defination.Sec_bot_l], pointsinfo[Defination.SecTopRT].Y + 500 - info[Defination.Sec_bot_b], 0), new Point3d(pointsinfo[Defination.SecTopRT].X - 500, pointsinfo[Defination.SecTopRT].Y + 500 - info[Defination.Sec_bot_b], 0), SecThick.VBoth, "nbl");

            //info for future
            pointsinfo[Defination.SecBotLT] = new Point3d(pointsinfo[Defination.SecTopRT].X - 500, pointsinfo[Defination.SecTopRT].Y + 500, 0);
            pointsinfo[Defination.SecBotRT] = new Point3d(pointsinfo[Defination.SecTopRT].X - 500 + info[Defination.Sec_bot_l], pointsinfo[Defination.SecTopRT].Y + 500, 0);
            pointsinfo[Defination.SecBotRB] = new Point3d(pointsinfo[Defination.SecTopRT].X - 500 + info[Defination.Sec_bot_l], pointsinfo[Defination.SecTopRT].Y + 500 - info[Defination.Sec_bot_b], 0);
            pointsinfo[Defination.SecBotLB] = new Point3d(pointsinfo[Defination.SecTopRT].X - 500, pointsinfo[Defination.SecTopRT].Y + 500 - info[Defination.Sec_bot_b], 0);
            //dimensioning
            var botsec = "";
            try
            {
                botsec = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.BoxData.Z == ListCentalSuppoData[i].ListSecondrySuppo.Max(s => s.BoxData.Z)).First().BoxData.Z.ToString();
            }
            catch (Exception)
            {

            }

            CreateAlighDimen(AcadBlockTableRecord, AcadTransaction, pointsinfo[Defination.SecBotRT], pointsinfo[Defination.SecBotRB], botsec, -1000);


            //draw upper plate 
            height = 200;
            length = 1500;
            info[Defination.Plate_l] = length;
            info[Defination.Plate_b] = height;


            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d((pointsinfo[Defination.SecBotLT].X + pointsinfo[Defination.SecBotRT].X) / 2 - info[Defination.Plate_l] / 2, pointsinfo[Defination.SecBotLT].Y + info[Defination.Plate_b], 0), new Point3d((pointsinfo[Defination.SecBotLT].X + pointsinfo[Defination.SecBotRT].X) / 2 + info[Defination.Plate_l] / 2, pointsinfo[Defination.SecBotLT].Y + info[Defination.Plate_b], 0), new Point3d((pointsinfo[Defination.SecBotLT].X + pointsinfo[Defination.SecBotRT].X) / 2 + info[Defination.Plate_l] / 2, pointsinfo[Defination.SecBotLT].Y, 0), new Point3d((pointsinfo[Defination.SecBotLT].X + pointsinfo[Defination.SecBotRT].X) / 2 - info[Defination.Plate_l] / 2, pointsinfo[Defination.SecBotLT].Y, 0), SecThick.Nothing);



            //height = 1000;
            //length = 3000;
            //info[Defination.Concrete_l] = length;
            //info[Defination.Concrete_b] = height;

            //concrete down
            CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "concrete_patel", (pointsinfo[Defination.SecBotLB].X + pointsinfo[Defination.SecBotRB].X) / 2, pointsinfo[Defination.SecBotLB].Y, 500);

            //concrete verti ht
            try
            {
                TotalHt = ListCentalSuppoData[i].ListConcreteData[0].Boundingboxmax.Z.ToString();
            }
            catch (Exception)
            {

            }
            TOSLOC(AcadBlockTableRecord, AcadTransaction, AcadDatabase, (pointsinfo[Defination.SecBotLB].X + pointsinfo[Defination.SecBotRB].X) / 2, pointsinfo[Defination.SecBotLB].Y, TotalHt, "r", "EL.(+)");
            try
            {
                TotalHt = ListCentalSuppoData[i].ListConcreteData[0].Boundingboxmin.Z.ToString();
            }
            catch (Exception)
            {

            }
            TOSLOC(AcadBlockTableRecord, AcadTransaction, AcadDatabase, (pointsinfo[Defination.SecBotLB].X + pointsinfo[Defination.SecBotRB].X) / 2, pointsinfo[Defination.SecBotLB].Y - (500 * 3.5063), TotalHt, "r", "FGL.EL.(+)");

            //concrete block dimension
            try
            {
                botsec = ListCentalSuppoData[i].ListConcreteData[0].BoxData.Z.ToString();
            }
            catch (Exception)
            {

            }

            CreateAlighDimen(AcadBlockTableRecord, AcadTransaction, new Point3d((pointsinfo[Defination.SecBotLB].X + pointsinfo[Defination.SecBotRB].X) / 2 - (3 * 500), pointsinfo[Defination.SecBotLB].Y, 0), new Point3d((pointsinfo[Defination.SecBotLB].X + pointsinfo[Defination.SecBotRB].X) / 2 - (3 * 500), pointsinfo[Defination.SecBotLB].Y - (500 * 3.5063), 0), botsec, 1000);

            //if (ListCentalSuppoData[i].ListConcreteData.Count > 0)
            //{
            //    FixCreateBottomSupportTopType2(Document2D, AcadBlockTableRecord, AcadTransaction, AcadDatabase, (pointsinfo[Defination.SecBotLT].X + pointsinfo[Defination.SecBotRT].X) / 2, centerY, height, length, i);

            //}

            //support name and quantity
            CreateSupportName(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, i);

            tracex += boxlen;
        }


        //commented arguments ref double tracex, double boxlen, double boxht, ref double spaceY, Document Document2D, string SupType
        public void GenericCase(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, [Optional] int i)
        {

            var strpt = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => (e.PartDirection == "Hor") && (e.Boundingboxmax.Z == ListCentalSuppoData[i].ListSecondrySuppo.Max(s => s.Boundingboxmax.Z))).First().StPt;

            var endpt = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => (e.PartDirection == "Hor") && (e.Boundingboxmax.Z == ListCentalSuppoData[i].ListSecondrySuppo.Max(s => s.Boundingboxmax.Z))).First().EndPt;

            var height = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => (e.PartDirection == "Hor") && (e.Boundingboxmax.Z == ListCentalSuppoData[i].ListSecondrySuppo.Max(s => s.Boundingboxmax.Z))).First().BoxData.Z;

            var length = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => (e.PartDirection == "Hor") && (e.Boundingboxmax.Z == ListCentalSuppoData[i].ListSecondrySuppo.Max(s => s.Boundingboxmax.Z))).First().BoxData.X;

            //var lengthY = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => (e.PartDirection == "Hor") && (e.Boundingboxmax.Z == ListCentalSuppoData[i].ListSecondrySuppo.Max(s => s.Boundingboxmax.Z))).First().BoxData.Y;

            //var length = Math.Max(lengthX, lengthY);

            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(strpt[0], strpt[2] + height / 2, 0), new Point3d(strpt[0] + length, strpt[2] + height / 2, 0), new Point3d(strpt[0] + length, strpt[2] - height / 2, 0), new Point3d(strpt[0] - length, strpt[2] - height / 2, 0), SecThick.HBoth, "R");


            strpt = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection == "Ver").First().StPt;

            endpt = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection == "Ver").First().EndPt;

            height = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection == "Ver").First().BoxData.Z;

            length = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection == "Ver").First().BoxData.X;

            //lengthY = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection == "Ver").First().BoxData.Y;

            //length = Math.Min(lengthX, lengthY);

            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(strpt[0], strpt[2] + height / 2, 0), new Point3d(strpt[0] + length, strpt[2] + height / 2, 0), new Point3d(strpt[0] + length, strpt[2] - height / 2, 0), new Point3d(strpt[0] - length, strpt[2] - height / 2, 0), SecThick.HBoth, "R");


        }



        //enum for support identification
        public enum SupportType
        {
            Nothing = 0,
            Support1,
            Support2,
            Support3,
            Support4,
            Support5,
            Support6,
            Support7,
            Support8,
            Support9,
            Support10,
            Support11,

            Support13,
            Support14,
            Support15,
            Support16,
            Support17,
            Support18,
            Support19,
            Support20,
            Support21,
            Support22,
            Support23,
            Support24,
            Support25,
            Support26,
            Support27,
            Support28,
            Support29,
            Support31,
            Support33,


            Elevation,
            S_Type,
            SL_Tyep,
            SR_Tyep,

        }







    }
}
