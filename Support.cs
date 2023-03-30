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

        //for collecting information
        Dictionary<Defination, double> info = new Dictionary<Defination, double>();

        //for collecting extra info
        Dictionary<string, double> extrainfo = new Dictionary<string, double>();

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

                string Text = GetTextInsidetheBBOx(AcadTransaction, AcadBlockTable, PSuppoData.Boundingboxmin, PSuppoData.Boundingboxmax);

                if (RawSupportData.Exists(x => x.Name.Equals(Text)))
                {
                    RawSupportData.Find(x => x.Name.Equals(Text)).Quantity++;

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

                SeparateAndFillSupport(ref ListSuppoData, ref RawSupportData, Text);
            }
        }

        void SeparateAndFillSupport(ref List<SupporSpecData> ListSuppoData, ref List<SupportData> RawSupportData, string Name)
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

        void GetAllSondaryPartinRange(SupporSpecData PSuppoData, ref List<SupporSpecData> ListPartsRange, ref List<string> ListProcessedDataIds, List<SupporSpecData> ListSecondarySuppoData)
        {
            foreach (SupporSpecData SSupportData in ListSecondarySuppoData)
            {
                bool IsXRange = false;
                bool IsYRange = false;
                bool IsZRange = false;

                if (SSupportData.Boundingboxmin.X <= PSuppoData.Boundingboxmin.X && PSuppoData.Boundingboxmax.X <= SSupportData.Boundingboxmax.X)
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

                if (IsXRange && IsYRange && IsZRange)
                {
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

                    // to explode block of text collect them
                    if (AcEnt.GetType() == typeof(BlockReference))
                    {
                        if (AcEnt.GetType().Name.Contains("BlockReference"))
                        {
                            SupporSpecData SuppoSpecdata = new SupporSpecData();
                            BlockReference BlockRef = AcEnt as BlockReference;

                            if (BlockRef.Rotation == 0)
                            {
                                FillBoundingBox(AcEnt, ref SuppoSpecdata);
                                SuppoSpecdata.CalculateCentroid();
                                FillDirVec(AcEnt, ref SuppoSpecdata);
                                SuppoSpecdata.CalculateDist();
                                SuppoSpecdata.CalculateVolume();
                                SuppoSpecdata.SupportName = BlockRef.Name;
                                SuppoSpecdata.SuppoId = "P" + Count.ToString();
                                // BlockRef.ScaleFactors

                                ListPSuppoData.Add(SuppoSpecdata);
                            }
                        }
                        Count++;
                    }
                }
                catch (Exception)
                {
                }
            }

            return ListPSuppoData;
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
                            double[] StPt = new double[3];
                            double[] EndPt = new double[3];
                            SuppoSpecdata.Size = Structureobj.Size;
                            SuppoSpecdata.StPt = Structureobj.StartPoint;
                            SuppoSpecdata.EndPt = Structureobj.EndPoint;
                            //  Structureobj.
                        }
                        catch (Exception)
                        {
                        }

                        SuppoSpecdata.SuppoId = "S" + Count.ToString();

                        ListSecondarySuppoData.Add(SuppoSpecdata);
                    }
                    Count++;
                }
                catch (Exception Ex)
                {
                    int indfx = 0;
                }
            }

            return ListSecondarySuppoData;
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

                        SuppoSpecdata.Centroid = GetPt3DFromPoint3D(SLD.MassProperties.Centroid);
                        FillBoundingBox(AcEnt, ref SuppoSpecdata);
                        FillDirVec(AcEnt, ref SuppoSpecdata);
                        SuppoSpecdata.Volume = SLD.MassProperties.Volume;
                        SuppoSpecdata.CalculateDist();
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
                bool HasBottom = false;
                int PrimarySuppoCnt = 0;
                int SecondrySuppoCnt = 0;

                if (Data.ListConcreteData.Count > 0)
                {
                    HasBottom = true;
                }

                PrimarySuppoCnt = Data.ListPrimarySuppo.Count;
                SecondrySuppoCnt = Data.ListSecondrySuppo.Count;

                int SupportCount = Data.ListConcreteData.Count + Data.ListPrimarySuppo.Count + Data.ListSecondrySuppo.Count;

                if (SupportCount <= 2)
                {

                }
                else
                {
                    // if (SupportCount == 5 && Data.ListConcreteData.Count == 2 && Data.ListSecondrySuppo.Count == 2)
                    //  {
                    //  string 
                    // if (AreCentroidsinLine(Data))
                    ///  {
                    CheckRotationtTogetType(Data);
                    // }
                    //  }
                    // else if (SupportCount == 3 && Data.ListConcreteData.Count == 0 && Data.ListSecondrySuppo.Count == 2)
                    //  {
                    //    CheckRotationtThreeParts(Data);
                    //  }
                }
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

            if (CSupportCount == 0 && PSupportCount == 1 && SSupportCount == 2)
            {
                CheckforTypeSupport3(ref SupportData);
            }
            else if (CSupportCount == 2 && PSupportCount == 1 && SSupportCount == 2)
            {
                if (!CheckforTypeSupport2(ref SupportData))
                {
                    CheckforTypeSupport4(ref SupportData);
                }
            }
        }

        bool CheckforTypeSupport4(ref SupportData SupportData)
        {

            if (SupportData.ListSecondrySuppo[0].Angle.ZinDegree.Equals(SupportData.ListSecondrySuppo[1].Angle.ZinDegree))
            {
                if (Math.Abs(SupportData.ListSecondrySuppo[0].Angle.XinDegree) > Math.Abs(SupportData.ListSecondrySuppo[1].Angle.XinDegree))
                {
                    if (Math.Abs(SupportData.ListSecondrySuppo[0].Angle.XinDegree).Equals(90))
                    {
                        if (Math.Round(SupportData.ListSecondrySuppo[0].Centroid.X) > Math.Round(SupportData.ListSecondrySuppo[1].Boundingboxmin.X) && SupportData.ListSecondrySuppo[0].Centroid.Z > SupportData.ListSecondrySuppo[1].Centroid.Z)
                        {
                            SupportData.ListSecondrySuppo[0].PartDirection = "Hor";
                            SupportData.ListSecondrySuppo[1].PartDirection = "Ver";
                            SupportData.SupportType = "Support4";
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
                    if (Math.Abs(SupportData.ListSecondrySuppo[1].Angle.XinDegree).Equals(90))
                    {
                        if (Math.Round(SupportData.ListSecondrySuppo[0].Centroid.X) > Math.Round(SupportData.ListSecondrySuppo[1].Boundingboxmin.X) && SupportData.ListSecondrySuppo[0].Centroid.Z < SupportData.ListSecondrySuppo[1].Centroid.Z)
                        {
                            SupportData.ListSecondrySuppo[1].PartDirection = "Hor";
                            SupportData.ListSecondrySuppo[0].PartDirection = "Ver";
                            SupportData.SupportType = "Support4";
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
                            if (Math.Round(SupportData.ListSecondrySuppo[0].StPt[0]).Equals(Math.Round(SupportData.ListSecondrySuppo[1].StPt[0])) || Math.Round(SupportData.ListSecondrySuppo[0].EndPt[0]).Equals(Math.Round(SupportData.ListSecondrySuppo[1].EndPt[0])))
                            {
                                SupportData.ListSecondrySuppo[1].PartDirection = "45Inclined";
                                SupportData.ListSecondrySuppo[0].PartDirection = "Hor";
                                SupportData.SupportType = "Support3";
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
                            if (Math.Round(SupportData.ListSecondrySuppo[0].StPt[0]).Equals(Math.Round(SupportData.ListSecondrySuppo[1].StPt[0])) || Math.Round(SupportData.ListSecondrySuppo[0].EndPt[0]).Equals(Math.Round(SupportData.ListSecondrySuppo[1].EndPt[0])))
                            {
                                SupportData.ListSecondrySuppo[0].PartDirection = "45Inclined";
                                SupportData.ListSecondrySuppo[1].PartDirection = "Hor";
                                SupportData.SupportType = "Support3";
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
                                SupportData.ListSecondrySuppo[0].PartDirection = "Hor";
                                SupportData.ListSecondrySuppo[1].PartDirection = "Ver";
                                SupportData.SupportType = "Support2";
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
                        if (Math.Abs(SupportData.ListSecondrySuppo[1].Angle.XinDegree).Equals(90))
                        {
                            if (Math.Round(SupportData.ListSecondrySuppo[0].Centroid.X) == Math.Round(SupportData.ListSecondrySuppo[1].Centroid.X) && SupportData.ListSecondrySuppo[0].Centroid.Z < SupportData.ListSecondrySuppo[1].Centroid.Z)
                            {
                                SupportData.ListSecondrySuppo[1].PartDirection = "Hor";
                                SupportData.ListSecondrySuppo[1].PartDirection = "Ver";
                                SupportData.SupportType = "Support2";
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
                CopyPasteTemplateFile("Temp1", Document2D, 0);

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

                Lsectiondetails.Add(" 75 40", "ISLC 75");
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
                Lsectiondetails.Add("400 100", "ISLC 400");


                //logic for adding block      

                //9869.9480;                  
                //67542.6980;
                //adding blocks here

                double boxlen = 17299.3016;
                double boxht = 12734.3388;

                double tracex = 619.1209;

                string suptype = ListCentalSuppoData[0].SupportType;

                //for (int j = 0; j <= 1; j++)
                //{
                for (int i = 0; i < ListCentalSuppoData.Count; i++)
                {
                    CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                }
                //}






                //CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, SupportType.Support3);
                //CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, SupportType.Support4);
                //CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, SupportType.Support5);



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

                //// Add a center mark
                //using (Circle centerMarkCircle = new Circle())
                //{
                //    centerMarkCircle.Center = circle.Center;
                //    centerMarkCircle.Radius = 0.5 * circle.Radius;

                //    using (BlockTableRecord ms = (BlockTableRecord)AcadTransaction.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(AcadDatabase), OpenMode.ForWrite))
                //    {
                //        centerMarkCircle.SetDatabaseDefaults();
                //        centerMarkCircle.ColorIndex = 1; // optional - sets color to red

                //        ObjectId centerMarkId = ms.AppendEntity(centerMarkCircle);
                //        AcadTransaction.AddNewlyCreatedDBObject(centerMarkCircle, true);

                //        // Add a leader to the center mark
                //        using (Leader centerMarkLeader = new Leader())
                //        {
                //            centerMarkLeader.AppendVertex(centerMarkCircle.Center);
                //            centerMarkLeader.AppendVertex(centerMarkCircle.Center + new Vector3d(centerMarkCircle.Radius, 0, 0));
                //            centerMarkLeader.SetDatabaseDefaults();

                //            ms.AppendEntity(centerMarkLeader);
                //            AcadTransaction.AddNewlyCreatedDBObject(centerMarkLeader, true);

                //            // Add a hook line to the leader
                //            using (Line centerMarkHookLine = new Line())
                //            {
                //                Point3d startPt = centerMarkLeader.VertexAt(1);
                //                Point3d endPt = centerMarkLeader.VertexAt(1) + new Vector3d(0, -0.5 * circle.Radius, 0);

                //                centerMarkHookLine.StartPoint = startPt;
                //                centerMarkHookLine.EndPoint = endPt;
                //                centerMarkHookLine.SetDatabaseDefaults();

                //                ms.AppendEntity(centerMarkHookLine);
                //                AcadTransaction.AddNewlyCreatedDBObject(centerMarkHookLine, true);
                //            }
                //        }

                //    }
                //}


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
                    DrawSupport2(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                }
                else
                {

                    spaceY = 71075.0829;
                    tracex = tempX + 10000 + 619.1209;//gaps
                                                      // gets the template
                                                      // GetTemplate(AcadBlockTableRecord, AcadTransaction, AcadDatabase,tracex- 619.1209);
                    CopyPasteTemplateFile("Temp1", Document2D, tracex - 619.1209);
                    tempX += 101659.6570 + 10000;
                    spaceX = tempX - 19068.9248;
                    DrawSupport2(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                }
            }

            if (SupType == SupportType.SL_Tyep.ToString())
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
                    CopyPasteTemplateFile("Temp1", Document2D, tracex - 619.1209);
                    tempX += 101659.6570 + 10000;
                    spaceX = tempX - 19068.9248;
                }
            }

            if (SupType == SupportType.SR_Tyep.ToString())
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
                    CopyPasteTemplateFile("Temp1", Document2D, tracex - 619.1209);
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
                    DrawSupport3(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                }
                else
                {

                    spaceY = 71075.0829;
                    tracex = tempX + 10000 + 619.1209;//gaps
                                                      // gets the template
                                                      // GetTemplate(AcadBlockTableRecord, AcadTransaction, AcadDatabase,tracex- 619.1209);
                    CopyPasteTemplateFile("Temp1", Document2D, tracex - 619.1209);
                    tempX += 101659.6570 + 10000;
                    spaceX = tempX - 19068.9248;
                    DrawSupport3(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
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
                    DrawSupport4(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                }
                else
                {

                    spaceY = 71075.0829;
                    tracex = tempX + 10000 + 619.1209;//gaps
                                                      // gets the template
                                                      // GetTemplate(AcadBlockTableRecord, AcadTransaction, AcadDatabase,tracex- 619.1209);
                    CopyPasteTemplateFile("Temp1", Document2D, tracex - 619.1209);
                    tempX += 101659.6570 + 10000;
                    spaceX = tempX - 19068.9248;
                    DrawSupport4(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
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
                    DrawSupport5(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                }
                else
                {

                    spaceY = 71075.0829;
                    tracex = tempX + 10000 + 619.1209;//gaps
                                                      // gets the template
                                                      // GetTemplate(AcadBlockTableRecord, AcadTransaction, AcadDatabase,tracex- 619.1209);
                    CopyPasteTemplateFile("Temp1", Document2D, tracex - 619.1209);
                    tempX += 101659.6570 + 10000;
                    spaceX = tempX - 19068.9248;
                    DrawSupport5(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
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
                    DrawSupport6(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                }
                else
                {

                    spaceY = 71075.0829;
                    tracex = tempX + 10000 + 619.1209;//gaps
                                                      // gets the template
                                                      // GetTemplate(AcadBlockTableRecord, AcadTransaction, AcadDatabase,tracex- 619.1209);
                    CopyPasteTemplateFile("Temp1", Document2D, tracex - 619.1209);
                    tempX += 101659.6570 + 10000;
                    spaceX = tempX - 19068.9248;
                    DrawSupport6(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
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


            // Get the assembly that contains the code that is currently executing
            System.Reflection.Assembly currentAssembly = System.Reflection.Assembly.GetExecutingAssembly();

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

            dim.DimensionText = Math.Max(ListCentalSuppoData[i].ListConcreteData[0].BoxData.Z, ListCentalSuppoData[i].ListConcreteData[1].BoxData.Z).ToString();

            dim.Dimclre = Color.FromColor(System.Drawing.Color.Cyan);
            dim.Dimclrt = Color.FromColor(System.Drawing.Color.Yellow);
            dim.Dimclrd = Color.FromColor(System.Drawing.Color.Cyan);

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

            string sLineTypName = "CENTERX2";
            if (acLineTypTbl.Has(sLineTypName) == false)
            {
                acadDatabase.LoadLineTypeFile(sLineTypName, "acad.lin");
                newline.Linetype = sLineTypName;

            }
            acadBlockTableRecord.AppendEntity(newline);
            acadTransaction.AddNewlyCreatedDBObject(newline, true);

            info[Defination.Prim_ht] = ht_frm_cen;

            //detail line
            Point3d dpt1 = new Point3d(centerX + length / 2, centerY - ht_frm_cen - height, 0);
            Point3d dPt2 = new Point3d(centerX + length / 2 + 4000, centerY - ht_frm_cen - height, 0);
            Line dline = new Line(dpt1, dPt2);
            acadBlockTableRecord.AppendEntity(dline);
            dline.Color = Color.FromColorIndex(ColorMethod.ByAci, 171);
            acadTransaction.AddNewlyCreatedDBObject(dline, true);

            //mtext
            CreateMtextfunc(acadBlockTableRecord, acadTransaction, acadDatabase, new Point3d(centerX + length / 2 + 2500, centerY - ht_frm_cen - height + 300, 0), "HPP.(+)100.000");


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

            if (ListCentalSuppoData[0].ListConcreteData[0].BoxData.Z > ListCentalSuppoData[0].ListConcreteData[1].BoxData.Z)
            {
                TotalHt = (ListCentalSuppoData[0].ListConcreteData[0].BoxData.Z + ListCentalSuppoData[0].ListSecondrySuppo[0].BoxData.Z + ListCentalSuppoData[0].ListSecondrySuppo[1].BoxData.Z).ToString();
            }
            else
            {
                TotalHt = (ListCentalSuppoData[0].ListConcreteData[1].BoxData.Z + ListCentalSuppoData[0].ListSecondrySuppo[0].BoxData.Z + ListCentalSuppoData[0].ListSecondrySuppo[1].BoxData.Z).ToString();
            }
            //mtext
            CreateMtextfunc(acadBlockTableRecord, acadTransaction, acadDatabase, new Point3d(centerX + length / 2 + 1200, centerY - ht_frm_cen + 300, 0), "TOS EL.(+)100." + TotalHt /*info[Defination.Sec_ht].ToString()*/);

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

        public void CreateDimension(Point3d strpt, Point3d endpt, [Optional] string topsec)
        {


            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            //dimension line point

            // Create the dimension
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForWrite);
                RotatedDimension dim = new RotatedDimension(0, strpt, endpt, new Point3d(endpt.X, endpt.Y + 1800, 0), "", ObjectId.Null);

                dim.Dimtxt = 100;
                dim.Dimasz = 150;

                dim.DimensionText = topsec;
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
        private void FixCreatePrimarySupportwithvertex(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, double centerX, double centerY, double radius)
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

            //detail line
            Point3d dpt1 = new Point3d(centerX + radius, centerY, 0);
            Point3d dPt2 = new Point3d(centerX + radius + 4000, centerY, 0);
            Line dline = new Line(dpt1, dPt2);
            AcadBlockTableRecord.AppendEntity(dline);
            dline.Color = Color.FromColorIndex(ColorMethod.ByAci, 171);
            AcadTransaction.AddNewlyCreatedDBObject(dline, true);

            //mtext
            CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + radius + 1500, centerY + 300, 0), ListCentalSuppoData[0].ListPrimarySuppo[0].SupportName);

            CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + radius + 1500, centerY - 100, 0), "CL.EL.(+)100." + info[Defination.Prim_ht].ToString());


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


        public void CreateMtextfunc(BlockTableRecord acadBlockTableRecord, Transaction acadTransaction, Database acadDatabase, Point3d location, string text, double textheight = 200)
        {

            // Create a new MText object with some text
            MText mtext = new MText();

            mtext.Contents = text;

            // Set the position of the MText object
            mtext.Location = location;
            mtext.TextHeight = textheight;

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

            CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + radius + 1500, centerY - 100, 0), "CL.EL.(+)100." + info[Defination.Prim_ht].ToString());

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

            string workingDirectory = Directory.GetCurrentDirectory();

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

            string workingDirectory = System.IO.Directory.GetCurrentDirectory();

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
        public void LineDraw(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction,Database acadDatabase, Point3d startpt, Point3d endpt, MyCol color, [Optional] string Linetype)
        {
            Point3d cpt1 = startpt;//new Point3d(centerX, centerY + radius + 250, 0);
            Point3d cPt2 = endpt;// new Point3d(centerX, centerY - radius - 250, 0);
            Line cline = new Line(cpt1, cPt2);

            
            try
            {
                LinetypeTable acLineTypTbl;
                acLineTypTbl = AcadTransaction.GetObject(acadDatabase.LinetypeTableId,
                                                       OpenMode.ForRead) as LinetypeTable;

                if (acLineTypTbl.Has(Linetype) == false)
                {
                    acadDatabase.LoadLineTypeFile(Linetype, "acad.lin");
                    cline.Linetype = Linetype;

                }
                cline.Linetype = Linetype;
            }
            catch (Exception e)
            {

            }
            if (color == null)
            {
                color = MyCol.LightBlue;
            }
            cline.Color = Color.FromColor(color);
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
                    LineDraw(acadBlockTableRecord, AcadTransaction,acadDatabase, new Point3d(centerX - length / 2 + thickness, centerY - ht_frm_cen, 0), new Point3d(centerX - length / 2 + thickness, centerY - ht_frm_cen - height, 0), MyCol.LightBlue);
                    LineDraw(acadBlockTableRecord, AcadTransaction,acadDatabase, new Point3d(centerX + length / 2 - thickness, centerY - ht_frm_cen, 0), new Point3d(centerX + length / 2 - thickness, centerY - ht_frm_cen - height, 0), MyCol.LightBlue);
                    break;
                case SecThick.HHidBoth:
                    // code block
                    LineDraw(acadBlockTableRecord, AcadTransaction,acadDatabase, new Point3d(centerX - length / 2 + thickness, centerY - ht_frm_cen, 0), new Point3d(centerX - length / 2 + thickness, centerY - ht_frm_cen - height, 0), MyCol.Yellow, "DASHED");
                    LineDraw(acadBlockTableRecord, AcadTransaction,acadDatabase, new Point3d(centerX + length / 2 - thickness, centerY - ht_frm_cen, 0), new Point3d(centerX + length / 2 - thickness, centerY - ht_frm_cen - height, 0), MyCol.Yellow, "DASHED");
                    break;
                case SecThick.Left:
                    // code block
                    LineDraw(acadBlockTableRecord, AcadTransaction,acadDatabase, new Point3d(centerX - length / 2 + thickness, centerY - ht_frm_cen, 0), new Point3d(centerX - length / 2 + thickness, centerY - ht_frm_cen - height, 0), MyCol.LightBlue);
                    break;
                case SecThick.HidLeft:
                    // code block
                    LineDraw(acadBlockTableRecord, AcadTransaction,acadDatabase, new Point3d(centerX - length / 2 + thickness, centerY - ht_frm_cen, 0), new Point3d(centerX - length / 2 + thickness, centerY - ht_frm_cen - height, 0), MyCol.Yellow, "DASHED");
                    break;
                case SecThick.Right:
                    // code block
                    LineDraw(acadBlockTableRecord, AcadTransaction,acadDatabase, new Point3d(centerX + length / 2 - thickness, centerY - ht_frm_cen, 0), new Point3d(centerX + length / 2 - thickness, centerY - ht_frm_cen - height, 0), MyCol.LightBlue);
                    break;
                case SecThick.HidRight:
                    // code block
                    LineDraw(acadBlockTableRecord, AcadTransaction,acadDatabase, new Point3d(centerX + length / 2 - thickness, centerY - ht_frm_cen, 0), new Point3d(centerX + length / 2 - thickness, centerY - ht_frm_cen - height, 0), MyCol.Yellow, "DASHED");
                    break;



                case SecThick.VBoth:
                    // code block
                    LineDraw(acadBlockTableRecord, AcadTransaction,acadDatabase, new Point3d(centerX - length / 2, centerY - ht_frm_cen - thickness, 0), new Point3d(centerX - length / 2 + thickness, centerY - ht_frm_cen - height, 0), MyCol.LightBlue);
                    LineDraw(acadBlockTableRecord, AcadTransaction,acadDatabase, new Point3d(centerX + length / 2 - thickness, centerY - ht_frm_cen, 0), new Point3d(centerX + length / 2 - thickness, centerY - ht_frm_cen - height, 0), MyCol.LightBlue);
                    break;
                case SecThick.VHidBoth:
                    // code block
                    LineDraw(acadBlockTableRecord, AcadTransaction,acadDatabase, new Point3d(centerX - length / 2 + thickness, centerY - ht_frm_cen, 0), new Point3d(centerX - length / 2 + thickness, centerY - ht_frm_cen - height, 0), MyCol.Yellow, "DASHED");
                    LineDraw(acadBlockTableRecord, AcadTransaction,acadDatabase, new Point3d(centerX + length / 2 - thickness, centerY - ht_frm_cen, 0), new Point3d(centerX + length / 2 - thickness, centerY - ht_frm_cen - height, 0), MyCol.Yellow, "DASHED");
                    break;
                case SecThick.Top:
                    // code block
                    LineDraw(acadBlockTableRecord, AcadTransaction,acadDatabase, new Point3d(centerX - length / 2 + thickness, centerY - ht_frm_cen, 0), new Point3d(centerX - length / 2 + thickness, centerY - ht_frm_cen - height, 0), MyCol.LightBlue);
                    break;
                case SecThick.HidTop:
                    // code block
                    LineDraw(acadBlockTableRecord, AcadTransaction,acadDatabase, new Point3d(centerX - length / 2 + thickness, centerY - ht_frm_cen, 0), new Point3d(centerX - length / 2 + thickness, centerY - ht_frm_cen - height, 0), MyCol.Yellow, "DASHED");
                    break;
                case SecThick.Bottom:
                    // code block
                    LineDraw(acadBlockTableRecord, AcadTransaction,acadDatabase, new Point3d(centerX + length / 2 - thickness, centerY - ht_frm_cen, 0), new Point3d(centerX + length / 2 - thickness, centerY - ht_frm_cen - height, 0), MyCol.LightBlue);
                    break;
                case SecThick.HidBottom:
                    // code block
                    LineDraw(acadBlockTableRecord, AcadTransaction,acadDatabase, new Point3d(centerX + length / 2 - thickness, centerY - ht_frm_cen, 0), new Point3d(centerX + length / 2 - thickness, centerY - ht_frm_cen - height, 0), MyCol.Yellow, "DASHED");
                    break;


            }

        }

        //generic secondary support(for both top bottom)
        private void BoxGenCreateSecondarySupportBottom(BlockTableRecord acadBlockTableRecord, Transaction AcadTransaction, Database acadDatabase, Point3d lefttop, Point3d righttop, Point3d rightbot, Point3d leftbot, SecThick secthik, double thickness = 100)
        {

            LineDraw(acadBlockTableRecord, AcadTransaction,acadDatabase, lefttop, righttop, MyCol.PaleTurquoise);
            LineDraw(acadBlockTableRecord, AcadTransaction,acadDatabase, righttop, rightbot, MyCol.PaleTurquoise);
            LineDraw(acadBlockTableRecord, AcadTransaction,acadDatabase, rightbot, leftbot, MyCol.PaleTurquoise);
            LineDraw(acadBlockTableRecord, AcadTransaction,acadDatabase, leftbot, lefttop, MyCol.PaleTurquoise);


            thickness = 100;
            //if(secthik==SecThick.Both)
            //{
            //    LineDraw(acadBlockTableRecord, AcadTransaction,acadDatabase, new Point3d(centerX - length / 2 + thickness, centerY - ht_frm_cen, 0), new Point3d(centerX - length / 2 + thickness, centerY - ht_frm_cen - height, 0), MyCol.LightBlue);
            //    LineDraw(acadBlockTableRecord, AcadTransaction,acadDatabase, new Point3d(centerX + length / 2 - thickness, centerY - ht_frm_cen, 0), new Point3d(centerX + length / 2 - thickness, centerY - ht_frm_cen - height, 0), MyCol.LightBlue);
            //}

            switch (secthik)
            {
                case SecThick.VBoth:
                    // code block
                    LineDraw(acadBlockTableRecord, AcadTransaction,acadDatabase, new Point3d(lefttop.X + thickness, lefttop.Y, 0), new Point3d(leftbot.X + thickness, leftbot.Y, 0), MyCol.LightBlue);
                    LineDraw(acadBlockTableRecord, AcadTransaction,acadDatabase, new Point3d(righttop.X - thickness, righttop.Y, 0), new Point3d(rightbot.X - thickness, rightbot.Y, 0), MyCol.LightBlue);
                    break;
                case SecThick.VHidBoth:
                    // code block
                    LineDraw(acadBlockTableRecord, AcadTransaction,acadDatabase, new Point3d(lefttop.X + thickness, lefttop.Y, 0), new Point3d(leftbot.X + thickness, leftbot.Y, 0), MyCol.LightBlue, "DASHED");
                    LineDraw(acadBlockTableRecord, AcadTransaction,acadDatabase, new Point3d(righttop.X - thickness, righttop.Y, 0), new Point3d(rightbot.X - thickness, rightbot.Y, 0), MyCol.LightBlue, "DASHED");
                    break;
                case SecThick.Left:
                    // code block
                    LineDraw(acadBlockTableRecord, AcadTransaction,acadDatabase, new Point3d(lefttop.X + thickness, lefttop.Y, 0), new Point3d(leftbot.X + thickness, leftbot.Y, 0), MyCol.LightBlue);
                    break;
                case SecThick.HidLeft:
                    // code block
                    LineDraw(acadBlockTableRecord, AcadTransaction,acadDatabase, new Point3d(lefttop.X + thickness, lefttop.Y, 0), new Point3d(leftbot.X + thickness, leftbot.Y, 0), MyCol.LightBlue, "DASHED");
                    break;
                case SecThick.Right:
                    // code block
                    LineDraw(acadBlockTableRecord, AcadTransaction,acadDatabase, new Point3d(righttop.X - thickness, righttop.Y, 0), new Point3d(rightbot.X - thickness, rightbot.Y, 0), MyCol.LightBlue);
                    break;
                case SecThick.HidRight:
                    // code block
                    LineDraw(acadBlockTableRecord, AcadTransaction,acadDatabase, new Point3d(righttop.X - thickness, righttop.Y, 0), new Point3d(rightbot.X - thickness, rightbot.Y, 0), MyCol.LightBlue, "DASHED");
                    break;



                case SecThick.HBoth:
                    // code block
                    LineDraw(acadBlockTableRecord, AcadTransaction,acadDatabase, new Point3d(lefttop.X, lefttop.Y - thickness, 0), new Point3d(righttop.X, righttop.Y - thickness, 0), MyCol.LightBlue);
                    LineDraw(acadBlockTableRecord, AcadTransaction,acadDatabase, new Point3d(leftbot.X, leftbot.Y + thickness, 0), new Point3d(rightbot.X, rightbot.Y + thickness, 0), MyCol.LightBlue);
                    break;
                case SecThick.HHidBoth:
                    // code block
                    LineDraw(acadBlockTableRecord, AcadTransaction,acadDatabase, new Point3d(lefttop.X, lefttop.Y - thickness, 0), new Point3d(righttop.X, righttop.Y - thickness, 0), MyCol.LightBlue, "DASHED");
                    LineDraw(acadBlockTableRecord, AcadTransaction,acadDatabase, new Point3d(leftbot.X, leftbot.Y + thickness, 0), new Point3d(rightbot.X, rightbot.Y + thickness, 0), MyCol.LightBlue, "DASHED");
                    break;
                case SecThick.Top:
                    // code block
                    LineDraw(acadBlockTableRecord, AcadTransaction,acadDatabase, new Point3d(lefttop.X, lefttop.Y - thickness, 0), new Point3d(righttop.X, righttop.Y - thickness, 0), MyCol.LightBlue);
                    break;
                case SecThick.HidTop:
                    // code block
                    LineDraw(acadBlockTableRecord, AcadTransaction,acadDatabase, new Point3d(lefttop.X, lefttop.Y - thickness, 0), new Point3d(righttop.X, righttop.Y - thickness, 0), MyCol.LightBlue, "DASHED");
                    break;
                case SecThick.Bottom:
                    // code block
                    LineDraw(acadBlockTableRecord, AcadTransaction,acadDatabase, new Point3d(leftbot.X, leftbot.Y + thickness, 0), new Point3d(rightbot.X, rightbot.Y + thickness, 0), MyCol.LightBlue);
                    break;
                case SecThick.HidBottom:
                    // code block
                    LineDraw(acadBlockTableRecord, AcadTransaction,acadDatabase, new Point3d(leftbot.X, leftbot.Y + thickness, 0), new Point3d(rightbot.X, rightbot.Y + thickness, 0), MyCol.LightBlue, "DASHED");
                    break;


            }

        }

        //aligh dimensioning
        public void CreateAlighDimen(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Point3d startpt, Point3d endpt, [Optional] string dimtxt)
        {

            Line parallelline = CreateParallelLine(startpt, endpt, -2500);
            //dimensioning
            AlignedDimension align = new AlignedDimension(startpt, endpt, parallelline.EndPoint, "", ObjectId.Null);

            align.Dimtxt = 100;
            align.Dimasz = 150;
            align.DimensionText = dimtxt;
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

        //support details
        [Obsolete]
        public void DrawSupport2(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, ref double tracex, double boxlen, double boxht, ref double spaceY, Document Document2D, string SupType, [Optional] int i)
        {

            double upperYgap = 3500;
            double centerX = tracex + boxlen / 2;  // 9869.9480;
            double centerY = spaceY - upperYgap;
            //box boundaries
            //vertical line
            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex + boxlen, spaceY + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);

            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex, spaceY - boxht + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);


            FixCreatePrimarySupportwithvertex(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY, 801.5625);

            double height = 1000;
            double length = 3000;


            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht] - height, 0), new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht] - height, 0), SecThick.HBoth);

            //dimensioning

            var ldist = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.PartDirection == "Hor");

            CreateDimension(new Point3d(centerX - length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX, centerY, 0));
            CreateDimension(new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX, centerY, 0));


            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + length / 2, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length / 2 + 4000, centerY - info[Defination.Prim_ht], 0), MyCol.LightBlue);
            string TotalHt;

            //mtext

            if (ListCentalSuppoData[i].ListConcreteData[0].BoxData.Z > ListCentalSuppoData[i].ListConcreteData[1].BoxData.Z)
            {
                TotalHt = Math.Round((ListCentalSuppoData[i].ListConcreteData[0].BoxData.Z + ListCentalSuppoData[i].ListSecondrySuppo[0].BoxData.Z + ListCentalSuppoData[i].ListSecondrySuppo[1].BoxData.Z), 2).ToString();
            }
            else
            {
                TotalHt = Math.Round((ListCentalSuppoData[i].ListConcreteData[1].BoxData.Z + ListCentalSuppoData[i].ListSecondrySuppo[0].BoxData.Z + ListCentalSuppoData[i].ListSecondrySuppo[1].BoxData.Z), 2).ToString();
            }

            CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + 2500, centerY - info[Defination.Prim_ht] + 300, 0), "TOS EL.(+)100." + TotalHt);

            info[Defination.Sec_ht_top] = info[Defination.Prim_ht] + height;

            height = 3000;
            length = 1000;

            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length / 2, centerY - info[Defination.Sec_ht_top], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top], 0), new Point3d(centerX + length / 2, centerY - info[Defination.Sec_ht_top] - height, 0), new Point3d(centerX - length / 2, centerY - info[Defination.Sec_ht_top] - height, 0), SecThick.VBoth);

            //dimensioning
            var botsec = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.BoxData.Z == ListCentalSuppoData[i].ListSecondrySuppo.Max(s => s.BoxData.Z)).First().BoxData.Z.ToString();

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

        public void DrawSupport3(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, ref double tracex, double boxlen, double boxht, ref double spaceY, Document Document2D, string SupType, [Optional] int i)
        {
            double upperYgap = 3500;

            double centerX = tracex + boxlen / 2;  // 9869.9480;
            double centerY = spaceY - upperYgap;
            //box boundaries
            //vertical line
            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex + boxlen, spaceY + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);


            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex, spaceY - boxht + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);

            FixCreatePrimarySupportwithvertex(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY, 801.5625);

            double height = 1000;
            double length = 3000;
            //double ht_frm_cen = 1220.7383;

            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length * 0.66, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length * 0.34, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length * 0.34, centerY - info[Defination.Prim_ht] - height, 0), new Point3d(centerX - length * 0.66, centerY - info[Defination.Prim_ht] - height, 0), SecThick.HBoth);

            //dimensioning
            CreateDimension(new Point3d(centerX - length * 0.66, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX, centerY, 0));
            CreateDimension(new Point3d(centerX + length * 0.34, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX, centerY, 0));

            ////mtext
            //string TotalHt = "";

            //if (ListCentalSuppoData[i].ListConcreteData[0].BoxData.Z > ListCentalSuppoData[i].ListConcreteData[1].BoxData.Z)
            //{
            //    TotalHt = Math.Round((ListCentalSuppoData[i].ListConcreteData[0].BoxData.Z + ListCentalSuppoData[i].ListSecondrySuppo[0].BoxData.Z + ListCentalSuppoData[i].ListSecondrySuppo[1].BoxData.Z), 2).ToString();
            //}
            //else
            //{
            //    TotalHt =Math.Round( (ListCentalSuppoData[i].ListConcreteData[1].BoxData.Z + ListCentalSuppoData[i].ListSecondrySuppo[0].BoxData.Z + ListCentalSuppoData[i].ListSecondrySuppo[1].BoxData.Z),2).ToString();
            //}

            //CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + 2500, centerY - info[Defination.Prim_ht] + 300, 0), "TOS EL.(+)100." + TotalHt);


            info[Defination.Sec_ht_top] = info[Defination.Prim_ht] + height;

            height = 1000;
            length = 3000;

            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length * 0.66, centerY - info[Defination.Sec_ht_top] - 1000, 0), new Point3d(centerX - length * 0.66 + 1737.7464, centerY - info[Defination.Sec_ht_top], 0), new Point3d(centerX - length * 0.66 + 1737.7464 + 1000, centerY - info[Defination.Sec_ht_top], 0), new Point3d(centerX - length * 0.66, centerY - info[Defination.Sec_ht_top] - 1000 - 571.1660, 0), SecThick.Nothing);

            //dimensioning
            var botsec = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.BoxData.Z == ListCentalSuppoData[i].ListSecondrySuppo.Max(s => s.BoxData.Z)).First().BoxData.Z.ToString();

            CreateAlighDimen(AcadBlockTableRecord, AcadTransaction, new Point3d(centerX - length * 0.66 + 1737.7464 + 1000, centerY - info[Defination.Sec_ht_top], 0), new Point3d(centerX - length * 0.66, centerY - info[Defination.Sec_ht_top] - 1000 - 571.1660, 0), botsec);

            //hori small dimen
            var botsec2 = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.BoxData.Z == ListCentalSuppoData[i].ListSecondrySuppo.Max(s => s.BoxData.Z)).First().BoxData.Z.ToString();

            CreateAlighDimen(AcadBlockTableRecord, AcadTransaction, new Point3d(centerX + length * 0.34, centerY - info[Defination.Prim_ht] - height, 0), new Point3d(centerX - length * 0.66 + 1737.7464 + 1000, centerY - info[Defination.Sec_ht_top], 0), botsec2);

            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length * 0.66, centerY - info[Defination.Sec_ht_top] - 1000 - 115, 0), new Point3d(centerX - length * 0.66 + 1737.7464 + 200, centerY - info[Defination.Sec_ht_top], 0), MyCol.Yellow, "Dashed");

            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length * 0.66 + 1737.7464 + 1000 - 200, centerY - info[Defination.Sec_ht_top], 0), new Point3d(centerX - length * 0.66, centerY - info[Defination.Sec_ht_top] - 1000 - 571.1660 + 115, 0), MyCol.Yellow, "Dashed");

            //support name and quantity
            CreateSupportName(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, i);

            tracex += boxlen;

        }

        [Obsolete]
        public void DrawSupport4(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, ref double tracex, double boxlen, double boxht, ref double spaceY, Document Document2D, string SupType, [Optional] int i)
        {
            double upperYgap = 3500;

            double centerX = tracex + boxlen / 2;  // 9869.9480;
            double centerY = spaceY - upperYgap;
            //box boundaries
            //vertical line
            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex + boxlen, spaceY + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);

            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex, spaceY - boxht + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);

            FixCreatePrimarySupportwithvertex(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY, 801.5625);

            double height = 1000;
            double length = 3000;
            //double ht_frm_cen = 1220.7383;

            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length * 0.66, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length * 0.34, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length * 0.34, centerY - info[Defination.Prim_ht] - height, 0), new Point3d(centerX - length * 0.66, centerY - info[Defination.Prim_ht] - height, 0), SecThick.HBoth);

            //dimensioning
            CreateDimension(new Point3d(centerX - length * 0.66, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX, centerY, 0));
            CreateDimension(new Point3d(centerX + length * 0.34, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX, centerY, 0));


            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + length * 0.34, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length * 0.34 + 4000, centerY - info[Defination.Prim_ht], 0), MyCol.LightBlue);


            string TotalHt;

            //mtext

            if (ListCentalSuppoData[i].ListConcreteData[0].BoxData.Z > ListCentalSuppoData[i].ListConcreteData[1].BoxData.Z)
            {
                TotalHt = Math.Round((ListCentalSuppoData[i].ListConcreteData[0].BoxData.Z + ListCentalSuppoData[i].ListSecondrySuppo[0].BoxData.Z + ListCentalSuppoData[i].ListSecondrySuppo[1].BoxData.Z), 2).ToString();
            }
            else
            {
                TotalHt = Math.Round((ListCentalSuppoData[i].ListConcreteData[1].BoxData.Z + ListCentalSuppoData[i].ListSecondrySuppo[0].BoxData.Z + ListCentalSuppoData[i].ListSecondrySuppo[1].BoxData.Z), 2).ToString();
            }


            CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + 2500, centerY - info[Defination.Prim_ht] + 300, 0), "TOS EL.(+)100." + TotalHt);

            info[Defination.Sec_ht_top] = info[Defination.Prim_ht] + height;

            height = 3000;
            length = 1000;
            info[Defination.Sec_ht_bot] = info[Defination.Sec_ht_top] + height;
            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - height * 0.66, centerY - info[Defination.Sec_ht_top], 0), new Point3d(centerX - height * 0.66 + length, centerY - info[Defination.Sec_ht_top], 0), new Point3d(centerX - height * 0.66 + length, centerY - info[Defination.Sec_ht_top] - height, 0), new Point3d(centerX - height * 0.66, centerY - info[Defination.Sec_ht_top] - height, 0), SecThick.VBoth);

            //dimensioning

            var botsec = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.BoxData.Z == ListCentalSuppoData[i].ListSecondrySuppo.Max(s => s.BoxData.Z)).First().BoxData.Z.ToString();

            CreateAlighDimen(AcadBlockTableRecord, AcadTransaction, new Point3d(centerX - height * 0.66 + length, centerY - info[Defination.Sec_ht_top], 0), new Point3d(centerX - height * 0.66 + length, centerY - info[Defination.Sec_ht_top] - height, 0), botsec);

            height = 1000;
            length = 3000;
            info[Defination.Concrete_l] = length;
            info[Defination.Concrete_b] = height;
            //FixCreateSecondarySupportTop(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY);
            FixCreateBottomSupportTopType2(Document2D, AcadBlockTableRecord, AcadTransaction, AcadDatabase, (centerX - length * 0.66 + centerX - length * 0.66 + height) / 2, centerY, height, length, i);

            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + length * 0.34, centerY - info[Defination.Sec_ht_bot] - length, 0), new Point3d(centerX + length * 0.34 + 4000, centerY - info[Defination.Sec_ht_bot] - length, 0), MyCol.LightBlue);

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
        public void DrawSupport5(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, ref double tracex, double boxlen, double boxht, ref double spaceY, Document Document2D, string SupType, [Optional] int i)
        {
            double upperYgap = 3500;

            double centerX = tracex + boxlen / 2;  // 9869.9480;
            double centerY = spaceY - upperYgap;
            //box boundaries
            //vertical line
            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex + boxlen, spaceY + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);

            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex, spaceY - boxht + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);

            FixCreatePrimarySupportwithvertex(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY, 801.5625);

            double height = 1000;
            double length = 3000;
            //double ht_frm_cen = 1220.7383;

            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length * 0.66 + 1000, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length * 0.34, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length * 0.34, centerY - info[Defination.Prim_ht] - height, 0), new Point3d(centerX - length * 0.66 + 1000, centerY - info[Defination.Prim_ht] - height, 0), SecThick.HBoth);

            //dimensioning
            CreateDimension(new Point3d(centerX - length * 0.66 + 1000, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX, centerY, 0));
            CreateDimension(new Point3d(centerX + length * 0.34, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX, centerY, 0));


            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + length * 0.34, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length * 0.34 + 4000, centerY - info[Defination.Prim_ht], 0), MyCol.LightBlue);

            string TotalHt;

            //mtext

            if (ListCentalSuppoData[i].ListConcreteData[0].BoxData.Z > ListCentalSuppoData[i].ListConcreteData[1].BoxData.Z)
            {
                TotalHt = Math.Round((ListCentalSuppoData[i].ListConcreteData[0].BoxData.Z + ListCentalSuppoData[i].ListSecondrySuppo[0].BoxData.Z + ListCentalSuppoData[i].ListSecondrySuppo[1].BoxData.Z), 2).ToString();
            }
            else
            {
                TotalHt = Math.Round((ListCentalSuppoData[i].ListConcreteData[1].BoxData.Z + ListCentalSuppoData[i].ListSecondrySuppo[0].BoxData.Z + ListCentalSuppoData[i].ListSecondrySuppo[1].BoxData.Z), 2).ToString();
            }


            CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + 2500, centerY - info[Defination.Prim_ht] + 300, 0), "TOS EL.(+)100." + TotalHt);

            info[Defination.Sec_ht_top] = info[Defination.Prim_ht] + height;

            height = 3000;
            length = 1000;
            info[Defination.Sec_ht_bot] = info[Defination.Sec_ht_top] + height;
            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - height * 0.66, centerY - info[Defination.Sec_ht_top] + 1000, 0), new Point3d(centerX - height * 0.66 + length, centerY - info[Defination.Sec_ht_top] + 1000, 0), new Point3d(centerX - height * 0.66 + length, centerY - info[Defination.Sec_ht_top] - height, 0), new Point3d(centerX - height * 0.66, centerY - info[Defination.Sec_ht_top] - height, 0), SecThick.VBoth);

            //dimensioning
            var botsec = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.BoxData.Z == ListCentalSuppoData[i].ListSecondrySuppo.Max(s => s.BoxData.Z)).First().BoxData.Z.ToString();

            CreateAlighDimen(AcadBlockTableRecord, AcadTransaction, new Point3d(centerX - height * 0.66 + length, centerY - info[Defination.Sec_ht_top], 0), new Point3d(centerX - height * 0.66 + length, centerY - info[Defination.Sec_ht_top] - height, 0), botsec);

            height = 1000;
            length = 3000;
            info[Defination.Concrete_l] = length;
            info[Defination.Concrete_b] = height;
            //FixCreateSecondarySupportTop(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY);
            FixCreateBottomSupportTopType2(Document2D, AcadBlockTableRecord, AcadTransaction, AcadDatabase, (centerX - length * 0.66 + centerX - length * 0.66 + height) / 2, centerY, height, length, i);


            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + length * 0.34, centerY - info[Defination.Sec_ht_bot] - length, 0), new Point3d(centerX + length * 0.34 + 4000, centerY - info[Defination.Sec_ht_bot] - length, 0), MyCol.LightBlue);

            //hori small dimen
            var botsec2 = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.BoxData.Z == ListCentalSuppoData[i].ListSecondrySuppo.Max(s => s.BoxData.Z)).First().BoxData.Z.ToString();

            CreateAlighDimen(AcadBlockTableRecord, AcadTransaction, new Point3d(centerX + length * 0.34, centerY - info[Defination.Prim_ht] - height, 0), new Point3d(centerX - length * 0.66 + 1737.7464 + 1000, centerY - info[Defination.Sec_ht_top], 0), botsec2);

            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length * 0.66, centerY - info[Defination.Sec_ht_top] - 1000 - 115, 0), new Point3d(centerX - length * 0.66 + 1737.7464 + 200, centerY - info[Defination.Sec_ht_top], 0), MyCol.Yellow, "Dashed");

            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length * 0.66 + 1737.7464 + 1000 - 200, centerY - info[Defination.Sec_ht_top], 0), new Point3d(centerX - length * 0.66, centerY - info[Defination.Sec_ht_top] - 1000 - 571.1660 + 115, 0), MyCol.Yellow, "Dashed");

            //support name and quantity
            CreateSupportName(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, i);

            tracex += boxlen;


        }

        [Obsolete]
        public void DrawSupport6(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, ref double tracex, double boxlen, double boxht, ref double spaceY, Document Document2D, string SupType, [Optional] int i)
        {
            double upperYgap = 2500;

            double centerX = tracex + boxlen / 2;  // 9869.9480;
            double centerY = spaceY - upperYgap;
            //box boundaries
            //vertical line
            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex + boxlen, spaceY + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);

            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex, spaceY - boxht + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);



            FixCreatePrimarySupportwithvertex(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX - 500, centerY, 801.5625);

            double height = 1000;
            double length = 4000;

            info[Defination.Sec_top_l] = length;
            info[Defination.Sec_top_b] = height;
            //double ht_frm_cen = 1220.7383;

            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length * 0.66 + 1000, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length * 0.34, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length * 0.34, centerY - info[Defination.Prim_ht] - height, 0), new Point3d(centerX - length * 0.66 + 1000, centerY - info[Defination.Prim_ht] - height, 0), SecThick.HBoth);

            //dimensioning
            CreateDimension(new Point3d(centerX - length * 0.66 + 1000, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX, centerY, 0));
            CreateDimension(new Point3d(centerX + length * 0.34, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX, centerY, 0));


            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + length * 0.34, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + length * 0.34 + 4000, centerY - info[Defination.Prim_ht], 0), MyCol.LightBlue);

            string TotalHt;

            //mtext

            if (ListCentalSuppoData[i].ListConcreteData[0].BoxData.Z > ListCentalSuppoData[i].ListConcreteData[1].BoxData.Z)
            {
                TotalHt = Math.Round((ListCentalSuppoData[i].ListConcreteData[0].BoxData.Z + ListCentalSuppoData[i].ListSecondrySuppo[0].BoxData.Z + ListCentalSuppoData[i].ListSecondrySuppo[1].BoxData.Z), 2).ToString();
            }
            else
            {
                TotalHt = Math.Round((ListCentalSuppoData[i].ListConcreteData[1].BoxData.Z + ListCentalSuppoData[i].ListSecondrySuppo[0].BoxData.Z + ListCentalSuppoData[i].ListSecondrySuppo[1].BoxData.Z), 2).ToString();
            }


            CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + 2500, centerY - info[Defination.Prim_ht] + 300, 0), "TOS EL.(+)100." + TotalHt);

            info[Defination.Sec_ht_top] = info[Defination.Prim_ht] + height;

            height = 6000;
            length = 1000;
            info[Defination.Sec_bot_l] = length;
            info[Defination.Sec_bot_b] = height;

            info[Defination.Sec_ht_bot] = info[Defination.Sec_ht_top] + height - 1000;

            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Sec_top_l] * 0.34, centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + info[Defination.Sec_top_l] * 0.34 + info[Defination.Sec_bot_l], centerY - info[Defination.Prim_ht], 0), new Point3d(centerX + info[Defination.Sec_top_l] * 0.34 + info[Defination.Sec_bot_l], centerY - info[Defination.Prim_ht] - info[Defination.Sec_bot_b], 0), new Point3d(centerX + info[Defination.Sec_top_l] * 0.34, centerY - info[Defination.Prim_ht] - info[Defination.Sec_bot_b], 0), SecThick.VBoth);

            //dimensioning
            var botsec = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.BoxData.Z == ListCentalSuppoData[i].ListSecondrySuppo.Max(s => s.BoxData.Z)).First().BoxData.Z.ToString();

            CreateAlighDimen(AcadBlockTableRecord, AcadTransaction, new Point3d(centerX - height * 0.66 + length, centerY - info[Defination.Sec_ht_top], 0), new Point3d(centerX - height * 0.66 + length, centerY - info[Defination.Sec_ht_top] - height, 0), botsec);




            //lower sec top supp
            height = 1000;
            length = 4000;
            extrainfo["Sec_low_b"] = height;
            extrainfo["Sec_low_l"] = length;
            extrainfo["Sec_low_l"] = length + 500;


            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Sec_top_l] * 0.34 - extrainfo["Sec_low_l"], centerY - info[Defination.Prim_ht] - info[Defination.Sec_bot_b] + extrainfo["Sec_low_b"] + 500, 0), new Point3d(centerX + info[Defination.Sec_top_l] * 0.34, centerY - info[Defination.Prim_ht] - info[Defination.Sec_bot_b] + extrainfo["Sec_low_b"] + 500, 0), new Point3d(centerX + info[Defination.Sec_top_l] * 0.34, centerY - info[Defination.Prim_ht] - info[Defination.Sec_bot_b] + 500, 0), new Point3d(centerX + info[Defination.Sec_top_l] * 0.34 - extrainfo["Sec_low_l"], centerY - info[Defination.Prim_ht] - info[Defination.Sec_bot_b] + 500, 0), SecThick.HBoth);

            //prim of lower sec top supp
            FixCreatePrimarySupportwithvertex(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX - 500, centerY - info[Defination.Prim_ht] - info[Defination.Sec_bot_b] + extrainfo["Sec_low_b"] + 500 + (1200 * 1.5), 1200);


            height = 1000;
            length = 3000;
            info[Defination.Concrete_l] = length;
            info[Defination.Concrete_b] = height;
            //FixCreateSecondarySupportTop(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY);
            FixCreateBottomSupportTopType2(Document2D, AcadBlockTableRecord, AcadTransaction, AcadDatabase, (centerX + info[Defination.Sec_top_l] * 0.34 + info[Defination.Sec_bot_l] + centerX + info[Defination.Sec_top_l] * 0.34) / 2, centerY, height, length, i);

            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + length * 0.34, centerY - info[Defination.Sec_ht_bot] - length, 0), new Point3d(centerX + length * 0.34 + 4000, centerY - info[Defination.Sec_ht_bot] - length, 0), MyCol.LightBlue);

            //hori small dimen
            var botsec2 = ListCentalSuppoData[i].ListSecondrySuppo.Where(e => e.BoxData.Z == ListCentalSuppoData[i].ListSecondrySuppo.Max(s => s.BoxData.Z)).First().BoxData.Z.ToString();

            CreateAlighDimen(AcadBlockTableRecord, AcadTransaction, new Point3d(centerX + length * 0.34, centerY - info[Defination.Prim_ht] - height, 0), new Point3d(centerX - length * 0.66 + 1737.7464 + 1000, centerY - info[Defination.Sec_ht_top], 0), botsec2);

            LineDraw(AcadBlockTableRecord, AcadTransaction,AcadDatabase, new Point3d(centerX - length * 0.66, centerY - info[Defination.Sec_ht_top] - 1000 - 115, 0), new Point3d(centerX - length * 0.66 + 1737.7464 + 200, centerY - info[Defination.Sec_ht_top], 0), MyCol.Yellow, "Dashed");

            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - length * 0.66 + 1737.7464 + 1000 - 200, centerY - info[Defination.Sec_ht_top], 0), new Point3d(centerX - length * 0.66, centerY - info[Defination.Sec_ht_top] - 1000 - 571.1660 + 115, 0), MyCol.Yellow, "Dashed");

            //support name and quantity
            CreateSupportName(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, i);

            tracex += boxlen;


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
            Concrete_b

        }

        //for support name and quantity
        public void CreateSupportName(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, ref double tracex, double boxlen, double boxht, ref double spaceY, int i)
        {
            //support name and quantity
            double centerX = tracex + boxlen / 2;

            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - 1500, spaceY - boxht + 700 + 1100, 0), new Point3d(centerX + 1500, spaceY - boxht + 700 + 1100, 0), new Point3d(centerX + 1500, spaceY - boxht + 700, 0), new Point3d(centerX - 1500, spaceY - boxht + 700, 0), SecThick.Nothing);

            CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - 1500 + 1000, spaceY - boxht + 700 + 1100 - 100, 0), ListCentalSuppoData[i].Name, 350);

            //support quant
            int quant = ListCentalSuppoData[i].Quantity;

            CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - 1500 + 800, spaceY - boxht + 700 + 1100 - 600, 0), "QTY. " + quant + " NOS.", 250);
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

            Elevation,
            S_Type,
            SL_Tyep,
            SR_Tyep,

        }

    }
}
