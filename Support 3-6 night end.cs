﻿
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
using System.Windows.Media.Media3D;

using MyCol = System.Drawing.Color;
using Autodesk.AutoCAD.BoundaryRepresentation;
using System.Numerics;
using Plane = Autodesk.AutoCAD.Geometry.Plane;
using Exception = System.Exception;
using Autodesk.AutoCAD.Runtime;
using PlantApp = Autodesk.ProcessPower.PlantInstance.PlantApplication;
using System.Threading;


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

        //dictionary for tag name and concrete size
        Dictionary<string, double> ConcreteSize_WithTAG = new Dictionary<string, double>();

        //for collecting created tags
        List<string> Created_TAG = new List<string>();

        Dictionary<string, Point3d> DicTextPos = new Dictionary<string, Point3d>();

        //dictionary for determining parts
        public Dictionary<string, string> Csectiondetails = new Dictionary<string, string>();

        public Dictionary<string, string> Lsectiondetails = new Dictionary<string, string>();


        public List<string> SupportNotCreated = new List<string>();

        List<List<SupporSpecData>> GroupsOfPlates = new List<List<SupporSpecData>>();

        Dictionary<List<SupporSpecData>, SupportData> GroupOfplateswithMainSupport = new Dictionary<List<SupporSpecData>, SupportData>();
        Dictionary<List<string>, string> Filenamewithtags = new Dictionary<List<string>, string>();

        List<string> AllSupportsTagsCivil = new List<string>();
        List<string> AllSupportsTagsGoalPost = new List<string>();
        List<string> AllSupportsTagsPlates = new List<string>();
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
            Document ADoc = null;
            Transaction ATransaction = null;
            BlockTable ABlockTable = null;
            BlockTableRecord ABlockTableRecord = null;
            Database ADatabase = null;
            Editor AEditor = null;
            //PromptSelectionResult selectionRes;

            AcadDoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            if (AcadDoc != null)
            {
                string Path = System.IO.Path.GetDirectoryName(AcadDoc.Name);
                Logger.filePath = Path + string.Concat("Log2d", DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss"), ".txt");
                Logger.GetInstance.Debug("Started Reading File");
            }

            string[] LayersName = new string[3];

            List<SupportData> RawSupportData = new List<SupportData>();

            List<SupporSpecData> ListPSuppoData = new List<SupporSpecData>();
            List<SupporSpecData> ListSecondarySuppoData = new List<SupporSpecData>();
            List<SupporSpecData> ListConcreteSupportData = new List<SupporSpecData>();
            string FilePathOpenDoc = "";
            if (AcadDoc != null)
            {
                FilePathOpenDoc = AcadDoc.Name;
                string Filepath = "";

                Project currentProject = PlantApp.CurrentProject.ProjectParts["Piping"];
                DataLinksManager dlm = currentProject.DataLinksManager;

                List<PnPProjectDrawing> dwgList = currentProject.GetPnPDrawingFiles();

                DocumentCollection documentCollection = Application.DocumentManager;

                foreach (PnPProjectDrawing dwg in dwgList)
                {
                    if (dwg.AbsoluteFileName.ToUpper().Contains("PIPING")
                        ||
                        dwg.AbsoluteFileName.ToUpper().Contains("SUPPORT"))
                    {
                        string filename = "";
                        Filepath = PlantApp.CurrentProject.ProjectFolderPath + "\\" + dwg.RelativeFileName;
                        if (System.IO.File.Exists(Filepath))
                        {
                            if (Filepath != FilePathOpenDoc)
                            {
                                AcadDoc = Application.DocumentManager.Open(Filepath, false);

                                Application.DocumentManager.MdiActiveDocument = AcadDoc;
                                AcadEditor = AcadDoc.Editor;

                                AcadDatabase = AcadDoc.Database;

                                if (AcadDatabase != null)
                                {
                                    AcadTransaction = AcadDatabase.TransactionManager.StartTransaction();

                                    if (AcadTransaction != null)
                                    {
                                        using (AcadDoc.LockDocument())
                                        {
                                            AcadBlockTable = AcadTransaction.GetObject(AcadDatabase.BlockTableId,
                                                OpenMode.ForWrite) as BlockTable;

                                            filename = AcadDoc.Name;
                                            if (AcadBlockTable != null)
                                            {
                                                AcadBlockTableRecord = AcadTransaction.GetObject(AcadBlockTable[BlockTableRecord.ModelSpace],
                                                                OpenMode.ForWrite) as BlockTableRecord;

                                                try
                                                {
                                                    LayersName = GetNamesOfSupportEnteredbyUser(AcadTransaction, AcadDatabase);
                                                }
                                                catch (Exception)
                                                {
                                                    //Logger.GetInstance.Error("Error while Getting Layer Name");
                                                }
                                                if (LayersName[0] != null && LayersName[0].Length > 0)
                                                {
                                                    try
                                                    {
                                                        GetAllPrimarySupportData(AcadEditor, AcadTransaction, LayersName[0], ref ListPSuppoData, filename);
                                                        //Logger.GetInstance.Debug("Total number of Support from Primary Layer"+ ListPSuppoData.Count.ToString());
                                                    }
                                                    catch (Exception)
                                                    {
                                                        //Logger.GetInstance.Error("Error while Reading Primary Support");
                                                        ListPSuppoData = new List<SupporSpecData>();
                                                    }
                                                }

                                                if (LayersName[1] != null && LayersName[1].Length > 0)
                                                {
                                                    try
                                                    {
                                                        GetAllSecondarySupportData(AcadEditor, AcadTransaction, LayersName[1], ref ListSecondarySuppoData, filename);

                                                        //Logger.GetInstance.Debug("Total number of Support from Secondary Layer" + ListSecondarySuppoData.Count.ToString());
                                                    }
                                                    catch (Exception)
                                                    {
                                                        //Logger.GetInstance.Error("Error while Reading Secondary Support");
                                                        ListSecondarySuppoData = new List<SupporSpecData>();
                                                    }
                                                }

                                                if (LayersName[2] != null && LayersName[2].Length > 0)
                                                {
                                                    try
                                                    {
                                                        GetAllConcreteSupportData(AcadEditor, AcadTransaction, LayersName[2], ref ListConcreteSupportData, filename);

                                                        //Logger.GetInstance.Debug("Total number of Support from Concrete Layer" + ListConcreteSupportData.Count.ToString());
                                                    }
                                                    catch (Exception)
                                                    {
                                                        //Logger.GetInstance.Error("Error while Reading Concrete Support");
                                                        ListConcreteSupportData = new List<SupporSpecData>();
                                                    }
                                                }
                                            }
                                            LayerTable acLyrTbl;
                                            acLyrTbl = AcadTransaction.GetObject(AcadDatabase.LayerTableId, OpenMode.ForRead) as LayerTable;
                                            if (acLyrTbl != null)
                                            {
                                                if (!acLyrTbl.Has("Support Tag"))
                                                {
                                                    LayerTableRecord acLyrTblRec = new LayerTableRecord();
                                                    // Assign the layer the ACI color 1 and a name
                                                    acLyrTblRec.Color = Color.FromColorIndex(ColorMethod.ByAci, 7); ;
                                                    acLyrTblRec.Name = "Support Tag";
                                                    // Upgrade the Layer table for write
                                                    acLyrTbl.UpgradeOpen();
                                                    // Append the new layer to the Layer table and the transaction
                                                    acLyrTbl.Add(acLyrTblRec);
                                                    AcadTransaction.AddNewlyCreatedDBObject(acLyrTblRec, true);
                                                }
                                                else
                                                {
                                                    Editor ed = AcadDoc.Editor;
                                                    ObjectIdCollection ents = GetEntitiesOnLayer("Support Tag", ed);
                                                    foreach (ObjectId id in ents)
                                                    {
                                                        DBObject obj = AcadTransaction.GetObject(id, OpenMode.ForWrite);
                                                        obj.Erase();
                                                    }
                                                }
                                            }

                                        }
                                    }


                                    //AcadDoc.SendStringToExecute("q_save\n", false, false, false);
                                    //LayerTable acLyrTbl;
                                    //acLyrTbl = AcadTransaction.GetObject(AcadDatabase.LayerTableId, OpenMode.ForRead) as LayerTable;
                                    //using (AcadDoc.LockDocument())
                                    //{

                                    //}
                                    //AcadDoc.sav
                                    AcadTransaction.Commit();
                                    //AcadEditor.Command("q_save");
                                    AcadEditor.WriteMessage("q_save");
                                    AcadDoc.SendStringToExecute("q_save\n", false, false, false);
                                    //AcadDoc.CloseAndDiscard();
                                    try
                                    {
                                        AcadDoc.CloseAndSave(AcadDoc.Name);
                                    }
                                    catch (Exception e)
                                    {

                                    }

                                }

                            }
                        }
                    }
                }
                try
                {
                    Application.DocumentManager.CloseAll();
                }
                catch (Exception)
                {

                }

                Thread t = new Thread(SP);
                t.Start();
                t.Join();
                AcadDoc = Application.DocumentManager.Open(FilePathOpenDoc, false);
                using (AcadDoc.LockDocument())
                {
                    AcadEditor = AcadDoc.Editor;
                    AcadDatabase = AcadDoc.Database;
                    AcadTransaction = AcadDatabase.TransactionManager.StartTransaction();
                    AcadBlockTable = AcadTransaction.GetObject(AcadDatabase.BlockTableId,
                    OpenMode.ForWrite) as BlockTable;
                    string filename = AcadDoc.Name;

                    try
                    {
                        LayersName = GetNamesOfSupportEnteredbyUser(AcadTransaction, AcadDatabase);
                    }
                    catch (Exception)
                    {
                        //Logger.GetInstance.Error("Error while Getting Layer Name");
                    }
                    if (LayersName[0] != null && LayersName[0].Length > 0)
                    {
                        try
                        {
                            GetAllPrimarySupportData(AcadEditor, AcadTransaction, LayersName[0], ref ListPSuppoData, filename);
                            //Logger.GetInstance.Debug("Total number of Support from Primary Layer"+ ListPSuppoData.Count.ToString());
                        }
                        catch (Exception)
                        {
                            //Logger.GetInstance.Error("Error while Reading Primary Support");
                            ListPSuppoData = new List<SupporSpecData>();
                        }
                    }

                    if (LayersName[1] != null && LayersName[1].Length > 0)
                    {
                        try
                        {
                            GetAllSecondarySupportData(AcadEditor, AcadTransaction, LayersName[1], ref ListSecondarySuppoData, filename);

                            //Logger.GetInstance.Debug("Total number of Support from Secondary Layer" + ListSecondarySuppoData.Count.ToString());
                        }
                        catch (Exception)
                        {
                            //Logger.GetInstance.Error("Error while Reading Secondary Support");
                            ListSecondarySuppoData = new List<SupporSpecData>();
                        }
                    }

                    if (LayersName[2] != null && LayersName[2].Length > 0)
                    {
                        try
                        {
                            GetAllConcreteSupportData(AcadEditor, AcadTransaction, LayersName[2], ref ListConcreteSupportData, filename);

                            //Logger.GetInstance.Debug("Total number of Support from Concrete Layer" + ListConcreteSupportData.Count.ToString());
                        }
                        catch (Exception)
                        {
                            //Logger.GetInstance.Error("Error while Reading Concrete Support");
                            ListConcreteSupportData = new List<SupporSpecData>();
                        }
                    }
                    DicTextPos = GetAllTexts(AcadTransaction, AcadBlockTable);

                    SaparateSupports(ref RawSupportData, ListPSuppoData, ListSecondarySuppoData, ListConcreteSupportData, AcadTransaction, AcadBlockTable);

                    ListCentalSuppoData = RawSupportData;

                    //int ConcreteSupportCount = 0;
                    //List<SupporSpecData> ConcreteSupport = new List<SupporSpecData>();
                    //List<SupporSpecData> SecondarySupport = new List<SupporSpecData>();
                    //foreach (SupportData sp in ListCentalSuppoData)
                    //{
                    //    ConcreteSupportCount = ConcreteSupportCount + sp.ListConcreteData.Count;
                    //    if (sp.ListConcreteData.Count != 0)
                    ////    {
                    //        ConcreteSupport.Add(sp.ListConcreteData.OrderBy(a => a.Centroid.Z).Last());
                    //    }
                    //    else if (sp.ListSecondrySuppo.Count != 0)
                    //    {
                    //        ConcreteSupport.Add(sp.ListSecondrySuppo.OrderBy(a => a.Centroid.Z).First());
                    //    }

                    //    //SecondarySupport.Add(sp.ListSecondrySuppo.OrderBy(a=>a.Centroid.Z).First());
                    //}
                    //ConcreteSupport = ConcreteSupport.OrderBy(a => a.Centroid.Z).ToList();
                    //if (ConcreteSupport.Count > 0)
                    //{
                    //}
                    LayerTable acLyrTbl;
                    acLyrTbl = AcadTransaction.GetObject(AcadDatabase.LayerTableId, OpenMode.ForRead) as LayerTable;
                    using (AcadDoc.LockDocument())
                    {
                        if (acLyrTbl != null)
                        {
                            if (!acLyrTbl.Has("Support Tag"))
                            {
                                LayerTableRecord acLyrTblRec = new LayerTableRecord();
                                // Assign the layer the ACI color 1 and a name
                                acLyrTblRec.Color = Color.FromColorIndex(ColorMethod.ByAci, 7); ;
                                acLyrTblRec.Name = "Support Tag";
                                // Upgrade the Layer table for write
                                acLyrTbl.UpgradeOpen();
                                // Append the new layer to the Layer table and the transaction
                                acLyrTbl.Add(acLyrTblRec);
                                AcadTransaction.AddNewlyCreatedDBObject(acLyrTblRec, true);
                            }
                            else
                            {
                                Editor ed = AcadDoc.Editor;
                                ObjectIdCollection ents = GetEntitiesOnLayer("Support Tag", ed);
                                foreach (ObjectId id in ents)
                                {
                                    DBObject obj = AcadTransaction.GetObject(id, OpenMode.ForWrite);
                                    obj.Erase();
                                }
                            }
                        }

                    }

                }
                AcadTransaction.Commit();

                List<Point3d> LocationofCentroid = new List<Point3d>();
                int count = 1;
                if (AcadDoc != null)
                {
                    FilePathOpenDoc = AcadDoc.Name;
                    Filepath = "";

                    currentProject = PlantApp.CurrentProject.ProjectParts["Piping"];
                    dlm = currentProject.DataLinksManager;

                    dwgList = currentProject.GetPnPDrawingFiles();

                    documentCollection = Application.DocumentManager;

                    foreach (PnPProjectDrawing dwg in dwgList)
                    {
                        if (dwg.AbsoluteFileName.ToUpper().Contains("PIPING")
                            ||
                            dwg.AbsoluteFileName.ToUpper().Contains("SUPPORT"))
                        {


                            Filepath = PlantApp.CurrentProject.ProjectFolderPath + "\\" + dwg.RelativeFileName;
                            if (System.IO.File.Exists(Filepath))
                            {
                                if (Filepath != FilePathOpenDoc)
                                {
                                    AcadDoc = Application.DocumentManager.Open(Filepath, false);

                                    Application.DocumentManager.MdiActiveDocument = AcadDoc;
                                    AcadEditor = AcadDoc.Editor;

                                    AcadDatabase = AcadDoc.Database;

                                    if (AcadDatabase != null)
                                    {
                                        AcadTransaction = AcadDatabase.TransactionManager.StartTransaction();

                                        if (AcadTransaction != null)
                                        {
                                            using (AcadDoc.LockDocument())
                                            {
                                                List<string> Tagsinfile = new List<string>();
                                                AcadBlockTable = AcadTransaction.GetObject(AcadDatabase.BlockTableId,
                                                    OpenMode.ForWrite) as BlockTable;

                                                if (AcadBlockTable != null)
                                                {
                                                    AcadBlockTableRecord = AcadTransaction.GetObject(AcadBlockTable[BlockTableRecord.ModelSpace],
                                                                    OpenMode.ForWrite) as BlockTableRecord;

                                                    //int ConcreteSupportCount = 0;
                                                    List<ConcreteSuppoWihMainSuppo> ConcreteSupport = new List<ConcreteSuppoWihMainSuppo>();
                                                    Dictionary<List<SupporSpecData>, SupportData> tempdic = new Dictionary<List<SupporSpecData>, SupportData>();

                                                    foreach (SupportData sp in ListCentalSuppoData)
                                                    {
                                                        if (sp.ListConcreteData.Count > 1)
                                                        {
                                                            tempdic.Add(sp.ListConcreteData, sp);
                                                        }
                                                        else
                                                        if (sp.ListConcreteData.Count != 0)
                                                        {
                                                            ConcreteSupport.Add(new ConcreteSuppoWihMainSuppo(sp.ListConcreteData.OrderBy(a => a.Centroid.Z).Last(), sp));
                                                        }
                                                        else if (sp.ListSecondrySuppo.Count != 0)
                                                        {
                                                            ConcreteSupport.Add(new ConcreteSuppoWihMainSuppo(sp.ListSecondrySuppo.OrderBy(a => a.Centroid.Z).First(), sp));
                                                            //new ConcreteSuppoWihMainSuppo(sp.ListSecondrySuppo.OrderBy(a => a.Centroid.Z).First()), sp)
                                                        }


                                                    }

                                                    //ConcreteSupport = ConcreteSupport.OrderBy(a => a.Centroid.Z).ToList();
                                                    if (ConcreteSupport.Count > 0)
                                                    {
                                                        for (int i = 0; i < ConcreteSupport.Count; i++)
                                                        {
                                                            if (ConcreteSupport[i].specData.SupportFileName.Equals(AcadDoc.Name))
                                                            {
                                                                LocationofCentroid.Add(new Point3d(ConcreteSupport[i].specData.Centroid.X, ConcreteSupport[i].specData.Centroid.Y, ConcreteSupport[i].specData.Centroid.Z));
                                                                // Open the Block table record Model space for write
                                                                BlockTableRecord acBlkTblRec;
                                                                acBlkTblRec = AcadTransaction.GetObject(AcadBlockTable[BlockTableRecord.ModelSpace],
                                                                                                OpenMode.ForWrite) as BlockTableRecord;
                                                                // Create a single-line text object
                                                                DBText acText = new DBText();
                                                                acText.SetDatabaseDefaults();
                                                                acText.Position = new Point3d(ConcreteSupport[i].specData.Centroid.X, ConcreteSupport[i].specData.Centroid.Y, ConcreteSupport[i].specData.Centroid.Z);
                                                                acText.TextString = count < 10 ? "PS-0" + (count).ToString() : "PS-" + (count).ToString();
                                                                ConcreteSupport[i].Main.Name = acText.TextString;
                                                                acBlkTblRec.AppendEntity(acText);
                                                                acText.Height = 10;
                                                                acText.Layer = "Support Tag";
                                                                AcadTransaction.AddNewlyCreatedDBObject(acText, true);
                                                                AllSupportsTagsCivil.Add(acText.TextString);
                                                                Tagsinfile.Add(acText.TextString);
                                                                count++;
                                                            }

                                                        }
                                                    }

                                                    List<DataOFGoalPostSupport> dataOFGoalPostSupports = GetDataofGoalPostSupport(tempdic);

                                                    foreach (DataOFGoalPostSupport dataofSupport in dataOFGoalPostSupports)
                                                    {
                                                        List<List<SupporSpecData>> groupofSupport = dataofSupport.AttachedSupports;
                                                        char Val = 'A';
                                                        bool increment = false;
                                                        if (groupofSupport.Count > 1)
                                                        {
                                                            foreach (List<SupporSpecData> supporSpecData in groupofSupport)
                                                            {
                                                                SupporSpecData MaxZ = supporSpecData.OrderBy(a => a.Centroid.Z).Last();
                                                                if (MaxZ.SupportFileName.Equals(AcadDoc.Name))
                                                                {

                                                                    // Open the Block table record Model space for write
                                                                    BlockTableRecord acBlkTblRec;
                                                                    acBlkTblRec = AcadTransaction.GetObject(AcadBlockTable[BlockTableRecord.ModelSpace],
                                                                                                    OpenMode.ForWrite) as BlockTableRecord;
                                                                    // Create a single-line text object
                                                                    DBText acText = new DBText();
                                                                    acText.SetDatabaseDefaults();
                                                                    acText.Position = new Point3d(MaxZ.Centroid.X, MaxZ.Centroid.Y, MaxZ.Centroid.Z);
                                                                    acText.TextString = count < 10 ? "PS-0" + (count).ToString() + Val : "PS-" + (count).ToString() + Val;
                                                                    dataofSupport.CompleteSupport.Name = acText.TextString;
                                                                    acText.Height = 10;
                                                                    acBlkTblRec.AppendEntity(acText);
                                                                    //acText.Erase();
                                                                    acText.Layer = "Support Tag";
                                                                    AcadTransaction.AddNewlyCreatedDBObject(acText, true);
                                                                    increment = true;
                                                                    AllSupportsTagsGoalPost.Add(acText.TextString);
                                                                    Tagsinfile.Add(acText.TextString);
                                                                }
                                                                Val++;
                                                            }
                                                            if (increment == true)
                                                            {
                                                                count++;

                                                            }

                                                        }
                                                        else
                                                        {
                                                            foreach (List<SupporSpecData> supporSpecData in groupofSupport)
                                                            {
                                                                SupporSpecData MaxZ = supporSpecData.OrderBy(a => a.Centroid.Z).Last();
                                                                if (MaxZ.SupportFileName.Equals(AcadDoc.Name))
                                                                {

                                                                    // Open the Block table record Model space for write
                                                                    BlockTableRecord acBlkTblRec;
                                                                    acBlkTblRec = AcadTransaction.GetObject(AcadBlockTable[BlockTableRecord.ModelSpace],
                                                                                                    OpenMode.ForWrite) as BlockTableRecord;
                                                                    // Create a single-line text object
                                                                    DBText acText = new DBText();
                                                                    acText.SetDatabaseDefaults();
                                                                    acText.Position = new Point3d(MaxZ.Centroid.X, MaxZ.Centroid.Y, MaxZ.Centroid.Z);
                                                                    acText.TextString = count < 10 ? "PS-0" + (count).ToString() : "PS-" + (count).ToString();
                                                                    dataofSupport.CompleteSupport.Name = acText.TextString;
                                                                    acText.Height = 10;
                                                                    acBlkTblRec.AppendEntity(acText);
                                                                    //acText.Erase();
                                                                    acText.Layer = "Support Tag";
                                                                    AcadTransaction.AddNewlyCreatedDBObject(acText, true);
                                                                    AllSupportsTagsCivil.Add(acText.TextString);
                                                                    Tagsinfile.Add(acText.TextString);
                                                                    count++;
                                                                }

                                                            }
                                                        }

                                                    }
                                                    foreach (KeyValuePair<List<SupporSpecData>, SupportData> plates in GroupOfplateswithMainSupport)
                                                    {
                                                        if (plates.Key.Count() > 0)
                                                        {
                                                            var plateMember = plates.Key.OrderBy(a => a.Centroid.Z).Last();
                                                            if (plateMember.SupportFileName.Equals(AcadDoc.Name))
                                                            {
                                                                // Open the Block table record Model space for write
                                                                BlockTableRecord acBlkTblRec;
                                                                acBlkTblRec = AcadTransaction.GetObject(AcadBlockTable[BlockTableRecord.ModelSpace],
                                                                                                OpenMode.ForWrite) as BlockTableRecord;
                                                                // Create a single-line text object
                                                                DBText acText = new DBText();
                                                                acText.SetDatabaseDefaults();
                                                                acText.Position = new Point3d(plateMember.Centroid.X, plateMember.Centroid.Y, plateMember.Centroid.Z);
                                                                acText.TextString = count < 10 ? "PS-0" + (count).ToString() : "PS-" + (count).ToString();
                                                                plates.Value.Name = acText.TextString;
                                                                acBlkTblRec.AppendEntity(acText);
                                                                acText.Height = 10;
                                                                acText.Layer = "Support Tag";
                                                                AcadTransaction.AddNewlyCreatedDBObject(acText, true);
                                                                AllSupportsTagsPlates.Add(acText.TextString);
                                                                Tagsinfile.Add(acText.TextString);
                                                                count++;
                                                            }
                                                        }
                                                    }
                                                    Filenamewithtags.Add(Tagsinfile, AcadDoc.Name);
                                                }
                                            }
                                        }

                                        AcadTransaction.Commit();
                                        AcadEditor.WriteMessage("q_save");
                                        AcadDoc.SendStringToExecute("q_save\n", false, false, false);

                                        try
                                        {
                                            AcadDoc.CloseAndSave(AcadDoc.Name);
                                        }
                                        catch (Exception e)
                                        {

                                        }

                                        //AcadDoc.CloseAndDiscard();
                                    }
                                }
                            }
                        }
                    }
                    try
                    {
                        Application.DocumentManager.CloseAll();

                    }
                    catch (Exception)
                    {

                    }

                    t = new Thread(SP);
                    t.Start();
                    t.Join();
                    AcadDoc = Application.DocumentManager.Open(FilePathOpenDoc, false);

                    using (AcadDoc.LockDocument())
                    {
                        List<string> Tagsinfile = new List<string>();
                        AcadEditor = AcadDoc.Editor;
                        AcadDatabase = AcadDoc.Database;
                        AcadTransaction = AcadDatabase.TransactionManager.StartTransaction();
                        AcadBlockTable = AcadTransaction.GetObject(AcadDatabase.BlockTableId,
                        OpenMode.ForWrite) as BlockTable;
                        AcadBlockTableRecord = AcadTransaction.GetObject(AcadBlockTable[BlockTableRecord.ModelSpace],
                                                                   OpenMode.ForWrite) as BlockTableRecord;

                        List<ConcreteSuppoWihMainSuppo> ConcreteSupport = new List<ConcreteSuppoWihMainSuppo>();
                        Dictionary<List<SupporSpecData>, SupportData> tempdic = new Dictionary<List<SupporSpecData>, SupportData>();

                        foreach (SupportData sp in ListCentalSuppoData)
                        {
                            if (sp.ListConcreteData.Count > 1)
                            {
                                tempdic.Add(sp.ListConcreteData, sp);
                            }
                            else
                            if (sp.ListConcreteData.Count != 0)
                            {
                                ConcreteSupport.Add(new ConcreteSuppoWihMainSuppo(sp.ListConcreteData.OrderBy(a => a.Centroid.Z).Last(), sp));
                            }
                            else if (sp.ListSecondrySuppo.Count != 0)
                            {
                                ConcreteSupport.Add(new ConcreteSuppoWihMainSuppo(sp.ListSecondrySuppo.OrderBy(a => a.Centroid.Z).First(), sp));
                                //new ConcreteSuppoWihMainSuppo(sp.ListSecondrySuppo.OrderBy(a => a.Centroid.Z).First()), sp)
                            }

                        }
                        //int count = 1;

                        //ConcreteSupport = ConcreteSupport.OrderBy(a => a.Centroid.Z).ToList();
                        if (ConcreteSupport.Count > 0)
                        {
                            for (int i = 0; i < ConcreteSupport.Count; i++)
                            {
                                if (ConcreteSupport[i].specData.SupportFileName.Equals(AcadDoc.Name))
                                {
                                    LocationofCentroid.Add(new Point3d(ConcreteSupport[i].specData.Centroid.X, ConcreteSupport[i].specData.Centroid.Y, ConcreteSupport[i].specData.Centroid.Z));
                                    // Open the Block table record Model space for write
                                    BlockTableRecord acBlkTblRec;
                                    acBlkTblRec = AcadTransaction.GetObject(AcadBlockTable[BlockTableRecord.ModelSpace],
                                                                    OpenMode.ForWrite) as BlockTableRecord;
                                    // Create a single-line text object
                                    DBText acText = new DBText();
                                    acText.SetDatabaseDefaults();
                                    acText.Position = new Point3d(ConcreteSupport[i].specData.Centroid.X, ConcreteSupport[i].specData.Centroid.Y, ConcreteSupport[i].specData.Centroid.Z);
                                    acText.TextString = count < 10 ? "PS-0" + (count).ToString() : "PS-" + (count).ToString();
                                    ConcreteSupport[i].Main.Name = acText.TextString;
                                    acBlkTblRec.AppendEntity(acText);
                                    acText.Height = 10;
                                    //acText.Erase();
                                    acText.Layer = "Support Tag";
                                    AllSupportsTagsCivil.Add(acText.TextString);
                                    AcadTransaction.AddNewlyCreatedDBObject(acText, true);
                                    Tagsinfile.Add(acText.TextString);
                                    count++;
                                }

                            }
                        }
                        List<DataOFGoalPostSupport> dataOFGoalPostSupports = GetDataofGoalPostSupport(tempdic);

                        //foreach(DataOFGoalPostSupport dataofSupport in dataOFGoalPostSupports)
                        //{
                        //    List<List<SupporSpecData>> groupofSupport = dataofSupport.AttachedSupports;
                        //    char Val = 'A';
                        //    foreach (List<SupporSpecData> supporSpecData in groupofSupport)
                        //    {
                        //        SupporSpecData MaxZ = supporSpecData.OrderBy(a => a.Centroid.Z).Last();
                        //        if (MaxZ.SupportFileName.Equals(AcadDoc.Name))
                        //        {

                        //            // Open the Block table record Model space for write
                        //            BlockTableRecord acBlkTblRec;
                        //            acBlkTblRec = AcadTransaction.GetObject(AcadBlockTable[BlockTableRecord.ModelSpace],
                        //                                            OpenMode.ForWrite) as BlockTableRecord;
                        //            // Create a single-line text object
                        //            DBText acText = new DBText();
                        //            acText.SetDatabaseDefaults();
                        //            acText.Position = new Point3d(MaxZ.Centroid.X, MaxZ.Centroid.Y, MaxZ.Centroid.Z);
                        //            acText.TextString = count < 10 ? "PS-0" + (count).ToString()+Val: "PS-" + (count).ToString()+Val;
                        //            acText.Thickness = 10;
                        //            dataofSupport.CompleteSupport.Name = acText.TextString;
                        //            acBlkTblRec.AppendEntity(acText);
                        //            //acText.Erase();
                        //            acText.Layer = "Support Tag";
                        //            AcadTransaction.AddNewlyCreatedDBObject(acText, true);

                        //        }
                        //        Val++;

                        //    }
                        //    count++;
                        //}

                        foreach (DataOFGoalPostSupport dataofSupport in dataOFGoalPostSupports)
                        {
                            List<List<SupporSpecData>> groupofSupport = dataofSupport.AttachedSupports;
                            char Val = 'A';
                            bool increment = false;
                            if (groupofSupport.Count > 1)
                            {
                                foreach (List<SupporSpecData> supporSpecData in groupofSupport)
                                {
                                    SupporSpecData MaxZ = supporSpecData.OrderBy(a => a.Centroid.Z).Last();
                                    if (MaxZ.SupportFileName.Equals(AcadDoc.Name))
                                    {

                                        // Open the Block table record Model space for write
                                        BlockTableRecord acBlkTblRec;
                                        acBlkTblRec = AcadTransaction.GetObject(AcadBlockTable[BlockTableRecord.ModelSpace],
                                                                        OpenMode.ForWrite) as BlockTableRecord;
                                        // Create a single-line text object
                                        DBText acText = new DBText();
                                        acText.SetDatabaseDefaults();
                                        acText.Position = new Point3d(MaxZ.Centroid.X, MaxZ.Centroid.Y, MaxZ.Centroid.Z);
                                        acText.TextString = count < 10 ? "PS-0" + (count).ToString() + Val : "PS-" + (count).ToString() + Val;
                                        dataofSupport.CompleteSupport.Name = acText.TextString;
                                        acText.Height = 10;
                                        acBlkTblRec.AppendEntity(acText);
                                        //acText.Erase();
                                        acText.Layer = "Support Tag";
                                        AcadTransaction.AddNewlyCreatedDBObject(acText, true);
                                        increment = true;
                                        Tagsinfile.Add(acText.TextString);
                                        AllSupportsTagsGoalPost.Add(acText.TextString);

                                    }
                                    Val++;

                                }
                                if (increment == true)
                                {
                                    count++;
                                }
                            }
                            else
                            {
                                foreach (List<SupporSpecData> supporSpecData in groupofSupport)
                                {
                                    SupporSpecData MaxZ = supporSpecData.OrderBy(a => a.Centroid.Z).Last();
                                    if (MaxZ.SupportFileName.Equals(AcadDoc.Name))
                                    {

                                        // Open the Block table record Model space for write
                                        BlockTableRecord acBlkTblRec;
                                        acBlkTblRec = AcadTransaction.GetObject(AcadBlockTable[BlockTableRecord.ModelSpace],
                                                                        OpenMode.ForWrite) as BlockTableRecord;
                                        // Create a single-line text object
                                        DBText acText = new DBText();
                                        acText.SetDatabaseDefaults();
                                        acText.Position = new Point3d(MaxZ.Centroid.X, MaxZ.Centroid.Y, MaxZ.Centroid.Z);
                                        acText.TextString = count < 10 ? "PS-0" + (count).ToString() : "PS-" + (count).ToString();
                                        dataofSupport.CompleteSupport.Name = acText.TextString;
                                        acText.Height = 10;
                                        acBlkTblRec.AppendEntity(acText);
                                        //acText.Erase();
                                        acText.Layer = "Support Tag";
                                        AcadTransaction.AddNewlyCreatedDBObject(acText, true);
                                        AllSupportsTagsCivil.Add(acText.TextString);
                                        Tagsinfile.Add(acText.TextString);
                                        count++;
                                    }


                                }
                            }


                        }
                        foreach (KeyValuePair<List<SupporSpecData>, SupportData> plates in GroupOfplateswithMainSupport)
                        {
                            if (plates.Key.Count() > 0)
                            {
                                var plateMember = plates.Key.OrderBy(a => a.Centroid.Z).Last();
                                if (plateMember.SupportFileName.Equals(AcadDoc.Name))
                                {
                                    // Open the Block table record Model space for write
                                    BlockTableRecord acBlkTblRec;
                                    acBlkTblRec = AcadTransaction.GetObject(AcadBlockTable[BlockTableRecord.ModelSpace],
                                                                    OpenMode.ForWrite) as BlockTableRecord;
                                    // Create a single-line text object
                                    DBText acText = new DBText();
                                    acText.SetDatabaseDefaults();
                                    acText.Position = new Point3d(plateMember.Centroid.X, plateMember.Centroid.Y, plateMember.Centroid.Z);
                                    acText.TextString = count < 10 ? "PS-0" + (count).ToString() : "PS-" + (count).ToString();
                                    plates.Value.Name = acText.TextString;
                                    acBlkTblRec.AppendEntity(acText);
                                    acText.Height = 10;
                                    acText.Layer = "Support Tag";
                                    AcadTransaction.AddNewlyCreatedDBObject(acText, true);
                                    AllSupportsTagsPlates.Add(acText.TextString);
                                    Tagsinfile.Add(acText.TextString);
                                    count++;
                                }
                            }
                        }
                        Filenamewithtags.Add(Tagsinfile, AcadDoc.Name);
                    }
                }

                AcadTransaction.Commit();
                //List<List<string>> Temp = new List<List<string>>();
            }
        }

        internal void GetTaggingInformation()
        {
            Logger.GetInstance.Debug("Total number of Tagged civil pedestals excluding goal post pedestals : " + AllSupportsTagsCivil.Count().ToString());
            Logger.GetInstance.Debug("Total number of Tagged civil pedestals for goalpost supports : " + AllSupportsTagsGoalPost.Count().ToString());
            Logger.GetInstance.Debug("Total number of Tagged insert plates : " + AllSupportsTagsPlates.Count().ToString());
            foreach (KeyValuePair<List<string>, string> keyval in Filenamewithtags)
            {
                if (keyval.Key.Count > 0)
                {
                    Logger.GetInstance.Debug(Path.GetFileName(keyval.Value) + " contains supports from : " + keyval.Key.FirstOrDefault().ToString() + " to s" + keyval.Key.Last().ToString());
                }
                else
                {
                    Logger.GetInstance.Debug(Path.GetFileName(keyval.Value) + " Contains no supports");
                }
            }
        }

        private List<DataOFGoalPostSupport> GetDataofGoalPostSupport(Dictionary<List<SupporSpecData>, SupportData> tempdic)
        {
            List<DataOFGoalPostSupport> GoalPostData = new List<DataOFGoalPostSupport>();
            GroupOfplateswithMainSupport = new Dictionary<List<SupporSpecData>, SupportData>();
            GroupsOfPlates = new List<List<SupporSpecData>>();
            foreach (KeyValuePair<List<SupporSpecData>, SupportData> keyval in tempdic)
            {
                List<List<SupporSpecData>> GroupofAttachedSupport = new List<List<SupporSpecData>>();
                List<SupporSpecData> ConcreteList = keyval.Key;
                var PlateSupport = ConcreteList.Where(a => a.SupportName != null && a.SupportName.Equals("PLATE"));
                if (PlateSupport.Count() != (ConcreteList.Count()))
                {
                    List<SupporSpecData> AttachedSupport = new List<SupporSpecData>();
                    List<string> tempList = new List<string>();
                    foreach (SupporSpecData concrete in ConcreteList)
                    {
                        if (concrete.ListtouchingParts.Count() != 0)
                        {
                            var attachedSupportName = concrete.ListtouchingParts.Where(a => a.Contains("C")).ToList();
                            AttachedSupport = new List<SupporSpecData>();
                            foreach (string touchingpart in attachedSupportName)
                            {
                                SupporSpecData spdata = AttachedSupport.FirstOrDefault(a => a.SuppoId.Equals(concrete.SuppoId));
                                var listcontainsupp = tempList.FirstOrDefault(a => a.Equals(concrete.SuppoId));
                                if (spdata == null && listcontainsupp == null)
                                {
                                    var touchingSuppo = ConcreteList.FirstOrDefault(a => a.SuppoId.Equals(touchingpart));
                                    AttachedSupport.Add(touchingSuppo);
                                    AttachedSupport.Add(concrete);
                                    tempList.Add(touchingSuppo.SuppoId);
                                    tempList.Add(concrete.SuppoId);
                                    GroupofAttachedSupport.Add(AttachedSupport);
                                }

                            }
                        }
                    }
                }
                else
                {
                    //GroupsOfPlates.Add(PlateSupport.ToList());
                    GroupOfplateswithMainSupport.Add(PlateSupport.ToList(), keyval.Value);

                }
                GoalPostData.Add(new DataOFGoalPostSupport(keyval.Value, GroupofAttachedSupport));
            }
            return GoalPostData;
        }

        private ObjectIdCollection GetEntitiesOnLayer(string layerName, Editor ed)
        {
            TypedValue[] tvs =

        new TypedValue[1] {

            new TypedValue(

              (int)DxfCode.LayerName,

              layerName

            )

          };

            SelectionFilter sf =

              new SelectionFilter(tvs);

            PromptSelectionResult psr =

              ed.SelectAll(sf);


            if (psr.Status == PromptStatus.OK)

                return

                  new ObjectIdCollection(

                    psr.Value.GetObjectIds()

                  );

            else

                return new ObjectIdCollection();
        }

        Dictionary<string, Point3d> GetAllTexts(Transaction AcadTransaction, BlockTable AcadBlockTable)
        {
            var AcadBlockTableRecord = AcadTransaction.GetObject(AcadBlockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

            Dictionary<string, Point3d> DicTextLoc = new Dictionary<string, Point3d>();

            foreach (var id in AcadBlockTableRecord)
            {
                if (id.ObjectClass.DxfName == "TEXT")
                {
                    try
                    {
                        MText mtext = (MText)AcadTransaction.GetObject(id, OpenMode.ForWrite);

                        Extents3d? Ext3d = mtext.Bounds;

                        DicTextLoc[mtext.Text] = Ext3d.Value.MinPoint;
                    }
                    catch (Exception)
                    {
                        try
                        {
                            DBText text = (DBText)AcadTransaction.GetObject(id, OpenMode.ForRead);
                            //text.Position
                            // text.TextString
                            DicTextLoc[text.TextString] = text.Position;
                        }
                        catch (Exception)
                        {
                        }
                    }
                }

            }
            return DicTextLoc;
        }

        public void SP()
        {
            System.Threading.Thread.Sleep(5000);
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
                    try
                    {
                        MText mtext = (MText)AcadTransaction.GetObject(id, OpenMode.ForWrite);

                        Extents3d? Ext3d = mtext.Bounds;


                        if ((Math.Round(MinPoint.X) < Math.Round(Ext3d.Value.MinPoint.X) && Math.Round(Ext3d.Value.MinPoint.X) <= Math.Round(MaxPoint.X) && Math.Round(MinPoint.Y) <= Math.Round(Ext3d.Value.MinPoint.Y) && Math.Round(Ext3d.Value.MinPoint.Y) <= Math.Round(MaxPoint.Y) && Math.Round(MinPoint.Z) <= Math.Round(Ext3d.Value.MinPoint.Z) && Math.Round(Ext3d.Value.MinPoint.Z) <= Math.Round(MaxPoint.Z)) || (Math.Round(MaxPoint.X) <= Math.Round(Ext3d.Value.MinPoint.X) && Math.Round(Ext3d.Value.MinPoint.X) <= Math.Round(MinPoint.X) && Math.Round(MaxPoint.Y) <= Math.Round(Ext3d.Value.MinPoint.Y) && Math.Round(Ext3d.Value.MinPoint.Y) <= Math.Round(MinPoint.Y) && Math.Round(MaxPoint.Z) <= Math.Round(Ext3d.Value.MinPoint.Z) && Math.Round(Ext3d.Value.MinPoint.Z) <= Math.Round(MinPoint.Z)))
                        {
                            DicTextPos[mtext.Text] = Ext3d.Value.MinPoint;
                            return mtext.Text;

                        }
                    }
                    catch
                    {
                        try
                        {
                            DBText text = (DBText)AcadTransaction.GetObject(id, OpenMode.ForRead);

                            if ((Math.Round(MinPoint.X) < Math.Round(text.Position.X) && Math.Round(text.Position.X) <= Math.Round(MaxPoint.X) && Math.Round(MinPoint.Y) <= Math.Round(text.Position.Y) && Math.Round(text.Position.Y) <= Math.Round(MaxPoint.Y) && Math.Round(MinPoint.Z) <= Math.Round(text.Position.Z) && Math.Round(text.Position.Z) <= Math.Round(MaxPoint.Z)) || (Math.Round(MaxPoint.X) <= Math.Round(text.Position.X) && Math.Round(text.Position.X) <= Math.Round(MinPoint.X) && Math.Round(MaxPoint.Y) <= Math.Round(text.Position.Y) && Math.Round(text.Position.Y) <= Math.Round(MinPoint.Y) && Math.Round(MaxPoint.Z) <= Math.Round(text.Position.Z) && Math.Round(text.Position.Z) <= Math.Round(MinPoint.Z)))
                            {
                                DicTextPos[text.TextString] = text.Position;
                                return text.TextString;
                            }
                        }
                        catch
                        {
                        }
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

                List<SupporSpecData> TempSuppoList = new List<SupporSpecData>();
                SupporSpecData TempSuppoData = new SupporSpecData();

                TempSuppoList = new List<SupporSpecData>(ListSuppoData);
                //Temporary Adding this  need to Modify This
                if (ListSuppoData != null)
                {
                    foreach (SupporSpecData Sup in TempSuppoList)
                    {
                        if (Sup.SuppoId != null && Sup.SuppoId.Contains("S") && Sup.TouchingPartid != null && Sup.TouchingPartid.Contains("P"))
                        {
                            TempSuppoData = Sup;
                            GetllTouchingParts(TempSuppoData, ref ListSuppoData, ref ListProcessedDataIds, ListAllSuppoData);
                        }
                    }

                    SeparateAndFillSupport(ref ListSuppoData, ref RawSupportData, Text, AcadTransaction, AcadBlockTable);
                }
            }

            System.Threading.Thread.Sleep(5000);
            GC.Collect();
            foreach (SupporSpecData PSuppoData in ListSecondarySuppoData)
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

                /*  GetAllTouchingSecondarySupport(PSuppoData, ref ListSuppoData, ref ListProcessedDataIds, ListSecondarySuppoData);

                  SupporSpecData TempSuppoData = new SupporSpecData();
                  //Temporary Adding this  need to Modify This
                  foreach (SupporSpecData Sup in ListSuppoData)
                  {
                      if (Sup.SuppoId.Contains("S"))
                      {
                          TempSuppoData = Sup;
                      }
                  }*/


                GetllTouchingParts(PSuppoData, ref ListSuppoData, ref ListProcessedDataIds, ListAllSuppoData);

                SeparateAndFillSupport(ref ListSuppoData, ref RawSupportData, Text, AcadTransaction, AcadBlockTable);
            }
        }

        void GetAllTouchingPart(ref List<SupporSpecData> RawSupportData)
        {
            foreach (SupporSpecData SSupportData in RawSupportData)
            {
                List<string> ListTouchingParts = new List<string>();

                if (SSupportData.SuppoId == null)
                {
                    continue;
                }

                foreach (SupporSpecData SupData in RawSupportData)
                {
                    if (SupData.SuppoId == null || SupData.SuppoId == SSupportData.SuppoId)
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

                    if (IsXRange && IsYRange && IsZRange)
                    {
                        ListTouchingParts.Add(SupData.SuppoId);
                    }
                }

                SSupportData.ListtouchingParts = ListTouchingParts;
            }
        }
        void CheckforGussetPlate(ref SupportData RawSupportData)
        {
            foreach (SupporSpecData SSupportData in RawSupportData.ListSecondrySuppo)
            {
                if (SSupportData.SupportName == null)
                {
                    continue;
                }

                if (!SSupportData.SupportName.ToUpper().Equals("PLATE"))
                {
                    continue;
                }
                foreach (SupporSpecData SupData in RawSupportData.ListConcreteData)
                {
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

                    if (IsXRange && IsYRange && IsZRange)
                    {
                        if (SupData.SuppoId != null && SupData.SuppoId.ToUpper().Contains("S") && SSupportData.SuppoId != null && SSupportData.SuppoId.ToUpper().Contains("C"))
                        {
                            if (SupData.SupportName.ToUpper().Equals("PLATE"))
                            {
                                SupData.IsGussetplate = true;
                            }
                        }
                        else if (SupData.SuppoId != null && SupData.SuppoId.ToUpper().Contains("C") && SSupportData.SuppoId != null && SSupportData.SuppoId.ToUpper().Contains("S"))
                        {
                            if (SSupportData.SupportName.ToUpper().Equals("PLATE"))
                            {
                                SSupportData.IsGussetplate = true;
                            }
                        }
                    }
                }
            }
        }
        void SeparateAndFillSupport(ref List<SupporSpecData> ListSuppoData, ref List<SupportData> RawSupportData, string Name, Transaction AcadTransaction, BlockTable AcadBlockTable)
        {
            SupportData FullSuppo = new SupportData();
            List<SupporSpecData> ListPSuppoData = new List<SupporSpecData>();
            List<SupporSpecData> ListSecondarySuppoData = new List<SupporSpecData>();
            List<SupporSpecData> ListConcreteSupportData = new List<SupporSpecData>();

            GetAllTouchingPart(ref ListSuppoData);
            foreach (SupporSpecData SupData in ListSuppoData)
            {
                if (SupData.SuppoId != null && SupData.SuppoId.Contains("P"))
                {
                    ListPSuppoData.Add(SupData);
                }
                else if (SupData.SuppoId != null && SupData.SuppoId.Contains("S"))
                {
                    ListSecondarySuppoData.Add(SupData);
                }
                else if (SupData.SuppoId != null && SupData.SuppoId.Contains("C"))
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


            CheckforGussetPlate(ref FullSuppo);
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

                    //if (SupData.SuppoId != null && SupData.SuppoId.ToUpper().Contains("S") && SSupportData.SuppoId != null && SSupportData.SuppoId.ToUpper().Contains("C"))
                    //{
                    //    if (SupData.SupportName.ToUpper().Equals("PLATE"))
                    //    {
                    //        SupData.IsGussetplate = true;
                    //    }
                    //}
                    //else if (SupData.SuppoId != null && SupData.SuppoId.ToUpper().Contains("C") && SSupportData.SuppoId != null && SSupportData.SuppoId.ToUpper().Contains("S"))
                    //{
                    //    if (SSupportData.SupportName.ToUpper().Equals("PLATE"))
                    //    {
                    //        SSupportData.IsGussetplate = true;
                    //    }
                    //}

                    if (SupData.SuppoId != null && SupData.SuppoId.ToUpper().Contains("P"))
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
            if (PSuppoData.BottomPrim == null)
            {
                return;
            }
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
                try
                {
                    ModifyBoundigBox(ref PSuppoData);
                }
                catch (Exception)
                {
                }
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

                    //if (PRSuppoData.SuppoId != null && PRSuppoData.SuppoId.ToUpper().Contains("S") && SSupportData.SuppoId != null && SSupportData.SuppoId.ToUpper().Contains("C"))
                    //{
                    //    if (PRSuppoData.SupportName.ToUpper().Equals("PLATE"))
                    //    {
                    //        PRSuppoData.IsGussetplate = true;
                    //    }
                    //}
                    //else if (PRSuppoData.SuppoId != null && PRSuppoData.SuppoId.ToUpper().Contains("C") && SSupportData.SuppoId != null && SSupportData.SuppoId.ToUpper().Contains("S"))
                    //{
                    //    if (SSupportData.SupportName.ToUpper().Equals("PLATE"))
                    //    {
                    //        SSupportData.IsGussetplate = true;
                    //    }
                    //}


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
            SuppoSpecdata.Boundingboxmin = GetPt3DFromPoint3d(Ext.Value.MinPoint);
            SuppoSpecdata.Boundingboxmax = GetPt3DFromPoint3d(Ext.Value.MaxPoint);
        }

        Pt3D GetPt3DFromPoint3d(Point3d Pt)
        {
            Pt3D LocalPoint3d = new Pt3D();
            LocalPoint3d.X = Pt.X;
            LocalPoint3d.Y = Pt.Y;
            LocalPoint3d.Z = Pt.Z;

            return LocalPoint3d;
        }

        Pt3D GetPt3DFromPoint3D(Point3D Pt)
        {
            Pt3D LocalPoint3d = new Pt3D();
            LocalPoint3d.X = Pt.X;
            LocalPoint3d.Y = Pt.Y;
            LocalPoint3d.Z = Pt.Z;

            return LocalPoint3d;
        }

        Point3d GetPoint3dFromPt3D(Pt3D Pt)
        {
            Point3d LocalPoint3d = new Point3d(Pt.X, Pt.Y, Pt.Z);
            //LocalPoint3d.X = Pt.X;
            //LocalPoint3d.Y = Pt.Y;
            //LocalPoint3d.Z = Pt.Z;

            return LocalPoint3d;
        }

        Point3D GetPoint3DFromPt3D(Pt3D Pt)
        {
            Point3D LocalPoint3d = new Point3D(Pt.X, Pt.Y, Pt.Z);
            //LocalPoint3d.X = Pt.X;
            //LocalPoint3d.Y = Pt.Y;
            //LocalPoint3d.Z = Pt.Z;

            return LocalPoint3d;
        }

        Point3d GetPoint3dFromPoint3D(Point3D Pt)
        {
            Point3d LocalPoint3d = new Point3d(Pt.X, Pt.Y, Pt.Z);
            //LocalPoint3d.X = Pt.X;
            //LocalPoint3d.Y = Pt.Y;
            //LocalPoint3d.Z = Pt.Z;

            return LocalPoint3d;
        }

        Vector3d GetVect3dFromVect3D(Vector3D Pt)
        {
            Vector3d LocalPoint3d = new Vector3d(Pt.X, Pt.Y, Pt.Z);
            //LocalPoint3d.X = Pt.X;
            //LocalPoint3d.Y = Pt.Y;
            //LocalPoint3d.Z = Pt.Z;

            return LocalPoint3d;
        }

        GeometRi.Point3d GetGeometRiPt3DFromPt3D(Pt3D Pt)
        {
            GeometRi.Point3d LocalPoint3d = new GeometRi.Point3d();
            LocalPoint3d.X = Pt.X;
            LocalPoint3d.Y = Pt.Y;
            LocalPoint3d.Z = Pt.Z;

            return LocalPoint3d;
        }

        GeometRi.Point3d GetGeometRiPt3DFromPoint3d(Point3d Pt)
        {
            GeometRi.Point3d LocalPoint3d = new GeometRi.Point3d();
            LocalPoint3d.X = Pt.X;
            LocalPoint3d.Y = Pt.Y;
            LocalPoint3d.Z = Pt.Z;

            return LocalPoint3d;
        }

        double[] GetArrayOfDoubleFromGeometRiPoint3D(GeometRi.Point3d Pt)
        {
            double[] LocalPoint3d = new double[3];
            LocalPoint3d[0] = Pt.X;
            LocalPoint3d[1] = Pt.Y;
            LocalPoint3d[2] = Pt.Z;

            return LocalPoint3d;
        }
        Point3d GetPoint3dFromGeometRiPoint3D(GeometRi.Point3d Pt)
        {
            Point3d LocalPoint3d = new Point3d(Pt.X, Pt.Y, Pt.Z);
            //LocalPoint3d.X = Pt.X;
            //LocalPoint3d.Y = Pt.Y;
            //LocalPoint3d.Z = Pt.Z;

            return LocalPoint3d;
        }

        double[] GetArrayOfDoubleFromPt3D(Pt3D Pt)
        {
            double[] LocalPoint3d = new double[3];
            LocalPoint3d[0] = Pt.X;
            LocalPoint3d[1] = Pt.Y;
            LocalPoint3d[2] = Pt.Z;

            return LocalPoint3d;
        }

        double[] GdetArrayOfDoubleFromVect3d(Vector3d Pt)
        {
            double[] LocalPoint3d = new double[3];
            LocalPoint3d[0] = Pt.X;
            LocalPoint3d[1] = Pt.Y;
            LocalPoint3d[2] = Pt.Z;

            return LocalPoint3d;
        }

        GeometRi.Vector3d GetGeometRiVect3dFromPt3D(Pt3D Pt)
        {
            GeometRi.Vector3d LocalPoint3d = new GeometRi.Vector3d();
            LocalPoint3d.X = Pt.X;
            LocalPoint3d.Y = Pt.Y;
            LocalPoint3d.Z = Pt.Z;

            return LocalPoint3d;
        }

        GeometRi.Vector3d GetGeometRiVect3dFromVector3D(Vector3D Pt)
        {
            GeometRi.Vector3d LocalPoint3d = new GeometRi.Vector3d();
            LocalPoint3d.X = Pt.X;
            LocalPoint3d.Y = Pt.Y;
            LocalPoint3d.Z = Pt.Z;

            return LocalPoint3d;
        }

        Vector3D GetVect3DFromPt3D(Pt3D Pt)
        {
            Vector3D LocalPoint3d = new Vector3D();
            LocalPoint3d.X = Pt.X;
            LocalPoint3d.Y = Pt.Y;
            LocalPoint3d.Z = Pt.Z;

            return LocalPoint3d;
        }

        Vector3D GetVect3dFromVect3D(Vector3d Pt)
        {
            Vector3D LocalPoint3d = new Vector3D();
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

        Pt3D GetPt3DFromVecData3D(Vector3D Vec)
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

                        //Logger.GetInstance.Debug("Layer 1 Nameis : "+ LayersName[0]);
                    }
                    else if (AcadLyrTblRec.Name.ToLower().Equals("secondary support"))
                    {
                        LayersName[1] = AcadLyrTblRec.Name;

                        //Logger.GetInstance.Debug("Layer 2 Nameis : " + LayersName[1]);
                    }
                    else if (AcadLyrTblRec.Name.ToLower().Equals("concrete support"))
                    {
                        LayersName[2] = AcadLyrTblRec.Name;

                        //Logger.GetInstance.Debug("Layer 3 Nameis : " + LayersName[2]);
                    }
                }
            }
            return LayersName;
        }
        List<SupporSpecData> GetAllPrimarySupportData(Editor AcadEditor, Transaction AcadTransaction, string LayerName, ref List<SupporSpecData> ListPSuppoData, string filename)
        {
            // = new List<SupporSpecData>();
            try
            {
                PromptSelectionResult SelectionRes;
                ObjectIdCollection Ents;
                TypedValue[] SelFilterName = new TypedValue[1] { new TypedValue((int)DxfCode.LayerName, LayerName) };
                SelectionFilter SelFilter = new SelectionFilter(SelFilterName);

                List<CustomPlane> PipeData = new List<CustomPlane>();

                SelectionRes = AcadEditor.SelectAll(SelFilter);

                //Getting Object ID of the each selected entiry
                Ents = new ObjectIdCollection(SelectionRes.Value.GetObjectIds());

                int Count = ListPSuppoData.Count + 1;
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

                                SuppoSpecdata.AcadObjID = AcEnt.ObjectId;
                                FillBoundingBox(AcEnt, ref SuppoSpecdata);
                                SuppoSpecdata.CalculateCentroid();
                                FillDirVec(AcEnt, ref SuppoSpecdata);
                                SuppoSpecdata.CalculateDist();
                                SuppoSpecdata.CalculateVolume();
                                SuppoSpecdata.SupportName = BlockRef.Name;

                                SuppoSpecdata.PrimBoundingBoxData = new List<BoundingBoxFace>(Calculate.GetBoundingBoxFaces(GetPoint3DFromPt3D(SuppoSpecdata.Boundingboxmax), GetPoint3DFromPt3D(SuppoSpecdata.Boundingboxmin)));

                                SuppoSpecdata.SuppoId = "P" + Count.ToString();
                                // BlockRef.ScaleFactors

                                if (BlockRef.Name.ToUpper().Contains("GRP FLG"))
                                {
                                    GetRotationFlgGrp(BlockRef, ref SuppoSpecdata);
                                    if (PipeData.Count < 1)
                                    {
                                        PipeData = GetPipeAxis(AcadEditor, AcadTransaction);
                                    }
                                    SuppoSpecdata.ProjectionPlane = GetPlaneData(PipeData, SuppoSpecdata.Centroid);
                                }
                                else if (BlockRef.Name.ToUpper().Contains("NB"))
                                {
                                    GetRotationNBParts(BlockRef, ref SuppoSpecdata);
                                    if (PipeData.Count < 1)
                                    {
                                        PipeData = GetPipeAxis(AcadEditor, AcadTransaction);
                                    }
                                    SuppoSpecdata.ProjectionPlane = GetPlaneData(PipeData, SuppoSpecdata.Centroid);
                                }
                                else
                                {
                                    //SuppoSpecdata.IsSupportNB = true;
                                    DBObjectCollection ExpObjs = new DBObjectCollection();
                                    BlockRef.Explode(ExpObjs);
                                    GetZDir(ref SuppoSpecdata, ExpObjs);

                                    if (PipeData.Count < 1)
                                    {
                                        PipeData = GetPipeAxis(AcadEditor, AcadTransaction);
                                    }
                                    SuppoSpecdata.ProjectionPlane = GetPlaneData(PipeData, SuppoSpecdata.Centroid);
                                }
                            }
                        }
                        else if (AcEnt.GetType() == typeof(Autodesk.ProcessPower.PnP3dObjects.Support))
                        {
                            Autodesk.ProcessPower.PnP3dObjects.Support Suppo = AcEnt as Autodesk.ProcessPower.PnP3dObjects.Support;

                            dynamic Obj = Suppo.AcadObject;

                            SuppoSpecdata.AcadObjID = AcEnt.ObjectId;
                            SuppoSpecdata.SupportFileName = filename;
                            if (Obj != null)
                            {
                                SuppoSpecdata.SupportName = Obj.PartFamilyLongDesc;
                                string Tag = Obj.LineNumberTag;
                                SuppoSpecdata.Size = Obj.Size;
                            }

                            Autodesk.ProcessPower.PnP3dObjects.PartSizeProperties PartProp = Suppo.PartSizeProperties;

                            SuppoSpecdata.NomianalDia = PartProp.NominalDiameter;

                            FillBoundingBox(AcEnt, ref SuppoSpecdata);
                            SuppoSpecdata.CalculateCentroid();
                            FillDirVec(AcEnt, ref SuppoSpecdata);
                            SuppoSpecdata.CalculateDist();
                            SuppoSpecdata.CalculateVolume();

                            SuppoSpecdata.ProjectionPlane = new CustomPlane();
                            SuppoSpecdata.ProjectionPlane.PointOnPlane = SuppoSpecdata.Centroid;
                            SuppoSpecdata.ProjectionPlane.Normal = GetPt3DFromVecData(AcEnt.Ecs.CoordinateSystem3d.Xaxis);

                            SuppoSpecdata.PrimBoundingBoxData = new List<BoundingBoxFace>(Calculate.GetBoundingBoxFaces(GetPoint3DFromPt3D(SuppoSpecdata.Boundingboxmax), GetPoint3DFromPt3D(SuppoSpecdata.Boundingboxmin)));


                            if (PipeData.Count < 1)
                            {
                                PipeData = GetPipeAxis(AcadEditor, AcadTransaction);
                            }
                            SuppoSpecdata.ProjectionPlane = GetPlaneData(PipeData, SuppoSpecdata.Centroid);


                            try
                            {
                                //SuppoSpecdata.IsSupportNB = true;
                                DBObjectCollection ExpObjs = new DBObjectCollection();
                                AcEnt.Explode(ExpObjs);

                                foreach (var ent in ExpObjs)
                                {
                                    if (ent.GetType() == typeof(BlockReference))
                                    {
                                        DBObjectCollection ExpObjsRef = new DBObjectCollection();
                                        BlockReference blkref = ent as BlockReference;
                                        blkref.Explode(ExpObjsRef);
                                        foreach (var blkent in ExpObjsRef)
                                        {
                                            if (blkent.GetType() == typeof(Solid3d))
                                            {
                                                Solid3d solid3D = blkent as Solid3d;
                                                SuppoSpecdata.ListfaceData = GetFacesData(solid3D);
                                            }
                                        }
                                    }
                                    else if (ent.GetType() == typeof(Solid3d))
                                    {
                                        Solid3d solid3D = ent as Solid3d;
                                        SuppoSpecdata.ListfaceData = GetFacesData(solid3D);
                                    }
                                }
                            }
                            catch (Exception)
                            {

                            }

                            SuppoSpecdata.SuppoId = "P" + Count.ToString();
                        }

                        Count++;
                        ListPSuppoData.Add(SuppoSpecdata);
                    }
                    catch (Exception e)
                    {
                    }
                }
            }
            catch (Exception)
            {
            }
            return ListPSuppoData;
        }

        void GetRecursiveFaces(Entity AcEnt, List<FaceData> Listfaces)
        {
            List<FaceData> Faceslist = new List<FaceData>();
            try
            {
                DBObjectCollection ExpObjs = new DBObjectCollection();
                AcEnt.Explode(ExpObjs);

                foreach (var ent in ExpObjs)
                {
                    if (ent.GetType() == typeof(BlockReference))
                    {
                        Entity Ent = ent as Entity;
                        GetRecursiveFaces(Ent, Listfaces);
                        //DBObjectCollection ExpObjsRef = new DBObjectCollection();
                        //BlockReference blkref = ent as BlockReference;
                        //blkref.Explode(ExpObjsRef);
                        //foreach (var blkent in ExpObjsRef)
                        //{
                        //    if (blkent.GetType() == typeof(Solid3d))
                        //    {
                        //        Solid3d solid3D = blkent as Solid3d;

                        //    }
                        //}
                    }
                    else if (ent.GetType() == typeof(Solid3d))
                    {
                        Solid3d solid3D = ent as Solid3d;
                        ;
                        Faceslist = GetFacesData(solid3D);

                        if (Faceslist.Count > 0)
                        {
                            Listfaces.AddRange(Faceslist);
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
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

                    try
                    {
                        CirDataS.Vector.X = CirData.Normal.X;
                        CirDataS.Vector.Y = CirData.Normal.Y;
                        CirDataS.Vector.Z = CirData.Normal.Z;

                        SuppoSpecdata.PrimaryZhtNB = CirData.Center.Z;
                        CirDataS.AcadPlane = CirData.GetPlane();

                        SuppoSpecdata.ProjectionPlane = new CustomPlane();
                        SuppoSpecdata.ProjectionPlane.PointOnPlane = new Pt3D(GetPt3DFromPoint3d(CirData.Center));
                        SuppoSpecdata.ProjectionPlane.Normal = GetPt3DFromVecData(CirData.Normal);

                    }
                    catch (Exception)
                    {

                    }
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

            try
            {
                //SuppoSpecdata.IsSupportNB = true;
                DBObjectCollection ExpObjs2 = new DBObjectCollection();
                BlockRef.Explode(ExpObjs2);

                foreach (var ent2 in ExpObjs2)
                {
                    if (ent2.GetType() == typeof(BlockReference))
                    {
                        DBObjectCollection ExpObjsRef = new DBObjectCollection();
                        BlockReference blkref = ent2 as BlockReference;
                        blkref.Explode(ExpObjsRef);
                        foreach (var blkent in ExpObjsRef)
                        {
                            if (blkent.GetType() == typeof(Solid3d))
                            {
                                Solid3d solid3D = blkent as Solid3d;
                                SuppoSpecdata.ListfaceData = GetFacesData(solid3D);
                            }
                        }
                    }
                    else if (ent2.GetType() == typeof(Solid3d))
                    {
                        Solid3d solid3D = ent2 as Solid3d;
                        SuppoSpecdata.ListfaceData = GetFacesData(solid3D);
                    }
                }
            }
            catch (Exception)
            {

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
                    SuppoSpecdata.ProjectionPlane = new CustomPlane();
                    SuppoSpecdata.ProjectionPlane.PointOnPlane = new Pt3D(GetPt3DFromPoint3d(PlaneSurf.PointOnPlane));
                    SuppoSpecdata.ProjectionPlane.Normal = GetPt3DFromVecData(PlaneSurf.Normal);
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
                if (Dist < Calculate.DistPoint(GetPt3DFromPoint3d(FaceDataToProcess.PtonPlane), GetPt3DFromPoint3d(Face.PtonPlane)))
                {
                    Dist = Calculate.DistPoint(GetPt3DFromPoint3d(FaceDataToProcess.PtonPlane), GetPt3DFromPoint3d(Face.PtonPlane));

                    SuppoSpecdata.BottomPrim = GetPt3DFromPoint3d(Face.PtonPlane);
                }
            }


            try
            {
                //SuppoSpecdata.IsSupportNB = true;
                DBObjectCollection ExpObjs2 = new DBObjectCollection();
                BlockRef.Explode(ExpObjs2);

                foreach (var ent2 in ExpObjs2)
                {
                    if (ent2.GetType() == typeof(BlockReference))
                    {
                        DBObjectCollection ExpObjsRef = new DBObjectCollection();
                        BlockReference blkref = ent2 as BlockReference;
                        blkref.Explode(ExpObjsRef);
                        foreach (var blkent in ExpObjsRef)
                        {
                            if (blkent.GetType() == typeof(Solid3d))
                            {
                                Solid3d solid3D = blkent as Solid3d;
                                SuppoSpecdata.ListfaceData = GetFacesData(solid3D);
                            }
                        }
                    }
                    else if (ent2.GetType() == typeof(Solid3d))
                    {
                        Solid3d solid3D = ent2 as Solid3d;
                        SuppoSpecdata.ListfaceData = GetFacesData(solid3D);
                    }
                }
            }
            catch (Exception)
            {

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

                                Midpt = GetPt3DFromPoint3d(LineSeg.MidPoint);
                                IsFirst = false;
                            }
                            else if (LenghtCurveold > LineSeg.Length)
                            {
                                LengthofVer = LineSeg.Length;

                                Midpt = GetPt3DFromPoint3d(LineSeg.MidPoint);
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

        List<SupporSpecData> GetAllSecondarySupportData(Editor AcadEditor, Transaction AcadTransaction, string LayerName, ref List<SupporSpecData> ListSecondarySuppoData, string filename)
        {

            try
            {
                PromptSelectionResult SelectionRes;
                ObjectIdCollection Ents;
                TypedValue[] SelFilterName = new TypedValue[1] { new TypedValue((int)DxfCode.LayerName, LayerName) };
                SelectionFilter SelFilter = new SelectionFilter(SelFilterName);
                List<CustomPlane> PipeData = new List<CustomPlane>();
                SelectionRes = AcadEditor.SelectAll(SelFilter);

                //Getting Object ID of the each selected entiry
                Ents = new ObjectIdCollection(SelectionRes.Value.GetObjectIds());


                int Count = ListSecondarySuppoData.Count + 1;
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

                            SuppoSpecdata.AcadObjID = AcEnt.ObjectId;
                            FillBoundingBox(AcEnt, ref SuppoSpecdata);
                            SuppoSpecdata.CalculateCentroid();
                            FillDirVec(AcEnt, ref SuppoSpecdata);
                            SuppoSpecdata.CalculateDist();
                            SuppoSpecdata.CalculateVolume();
                            SuppoSpecdata.SupportFileName = filename;
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
                                    if (Structureobj.Size != null && Structureobj.Size.Length > 0 && Structureobj.Size.ToLower().Contains("angle") || Structureobj.Size.ToLower().Contains("thck"))
                                    {
                                        double Length = Calculate.DistPoint(GetPt3DFromArray(SuppoSpecdata.StPt), GetPt3DFromArray(SuppoSpecdata.EndPt));
                                        if (Length > 0)
                                        {
                                            FillAngleData(ref SuppoSpecdata, Length);
                                        }
                                        else
                                        {
                                            FillAngleData(ref SuppoSpecdata, 0);
                                        }

                                    }

                                    SuppoSpecdata.SupportName = Structureobj.PnPClassName;
                                }
                                //  Structureobj.
                            }
                            catch (System.Exception)
                            {
                            }


                            if (PipeData.Count < 1)
                            {
                                PipeData = GetPipeAxis(AcadEditor, AcadTransaction);
                            }
                            SuppoSpecdata.ProjectionPlane = GetPlaneData(PipeData, SuppoSpecdata.Centroid);

                            try
                            {
                                GetRecursiveFaces(AcEnt, SuppoSpecdata.ListfaceData);
                                //SuppoSpecdata.IsSupportNB = true;
                                /* DBObjectCollection ExpObjs = new DBObjectCollection();
                                 AcEnt.Explode(ExpObjs);

                                 foreach (var ent in ExpObjs)
                                 {
                                     if (ent.GetType() == typeof(Solid3d))
                                     {




                                         //if (solid3D != null)
                                         //{
                                         //    using (var Breps = new Autodesk.AutoCAD.BoundaryRepresentation.Brep(solid3D))
                                         //    {
                                         //        Autodesk.AutoCAD.BoundaryRepresentation.BrepFaceCollection FaceColl = Breps.Faces;




                                         //    }
                                         //}
                                     }
                                 }*/
                            }
                            catch (Exception)
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
                            SuppoSpecdata.AcadObjID = AcEnt.ObjectId;
                            GetSmallestEdge(ref SuppoSpecdata);

                            Pt3D Cntrd = new Pt3D();
                            Cntrd.X = Math.Abs(SLD.MassProperties.Centroid[0]);
                            Cntrd.Y = Math.Abs(SLD.MassProperties.Centroid[1]);
                            Cntrd.Z = Math.Abs(SLD.MassProperties.Centroid[2]);
                            SuppoSpecdata.Centroid = Cntrd;
                            FillBoundingBox(AcEnt, ref SuppoSpecdata);
                            FillDirVec(AcEnt, ref SuppoSpecdata);
                            SuppoSpecdata.Volume = SLD.MassProperties.Volume;

                            if (PipeData.Count < 1)
                            {
                                PipeData = GetPipeAxis(AcadEditor, AcadTransaction);
                            }
                            SuppoSpecdata.ProjectionPlane = GetPlaneData(PipeData, SuppoSpecdata.Centroid);

                            SuppoSpecdata.CalculateDist();
                            SuppoSpecdata.SupportName = "PLATE";
                            SuppoSpecdata.SuppoId = "S" + Count.ToString();
                            SuppoSpecdata.SupportFileName = filename;
                            ListSecondarySuppoData.Add(SuppoSpecdata);
                        }

                        Count++;
                    }
                    catch (System.Exception)
                    {
                    }
                }
            }
            catch (System.Exception)
            {
            }
            return ListSecondarySuppoData;
        }

        void FillAngleData(ref
            SupporSpecData Data, double Length)
        {
            if (Data.Size.ToLower().Contains("angle") && Data.Size.ToLower().Contains("thck"))
            {
                double AngleSize = 0;
                double Thickness = 0;
                string[] SplitStr = Data.Size.Split();

                if (SplitStr[1].Length > 0)
                {
                    try
                    {
                        AngleSize = Convert.ToInt32(SplitStr[1]);
                    }
                    catch (Exception)
                    {
                    }
                }
                if (SplitStr[3].Length > 0)
                {
                    try
                    {
                        Thickness = Convert.ToInt32(SplitStr[3]);
                    }
                    catch (Exception)
                    {
                    }
                }

                if (AngleSize > 0 && Thickness > 0)
                {
                    Data.LSecData = new AngleDt();
                    Data.LSecData.AngleSize = AngleSize;
                    Data.LSecData.AngleThck = Thickness;
                    Data.LSecData.Length = Length;
                }
            }
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

        List<Edgeinfo> GetAllEdgeInfo(Autodesk.AutoCAD.BoundaryRepresentation.Face AcadFace, bool IsLinearEdges = true)
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
                            EdgeData.StPt = GetPt3DFromPoint3d(LineSeg.StartPoint);
                            EdgeData.EndPt = GetPt3DFromPoint3d(LineSeg.EndPoint);
                            EdgeData.MidPoint = GetPt3DFromPoint3d(LineSeg.MidPoint);
                            EdgeData.EdgeLength = LineSeg.Length;

                            ListlinearEdge.Add(EdgeData);
                        }

                        else if (Curve.IsCircularArc && !IsLinearEdges)
                        {
                            EdgeData.StPt = GetPt3DFromPoint3d(Curve.StartPoint);
                            EdgeData.EndPt = GetPt3DFromPoint3d(Curve.EndPoint);

                            CircularArc3d arc3D = Curve.NativeCurve as CircularArc3d;
                            EdgeData.Radius = arc3D.Radius;
                            EdgeData.Center = GetPt3DFromPoint3d(arc3D.Center);

                            if (Curve.IsClosed())
                            {
                                EdgeData.TypeEdge = EdgeType.ClosedCircularEdge;
                            }
                            else
                            {
                                EdgeData.TypeEdge = EdgeType.OpenCircularEdge;

                            }
                            ListlinearEdge.Add(EdgeData);
                        }

                    }
                }
            }

            return ListlinearEdge;
        }

        void CheckforAnchorPlate(Solid3d solid3D, ref SupporSpecData Support)
        {
            int count = 0;
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
                        // ExtBSurf.Is
                        if (ExtBSurf.IsCylinder)
                        {
                            Support.IsAnchor = true;
                            count++;
                        }
                    }
                }
            }

            Support.NoOfAnchoreHole = count;
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

                            FaceLoc.PtonPlane = PlaneSurf.PointOnPlane;
                            FaceLoc.ListlinearEdge = GetAllEdgeInfo(FaceLoc.AcadFace);
                            FaceLoc.ListAllEdges = GetAllEdgeInfo(FaceLoc.AcadFace, false);
                        }
                        else if (ExtBSurf.IsCylinder)
                        {

                            Cylinder CylinderSurface = ExtBSurf.BaseSurface as Cylinder;
                            // CoordinateSystem3d CoSym = PlaneSurf.GetCoordinateSystem();
                            //GetDirectionFace(ref FaceLoc, CylinderSurface.getc GetCoordinateSystem());
                            //FaceLoc.FaceNormal = CylinderSurface.Normal;
                            FaceLoc.ListlinearEdge = GetAllEdgeInfo(FaceLoc.AcadFace);
                            FaceLoc.ListAllEdges = GetAllEdgeInfo(FaceLoc.AcadFace, false);
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

        List<SupporSpecData> GetAllConcreteSupportData(Editor AcadEditor, Transaction AcadTransaction, string LayerName, ref List<SupporSpecData> ListConcreteSupportData, string filename)
        {
            try
            {
                PromptSelectionResult SelectionRes;
                ObjectIdCollection Ents;
                TypedValue[] SelFilterName = new TypedValue[1] { new TypedValue((int)DxfCode.LayerName, LayerName) };
                SelectionFilter SelFilter = new SelectionFilter(SelFilterName);

                SelectionRes = AcadEditor.SelectAll(SelFilter);

                //Getting Object ID of the each selected entiry
                Ents = new ObjectIdCollection(SelectionRes.Value.GetObjectIds());

                int Count = ListConcreteSupportData.Count + 1;
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

                            SuppoSpecdata.AcadObjID = AcEnt.ObjectId;

                            CheckforAnchorPlate(SLD, ref SuppoSpecdata);
                            SuppoSpecdata.ListfaceData = GetFacesData(SLD);
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
                            SuppoSpecdata.SupportFileName = filename;
                            ListConcreteSupportData.Add(SuppoSpecdata);
                        }
                        else if (AcEnt.GetType().Name.Contains("ImpCurve"))
                        {
                            SupporSpecData SuppoSpecdata = new SupporSpecData();
                            // Solid3d SLD = AcEnt as Solid3d;

                            SuppoSpecdata.AcadObjID = AcEnt.ObjectId;
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

                            try
                            {
                                DBObjectCollection ExpObjs = new DBObjectCollection();
                                AcEnt.Explode(ExpObjs);

                                foreach (var ent in ExpObjs)
                                {
                                    if (ent.GetType() == typeof(Solid3d))
                                    {
                                        Solid3d solid3D = ent as Solid3d;
                                        SuppoSpecdata.ListfaceData = GetFacesData(solid3D);
                                    }
                                }
                            }
                            catch (Exception)
                            {

                            }

                            SuppoSpecdata.SuppoId = "C" + Count.ToString();
                            SuppoSpecdata.SupportFileName = filename;
                            ListConcreteSupportData.Add(SuppoSpecdata);
                        }

                        Count++;
                    }
                    catch (Exception)
                    {
                    }
                }

            }
            catch (Exception)
            {
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
            int CompletSCountCount = 0;
            int OnlyPrimaryCount = 0;
            int WithoutPrimCount = 0;
            int count = 0;

            foreach (SupportData Data in ListCentalSuppoData)
            {
                CheckRotationtTogetType(Data);
                if (Data.ListConcreteData.Count == 0 && Data.ListPrimarySuppo.Count > 0 && Data.ListSecondrySuppo.Count == 0)
                {
                    OnlyPrimaryCount++;
                }
                /*if( Data.ListConcreteData.Count>0&& Data.ListPrimarySuppo.Count > 0 &&Data.ListSecondrySuppo.Count > 0)
                {
                    CompletSCountCount++;
                }
                else if(Data.ListConcreteData.Count == 0 && Data.ListPrimarySuppo.Count > 0 && Data.ListSecondrySuppo.Count == 0)
                {
                    OnlyPrimaryCount++;
                }
                else if (Data.ListConcreteData.Count > 0 && Data.ListPrimarySuppo.Count == 0 && Data.ListSecondrySuppo.Count > 0)
                {
                    WithoutPrimCount++;
                }
                if (Data.SupportType!= null&& Data.SupportType.Length>0)
                {
                    if(Data.SupportType.ToUpper().Equals("SUPPORT13"))
                    {

                    }
                    else
                    {
                        Logger.GetInstance.Debug("Detected Support type =" + Data.SupportType);
                        count++;
                    }

                    if(Data.Name != null && Data.Name.Length > 0)
                    {
                        Logger.GetInstance.Debug("Detected Support type =" + Data.Name);
                    }

                }
                */
            }
            Logger.GetInstance.Debug("Only Primary Support Count =" + OnlyPrimaryCount.ToString());
#if _DEBUG
            /*Logger.GetInstance.Debug("Detected Support Count =" + count.ToString());
          
            Logger.GetInstance.Debug("Complete Support Count ="+ CompletSCountCount.ToString());
            Logger.GetInstance.Debug("Only Primary Support Count =" + OnlyPrimaryCount.ToString());
            Logger.GetInstance.Debug("Without Primary Support Count =" + WithoutPrimCount.ToString());*/
#endif 
        }

        void CheckRotationtTogetType(SupportData SData)
        {
            SupportData SupportData = GetRotationofParts(SData);


            ProcessDicDataToGetType(SupportData);
        }

        int GetAngleCount(SupportData SuppData)
        {
            int AngleCount = 0;
            foreach (var Spart in SuppData.ListSecondrySuppo)
            {
                if (Spart.Size != null && (Spart.Size.ToUpper().Contains("ANGLE") || Spart.Size.ToUpper().Contains("L-") || Spart.Size.ToUpper().Contains("ISA")) && !(Spart.Size.ToUpper().Contains("WEB")))
                {
                    AngleCount++;
                }
            }
            return AngleCount;
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
            /*
            else if (CSupportCount == 0 && PSupportCount == 0 && SSupportCount == 1)
            {
                if (GetAllISMC(SupportData).Count == 1)
                {
                    SupportData.SupportType = "Support57";
                }
            }
            else if (CSupportCount == 0 && PSupportCount == 0 && SSupportCount == 2)
            {
                CheckforTypeSupport54(ref SupportData);
            }
            else if (CSupportCount == 1 && PSupportCount == 0 && SSupportCount == 1)
            {
                CheckforTypeSupport58(ref SupportData);

            }
            else if (CSupportCount == 1 && PSupportCount > 0 && SSupportCount == 1)
            {
                CheckforTypeSupport58(ref SupportData);

            }
            else if (CSupportCount == 3 && SSupportCount == 2)
            {
                CheckforTypeSupport60(ref SupportData);

            }
            else if (CSupportCount == 0 && PSupportCount == 1 && SSupportCount == 1)
            {
                if (!CheckforTypeSupport14(ref SupportData))
                {
                    if (SupportData.ListSecondrySuppo[0].Size != null)
                    {
                        if ((SupportData.ListSecondrySuppo[0].Size.ToUpper().Contains("ANGLE") || SupportData.ListSecondrySuppo[0].Size.ToUpper().Contains("L-")) && !(SupportData.ListSecondrySuppo[0].Size.ToUpper().Contains("WEB")))
                        {
                            CheckforTypeSupport35(ref SupportData);
                        }
                    }
                }
            }
            else if (CSupportCount == 1 && PSupportCount == 1 && SSupportCount == 1)
            {
                CheckforTypeSupport14(ref SupportData);
            }
            else if (CSupportCount == 1 && PSupportCount == 2 && SSupportCount == 1)
            {
                CheckforTypeSupport86(ref SupportData);
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
            else if (CSupportCount == 0 && PSupportCount == 1 && SSupportCount == 3)
            {
                int NoofAngles = GetAngleCount(SupportData);
                if (NoofAngles == 3)
                {
                    CheckforTypeSupport38(ref SupportData);
                }
            }
            else if (CSupportCount == 1 && PSupportCount == 1 && SSupportCount == 2)
            {
                if (!CheckforTypeSupport2(ref SupportData))
                {
                    if (!CheckforTypeSupport4(ref SupportData))
                    {
                        if (!CheckforTypeSupport50(ref SupportData))
                        {
                            if (SupportData.ListSecondrySuppo.Any(x => x.SupportName == "PLATE"))
                            {
                                if (!CheckforTypeSupport8(ref SupportData))
                                {

                                }
                            }
                        }
                    }
                }
            }
            else if ((CSupportCount == 2 || CSupportCount == 1) && (PSupportCount == 0 || PSupportCount == 4 || PSupportCount == 2 || PSupportCount == 6 || PSupportCount == 7 || PSupportCount == 3 || PSupportCount == 1 || PSupportCount == 5) && (SSupportCount == 6 || SSupportCount == 2 || SSupportCount == 4 || SSupportCount == 5))
            {
                if (GetGussetCout(ref SupportData) == 4)
                {
                    if (!CheckforTypeSupport50(ref SupportData))
                    {
                        if (!CheckforTypeSupport64(ref SupportData))
                        {
                            if (!CheckforTypeSupport74(ref SupportData))
                            {
                                CheckforTypeSupport80(ref SupportData);
                            }
                        }
                    }
                }
                if (GetGussetCout(ref SupportData) == 0)
                {
                    if (!CheckforTypeSupport50(ref SupportData))
                    {
                        CheckforTypeSupport80(ref SupportData);
                    }
                }
            }
            else if (CSupportCount == 2 && PSupportCount == 9 && SSupportCount == 6 || CSupportCount == 2 && PSupportCount == 8 && SSupportCount == 6 || CSupportCount == 2 && PSupportCount == 4 && SSupportCount == 6
                  || CSupportCount == 2 && PSupportCount == 3 && SSupportCount == 6
                  || CSupportCount == 1 && PSupportCount == 4 && SSupportCount == 2
                  || CSupportCount == 1 && PSupportCount == 2 && SSupportCount == 2)
            {
                if (GetGussetCout(ref SupportData) == 4)
                {
                    if (!CheckforTypeSupport85(ref SupportData))
                    {
                        if (!CheckforTypeSupport64(ref SupportData))
                        {
                            CheckforTypeSupport74(ref SupportData);
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
            else if (CSupportCount == 2 && PSupportCount == 1 && SSupportCount == 4)
            {
                if (GetGussetCout(ref SupportData) == 2)
                    CheckforTypeSupport34(ref SupportData);
            }
            else if (CSupportCount == 2 && PSupportCount == 2 && SSupportCount == 3)
            {
                if (!CheckforTypeSupport22(ref SupportData))
                {
                    if (!CheckforTypeSupport6(ref SupportData))
                    {
                        if (!CheckforTypeSupport30(ref SupportData))
                        {
                            CheckforTypeSupport74(ref SupportData);
                        }
                    }
                }
            }
            else if (CSupportCount == 2 && PSupportCount == 3 && SSupportCount == 4 || CSupportCount == 2 && PSupportCount == 4 && SSupportCount == 3 || CSupportCount == 2 && PSupportCount == 4 && SSupportCount == 2)
            {
                CheckforTypeSupport74(ref SupportData);
            }
            else if (CSupportCount == 2 && PSupportCount == 2 && SSupportCount == 4)
            {
                CheckforTypeSupport43(ref SupportData);
            }
            else if (CSupportCount == 2 && PSupportCount == 2 && SSupportCount == 2)
            {
                CheckforTypeSupport7(ref SupportData);
            }
            else if ((CSupportCount == 2 || CSupportCount == 1) && (PSupportCount == 1 || PSupportCount == 0 || PSupportCount == 2) && SSupportCount == 3)
            {
                if (!CheckforTypeSupport31(ref SupportData))
                {
                    CheckforTypeSupport64(ref SupportData);
                }
            }
            else if (CSupportCount == 1 && PSupportCount == 0 && SSupportCount == 4)
            {
                CheckforTypeSupport64(ref SupportData);
            }
            else if (CSupportCount == 2 && PSupportCount == 2 && SSupportCount == 3)
            {

            }
            else if (CSupportCount == 1 && PSupportCount == 2 && SSupportCount == 3)
            {
                if (!CheckforTypeSupport22(ref SupportData))
                {
                    if (!CheckforTypeSupport6(ref SupportData))
                    {

                    }
                }
            }
            else if (CSupportCount == 2 && PSupportCount == 3 && SSupportCount == 4)
            {
                if (!CheckforTypeSupport32(ref SupportData))
                {
                    CheckforTypeSupport36(ref SupportData);
                }
            }
            else if (CSupportCount == 1 && PSupportCount == 2 && SSupportCount == 2)
            {
                CheckforTypeSupport7(ref SupportData);
            }
            else if (CSupportCount == 1 && PSupportCount == 2 && SSupportCount == 4)
            {
                try
                {
                    if (Checkforangle(ref SupportData))
                    {
                        CheckforTypeSupport20(ref SupportData);
                    }
                    else
                    {
                        CheckforTypeSupport41(ref SupportData);
                    }

                }
                catch (Exception)
                {
                }
            }
            else if (CSupportCount == 1 && PSupportCount == 3 && SSupportCount == 4)
            {

                CheckforTypeSupport18(ref SupportData);
            }
            else if (CSupportCount == 1 && PSupportCount == 3 && SSupportCount == 5)
            {
                if (!CheckforTypeSupport19(ref SupportData))
                {
                    CheckforTypeSupport80(ref SupportData);
                }
            }
            else if (CSupportCount == 2 && PSupportCount == 1 && SSupportCount == 7 || CSupportCount == 2 && PSupportCount == 2 && SSupportCount == 7)
            {
                if (HasWord(SupportData.ListSecondrySuppo, "NB"))
                {

                    //CheckforTypeSupport28(ref SupportData);
                    //Need to Add Check for Primary Sup
                    CheckforTypeSupport25(ref SupportData);
                }
            }

            else if (CSupportCount == 2 && PSupportCount == 1 && SSupportCount == 7 || CSupportCount == 2 && PSupportCount == 2 && SSupportCount == 7 || CSupportCount == 2 && PSupportCount == 4 && SSupportCount == 7)
            {
                if (HasWord(SupportData.ListSecondrySuppo, "NB"))
                {

                    //CheckforTypeSupport28(ref SupportData);
                    //Need to Add Check for Primary Sup
                    CheckforTypeSupport25(ref SupportData);
                }
            }
            else if (CSupportCount == 2 && PSupportCount == 1 && SSupportCount == 8)
            {
                if (HasWord(SupportData.ListSecondrySuppo, "NB"))
                {
                    if (GetAngleCount(SupportData) == 1)
                    {
                        CheckforTypeSupport29(ref SupportData);
                    }
                }
            }
            else if (CSupportCount == 2 && PSupportCount == 1 && SSupportCount == 9)
            {
                if (HasWord(SupportData.ListSecondrySuppo, "NB"))
                {
                    if (GetAngleCount(SupportData) == 2)
                    {
                        CheckforTypeSupport48(ref SupportData);
                    }
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
            else if (CSupportCount == 2 && PSupportCount == 3 && SSupportCount == 11)
            {
                if (HasWord(SupportData.ListSecondrySuppo, "NB"))
                {
                    if (GetAngleCount(SupportData) == 2)
                    {
                        CheckforTypeSupport47(ref SupportData);
                    }
                }
            }
            else if (CSupportCount == 2 && PSupportCount == 3 && SSupportCount == 10)
            {
                if (HasWord(SupportData.ListSecondrySuppo, "NB"))
                {
                    if (GetAngleCount(SupportData) == 1)
                    {
                        CheckforTypeSupport49(ref SupportData);
                    }
                }
            }
            else if (CSupportCount == 2 && PSupportCount == 0 && SSupportCount == 8 || CSupportCount == 2 && PSupportCount == 0 && SSupportCount == 7 || CSupportCount == 2 && PSupportCount == 7 && SSupportCount == 8
                || CSupportCount == 2 && PSupportCount == 4 && SSupportCount == 8)
            {
                CheckforTypeSupport70(ref SupportData);
            }


            */

        }

        bool CheckanglesCentroidInLine(SupportData SupData)
        {
            if (SupData.ListSecondrySuppo[0] != null && SupData.ListSecondrySuppo[0].LSecData != null && SupData.ListSecondrySuppo[0].LSecData.AngleSize > 0)
            {
                List<SupporSpecData> ListEmpty = new List<SupporSpecData>();
                string Orientation = "";
                return CheckCentroidInLine(GetCentroidFromList(SupData.ListConcreteData), GetCentroidFromList(SupData.ListSecondrySuppo), GetCentroidFromList(ListEmpty), ref Orientation, SupData.ListSecondrySuppo[0].LSecData.AngleSize);
            }

            return false;
        }

        bool CheckforTypeSupport80(ref SupportData SupData)
        {
            Dictionary<int, SupporSpecData> DicIndexData = new Dictionary<int, SupporSpecData>();
            Dictionary<int, SupporSpecData> DicLSecData = new Dictionary<int, SupporSpecData>();
            DicIndexData = GetAllISMC(SupData);
            DicLSecData = GetAllAngles(SupData);


            if (DicLSecData.Count > 0)
            {
                if (SupData.ListPrimarySuppo.Count > 0)
                {
                    if (SupData.ListSecondrySuppo.Count == 2 && DicLSecData.Count == 2)
                    {
                        if (CheckanglesCentroidInLine(SupData))
                        {
                            if (Math.Abs(Math.Round(SupData.ListSecondrySuppo[0].Angle.ZinDegree - SupData.ListSecondrySuppo[1].Angle.ZinDegree)).Equals(90) && Math.Abs(Math.Round(SupData.ListSecondrySuppo[0].Angle.YinDegree - SupData.ListSecondrySuppo[1].Angle.YinDegree)).Equals(90) && Math.Round(SupData.ListSecondrySuppo[0].Angle.XinDegree).Equals(Math.Round(SupData.ListSecondrySuppo[1].Angle.XinDegree)))
                            {
                                SupData.SupportType = "Support110";
                                return true;
                            }
                        }
                    }
                }
            }

            List<SupporSpecData> ListIsmc = new List<SupporSpecData>();
            List<SupporSpecData> ListEmpty = new List<SupporSpecData>();
            foreach (var Part in DicIndexData)
            {
                ListIsmc.Add(Part.Value);
            }
            List<string> ListParSup = new List<string>();
            List<List<string>> CombinedParSupp = new List<List<string>>();

            ListParSup = Checkandgetparllelsupports(ref SupData, ref CombinedParSupp);

            Dictionary<string, double> DicMidistPt = new Dictionary<string, double>();
            DicMidistPt = CheckforMidDist(SupData);

            Dictionary<string, double> DicDistConSup = new Dictionary<string, double>();
            DicDistConSup = GetminDistFromConcrete(SupData);

            string Id = GetMinDistFromDic(DicDistConSup);

            string VerPartId = "";
            foreach (var Part in CombinedParSupp)
            {
                if (Part.Count == 1)
                {
                    if (Part[0] == Id)
                    {
                        VerPartId = Id;
                        break;
                    }
                }
            }

            if (VerPartId.Length < 1)
            {
                return false;
            }

            Dictionary<string, MinPtDist> DicAngleData = new Dictionary<string, MinPtDist>();

            List<Vector3D> ListVecData = new List<Vector3D>();

            DicAngleData = GetAngleBetweenSupport(VerPartId, SupData, ref ListVecData);

            double maxValue = 0;
            string MaxValueID = "";

            foreach (var DicData in DicDistConSup)
            {
                if (DicData.Value > maxValue)
                {
                    maxValue = DicData.Value;
                    MaxValueID = DicData.Key;
                }
            }

            if (CheckVectorsArePlaner(ListVecData))
            {
                int NevAngleCount = 0;
                int PosAngleCount = 0;
                List<string> ListNevPartId = new List<string>();
                List<string> ListPosPartId = new List<string>();
                foreach (var AngleData in DicAngleData)
                {
                    if (AngleData.Value.Angle.Equals(90))
                    {
                        PosAngleCount++;
                        ListPosPartId.Add(AngleData.Key);
                    }
                    else if (AngleData.Value.Angle.Equals(-90))
                    {
                        NevAngleCount++;
                        ListNevPartId.Add(AngleData.Key);
                    }
                }

                if (DicAngleData.Count == 1 && PosAngleCount == 1)
                {
                    if ((Math.Abs(Math.Round(GetPartbyId(DicAngleData.ElementAt(0).Key, SupData.ListSecondrySuppo).Angle.XinDegree))).Equals(90) && Math.Abs(Math.Round(GetPartbyId(DicAngleData.ElementAt(0).Key, SupData.ListSecondrySuppo).Angle.ZinDegree)).Equals(Math.Round(GetPartbyId(VerPartId, SupData.ListSecondrySuppo).Angle.ZinDegree)) && GetPartbyId(VerPartId, SupData.ListSecondrySuppo).Boundingboxmax.Z <= GetPartbyId(DicAngleData.ElementAt(0).Key, SupData.ListSecondrySuppo).Boundingboxmax.Z && GetPartbyId(DicAngleData.ElementAt(0).Key, SupData.ListSecondrySuppo).Centroid.Z <= GetPartbyId(VerPartId, SupData.ListSecondrySuppo).Boundingboxmax.Z)
                    {
                        SupData.SupportType = "Support80";
                        return true;
                    }
                }
            }
            else
            {
                if ((DicIndexData.Count == 4 || DicIndexData.Count == 5) && CombinedParSupp.Count == 3 && (DicMidistPt.Count == 1 || DicMidistPt.Count == 2))
                {
                    ListVecData = new List<Vector3D>();
                    List<string> SupportstobeExcluded = new List<string>();
                    List<string> ListPerPartID = new List<string>();

                    foreach (var CSuppo in DicMidistPt)
                    {
                        SupportstobeExcluded.Add(CSuppo.Key);
                        ListPerPartID.Add(CSuppo.Key);
                    }

                    DicAngleData = GetAngleBetweenSupport(VerPartId, SupData, ref ListVecData, SupportstobeExcluded);

                    if (CheckVectorsArePlaner(ListVecData))
                    {
                        int NevAngleCount = 0;
                        int PosAngleCount = 0;
                        List<string> ListNevPartId = new List<string>();
                        List<string> ListPosPartId = new List<string>();

                        foreach (var AngleData in DicAngleData)
                        {
                            if (AngleData.Value.Angle.Equals(90))
                            {
                                PosAngleCount++;
                                ListPosPartId.Add(AngleData.Key);
                            }
                            else if (AngleData.Value.Angle.Equals(-90))
                            {
                                NevAngleCount++;
                                ListNevPartId.Add(AngleData.Key);
                            }
                        }

                        if (ListPosPartId.Count == 2)
                        {

                            if (ListPerPartID.Count == 1 && DicIndexData.Count == 4)
                            {
                                if (GetTouchingPrimaryCount(GetPartbyId(ListPerPartID[0], SupData.ListSecondrySuppo).ListtouchingParts) == 2 && GetGRPFLGCount(SupData.ListPrimarySuppo) == 2)
                                {
                                    if (GetPartbyId(ListPosPartId[0], SupData.ListSecondrySuppo).Boundingboxmax.Z > GetPartbyId(ListPosPartId[1], SupData.ListSecondrySuppo).Boundingboxmax.Z)
                                    {
                                        if (GetPartbyId(ListPerPartID[0], SupData.ListSecondrySuppo).ListtouchingParts.Contains(ListPosPartId[0]))
                                        {
                                            if (Math.Round(GetPartbyId(ListPosPartId[0], SupData.ListSecondrySuppo).Angle.ZinDegree).Equals(Math.Round(GetPartbyId(VerPartId, SupData.ListSecondrySuppo).Angle.ZinDegree)) && Math.Abs(Math.Round(GetPartbyId(ListPosPartId[1], SupData.ListSecondrySuppo).Angle.ZinDegree - GetPartbyId(VerPartId, SupData.ListSecondrySuppo).Angle.ZinDegree)).Equals(180))
                                            {
                                                SupData.SupportType = "Support104";
                                            }
                                            else if (SupData.ListPrimarySuppo.Count == 3 && GetPartbyId(ListPosPartId[1], SupData.ListSecondrySuppo).ListtouchingParts.Count == 2 && Math.Round(GetPartbyId(ListPosPartId[0], SupData.ListSecondrySuppo).Angle.ZinDegree).Equals(Math.Round(GetPartbyId(VerPartId, SupData.ListSecondrySuppo).Angle.ZinDegree)) && Math.Round(GetPartbyId(ListPosPartId[1], SupData.ListSecondrySuppo).Angle.ZinDegree).Equals(Math.Round(GetPartbyId(VerPartId, SupData.ListSecondrySuppo).Angle.ZinDegree)))
                                            {
                                                SupData.SupportType = "Support107";
                                            }
                                        }
                                        else if (GetPartbyId(ListPosPartId[1], SupData.ListSecondrySuppo).Boundingboxmax.Z > GetPartbyId(ListPosPartId[0], SupData.ListSecondrySuppo).Boundingboxmax.Z)
                                        {
                                            if (GetPartbyId(ListPerPartID[0], SupData.ListSecondrySuppo).ListtouchingParts.Contains(ListPosPartId[1]))
                                            {
                                                if (Math.Round(GetPartbyId(ListPosPartId[1], SupData.ListSecondrySuppo).Angle.ZinDegree).Equals(Math.Round(GetPartbyId(VerPartId, SupData.ListSecondrySuppo).Angle.ZinDegree)) && Math.Abs(Math.Round(GetPartbyId(ListPosPartId[0], SupData.ListSecondrySuppo).Angle.ZinDegree - GetPartbyId(VerPartId, SupData.ListSecondrySuppo).Angle.ZinDegree)).Equals(180))
                                                {
                                                    SupData.SupportType = "Support104";

                                                }
                                                else if (SupData.ListPrimarySuppo.Count == 3 && GetPartbyId(ListPosPartId[1], SupData.ListSecondrySuppo).ListtouchingParts.Count == 2 && Math.Round(GetPartbyId(ListPosPartId[0], SupData.ListSecondrySuppo).Angle.ZinDegree).Equals(Math.Round(GetPartbyId(VerPartId, SupData.ListSecondrySuppo).Angle.ZinDegree)) && Math.Round(GetPartbyId(ListPosPartId[1], SupData.ListSecondrySuppo).Angle.ZinDegree).Equals(Math.Round(GetPartbyId(VerPartId, SupData.ListSecondrySuppo).Angle.ZinDegree)))
                                                {
                                                    SupData.SupportType = "Support107";
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            else if (ListPerPartID.Count == 2)
                            {
                                if (GetPartbyId(ListPosPartId[0], SupData.ListSecondrySuppo).Angle.ZinDegree.Equals(GetPartbyId(ListPosPartId[1], SupData.ListSecondrySuppo).Angle.ZinDegree) && Math.Round(GetPartbyId(ListPosPartId[1], SupData.ListSecondrySuppo).Angle.ZinDegree).Equals(Math.Round(GetPartbyId(VerPartId, SupData.ListSecondrySuppo).Angle.ZinDegree)))
                                {
                                    if (GetTouchingPrimaryCount(GetPartbyId(ListPerPartID[0], SupData.ListSecondrySuppo).ListtouchingParts) == 2 && GetTouchingPrimaryCount(GetPartbyId(ListPerPartID[1], SupData.ListSecondrySuppo).ListtouchingParts) == 2 && SupData.ListPrimarySuppo.Count == 4 && GetGRPFLGCount(SupData.ListPrimarySuppo) == 4)
                                    {
                                        SupData.SupportType = "Support105";

                                    }
                                }
                            }

                            if (SupData.SupportType == "Support105" || SupData.SupportType == "Support104" || SupData.SupportType == "Support107")
                            {
                                foreach (var SData in SupData.ListSecondrySuppo)
                                {
                                    if (SData.SuppoId != null && SData.SuppoId.Equals(VerPartId))
                                    {
                                        SData.PartDirection = "Ver";

                                    }
                                    else if (SData.SuppoId != null && ListPosPartId.Contains(SData.SuppoId) || ListNevPartId.Contains(SData.SuppoId))
                                    {
                                        if (Math.Abs(Math.Round(SData.Angle.XinDegree)).Equals(90))
                                        {
                                            SData.PartDirection = "Hor";

                                        }
                                    }
                                    else if (SData.SuppoId != null && ListPerPartID.Contains(SData.SuppoId) && DicMidistPt.ContainsKey(SData.SuppoId))
                                    {
                                        SData.PartDirection = "3DHOR";
                                    }

                                }

                                return true;
                            }

                        }
                    }

                }
            }
            return false;
        }

        int GetGRPFLGCount(List<SupporSpecData> ListSuppoData)
        {
            int GRPCount = 0;
            foreach (var Sup in ListSuppoData)
            {
                if (Sup.SupportName.ToUpper().Contains("GRP FLG"))
                {
                    GRPCount++;
                }
            }

            return GRPCount;
        }

        bool CheckforTypeSupport74(ref SupportData SupData)
        {
            Dictionary<int, SupporSpecData> DicIndexData = new Dictionary<int, SupporSpecData>();

            DicIndexData = GetAllISMC(SupData);

            List<SupporSpecData> ListIsmc = new List<SupporSpecData>();
            List<SupporSpecData> ListEmpty = new List<SupporSpecData>();
            foreach (var Part in DicIndexData)
            {
                ListIsmc.Add(Part.Value);
            }
            List<string> ListParSup = new List<string>();
            List<List<string>> CombinedParSupp = new List<List<string>>();

            ListParSup = Checkandgetparllelsupports(ref SupData, ref CombinedParSupp);

            Dictionary<string, double> DicMidistPt = new Dictionary<string, double>();
            DicMidistPt = CheckforMidDist(SupData);

            Dictionary<string, double> DicDistConSup = new Dictionary<string, double>();
            DicDistConSup = GetminDistFromConcrete(SupData);

            string Id = GetMinDistFromDic(DicDistConSup);

            string VerPartId = "";
            foreach (var Part in CombinedParSupp)
            {
                if (Part.Count == 1)
                {
                    if (Part[0] == Id)
                    {
                        VerPartId = Id;
                        break;
                    }
                }
            }

            if (VerPartId.Length < 1)
            {
                return false;
            }

            Dictionary<string, MinPtDist> DicAngleData = new Dictionary<string, MinPtDist>();

            List<Vector3D> ListVecData = new List<Vector3D>();

            DicAngleData = GetAngleBetweenSupport(VerPartId, SupData, ref ListVecData);

            double maxValue = 0;
            string MaxValueID = "";

            foreach (var DicData in DicDistConSup)
            {
                if (DicData.Value > maxValue)
                {
                    maxValue = DicData.Value;
                    MaxValueID = DicData.Key;
                }
            }

            if (CheckVectorsArePlaner(ListVecData))
            {
                int NevAngleCount = 0;
                int PosAngleCount = 0;
                List<string> ListNevPartId = new List<string>();
                List<string> ListPosPartId = new List<string>();
                foreach (var AngleData in DicAngleData)
                {
                    if (AngleData.Value.Angle.Equals(90))
                    {
                        PosAngleCount++;
                        ListPosPartId.Add(AngleData.Key);
                    }
                    else if (AngleData.Value.Angle.Equals(-90))
                    {
                        NevAngleCount++;
                        ListNevPartId.Add(AngleData.Key);
                    }
                }

                if (DicAngleData.Count == 1)
                {
                    if ((Math.Abs(Math.Round(GetPartbyId(VerPartId, SupData.ListSecondrySuppo).Angle.ZinDegree - GetPartbyId(DicAngleData.ElementAt(0).Key, SupData.ListSecondrySuppo).Angle.ZinDegree))).Equals(180) && Math.Abs(Math.Round(GetPartbyId(DicAngleData.ElementAt(0).Key, SupData.ListSecondrySuppo).Angle.XinDegree)).Equals(180) && GetTouchingPrimaryCount(GetPartbyId(DicAngleData.ElementAt(0).Key, SupData.ListSecondrySuppo).ListtouchingParts) == 4 && GetPartbyId(VerPartId, SupData.ListSecondrySuppo).Boundingboxmax.Z <= GetPartbyId(DicAngleData.ElementAt(0).Key, SupData.ListSecondrySuppo).Boundingboxmax.Z && GetPartbyId(DicAngleData.ElementAt(0).Key, SupData.ListSecondrySuppo).Centroid.Z <= GetPartbyId(VerPartId, SupData.ListSecondrySuppo).Boundingboxmax.Z)
                    {
                        SupData.SupportType = "Support79";
                        return true;
                    }
                }
                else if (DicAngleData.Count == 2)
                {
                    if ((Math.Abs(Math.Round(GetPartbyId(VerPartId, SupData.ListSecondrySuppo).Angle.ZinDegree - GetPartbyId(DicAngleData.ElementAt(0).Key, SupData.ListSecondrySuppo).Angle.ZinDegree))).Equals(180) && GetPartbyId(DicAngleData.ElementAt(0).Key, SupData.ListSecondrySuppo).Angle.XinDegree.Equals(Math.Round(GetPartbyId(DicAngleData.ElementAt(1).Key, SupData.ListSecondrySuppo).Angle.XinDegree)) && GetTouchingPrimaryCount(GetPartbyId(DicAngleData.ElementAt(1).Key, SupData.ListSecondrySuppo).ListtouchingParts) == 1 && GetTouchingPrimaryCount(GetPartbyId(DicAngleData.ElementAt(0).Key, SupData.ListSecondrySuppo).ListtouchingParts) == 1)
                    {
                        if (NevAngleCount == 2)
                        {
                            SupData.SupportType = "Support74";
                            return true;
                        }
                        else if (PosAngleCount == 2)
                        {
                            SupData.SupportType = "Support76";
                            return true;
                        }
                    }
                    else if ((Math.Abs(Math.Round(GetPartbyId(VerPartId, SupData.ListSecondrySuppo).Angle.ZinDegree - GetPartbyId(DicAngleData.ElementAt(0).Key, SupData.ListSecondrySuppo).Angle.ZinDegree))).Equals(180) && (GetPartbyId(DicAngleData.ElementAt(0).Key, SupData.ListSecondrySuppo).Angle.XinDegree.Equals(Math.Round(GetPartbyId(DicAngleData.ElementAt(1).Key, SupData.ListSecondrySuppo).Angle.XinDegree)) || ((Math.Abs(Math.Round(GetPartbyId(DicAngleData.ElementAt(1).Key, SupData.ListSecondrySuppo).Angle.XinDegree - GetPartbyId(DicAngleData.ElementAt(0).Key, SupData.ListSecondrySuppo).Angle.XinDegree))).Equals(180)) && NevAngleCount == 1 && PosAngleCount == 1))
                    {
                        if (GetTouchingPrimaryCount(GetPartbyId(ListNevPartId[0], SupData.ListSecondrySuppo).ListtouchingParts) == 3 && GetTouchingPrimaryCount(GetPartbyId(ListNevPartId[0], SupData.ListSecondrySuppo).ListtouchingParts) == 1)
                        {
                            double Dist1 = GetPerpendicuarFootDist(ListNevPartId[0], VerPartId, SupData);
                            double Dist2 = GetPerpendicuarFootDist(ListPosPartId[0], VerPartId, SupData);

                            if (Dist1 > Dist2)
                            {
                                SupData.SupportType = "Support78";
                                return true;
                            }
                        }
                    }
                }
                else if (DicAngleData.Count == 3)
                {
                    if ((Math.Abs(Math.Round(GetPartbyId(VerPartId, SupData.ListSecondrySuppo).Angle.ZinDegree - GetPartbyId(DicAngleData.ElementAt(0).Key, SupData.ListSecondrySuppo).Angle.ZinDegree))).Equals(180) && GetPartbyId(DicAngleData.ElementAt(0).Key, SupData.ListSecondrySuppo).Angle.XinDegree.Equals(Math.Round(GetPartbyId(DicAngleData.ElementAt(1).Key, SupData.ListSecondrySuppo).Angle.XinDegree)) &&
                         GetPartbyId(DicAngleData.ElementAt(2).Key, SupData.ListSecondrySuppo).Angle.XinDegree.Equals(Math.Round(GetPartbyId(DicAngleData.ElementAt(1).Key, SupData.ListSecondrySuppo).Angle.XinDegree)) && GetTouchingPrimaryCount(GetPartbyId(DicAngleData.ElementAt(1).Key, SupData.ListSecondrySuppo).ListtouchingParts) == 1 && GetTouchingPrimaryCount(GetPartbyId(DicAngleData.ElementAt(0).Key, SupData.ListSecondrySuppo).ListtouchingParts) == 1 && GetTouchingPrimaryCount(GetPartbyId(DicAngleData.ElementAt(2).Key, SupData.ListSecondrySuppo).ListtouchingParts) == 1)
                    {
                        if (NevAngleCount == 3)
                        {
                            SupData.SupportType = "Support75";
                            return true;
                        }
                        else if (PosAngleCount == 3)
                        {
                            SupData.SupportType = "Support77";
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        double GetPerpendicuarFootDist(string ParId, string VerPart, SupportData SupData)
        {

            double Dist = 0;
            Point3d PerFoot = FindPerpendicularFoot(GetPt3DFromArray(
            GetPartbyId(ParId, SupData.ListSecondrySuppo).StPt), GetPartbyId(VerPart, SupData.ListSecondrySuppo).StPt, GetPartbyId(VerPart, SupData.ListSecondrySuppo).EndPt);


            return Calculate.DistPoint(GetPt3DFromPoint3d(PerFoot), SupData.ListConcreteData[0].Centroid);
        }

        bool CheckforTypeSupport70(ref SupportData SupData)
        {
            Dictionary<int, SupporSpecData> DicIndexData = new Dictionary<int, SupporSpecData>();

            DicIndexData = GetAllISMC(SupData);

            List<SupporSpecData> ListIsmc = new List<SupporSpecData>();
            List<SupporSpecData> ListEmpty = new List<SupporSpecData>();
            foreach (var Part in DicIndexData)
            {
                ListIsmc.Add(Part.Value);
            }
            List<string> ListParSup = new List<string>();
            List<List<string>> CombinedParSupp = new List<List<string>>();

            ListParSup = Checkandgetparllelsupports(ref SupData, ref CombinedParSupp);

            Dictionary<string, double> DicMidistPt = new Dictionary<string, double>();
            DicMidistPt = CheckforMidDist(SupData);

            Dictionary<string, double> DicDistConSup = new Dictionary<string, double>();
            DicDistConSup = GetminDistFromConcrete(SupData);

            string Id = GetMinDistFromDic(DicDistConSup);

            string VerPartId = "";
            foreach (var Part in CombinedParSupp)
            {
                if (Part.Count == 1)
                {
                    if (Part[0] == Id)
                    {
                        VerPartId = Id;
                        break;
                    }
                }
            }

            if (VerPartId.Length < 1)
            {
                return false;
            }

            Dictionary<string, MinPtDist> DicAngleData = new Dictionary<string, MinPtDist>();

            List<Vector3D> ListVecData = new List<Vector3D>();

            DicAngleData = GetAngleBetweenSupport(VerPartId, SupData, ref ListVecData);

            double maxValue = 0;
            string MaxValueID = "";

            foreach (var DicData in DicDistConSup)
            {
                if (DicData.Value > maxValue)
                {
                    maxValue = DicData.Value;
                    MaxValueID = DicData.Key;
                }
            }

            if (DicMidistPt != null && DicMidistPt.Count == 1 && CheckVectorsArePlaner(ListVecData))
            {
                if ((GetPartbyId(VerPartId, SupData.ListSecondrySuppo).Boundingboxmax.Z) <= (GetPartbyId(MaxValueID, SupData.ListSecondrySuppo).Boundingboxmax.Z))
                {
                    if (MaxValueID.Equals(DicMidistPt.ElementAt(0).Key))
                    {
                        int Pos90Count = 0;
                        int Nev90Count = 0;
                        int InclinedPos = 0;
                        int InclinedNev = 0;
                        List<string> PosId = new List<string>();
                        List<string> NevId = new List<string>();
                        List<string> PosIncli = new List<string>();
                        List<string> NevIncli = new List<string>();
                        foreach (var data in DicAngleData)
                        {
                            if (DicMidistPt.ElementAt(0).Key.Equals(data.Key))
                            {
                                continue;
                            }

                            if (Math.Round(data.Value.Angle).Equals(90))
                            {
                                Pos90Count++;
                                PosId.Add(data.Key);
                            }
                            else if (Math.Round(data.Value.Angle).Equals(-90))
                            {
                                Nev90Count++;
                                NevId.Add(data.Key);
                            }
                            else if (Math.Round(data.Value.Angle).Equals(-45))
                            {
                                NevIncli.Add(data.Key);
                                InclinedNev++;
                            }
                            else if (Math.Round(data.Value.Angle).Equals(45))
                            {
                                PosIncli.Add(data.Key);
                                InclinedPos++;
                            }
                        }

                        int HorAngleCount = 0;
                        int InclinedAngle = 0;

                        SupporSpecData SupDataHor = new SupporSpecData();
                        SupporSpecData SupDataVer = new SupporSpecData();
                        SupporSpecData SupDataInc1 = new SupporSpecData();
                        SupporSpecData SupDataInc2 = new SupporSpecData();
                        if (InclinedPos == 1 && InclinedNev == 1)
                        {
                            foreach (var SData in SupData.ListSecondrySuppo)
                            {
                                if (DicMidistPt.ElementAt(0).Key.Equals(SData.SuppoId))
                                {
                                    SData.PartDirection = "Hor";
                                    HorAngleCount++;
                                    SupDataHor = SData;
                                }
                                else if (SData.SuppoId != null && PosId.Contains(SData.SuppoId))
                                {
                                    if (Math.Abs(Math.Round(SData.Angle.XinDegree)).Equals(90))
                                    {
                                        SData.PartDirection = "Hor";
                                        HorAngleCount++;
                                    }
                                }
                                else if (SData.SuppoId != null && SData.SuppoId.Equals(VerPartId))
                                {
                                    SData.PartDirection = "Ver";
                                    SupDataVer = SData;
                                }
                                else if (SData.SuppoId != null && NevId.Contains(SData.SuppoId))
                                {
                                    if (Math.Abs(Math.Round(SData.Angle.XinDegree)).Equals(90))
                                    {
                                        SData.PartDirection = "Hor";
                                        HorAngleCount++;
                                    }
                                }
                                else if (SData.SuppoId != null && PosIncli.Contains(SData.SuppoId))
                                {
                                    if (Math.Abs(Math.Round(SData.Angle.XinDegree)).Equals(45) || Math.Abs(Math.Round(SData.Angle.XinDegree)).Equals(135))
                                    {
                                        SData.PartDirection = "45Inclined";
                                        InclinedAngle++;

                                        SupDataInc1 = SData;
                                    }
                                }
                                else if (SData.SuppoId != null && NevIncli.Contains(SData.SuppoId))
                                {
                                    if (Math.Abs(Math.Round(SData.Angle.XinDegree)).Equals(45) || Math.Abs(Math.Round(SData.Angle.XinDegree)).Equals(135))
                                    {
                                        SData.PartDirection = "45Inclined";
                                        InclinedAngle++;

                                        SupDataInc2 = SData;
                                    }
                                }
                            }

                            if (SupDataHor.ListtouchingParts.Contains(SupDataInc1.SuppoId) && SupDataHor.ListtouchingParts.Contains(SupDataInc1.SuppoId) &&
                                SupDataVer.ListtouchingParts.Contains(SupDataInc1.SuppoId) && SupDataVer.ListtouchingParts.Contains(SupDataInc1.SuppoId) && InclinedAngle == 2)
                            {
                                if (GetTouchingPrimaryCount(GetPartbyId(DicMidistPt.ElementAt(0).Key, SupData.ListSecondrySuppo).ListtouchingParts) == 7)
                                {
                                    SupData.SupportType = "Support92";
                                    return true;
                                }
                                else if (GetTouchingPrimaryCount(GetPartbyId(DicMidistPt.ElementAt(0).Key, SupData.ListSecondrySuppo).ListtouchingParts) == 4)
                                {
                                    SupData.SupportType = "Support93";
                                    return true;
                                }
                                else if (GetTouchingPrimaryCount(GetPartbyId(DicMidistPt.ElementAt(0).Key, SupData.ListSecondrySuppo).ListtouchingParts) == 0)
                                {
                                    SupData.SupportType = "Support70";
                                    return true;
                                }
                            }
                        }
                        else if (InclinedNev == 1 && Nev90Count == 1)
                        {
                            foreach (var SData in SupData.ListSecondrySuppo)
                            {
                                if (DicMidistPt.ElementAt(0).Key.Equals(SData.SuppoId))
                                {
                                    SData.PartDirection = "Hor";
                                    HorAngleCount++;
                                    SupDataHor = SData;
                                }
                                else if (SData.SuppoId != null && PosId.Contains(SData.SuppoId))
                                {
                                    if (Math.Abs(Math.Round(SData.Angle.XinDegree)).Equals(90))
                                    {
                                        SData.PartDirection = "Hor";
                                        HorAngleCount++;
                                    }
                                }
                                else if (SData.SuppoId != null && SData.SuppoId.Equals(VerPartId))
                                {
                                    SData.PartDirection = "Ver";
                                    SupDataVer = SData;
                                }
                                else if (SData.SuppoId != null && NevId.Contains(SData.SuppoId))
                                {
                                    if (Math.Abs(Math.Round(SData.Angle.XinDegree)).Equals(90))
                                    {
                                        SData.PartDirection = "Hor";

                                        HorAngleCount++;
                                    }
                                }

                                else if (SData.SuppoId != null && NevIncli.Contains(SData.SuppoId))
                                {
                                    if (Math.Abs(Math.Round(SData.Angle.XinDegree)).Equals(45) || Math.Abs(Math.Round(SData.Angle.XinDegree)).Equals(135))
                                    {
                                        SData.PartDirection = "45Inclined";
                                        InclinedAngle++;

                                        SupDataInc2 = SData;
                                    }
                                }
                            }

                            if (GetPartbyId(DicMidistPt.ElementAt(0).Key, SupData.ListSecondrySuppo).Boundingboxmax.Z > GetPartbyId(VerPartId, SupData.ListSecondrySuppo).Boundingboxmax.Z && GetPartbyId(NevIncli[0], SupData.ListSecondrySuppo).ListtouchingParts.Contains(NevId[0]) && GetPartbyId(NevIncli[0], SupData.ListSecondrySuppo).ListtouchingParts.Contains(VerPartId))
                            {
                                SupData.SupportType = "Support81";
                                return true;
                            }
                        }
                    }
                }
            }
            else if ((DicMidistPt == null || DicMidistPt.Count == 0) && CheckVectorsArePlaner(ListVecData))
            {
                if ((GetPartbyId(VerPartId, SupData.ListSecondrySuppo).Boundingboxmax.Z) <= (GetPartbyId(MaxValueID, SupData.ListSecondrySuppo).Boundingboxmax.Z))
                {
                    int Pos90Count = 0;
                    int Nev90Count = 0;
                    int InclinedPos = 0;
                    int InclinedNev = 0;
                    List<string> PosId = new List<string>();
                    List<string> NevId = new List<string>();
                    List<string> PosIncli = new List<string>();
                    List<string> NevIncli = new List<string>();
                    foreach (var data in DicAngleData)
                    {
                        if (Math.Round(data.Value.Angle).Equals(90))
                        {
                            Pos90Count++;
                            PosId.Add(data.Key);
                        }
                        else if (Math.Round(data.Value.Angle).Equals(-90))
                        {
                            Nev90Count++;
                            NevId.Add(data.Key);
                        }
                        else if (Math.Round(data.Value.Angle).Equals(-45))
                        {
                            NevIncli.Add(data.Key);
                            InclinedNev++;
                        }
                        else if (Math.Round(data.Value.Angle).Equals(45))
                        {
                            PosIncli.Add(data.Key);
                            InclinedPos++;
                        }
                    }

                    int HorAngleCount = 0;
                    int InclinedAngle = 0;

                    SupporSpecData SupDataHor = new SupporSpecData();
                    SupporSpecData SupDataVer = new SupporSpecData();
                    SupporSpecData SupDataInc1 = new SupporSpecData();
                    SupporSpecData SupDataInc2 = new SupporSpecData();
                    if (InclinedNev == 1 && InclinedPos == 0 || InclinedPos == 1 && InclinedNev == 0)
                    {
                        foreach (var SData in SupData.ListSecondrySuppo)
                        {
                            if (SData.SuppoId != null && PosId.Contains(SData.SuppoId))
                            {
                                if (Math.Abs(Math.Round(SData.Angle.XinDegree)).Equals(90))
                                {
                                    SData.PartDirection = "Hor";
                                    SupDataHor = SData;
                                    HorAngleCount++;
                                }
                            }
                            else if (SData.SuppoId != null && SData.SuppoId.Equals(VerPartId))
                            {
                                SData.PartDirection = "Ver";
                                SupDataVer = SData;
                            }
                            else if (SData.SuppoId != null && NevId.Contains(SData.SuppoId))
                            {
                                if (Math.Abs(Math.Round(SData.Angle.XinDegree)).Equals(90))
                                {
                                    SData.PartDirection = "Hor";
                                    HorAngleCount++;
                                }
                            }
                            else if (SData.SuppoId != null && PosIncli.Contains(SData.SuppoId))
                            {
                                if (Math.Abs(Math.Round(SData.Angle.XinDegree)).Equals(45) || Math.Abs(Math.Round(SData.Angle.XinDegree)).Equals(135))
                                {
                                    SData.PartDirection = "45Inclined";
                                    InclinedAngle++;

                                    SupDataInc1 = SData;
                                }
                            }
                            else if (SData.SuppoId != null && NevIncli.Contains(SData.SuppoId))
                            {
                                if (Math.Abs(Math.Round(SData.Angle.XinDegree)).Equals(45) || Math.Abs(Math.Round(SData.Angle.XinDegree)).Equals(135))
                                {
                                    SData.PartDirection = "45Inclined";
                                    InclinedAngle++;

                                    SupDataInc2 = SData;
                                }
                            }
                        }

                        if (SupDataHor.ListtouchingParts.Contains(SupDataInc1.SuppoId) && SupDataHor.ListtouchingParts.Contains(SupDataInc1.SuppoId) ||
                           (SupDataVer.ListtouchingParts.Contains(SupDataInc2.SuppoId) && SupDataVer.ListtouchingParts.Contains(SupDataInc2.SuppoId)) && InclinedAngle == 1)
                        {
                            SupData.SupportType = "Support82";
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        int GetTouchingPrimaryCount(List<string> ListTouchingPartData)
        {
            int PTouchingParts = 0;

            foreach (string Id in ListTouchingPartData)
            {
                if (Id.Contains("P"))
                {
                    PTouchingParts++;
                }
            }

            return PTouchingParts;
        }

        bool CheckforTypeSupport36(ref SupportData SupData)
        {
            Dictionary<int, SupporSpecData> DicIndexData = new Dictionary<int, SupporSpecData>();

            DicIndexData = GetAllISMC(SupData);

            List<SupporSpecData> ListIsmc = new List<SupporSpecData>();
            List<SupporSpecData> ListEmpty = new List<SupporSpecData>();
            foreach (var Part in DicIndexData)
            {
                ListIsmc.Add(Part.Value);
            }
            List<string> ListParSup = new List<string>();
            List<List<string>> CombinedParSupp = new List<List<string>>();

            ListParSup = Checkandgetparllelsupports(ref SupData, ref CombinedParSupp);

            Dictionary<string, double> DicMidistPt = new Dictionary<string, double>();
            DicMidistPt = CheckforMidDist(SupData);

            Dictionary<string, double> DicDistConSup = new Dictionary<string, double>();
            DicDistConSup = GetminDistFromConcrete(SupData);

            string Id = GetMinDistFromDic(DicDistConSup);

            string VerPartId = "";
            foreach (var Part in CombinedParSupp)
            {
                if (Part.Count == 1)
                {
                    if (Part[0] == Id)
                    {
                        VerPartId = Id;
                        break;
                    }
                }
            }

            if (VerPartId.Length < 1)
            {
                return false;
            }

            Dictionary<string, MinPtDist> DicAngleData = new Dictionary<string, MinPtDist>();

            List<Vector3D> ListVecData = new List<Vector3D>();

            DicAngleData = GetAngleBetweenSupport(VerPartId, SupData, ref ListVecData);

            double maxValue = 0;
            string MaxValueID = "";

            foreach (var DicData in DicDistConSup)
            {
                if (DicData.Value > maxValue)
                {
                    maxValue = DicData.Value;
                    MaxValueID = DicData.Key;
                }
            }

            if ((GetPartbyId(VerPartId, SupData.ListSecondrySuppo).Boundingboxmax.Z) <= (GetPartbyId(MaxValueID, SupData.ListSecondrySuppo).Boundingboxmax.Z))
            {
                if ((DicMidistPt == null || DicMidistPt.Count == 1) && CheckVectorsArePlaner(ListVecData))
                {
                    int Pos90Count = 0;
                    int Nev90Count = 0;
                    List<string> PosId = new List<string>();
                    List<string> NevId = new List<string>();

                    foreach (var data in DicAngleData)
                    {
                        if (DicMidistPt.ElementAt(0).Key == data.Key)
                        {
                            continue;
                        }
                        if (Math.Round(data.Value.Angle).Equals(90))
                        {
                            Pos90Count++;
                            PosId.Add(data.Key);
                        }
                        else if (Math.Round(data.Value.Angle).Equals(-90))
                        {
                            Nev90Count++;
                            NevId.Add(data.Key);
                        }
                    }

                    int HorAngleCount = 0;
                    if (Pos90Count == 2)
                    {
                        foreach (var SData in SupData.ListSecondrySuppo)
                        {
                            if (SData.SuppoId != null && PosId.Contains(SData.SuppoId))
                            {
                                if (Math.Abs(Math.Round(SData.Angle.XinDegree)).Equals(90))
                                {
                                    SData.PartDirection = "Hor";
                                    HorAngleCount++;
                                }
                            }
                            else if (SData.SuppoId != null && SData.SuppoId.Equals(VerPartId))
                            {
                                SData.PartDirection = "Ver";
                            }
                            else if (SData.SuppoId != null && NevId.Contains(SData.SuppoId))
                            {
                                if (Math.Abs(Math.Round(SData.Angle.XinDegree)).Equals(90))
                                {
                                    SData.PartDirection = "Hor";
                                    HorAngleCount++;
                                }
                            }
                            else if (SData.SuppoId != null && DicMidistPt.ElementAt(0).Key.Equals(SData.SuppoId))
                            {
                                SData.PartDirection = "Hor";
                                HorAngleCount++;
                            }
                        }

                        if (HorAngleCount == 3)
                        {
                            SupData.SupportType = "Support36";

                            return true;
                        }
                    }
                }
            }

            return false;
        }

        bool CheckforTypeSupport64(ref SupportData SupData)
        {
            Dictionary<int, SupporSpecData> DicIndexData = new Dictionary<int, SupporSpecData>();

            DicIndexData = GetAllISMC(SupData);

            List<SupporSpecData> ListIsmc = new List<SupporSpecData>();
            List<SupporSpecData> ListEmpty = new List<SupporSpecData>();
            foreach (var Part in DicIndexData)
            {
                ListIsmc.Add(Part.Value);
            }
            List<string> ListParSup = new List<string>();
            List<List<string>> CombinedParSupp = new List<List<string>>();

            ListParSup = Checkandgetparllelsupports(ref SupData, ref CombinedParSupp);

            Dictionary<string, double> DicMidistPt = new Dictionary<string, double>();
            DicMidistPt = CheckforMidDist(SupData);

            Dictionary<string, double> DicDistConSup = new Dictionary<string, double>();
            DicDistConSup = GetminDistFromConcrete(SupData);

            string Id = GetMinDistFromDic(DicDistConSup);

            string VerPartId = "";
            foreach (var Part in CombinedParSupp)
            {
                if (Part.Count == 1)
                {
                    if (Part[0] == Id)
                    {
                        VerPartId = Id;
                        break;
                    }
                }
            }

            if (VerPartId.Length < 1)
            {
                return false;
            }

            Dictionary<string, MinPtDist> DicAngleData = new Dictionary<string, MinPtDist>();

            List<Vector3D> ListVecData = new List<Vector3D>();

            DicAngleData = GetAngleBetweenSupport(VerPartId, SupData, ref ListVecData);

            double maxValue = 0;
            string MaxValueID = "";

            foreach (var DicData in DicDistConSup)
            {
                if (DicData.Value > maxValue)
                {
                    maxValue = DicData.Value;
                    MaxValueID = DicData.Key;
                }
            }

            if ((GetPartbyId(MaxValueID, SupData.ListSecondrySuppo).Boundingboxmax.Z) <= (GetPartbyId(VerPartId, SupData.ListSecondrySuppo).Boundingboxmax.Z))
            {
                if ((DicMidistPt == null || DicMidistPt.Count == 0) && CheckVectorsArePlaner(ListVecData))
                {
                    int Pos90Count = 0;
                    int Nev90Count = 0;
                    List<string> PosId = new List<string>();
                    List<string> NevId = new List<string>();

                    foreach (var data in DicAngleData)
                    {
                        if (Math.Round(data.Value.Angle).Equals(90))
                        {
                            Pos90Count++;
                            PosId.Add(data.Key);
                        }
                        else if (Math.Round(data.Value.Angle).Equals(-90))
                        {
                            Nev90Count++;
                            NevId.Add(data.Key);
                        }
                    }

                    int HorAngleCount = 0;

                    // Getting for the 69 type 97 and 98 type
                    string HorId = "";
                    List<string> HorIDs = new List<string>();
                    if (Pos90Count == 2 || Nev90Count == 2 || Pos90Count == 3 || (Pos90Count == 2 && Nev90Count == 1) || Pos90Count == 1)
                    {
                        foreach (var SData in SupData.ListSecondrySuppo)
                        {
                            if (SData.SuppoId != null && PosId.Contains(SData.SuppoId))
                            {
                                if (Math.Abs(Math.Round(SData.Angle.XinDegree)).Equals(90))
                                {
                                    SData.PartDirection = "Hor";
                                    HorAngleCount++;
                                    HorId = SData.SuppoId;
                                    HorIDs.Add(SData.SuppoId);
                                }
                            }
                            else if (SData.SuppoId != null && SData.SuppoId.Equals(VerPartId))
                            {
                                SData.PartDirection = "Ver";
                            }
                            else if (SData.SuppoId != null && NevId.Contains(SData.SuppoId))
                            {
                                if (Math.Abs(Math.Round(SData.Angle.XinDegree)).Equals(90))
                                {
                                    SData.PartDirection = "Hor";
                                    HorAngleCount++;
                                    HorIDs.Add(SData.SuppoId);
                                }
                            }
                        }

                        if (Pos90Count == 2 && Nev90Count == 1 && ListIsmc.Count == 4 && HorAngleCount == 3)
                        {
                            if (SupData.ListPrimarySuppo.Count == 0)
                            {
                                SupData.SupportType = "Support66";
                                return true;
                            }
                            else if (SupData.ListPrimarySuppo.Count == 3 && HorIDs.Count == 3)
                            {
                                if (HorIDs[0] != null && HorIDs[0].Length > 0 && GetTouchingPrimaryCount(GetPartbyId(HorIDs[0], SupData.ListSecondrySuppo).ListtouchingParts) == 1 && HorIDs[1] != null && HorIDs[1].Length > 0 && GetTouchingPrimaryCount(GetPartbyId(HorIDs[1], SupData.ListSecondrySuppo).ListtouchingParts) == 1 && HorIDs[2] != null && HorIDs[2].Length > 0 && GetTouchingPrimaryCount(GetPartbyId(HorIDs[2], SupData.ListSecondrySuppo).ListtouchingParts) == 1)
                                {
                                    SupData.SupportType = "Support106";
                                    return true;
                                }
                            }
                        }
                        else if (Pos90Count == 2 && Nev90Count == 1 && ListIsmc.Count == 4 && HorAngleCount == 3)
                        {
                            SupData.SupportType = "Support67";
                            return true;
                        }
                        else if (Pos90Count == 3 && ListIsmc.Count == 4 && HorAngleCount == 3)
                        {
                            SupData.SupportType = "Support65";
                            return true;
                        }
                        /* else if (Nev90Count == 3 && ListIsmc.Count == 4)
                         {
                             SupData.SupportType = "Support69";
                             return true;
                         }*/
                        if (Nev90Count == 2 && ListIsmc.Count == 3 && HorAngleCount == 2)
                        {
                            if (SupData.ListPrimarySuppo.Count == 0)
                            {
                                SupData.SupportType = "Support63";
                                return true;
                            }
                            else if (SupData.ListPrimarySuppo.Count == 2 && HorIDs.Count == 2)
                            {
                                if (HorIDs[0] != null && HorIDs[0].Length > 0 && GetTouchingPrimaryCount(GetPartbyId(HorIDs[0], SupData.ListSecondrySuppo).ListtouchingParts) == 1 && HorIDs[1] != null && HorIDs[1].Length > 0 && GetTouchingPrimaryCount(GetPartbyId(HorIDs[1], SupData.ListSecondrySuppo).ListtouchingParts) == 1)
                                {
                                    SupData.SupportType = "Support103";
                                    return true;
                                }
                            }
                        }
                        else if (Pos90Count == 2 && ListIsmc.Count == 3 && HorAngleCount == 2)
                        {
                            if (SupData.ListPrimarySuppo.Count == 0)
                            {
                                SupData.SupportType = "Support64";
                                return true;
                            }
                            else if (SupData.ListPrimarySuppo.Count == 2 && HorIDs.Count == 2)
                            {
                                if (HorIDs[0] != null && HorIDs[0].Length > 0 && GetTouchingPrimaryCount(GetPartbyId(HorIDs[0], SupData.ListSecondrySuppo).ListtouchingParts) == 1 && HorIDs[1] != null && HorIDs[1].Length > 0 && GetTouchingPrimaryCount(GetPartbyId(HorIDs[1], SupData.ListSecondrySuppo).ListtouchingParts) == 1)
                                {
                                    SupData.SupportType = "Support102";
                                    return true;
                                }
                            }
                        }
                        else if (Pos90Count == 1 && ListIsmc.Count == 2 && HorAngleCount == 1)
                        {
                            if (HorId != null)
                            {

                                if (GetTouchingPrimaryCount(GetPartbyId(HorId, SupData.ListSecondrySuppo).ListtouchingParts) == 0)
                                {
                                    SupData.SupportType = "Support69";
                                    return true;
                                }
                                else if (GetTouchingPrimaryCount(GetPartbyId(HorId, SupData.ListSecondrySuppo).ListtouchingParts) == 6)
                                {
                                    SupData.SupportType = "Support97";
                                    return true;
                                }
                                else if (GetTouchingPrimaryCount(GetPartbyId(HorId, SupData.ListSecondrySuppo).ListtouchingParts) == 7)
                                {
                                    SupData.SupportType = "Support98";
                                    return true;
                                }
                            }
                        }

                    }

                }
            }
            else
            {
                if ((DicMidistPt == null || DicMidistPt.Count == 0) && CheckVectorsArePlaner(ListVecData))
                {
                    int Pos90Count = 0;
                    int Nev90Count = 0;
                    List<string> PosId = new List<string>();
                    List<string> NevId = new List<string>();

                    foreach (var data in DicAngleData)
                    {
                        if (Math.Round(data.Value.Angle).Equals(90))
                        {
                            Pos90Count++;
                            PosId.Add(data.Key);
                        }
                        else if (Math.Round(data.Value.Angle).Equals(-90))
                        {
                            Nev90Count++;
                            NevId.Add(data.Key);
                        }
                    }

                    int HorAngleCount = 0;

                    SupporSpecData DataHor = new SupporSpecData();
                    SupporSpecData DataVer = new SupporSpecData();
                    if (Pos90Count == 2 || Nev90Count == 2 || Pos90Count == 3 || (Pos90Count == 2 && Nev90Count == 1) || Pos90Count == 1)
                    {
                        foreach (var SData in SupData.ListSecondrySuppo)
                        {
                            if (SData.SuppoId != null && PosId.Contains(SData.SuppoId))
                            {
                                if (Math.Abs(Math.Round(SData.Angle.XinDegree)).Equals(90))
                                {
                                    DataHor = SData;
                                    SData.PartDirection = "Hor";
                                    HorAngleCount++;
                                }
                            }
                            else if (SData.SuppoId != null && SData.SuppoId.Equals(VerPartId))
                            {
                                SData.PartDirection = "Ver";
                                DataVer = SData;
                            }
                            else if (SData.SuppoId != null && NevId.Contains(SData.SuppoId))
                            {
                                if (Math.Abs(Math.Round(SData.Angle.XinDegree)).Equals(90))
                                {
                                    SData.PartDirection = "Hor";
                                    HorAngleCount++;
                                }
                            }
                        }

                        if (Pos90Count == 1 && ListIsmc.Count == 2 && HorAngleCount == 1)
                        {
                            if (DataHor != null && DataVer != null && DataHor.Angle != null && DataVer.Angle != null && Math.Abs(Math.Round(DataHor.Angle.ZinDegree - DataVer.Angle.ZinDegree)).Equals(180))
                            {
                                SupData.SupportType = "Support70";
                                return true;
                            }
                        }

                    }

                }
            }

            return false;
        }
        bool CheckforTypeSupport60(ref SupportData SupData)
        {
            if (GetConcretePlateCount(ref SupData).Equals(3))
            {
                Dictionary<int, SupporSpecData> DicIndexData = new Dictionary<int, SupporSpecData>();

                DicIndexData = GetAllISMC(SupData);

                List<SupporSpecData> ListIsmc = new List<SupporSpecData>();
                List<SupporSpecData> ListEmpty = new List<SupporSpecData>();
                foreach (var Part in DicIndexData)
                {
                    ListIsmc.Add(Part.Value);
                }

                if (ListIsmc.Count == 2)
                {
                    if (Math.Abs(Math.Round(SupData.ListSecondrySuppo[0].Angle.XinDegree)).Equals(90) && (Math.Abs(Math.Round(SupData.ListSecondrySuppo[1].Angle.XinDegree))).Equals(45) || (Math.Abs(Math.Round(SupData.ListSecondrySuppo[1].Angle.XinDegree)).Equals(135)))
                    {
                        int CplateCount = 0;
                        if ((SupData.ListSecondrySuppo[0].ListtouchingParts.Count == 6 && GetTouchingPrimaryCount(SupData.ListSecondrySuppo[0].ListtouchingParts) == 3)
                            || (SupData.ListSecondrySuppo[0].ListtouchingParts.Count == 7 && GetTouchingPrimaryCount(SupData.ListSecondrySuppo[0].ListtouchingParts) == 4) || (SupData.ListSecondrySuppo[0].ListtouchingParts.Count == 3 || (SupData.ListSecondrySuppo[0].ListtouchingParts.Count == 7 && GetTouchingPrimaryCount(SupData.ListSecondrySuppo[0].ListtouchingParts) == 4) || (SupData.ListSecondrySuppo[0].ListtouchingParts.Count == 3 || (SupData.ListSecondrySuppo[0].ListtouchingParts.Count == 5 && GetTouchingPrimaryCount(SupData.ListSecondrySuppo[0].ListtouchingParts) == 2)) || (SupData.ListSecondrySuppo[0].ListtouchingParts.Count == 4 && GetTouchingPrimaryCount(SupData.ListSecondrySuppo[0].ListtouchingParts) == 1)))

                        {
                            foreach (string Id in SupData.ListSecondrySuppo[0].ListtouchingParts)
                            {
                                if (Id.ToUpper().Contains("C"))
                                {
                                    CplateCount++;
                                }
                            }
                        }
                        if (CplateCount == 2 && Math.Round(SupData.ListSecondrySuppo[0].Angle.ZinDegree).Equals(Math.Round(SupData.ListSecondrySuppo[1].Angle.ZinDegree)))
                        {
                            if (SupData.ListSecondrySuppo[0].ListtouchingParts.Count == 3)
                            {
                                SupData.SupportType = "Support53";
                                return true;
                            }
                            else if (SupData.ListSecondrySuppo[0].ListtouchingParts.Count == 4)
                            {
                                SupData.SupportType = "Support101";
                                return true;
                            }
                            else if (SupData.ListSecondrySuppo[0].ListtouchingParts.Count == 5)
                            {
                                SupData.SupportType = "Support99";
                                return true;
                            }
                            else if (SupData.ListSecondrySuppo[0].ListtouchingParts.Count == 6)
                            {
                                SupData.SupportType = "Support87";
                                return true;
                            }
                            else if (SupData.ListSecondrySuppo[0].ListtouchingParts.Count == 7)
                            {
                                SupData.SupportType = "Support89";
                                return true;
                            }
                        }

                        if (CplateCount == 2 && Math.Abs(Math.Round(SupData.ListSecondrySuppo[0].Angle.ZinDegree - SupData.ListSecondrySuppo[1].Angle.ZinDegree)).Equals(180))
                        {
                            if (SupData.ListSecondrySuppo[0].ListtouchingParts.Count == 3)
                            {
                                SupData.SupportType = "Support61";
                                return true;
                            }
                            else if (SupData.ListSecondrySuppo[0].ListtouchingParts.Count == 4)
                            {
                                SupData.SupportType = "Support94";
                                return true;
                            }
                            else if (SupData.ListSecondrySuppo[0].ListtouchingParts.Count == 5)
                            {
                                SupData.SupportType = "Support100";
                                return true;
                            }
                        }
                    }
                    else if (Math.Abs(Math.Round(SupData.ListSecondrySuppo[1].Angle.XinDegree)).Equals(90) && (Math.Abs(Math.Round(SupData.ListSecondrySuppo[0].Angle.XinDegree))).Equals(45) || (Math.Abs(Math.Round(SupData.ListSecondrySuppo[0].Angle.XinDegree)).Equals(135)))
                    {
                        int CplateCount = 0;
                        if ((SupData.ListSecondrySuppo[1].ListtouchingParts.Count == 6 && GetTouchingPrimaryCount(SupData.ListSecondrySuppo[1].ListtouchingParts) == 3)
                            || (SupData.ListSecondrySuppo[1].ListtouchingParts.Count == 7 && GetTouchingPrimaryCount(SupData.ListSecondrySuppo[1].ListtouchingParts) == 4) || (SupData.ListSecondrySuppo[1].ListtouchingParts.Count == 3))
                        {
                            foreach (string Id in SupData.ListSecondrySuppo[1].ListtouchingParts)
                            {
                                if (Id.ToUpper().Contains("C"))
                                {
                                    CplateCount++;
                                }
                            }
                        }
                        if (CplateCount == 2 && Math.Round(SupData.ListSecondrySuppo[0].Angle.ZinDegree).Equals(Math.Round(SupData.ListSecondrySuppo[1].Angle.ZinDegree)))
                        {
                            if (SupData.ListSecondrySuppo[1].ListtouchingParts.Count == 3)
                            {
                                SupData.SupportType = "Support53";
                                return true;
                            }
                            else if (SupData.ListSecondrySuppo[1].ListtouchingParts.Count == 4)
                            {
                                SupData.SupportType = "Support101";
                                return true;
                            }
                            else if (SupData.ListSecondrySuppo[1].ListtouchingParts.Count == 5)
                            {
                                SupData.SupportType = "Support99";
                                return true;
                            }
                            else if (SupData.ListSecondrySuppo[1].ListtouchingParts.Count == 6)
                            {
                                SupData.SupportType = "Support87";
                                return true;
                            }
                            else if (SupData.ListSecondrySuppo[1].ListtouchingParts.Count == 7)
                            {
                                SupData.SupportType = "Support89";
                                return true;
                            }
                        }
                        if (CplateCount == 2 && Math.Abs(Math.Round(SupData.ListSecondrySuppo[0].Angle.ZinDegree - SupData.ListSecondrySuppo[1].Angle.ZinDegree)).Equals(180))
                        {
                            if (SupData.ListSecondrySuppo[0].ListtouchingParts.Count == 3)
                            {
                                SupData.SupportType = "Support61";
                                return true;
                            }
                            else if (SupData.ListSecondrySuppo[0].ListtouchingParts.Count == 4)
                            {
                                SupData.SupportType = "Support94";
                                return true;
                            }
                            else if (SupData.ListSecondrySuppo[0].ListtouchingParts.Count == 4)
                            {
                                SupData.SupportType = "Support100";
                                return true;
                            }
                        }
                    }

                }
            }

            return false;
        }

        int GetConcretePlateCount(ref SupportData SupData)
        {
            int Platecount = 0;

            foreach (var SSuppo in SupData.ListConcreteData)
            {
                if (SSuppo != null && SSuppo.SupportName != null && SSuppo.SupportName.ToLower().Equals("plate"))
                {
                    Platecount++;
                }
            }
            return Platecount;
        }

        bool CheckforTypeSupport58(ref SupportData SupData)
        {
            if (GetAllISMC(SupData).Count == 1 && Math.Abs(Math.Round(GetAllISMC(SupData)[0].Angle.XinDegree)).Equals(90))
            {
                if (SupData.ListPrimarySuppo.Count > 0)
                {
                    SupData.SupportType = "Support108";
                    return true;
                }
                else
                {
                    SupData.SupportType = "Support59";
                    return true;
                }
            }

            return false;
        }

        bool CheckforTypeSupport54(ref SupportData SupData)
        {
            Dictionary<int, SupporSpecData> DicIndexData = new Dictionary<int, SupporSpecData>();

            DicIndexData = GetAllISMC(SupData);

            List<SupporSpecData> ListIsmc = new List<SupporSpecData>();
            List<SupporSpecData> ListEmpty = new List<SupporSpecData>();
            foreach (var Part in DicIndexData)
            {
                ListIsmc.Add(Part.Value);
            }

            string Orientation = "";

            if (CheckCentroidInLine(GetDicCentroidBottomPart(SupData), GetDicCentroidPrimaryPart(SupData), GetCentroidFromList(ListIsmc), ref Orientation))
            {

                if (ListIsmc.Count == 2)
                {
                    int MaxHorIndex = 0;
                    int MinVerIndex = 0;
                    if (ListIsmc[0].Centroid.Z > ListIsmc[1].Centroid.Z)
                    {
                        MaxHorIndex = 0;
                        MinVerIndex = 1;
                    }
                    else
                    {
                        MaxHorIndex = 1;
                        MinVerIndex = 0;
                    }

                    if (Math.Round(ListIsmc[MaxHorIndex].Angle.ZinDegree).Equals(Math.Round(ListIsmc[MinVerIndex].Angle.ZinDegree)) && Math.Abs(Math.Round(ListIsmc[MaxHorIndex].Angle.XinDegree)).Equals(90) &&
                        Math.Abs(Math.Round(ListIsmc[MaxHorIndex].Angle.XinDegree - ListIsmc[MinVerIndex].Angle.ZinDegree)).Equals(90))
                    {
                        GetPartbyId(ListIsmc[MaxHorIndex].SuppoId, SupData.ListSecondrySuppo).PartDirection = "Hor";
                        GetPartbyId(ListIsmc[MaxHorIndex].SuppoId, SupData.ListSecondrySuppo).PartDirection = "Ver";
                        SupData.SupportType = "Support54";
                        return true;
                    }

                }
            }
            else
            {
                if (ListIsmc.Count == 2)
                {
                    int MaxHorIndex = 0;
                    int MinVerIndex = 0;
                    if (ListIsmc[0].Centroid.Z > ListIsmc[1].Centroid.Z)
                    {
                        MaxHorIndex = 0;
                        MinVerIndex = 1;
                    }
                    else
                    {
                        MaxHorIndex = 1;
                        MinVerIndex = 0;
                    }

                    if (Math.Round(ListIsmc[MaxHorIndex].Angle.ZinDegree).Equals(Math.Round(ListIsmc[MinVerIndex].Angle.ZinDegree)) && Math.Abs(Math.Round(ListIsmc[MaxHorIndex].Angle.XinDegree)).Equals(90) &&
                        Math.Abs(Math.Round(ListIsmc[MaxHorIndex].Angle.XinDegree - ListIsmc[MinVerIndex].Angle.XinDegree)).Equals(90))
                    {
                        double Dist1 = 0;
                        double Dist2 = 0;

                        if (ListIsmc[MaxHorIndex].StPt != null && ListIsmc[MinVerIndex].StPt != null && ListIsmc[MinVerIndex].EndPt != null)
                        {

                            Dist1 = Calculate.DistPoint(GetPt3DFromArray(ListIsmc[MaxHorIndex].StPt), GetPt3DFromArray(ListIsmc[MinVerIndex].StPt));

                            Dist2 = Calculate.DistPoint(GetPt3DFromArray(ListIsmc[MaxHorIndex].StPt), GetPt3DFromArray(ListIsmc[MinVerIndex].EndPt));


                            if (Dist1 != Dist2)
                            {
                                Vector3D Vec1 = GetVector(GetPt3DFromArray(ListIsmc[MinVerIndex].StPt), GetPt3DFromArray(ListIsmc[MinVerIndex].EndPt));

                                Vector3D Vec2 = GetVector(GetPt3DFromArray(ListIsmc[MaxHorIndex].StPt), GetPt3DFromArray(ListIsmc[MinVerIndex].EndPt));

                                Vector3D Vec3 = new Vector3D(0, 0, 1);

                                Vector3D Vec5 = GetVector(GetPt3DFromArray(ListIsmc[MaxHorIndex].EndPt), GetPt3DFromArray(ListIsmc[MinVerIndex].EndPt));

                                double RotationAngle1 = Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(Vec1, Vec2, Vec3));

                                double RotationAngle2 = Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(Vec1, Vec5, Vec3));

                                if (RotationAngle1 < 0 && RotationAngle2 > 0 && Math.Abs(RotationAngle1) > Math.Abs(RotationAngle2))
                                {
                                    GetPartbyId(ListIsmc[MaxHorIndex].SuppoId, SupData.ListSecondrySuppo).PartDirection = "Hor";
                                    GetPartbyId(ListIsmc[MinVerIndex].SuppoId, SupData.ListSecondrySuppo).PartDirection = "Ver";
                                    SupData.SupportType = "Support55";
                                    return true;
                                }
                                else if (RotationAngle1 < 0 && RotationAngle2 > 0 && Math.Abs(RotationAngle1) < Math.Abs(RotationAngle2))
                                {

                                    GetPartbyId(ListIsmc[MaxHorIndex].SuppoId, SupData.ListSecondrySuppo).PartDirection = "Hor";
                                    GetPartbyId(ListIsmc[MinVerIndex].SuppoId, SupData.ListSecondrySuppo).PartDirection = "Ver";
                                    SupData.SupportType = "Support56";
                                    return true;
                                }
                            }
                        }

                    }

                }
            }
            return false;
        }
        bool CheckforTypeSupport50(ref SupportData SupData)
        {
            Dictionary<int, SupporSpecData> DicIndexData = new Dictionary<int, SupporSpecData>();

            DicIndexData = GetAllISMC(SupData);

            List<SupporSpecData> ListIsmc = new List<SupporSpecData>();
            List<SupporSpecData> ListEmpty = new List<SupporSpecData>();
            foreach (var Part in DicIndexData)
            {
                ListIsmc.Add(Part.Value);
            }

            string Orientation = "";


            List<Pt3D> PrimPartCent = GetDicCentroidPrimaryPart(SupData);

            if (SupData.ListPrimarySuppo.Count > 1)
            {
                PrimPartCent = new List<Pt3D>();
            }

            if (CheckCentroidInLine(GetDicCentroidBottomPart(SupData), PrimPartCent, GetCentroidFromList(ListIsmc), ref Orientation))
            {
                List<string> ListParSup = new List<string>();
                List<List<string>> CombinedParSupp = new List<List<string>>();

                ListParSup = Checkandgetparllelsupports(ref SupData, ref CombinedParSupp);

                Dictionary<string, double> DicMidistPt = new Dictionary<string, double>();
                DicMidistPt = CheckforMidDist(SupData);

                Dictionary<string, double> DicDistConSup = new Dictionary<string, double>();
                DicDistConSup = GetminDistFromConcrete(SupData);

                string Id = GetMinDistFromDic(DicDistConSup);

                string VerPartId = "";
                foreach (var Part in CombinedParSupp)
                {
                    if (Part.Count == 1)
                    {
                        if (Part[0] == Id)
                        {
                            VerPartId = Id;
                            break;
                        }
                    }
                }

                if (VerPartId.Length < 1)
                {
                    return false;
                }

                if (CombinedParSupp.Count == 2 && DicMidistPt.Count == 1 && GetPartbyId(DicMidistPt.ElementAt(0).Key, SupData.ListSecondrySuppo) != null && GetPartbyId(VerPartId, SupData.ListSecondrySuppo) != null && Math.Abs(Math.Round(GetPartbyId(DicMidistPt.ElementAt(0).Key, SupData.ListSecondrySuppo).Angle.XinDegree)).Equals(90)
                  && Math.Round(GetPartbyId(VerPartId, SupData.ListSecondrySuppo).Angle.ZinDegree).Equals(Math.Round(GetPartbyId(DicMidistPt.ElementAt(0).Key, SupData.ListSecondrySuppo).Angle.ZinDegree)) &&
                  GetPartbyId(VerPartId, SupData.ListSecondrySuppo).Boundingboxmax.Z < GetPartbyId(DicMidistPt.ElementAt(0).Key, SupData.ListSecondrySuppo).Boundingboxmax.Z)
                {
                    GetPartbyId(VerPartId, SupData.ListSecondrySuppo).PartDirection = "Ver";

                    GetPartbyId(DicMidistPt.ElementAt(0).Key, SupData.ListSecondrySuppo).PartDirection = "Hor";

                    if (GetGussetCout(ref SupData) == 0)
                    {
                        if (GetTouchingPrimaryCount(GetPartbyId(DicMidistPt.ElementAt(0).Key, SupData.ListSecondrySuppo).ListtouchingParts) > 2)
                        {
                            SupData.SupportType = "Support96";
                            return true;
                        }
                        else if (GetTouchingPrimaryCount(GetPartbyId(DicMidistPt.ElementAt(0).Key, SupData.ListSecondrySuppo).ListtouchingParts) == 2)
                        {
                            SupData.SupportType = "Support95";
                            return true;
                        }
                        else if (GetTouchingPrimaryCount(GetPartbyId(DicMidistPt.ElementAt(0).Key, SupData.ListSecondrySuppo).ListtouchingParts) == 0)
                        {
                            SupData.SupportType = "Support83";
                            return true;
                        }
                    }
                    else
                    {
                        SupData.SupportType = "Support50";
                        return true;
                    }

                }
                if (CombinedParSupp.Count == 2 && DicMidistPt.Count == 1 && GetPartbyId(DicMidistPt.ElementAt(0).Key, SupData.ListSecondrySuppo) != null && GetPartbyId(VerPartId, SupData.ListSecondrySuppo) != null && Math.Abs(Math.Round(GetPartbyId(DicMidistPt.ElementAt(0).Key, SupData.ListSecondrySuppo).Angle.XinDegree)).Equals(90)
                  && Math.Abs(Math.Round(GetPartbyId(VerPartId, SupData.ListSecondrySuppo).Angle.ZinDegree - GetPartbyId(DicMidistPt.ElementAt(0).Key, SupData.ListSecondrySuppo).Angle.ZinDegree)).Equals(180) &&
                  GetPartbyId(VerPartId, SupData.ListSecondrySuppo).Boundingboxmax.Z < GetPartbyId(DicMidistPt.ElementAt(0).Key, SupData.ListSecondrySuppo).Boundingboxmax.Z)
                {
                    GetPartbyId(VerPartId, SupData.ListSecondrySuppo).PartDirection = "Ver";

                    GetPartbyId(DicMidistPt.ElementAt(0).Key, SupData.ListSecondrySuppo).PartDirection = "Hor";

                    SupData.SupportType = "Support68";
                    return true;

                }
                else if (CombinedParSupp.Count == 2 && DicMidistPt.Count == 1 && GetPartbyId(DicMidistPt.ElementAt(0).Key, SupData.ListSecondrySuppo) != null && GetPartbyId(VerPartId, SupData.ListSecondrySuppo) != null && Math.Abs(Math.Round(GetPartbyId(DicMidistPt.ElementAt(0).Key, SupData.ListSecondrySuppo).Angle.XinDegree)).Equals(90)
                  && Math.Round(GetPartbyId(VerPartId, SupData.ListSecondrySuppo).Angle.ZinDegree).Equals(Math.Round(GetPartbyId(DicMidistPt.ElementAt(0).Key, SupData.ListSecondrySuppo).Angle.ZinDegree)) && Math.Round(
                  GetPartbyId(VerPartId, SupData.ListSecondrySuppo).Boundingboxmax.Z).Equals(Math.Round(GetPartbyId(DicMidistPt.ElementAt(0).Key, SupData.ListSecondrySuppo).Boundingboxmax.Z)))
                {
                    GetPartbyId(VerPartId, SupData.ListSecondrySuppo).PartDirection = "Ver";

                    GetPartbyId(DicMidistPt.ElementAt(0).Key, SupData.ListSecondrySuppo).PartDirection = "Hor";

                    if (SupData.ListPrimarySuppo.Count == 0)
                    {
                        SupData.SupportType = "Support51";
                        return true;
                    }
                    else if (SupData.ListPrimarySuppo.Count > 0)
                    {
                        SupData.SupportType = "Support115";
                        return true;
                    }
                }
                else if (CombinedParSupp.Count == 2 && DicMidistPt.Count == 1 && GetPartbyId(DicMidistPt.ElementAt(0).Key, SupData.ListSecondrySuppo) != null && GetPartbyId(VerPartId, SupData.ListSecondrySuppo) != null && Math.Abs(Math.Round(GetPartbyId(DicMidistPt.ElementAt(0).Key, SupData.ListSecondrySuppo).Angle.XinDegree)).Equals(90)
                  && Math.Abs(Math.Round(GetPartbyId(VerPartId, SupData.ListSecondrySuppo).Angle.ZinDegree - GetPartbyId(DicMidistPt.ElementAt(0).Key, SupData.ListSecondrySuppo).Angle.ZinDegree)).Equals(180) && Math.Round(
                  GetPartbyId(VerPartId, SupData.ListSecondrySuppo).Boundingboxmax.Z).Equals(Math.Round(GetPartbyId(DicMidistPt.ElementAt(0).Key, SupData.ListSecondrySuppo).Boundingboxmax.Z)))
                {
                    bool PriamaryCond = true;

                    foreach (var PrimSup in SupData.ListPrimarySuppo)
                    {
                        if (PrimSup.ListtouchingParts.Count != 1 || !PrimSup.ListtouchingParts.Contains(DicMidistPt.ElementAt(0).Key))
                        {
                            PriamaryCond = false;
                        }
                    }


                    if (PriamaryCond)
                    {
                        if (SupData.ListPrimarySuppo.Count == 4)
                        {
                            SupData.SupportType = "Support72";
                            return true;
                        }
                        else if (SupData.ListPrimarySuppo.Count == 2)
                        {
                            SupData.SupportType = "Support73";
                            return true;
                        }

                    }
                }
                else if (SupData.ListPrimarySuppo.Count == 1 && CheckCentroidInLine(GetCentroidFromList(ListEmpty), GetDicCentroidPrimaryPart(SupData), GetCentroidFromList(ListIsmc), ref Orientation))
                {


                }

            }
            else
            {

            }
            return false;

        }

        bool CheckforTypeSupport85(ref SupportData SupData)
        {
            Dictionary<int, SupporSpecData> DicIndexData = new Dictionary<int, SupporSpecData>();

            DicIndexData = GetAllISMC(SupData);

            List<SupporSpecData> ListIsmc = new List<SupporSpecData>();
            List<SupporSpecData> ListEmpty = new List<SupporSpecData>();
            foreach (var Part in DicIndexData)
            {
                ListIsmc.Add(Part.Value);
            }

            string Orientation = "";


            List<Pt3D> PrimPartCent = GetDicCentroidPrimaryPart(SupData);

            if (SupData.ListPrimarySuppo.Count > 1)
            {
                PrimPartCent = new List<Pt3D>();
            }

            if (CheckCentroidInLine(GetDicCentroidBottomPart(SupData), PrimPartCent, GetCentroidFromList(ListIsmc), ref Orientation))
            {
                List<string> ListParSup = new List<string>();
                List<List<string>> CombinedParSupp = new List<List<string>>();

                ListParSup = Checkandgetparllelsupports(ref SupData, ref CombinedParSupp);

                Dictionary<string, double> DicMidistPt = new Dictionary<string, double>();
                DicMidistPt = CheckforMidDist(SupData);

                Dictionary<string, double> DicDistConSup = new Dictionary<string, double>();
                DicDistConSup = GetminDistFromConcrete(SupData);

                string Id = GetMinDistFromDic(DicDistConSup);

                string VerPartId = "";
                foreach (var Part in CombinedParSupp)
                {
                    if (Part.Count == 1)
                    {
                        if (Part[0] == Id)
                        {
                            VerPartId = Id;
                            break;
                        }
                    }
                }

                if (VerPartId.Length < 1)
                {
                    return false;
                }

                if (CombinedParSupp.Count == 2 && DicMidistPt.Count == 1 && GetPartbyId(DicMidistPt.ElementAt(0).Key, SupData.ListSecondrySuppo) != null && GetPartbyId(VerPartId, SupData.ListSecondrySuppo) != null && Math.Abs(Math.Round(GetPartbyId(DicMidistPt.ElementAt(0).Key, SupData.ListSecondrySuppo).Angle.XinDegree)).Equals(90)
                  && Math.Round(GetPartbyId(VerPartId, SupData.ListSecondrySuppo).Angle.ZinDegree).Equals(Math.Round(GetPartbyId(DicMidistPt.ElementAt(0).Key, SupData.ListSecondrySuppo).Angle.ZinDegree)) &&
                  GetPartbyId(VerPartId, SupData.ListSecondrySuppo).Boundingboxmax.Z < GetPartbyId(DicMidistPt.ElementAt(0).Key, SupData.ListSecondrySuppo).Boundingboxmax.Z)
                {
                    GetPartbyId(VerPartId, SupData.ListSecondrySuppo).PartDirection = "Ver";

                    GetPartbyId(DicMidistPt.ElementAt(0).Key, SupData.ListSecondrySuppo).PartDirection = "Hor";

                    if (GetPartbyId(DicMidistPt.ElementAt(0).Key, SupData.ListSecondrySuppo).ListtouchingParts.Count == 10
                        || GetPartbyId(DicMidistPt.ElementAt(0).Key, SupData.ListSecondrySuppo).ListtouchingParts.Count == 9 || GetPartbyId(DicMidistPt.ElementAt(0).Key, SupData.ListSecondrySuppo).ListtouchingParts.Count == 5
                        || GetPartbyId(DicMidistPt.ElementAt(0).Key, SupData.ListSecondrySuppo).ListtouchingParts.Count == 4 || GetPartbyId(DicMidistPt.ElementAt(0).Key, SupData.ListSecondrySuppo).ListtouchingParts.Count == 3)
                    {
                        if (GetTouchingPrimaryCount(GetPartbyId(DicMidistPt.ElementAt(0).Key, SupData.ListSecondrySuppo).ListtouchingParts) == 8
                            || GetTouchingPrimaryCount(GetPartbyId(DicMidistPt.ElementAt(0).Key, SupData.ListSecondrySuppo).ListtouchingParts) == 9 || GetTouchingPrimaryCount(GetPartbyId(DicMidistPt.ElementAt(0).Key, SupData.ListSecondrySuppo).ListtouchingParts) == 4
                            || GetTouchingPrimaryCount(GetPartbyId(DicMidistPt.ElementAt(0).Key, SupData.ListSecondrySuppo).ListtouchingParts) == 3 || GetTouchingPrimaryCount(GetPartbyId(DicMidistPt.ElementAt(0).Key, SupData.ListSecondrySuppo).ListtouchingParts) == 2)
                        {
                            if (GetGussetCout(ref SupData) == 0)
                            {
                                if (GetTouchingPrimaryCount(GetPartbyId(DicMidistPt.ElementAt(0).Key, SupData.ListSecondrySuppo).ListtouchingParts) == 4)
                                {
                                    SupData.SupportType = "Support96";
                                    return true;
                                }
                                else if (GetTouchingPrimaryCount(GetPartbyId(DicMidistPt.ElementAt(0).Key, SupData.ListSecondrySuppo).ListtouchingParts) == 2)
                                {
                                    SupData.SupportType = "Support95";
                                    return true;
                                }
                                else if (GetTouchingPrimaryCount(GetPartbyId(DicMidistPt.ElementAt(0).Key, SupData.ListSecondrySuppo).ListtouchingParts) == 0)
                                {
                                    SupData.SupportType = "Support83";
                                    return true;
                                }

                            }
                            else
                            {
                                if (GetTouchingPrimaryCount(GetPartbyId(DicMidistPt.ElementAt(0).Key, SupData.ListSecondrySuppo).ListtouchingParts) == 8)
                                {
                                    SupData.SupportType = "Support88";
                                    return true;
                                }
                                else if (GetTouchingPrimaryCount(GetPartbyId(DicMidistPt.ElementAt(0).Key, SupData.ListSecondrySuppo).ListtouchingParts) == 4)
                                {
                                    SupData.SupportType = "Support90";
                                    return true;
                                }
                                else if (GetTouchingPrimaryCount(GetPartbyId(DicMidistPt.ElementAt(0).Key, SupData.ListSecondrySuppo).ListtouchingParts) == 3)
                                {
                                    SupData.SupportType = "Support91";
                                    return true;
                                }
                            }
                        }
                    }
                }
                if (CombinedParSupp.Count == 2 && DicMidistPt.Count == 1 && GetPartbyId(DicMidistPt.ElementAt(0).Key, SupData.ListSecondrySuppo) != null && GetPartbyId(VerPartId, SupData.ListSecondrySuppo) != null && Math.Abs(Math.Round(GetPartbyId(DicMidistPt.ElementAt(0).Key, SupData.ListSecondrySuppo).Angle.XinDegree)).Equals(90)
                  && Math.Abs(Math.Round(GetPartbyId(VerPartId, SupData.ListSecondrySuppo).Angle.ZinDegree - GetPartbyId(DicMidistPt.ElementAt(0).Key, SupData.ListSecondrySuppo).Angle.ZinDegree)).Equals(180) &&
                  GetPartbyId(VerPartId, SupData.ListSecondrySuppo).Boundingboxmax.Z < GetPartbyId(DicMidistPt.ElementAt(0).Key, SupData.ListSecondrySuppo).Boundingboxmax.Z)
                {
                    GetPartbyId(VerPartId, SupData.ListSecondrySuppo).PartDirection = "Ver";

                    GetPartbyId(DicMidistPt.ElementAt(0).Key, SupData.ListSecondrySuppo).PartDirection = "Hor";

                    SupData.SupportType = "Support68";
                    return true;

                }
                else if (CombinedParSupp.Count == 2 && DicMidistPt.Count == 1 && GetPartbyId(DicMidistPt.ElementAt(0).Key, SupData.ListSecondrySuppo) != null && GetPartbyId(VerPartId, SupData.ListSecondrySuppo) != null && Math.Abs(Math.Round(GetPartbyId(DicMidistPt.ElementAt(0).Key, SupData.ListSecondrySuppo).Angle.XinDegree)).Equals(90)
                  && Math.Round(GetPartbyId(VerPartId, SupData.ListSecondrySuppo).Angle.ZinDegree).Equals(Math.Round(GetPartbyId(DicMidistPt.ElementAt(0).Key, SupData.ListSecondrySuppo).Angle.ZinDegree)) && Math.Round(
                  GetPartbyId(VerPartId, SupData.ListSecondrySuppo).Boundingboxmax.Z).Equals(Math.Round(GetPartbyId(DicMidistPt.ElementAt(0).Key, SupData.ListSecondrySuppo).Boundingboxmax.Z)))
                {
                    GetPartbyId(VerPartId, SupData.ListSecondrySuppo).PartDirection = "Ver";

                    GetPartbyId(DicMidistPt.ElementAt(0).Key, SupData.ListSecondrySuppo).PartDirection = "Hor";

                    if (SupData.ListPrimarySuppo.Count == 0)
                    {
                        SupData.SupportType = "Support51";
                        return true;
                    }
                    else if (SupData.ListPrimarySuppo.Count > 0)
                    {
                        SupData.SupportType = "Support115";
                        return true;
                    }
                }
                else if (CombinedParSupp.Count == 2 && DicMidistPt.Count == 1 && GetPartbyId(DicMidistPt.ElementAt(0).Key, SupData.ListSecondrySuppo) != null && GetPartbyId(VerPartId, SupData.ListSecondrySuppo) != null && Math.Abs(Math.Round(GetPartbyId(DicMidistPt.ElementAt(0).Key, SupData.ListSecondrySuppo).Angle.XinDegree)).Equals(90)
                  && Math.Abs(Math.Round(GetPartbyId(VerPartId, SupData.ListSecondrySuppo).Angle.ZinDegree - GetPartbyId(DicMidistPt.ElementAt(0).Key, SupData.ListSecondrySuppo).Angle.ZinDegree)).Equals(180) && Math.Round(
                  GetPartbyId(VerPartId, SupData.ListSecondrySuppo).Boundingboxmax.Z).Equals(Math.Round(GetPartbyId(DicMidistPt.ElementAt(0).Key, SupData.ListSecondrySuppo).Boundingboxmax.Z)))
                {
                    bool PriamaryCond = true;

                    foreach (var PrimSup in SupData.ListPrimarySuppo)
                    {
                        if (PrimSup.ListtouchingParts.Count != 1 || !PrimSup.ListtouchingParts.Contains(DicMidistPt.ElementAt(0).Key))
                        {
                            PriamaryCond = false;
                        }
                    }


                    if (PriamaryCond)
                    {
                        if (SupData.ListPrimarySuppo.Count == 4)
                        {
                            SupData.SupportType = "Support72";
                            return true;
                        }
                        else if (SupData.ListPrimarySuppo.Count == 2)
                        {
                            SupData.SupportType = "Support73";
                            return true;
                        }

                    }
                }
                else if (SupData.ListPrimarySuppo.Count == 1 && CheckCentroidInLine(GetCentroidFromList(ListEmpty), GetDicCentroidPrimaryPart(SupData), GetCentroidFromList(ListIsmc), ref Orientation))
                {


                }

            }
            else
            {
                if (ListIsmc.Count == 2)
                {
                    int MaxHorIndex = 0;
                    int MinVerIndex = 0;
                    if (ListIsmc[0].Centroid.Z > ListIsmc[1].Centroid.Z)
                    {
                        MaxHorIndex = 0;
                        MinVerIndex = 1;
                    }
                    else
                    {
                        MaxHorIndex = 1;
                        MinVerIndex = 0;
                    }

                    if (Math.Round(ListIsmc[MaxHorIndex].Angle.ZinDegree).Equals(Math.Round(ListIsmc[MinVerIndex].Angle.ZinDegree)) && Math.Abs(Math.Round(ListIsmc[MaxHorIndex].Angle.XinDegree)).Equals(90) &&
                        Math.Abs(Math.Round(ListIsmc[MaxHorIndex].Angle.XinDegree - ListIsmc[MinVerIndex].Angle.XinDegree)).Equals(90))
                    {
                        double Dist1 = 0;
                        double Dist2 = 0;

                        if (ListIsmc[MaxHorIndex].StPt != null && ListIsmc[MinVerIndex].StPt != null && ListIsmc[MinVerIndex].EndPt != null)
                        {

                            Dist1 = Calculate.DistPoint(GetPt3DFromArray(ListIsmc[MaxHorIndex].StPt), GetPt3DFromArray(ListIsmc[MinVerIndex].StPt));

                            Dist2 = Calculate.DistPoint(GetPt3DFromArray(ListIsmc[MaxHorIndex].StPt), GetPt3DFromArray(ListIsmc[MinVerIndex].EndPt));


                            if (Dist1 != Dist2)
                            {
                                Vector3D Vec1 = new Vector3D(1, 0, 0);

                                Vector3D Vec2 = new Vector3D(1, 0, 0);

                                Vector3D Vec3 = new Vector3D(0, 0, 1);

                                Vector3D Vec5 = new Vector3D(1, 0, 0);

                                if (Dist1 > Dist2)
                                {
                                    Vec1 = GetVector(GetPt3DFromArray(ListIsmc[MinVerIndex].StPt), GetPt3DFromArray(ListIsmc[MinVerIndex].EndPt));
                                    Vec2 = GetVector(GetPt3DFromArray(ListIsmc[MinVerIndex].StPt), GetPt3DFromArray(ListIsmc[MaxHorIndex].StPt));
                                    Vec5 = GetVector(GetPt3DFromArray(ListIsmc[MinVerIndex].StPt), GetPt3DFromArray(ListIsmc[MaxHorIndex].EndPt));
                                }
                                else
                                {
                                    Vec1 = GetVector(GetPt3DFromArray(ListIsmc[MinVerIndex].EndPt), GetPt3DFromArray(ListIsmc[MinVerIndex].StPt));
                                    Vec2 = GetVector(GetPt3DFromArray(ListIsmc[MinVerIndex].EndPt), GetPt3DFromArray(ListIsmc[MaxHorIndex].StPt));
                                    Vec5 = GetVector(GetPt3DFromArray(ListIsmc[MinVerIndex].EndPt), GetPt3DFromArray(ListIsmc[MaxHorIndex].EndPt));
                                }

                                double RotationAngle1 = Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(Vec1, Vec2, Vec3));

                                double RotationAngle2 = Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(Vec1, Vec5, Vec3));

                                if (Math.Abs(RotationAngle1) > Math.Abs(RotationAngle2) && VectorHasNegativeMem(Vec5) || VectorHasNegativeMem(Vec2) && Math.Abs(RotationAngle2) > Math.Abs(RotationAngle1))
                                {
                                    GetPartbyId(ListIsmc[MaxHorIndex].SuppoId, SupData.ListSecondrySuppo).PartDirection = "Hor";
                                    GetPartbyId(ListIsmc[MinVerIndex].SuppoId, SupData.ListSecondrySuppo).PartDirection = "Ver";
                                    if (GetTouchingPrimaryCount(GetPartbyId(ListIsmc[MaxHorIndex].SuppoId, SupData.ListSecondrySuppo).ListtouchingParts) == 9)
                                    {
                                        SupData.SupportType = "Support85";
                                    }
                                }
                            }
                        }

                    }

                }
            }
            return false;

        }

        bool VectorHasNegativeMem(Vector3D Vec3d)
        {
            if (Vec3d.X < 0 || Vec3d.Y < 0 || Vec3d.Z < 0)
            {
                return true;
            }
            return false;
        }

        SupporSpecData GetPartbyId(string Id, List<SupporSpecData> ListSuppo)
        {
            foreach (var Suppo in ListSuppo)
            {
                if (Id.Equals(Suppo.SuppoId))
                {
                    return Suppo;
                }
            }

            return null;
        }

        List<Pt3D> GetCentroidFromList(List<SupporSpecData> SData)
        {
            List<Pt3D> Centroids = new List<Pt3D>();
            if (SData.Count > 0)
            {
                foreach (var Part in SData)
                {
                    Centroids.Add(Part.Centroid);
                }
                return Centroids;
            }

            return null;
        }
        Dictionary<int, SupporSpecData> GetAllISMC(SupportData SupData)
        {
            Dictionary<int, SupporSpecData> DicIdndexIsmc = new Dictionary<int, SupporSpecData>();

            for (int inx = 0; inx < SupData.ListSecondrySuppo.Count; inx++)
            {
                if (SupData.ListSecondrySuppo[inx] != null && SupData.ListSecondrySuppo[inx].Size != null && SupData.ListSecondrySuppo[inx].SupportName != null && SupData.ListSecondrySuppo[inx].SupportName.ToLower().Contains("web") || SupData.ListSecondrySuppo[inx].Size.ToLower().Contains("web") || SupData.ListSecondrySuppo[inx].Size.ToLower().Contains("upe"))
                {
                    DicIdndexIsmc[inx] = SupData.ListSecondrySuppo[inx];
                }
            }
            return DicIdndexIsmc;
        }

        Dictionary<int, SupporSpecData> GetAllAngles(SupportData SupData)
        {
            Dictionary<int, SupporSpecData> DicIndexAngle = new Dictionary<int, SupporSpecData>();

            for (int inx = 0; inx < SupData.ListSecondrySuppo.Count; inx++)
            {
                if (SupData.ListSecondrySuppo[inx] != null && SupData.ListSecondrySuppo[inx].Size != null && SupData.ListSecondrySuppo[inx].SupportName != null && SupData.ListSecondrySuppo[inx].SupportName.ToLower().Contains("angle") || SupData.ListSecondrySuppo[inx].Size.ToLower().Contains("thck"))
                {
                    DicIndexAngle[inx] = SupData.ListSecondrySuppo[inx];
                }
            }
            return DicIndexAngle;
        }

        AngleDt ExtractAngleSize()
        {
            AngleDt angleDt = new AngleDt();

            return angleDt;
        }

        bool CheckforTypeSupport49(ref SupportData SupData)
        {
            int plateCount = 0;
            SupporSpecData NBPart = new SupporSpecData();
            List<SupporSpecData> ChannelC = new List<SupporSpecData>();

            List<SupporSpecData> AngleData = new List<SupporSpecData>();

            foreach (var Data in SupData.ListSecondrySuppo)
            {
                if (Data.SupportName != null && Data.SupportName.ToUpper().Equals("PLATE"))
                {
                    plateCount++;
                }
                else if (Data.Size != null && Data.Size.ToUpper().Contains("NB"))
                {
                    NBPart = Data;
                }
                else if (Data.Size != null && Data.Size.ToUpper().Contains("ISMC"))
                {
                    ChannelC.Add(Data);
                }
                else if (Data.Size != null && (Data.Size.ToUpper().Contains("L-") || Data.Size.ToUpper().Contains("ANGLE") || Data.Size.ToUpper().Contains("ISA")) && (!Data.Size.ToUpper().Contains("WEB")))
                {
                    AngleData.Add(Data);
                }
            }

            if (plateCount == 5 && NBPart != null && ChannelC != null && ChannelC.Count == 3 && AngleData != null && AngleData.Count == 1)
            {
                if (Math.Round(Math.Abs(ChannelC[0].Angle.XinDegree - NBPart.Angle.XinDegree)).Equals(90) && Math.Round(Math.Abs(ChannelC[1].Angle.XinDegree - NBPart.Angle.XinDegree)).Equals(90) && Math.Round(Math.Abs(ChannelC[2].Angle.XinDegree - NBPart.Angle.XinDegree)).Equals(90) && Math.Round(Math.Abs(AngleData[0].Angle.XinDegree - NBPart.Angle.XinDegree)).Equals(90))
                {
                    List<string> ListParSup = new List<string>();
                    List<List<string>> CombinedParSupp = new List<List<string>>();

                    ListParSup = Checkandgetparllelsupports(ref SupData, ref CombinedParSupp);

                    Dictionary<string, double> DicMidistPt = new Dictionary<string, double>();
                    DicMidistPt = CheckforMidDist(SupData);

                    Dictionary<string, double> DicDistConSup = new Dictionary<string, double>();
                    DicDistConSup = GetminDistFromConcrete(SupData);

                    string Id = GetMinDistFromDic(DicDistConSup);

                    string VerPartId = "";
                    foreach (var Part in CombinedParSupp)
                    {
                        if (Part.Count == 1)
                        {
                            if (Part[0] == Id)
                            {
                                VerPartId = Id;
                                break;
                            }
                        }
                    }

                    if (VerPartId.Length < 1)
                    {
                        return false;
                    }

                    Dictionary<string, MinPtDist> DicAngleData = new Dictionary<string, MinPtDist>();

                    List<Vector3D> ListVecData = new List<Vector3D>();

                    DicAngleData = GetAngleBetweenSupport(VerPartId, SupData, ref ListVecData);

                    if (Math.Round(DicAngleData[AngleData[0].SuppoId].Angle).Equals(-90) && Math.Round(DicAngleData[ChannelC[0].SuppoId].Angle).Equals(90) && Math.Round(DicAngleData[ChannelC[1].SuppoId].Angle).Equals(90) && Math.Round(DicAngleData[ChannelC[2].SuppoId].Angle).Equals(90))
                    {
                        if (DicAngleData[AngleData[0].SuppoId].Mindist > DicAngleData[ChannelC[0].SuppoId].Mindist && DicAngleData[AngleData[0].SuppoId].Mindist > DicAngleData[ChannelC[1].SuppoId].Mindist && DicAngleData[AngleData[0].SuppoId].Mindist > DicAngleData[ChannelC[2].SuppoId].Mindist)
                        {
                            if (GettouchigPrimCount(SupData, ChannelC[0]) == 1 && GettouchigPrimCount(SupData, ChannelC[1]) == 1 && GettouchigPrimCount(SupData, ChannelC[2]) == 1)
                            {
                                SupData.SupportType = "Support49";
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }
        bool CheckforTypeSupport47(ref SupportData SupData)
        {
            int plateCount = 0;
            SupporSpecData NBPart = new SupporSpecData();
            List<SupporSpecData> ChannelC = new List<SupporSpecData>();

            List<SupporSpecData> AngleData = new List<SupporSpecData>();

            foreach (var Data in SupData.ListSecondrySuppo)
            {
                if (Data.SupportName != null && Data.SupportName.ToUpper().Equals("PLATE"))
                {
                    plateCount++;
                }
                else if (Data.Size != null && Data.Size.ToUpper().Contains("NB"))
                {
                    NBPart = Data;
                }
                else if (Data.Size != null && Data.Size.ToUpper().Contains("ISMC"))
                {
                    ChannelC.Add(Data);
                }
                else if (Data.Size != null && (Data.Size.ToUpper().Contains("L-") || Data.Size.ToUpper().Contains("ANGLE") || Data.Size.ToUpper().Contains("ISA")) && (!Data.Size.ToUpper().Contains("WEB")))
                {
                    AngleData.Add(Data);
                }
            }

            if (plateCount == 5 && NBPart != null && ChannelC != null && ChannelC.Count == 3 && AngleData != null && AngleData.Count == 2)
            {
                if (Math.Round(Math.Abs(ChannelC[0].Angle.XinDegree - NBPart.Angle.XinDegree)).Equals(90) && Math.Round(Math.Abs(ChannelC[1].Angle.XinDegree - NBPart.Angle.XinDegree)).Equals(90) && Math.Round(Math.Abs(ChannelC[2].Angle.XinDegree - NBPart.Angle.XinDegree)).Equals(90) && Math.Round(Math.Abs(AngleData[0].Angle.XinDegree - NBPart.Angle.XinDegree)).Equals(90) && Math.Round(AngleData[0].Angle.XinDegree).Equals(Math.Round(AngleData[1].Angle.XinDegree)))
                {
                    List<string> ListParSup = new List<string>();
                    List<List<string>> CombinedParSupp = new List<List<string>>();

                    ListParSup = Checkandgetparllelsupports(ref SupData, ref CombinedParSupp);

                    Dictionary<string, double> DicMidistPt = new Dictionary<string, double>();
                    DicMidistPt = CheckforMidDist(SupData);

                    Dictionary<string, double> DicDistConSup = new Dictionary<string, double>();
                    DicDistConSup = GetminDistFromConcrete(SupData);

                    string Id = GetMinDistFromDic(DicDistConSup);

                    string VerPartId = "";
                    foreach (var Part in CombinedParSupp)
                    {
                        if (Part.Count == 1)
                        {
                            if (Part[0] == Id)
                            {
                                VerPartId = Id;
                                break;
                            }
                        }
                    }

                    if (VerPartId.Length < 1)
                    {
                        return false;
                    }

                    Dictionary<string, MinPtDist> DicAngleData = new Dictionary<string, MinPtDist>();

                    List<Vector3D> ListVecData = new List<Vector3D>();

                    DicAngleData = GetAngleBetweenSupport(VerPartId, SupData, ref ListVecData);

                    if (Math.Round(DicAngleData[AngleData[0].SuppoId].Angle).Equals(-90) && (Math.Round(DicAngleData[AngleData[1].SuppoId].Angle)).Equals(-90) && Math.Round(DicAngleData[ChannelC[0].SuppoId].Angle).Equals(90) && Math.Round(DicAngleData[ChannelC[1].SuppoId].Angle).Equals(90) && Math.Round(DicAngleData[ChannelC[2].SuppoId].Angle).Equals(90))
                    {
                        if (DicAngleData[AngleData[0].SuppoId].Mindist > DicAngleData[ChannelC[0].SuppoId].Mindist && DicAngleData[AngleData[0].SuppoId].Mindist > DicAngleData[ChannelC[1].SuppoId].Mindist && DicAngleData[AngleData[0].SuppoId].Mindist > DicAngleData[ChannelC[2].SuppoId].Mindist)
                        {
                            if (GettouchigPrimCount(SupData, ChannelC[0]) == 1 && GettouchigPrimCount(SupData, ChannelC[1]) == 1 && GettouchigPrimCount(SupData, ChannelC[2]) == 1)
                            {
                                SupData.SupportType = "Support47";
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        bool CheckforTypeSupport38(ref SupportData SupData)
        {
            int inx = 0;
            return false;
        }
        // bool CheckforTypeSupport28(ref SupportData SupData)
        //{
        //}

        int GettouchigPrimCount(SupportData SupData, SupporSpecData ChannelC)
        {
            int Count = 0;
            foreach (var Data in SupData.ListPrimarySuppo)
            {
                if (Data.ListtouchingParts.Contains(ChannelC.SuppoId))
                {
                    Count++;
                }
            }

            return Count;
        }
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


        bool Checkforangle(ref SupportData SupportData)
        {
            foreach (var SSup in SupportData.ListSecondrySuppo)
            {
                if (SSup != null && (SSup.Size.ToLower().Contains("l-") || SSup.Size.ToLower().Contains("angle") || SSup.Size.ToLower().Contains("isa")) && !SSup.Size.ToLower().Contains("web"))
                {
                    return true;
                }
            }
            return false;
        }

        bool CheckforTypeSupport43(ref SupportData SupData)
        {
            List<string> ListFlgId = new List<string>();

            if (SupData.ListPrimarySuppo[0].SupportName.Contains("GRP FLG") && SupData.ListPrimarySuppo[1].SupportName.Contains("GRP FLG"))
            {
                ListFlgId.Add(SupData.ListPrimarySuppo[0].SuppoId);
                ListFlgId.Add(SupData.ListPrimarySuppo[1].SuppoId);
                System.Windows.Media.Media3D.Vector3D Vec1 = new System.Windows.Media.Media3D.Vector3D(1, 0, 0);
                System.Windows.Media.Media3D.Vector3D Vec2 = new System.Windows.Media.Media3D.Vector3D(0, 1, 0);
                System.Windows.Media.Media3D.Vector3D Vec3 = new System.Windows.Media.Media3D.Vector3D(0, 0, 1);

                Vec1.X = SupData.ListPrimarySuppo[0].NoramlDir.X;
                Vec1.Y = SupData.ListPrimarySuppo[0].NoramlDir.Y;
                Vec1.Z = SupData.ListPrimarySuppo[0].NoramlDir.Z;
                Vec2.X = SupData.ListPrimarySuppo[1].NoramlDir.X;
                Vec2.Y = SupData.ListPrimarySuppo[1].NoramlDir.Y;
                Vec2.Z = SupData.ListPrimarySuppo[1].NoramlDir.Z;

                if (Math.Abs(Math.Round(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(Vec1, Vec2, Vec3)))).Equals(180))
                {
                    List<SupporSpecData> ListPlateData = new List<SupporSpecData>();

                    // Need to make this line more safe may cause Exception
                    if (GetPlateFromSecondarySuppo(SupData.ListSecondrySuppo, ref ListPlateData))
                    {
                        if (ListPlateData != null && ListPlateData.Count == 1)
                        {
                            List<string> ListParSup = new List<string>();
                            List<List<string>> CombinedParSupp = new List<List<string>>();

                            ListParSup = Checkandgetparllelsupports(ref SupData, ref CombinedParSupp);

                            Dictionary<string, double> DicMidistPt = new Dictionary<string, double>();
                            DicMidistPt = CheckforMidDist(SupData);

                            Dictionary<string, double> DicDistConSup = new Dictionary<string, double>();
                            DicDistConSup = GetminDistFromConcrete(SupData);

                            string Id = GetMinDistFromDic(DicDistConSup);

                            string VerPartId = "";
                            foreach (var Part in CombinedParSupp)
                            {
                                if (Part.Count == 1)
                                {
                                    if (Part[0] == Id)
                                    {
                                        VerPartId = Id;
                                        break;
                                    }
                                }
                            }

                            if (VerPartId.Length < 1)
                            {
                                return false;
                            }

                            string ParttochingVerKey = "";
                            string Parttoplate = "";
                            SupporSpecData VerPart = new SupporSpecData();
                            SupporSpecData PartTouchingtoVer = new SupporSpecData();
                            SupporSpecData PartToucingPlateandpartnextver = new SupporSpecData();
                            foreach (var SSupp in SupData.ListSecondrySuppo)
                            {
                                if (SSupp.SuppoId.Equals(VerPartId))
                                {
                                    VerPart = SSupp;
                                    foreach (string SPart in SSupp.ListtouchingParts)
                                    {
                                        if (DicDistConSup.ContainsKey(SPart))
                                        {
                                            ParttochingVerKey = SPart;
                                        }
                                    }
                                }
                            }

                            foreach (var SSupp in SupData.ListSecondrySuppo)
                            {
                                if (SSupp.SuppoId.Equals(ParttochingVerKey))
                                {
                                    PartTouchingtoVer = SSupp;
                                    foreach (string SPart in SSupp.ListtouchingParts)
                                    {
                                        if (DicDistConSup.ContainsKey(SPart) && SPart != VerPartId)
                                        {
                                            Parttoplate = SPart;
                                        }
                                    }
                                }
                            }

                            try
                            {
                                if (SupData.ListSecondrySuppo.Exists(e => e.SuppoId.Equals(Parttoplate)))
                                {
                                    PartToucingPlateandpartnextver = SupData.ListSecondrySuppo.Find(e => e.SuppoId.Equals(Parttoplate));
                                }
                            }
                            catch (Exception)
                            {
                            }

                            Dictionary<string, MinPtDist> DicAngleData = new Dictionary<string, MinPtDist>();

                            List<Vector3D> ListVecData = new List<Vector3D>();

                            DicAngleData = GetAngleBetweenSupport(VerPartId, SupData, ref ListVecData);

                            if (VerPart != null && PartTouchingtoVer != null && PartToucingPlateandpartnextver != null)
                            {
                                if (DicAngleData[PartTouchingtoVer.SuppoId].Angle.Equals(-90) && Math.Abs(Math.Round(VerPart.Angle.ZinDegree - PartTouchingtoVer.Angle.ZinDegree)).Equals(90) &&
                                    Math.Abs(Math.Round(PartTouchingtoVer.Angle.ZinDegree - PartToucingPlateandpartnextver.Angle.ZinDegree)).Equals(90) && (Math.Round(PartTouchingtoVer.Angle.XinDegree)).Equals((Math.Round(PartToucingPlateandpartnextver.Angle.XinDegree))).Equals(-90))
                                {

                                    foreach (var Data in SupData.ListSecondrySuppo)
                                    {
                                        if (Data.SuppoId != null && (Data.SuppoId.Equals(PartToucingPlateandpartnextver.SuppoId) || Data.SuppoId.Equals(PartTouchingtoVer.SuppoId)))
                                        {
                                            Data.PartDirection = "Hor";
                                        }
                                        else if (Data.SuppoId != null && Data.SuppoId.Equals(VerPartId))
                                        {
                                            Data.PartDirection = "Ver";
                                        }
                                    }
                                    SupData.SupportType = "Support43";
                                    return true;
                                }
                            }

                        }
                    }
                }

            }
            return false;
        }

        bool CheckforTypeSupport41(ref SupportData SupData)
        {
            List<string> ListParSup = new List<string>();
            List<List<string>> CombinedParSupp = new List<List<string>>();

            ListParSup = Checkandgetparllelsupports(ref SupData, ref CombinedParSupp);

            Dictionary<string, double> DicDistConSup = new Dictionary<string, double>();
            DicDistConSup = GetminDistFromConcrete(SupData);

            string Id = GetMinDistFromDic(DicDistConSup);

            string VerPartId = "";
            foreach (var Part in CombinedParSupp)
            {
                if (Part.Count == 1)
                {
                    if (Part[0] == Id)
                    {
                        VerPartId = Id;
                        break;
                    }
                }
            }

            if (VerPartId.Length < 1)
            {
                return false;
            }

            Dictionary<string, MinPtDist> DicAngleData = new Dictionary<string, MinPtDist>();

            List<Vector3D> ListVecData = new List<Vector3D>();

            DicAngleData = GetAngleBetweenSupport(VerPartId, SupData, ref ListVecData);

            int POS90count = 0;
            int NEV90count = 0;
            string NegativePartId = "";
            List<string> PosPartsId = new List<string>();
            if (DicAngleData.Count == 3)
            {
                foreach (var Data in DicAngleData)
                {
                    if (Math.Round(Data.Value.Angle).Equals(90))
                    {
                        POS90count++;
                        PosPartsId.Add(Data.Key);
                    }
                    else if (Math.Round(Data.Value.Angle).Equals(-90))
                    {
                        NEV90count++;
                        NegativePartId = Data.Key;
                    }
                }
            }


            List<SupporSpecData> PosPartList = new List<SupporSpecData>();
            SupporSpecData NevPartData = new SupporSpecData();
            SupporSpecData VerPartData = new SupporSpecData();

            if (POS90count == 2 && NEV90count == 1 &&
                !CheckVectorsArePlaner(ListVecData))
            {
                foreach (var Data in SupData.ListSecondrySuppo)
                {
                    if (Data.SuppoId != null && PosPartsId.Contains(Data.SuppoId))
                    {
                        PosPartList.Add(Data);
                    }
                    else if (Data.SuppoId != null && Data.SuppoId.Equals(NegativePartId))
                    {
                        NevPartData = Data;
                    }
                    else if (Data.SuppoId != null && Data.SuppoId.Equals(VerPartId))
                    {
                        VerPartData = Data;
                    }
                }
                if (PosPartList.Count == 2 && Math.Round(PosPartList[0].Angle.ZinDegree).Equals(Math.Round(PosPartList[1].Angle.ZinDegree)) && NevPartData != null && VerPartData != null && Math.Round(NevPartData.Angle.ZinDegree).Equals(Math.Round(VerPartData.Angle.ZinDegree)) && Math.Abs(Math.Round(PosPartList[0].Angle.ZinDegree - NevPartData.Angle.ZinDegree)).Equals(90))
                {

                    foreach (var Data in SupData.ListSecondrySuppo)
                    {
                        if (Data.SuppoId != null && PosPartsId.Contains(Data.SuppoId))
                        {
                            Data.PartDirection = "Hor";
                        }
                        else if (Data.SuppoId != null && Data.SuppoId.Equals(NegativePartId))
                        {
                            Data.PartDirection = "Hor";
                        }
                        else if (Data.SuppoId != null && Data.SuppoId.Equals(VerPartId))
                        {
                            Data.PartDirection = "Ver";
                        }
                    }
                    SupData.SupportType = "Support41";
                    return true;
                }

            }

            return false;
        }

        //bool CheckforTypeSupport36(ref SupportData SupportData)
        //{

        // }
        bool CheckforTypeSupport31(ref SupportData SupportData)
        {
            List<string> ListParSup = new List<string>();
            List<List<string>> CombinedParSupp = new List<List<string>>();

            ListParSup = Checkandgetparllelsupports(ref SupportData, ref CombinedParSupp);

            Dictionary<string, double> DicDistConSup = new Dictionary<string, double>();
            string Id = GetMinDistFromDic(DicDistConSup);

            string VerPartId = "";
            foreach (var Part in CombinedParSupp)
            {
                if (Part.Count == 1)
                {
                    if (Part[0] == Id)
                    {
                        VerPartId = Id;
                        break;
                    }
                }
            }

            if (VerPartId.Length < 1)
            {
                return false;
            }

            Dictionary<string, MinPtDist> DicAngleData = new Dictionary<string, MinPtDist>();

            List<Vector3D> ListVecData = new List<Vector3D>();

            DicAngleData = GetAngleBetweenSupport(VerPartId, SupportData, ref ListVecData);

            if (CheckVectorsArePlaner(ListVecData))
            {
                return CheckAnyPartSecInclined(ref SupportData);
            }

            return false;
        }

        int GetGussetCout(ref SupportData SupportData)
        {
            int Count = 0;

            foreach (var SSup in SupportData.ListSecondrySuppo)
            {
                if (SSup.IsGussetplate)
                {
                    Count++;
                }
            }

            return Count;
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
                if (Math.Abs(Math.Round(SecSup.Angle.XinDegree)).Equals(45) || Math.Abs(Math.Round(SecSup.Angle.XinDegree)).Equals(135))
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
                else if (Math.Abs(Math.Round(SecSup.Angle.XinDegree)).Equals(90))
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
                        if (SupportData.ListPrimarySuppo == null || SupportData.ListPrimarySuppo.Count == 0)
                        {
                            SupportData.SupportType = "Support63";
                            return true;
                        }
                        else if (Calculate.DistPoint(SupportData.ListPrimarySuppo[0].Centroid, GetPt3DFromArray(VerPart.EndPt)) > Calculate.DistPoint(GetPt3DFromArray(VerPart.EndPt), GetPt3DFromArray(Closestpt)))
                        {
                            SupportData.SupportType = "Support31";
                            return true;
                        }
                    }
                }
                else if (Math.Round(VerPart.Boundingboxmax.Z) < Math.Round(HorPart.Centroid.Z))
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

                        SupportData.SupportType = "Support62";
                        return true;
                    }
                }

            }

            return false;
        }
        //Need to Add the some code for the some conditon for the Support30
        bool CheckforTypeSupport30(ref SupportData SupData)
        {
            List<string> ListFlgId = new List<string>();
            if (SupData.ListPrimarySuppo[0].SupportName.Contains("GRP FLG") && SupData.ListPrimarySuppo[1].SupportName.Contains("GRP FLG"))
            {
                ListFlgId.Add(SupData.ListPrimarySuppo[0].SuppoId);
                ListFlgId.Add(SupData.ListPrimarySuppo[1].SuppoId);
                System.Windows.Media.Media3D.Vector3D Vec1 = new System.Windows.Media.Media3D.Vector3D(1, 0, 0);
                System.Windows.Media.Media3D.Vector3D Vec2 = new System.Windows.Media.Media3D.Vector3D(0, 1, 0);
                System.Windows.Media.Media3D.Vector3D Vec3 = new System.Windows.Media.Media3D.Vector3D(0, 0, 1);

                Vec1.X = SupData.ListPrimarySuppo[0].NoramlDir.X;
                Vec1.Y = SupData.ListPrimarySuppo[0].NoramlDir.Y;
                Vec1.Z = SupData.ListPrimarySuppo[0].NoramlDir.Z;
                Vec2.X = SupData.ListPrimarySuppo[1].NoramlDir.X;
                Vec2.Y = SupData.ListPrimarySuppo[1].NoramlDir.Y;
                Vec2.Z = SupData.ListPrimarySuppo[1].NoramlDir.Z;

                if (Math.Abs(Math.Round(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(Vec1, Vec2, Vec3)))).Equals(180))
                {
                    List<SupporSpecData> ListPlateData = new List<SupporSpecData>();

                    // Need to make this line more safe may cause Exception
                    if (GetPlateFromSecondarySuppo(SupData.ListSecondrySuppo, ref ListPlateData))
                    {
                        if (ListPlateData != null && ListPlateData.Count == 1)
                        {
                            List<string> ListParSup = new List<string>();
                            List<List<string>> CombinedParSupp = new List<List<string>>();

                            ListParSup = Checkandgetparllelsupports(ref SupData, ref CombinedParSupp);

                            Dictionary<string, double> DicDistConSup = new Dictionary<string, double>();
                            DicDistConSup = GetminDistFromConcrete(SupData);

                            string Id = GetMinDistFromDic(DicDistConSup);

                            string VerPartId = "";
                            foreach (var Part in CombinedParSupp)
                            {
                                if (Part.Count == 1)
                                {
                                    if (Part[0] == Id)
                                    {
                                        VerPartId = Id;
                                        break;
                                    }
                                }
                            }

                            if (VerPartId.Length < 1)
                            {
                                return false;
                            }

                            Dictionary<string, MinPtDist> DicAngleData = new Dictionary<string, MinPtDist>();

                            List<Vector3D> ListVecData = new List<Vector3D>();

                            DicAngleData = GetAngleBetweenSupport(VerPartId, SupData, ref ListVecData);

                            int POS90count = 0;

                            string NegativePartId = "";
                            List<string> PosPartsId = new List<string>();
                            if (DicAngleData.Count == 1)
                            {
                                foreach (var Data in DicAngleData)
                                {
                                    if (Math.Round(Data.Value.Angle).Equals(90))
                                    {
                                        POS90count++;
                                        PosPartsId.Add(Data.Key);
                                    }
                                }

                                if (POS90count == 1 && CheckVectorsArePlaner(ListVecData))
                                {
                                    SupporSpecData SupData1 = new SupporSpecData();
                                    SupporSpecData SupData2 = new SupporSpecData();

                                    foreach (var Data in SupData.ListSecondrySuppo)
                                    {
                                        if (Data.SuppoId != null && Data.SuppoId == PosPartsId[0])
                                        {
                                            SupData1 = Data;
                                        }
                                        else if (Data.SuppoId != null && Data.SuppoId == VerPartId)
                                        {
                                            SupData2 = Data;
                                        }
                                    }

                                    if (Math.Round(SupData1.Angle.ZinDegree).Equals(Math.Round(SupData2.Angle.ZinDegree)))
                                    {
                                        if (ListPlateData[0].ListtouchingParts.Contains(PosPartsId[0]))
                                        {
                                            foreach (var Data in SupData.ListSecondrySuppo)
                                            {
                                                if (Data.SuppoId != null && DicAngleData.ContainsKey(Data.SuppoId))
                                                {
                                                    Data.PartDirection = "Hor";
                                                }
                                                else if (Data.SuppoId != null && Data.SuppoId == VerPartId)
                                                {
                                                    Data.PartDirection = "Ver";
                                                }
                                            }

                                            SupData.SupportType = "Support30";
                                            return true;
                                        }
                                    }

                                }
                            }
                        }
                    }
                }
            }

            return false;
        }

        bool GetPlateFromSecondarySuppo(List<SupporSpecData> ListSecSuppoData, ref List<SupporSpecData> ListPlateData)
        {
            bool Hasplate = false;

            foreach (var SuppoData in ListSecSuppoData)
            {
                if (SuppoData.SupportName != null && SuppoData.SupportName.ToUpper().Contains("PLATE"))
                {
                    ListPlateData.Add(SuppoData);
                    Hasplate = true;
                }
            }

            return Hasplate;
        }

        List<int> CheckandGetindexesofGRPFLG(ref SupportData SupData)
        {
            List<int> listindex = new List<int>();
            for (int inx = 0; inx < SupData.ListPrimarySuppo.Count; inx++)
            {
                if (SupData.ListPrimarySuppo[inx].SupportName.ToUpper().Contains("GRP FLG"))
                {
                    listindex.Add(inx);
                }
            }

            return listindex;
        }

        bool CheckforTypeSupport32(ref SupportData SupData)
        {
            List<int> listindex = new List<int>();
            listindex = CheckandGetindexesofGRPFLG(ref SupData);
            List<string> ListFlgId = new List<string>();

            if (listindex.Count == 2)
            {
                ListFlgId.Add(SupData.ListPrimarySuppo[listindex[0]].SuppoId);
                ListFlgId.Add(SupData.ListPrimarySuppo[listindex[1]].SuppoId);
                System.Windows.Media.Media3D.Vector3D Vec1 = new System.Windows.Media.Media3D.Vector3D(1, 0, 0);
                System.Windows.Media.Media3D.Vector3D Vec2 = new System.Windows.Media.Media3D.Vector3D(0, 1, 0);
                System.Windows.Media.Media3D.Vector3D Vec3 = new System.Windows.Media.Media3D.Vector3D(0, 0, 1);

                Vec1.X = SupData.ListPrimarySuppo[listindex[0]].NoramlDir.X;
                Vec1.Y = SupData.ListPrimarySuppo[listindex[0]].NoramlDir.Y;
                Vec1.Z = SupData.ListPrimarySuppo[listindex[0]].NoramlDir.Z;
                Vec2.X = SupData.ListPrimarySuppo[listindex[1]].NoramlDir.X;
                Vec2.Y = SupData.ListPrimarySuppo[listindex[1]].NoramlDir.Y;
                Vec2.Z = SupData.ListPrimarySuppo[listindex[1]].NoramlDir.Z;

                if (Math.Abs(Math.Round(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(Vec1, Vec2, Vec3)))).Equals(180))
                {
                    List<SupporSpecData> ListPlateData = new List<SupporSpecData>();

                    // Need to make this line more safe may cause Exception
                    if (GetPlateFromSecondarySuppo(SupData.ListSecondrySuppo, ref ListPlateData))
                    {
                        if (ListPlateData != null && ListPlateData.Count == 1)
                        {
                            List<string> ListParSup = new List<string>();
                            List<List<string>> CombinedParSupp = new List<List<string>>();

                            ListParSup = Checkandgetparllelsupports(ref SupData, ref CombinedParSupp);

                            Dictionary<string, double> DicMidistPt = new Dictionary<string, double>();
                            DicMidistPt = CheckforMidDist(SupData);

                            Dictionary<string, double> DicDistConSup = new Dictionary<string, double>();
                            DicDistConSup = GetminDistFromConcrete(SupData);

                            string Id = GetMinDistFromDic(DicDistConSup);

                            string VerPartId = "";
                            foreach (var Part in CombinedParSupp)
                            {
                                if (Part.Count == 1)
                                {
                                    if (Part[0] == Id)
                                    {
                                        VerPartId = Id;
                                        break;
                                    }
                                }
                            }

                            if (VerPartId.Length < 1)
                            {
                                return false;
                            }

                            Dictionary<string, MinPtDist> DicAngleData = new Dictionary<string, MinPtDist>();

                            List<Vector3D> ListVecData = new List<Vector3D>();

                            DicAngleData = GetAngleBetweenSupport(VerPartId, SupData, ref ListVecData);

                            int POS90count = 0;
                            int SymetricCount = 0;

                            string NegativePartId = "";
                            List<string> PosPartsId = new List<string>();
                            if (DicAngleData.Count == 2)
                            {
                                foreach (var Data in DicAngleData)
                                {
                                    if (Math.Round(Data.Value.Angle).Equals(90))
                                    {
                                        POS90count++;
                                        PosPartsId.Add(Data.Key);
                                    }
                                    else if (DicMidistPt.Count == 1 && DicMidistPt.ContainsKey(Data.Key))
                                    {
                                        SymetricCount++;
                                    }
                                }

                                SupporSpecData SupData1 = new SupporSpecData();
                                SupporSpecData SupData2 = new SupporSpecData();
                                SupporSpecData SymPart = new SupporSpecData();
                                if (POS90count == 1 && SymetricCount == 1 && !CheckVectorsArePlaner(ListVecData))
                                {
                                    foreach (var Data in SupData.ListSecondrySuppo)
                                    {
                                        if (Data.SuppoId != null && Data.SuppoId == PosPartsId[0])
                                        {
                                            SupData1 = Data;
                                        }
                                        else if (Data.SuppoId != null && Data.SuppoId == VerPartId)
                                        {
                                            SupData2 = Data;
                                        }
                                        else if (Data.SuppoId != null && Data.SuppoId == DicMidistPt.ElementAt(0).Key)
                                        {
                                            SymPart = Data;
                                        }
                                    }

                                    if (ListPlateData != null && ListPlateData.Count == 1 && SupData1.ListtouchingParts.Contains(ListPlateData[0].SuppoId))
                                    {
                                        if (Math.Abs(Math.Round(SymPart.Angle.ZinDegree - SupData2.Angle.ZinDegree)).Equals(180) && Math.Abs(Math.Round(SupData1.Angle.ZinDegree - SupData2.Angle.ZinDegree)).Equals(90) && (SymPart.Boundingboxmax.Z > SupData2.Boundingboxmax.Z) &&
                                            SymPart.ListtouchingParts.Contains(SupData.ListPrimarySuppo[GetMissingIndex(listindex, SupData)].SuppoId) && Math.Round(SupData1.Angle.XinDegree).Equals(Math.Round(SymPart.Angle.XinDegree)))
                                        {
                                            foreach (var Data in SupData.ListSecondrySuppo)
                                            {
                                                if (Data.SuppoId != null && SupData1.Equals(Data.SuppoId))
                                                {
                                                    Data.PartDirection = "Hor";
                                                }
                                                else if (Data.SuppoId != null && Data.SuppoId == VerPartId)
                                                {
                                                    Data.PartDirection = "Ver";
                                                }
                                                else if (Data.SuppoId != null && Data.SuppoId == SymPart.SuppoId)
                                                {
                                                    Data.PartDirection = "Hor";
                                                }
                                            }

                                            SupData.SupportType = "Support32";
                                            return true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

            }
            return false;
        }

        int GetMissingIndex(List<int> listindex, SupportData SupData)
        {
            for (int inx = 0; inx < SupData.ListPrimarySuppo.Count; inx++)
            {
                if (!listindex.Contains(inx))
                {
                    return inx;
                }
            }

            return 0;
        }
        bool CheckforTypeSupport48(ref SupportData SupData)
        {
            int plateCount = 0;
            SupporSpecData NBPart = new SupporSpecData();
            List<SupporSpecData> ChannelC = new List<SupporSpecData>();

            List<SupporSpecData> AngleData = new List<SupporSpecData>();

            foreach (var Data in SupData.ListSecondrySuppo)
            {
                if (Data.SupportName != null && Data.SupportName.ToUpper().Equals("PLATE"))
                {
                    plateCount++;
                }
                else if (Data.Size != null && Data.Size.ToUpper().Contains("NB"))
                {
                    NBPart = Data;
                }
                else if (Data.Size != null && Data.Size.ToUpper().Contains("ISMC"))
                {
                    ChannelC.Add(Data);
                }
                else if (Data.Size != null && (Data.Size.ToUpper().Contains("L-") || Data.Size.ToUpper().Contains("ANGLE") || Data.Size.ToUpper().Contains("ISA")) && (!Data.Size.ToUpper().Contains("WEB")))
                {
                    AngleData.Add(Data);
                }
            }

            if (plateCount == 5 && NBPart != null && ChannelC != null && ChannelC.Count == 1 && AngleData != null && AngleData.Count == 2)
            {
                if (Math.Round(Math.Abs(ChannelC[0].Angle.XinDegree - NBPart.Angle.XinDegree)).Equals(90) && Math.Round(Math.Abs(AngleData[0].Angle.XinDegree - NBPart.Angle.XinDegree)).Equals(90) && Math.Round(AngleData[0].Angle.XinDegree).Equals(Math.Round(AngleData[1].Angle.XinDegree)))
                {
                    List<string> ListParSup = new List<string>();
                    List<List<string>> CombinedParSupp = new List<List<string>>();

                    ListParSup = Checkandgetparllelsupports(ref SupData, ref CombinedParSupp);

                    Dictionary<string, double> DicMidistPt = new Dictionary<string, double>();
                    DicMidistPt = CheckforMidDist(SupData);

                    Dictionary<string, double> DicDistConSup = new Dictionary<string, double>();
                    DicDistConSup = GetminDistFromConcrete(SupData);

                    string Id = GetMinDistFromDic(DicDistConSup);

                    string VerPartId = "";
                    foreach (var Part in CombinedParSupp)
                    {
                        if (Part.Count == 1)
                        {
                            if (Part[0] == Id)
                            {
                                VerPartId = Id;
                                break;
                            }
                        }
                    }

                    if (VerPartId.Length < 1)
                    {
                        return false;
                    }

                    Dictionary<string, MinPtDist> DicAngleData = new Dictionary<string, MinPtDist>();

                    List<Vector3D> ListVecData = new List<Vector3D>();

                    DicAngleData = GetAngleBetweenSupport(VerPartId, SupData, ref ListVecData);

                    if (Math.Round(DicAngleData[AngleData[0].SuppoId].Angle).Equals(-90) && (Math.Round(DicAngleData[AngleData[1].SuppoId].Angle)).Equals(-90) && (Math.Round(DicAngleData[NBPart.SuppoId].Angle).Equals(90)) && DicAngleData[AngleData[0].SuppoId].Mindist > DicAngleData[ChannelC[0].SuppoId].Mindist && DicAngleData[AngleData[1].SuppoId].Mindist > DicAngleData[NBPart.SuppoId].Mindist && SupData.ListSecondrySuppo[0].TouchingPartid.Contains(SupData.ListPrimarySuppo[0].SuppoId))
                    {
                        SupData.SupportType = "Support48";
                        return true;
                    }
                }
            }
            return false;
        }
        bool CheckforTypeSupport29(ref SupportData SupData)
        {
            int plateCount = 0;
            SupporSpecData NBPart = new SupporSpecData();
            List<SupporSpecData> ChannelC = new List<SupporSpecData>();

            SupporSpecData AngleData = new SupporSpecData();

            foreach (var Data in SupData.ListSecondrySuppo)
            {
                if (Data.SupportName != null && Data.SupportName.ToUpper().Equals("PLATE"))
                {
                    plateCount++;
                }
                else if (Data.Size != null && Data.Size.ToUpper().Contains("NB"))
                {
                    NBPart = Data;
                }
                else if (Data.Size != null && Data.Size.ToUpper().Contains("ISMC"))
                {
                    ChannelC.Add(Data);
                }
                else if (Data.Size != null && (Data.Size.ToUpper().Contains("L-") || Data.Size.ToUpper().Contains("ANGLE") || Data.Size.ToUpper().Contains("ISA")) && (!Data.Size.ToUpper().Contains("WEB")))
                {
                    AngleData = Data;
                }
            }

            if (plateCount == 5 && NBPart != null && ChannelC != null && ChannelC.Count == 1 && AngleData != null)
            {
                if (Math.Round(Math.Abs(ChannelC[0].Angle.XinDegree - NBPart.Angle.XinDegree)).Equals(90) && Math.Round(Math.Abs(AngleData.Angle.XinDegree - NBPart.Angle.XinDegree)).Equals(90))
                {
                    System.Windows.Media.Media3D.Vector3D Vec1 = new System.Windows.Media.Media3D.Vector3D(1, 0, 0);
                    System.Windows.Media.Media3D.Vector3D Vec2 = new System.Windows.Media.Media3D.Vector3D(0, 1, 0);
                    System.Windows.Media.Media3D.Vector3D Vec3 = new System.Windows.Media.Media3D.Vector3D(1, 0, 0);

                    System.Windows.Media.Media3D.Vector3D Vec4 = new System.Windows.Media.Media3D.Vector3D(1, 0, 0);

                    Pt3D Point3D1 = new Pt3D();
                    Pt3D Point3DAng = new Pt3D();
                    if (Calculate.DistPoint(GetPt3DFromArray(NBPart.EndPt), GetPt3DFromArray(ChannelC[0].EndPt)) > Calculate.DistPoint(GetPt3DFromArray(NBPart.EndPt), GetPt3DFromArray(ChannelC[0].StPt)))
                    {

                        Vec1.X = ChannelC[0].EndPt[0] - ChannelC[0].StPt[0];
                        Vec1.Y = ChannelC[0].EndPt[1] - ChannelC[0].StPt[1];
                        Vec1.Z = ChannelC[0].EndPt[2] - ChannelC[0].StPt[2];
                        Point3D1 = GetPt3DFromArray(ChannelC[0].StPt);
                    }
                    else
                    {
                        Vec1.X = ChannelC[0].StPt[0] - ChannelC[0].EndPt[0];
                        Vec1.Y = ChannelC[0].StPt[1] - ChannelC[0].EndPt[1];
                        Vec1.Z = ChannelC[0].StPt[2] - ChannelC[0].EndPt[2];

                        Point3D1 = GetPt3DFromArray(ChannelC[0].EndPt);
                    }

                    if (Calculate.DistPoint(GetPt3DFromArray(NBPart.EndPt), GetPt3DFromArray(AngleData.EndPt)) > Calculate.DistPoint(GetPt3DFromArray(NBPart.EndPt), GetPt3DFromArray(AngleData.StPt)))
                    {
                        Vec4.X = AngleData.EndPt[0] - AngleData.StPt[0];
                        Vec4.Y = AngleData.EndPt[1] - AngleData.StPt[1];
                        Vec4.Z = AngleData.EndPt[2] - AngleData.StPt[2];

                        Point3DAng = GetPt3DFromArray(AngleData.StPt);
                    }
                    else
                    {
                        Vec4.X = AngleData.StPt[0] - AngleData.EndPt[0];
                        Vec4.Y = AngleData.StPt[1] - AngleData.EndPt[1];
                        Vec4.Z = AngleData.StPt[2] - AngleData.EndPt[2];
                        Point3DAng = GetPt3DFromArray(AngleData.EndPt);
                    }


                    Vec2.X = NBPart.EndPt[0] - NBPart.StPt[0];
                    Vec2.Y = NBPart.EndPt[1] - NBPart.StPt[1];
                    Vec2.Z = NBPart.EndPt[2] - NBPart.StPt[2];

                    if ((Math.Round(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(Vec1, Vec2, Vec3))).Equals(90)
                       && Math.Round(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(Vec4, Vec2, Vec3))).Equals(-90)) ||
                       (Math.Round(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(Vec1, Vec2, Vec3))).Equals(-90)
                       && Math.Round(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(Vec4, Vec2, Vec3))).Equals(90)))
                    {
                        List<Vector3D> ListVecData = new List<Vector3D>();
                        ListVecData.Add(Vec1);
                        ListVecData.Add(Vec2);
                        ListVecData.Add(Vec4);
                        if (CheckVectorsArePlaner(ListVecData))
                        {
                            if (Calculate.DistPoint(SupData.ListConcreteData[0].Centroid, Point3DAng) > Calculate.DistPoint(SupData.ListConcreteData[0].Centroid, Point3D1))
                            {
                                // if (CheckTwoSuppoCollinear(ChannelC[0], ChannelC[1]))
                                {
                                    SupData.SupportType = "Support29";
                                    return true;
                                }
                            }
                        }
                    }

                }
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

        bool CheckforTypeSupport45(ref SupportData SupData)
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


                    if (Math.Round(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(Vec1, Vec2, Vec3))).Equals(90))
                    {
                        if (CheckforCollinear(ChannelC, NBPart))
                        {
                            if (SupData.ListPrimarySuppo.Count == 2)
                            {
                                SupData.SupportType = "Support33";
                                return true;
                            }
                            else
                            {
                                SupData.SupportType = "Support25";
                                return true;
                            }
                        }
                    }

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
                            if (SupData.ListPrimarySuppo.Count == 4 && SupData.ListPrimarySuppo[0].SuppoId != null && SupData.ListPrimarySuppo[1].SuppoId != null &&
                                SupData.ListPrimarySuppo[2].SuppoId != null && SupData.ListPrimarySuppo[3].SuppoId != null && ChannelC.SuppoId != null && SupData.ListPrimarySuppo[0].ListtouchingParts[0].Equals(ChannelC.SuppoId)
                                && SupData.ListPrimarySuppo[1].ListtouchingParts[0].Equals(ChannelC.SuppoId)
                                && SupData.ListPrimarySuppo[2].ListtouchingParts[0].Equals(ChannelC.SuppoId)
                                && SupData.ListPrimarySuppo[3].ListtouchingParts[0].Equals(ChannelC.SuppoId)
                                )
                            {
                                SupData.SupportType = "Support45";
                                return true;
                            }
                            else if (SupData.ListPrimarySuppo.Count == 2)
                            {
                                SupData.SupportType = "Support33";
                                return true;
                            }
                            else
                            {
                                SupData.SupportType = "Support25";
                                return true;
                            }
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
            int ISCMCCount = 0;
            int AngleCount = 0;

            List<SupporSpecData> ListIsmc = new List<SupporSpecData>();
            List<SupporSpecData> ListAngle = new List<SupporSpecData>();

            foreach (var Data in SupData.ListSecondrySuppo)
            {
                if ((Data.Size.ToUpper().Contains("L-") || Data.Size.ToUpper().Contains("ANGLE")) && !Data.Size.ToUpper().Contains("WEB"))
                {
                    AngleCount++;
                    ListAngle.Add(Data);
                }
                else if (Data.Size.ToUpper().Contains("WEB"))
                {
                    ISCMCCount++;
                    ListIsmc.Add(Data);
                }
            }

            //checfor

            List<string> ListParSup = new List<string>();
            List<List<string>> CombinedParSupp = new List<List<string>>();
            //AnycentroidSecondaryMaching(ref SupData) // will use this later 
            ListParSup = Checkandgetparllelsupports(ref SupData, ref CombinedParSupp);

            Dictionary<string, double> DicDistConSup = new Dictionary<string, double>();
            DicDistConSup = GetminDistFromConcrete(SupData);

            string Id = GetMinDistFromDic(DicDistConSup);

            string VerPartId = "";
            foreach (var Part in CombinedParSupp)
            {
                if (Part.Count == 1)
                {
                    if (Part[0] == Id)
                    {
                        VerPartId = Id;
                        break;
                    }
                }
            }

            if (VerPartId.Length < 1)
            {
                return false;
            }

            Dictionary<string, MinPtDist> DicAngleData = new Dictionary<string, MinPtDist>();

            List<Vector3D> ListVecData = new List<Vector3D>();

            DicAngleData = GetAngleBetweenSupport(VerPartId, SupData, ref ListVecData);

            int POS90count = 0;
            int NEV90count = 0;
            bool AngleCond = false;
            bool DistCond = false;
            string NegativePartId = "";
            string PosPartId = "";
            if (AngleCount == 1 && DicAngleData.Count == 3)
            {
                foreach (var Data in DicAngleData)
                {
                    if (Data.Key.Equals(ListAngle[0].SuppoId) && Math.Round(Data.Value.Angle).Equals(90))
                    {
                        POS90count++;
                        AngleCond = true;
                    }
                    else if (Math.Round(Data.Value.Angle).Equals(90))
                    {
                        POS90count++;
                        PosPartId = Data.Key;
                        if (Data.Value.Mindist > DicAngleData[ListAngle[0].SuppoId].Mindist)
                        {
                            DistCond = true;
                        }
                    }
                    else if (Math.Round(Data.Value.Angle).Equals(-90))
                    {
                        NEV90count++;
                        NegativePartId = Data.Key;
                    }
                }
            }


            //  CheckVectorsArePlaner(ListVecData);

            if (POS90count == 2 && NEV90count == 1 && AngleCond && DistCond && CheckVectorsArePlaner(ListVecData))
            {
                SupporSpecData SupData1 = new SupporSpecData();
                SupporSpecData SupData2 = new SupporSpecData();

                foreach (var Data in SupData.ListSecondrySuppo)
                {
                    if (Data.SuppoId != null && Data.SuppoId == NegativePartId)
                    {
                        SupData1 = Data;
                    }
                    else if (Data.SuppoId != null && Data.SuppoId == PosPartId)
                    {
                        SupData2 = Data;
                    }
                }

                if (Math.Round(SupData1.Angle.ZinDegree).Equals(Math.Round(SupData2.Angle.ZinDegree)))
                {
                    foreach (var Data in SupData.ListSecondrySuppo)
                    {
                        if (Data.SuppoId != null && DicAngleData.ContainsKey(Data.SuppoId))
                        {
                            Data.PartDirection = "Hor";
                        }
                        else
                        {
                            Data.PartDirection = "Ver";
                        }
                    }
                    SupData.SupportType = "Support20";
                    return true;
                }
                else if (Math.Abs(Math.Round(SupData1.Angle.ZinDegree - SupData2.Angle.ZinDegree)).Equals(180))
                {
                    foreach (var Data in SupData.ListSecondrySuppo)
                    {
                        if (Data.SuppoId != null && DicAngleData.ContainsKey(Data.SuppoId))
                        {
                            Data.PartDirection = "Hor";
                        }
                        else
                        {
                            Data.PartDirection = "Ver";
                        }
                    }
                    SupData.SupportType = "Support40";
                    return true;
                }
            }

            return false;
        }

        bool CheckVectorsArePlaner(List<Vector3D> ListVecData)
        {
            List<bool> ListAreCoplaner = new List<bool>();
            if (ListVecData.Count < 3)
            {
                return true;
            }
            else
            {
                for (int inx = 0; inx < ListVecData.Count - 2; inx++)
                {
                    if (Math.Round(Vector3D.DotProduct(ListVecData[inx], Vector3D.CrossProduct(ListVecData[inx + 1], ListVecData[inx + 2]))) == 0)
                    {
                        ListAreCoplaner.Add(true);
                    }
                    else
                    {
                        ListAreCoplaner.Add(false);
                    }
                }
            }

            if (ListAreCoplaner.Any(x => x.Equals(false)))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        Dictionary<string, MinPtDist> GetAngleBetweenSupport(string VerPartId, SupportData SupData, ref List<Vector3D> ListVecData, List<string> IdNottobeProcess = null)
        {
            Dictionary<string, MinPtDist> Minptlist = new Dictionary<string, MinPtDist>();

            Pt3D BottomPtofVerPart = new Pt3D();
            Pt3D MaxPt = new Pt3D();
            Vector3D Vec1 = new Vector3D();
            foreach (var SSuppo in SupData.ListSecondrySuppo)
            {
                if (SSuppo.SuppoId.Equals(VerPartId) && !SSuppo.SupportName.ToUpper().Equals("PLATE"))
                {
                    Pt3D MinPartPt = new Pt3D();

                    bool IsSymetricCen = false;
                    FindMinDist(SupData.ListConcreteData[0].Centroid, SSuppo, ref BottomPtofVerPart, ref IsSymetricCen);

                    MaxPt = GetotherPt(SSuppo, BottomPtofVerPart);
                    Vec1 = GetVector(MaxPt, BottomPtofVerPart);
                }
            }

            ListVecData.Add(Vec1);

            foreach (var SSuppo in SupData.ListSecondrySuppo)
            {
                if (!SSuppo.SuppoId.Equals(VerPartId) && !SSuppo.SupportName.ToUpper().Equals("PLATE"))
                {
                    if (IdNottobeProcess != null && IdNottobeProcess.Contains(SSuppo.SuppoId))
                    {
                        continue;
                    }
                    Pt3D MinPartPt = new Pt3D();
                    Pt3D MaxPt1 = new Pt3D();
                    MinPtDist PtandDist = new MinPtDist();
                    Vector3D Vec2 = new Vector3D();
                    bool IsSymetricCen = false;

                    Point3d ProjPt = FindPerpendicularFoot(BottomPtofVerPart, SSuppo.StPt, SSuppo.EndPt);


                    PtandDist.Mindist = FindMinDist(GetPt3DFromPoint3d(ProjPt), SSuppo, ref MinPartPt, ref IsSymetricCen);

                    PtandDist.MinPt = MinPartPt;
                    MaxPt1 = GetotherPt(SSuppo, MinPartPt);
                    Vec2 = GetVector(MaxPt1, MinPartPt);
                    ListVecData.Add(Vec2);
                    PtandDist.Angle = Calculate.ConvertRadiansToDegrees(
                    Calculate.GetSignedRotation(Vec1, Vec2, new Vector3D(1, 0, 0)));

                    Minptlist[SSuppo.SuppoId] = PtandDist;
                }
            }
            return Minptlist;
        }

        Vector3D GetVector(Pt3D MaxPt, Pt3D MinPartPt)
        {
            Vector3D Vec = new Vector3D();

            Vec.X = MaxPt.X - MinPartPt.X;
            Vec.Y = MaxPt.Y - MinPartPt.Y;
            Vec.Z = MaxPt.Z - MinPartPt.Z;

            Vec.Normalize();
            return Vec;
        }
        Pt3D GetotherPt(SupporSpecData SSuppo, Pt3D MinPartPt)
        {
            Pt3D pt3D = new Pt3D();
            if ((Math.Abs(SSuppo.StPt[0] - MinPartPt.X) < 0.1) && (Math.Abs(SSuppo.StPt[1] - MinPartPt.Y) < 0.1) && (Math.Abs(SSuppo.StPt[2] - MinPartPt.Z) < 0.1))
            {
                return GetPt3DFromArray(SSuppo.EndPt);
            }
            else
            {
                return GetPt3DFromArray(SSuppo.StPt);
            }
        }

        string GetMinDistFromDic(Dictionary<string, double> DicDistConSup)
        {
            bool IsFirst = true;
            double MinValue = 0;
            string SuppoId = "";
            foreach (var Data in DicDistConSup)
            {
                if (IsFirst)
                {
                    MinValue = Data.Value;
                    SuppoId = Data.Key;
                    IsFirst = false;
                }
                else
                {
                    if (Data.Value < MinValue)
                    {
                        MinValue = Data.Value;
                        SuppoId = Data.Key;
                    }
                }
            }

            return SuppoId;
        }

        Dictionary<string, double> GetminDistFromConcrete(SupportData SupData)
        {
            Dictionary<string, double> DicDistConSup = new Dictionary<string, double>();
            if (SupData.ListConcreteData != null && SupData.ListConcreteData.Count > 0)
            {
                foreach (var Data in SupData.ListSecondrySuppo)
                {
                    if (Data != null && Data.SuppoId != null && Data.SupportName != null && !Data.SupportName.ToUpper().Equals("PLATE"))
                    {
                        Pt3D MinPtCoordinate = new Pt3D();
                        bool IsSymetricCen = false;
                        DicDistConSup[Data.SuppoId] = FindMinDist(SupData.ListConcreteData[0].Centroid, Data, ref MinPtCoordinate, ref IsSymetricCen);
                    }
                }
            }

            return DicDistConSup;
        }


        Dictionary<string, double> CheckforMidDist(SupportData SupData)
        {
            Dictionary<string, double> DicDistConSup = new Dictionary<string, double>();
            if (SupData.ListConcreteData != null && SupData.ListConcreteData.Count > 0)
            {
                foreach (var Data in SupData.ListSecondrySuppo)
                {
                    if (Data != null && Data.SuppoId != null && Data.SupportName != null && !Data.SupportName.ToUpper().Equals("PLATE"))
                    {
                        Pt3D MinPtCoordinate = new Pt3D();
                        bool IsSymetricCen = false;
                        double Dist = FindMinDist(SupData.ListConcreteData[0].Centroid, Data, ref MinPtCoordinate, ref IsSymetricCen);
                        if (IsSymetricCen)
                        {
                            DicDistConSup[Data.SuppoId] = Dist;
                        }
                    }
                }
            }

            return DicDistConSup;
        }

        double FindMinDist(Pt3D InPt, SupporSpecData SecondarySup, ref Pt3D point, ref bool IsMidPoint)
        {
            double Dist1 = 0;
            double Dist2 = 0;

            if (InPt != null && SecondarySup.StPt != null && SecondarySup.EndPt != null)
            {
                if (SecondarySup.StPt[0] == 0 && SecondarySup.StPt[1] == 0 && SecondarySup.StPt[2] == 0 && SecondarySup.EndPt[0] == 0 && SecondarySup.EndPt[1] == 0 && SecondarySup.EndPt[2] == 0)
                {
                    return 0;
                }

                Dist1 = Calculate.DistPoint(InPt, GetPt3DFromArray(SecondarySup.StPt));

                Dist2 = Calculate.DistPoint(InPt, GetPt3DFromArray(SecondarySup.EndPt));

                if (Math.Round(Dist1).Equals(Math.Round(Dist2)))
                {
                    Pt3D MidPt = new Pt3D();
                    MidPt.X = (SecondarySup.StPt[0] + SecondarySup.EndPt[0]) / 2;
                    MidPt.Y = (SecondarySup.StPt[1] + SecondarySup.EndPt[1]) / 2;
                    MidPt.Z = (SecondarySup.StPt[2] + SecondarySup.EndPt[2]) / 2;
                    Dist1 = Calculate.DistPoint(InPt, MidPt);

                    point = MidPt;
                    IsMidPoint = true;
                    return Dist1;
                }
                else if (Dist1 < Dist2)
                {
                    point = GetPt3DFromArray(SecondarySup.StPt);
                    return Dist1;
                }
                else
                {
                    point = GetPt3DFromArray(SecondarySup.EndPt);
                    return Dist2;
                }
            }

            return 0;
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

        bool CheckforTypeSupport35(ref SupportData SupportData)
        {
            string Orientation = "";
            if (!AreCentroidsinLine(SupportData, ref Orientation))
            {
                //May be Need to Modify the Support
                System.Windows.Media.Media3D.Vector3D Vec1 = new System.Windows.Media.Media3D.Vector3D(1, 0, 0);
                System.Windows.Media.Media3D.Vector3D Vec2 = new System.Windows.Media.Media3D.Vector3D(0, 1, 0);
                System.Windows.Media.Media3D.Vector3D Vec3 = new System.Windows.Media.Media3D.Vector3D(0, 0, 1);

                Vec1.X = SupportData.ListPrimarySuppo[0].Directionvec.YDirVec.X;
                Vec1.Y = SupportData.ListPrimarySuppo[0].Directionvec.YDirVec.Y;
                Vec1.Z = SupportData.ListPrimarySuppo[0].Directionvec.YDirVec.Z;

                Vec2.X = (SupportData.ListSecondrySuppo[0].EndPt[0] - SupportData.ListSecondrySuppo[0].StPt[0]);
                Vec2.Y = (SupportData.ListSecondrySuppo[0].EndPt[1] - SupportData.ListSecondrySuppo[0].StPt[1]);
                Vec2.Z = (SupportData.ListSecondrySuppo[0].EndPt[2] - SupportData.ListSecondrySuppo[0].StPt[2]);

                if (Math.Round(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(Vec1, Vec2, Vec3))).Equals(0) || Math.Round(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(Vec1, Vec2, Vec3))).Equals(180))
                {
                    SupportData.SupportType = "Support35";
                    return true;
                }
            }
            else
            {
                if (SupportData.ListPrimarySuppo[0].SupportName.ToLower().Contains("custom clamp"))
                    SupportData.SupportType = "Support39";
                return true;
            }
            return false;
        }

        bool CheckforTypeSupport86(ref SupportData SupportData)
        {
            if (GetAllISMC(SupportData).Count == 1)
            {
                List<SupporSpecData> SuppSpecData = new List<SupporSpecData>(0);
                String Orientation = "";
                if (GetAllISMC(SupportData).ElementAt(0).Value.ListtouchingParts.Count == 3)
                {
                    if (!CheckCentroidInLine(GetCentroidFromList(SupportData.ListConcreteData), GetDicCentroidPrimaryPart(SupportData), GetCentroidFromList(SuppSpecData), ref Orientation))
                    {
                        SupportData.SupportType = "Support86";
                        return true;
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
            else
            {
                string Orientation = "";
                if (!AreCentroidsinLine(SupportData, ref Orientation))
                {
                    SupportData.SupportType = "Support14";
                    return true;
                }
                else
                {
                    SupportData.SupportType = "Support17";
                    return true;
                }

            }
            return false;
        }

        // Temporary Hard Coading the support Compare Later we Check All
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
                if (Data == null || Data.SupportName == null || Data.SupportName.ToUpper().Equals("PLATE"))
                {
                    continue;
                }

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
            if (GetAllISMC(SupData).Count != 3)
            {
                return false;
            }

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
                        if (Math.Abs(Math.Round(SupportData.ListSecondrySuppo[0].Angle.XinDegree - SupportData.ListSecondrySuppo[1].Angle.XinDegree)).Equals(45))
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

        bool CheckforTypeSupport34(ref SupportData SupportData)
        {
            int Index0 = 0;
            int Index1 = 0;

            bool IsIndex0 = false;

            for (int inx = 0; inx < SupportData.ListSecondrySuppo.Count; inx++)
            {
                if (SupportData.ListSecondrySuppo[inx].Size != null && SupportData.ListSecondrySuppo[inx].Size.ToLower().Contains("web"))
                {
                    if (!IsIndex0)
                    {
                        Index0 = inx;
                        IsIndex0 = true;
                    }
                    else
                    {
                        Index1 = inx;
                    }
                }
            }

            if (SupportData.ListSecondrySuppo[Index0].Angle.YinDegree.Equals(0) && SupportData.ListSecondrySuppo[Index1].Angle.YinDegree.Equals(0))
            {
                if (SupportData.ListSecondrySuppo[Index0].Angle.ZinDegree.Equals(SupportData.ListSecondrySuppo[Index1].Angle.ZinDegree))
                {
                    if (Math.Abs(SupportData.ListSecondrySuppo[Index0].Angle.XinDegree) > Math.Abs(SupportData.ListSecondrySuppo[Index1].Angle.XinDegree))
                    {
                        if (Math.Abs(SupportData.ListSecondrySuppo[Index0].Angle.XinDegree).Equals(90))
                        {
                            if (Math.Round(SupportData.ListSecondrySuppo[Index0].Centroid.X) == Math.Round(SupportData.ListSecondrySuppo[Index1].Centroid.X) && SupportData.ListSecondrySuppo[Index0].Centroid.Z > SupportData.ListSecondrySuppo[Index1].Centroid.Z)
                            {

                                string Orientation = "";
                                if (!AreCentroidsinLine(SupportData, ref Orientation))
                                {


                                }
                                else
                                {

                                    SupportData.SupportType = "Support34";
                                    SupportData.ListSecondrySuppo[Index0].PartDirection = "Hor";
                                    SupportData.ListSecondrySuppo[Index1].PartDirection = "Ver";
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
                        if (Math.Abs(SupportData.ListSecondrySuppo[Index1].Angle.XinDegree).Equals(90))
                        {
                            if (Math.Round(SupportData.ListSecondrySuppo[Index0].Centroid.X) == Math.Round(SupportData.ListSecondrySuppo[Index1].Centroid.X) && SupportData.ListSecondrySuppo[Index0].Centroid.Z < SupportData.ListSecondrySuppo[Index1].Centroid.Z)
                            {
                                string Orientation = "";
                                if (!AreCentroidsinLine(SupportData, ref Orientation))
                                {

                                }
                                else
                                {

                                    SupportData.SupportType = "Support34";
                                    SupportData.ListSecondrySuppo[Index1].PartDirection = "Hor";
                                    SupportData.ListSecondrySuppo[Index0].PartDirection = "Ver";
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

                                double Dist1 = Calculate.DistPoint(GetPt3DFromArray(SupportData.ListSecondrySuppo[0].StPt), GetPt3DFromPoint3d(MidPt));

                                double Dist2 = Calculate.DistPoint(GetPt3DFromArray(SupportData.ListSecondrySuppo[0].EndPt), GetPt3DFromPoint3d(MidPt));

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

                                double Dist1 = Calculate.DistPoint(GetPt3DFromArray(SupportData.ListSecondrySuppo[1].StPt), GetPt3DFromPoint3d(MidPt));

                                double Dist2 = Calculate.DistPoint(GetPt3DFromArray(SupportData.ListSecondrySuppo[1].EndPt), GetPt3DFromPoint3d(MidPt));

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

                                double Dist1 = Calculate.DistPoint(GetPt3DFromArray(SupportData.ListSecondrySuppo[0].StPt), GetPt3DFromPoint3d(MidPt));

                                double Dist2 = Calculate.DistPoint(GetPt3DFromArray(SupportData.ListSecondrySuppo[0].EndPt), GetPt3DFromPoint3d(MidPt));

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

                                double Dist1 = Calculate.DistPoint(GetPt3DFromArray(SupportData.ListSecondrySuppo[1].StPt), GetPt3DFromPoint3d(MidPt));

                                double Dist2 = Calculate.DistPoint(GetPt3DFromArray(SupportData.ListSecondrySuppo[1].EndPt), GetPt3DFromPoint3d(MidPt));

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
        bool CheckforTypeSupport2(ref SupportData SupportData)
        {
            if (GetAllISMC(SupportData).Count < 1)
            {
                return false;
            }

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

                                double Dist1 = Calculate.DistPoint(GetPt3DFromArray(SupportData.ListSecondrySuppo[0].StPt), GetPt3DFromPoint3d(MidPt));

                                double Dist2 = Calculate.DistPoint(GetPt3DFromArray(SupportData.ListSecondrySuppo[0].EndPt), GetPt3DFromPoint3d(MidPt));

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

                                double Dist1 = Calculate.DistPoint(GetPt3DFromArray(SupportData.ListSecondrySuppo[1].StPt), GetPt3DFromPoint3d(MidPt));

                                double Dist2 = Calculate.DistPoint(GetPt3DFromArray(SupportData.ListSecondrySuppo[1].EndPt), GetPt3DFromPoint3d(MidPt));

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

                                double Dist1 = Calculate.DistPoint(GetPt3DFromArray(SupportData.ListSecondrySuppo[0].StPt), GetPt3DFromPoint3d(MidPt));

                                double Dist2 = Calculate.DistPoint(GetPt3DFromArray(SupportData.ListSecondrySuppo[0].EndPt), GetPt3DFromPoint3d(MidPt));

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

                                double Dist1 = Calculate.DistPoint(GetPt3DFromArray(SupportData.ListSecondrySuppo[1].StPt), GetPt3DFromPoint3d(MidPt));

                                double Dist2 = Calculate.DistPoint(GetPt3DFromArray(SupportData.ListSecondrySuppo[1].EndPt), GetPt3DFromPoint3d(MidPt));

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

            //if (SupportData.ListSecondrySuppo.Any(Y => Y.Size.ToLower().Contains("angle")))
            //{
            //    string Orientation = "";
            //    if (AreCentroidsinLine(SupportData, ref Orientation))
            //    {

            //    }


            //}

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

        bool CheckCentroidInLine(List<Pt3D> BPartCentroids, List<Pt3D> PPartCentroids, List<Pt3D> SPartCentroids, ref string Orientation, double Tolang = 0)
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
                if (Math.Abs(ListXPt[0] - ListXPt[inx]) > 5 + Tolang)
                {
                    AllXAreinLine = false;
                    break;
                }
            }

            for (int inx = 0; inx < ListYPt.Count; inx++)
            {
                if (Math.Abs(ListYPt[0] - ListYPt[inx]) > 5 + Tolang)
                {
                    AllYAreinLine = false;
                    break;
                }
            }

            for (int inx = 0; inx < ListZPt.Count; inx++)
            {
                if (Math.Abs(ListZPt[0] - ListZPt[inx]) > 5 + Tolang)
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

            if (SPartCentroids != null && SPartCentroids.Count > 0)
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
                    if (!BPart.IsGussetplate)
                    {
                        Centroids.Add(BPart.Centroid);
                    }
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

        public void MakeBlockwithTag()
        {
            Document AcadDoc = null;
            Database AcadDatabase = null;
            Document Document2D = null;

            AcadDoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            string Filename = AcadDoc.Name;
            AcadDatabase = AcadDoc.Database;

            /*
            using (AcadDoc.LockDocument())
            {
                using (Transaction AcadTransaction = AcadDatabase.TransactionManager.StartTransaction())
                {
                    int tnx = 1;
                    for (int inx=0; inx<ListCentalSuppoData.Count; inx++)
                    {
                        BlockTable BTable = AcadTransaction.GetObject(AcadDatabase.BlockTableId, OpenMode.ForRead) as BlockTable;

                        tnx = inx;
                        while (BTable.Has("TS-"+tnx))
                        {
                            tnx++;
                        }

                        if (!BTable.Has("TS-" + tnx))
                        {
                            BlockTableRecord AcadBTableRec = new BlockTableRecord();
                            AcadBTableRec.Name = "TS-" + tnx;

                            // Add the new block to the block table

                            BTable.UpgradeOpen();

                            ObjectId btrId = BTable.Add(AcadBTableRec);

                            //tr.AddNewlyCreatedDBObject(btr, true);



                           


                           // DBObjectCollection ents = SquareOfLines(5);

                            //foreach (Entity ent in ents)

                           // {

                                btr.AppendEntity(ent);

                                tr.AddNewlyCreatedDBObject(ent, true);

                           // }



                            // Add a block reference to the model space



                            BlockTableRecord ms =

                              (BlockTableRecord)tr.GetObject(

                                bt[BlockTableRecord.ModelSpace],

                                OpenMode.ForWrite

                              );



                            BlockReference br =

                              new BlockReference(Point3d.Origin, btrId);



                            ms.AppendEntity(br);

                            tr.AddNewlyCreatedDBObject(br, true);


                        }

                        Suppo.ListConcreteData.
                    }
                }
            }*/
        }

        [Obsolete]
        public void Create2D()
        {

#if _DEBUG
            // return ;
#endif
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
            LastLog();
            Database newDb = AcadDatabase.Wblock();
            string Path = System.IO.Path.GetDirectoryName(Filename);
            newDb.SaveAs(Path + "2d.dwg", DwgVersion.Current);
            Thread t = new Thread(SP);
            t.Start();
            t.Join();
            Document2D.CloseAndSave(Path + "2d.dwg");
        }

        void LastLog()
        {
            List<string> NotExtractedSup = new List<string>();
            int CountCreted = 0;
            int CountNotCreted = 0;
            foreach (var Tag in DicTextPos)
            {
                if (Created_TAG.Contains(Tag.Key))
                {
                    CountCreted++;
                }
                else
                {
                    NotExtractedSup.Add(Tag.Key);
                    CountNotCreted++;
                }
            }

            Logger.GetInstance.Debug("Total Tags in drawing: " + DicTextPos.Count.ToString());

            Logger.GetInstance.Debug("Created Support with Tag: " + CountCreted.ToString());

            Logger.GetInstance.Debug("Total supports created :" + Created_TAG.Count.ToString());

            Logger.GetInstance.Debug("Supports not Created but having Tag Count :" + NotExtractedSup.Count.ToString());

            Logger.GetInstance.Debug("Not Created Supports:" + SupportNotCreated.Count.ToString());

            string Name = "";
            int Count = 0;
            foreach (string SupName in SupportNotCreated)
            {
                Name = Name + SupName + ",";

            }

            Logger.GetInstance.Debug("Tag No of Not Created Support:" + Name);
        }
        public void CreateConcreteBOM()
        {

#if _DEBUG
            //return;
#endif
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
            Create2DForConcreteBOM(Document2D);
            //Create2DView(Document2D);

            //Create2DForConcreteBOM1(Document2D);


            Database newDb = AcadDatabase.Wblock();
            string Path = System.IO.Path.GetDirectoryName(Filename);
            newDb.SaveAs(Path + "ConcreteBOM.dwg", DwgVersion.Current);
            Thread t = new Thread(SP);
            t.Start();
            t.Join();
            Document2D.CloseAndSave(Path + "ConcreteBOM.dwg");
        }

        List<Table> CreateBomPedestal(Dictionary<string, PedastalData> BomData)
        {
            int CountBOMData = 0;
            int Level = 0;

            List<Table> ListBomTable = new List<Table>();

            for (int dnx = CountBOMData; (dnx + (150 * Level)) < BomData.Count; dnx++)
            {
                Table BomTable = new Table();
                BomTable.SetSize(150 + 2, 7);
                string[,] str = new string[150 + 2, 7];
                str[0, 0] = "SUPPORT PEDESTAL NO.";
                str[0, 1] = "L";
                str[0, 2] = "W";
                str[0, 3] = "H";
                str[0, 4] = "INSERT PLATE NO";
                str[0, 5] = "QTY";

                //Autodesk.AutoCAD.DatabaseServices.CellRange rowFirst = Autodesk.AutoCAD.DatabaseServices.CellRange.Create(BomTable, 0, 0, 0, 6);
                // BomTable.MergeCells(rowFirst);

                for (int iCo1 = 0; iCo1 < BomTable.Rows.Count; iCo1++)
                    BomTable.Rows[iCo1].Height = 400;

                for (int iCo1 = 0; iCo1 < 6; iCo1++)
                {
                    BomTable.Columns[iCo1].Width = 1000;
                    /* if (iCo1 == 0)
                     {
                         BomTable.Columns[iCo1].Width = 1000;
                     }
                     else if (iCo1 == 1 || iCo1 == 2)
                     {
                         BomTable.Columns[iCo1].Width = 2500;
                     }
                     else if (iCo1 == 3 || iCo1 == 5 || iCo1 == 6)
                     {
                         BomTable.Columns[iCo1].Width = 1500;
                     }
                     else if (iCo1 == 8)
                     {
                         BomTable.Columns[iCo1].Width = 1;
                     }
                     else
                     {
                         BomTable.Columns[iCo1].Width = 1000;
                     }*/
                }

                int iNo = 1;

                for (int inx = 0; inx < 150 && (inx + (150 * Level)) < BomData.Count; inx++)
                {
                    for (int iCount1 = 0; iCount1 < 8; iCount1++)
                    {
                        if (iCount1 == 0)
                            str[iNo, iCount1] = BomData.ElementAt(inx).Key;
                        if (iCount1 == 1)
                            str[iNo, iCount1] = (Math.Round(BomData.ElementAt(inx + (150 * Level)).Value.L)).ToString();
                        if (iCount1 == 2)
                            str[iNo, iCount1] = (Math.Round(BomData.ElementAt(inx + (150 * Level)).Value.W)).ToString();
                        if (iCount1 == 3)
                            str[iNo, iCount1] = (Math.Round(BomData.ElementAt(inx + (150 * Level)).Value.H)).ToString();
                        if (iCount1 == 4)
                            str[iNo, iCount1] = "";
                        if (iCount1 == 5)
                            str[iNo, iCount1] = "1";
                        if (iCount1 == 6)
                            str[iNo, iCount1] = "";
                    }

                    iNo++;
                }

                for (int i = 0; i < BomTable.Rows.Count; i++)
                {
                    for (int j = 0; j < 6; j++)
                    {
                        BomTable.Cells[i, j].TextHeight = 140;
                        BomTable.Cells[i, j].Contents[0].TextString = "{\\fArial;" + str[i, j] + "}";
                        BomTable.Cells[i, j].Alignment = CellAlignment.MiddleCenter;
                    }
                }

                BomTable.DeleteColumns(BomTable.Columns.Count - 1, 1);
                //BomTable.Position = new Point3d(tracex, -BomTable.Height + spaceY, 0);

                ListBomTable.Add(BomTable);
                Level++;
            }
            // AcadBlockTableRecord.AppendEntity(BomTable);
            //AcadTransaction.AddNewlyCreatedDBObject(BomTable, true);

            return ListBomTable;
        }
        Dictionary<string, PedastalData> GetBomData()
        {
            Dictionary<string, PedastalData> BomDic = new Dictionary<string, PedastalData>();

            for (int i = 0; i < ListCentalSuppoData.Count; i++)
            {
                if (ListCentalSuppoData[i].Name != null && ListCentalSuppoData[i].Name.Length > 0)
                {
                    PedastalData PestalSize = new PedastalData();
                    if (ListCentalSuppoData[i].ListConcreteData.Count == 1)
                    {
                        PestalSize = GetSize(ListCentalSuppoData[i].ListConcreteData[0]);
                    }
                    else if (ListCentalSuppoData[i].ListConcreteData.Count == 2)
                    {
                        if ((ListCentalSuppoData[i].ListConcreteData != null) && ListCentalSuppoData[i].ListConcreteData[1].BoxData != null && ListCentalSuppoData[i].ListConcreteData[0].BoxData != null && ListCentalSuppoData[i].ListConcreteData[0].BoxData.Z > ListCentalSuppoData[i].ListConcreteData[1].BoxData.Z)
                        {
                            PestalSize = GetSize(ListCentalSuppoData[i].ListConcreteData[0]);
                        }
                        else
                        {
                            PestalSize = GetSize(ListCentalSuppoData[i].ListConcreteData[1]);
                        }
                    }

                    if (ListCentalSuppoData[i].Name != null && PestalSize != null)
                    {
                        BomDic[ListCentalSuppoData[i].Name] = PestalSize;
                    }
                }
            }

            return BomDic;
        }

        PedastalData GetSize(SupporSpecData Suppo)
        {
            PedastalData PestalSize = new PedastalData();
            if (Suppo.BoxData != null)
            {
                PestalSize.H = Suppo.BoxData.Z;

                if (Suppo.ListfaceData != null)
                {
                    foreach (var face in Suppo.ListfaceData)
                    {
                        if (Math.Abs(face.FaceNormal.X - 0) < 0.02 && Math.Abs(face.FaceNormal.Y - 0) < 0.02 && Math.Abs(face.FaceNormal.Y + 1) < 0.02)
                        {
                            List<double> ListEdgeLen = new List<double>();

                            foreach (var Edge in face.ListlinearEdge)
                            {
                                ListEdgeLen.Add(Edge.EdgeLength);
                            }

                            ListEdgeLen.Sort();

                            if (ListEdgeLen.Count == 4)
                            {
                                PestalSize.L = ListEdgeLen[3];
                                PestalSize.W = ListEdgeLen[1];
                            }


                        }
                    }
                }

                if (PestalSize.L == 0 && PestalSize.W == 0)
                {
                    PestalSize.L = Suppo.BoxData.X;
                    PestalSize.W = Suppo.BoxData.Y;
                }
            }
            return PestalSize;
        }

        string GetSizeofPlate(List<FaceData> ListfaceData)
        {
            string Size = "";
            List<double> ListEdgeLen = new List<double>();
            foreach (var face in ListfaceData)
            {
                foreach (var Edge in face.ListlinearEdge)
                {
                    ListEdgeLen.Add(Edge.EdgeLength);
                }
            }

            ListEdgeLen.Sort();

            if (ListEdgeLen.Count == 24)
            {
                Size = Math.Abs(ListEdgeLen[16]).ToString() + "x" + Math.Abs(ListEdgeLen[8]).ToString() + "x" + Math.Abs(ListEdgeLen[0]).ToString();
            }

            return Size;
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

                string suptype = ListCentalSuppoData[0].SupportType;

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

                //angle data
                DicWtData["ISA25X25X 3THK"] = 1.1;
                DicWtData["ISA30X30X 5THK"] = 2.2;
                DicWtData["ISA40X40X 5THK"] = 3;
                DicWtData["ISA40X40X 6THK"] = 3.5;
                DicWtData["ISA50X50X 5THK"] = 3.8;
                DicWtData["ISA50X50X 6THK"] = 4.5;
                DicWtData["ISA65X65X 5THK"] = 4.9;
                DicWtData["ISA65X65X 6THK"] = 5.8;
                DicWtData["ISA75X75X 5THK"] = 5.7;
                DicWtData["ISA75X75X 6THK"] = 6.8;
                DicWtData["ISA75X75X 10THK"] = 11;
                DicWtData["ISA100X100X 6THK"] = 9.2;
                DicWtData["ISA100X100X 10THK"] = 14.9;

                //NB Type data
                DicWtData["15 NB"] = 1.22;
                DicWtData["20 NB"] = 1.58;
                DicWtData["25 NB"] = 2.44;
                DicWtData["32 NB"] = 3.14;
                DicWtData["40 NB"] = 3.61;
                DicWtData["50 NB"] = 5.1;
                DicWtData["65 NB"] = 6.54;
                DicWtData["80 NB"] = 8.53;
                DicWtData["100 NB"] = 12.5;
                DicWtData["125 NB"] = 16.4;
                DicWtData["150 NB"] = 19.5;
                DicWtData["200 NB"] = 23.8;
                DicWtData["250 NB"] = 33;
                DicWtData["300 NB"] = 35.4;
                DicWtData["350 NB"] = 43.2;
                DicWtData["400 NB"] = 49.5;
                DicWtData["450 NB"] = 55.7;
                DicWtData["500 NB"] = 62;

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

                //angle data
                DicSurfaceAData["ISA25X25X 3THK"] = 0.1;
                DicSurfaceAData["ISA30X30X 5THK"] = 0.12;
                DicSurfaceAData["ISA40X40X 5THK"] = 0.16;
                DicSurfaceAData["ISA40X40X 6THK"] = 0.16;
                DicSurfaceAData["ISA50X50X 5THK"] = 0.2;
                DicSurfaceAData["ISA50X50X 6THK"] = 0.2;
                DicSurfaceAData["ISA65X65X 5THK"] = 0.26;
                DicSurfaceAData["ISA65X65X 6THK"] = 0.26;
                DicSurfaceAData["ISA75X75X 5THK"] = 0.3;
                DicSurfaceAData["ISA75X75X 6THK"] = 0.3;
                DicSurfaceAData["ISA75X75X 10THK"] = 0.3;
                DicSurfaceAData["ISA100X100X 6THK"] = 0.4;
                DicSurfaceAData["ISA100X100X 10THK"] = 0.4;

                //NB data
                DicSurfaceAData["15 NB"] = 0.07;
                DicSurfaceAData["20 NB"] = 0.08;
                DicSurfaceAData["25 NB"] = 0.10;
                DicSurfaceAData["32 NB"] = 0.13;
                DicSurfaceAData["40 NB"] = 0.15;
                DicSurfaceAData["50 NB"] = 0.19;
                DicSurfaceAData["65 NB"] = 0.24;
                DicSurfaceAData["80 NB"] = 0.28;
                DicSurfaceAData["100 NB"] = 0.36;
                DicSurfaceAData["125 NB"] = 0.44;
                DicSurfaceAData["150 NB"] = 0.53;
                DicSurfaceAData["200 NB"] = 0.69;
                DicSurfaceAData["250 NB"] = 0.86;
                DicSurfaceAData["300 NB"] = 1.02;
                DicSurfaceAData["350 NB"] = 1.12;
                DicSurfaceAData["400 NB"] = 1.28;
                DicSurfaceAData["450 NB"] = 1.44;
                DicSurfaceAData["500 NB"] = 1.60;


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


                for (int i = 0; i < ListCentalSuppoData.Count; i++)
                {
                    if (ListCentalSuppoData[i].SupportType == "Support13")
                    {
                        CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i);
                    }
                }
                //for (int j = 0; j <= 1; j++)
                //{
                for (int i = 0; i < ListCentalSuppoData.Count; i++)
                {
                    if (ListCentalSuppoData[i].SupportType == "Support13")
                    {
                        continue;
                    }
                    CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, null, i);
                }
#if _DEBUG


                CreateFullBlock(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, "Support96");
#endif

                Dictionary<string, List<double>> ListBomData = new Dictionary<string, List<double>>();

                Dictionary<string, int> UClampData = new Dictionary<string, int>();

                int AnchorHoles = 0;
                foreach (var SData in ListCentalSuppoData)
                {
                    //if (SData.SupportType != null)
                    // {
                    foreach (var SType in SData.ListSecondrySuppo)
                    {
                        if (SType.SupportName != null && SType.SupportName.ToLower().Equals("plate") && SType.IsGussetplate)
                        {
                            string Size = "GUSSET PLATE";

                            int count = 1;
                            if (Size != null && Size.Length > 0)
                            {
                                if (UClampData.ContainsKey(Size))
                                {
                                    count = UClampData[Size];
                                    count++;
                                    UClampData[Size] = count;
                                }
                                else
                                {
                                    UClampData[Size] = count;
                                }
                            }
                        }
                        else if (SType.Size != null && SType.Size.ToLower().Contains("shape"))
                        {
                            //string Size = GetIShapeSize(SType.Size);
                            //List<double> Temp = new List<double>();
                            //double Dist = Calculate.DistPoint(GetPt3DFromArray(SType.StPt), GetPt3DFromArray(SType.EndPt));

                            //if (ListBomData.ContainsKey(Size))
                            //{
                            //    Temp = ListBomData[Size];
                            //    Temp.Add(Dist);

                            //    ListBomData[Size] = Temp;
                            //}
                            //else
                            //{
                            //    Temp.Add(Dist);

                            //    ListBomData[Size] = Temp;
                            //}
                        }

                        else if (SType.Size != null && (SType.Size.ToLower().Contains("web") || SType.Size.ToLower().Contains("ismc")))
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
                        else if (SType.Size != null && (SType.Size.ToLower().Contains("angle") || SType.Size.ToLower().Contains("isa")))
                        {
                            string Size = GettheISA(SType);

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

                        else if (SType.Size != null && SType.Size.ToLower().Contains("nb"))
                        {
                            string Size = GettheNB(SType.Size);

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
                    foreach (var PType in SData.ListPrimarySuppo)
                    {
                        if (PType.SupportName.ToLower().Contains("clamp"))
                        {
                            string Size = PType.Size + "NB";

                            int count = 1;
                            if (Size != null && Size.Length > 0)
                            {
                                if (UClampData.ContainsKey(Size))
                                {
                                    count = UClampData[Size];
                                    count++;
                                    UClampData[Size] = count;
                                }
                                else
                                {
                                    UClampData[Size] = count;
                                }
                            }
                        }
                    }

                    foreach (var CType in SData.ListConcreteData)
                    {
                        if (CType.IsAnchor)
                        {
                            string Size = GetSizeofPlate(CType.ListfaceData);

                            if (Size != null && Size.Length > 0 && CType.NoOfAnchoreHole > 0)
                            {
                                Size = "ANCHOR PLATE" + Size + " THK";
                                int count = 1;
                                if (Size != null && Size.Length > 0)
                                {
                                    if (UClampData.ContainsKey(Size))
                                    {
                                        count = UClampData[Size];
                                        count++;
                                        UClampData[Size] = count;
                                    }
                                    else
                                    {
                                        UClampData[Size] = count;
                                    }
                                }
                            }
                            AnchorHoles = AnchorHoles + CType.NoOfAnchoreHole;
                        }
                    }

                    if (AnchorHoles > 0)
                    {
                        UClampData["ANCHOR FASTNER"] = AnchorHoles;
                    }
                    //}
                }

                Table BomTable = new Table();

                BomTable.SetSize(UClampData.Count + ListBomData.Count + 3, 9);
                string[,] str = new string[UClampData.Count + ListBomData.Count + 3, 9];

                str[0, 0] = "BOM";
                str[1, 0] = "SR. No.";
                str[1, 1] = "ITEM DESCRIPTION";
                str[1, 2] = "MATERIAL";
                str[1, 3] = "QTY";
                str[1, 4] = "UOM";
                str[1, 5] = "WEIGHT(KG)";
                str[1, 6] = "PAINTING AREA (METER sq.)";
                str[1, 7] = "Remark";

                Autodesk.AutoCAD.DatabaseServices.CellRange rowFirst = Autodesk.AutoCAD.DatabaseServices.CellRange.Create(BomTable, 0, 0, 0, 8);
                BomTable.MergeCells(rowFirst);

                for (int iCo1 = 0; iCo1 < BomTable.Rows.Count; iCo1++)
                    BomTable.Rows[iCo1].Height = 400;

                for (int iCo1 = 0; iCo1 < 9; iCo1++)
                {
                    if (iCo1 == 0)
                    {
                        BomTable.Columns[iCo1].Width = 1000;
                    }
                    else if (iCo1 == 1 || iCo1 == 2)
                    {
                        BomTable.Columns[iCo1].Width = 2500;
                    }
                    else if (iCo1 == 3 || iCo1 == 5 || iCo1 == 6)
                    {
                        BomTable.Columns[iCo1].Width = 1500;
                    }
                    else if (iCo1 == 8)
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
                    for (int iCount1 = 0; iCount1 < 8; iCount1++)
                    {
                        if (iCount1 == 0)
                            str[iNo, iCount1] = (inx + 1).ToString();
                        if (iCount1 == 1)
                            str[iNo, iCount1] = ListBomData.ElementAt(inx).Key;
                        if (iCount1 == 2)
                            str[iNo, iCount1] = "IS:2062 Gr.E250A";
                        if (iCount1 == 3)
                            str[iNo, iCount1] = (Math.Round(GetQuantityofParts(ListBomData.ElementAt(inx).Value), 1)).ToString();
                        if (iCount1 == 4)
                            str[iNo, iCount1] = "M";
                        if (iCount1 == 5)
                            str[iNo, iCount1] = (Math.Round(GetQuantityofParts(ListBomData.ElementAt(inx).Value) * DicWtData[ListBomData.ElementAt(inx).Key], 2)).ToString();
                        if (iCount1 == 6)
                            str[iNo, iCount1] = (Math.Round((DicSurfaceAData.ContainsKey(ListBomData.ElementAt(inx).Key) ? DicSurfaceAData[ListBomData.ElementAt(inx).Key] : 0) * GetQuantityofParts(ListBomData.ElementAt(inx).Value), 1)).ToString();

                        if (iCount1 == 7)
                            str[iNo, iCount1] = " ";
                    }
                    Weight = Weight + Math.Round(GetQuantityofParts(ListBomData.ElementAt(inx).Value) * DicWtData[ListBomData.ElementAt(inx).Key], 2);

                    SurfaceArea = SurfaceArea + Math.Round((DicSurfaceAData.ContainsKey(ListBomData.ElementAt(inx).Key) ? DicSurfaceAData[ListBomData.ElementAt(inx).Key] : 0) * GetQuantityofParts(ListBomData.ElementAt(inx).Value), 1);
                    iNo++;
                }

                for (int inx = 0; inx < UClampData.Count; inx++)
                {
                    for (int iCount1 = 0; iCount1 < 8; iCount1++)
                    {
                        if (iCount1 == 0)
                            str[iNo, iCount1] = (iNo - 1).ToString();
                        if (iCount1 == 1)
                            str[iNo, iCount1] = UClampData.ElementAt(inx).Key.ToUpper().Contains("NB") ? "'U'" + "  Clamp" + UClampData.ElementAt(inx).Key : UClampData.ElementAt(inx).Key;
                        if (iCount1 == 2)
                            str[iNo, iCount1] = "IS:2062 Gr.E250A";
                        if (iCount1 == 3)
                            str[iNo, iCount1] = (UClampData.ElementAt(inx).Value).ToString();
                        if (iCount1 == 4)
                            str[iNo, iCount1] = UClampData.ElementAt(inx).Value == 1 ? "NO" : "NOS";
                        if (iCount1 == 5)
                            str[iNo, iCount1] = " ";
                        if (iCount1 == 6)
                            str[iNo, iCount1] = " ";

                        if (iCount1 == 7)
                            str[iNo, iCount1] = " ";
                    }

                    iNo++;
                }

                for (int i = 0; i < BomTable.Rows.Count; i++)
                {
                    if (i == BomTable.Rows.Count - 1)
                    {
                        BomTable.Cells[i, 0].TextHeight = 140;
                        BomTable.Cells[i, 0].Contents[0].TextString = "{\\fArial;" + "Total" + "}";
                        BomTable.Cells[i, 0].Alignment = CellAlignment.MiddleCenter;

                        BomTable.Cells[i, 5].TextHeight = 140;
                        BomTable.Cells[i, 5].Contents[0].TextString = "{\\fArial;" + Weight.ToString() + "}";
                        BomTable.Cells[i, 5].Alignment = CellAlignment.MiddleCenter;

                        BomTable.Cells[i, 6].TextHeight = 140;
                        BomTable.Cells[i, 6].Contents[0].TextString = "{\\fArial;" + SurfaceArea.ToString() + "}";
                        BomTable.Cells[i, 6].Alignment = CellAlignment.MiddleCenter;
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




                ////BOM

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


                //AddDimStyleToDimensions();
                AcadTransaction.Commit();
            }
        }

        private string GettheNB(string size)
        {
            Dictionary<string, string> DicNBcode = new Dictionary<string, string>();
            DicNBcode["15 NB"] = "IS 1239 Medium Duty";
            DicNBcode["20 NB"] = "IS 1239 Medium Duty";
            DicNBcode["25  NB"] = "IS 1239 Medium Duty";
            DicNBcode["32 NB"] = "IS 1239 Medium Duty";
            DicNBcode["20 NB"] = "IS 1239 Medium Duty";
            DicNBcode["40 NB"] = "IS 1239 Medium Duty";
            DicNBcode["50 NB"] = "IS 1239 Medium Duty";
            DicNBcode["60 NB"] = "IS 1239 Medium Duty";
            DicNBcode["65 NB"] = "IS 1239 Medium Duty";
            DicNBcode["80 NB"] = "IS 1239 Medium Duty";
            DicNBcode["100 NB"] = "IS 1239 Medium Duty";
            DicNBcode["125 NB"] = "IS 1239 Medium Duty";
            DicNBcode["150 NB"] = "IS 1239 Medium Duty";
            DicNBcode["200 NB"] = "IS 3589 Gr. 330, 4.5 Thk";
            DicNBcode["250 NB"] = "IS 3589 Gr. 330, 5 Thk";
            DicNBcode["300 NB"] = "IS 3589 Gr. 330, 4.5 Thk";
            DicNBcode["350  NB"] = "IS 3589 Gr. 330, 5 Thk";
            DicNBcode["400 NB"] = "IS 3589 Gr. 330, 5 Thk";
            DicNBcode["450 NB"] = "IS 3589 Gr. 330, 5 Thk";
            DicNBcode["500 NB"] = "IS 3589 Gr. 330, 5 Thk";

            foreach (var Item in DicNBcode)
            {
                if (size.ToUpper().Contains(Item.Key))
                {
                    return Item.Key;
                }
            }

            return "";
        }

        private string GetIShapeSize(string size)
        {
            return size.Substring(0, size.IndexOf("w"));
        }

        string GettheISA(SupporSpecData Type)
        {
            string AngleSize = "";

            if (Type.LSecData != null && Type.LSecData.AngleSize > 0 && Type.LSecData.AngleThck > 0)
            {
                AngleSize = "ISA" + Math.Abs(Math.Round(Type.LSecData.AngleSize)).ToString() + "X" + Math.Abs(Math.Round(Type.LSecData.AngleSize)).ToString() + "X" + " " + Math.Abs(Math.Round(Type.LSecData.AngleThck)).ToString() + "THK";
            }

            return AngleSize;
        }
        void Create2DForConcreteBOM(Document Document2D)
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

                //logic for adding block      

                //9869.9480;                  
                //67542.6980;
                //adding blocks here

                spaceX = 81971.6112;
                spaceY = 71075.0829;

                tempX = 101659.6570;
                tempY = 71694.2039;


                double boxlen = 13483.4661;  //for concrete 17299.3016;
                double boxht = 12734.3388;

                double tracex = 619.1209;

                string suptype = "";

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
                ConcreteSize_WithTAG.Clear();
                try
                {
                    for (int i = 0; i < ListCentalSuppoData.Count; i++)
                    {
                        if (ListCentalSuppoData[i].ListConcreteData.Count > 0 && ListCentalSuppoData[i].Name.Length > 0)
                        {
                            var concrete_sup = ListCentalSuppoData[i].ListConcreteData.OrderByDescending(e => e.BoxData.Z);
                            //foreach (SupporSpecData sp in ListCentalSuppoData[i].ListConcreteData)
                            //{

                            datum.Add(concrete_sup.ElementAt(0).Boundingboxmin.Z);
                            //}
                            AddConcretBOM(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i, concrete_sup);
                            ConcreteSize_WithTAG[ListCentalSuppoData[i].Name] = MillimetersToMeters(concrete_sup.ElementAt(0).Boundingboxmin.Z);
                            //datum.Add(ListCentalSuppoData[i].ListConcreteData[0].Boundingboxmin.Z);
                        }

                    }
                }
                catch (Exception e)
                {

                }

                if (datum.Count > 0)
                {
                    datum_level = MillimetersToMeters(Math.Round(datum.Min()));
                }

                Dictionary<string, PedastalData> BomData = new Dictionary<string, PedastalData>();

                List<Table> BomTables = new List<Table>();

                BomData = GetBomData();
                BomTables = CreateBomPedestal(BomData);

                spaceY = 71075.0829;
                tracex = tempX + 10000 + 619.1209;

                CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Mtemp", tracex - 619.1209, 0, 1);

                for (int mnx = 0; mnx < BomTables.Count; mnx++)
                {
                    BomTables[mnx].Position = new Point3d(tracex + 619.1209 + (mnx * 8000), spaceY, 0);

                    BomTables[mnx].GenerateLayout();
                    AcadBlockTableRecord.AppendEntity(BomTables[mnx]);
                    AcadTransaction.AddNewlyCreatedDBObject(BomTables[mnx], true);
                }
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
            info[Defination.Prim_Radius] = 500;
            info[Defination.Prim_ht] = 500;
            info[Defination.Concrete_l] = 3000;
            info[Defination.Concrete_b] = 1000;

            if (SupType == SupportType.Support13.ToString())
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
            else
            {

                bool CheckFr3rdDim = IsThirdDim(i);

                if (CheckFr3rdDim)
                {
                    boxlen = 32000;
                }
                else
                {
                    boxlen = 17299.3016;
                }

                boxht = 12734.3388;
                if (tracex >= spaceX - boxlen)
                {
                    spaceY -= boxht;
                    tracex = tempX - 101659.6570 + 619.1209;
                }
                if (spaceY > boxht)
                {
                    ProjectTo2D(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i, CheckFr3rdDim);
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
                    ProjectTo2D(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, Document2D, ListCentalSuppoData[i].SupportType, i, CheckFr3rdDim);
                }

            }
        }

        public bool IsThirdDim(int i)
        {
            //take vectors all sec supports except plate      
            List<Vector3D> allsecpart = new List<Vector3D>();
            foreach (SupporSpecData sec in ListCentalSuppoData[i].ListSecondrySuppo)
            {
                Vector3D secvec = new Vector3D();

                if (sec.SupportName != null && sec.SupportName.ToLower().Contains("plate") || sec.IsGussetplate || sec.IsAnchor)
                {
                    continue;
                }
                secvec.X = sec.EndPt[0] - sec.StPt[0];
                secvec.Y = sec.EndPt[1] - sec.StPt[1];
                secvec.Z = sec.EndPt[2] - sec.StPt[2];
                allsecpart.Add(secvec);
            }


            return !CheckVectorsArePlaner(allsecpart);
        }

        public void AddConcretBOM(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, ref double tracex, double boxlen, double boxht, ref double spaceY, Document Document2D, string SupType, [Optional] int i, [Optional] IOrderedEnumerable<SupporSpecData> desc_list)
        {
            //boxlen = 17299.3016;
            //value came by scaling block to a height
            boxlen = 13483.4661;
            boxht = 12734;
            if (tracex >= spaceX - boxlen)
            {
                spaceY -= boxht;
                tracex = tempX - 101659.6570 + 619.1209;
            }
            if (spaceY > boxht)
            {
                BLOCK_WITH_ATTRIBUTES(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Concrete_BOM", ref tracex, ref spaceY, 1, desc_list: desc_list, i: i);
                tracex += boxlen;
            }
            else
            {

                spaceY = 71075.0829;
                tracex = tempX + 10000 + 619.1209;//gaps
                                                  // gets the template
                                                  // GetTemplate(AcadBlockTableRecord, AcadTransaction, AcadDatabase,tracex- 619.1209);

                // CopyPasteTemplateFile("Temp1", Document2D, tracex - 619.1209);
                BLOCK_WITH_ATTRIBUTES(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Concrete_BOM", ref tracex, ref spaceY, 1, desc_list: desc_list, i: i);

                tempX += 101659.6570 + 10000;
                spaceX = tempX - 19068.9248;

                CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Mtemp", tracex - 619.1209, 0, 1);

                tracex += boxlen;
            }
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



        public void CopyAndModifyEntities(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, string sourceDwg, double insertptX, double insertptY, double scaleFactor, [Optional] double rotationAngle, [Optional] Point3d rotationPoint)
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


                //for rotation
                if (rotationAngle != 0)
                {
                    // Define the rotation transformation
                    Matrix3d rotationMatrix = Matrix3d.Rotation(rotationAngle, Vector3d.ZAxis, rotationPoint);

                    // Apply the rotation transformation to the entity
                    sourceEntity.TransformBy(rotationMatrix);
                }


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

        public void BLOCK_WITH_ATTRIBUTES(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, string sourceDwg, ref double insertptX, ref double insertptY, double scaleFactor, [Optional] double rotationAngle, [Optional] Point3d rotationPoint, [Optional] IOrderedEnumerable<SupporSpecData> desc_list, int i = 0)
        {

            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);

            string workingDirectory = Path.GetDirectoryName(path);

            // Get the project file path by searching for the .csproj file in the working directory
            string sourceDwgPath = Directory.GetFiles(workingDirectory, sourceDwg + ".dwg").FirstOrDefault();

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
                var sourceEntity = tr2.GetObject(sourceObjectId, OpenMode.ForWrite) as BlockReference;

                var blockref = tr2.GetObject(sourceObjectId, OpenMode.ForWrite) as BlockReference;
                ColorMethod colorMethod = ColorMethod.ByLayer;
                int colorMethodCode = (int)colorMethod;
                sourceEntity.ColorIndex = colorMethodCode;

                try
                {
                    foreach (ObjectId attId in blockref.AttributeCollection)
                    {
                        AttributeReference attRef = tr2.GetObject(attId, OpenMode.ForRead) as AttributeReference;
                        if (attRef != null && attRef.Tag.Equals("height", StringComparison.OrdinalIgnoreCase))
                        {
                            attRef.UpgradeOpen();
                            attRef.TextString = Math.Round(desc_list.ElementAt(0).BoxData.Z).ToString();
                            attRef.DowngradeOpen();
                        }
                        else if (attRef != null && attRef.Tag.Equals("width", StringComparison.OrdinalIgnoreCase))
                        {
                            attRef.UpgradeOpen();
                            attRef.TextString = Math.Round(desc_list.ElementAt(0).BoxData.Y).ToString();
                            attRef.DowngradeOpen();
                        }
                        else if (attRef != null && attRef.Tag.Equals("length", StringComparison.OrdinalIgnoreCase))
                        {
                            attRef.UpgradeOpen();
                            attRef.TextString = Math.Round(desc_list.ElementAt(0).BoxData.X).ToString();
                            attRef.DowngradeOpen();
                        }
                        else if (attRef != null && attRef.Tag.Equals("datum_ht", StringComparison.OrdinalIgnoreCase))
                        {
                            attRef.UpgradeOpen();
                            attRef.TextString = Math.Round(MillimetersToMeters(desc_list.ElementAt(0).Boundingboxmin.Z), 2).ToString();
                            attRef.DowngradeOpen();
                        }
                        else if (attRef != null && attRef.Tag.Equals("tagname", StringComparison.OrdinalIgnoreCase))
                        {
                            attRef.UpgradeOpen();
                            attRef.TextString = ListCentalSuppoData[i].Name;
                            attRef.DowngradeOpen();
                        }
                    }



                    //foreach (ObjectId attributeId in blockref.AttributeCollection)
                    //{

                    //    var entity = tr2.GetObject(attributeId, OpenMode.ForRead) as Entity;
                    //    if (entity is AttributeDefinition)
                    //    {
                    //        var attributeDef = entity as AttributeDefinition;

                    //        // Set the values for the attributes
                    //        if (attributeDef.Tag == "ht")
                    //        {
                    //            var attributeValue = "10"; // Specify the value for the "ht" attribute
                    //            blockref.SetAttributeFromBlock(attributeDef, attributeValue);

                    //        }
                    //        else if (attributeDef.Tag == "wd")
                    //        {
                    //            var attributeValue = "20"; // Specify the value for the "wd" attribute
                    //            blockref.SetAttributeFromBlock(attributeDef, attributeValue);
                    //        }
                    //        else if (attributeDef.Tag == "lt")
                    //        {
                    //            var attributeValue = "30"; // Specify the value for the "lt" attribute
                    //            blockref.SetAttributeFromBlock(attributeDef, attributeValue);
                    //        }
                    //        else if (attributeDef.Tag == "dh")
                    //        {
                    //            var attributeValue = "40"; // Specify the value for the "dh" attribute
                    //            blockref.SetAttributeFromBlock(attributeDef, attributeValue);
                    //        }
                    //    }
                    //}




                }
                catch (Exception)
                {

                }




                Point3d strpt = new Point3d(0, 0, 0);
                Vector3d destvect = strpt.GetVectorTo(new Point3d(insertptX, insertptY, 0));

                Matrix3d scaleMatrix = Matrix3d.Scaling(scaleFactor, new Point3d(0, 0, 0));

                // Apply the scaling matrix to the entity
                sourceEntity.TransformBy(scaleMatrix);

                sourceEntity.TransformBy(Matrix3d.Displacement(destvect));


                //for rotation
                if (rotationAngle != 0)
                {
                    // Define the rotation transformation
                    Matrix3d rotationMatrix = Matrix3d.Rotation(rotationAngle, Vector3d.ZAxis, rotationPoint);

                    // Apply the rotation transformation to the entity
                    sourceEntity.TransformBy(rotationMatrix);
                }


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


        public GeometRi.Point3d CalculateCentroid(List<GeometRi.Segment3d> lines)
        {
            double totalX = 0.0;
            double totalY = 0.0;

            foreach (var line in lines)
            {
                totalX += line.P1.X + line.P2.X;
                totalY += line.P1.Y + line.P2.Y;
            }

            int lineCount = lines.Count * 2;
            double centroidX = totalX / lineCount;
            double centroidY = totalY / lineCount;

            return new GeometRi.Point3d(centroidX, centroidY, 0);
        }



        //fix new function for Bottom Support Top

        [Obsolete]
        private void FixCreateBottomSupportTopType2(Document Document2D, BlockTableRecord acadBlockTableRecord, Transaction acadTransaction, Database acadDatabase, double centerX, double centerY, double height, double length, [Optional] int i, string weldreq = "a")
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
                    dim.DimensionText = Math.Round(Math.Max(ListCentalSuppoData[i].ListConcreteData[0].BoxData.Z, ListCentalSuppoData[i].ListConcreteData[1].BoxData.Z)).ToString();
                }
                else
                {
                    dim.DimensionText = Math.Round(ListCentalSuppoData[i].ListConcreteData[0].BoxData.Z).ToString();
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


            var con_datum_ht = Math.Round((MillimetersToMeters((ListCentalSuppoData[i].ListConcreteData.Where(e => e.Boundingboxmin.Z == ListCentalSuppoData[i].ListConcreteData.Min(s => s.Boundingboxmin.Z)).First().Boundingboxmin.Z))), 2);
            try
            {
                //mtext
                CreateMtextfunc(acadBlockTableRecord, acadTransaction, acadDatabase, new Point3d(centerX + length / 2 + 2500, centerY - ht_frm_cen - height + 300, 0), "HPP.(+)" + con_datum_ht);

            }
            catch (Exception)
            {

            }


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

            try
            {

                if (weldreq.ToLower() == "a")
                {
                    CreateLeaderfromfile(acadBlockTableRecord, acadTransaction, acadDatabase, centerX + length / 2 - offsetinside - 450, centerY - ht_frm_cen);
                }
            }
            catch (Exception)
            {

            }




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

        public void CreateDimension(Point3d strpt, Point3d endpt, [Optional] string topsec, double vertical_offset = 1200, double text_pos_rotation = 0, double horizontal_offset = 0)
        {
            if ((strpt.X == 0 && strpt.Y == 0 && strpt.Z == 0) || (endpt.X == 0 && endpt.Y == 0 && endpt.Z == 0))
            {
                return;
            }

            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            //dimension line point

            // Create the dimension
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForWrite);
                RotatedDimension dim = new RotatedDimension(text_pos_rotation, strpt, endpt, new Point3d(endpt.X + horizontal_offset, endpt.Y + vertical_offset, 0), "", ObjectId.Null);

                dim.Dimtxt = 70;
                dim.Dimasz = 70;

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
        private void FixCreatePrimarySupportwithvertex(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, double centerX, double centerY, double radius, string suppnameside = "r", string supportName = "y")
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


            //addition of outer part of prim support
            try
            {
                CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Prim_Outer_Part", center.X, center.Y, radius);
            }
            catch (Exception)
            {

            }


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
            if (supportName.ToLower() == "y")
            {
                if (suppnameside == "l" || suppnameside == "L")
                {
                    CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - radius - 2000, centerY + 300, 0), ListCentalSuppoData[0].ListPrimarySuppo[0].SupportName);

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

            //last joining line
            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, line1.StartPoint, line2.StartPoint, MyCol.LightBlue);
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
        private void BoxGenCreateSecondarySupportBottom(BlockTableRecord acadBlockTableRecord, Transaction AcadTransaction, Database acadDatabase, Point3d lefttop, Point3d righttop, Point3d rightbot, Point3d leftbot, SecThick secthik, [Optional] string ISMCTAGDir, double verti_tag_offset = 0, double sectionsize = 100, string Centerline = "A")
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
            //for L-section 
            if (ISMCTAGDir == "LL" || ISMCTAGDir == "ll")
            {
                DrawLeader(acadBlockTableRecord, AcadTransaction, acadDatabase, "ISA " + sectionsize, new Point3d(lefttop.X, (lefttop.Y + leftbot.Y) / 2 + verti_tag_offset, 0), new Point3d(lefttop.X - 2000, (lefttop.Y + leftbot.Y) / 2 + verti_tag_offset, 0));
            }
            else if (ISMCTAGDir == "LR" || ISMCTAGDir == "lr")
            {
                DrawLeader(acadBlockTableRecord, AcadTransaction, acadDatabase, "ISA " + sectionsize, new Point3d(righttop.X, (righttop.Y + rightbot.Y) / 2 + verti_tag_offset, 0), new Point3d(righttop.X + 2000, (righttop.Y + rightbot.Y) / 2 + verti_tag_offset, 0));
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
            //for gusset plate
            else if (ISMCTAGDir == "GPL" || ISMCTAGDir == "gpl")
            {
                DrawLeader(acadBlockTableRecord, AcadTransaction, acadDatabase, "GUSSET", new Point3d(lefttop.X, (lefttop.Y + leftbot.Y) / 2 + verti_tag_offset, 0), new Point3d(lefttop.X - 2000, (lefttop.Y + leftbot.Y) / 2 + verti_tag_offset, 0));
            }
            else if (ISMCTAGDir == "GPR" || ISMCTAGDir == "gpr")
            {
                DrawLeader(acadBlockTableRecord, AcadTransaction, acadDatabase, "GUSSET", new Point3d(righttop.X, (righttop.Y + rightbot.Y) / 2 + verti_tag_offset, 0), new Point3d(righttop.X + 2000, (righttop.Y + rightbot.Y) / 2 + verti_tag_offset, 0));
            }

            //for plate
            else if (ISMCTAGDir == "PL" || ISMCTAGDir == "pl")
            {
                DrawLeader(acadBlockTableRecord, AcadTransaction, acadDatabase, "INSERT PLATE ", new Point3d(lefttop.X, (lefttop.Y + leftbot.Y) / 2 + verti_tag_offset, 0), new Point3d(lefttop.X - 2000, (lefttop.Y + leftbot.Y) / 2 + verti_tag_offset, 0));
            }
            else if (ISMCTAGDir == "PR" || ISMCTAGDir == "pr")
            {
                DrawLeader(acadBlockTableRecord, AcadTransaction, acadDatabase, "INSERT PLATE ", new Point3d(righttop.X, (righttop.Y + rightbot.Y) / 2 + verti_tag_offset, 0), new Point3d(righttop.X + 2000, (righttop.Y + rightbot.Y) / 2 + verti_tag_offset, 0));
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
                    LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, new Point3d(lefttop.X + thickness, lefttop.Y, 0), new Point3d(leftbot.X + thickness, leftbot.Y, 0), MyCol.LightBlue, "DASHED", 500);
                    LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, new Point3d(righttop.X - thickness, righttop.Y, 0), new Point3d(rightbot.X - thickness, rightbot.Y, 0), MyCol.LightBlue, "DASHED", 500);
                    break;
                case SecThick.Left:
                    // code block
                    LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, new Point3d(lefttop.X + thickness, lefttop.Y, 0), new Point3d(leftbot.X + thickness, leftbot.Y, 0), MyCol.LightBlue);
                    break;
                case SecThick.HidLeft:
                    // code block
                    LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, new Point3d(lefttop.X + thickness, lefttop.Y, 0), new Point3d(leftbot.X + thickness, leftbot.Y, 0), MyCol.LightBlue, "DASHED", 500);
                    break;
                case SecThick.Right:
                    // code block
                    LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, new Point3d(righttop.X - thickness, righttop.Y, 0), new Point3d(rightbot.X - thickness, rightbot.Y, 0), MyCol.LightBlue);
                    break;
                case SecThick.HidRight:
                    // code block
                    LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, new Point3d(righttop.X - thickness, righttop.Y, 0), new Point3d(rightbot.X - thickness, rightbot.Y, 0), MyCol.LightBlue, "DASHED", 500);
                    break;



                case SecThick.HBoth:
                    // code block
                    LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, new Point3d(lefttop.X, lefttop.Y - thickness, 0), new Point3d(righttop.X, righttop.Y - thickness, 0), MyCol.LightBlue);
                    LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, new Point3d(leftbot.X, leftbot.Y + thickness, 0), new Point3d(rightbot.X, rightbot.Y + thickness, 0), MyCol.LightBlue);
                    break;
                case SecThick.HHidBoth:
                    // code block
                    LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, new Point3d(lefttop.X, lefttop.Y - thickness, 0), new Point3d(righttop.X, righttop.Y - thickness, 0), MyCol.LightBlue, "DASHED", 500);
                    LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, new Point3d(leftbot.X, leftbot.Y + thickness, 0), new Point3d(rightbot.X, rightbot.Y + thickness, 0), MyCol.LightBlue, "DASHED", 500);
                    break;
                case SecThick.Top:
                    // code block
                    LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, new Point3d(lefttop.X, lefttop.Y - thickness, 0), new Point3d(righttop.X, righttop.Y - thickness, 0), MyCol.LightBlue);
                    break;
                case SecThick.HidTop:
                    // code block
                    LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, new Point3d(lefttop.X, lefttop.Y - thickness, 0), new Point3d(righttop.X, righttop.Y - thickness, 0), MyCol.LightBlue, "DASHED", 500);
                    break;
                case SecThick.Bottom:
                    // code block
                    LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, new Point3d(leftbot.X, leftbot.Y + thickness, 0), new Point3d(rightbot.X, rightbot.Y + thickness, 0), MyCol.LightBlue);
                    break;
                case SecThick.HidBottom:
                    // code block
                    LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, new Point3d(leftbot.X, leftbot.Y + thickness, 0), new Point3d(rightbot.X, rightbot.Y + thickness, 0), MyCol.LightBlue, "DASHED", 500);
                    break;






            }

            //////create centerline
            var verticaldist = GetDist(lefttop, leftbot);
            var horizontaldist = GetDist(lefttop, righttop);

            if (Centerline.ToLower() != "na")
            {
                if (verticaldist > horizontaldist)
                {
                    LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, new Point3d((lefttop.X + righttop.X) / 2, lefttop.Y + 100, 0), new Point3d((lefttop.X + righttop.X) / 2, leftbot.Y - 100, 0), MyCol.Red, "DASHED", 500);
                }
                else if (horizontaldist > verticaldist)
                {
                    LineDraw(acadBlockTableRecord, AcadTransaction, acadDatabase, new Point3d(lefttop.X - 100, (lefttop.Y + leftbot.Y) / 2, 0), new Point3d(righttop.X + 100, (lefttop.Y + leftbot.Y) / 2, 0), MyCol.Red, "DASHED", 500);
                }
            }

        }

        //aligh dimensioning
        public void CreateAlighDimen(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Point3d startpt, Point3d endpt, [Optional] string dimtxt, double offsetdist = -2000)
        {

            GeometRi.Segment3d parallelline = CreateParallelLine(startpt, endpt, offsetdist);

            if ((startpt.X == 0 && startpt.Y == 0 && startpt.Z == 0) || (endpt.X == 0 && endpt.Y == 0 && endpt.Z == 0))
            {
                return;
            }

            //dimensioning
            AlignedDimension align = new AlignedDimension(startpt, endpt, new Point3d(parallelline.P2.X, parallelline.P2.Y, parallelline.P2.Z), "", ObjectId.Null);

            align.Dimtxt = 70;
            align.Dimasz = 70;

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
        public GeometRi.Segment3d CreateParallelLine(Point3d startpt, Point3d endpt, double offset)
        {

            var xDifference = startpt.X - endpt.X;
            var yDifference = startpt.Y - endpt.Y;
            var length = Math.Sqrt(Math.Pow(xDifference, 2) + Math.Pow(yDifference, 2));


            var X1 = (startpt.X - offset * yDifference / length);
            var X2 = (endpt.X - offset * yDifference / length);
            var Y1 = (startpt.Y + offset * xDifference / length);
            var Y2 = (endpt.Y + offset * xDifference / length);

            GeometRi.Segment3d parallelLine = new GeometRi.Segment3d(new GeometRi.Point3d(X1, Y1, 0), new GeometRi.Point3d(X2, Y2, 0));

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

        public double GetDistArray(double[] strpt, double[] endpt)
        {
            double dist = Math.Sqrt(Math.Pow(endpt[0] - strpt[0], 2) + Math.Pow(endpt[1] - strpt[1], 2) + Math.Pow(endpt[2] - strpt[2], 2));
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
        public void CreateSupportName(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, ref double tracex, double boxlen, double boxht, ref double spaceY, int i, [Optional] string supportype)
        {
            //support name and quantity
            double centerX = tracex + boxlen / 2;

            BoxGenCreateSecondarySupportBottom(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - 1500, spaceY - boxht + 700 + 1100, 0), new Point3d(centerX + 1500, spaceY - boxht + 700 + 1100, 0), new Point3d(centerX + 1500, spaceY - boxht + 700, 0), new Point3d(centerX - 1500, spaceY - boxht + 700, 0), SecThick.Nothing, Centerline: "na");
#if _RELEASE
            CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - 1500 + 1000, spaceY - boxht + 700 + 1100 - 100, 0), ListCentalSuppoData[i].Name, 350, MyCol.Red);
#endif
#if _DEBUG

            CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - 1500 + 1000, spaceY - boxht + 700 + 1100 - 100, 0), ListCentalSuppoData[i].Name + " " + supportype, 350, MyCol.Red);
#endif
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
            acMText.TextHeight = 70;
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
            acLdr.Dimasz = 70;


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

        public void DrawLeader_WITH_TXT_ABOVE_BELOW(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, string Abovetext, string Belowtext, Point3d firstPoint, Point3d secondPoint, Point3d? thirdPoint = null, double TextHeight = 70)
        {

            // Create the MText Above annotation
            MText acMText = new MText();
            acMText.SetDatabaseDefaults();
            acMText.Contents = Abovetext;
            acMText.Location = new Point3d(secondPoint.X, secondPoint.Y + TextHeight, 0);

            if (thirdPoint.HasValue)
            {
                acMText.Location = new Point3d(thirdPoint.Value.X, thirdPoint.Value.Y + TextHeight, 0);
            }
            acMText.TextHeight = TextHeight;
            acMText.Attachment = AttachmentPoint.BottomLeft;
            AcadBlockTableRecord.AppendEntity(acMText);
            AcadTransaction.AddNewlyCreatedDBObject(acMText, true);

            // Create the MText Below annotation
            MText acMText_below = new MText();
            acMText_below.SetDatabaseDefaults();
            acMText_below.Contents = Belowtext;
            acMText_below.Location = new Point3d(secondPoint.X, secondPoint.Y - TextHeight / 2, 0);

            if (thirdPoint.HasValue)
            {
                acMText_below.Location = new Point3d(thirdPoint.Value.X, thirdPoint.Value.Y - TextHeight / 2, 0);
            }
            acMText_below.TextHeight = TextHeight;
            acMText_below.Attachment = AttachmentPoint.TopCenter;
            AcadBlockTableRecord.AppendEntity(acMText_below);
            AcadTransaction.AddNewlyCreatedDBObject(acMText_below, true);

            Leader acLdr = new Leader();
            acLdr.SetDatabaseDefaults();
            acLdr.AppendVertex(firstPoint);
            acLdr.AppendVertex(secondPoint);
            if (thirdPoint.HasValue)
            {
                acLdr.AppendVertex(thirdPoint.Value);
            }
            acLdr.HasArrowHead = true;
            acLdr.Dimasz = TextHeight;


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
            //acLdr.Annotation = acMText_below.ObjectId;
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

        //foot of perpendicular
        public static Point3d FindPerpendicularFoot(double[] destpt, double[] strpt, double[] endpt)
        {

            Vector3 segmentStart = new Vector3(Convert.ToSingle(strpt[0]), Convert.ToSingle(strpt[1]), Convert.ToSingle(strpt[2]));

            Vector3 segmentEnd = new Vector3(Convert.ToSingle(endpt[0]), Convert.ToSingle(endpt[1]), Convert.ToSingle(endpt[2]));

            Vector3 point = new Vector3(Convert.ToSingle(destpt[0]), Convert.ToSingle(destpt[1]), Convert.ToSingle(destpt[2]));

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
            try
            {
                TotalHt = Math.Round(Convert.ToDouble(TotalHt), 2).ToString();

            }
            catch (Exception)
            {

            }

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

        public void GENTOSLOC(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, double centerX, double centerY, string TotalHt, string position = "r", string HTtype = "TOS EL.(+)", double Xoffset = 3500)
        {
            try
            {
                TotalHt = Math.Round(Convert.ToDouble(TotalHt), 2).ToString();

            }
            catch (Exception)
            {

            }

            if (position.ToLower() == "l")
            {
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - Xoffset, centerY + 300, 0), HTtype + TotalHt, 100);
                LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX, centerY, 0), new Point3d(centerX - Xoffset, centerY, 0), MyCol.LightBlue);
            }
            else
            {
                CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + Xoffset, centerY + 300, 0), HTtype + TotalHt, 100);
                LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX, centerY, 0), new Point3d(centerX + Xoffset, centerY, 0), MyCol.LightBlue);
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

        public static double FindMinValue(double[] numbers)
        {
            if (numbers == null || numbers.Length == 0)
            {
                throw new ArgumentException("The array is empty or null.");
            }

            double minValue = numbers[0];
            for (int i = 1; i < numbers.Length; i++)
            {
                if (numbers[i] < minValue)
                {
                    minValue = numbers[i];
                }
            }

            return minValue;
        }

        public static Point3d GetLeftPoint(Point3d midPoint, Point3d point1, Point3d point2)
        {
            Vector3d vectorToP1 = point1 - midPoint;
            Vector3d vectorToP2 = point2 - midPoint;

            Vector3d normal = vectorToP1.CrossProduct(vectorToP2);
            if (normal.Z < 0)
                return point1;
            else
                return point2;
        }

        public static Point3d GetRightPoint(Point3d midPoint, Point3d point1, Point3d point2)
        {
            Vector3d vectorToP1 = point1 - midPoint;
            Vector3d vectorToP2 = point2 - midPoint;

            Vector3d normal = vectorToP1.CrossProduct(vectorToP2);
            if (normal.Z < 0)
                return point2;
            else
                return point1;
        }

        public void CHECKPRIM_CLAMP_BRAC(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, double centerX, double centerY, double radius, SupporSpecData primsup_des, [Optional] int i)
        {
            if (primsup_des.SupportName.ToLower().Contains("clamp"))
            {
                CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Prim_block_U", centerX, centerY, radius);
            }
            else if (primsup_des.SupportName.ToLower().Contains("brac"))
            {
                CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Prim_block_Brac", centerX, centerY, radius);
            }
            else
            {
                CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Prim_NB_Gen", centerX, centerY, radius);
            }

        }

        public void GEN_CHECKPRIM_CLAMP_BRAC(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, double centerX, double centerY, double radius, SupporSpecData primsup_des, [Optional] int i)
        {
            if (primsup_des.SupportName.ToLower().Contains("clamp"))
            {
                CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Prim_block_U_Gen", centerX, centerY, radius);
            }
            else if (primsup_des.SupportName.ToLower().Contains("brac"))
            {
                CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Prim_block_Brac_Gen", centerX, centerY - radius, radius);
            }
            else
            {
                CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "Prim_NB_Gen", centerX, centerY, radius);
            }

        }

        public double GET_PROJECTEDPT_DIST(SupporSpecData secsupp_des, double[] primsup_des)
        {

            var strpt = secsupp_des.StPt;
            var endpt = secsupp_des.EndPt;

            Point3d projectedpt = FindPerpendicularFoot(primsup_des, strpt, endpt);

            double l_dist_frm_centre = GetDist(new Point3d(strpt), projectedpt);
            double r_dist_frm_centre = GetDist(new Point3d(endpt), projectedpt);
            double min = Math.Min(l_dist_frm_centre, r_dist_frm_centre);
            return min;
        }




        //public static double[] FindClosestPoint_TOLINE(double[] lineStart, double[] lineEnd, double[] point1, double[] point2)
        //{
        //    double lineLength = CalculateDistance(lineStart, lineEnd);

        //    if (lineLength == 0)
        //    {
        //        return point1; // or point2, since the line is a single point
        //    }

        //    double t1 = CalculateParameter(lineStart, lineEnd, point1);
        //    double t2 = CalculateParameter(lineStart, lineEnd, point2);

        //    if (t1 >= 0 && t1 <= 1 && t2 >= 0 && t2 <= 1)
        //    {
        //        // Both points are within the line segment, find the closest point on the line
        //        double[] closestPoint = new double[3];
        //        for (int i = 0; i < 3; i++)
        //        {
        //            closestPoint[i] = lineStart[i] + t1 * (lineEnd[i] - lineStart[i]);
        //        }
        //        return closestPoint;
        //    }

        //    // Either one or both points are outside the line segment, find the closest point among them
        //    double distance1 = CalculateDistance(lineStart, point1);
        //    double distance2 = CalculateDistance(lineStart, point2);

        //    return distance1 <= distance2 ? point1 : point2;
        //}

        //private static double CalculateDistance(double[] point1, double[] point2)
        //{
        //    double sum = 0;
        //    for (int i = 0; i < 3; i++)
        //    {
        //        double diff = point1[i] - point2[i];
        //        sum += diff * diff;
        //    }
        //    return Math.Sqrt(sum);
        //}

        //private static double CalculateParameter(double[] lineStart, double[] lineEnd, double[] point)
        //{
        //    double sum = 0;
        //    for (int i = 0; i < 3; i++)
        //    {
        //        sum += (point[i] - lineStart[i]) * (lineEnd[i] - lineStart[i]);
        //    }
        //    double lineLengthSquared = CalculateDistance(lineStart, lineEnd) * CalculateDistance(lineStart, lineEnd);
        //    return sum / lineLengthSquared;
        //}

        //new logic for dist



        public static double[] FindClosestPoint_TOLINE(double[] pointA, double[] pointB, double[] pointX, double[] pointY)
        {
            double distanceX = CalculateDistanceToLine(pointA, pointB, pointX);
            double distanceY = CalculateDistanceToLine(pointA, pointB, pointY);

            if (distanceX < distanceY)
            {
                return pointX;
            }
            else if (distanceX > distanceY)
            {
                return pointY;
            }
            else
            {
                return null;
            }
        }

        private static double CalculateDistanceToLine(double[] lineStart, double[] lineEnd, double[] point)
        {
            double lineLength = CalculateDistance(lineStart, lineEnd);

            if (lineLength == 0)
            {
                return CalculateDistance(lineStart, point);
            }

            double t = ((point[0] - lineStart[0]) * (lineEnd[0] - lineStart[0]) +
                        (point[1] - lineStart[1]) * (lineEnd[1] - lineStart[1]) +
                        (point[2] - lineStart[2]) * (lineEnd[2] - lineStart[2])) / (lineLength * lineLength);

            if (t < 0)
            {
                return CalculateDistance(lineStart, point);
            }

            if (t > 1)
            {
                return CalculateDistance(lineEnd, point);
            }

            double[] projection = new double[3];
            for (int i = 0; i < 3; i++)
            {
                projection[i] = lineStart[i] + t * (lineEnd[i] - lineStart[i]);
            }

            return CalculateDistance(projection, point);
        }

        private static double CalculateDistance(double[] point1, double[] point2)
        {
            double sum = 0;
            for (int i = 0; i < 3; i++)
            {
                double diff = point1[i] - point2[i];
                sum += diff * diff;
            }
            return Math.Sqrt(sum);
        }

        //for calculating no of distances for primary
        public static List<double> CalculateAccumulation(List<double> inputList)
        {
            List<double> finalOutput = new List<double>();
            finalOutput.Clear();
            if (inputList.Count > 0)
            {
                finalOutput.Add(inputList[0]); // First number remains unchanged

                for (int k = 1; k < inputList.Count; k++)
                {
                    double subtractedValue = inputList[k] - inputList[k - 1];
                    finalOutput.Add(subtractedValue);
                }
            }
            return finalOutput;
        }

        public static List<double> PlaceFiguresOnSupport(double supportLength, int numberOfFigures, double margin)
        {
            //if (supportLength <= 0 || numberOfFigures < 2 || margin <= 0)
            //{
            //    return null;
            //}

            double spacing = 0.0;
            if (supportLength <= 0 || numberOfFigures < 2 || margin <= 0)
            {
                spacing = 1000;
            }
            else
            {
                spacing = (supportLength - 2 * margin) / (numberOfFigures - 1);
            }

            List<double> figurePositions = new List<double>();

            for (int i = 0; i < numberOfFigures; i++)
            {
                double position = margin + (spacing * i);
                figurePositions.Add(position);
            }

            return figurePositions;
        }

        private Point3d GetMidPoint(Point3d point3d1, Point3d point3d2)
        {
            return new Point3d((point3d1.X + point3d2.X) / 2, (point3d1.Y + point3d2.Y) / 2, (point3d1.Z + point3d2.Z) / 2);
        }

        public string GET_PRIM_HT_FRM_DATUM(SupporSpecData primsup_des)
        {
            string prim_height = "";
            try
            {
                if (primsup_des.SupportName.ToLower().Contains("clamp") || primsup_des.SupportName.ToLower().Contains("brac"))
                {
                    prim_height = Math.Round((MillimetersToMeters(primsup_des.Centroid.Z)), 2).ToString();
                }
                else
                {
                    prim_height = Math.Round((MillimetersToMeters(primsup_des.PrimaryZhtNB)), 2).ToString();
                }

            }
            catch (Exception)
            {

            }
            return prim_height;
        }

        public static double[] ProjectSegmentTo2D(double[] startPoint, double[] endPoint)
        {
            // Step 3: Calculate direction vector
            double[] directionVector = new double[3];
            for (int i = 0; i < 3; i++)
            {
                directionVector[i] = endPoint[i] - startPoint[i];
            }

            // Step 4: Normalize direction vector
            double length = Math.Sqrt(directionVector[0] * directionVector[0] +
                                      directionVector[1] * directionVector[1] +
                                      directionVector[2] * directionVector[2]);
            for (int i = 0; i < 3; i++)
            {
                directionVector[i] /= length;
            }

            // Step 6: Calculate dot product of direction vector and normal vector
            double dotProduct = directionVector[0] * 0 + directionVector[1] * 0 + directionVector[2] * 1;

            // Step 7: Calculate projection vector
            double[] projectionVector = new double[3];
            for (int i = 0; i < 3; i++)
            {
                projectionVector[i] = dotProduct * 0 + dotProduct * 0 + dotProduct * 1;
            }

            // Step 8: Convert 3D points to 2D points
            double[] projectedStartPoint = { startPoint[0], startPoint[1] };
            double[] projectedEndPoint = { endPoint[0], endPoint[1] };

            return projectedStartPoint.Concat(projectedEndPoint).ToArray();
        }

        public List<CustomPlane> GetPipeAxis(Editor AcadEditor, Transaction AcadTransaction)
        {
            var SelectionRes = AcadEditor.SelectAll();

            List<CustomPlane> PipeData = new List<CustomPlane>();
            //Getting Object ID of the each selected entiry
            var EntsIds = new ObjectIdCollection(SelectionRes.Value.GetObjectIds());

            foreach (ObjectId Id in EntsIds)
            {
                var AcEnt = (Entity)AcadTransaction.GetObject(Id, OpenMode.ForRead);
                if (AcEnt.GetType() == typeof(Autodesk.ProcessPower.PnP3dObjects.Pipe))
                {
                    CustomPlane Pipeinfo = new CustomPlane();
                    Autodesk.ProcessPower.PnP3dObjects.Pipe AcadPipe = AcEnt as Autodesk.ProcessPower.PnP3dObjects.Pipe;

                    Pipeinfo.StptPipe = AcadPipe.StartPoint;
                    Pipeinfo.EndptPipe = AcadPipe.EndPoint;
                    Pipeinfo.NormalPipe = AcadPipe.EndPoint - AcadPipe.StartPoint;
                    PipeData.Add(Pipeinfo);

                }
            }
            return PipeData;
        }

        public CustomPlane GetPlaneData(List<CustomPlane> PipeData, Pt3D Centroid)
        {
            int count = 0;

            CustomPlane Plane = new CustomPlane();

            double dist = 0;

            foreach (var pida in PipeData)
            {
                var PtONProj = FindPerpendicularFoot(new double[] { Centroid.X, Centroid.Y, Centroid.Z }, new double[] { pida.StptPipe.X, pida.StptPipe.Y, pida.StptPipe.Z }, new double[] { pida.EndptPipe.X, pida.EndptPipe.Y, pida.EndptPipe.Z });

                var DistBetw = GetDist(PtONProj, GetPoint3dFromPt3D(Centroid));

                if (count == 0)
                {
                    dist = DistBetw;
                    pida.PointOnPlane = GetPt3DFromPoint3d(PtONProj);
                    Vector3D Vec3D = new Vector3D(pida.NormalPipe.X, pida.NormalPipe.Y, pida.NormalPipe.Z);
                    Vec3D.Normalize();
                    pida.Normal = GetPt3DFromVecData3D(Vec3D);
                    Plane = pida;
                }

                if (DistBetw < dist)
                {
                    dist = DistBetw;
                    pida.PointOnPlane = GetPt3DFromPoint3d(PtONProj);
                    Vector3D Vec3D = new Vector3D(pida.NormalPipe.X, pida.NormalPipe.Y, pida.NormalPipe.Z);
                    Vec3D.Normalize();
                    pida.Normal = GetPt3DFromVecData3D(Vec3D);
                    Plane = pida;
                }

                count++;
            }
            return Plane;
        }

        //support details



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

            var primsup_des = ListCentalSuppoData[i].ListPrimarySuppo.OrderByDescending(e => e.Centroid.Z);



            //////////prim ht
            string prim_height = "";
            try
            {
                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0)
                {
                    prim_height = GET_PRIM_HT_FRM_DATUM(ListCentalSuppoData[i].ListPrimarySuppo[0]);
                }
            }
            catch (Exception)
            {

            }

            try
            {
                if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb"))
                {
                    FixCreatePrimarySupportwithvertex(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY, 801.5625);
                    //height of prim from bottom

                    CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] + 1500, centerY - 100, 0), "CL.EL.(+)" + prim_height);

                }

                else if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("clamp") || ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("brac"))
                {
                    double radius = 500;
                    info[Defination.Prim_Radius] = radius;
                    info[Defination.Prim_ht] = radius;

                    CHECKPRIM_CLAMP_BRAC(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY, radius, primsup_des.First());                    //height of prim from bottom
                    // CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] - 1500, centerY - 100, 0), "CL.EL.(+)" + info[Defination.Prim_ht].ToString());

                    CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - info[Defination.Prim_Radius] - 2500, centerY - 100, 0), "CL.EL.(+)" + prim_height);
                    LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius], centerY, 0), new Point3d(centerX + info[Defination.Prim_Radius] - 3000, centerY, 0), MyCol.LightBlue);
                    string clampname = ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName;
                    CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX - info[Defination.Prim_Radius] - 2500, centerY + 300, 0), clampname);

                }
                else if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.Length > 0)
                {
                    FixCreatePrimarySupportwithvertex(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY, 801.5625);
                    //height of prim from bottom

                    CreateMtextfunc(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(centerX + info[Defination.Prim_Radius] + 1500, centerY - 100, 0), "CL.EL.(+)" + prim_height);

                }
            }
            catch (Exception)
            {

            }



            ///////////////////ref block
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
            CreateSupportName(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, i, "13");


            tracex += boxlen; Created_TAG.Add(ListCentalSuppoData[i].Name); ////

        }


        public void ProjectTo2D(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, ref double tracex, double boxlen, double boxht, ref double spaceY, Document Document2D, string SupType, [Optional] int i, bool ChecfFor3rdDim)
        {



            List<BoundingBox> DimesionBoxData = new List<BoundingBox>();
            List<EntityData> LinesData = new List<EntityData>();
            Dictionary<string, Projection2DData> ProjectedSecEntity = new Dictionary<string, Projection2DData>();

            int edgecount = 1;
            int facecount = 1;


            if (!ChecfFor3rdDim)
            {
                //box lines
                LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex + boxlen, spaceY + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);

                LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex, spaceY - boxht + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);
                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0)
                {


                    var primorder = ListCentalSuppoData[i].ListPrimarySuppo.OrderByDescending(e => e.Centroid.Z);

                    if (primorder.First().ProjectionPlane.Normal == null)
                    {
                        SupportNotCreated.Add(ListCentalSuppoData[i].Name);
                    }
                    else
                    {
                        var normvec = GetVect3DFromPt3D(primorder.First().ProjectionPlane.Normal);
                        if (!(Math.Abs(normvec.Z).Equals(1)))
                        {
                            if (primorder.First().ProjectionPlane != null && primorder.First().ProjectionPlane.Normal != null)
                            {

                                //for Dim boundingBox
                                List<List<GeometRi.Segment3d>> AllParts = new List<List<GeometRi.Segment3d>>();

                                //for Dim boundingBox
                                List<GeometRi.Segment3d> AllSeg = new List<GeometRi.Segment3d>();


                                foreach (SupporSpecData SecData in ListCentalSuppoData[i].ListPrimarySuppo)
                                {
                                    List<FaceData> FacestoProj = new List<FaceData>();
                                    Projection2DData PData = new Projection2DData();

                                    foreach (BoundingBoxFace FaceLoc in SecData.PrimBoundingBoxData)
                                    {
                                        if (Math.Abs(Math.Round(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(FaceLoc.Normal, GetVect3DFromPt3D(primorder.First().ProjectionPlane.Normal), new Vector3D(0, 0, 1))))) >= 0 && Math.Abs(Math.Round(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(FaceLoc.Normal, GetVect3DFromPt3D(primorder.First().ProjectionPlane.Normal), new Vector3D(0, 0, 1))))) < 10)
                                        {

                                            FaceData primface = new FaceData();
                                            primface.FaceNormal = GetVect3dFromVect3D(FaceLoc.Normal);
                                            primface.PtonPlane = GetPoint3dFromPoint3D(FaceLoc.FacePoint);

                                            foreach (BoundingBoxEdge edge in FaceLoc.Edges)
                                            {
                                                Edgeinfo edgeprim = new Edgeinfo();
                                                edgeprim.StPt = GetPt3DFromPoint3D(edge.StartPoint);
                                                edgeprim.EndPt = GetPt3DFromPoint3D(edge.EndPoint);

                                                primface.ListAllEdges.Add(edgeprim);

                                            }


                                            FacestoProj.Add(primface);

                                        }
                                    }
                                    try
                                    {
                                        List<double[]> AllPoints = new List<double[]>();//= new double[][];
                                        for (int j = 0; j < FacestoProj.Count; j++)
                                        {
                                            FaceData faceData = new FaceData();

                                            List<Edgeinfo> Edgelist = new List<Edgeinfo>();

                                            for (int k = 0; k < FacestoProj[j].ListAllEdges.Count; k++)
                                            {


                                                Edgeinfo EdgeData = new Edgeinfo();


                                                //for calculating dist of edge
                                                EdgeData = FacestoProj[j].ListAllEdges[k];
                                                EdgeData.EdgeId = SecData.SuppoId + "," + "Edge" + edgecount.ToString();

                                                Edgelist.Add(EdgeData);

                                                GeometRi.Segment3d GeomSegref = new GeometRi.Segment3d(GetGeometRiPt3DFromPt3D(FacestoProj[j].ListAllEdges[k].StPt), GetGeometRiPt3DFromPt3D(FacestoProj[j].ListAllEdges[k].EndPt));


                                                GeometRi.Plane3d GeomPlaneRef = new GeometRi.Plane3d(GetGeometRiPt3DFromPt3D(ListCentalSuppoData[i].ListPrimarySuppo[0].ProjectionPlane.PointOnPlane), GetGeometRiVect3dFromPt3D(ListCentalSuppoData[i].ListPrimarySuppo[0].ProjectionPlane.Normal));

                                                GeometRi.Segment3d obj2 = GeomSegref.ProjectionTo(GeomPlaneRef) as GeometRi.Segment3d;


                                                if (obj2 != null)

                                                {
                                                    AllPoints.Add(GetArrayOfDoubleFromGeometRiPoint3D(obj2.P1));
                                                    AllPoints.Add(GetArrayOfDoubleFromGeometRiPoint3D(obj2.P2));
                                                }
                                                else
                                                {
                                                    GeometRi.Point3d p1 = GeomSegref.P1;
                                                    GeometRi.Point3d p2 = GeomSegref.P2;
                                                    p1 = p1.ProjectionTo(GeomPlaneRef);
                                                    p2 = p2.ProjectionTo(GeomPlaneRef);

                                                    obj2 = new GeometRi.Segment3d(p1, p2);
                                                    AllPoints.Add(GetArrayOfDoubleFromGeometRiPoint3D(obj2.P1));
                                                    AllPoints.Add(GetArrayOfDoubleFromGeometRiPoint3D(obj2.P2));
                                                }

                                                Tuple<GeometRi.Point3d, GeometRi.Point3d> result = Calculate.ProjectLineOntoXYPlane(obj2.P1, obj2.P2, GetVect3DFromPt3D((ListCentalSuppoData[i].ListPrimarySuppo[0].ProjectionPlane.Normal)));

                                                GeometRi.Segment3d resultLine = new GeometRi.Segment3d(result.Item1, result.Item2);

                                                EntityData entd = new EntityData();
                                                entd.Segment = resultLine;
                                                entd.IDSegment = SecData.SuppoId + "," + "Edge" + edgecount.ToString();
                                                LinesData.Add(entd);

                                                //for Dim boundingBox
                                                AllSeg.Add(resultLine);

                                                edgecount++;
                                            }
                                            //for Dim boundingBox
                                            AllParts.Add(AllSeg);

                                            faceData.ListAllEdges = Edgelist;
                                            faceData.ListPrimEdges = Edgelist;
                                            faceData.FaceIdLocal = "Face" + facecount.ToString();
                                            PData.ListfaceData.Add(faceData);

                                        }



                                    }
                                    catch (Exception e)
                                    {
                                    }


                                }
                                foreach (SupporSpecData SecData in ListCentalSuppoData[i].ListSecondrySuppo)
                                {
                                    List<FaceData> FacestoProj = new List<FaceData>();
                                    Projection2DData PData = new Projection2DData();

                                    foreach (FaceData FaceLoc in SecData.ListfaceData)
                                    {


                                        if (Math.Abs(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(GetVect3dFromVect3D(FaceLoc.FaceNormal), GetVect3DFromPt3D(primorder.First().ProjectionPlane.Normal), new Vector3D(0, 0, 1)))) >= 0 && Math.Abs(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(GetVect3dFromVect3D(FaceLoc.FaceNormal), GetVect3DFromPt3D(primorder.First().ProjectionPlane.Normal), new Vector3D(0, 0, 1)))) < 89 || HasOpenCircularEdge(FaceLoc))
                                        {
                                            FacestoProj.Add(FaceLoc);

                                        }
                                    }
                                    try
                                    {
                                        List<double[]> AllPoints = new List<double[]>();//= new double[][];


                                        for (int j = 0; j < FacestoProj.Count; j++)
                                        {
                                            FaceData faceData = new FaceData();

                                            List<Edgeinfo> Edgelist = new List<Edgeinfo>();

                                            for (int k = 0; k < FacestoProj[j].ListAllEdges.Count; k++)
                                            {
                                                Edgeinfo EdgeData = new Edgeinfo();


                                                //for calculating dist of edge
                                                EdgeData = FacestoProj[j].ListAllEdges[k];
                                                EdgeData.EdgeId = SecData.SuppoId + "," + "Edge" + edgecount.ToString();

                                                Edgelist.Add(EdgeData);

                                                GeometRi.Segment3d GeomSegref = new GeometRi.Segment3d(GetGeometRiPt3DFromPt3D(FacestoProj[j].ListAllEdges[k].StPt), GetGeometRiPt3DFromPt3D(FacestoProj[j].ListAllEdges[k].EndPt));


                                                GeometRi.Plane3d GeomPlaneRef = new GeometRi.Plane3d(GetGeometRiPt3DFromPt3D(primorder.First().ProjectionPlane.PointOnPlane), GetGeometRiVect3dFromPt3D(primorder.First().ProjectionPlane.Normal));

                                                GeometRi.Segment3d obj2 = GeomSegref.ProjectionTo(GeomPlaneRef) as GeometRi.Segment3d;


                                                if (obj2 != null)

                                                {
                                                    AllPoints.Add(GetArrayOfDoubleFromGeometRiPoint3D(obj2.P1));
                                                    AllPoints.Add(GetArrayOfDoubleFromGeometRiPoint3D(obj2.P2));
                                                }
                                                else
                                                {
                                                    GeometRi.Point3d p1 = GeomSegref.P1;
                                                    GeometRi.Point3d p2 = GeomSegref.P2;
                                                    p1 = p1.ProjectionTo(GeomPlaneRef);
                                                    p2 = p2.ProjectionTo(GeomPlaneRef);

                                                    obj2 = new GeometRi.Segment3d(p1, p2);
                                                    AllPoints.Add(GetArrayOfDoubleFromGeometRiPoint3D(obj2.P1));
                                                    AllPoints.Add(GetArrayOfDoubleFromGeometRiPoint3D(obj2.P2));
                                                }

                                                Tuple<GeometRi.Point3d, GeometRi.Point3d> result = Calculate.ProjectLineOntoXYPlane(obj2.P1, obj2.P2, GetVect3DFromPt3D((primorder.First().ProjectionPlane.Normal)));

                                                GeometRi.Segment3d resultLine = new GeometRi.Segment3d(result.Item1, result.Item2);

                                                EntityData entd = new EntityData();
                                                entd.Segment = resultLine;
                                                entd.IDSegment = SecData.SuppoId + "," + "Edge" + edgecount.ToString();
                                                LinesData.Add(entd);

                                                //for Dim boundingBox
                                                AllSeg.Add(resultLine);

                                                edgecount++;
                                            }
                                            //for Dim boundingBox
                                            AllParts.Add(AllSeg);

                                            faceData.ListAllEdges = Edgelist;
                                            faceData.FaceIdLocal = "Face" + facecount.ToString();
                                            PData.ListfaceData.Add(faceData);

                                        }



                                    }
                                    catch (Exception e)
                                    {
                                    }


                                }
                                foreach (SupporSpecData SecData in ListCentalSuppoData[i].ListConcreteData)
                                {
                                    List<FaceData> FacestoProj = new List<FaceData>();
                                    Projection2DData PData = new Projection2DData();

                                    foreach (FaceData FaceLoc in SecData.ListfaceData)
                                    {


                                        if (Math.Abs(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(GetVect3dFromVect3D(FaceLoc.FaceNormal), GetVect3DFromPt3D(primorder.First().ProjectionPlane.Normal), new Vector3D(0, 0, 1)))) >= 0 && Math.Abs(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(GetVect3dFromVect3D(FaceLoc.FaceNormal), GetVect3DFromPt3D(primorder.First().ProjectionPlane.Normal), new Vector3D(0, 0, 1)))) < 89)
                                        {
                                            FacestoProj.Add(FaceLoc);

                                            //primorder.First().ProjectionPlane.Normal

                                        }
                                    }
                                    try
                                    {
                                        List<double[]> AllPoints = new List<double[]>();//= new double[][];


                                        for (int j = 0; j < FacestoProj.Count; j++)
                                        {
                                            FaceData faceData = new FaceData();

                                            List<Edgeinfo> Edgelist = new List<Edgeinfo>();
                                            for (int k = 0; k < FacestoProj[j].ListAllEdges.Count; k++)
                                            {
                                                Edgeinfo EdgeData = new Edgeinfo();


                                                //for calculating dist of edge
                                                EdgeData = FacestoProj[j].ListAllEdges[k];
                                                EdgeData.EdgeId = "Edge" + edgecount.ToString();

                                                Edgelist.Add(EdgeData);

                                                GeometRi.Segment3d GeomSegref = new GeometRi.Segment3d(GetGeometRiPt3DFromPt3D(FacestoProj[j].ListAllEdges[k].StPt), GetGeometRiPt3DFromPt3D(FacestoProj[j].ListAllEdges[k].EndPt));

                                                GeometRi.Plane3d GeomPlaneRef = new GeometRi.Plane3d(GetGeometRiPt3DFromPt3D(primorder.First().ProjectionPlane.PointOnPlane), GetGeometRiVect3dFromPt3D(primorder.First().ProjectionPlane.Normal));



                                                GeometRi.Segment3d obj2 = GeomSegref.ProjectionTo(GeomPlaneRef) as GeometRi.Segment3d;


                                                if (obj2 != null)

                                                {
                                                    AllPoints.Add(GetArrayOfDoubleFromGeometRiPoint3D(obj2.P1));
                                                    AllPoints.Add(GetArrayOfDoubleFromGeometRiPoint3D(obj2.P2));
                                                }
                                                else
                                                {
                                                    GeometRi.Point3d p1 = GeomSegref.P1;
                                                    GeometRi.Point3d p2 = GeomSegref.P2;
                                                    p1 = p1.ProjectionTo(GeomPlaneRef);
                                                    p2 = p2.ProjectionTo(GeomPlaneRef);

                                                    obj2 = new GeometRi.Segment3d(p1, p2);
                                                    AllPoints.Add(GetArrayOfDoubleFromGeometRiPoint3D(obj2.P1));
                                                    AllPoints.Add(GetArrayOfDoubleFromGeometRiPoint3D(obj2.P2));
                                                }

                                                Tuple<GeometRi.Point3d, GeometRi.Point3d> result = Calculate.ProjectLineOntoXYPlane(obj2.P1, obj2.P2, GetVect3DFromPt3D((primorder.First().ProjectionPlane.Normal)));

                                                GeometRi.Segment3d resultLine = new GeometRi.Segment3d(result.Item1, result.Item2);




                                                //LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(result.Item1.X, result.Item1.Y, result.Item1.Z), new Point3d(result.Item2.X, result.Item2.Y, result.Item2.Z), MyCol.White);


                                                EntityData entd = new EntityData();
                                                entd.Segment = resultLine;
                                                entd.IDSegment = SecData.SuppoId + "," + "Edge" + edgecount.ToString();
                                                LinesData.Add(entd);

                                                //for Dim boundingBox
                                                AllSeg.Add(resultLine);

                                                edgecount++;

                                            }
                                            //for Dim boundingBox
                                            AllParts.Add(AllSeg);

                                            faceData.ListAllEdges = Edgelist;
                                            faceData.FaceIdLocal = "Face" + facecount.ToString();
                                            PData.ListfaceData.Add(faceData);

                                        }
                                    }
                                    catch (Exception e)
                                    {
                                    }
                                }
                                var AllLiness = Calculate.FitLinesInRectangle(LinesData, 11000, 7000, tracex + boxlen / 2, spaceY - boxht / 2);

                                List<GeometRi.Segment3d> allsecseg = new List<GeometRi.Segment3d>();
                                foreach (var seg in AllLiness)
                                {
                                    if (seg.IDSegment.ToLower().Contains("s"))
                                    {
                                        allsecseg.Add(seg.Segment);
                                    }
                                }

                                double minX = double.MaxValue;
                                double minY = double.MaxValue;
                                double maxX = double.MinValue;
                                double maxY = double.MinValue;

                                var maxminpts = Calculate.FindMinMaxCoordinates(allsecseg, out minX, out minY, out maxX, out maxY);


                                Dictionary<string, List<GeometRi.Segment3d>> DicIdEdgeList = new Dictionary<string, List<GeometRi.Segment3d>>();

                                foreach (var ent in AllLiness)
                                {
                                    if (ent.IDSegment.Substring(0, 1).ToLower().Equals("s"))
                                    {
                                        List<GeometRi.Segment3d> EdgeList = new List<GeometRi.Segment3d>();

                                        if (DicIdEdgeList.ContainsKey(ent.IDSegment.Split(',')[0]))
                                        {
                                            EdgeList = DicIdEdgeList[ent.IDSegment.Split(',')[0]];
                                            EdgeList.Add(ent.Segment);

                                            DicIdEdgeList[ent.IDSegment.Split(',')[0]] = EdgeList;

                                        }
                                        else
                                        {
                                            EdgeList.Add(ent.Segment);

                                            DicIdEdgeList[ent.IDSegment.Split(',')[0]] = EdgeList;
                                        }
                                    }
                                }

                                List<GeometRi.Segment3d> ListSegs = new List<GeometRi.Segment3d>();

                                foreach (var SecSupo in DicIdEdgeList)
                                {
                                    if (SecSupo.Value.Count < 15)
                                    {
                                        foreach (var line in SecSupo.Value)
                                        {
                                            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(line.P1.X, line.P1.Y, line.P1.Z), new Point3d(line.P2.X, line.P2.Y, line.P2.Z), MyCol.LightBlue);
                                        }
                                    }
                                    else
                                    {

                                        if (Math.Abs(Math.Round(GetRotationFromVec(GetPartbyId(SecSupo.Key, ListCentalSuppoData[i].ListSecondrySuppo).Directionvec).XinDegree)) > 0 && Math.Abs(Math.Round(GetRotationFromVec(GetPartbyId(SecSupo.Key, ListCentalSuppoData[i].ListSecondrySuppo).Directionvec).XinDegree)) % 90 != 0)
                                        {
                                            foreach (var line in SecSupo.Value)
                                            {
                                                LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(line.P1.X, line.P1.Y, line.P1.Z), new Point3d(line.P2.X, line.P2.Y, line.P2.Z), MyCol.LightBlue);
                                            }
                                        }
                                        else
                                        {
                                            List<GeometRi.Segment3d> ListSegOutSide = new List<GeometRi.Segment3d>();
                                            List<GeometRi.Segment3d> ListSegInSide = new List<GeometRi.Segment3d>();
                                            (ListSegInSide, ListSegOutSide) = Calculate.CalculateEdges(SecSupo.Value);
                                            ListSegs.AddRange(ListSegInSide);
                                            ListSegs.AddRange(ListSegOutSide);
                                        }
                                    }
                                }


                                foreach (var ent in AllLiness)
                                {
                                    var line = ent.Segment;
                                    if (ent.IDSegment.ToLower().Contains("c"))
                                    {
                                        LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(line.P1.X, line.P1.Y, line.P1.Z), new Point3d(line.P2.X, line.P2.Y, line.P2.Z), MyCol.Gray);
                                    }
                                    else if (ent.IDSegment.ToLower().Contains("s"))
                                    {
                                        // LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(line.P1.X, line.P1.Y, line.P1.Z), new Point3d(line.P2.X, line.P2.Y, line.P2.Z), MyCol.LightBlue);
                                    }
                                    else
                                    {
                                        LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(line.P1.X, line.P1.Y, line.P1.Z), new Point3d(line.P2.X, line.P2.Y, line.P2.Z), MyCol.Green);
                                    }

                                    // PData.AfterListfaceData.Add();
                                }


                                foreach (var line in ListSegs)
                                {
                                    LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(line.P1.X, line.P1.Y, line.P1.Z), new Point3d(line.P2.X, line.P2.Y, line.P2.Z), MyCol.LightBlue);
                                }

                                Dictionary<string, List<EntityData>> PrimPart2D = new Dictionary<string, List<EntityData>>();
                                for (int h = 0; h < ListCentalSuppoData[i].ListPrimarySuppo.Count; h++)
                                {
                                    List<EntityData> PrimParts = new List<EntityData>();
                                    for (int g = 0; g < AllLiness.Count; g++)
                                    {
                                        if (AllLiness[g].IDSegment.Contains(ListCentalSuppoData[i].ListPrimarySuppo[h].SuppoId))
                                        {
                                            PrimParts.Add(AllLiness[g]);
                                        }
                                    }
                                    PrimPart2D[ListCentalSuppoData[i].ListPrimarySuppo[h].SuppoId] = PrimParts;
                                }


                                Dictionary<string, List<EntityData>> SecPart2D = new Dictionary<string, List<EntityData>>();
                                for (int h = 0; h < ListCentalSuppoData[i].ListSecondrySuppo.Count; h++)
                                {
                                    List<EntityData> SecParts = new List<EntityData>();
                                    for (int g = 0; g < AllLiness.Count; g++)
                                    {
                                        if (AllLiness[g].IDSegment.Contains(ListCentalSuppoData[i].ListSecondrySuppo[h].SuppoId))
                                        {
                                            SecParts.Add(AllLiness[g]);
                                        }
                                    }
                                    SecPart2D[ListCentalSuppoData[i].ListSecondrySuppo[h].SuppoId] = SecParts;
                                }



                                Dictionary<string, List<EntityData>> ConcretePart2D = new Dictionary<string, List<EntityData>>();
                                for (int h = 0; h < ListCentalSuppoData[i].ListConcreteData.Count; h++)
                                {
                                    List<EntityData> ConcreteParts = new List<EntityData>();
                                    for (int g = 0; g < AllLiness.Count; g++)
                                    {
                                        if (AllLiness[g].IDSegment.Contains(ListCentalSuppoData[i].ListConcreteData[h].SuppoId))
                                        {
                                            ConcreteParts.Add(AllLiness[g]);

                                        }
                                    }
                                    ConcretePart2D[ListCentalSuppoData[i].ListConcreteData[h].SuppoId] = ConcreteParts;
                                }



                                var concrete_sort = ListCentalSuppoData[i].ListConcreteData.OrderBy(e => e.Boundingboxmin.Z);

                                if (concrete_sort.Count() > 0)
                                {
                                    ConcretDimension(AcadBlockTableRecord, AcadTransaction, AcadDatabase, concrete_sort.ElementAt(0), ConcretePart2D, i);
                                    FJWELD(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ConcretePart2D, i);
                                }

                                List<SupporSpecData> SecFilter = new List<SupporSpecData>();
                                foreach (SupporSpecData sec in ListCentalSuppoData[i].ListSecondrySuppo)
                                {
                                    if (sec.IsGussetplate == false && sec.IsAnchor == false)
                                    {
                                        SecFilter.Add(sec);
                                    }
                                }

                                //Secondary Dimensioning
                                var secondary_sort = SecFilter.OrderByDescending(e => e.Boundingboxmin.Z);
                                if (secondary_sort.Count() > 0)
                                {
                                    SecondaryDimension(AcadBlockTableRecord, AcadTransaction, AcadDatabase, secondary_sort, SecPart2D, AllParts, minX, maxX, i, PrimPart2D);
                                }

                                //for collecting prim faces edges
                                Dictionary<string, List<GeometRi.Segment3d>> PrimFaceEdges = new Dictionary<string, List<GeometRi.Segment3d>>();

                                foreach (SupporSpecData prim in primorder)
                                {
                                    List<GeometRi.Segment3d> primbyID = new List<GeometRi.Segment3d>();
                                    foreach (var priment in AllLiness)
                                    {
                                        if (priment.IDSegment.Contains(prim.SuppoId))
                                        {
                                            primbyID.Add(priment.Segment);
                                        }
                                    }
                                    PrimFaceEdges.Add(prim.SuppoId, primbyID);
                                }
                                ///////////////
                                PlacePrim(AcadBlockTableRecord, AcadTransaction, AcadDatabase, PrimFaceEdges, secondary_sort, SecPart2D, i);


                                //support name and quantity
                                CreateSupportName(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, i);

                                tracex += boxlen; Created_TAG.Add(ListCentalSuppoData[i].Name);
                            }
                        }
                        else
                        {
                            SupportNotCreated.Add(ListCentalSuppoData[i].Name);
                        }
                    }


                }
                else if (ListCentalSuppoData[i].ListSecondrySuppo.Count > 0)
                {

                    var primorder = ListCentalSuppoData[i].ListSecondrySuppo.OrderByDescending(e => e.Centroid.Z);
                    if (primorder.First().ProjectionPlane.Normal == null)
                    {
                        SupportNotCreated.Add(ListCentalSuppoData[i].Name);
                    }
                    else
                    {
                        var normvec = GetVect3DFromPt3D(primorder.First().ProjectionPlane.Normal);
                        if (!(Math.Abs(normvec.Z).Equals(1)))
                        {
                            if (primorder.First().ProjectionPlane != null && primorder.First().ProjectionPlane.Normal != null)
                            {
                                //for Dim boundingBox
                                List<List<GeometRi.Segment3d>> AllParts = new List<List<GeometRi.Segment3d>>();

                                //for Dim boundingBox
                                List<GeometRi.Segment3d> AllSeg = new List<GeometRi.Segment3d>();
                                foreach (SupporSpecData SecData in ListCentalSuppoData[i].ListSecondrySuppo)
                                {
                                    List<FaceData> FacestoProj = new List<FaceData>();
                                    Projection2DData PData = new Projection2DData();

                                    foreach (FaceData FaceLoc in SecData.ListfaceData)
                                    {


                                        if (Math.Abs(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(GetVect3dFromVect3D(FaceLoc.FaceNormal), GetVect3DFromPt3D(primorder.First().ProjectionPlane.Normal), new Vector3D(0, 0, 1)))) >= 0 && Math.Abs(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(GetVect3dFromVect3D(FaceLoc.FaceNormal), GetVect3DFromPt3D(primorder.First().ProjectionPlane.Normal), new Vector3D(0, 0, 1)))) < 89 || HasOpenCircularEdge(FaceLoc))
                                        {
                                            FacestoProj.Add(FaceLoc);

                                        }
                                    }
                                    try
                                    {
                                        List<double[]> AllPoints = new List<double[]>();//= new double[][];


                                        for (int j = 0; j < FacestoProj.Count; j++)
                                        {
                                            FaceData faceData = new FaceData();

                                            List<Edgeinfo> Edgelist = new List<Edgeinfo>();

                                            for (int k = 0; k < FacestoProj[j].ListAllEdges.Count; k++)
                                            {
                                                Edgeinfo EdgeData = new Edgeinfo();


                                                //for calculating dist of edge
                                                EdgeData = FacestoProj[j].ListAllEdges[k];
                                                EdgeData.EdgeId = SecData.SuppoId + "," + "Edge" + edgecount.ToString();

                                                Edgelist.Add(EdgeData);

                                                GeometRi.Segment3d GeomSegref = new GeometRi.Segment3d(GetGeometRiPt3DFromPt3D(FacestoProj[j].ListAllEdges[k].StPt), GetGeometRiPt3DFromPt3D(FacestoProj[j].ListAllEdges[k].EndPt));


                                                GeometRi.Plane3d GeomPlaneRef = new GeometRi.Plane3d(GetGeometRiPt3DFromPt3D(primorder.First().ProjectionPlane.PointOnPlane), GetGeometRiVect3dFromPt3D(primorder.First().ProjectionPlane.Normal));

                                                GeometRi.Segment3d obj2 = GeomSegref.ProjectionTo(GeomPlaneRef) as GeometRi.Segment3d;


                                                if (obj2 != null)

                                                {
                                                    AllPoints.Add(GetArrayOfDoubleFromGeometRiPoint3D(obj2.P1));
                                                    AllPoints.Add(GetArrayOfDoubleFromGeometRiPoint3D(obj2.P2));
                                                }
                                                else
                                                {
                                                    GeometRi.Point3d p1 = GeomSegref.P1;
                                                    GeometRi.Point3d p2 = GeomSegref.P2;
                                                    p1 = p1.ProjectionTo(GeomPlaneRef);
                                                    p2 = p2.ProjectionTo(GeomPlaneRef);

                                                    obj2 = new GeometRi.Segment3d(p1, p2);
                                                    AllPoints.Add(GetArrayOfDoubleFromGeometRiPoint3D(obj2.P1));
                                                    AllPoints.Add(GetArrayOfDoubleFromGeometRiPoint3D(obj2.P2));
                                                }

                                                Tuple<GeometRi.Point3d, GeometRi.Point3d> result = Calculate.ProjectLineOntoXYPlane(obj2.P1, obj2.P2, GetVect3DFromPt3D((primorder.First().ProjectionPlane.Normal)));

                                                GeometRi.Segment3d resultLine = new GeometRi.Segment3d(result.Item1, result.Item2);

                                                EntityData entd = new EntityData();
                                                entd.Segment = resultLine;
                                                entd.IDSegment = SecData.SuppoId + "," + "Edge" + edgecount.ToString();
                                                LinesData.Add(entd);

                                                //for Dim boundingBox
                                                AllSeg.Add(resultLine);

                                                edgecount++;
                                            }
                                            //for Dim boundingBox
                                            AllParts.Add(AllSeg);

                                            faceData.ListAllEdges = Edgelist;
                                            faceData.FaceIdLocal = "Face" + facecount.ToString();
                                            PData.ListfaceData.Add(faceData);

                                        }



                                    }
                                    catch (Exception e)
                                    {
                                    }


                                }
                                foreach (SupporSpecData SecData in ListCentalSuppoData[i].ListConcreteData)
                                {
                                    List<FaceData> FacestoProj = new List<FaceData>();
                                    Projection2DData PData = new Projection2DData();

                                    foreach (FaceData FaceLoc in SecData.ListfaceData)
                                    {


                                        if (Math.Abs(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(GetVect3dFromVect3D(FaceLoc.FaceNormal), GetVect3DFromPt3D(primorder.First().ProjectionPlane.Normal), new Vector3D(0, 0, 1)))) >= 0 && Math.Abs(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(GetVect3dFromVect3D(FaceLoc.FaceNormal), GetVect3DFromPt3D(primorder.First().ProjectionPlane.Normal), new Vector3D(0, 0, 1)))) < 89)
                                        {
                                            FacestoProj.Add(FaceLoc);

                                            //primorder.First().ProjectionPlane.Normal

                                        }
                                    }
                                    try
                                    {
                                        List<double[]> AllPoints = new List<double[]>();//= new double[][];


                                        for (int j = 0; j < FacestoProj.Count; j++)
                                        {
                                            FaceData faceData = new FaceData();

                                            List<Edgeinfo> Edgelist = new List<Edgeinfo>();
                                            for (int k = 0; k < FacestoProj[j].ListAllEdges.Count; k++)
                                            {
                                                Edgeinfo EdgeData = new Edgeinfo();


                                                //for calculating dist of edge
                                                EdgeData = FacestoProj[j].ListAllEdges[k];
                                                EdgeData.EdgeId = "Edge" + edgecount.ToString();

                                                Edgelist.Add(EdgeData);

                                                GeometRi.Segment3d GeomSegref = new GeometRi.Segment3d(GetGeometRiPt3DFromPt3D(FacestoProj[j].ListAllEdges[k].StPt), GetGeometRiPt3DFromPt3D(FacestoProj[j].ListAllEdges[k].EndPt));

                                                GeometRi.Plane3d GeomPlaneRef = new GeometRi.Plane3d(GetGeometRiPt3DFromPt3D(primorder.First().ProjectionPlane.PointOnPlane), GetGeometRiVect3dFromPt3D(primorder.First().ProjectionPlane.Normal));



                                                GeometRi.Segment3d obj2 = GeomSegref.ProjectionTo(GeomPlaneRef) as GeometRi.Segment3d;


                                                if (obj2 != null)

                                                {
                                                    AllPoints.Add(GetArrayOfDoubleFromGeometRiPoint3D(obj2.P1));
                                                    AllPoints.Add(GetArrayOfDoubleFromGeometRiPoint3D(obj2.P2));
                                                }
                                                else
                                                {
                                                    GeometRi.Point3d p1 = GeomSegref.P1;
                                                    GeometRi.Point3d p2 = GeomSegref.P2;
                                                    p1 = p1.ProjectionTo(GeomPlaneRef);
                                                    p2 = p2.ProjectionTo(GeomPlaneRef);

                                                    obj2 = new GeometRi.Segment3d(p1, p2);
                                                    AllPoints.Add(GetArrayOfDoubleFromGeometRiPoint3D(obj2.P1));
                                                    AllPoints.Add(GetArrayOfDoubleFromGeometRiPoint3D(obj2.P2));
                                                }

                                                Tuple<GeometRi.Point3d, GeometRi.Point3d> result = Calculate.ProjectLineOntoXYPlane(obj2.P1, obj2.P2, GetVect3DFromPt3D((primorder.First().ProjectionPlane.Normal)));

                                                GeometRi.Segment3d resultLine = new GeometRi.Segment3d(result.Item1, result.Item2);




                                                //LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(result.Item1.X, result.Item1.Y, result.Item1.Z), new Point3d(result.Item2.X, result.Item2.Y, result.Item2.Z), MyCol.White);


                                                EntityData entd = new EntityData();
                                                entd.Segment = resultLine;
                                                entd.IDSegment = SecData.SuppoId + "," + "Edge" + edgecount.ToString();
                                                LinesData.Add(entd);

                                                //for Dim boundingBox
                                                AllSeg.Add(resultLine);

                                                edgecount++;

                                            }
                                            //for Dim boundingBox
                                            AllParts.Add(AllSeg);

                                            faceData.ListAllEdges = Edgelist;
                                            faceData.FaceIdLocal = "Face" + facecount.ToString();
                                            PData.ListfaceData.Add(faceData);

                                        }
                                    }
                                    catch (Exception e)
                                    {
                                    }
                                }
                                var AllLiness = Calculate.FitLinesInRectangle(LinesData, 11000, 7000, tracex + boxlen / 2, spaceY - boxht / 2);

                                Dictionary<string, List<GeometRi.Segment3d>> DicIdEdgeList = new Dictionary<string, List<GeometRi.Segment3d>>();

                                foreach (var ent in AllLiness)
                                {
                                    if (ent.IDSegment.Substring(0, 1).ToLower().Equals("s"))
                                    {
                                        List<GeometRi.Segment3d> EdgeList = new List<GeometRi.Segment3d>();

                                        if (DicIdEdgeList.ContainsKey(ent.IDSegment.Split(',')[0]))
                                        {
                                            EdgeList = DicIdEdgeList[ent.IDSegment.Split(',')[0]];
                                            EdgeList.Add(ent.Segment);

                                            DicIdEdgeList[ent.IDSegment.Split(',')[0]] = EdgeList;

                                        }
                                        else
                                        {
                                            EdgeList.Add(ent.Segment);

                                            DicIdEdgeList[ent.IDSegment.Split(',')[0]] = EdgeList;
                                        }
                                    }
                                }

                                List<GeometRi.Segment3d> ListSegs = new List<GeometRi.Segment3d>();

                                foreach (var SecSupo in DicIdEdgeList)
                                {
                                    if (SecSupo.Value.Count < 15)
                                    {
                                        foreach (var line in SecSupo.Value)
                                        {
                                            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(line.P1.X, line.P1.Y, line.P1.Z), new Point3d(line.P2.X, line.P2.Y, line.P2.Z), MyCol.LightBlue);
                                        }
                                    }
                                    else
                                    {

                                        if (Math.Abs(Math.Round(GetRotationFromVec(GetPartbyId(SecSupo.Key, ListCentalSuppoData[i].ListSecondrySuppo).Directionvec).XinDegree)) > 0 && Math.Abs(Math.Round(GetRotationFromVec(GetPartbyId(SecSupo.Key, ListCentalSuppoData[i].ListSecondrySuppo).Directionvec).XinDegree)) % 90 != 0)
                                        {
                                            foreach (var line in SecSupo.Value)
                                            {
                                                LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(line.P1.X, line.P1.Y, line.P1.Z), new Point3d(line.P2.X, line.P2.Y, line.P2.Z), MyCol.LightBlue);
                                            }
                                        }
                                        else
                                        {
                                            List<GeometRi.Segment3d> ListSegOutSide = new List<GeometRi.Segment3d>();
                                            List<GeometRi.Segment3d> ListSegInSide = new List<GeometRi.Segment3d>();
                                            (ListSegInSide, ListSegOutSide) = Calculate.CalculateEdges(SecSupo.Value);
                                            ListSegs.AddRange(ListSegInSide);
                                            ListSegs.AddRange(ListSegOutSide);
                                        }
                                    }
                                }


                                foreach (var ent in AllLiness)
                                {
                                    var line = ent.Segment;
                                    if (ent.IDSegment.ToLower().Contains("c"))
                                    {
                                        LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(line.P1.X, line.P1.Y, line.P1.Z), new Point3d(line.P2.X, line.P2.Y, line.P2.Z), MyCol.Gray);
                                    }
                                    else if (ent.IDSegment.ToLower().Contains("s"))
                                    {
                                        // LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(line.P1.X, line.P1.Y, line.P1.Z), new Point3d(line.P2.X, line.P2.Y, line.P2.Z), MyCol.LightBlue);
                                    }
                                    else
                                    {
                                        LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(line.P1.X, line.P1.Y, line.P1.Z), new Point3d(line.P2.X, line.P2.Y, line.P2.Z), MyCol.Green);
                                    }

                                    // PData.AfterListfaceData.Add();
                                }


                                List<GeometRi.Segment3d> allsecseg = new List<GeometRi.Segment3d>();
                                foreach (var seg in AllLiness)
                                {
                                    if (seg.IDSegment.ToLower().Contains("s"))
                                    {
                                        allsecseg.Add(seg.Segment);
                                    }
                                }

                                double minX = double.MaxValue;
                                double minY = double.MaxValue;
                                double maxX = double.MinValue;
                                double maxY = double.MinValue;

                                var maxminpts = Calculate.FindMinMaxCoordinates(allsecseg, out minX, out minY, out maxX, out maxY);


                                foreach (var line in ListSegs)
                                {
                                    LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(line.P1.X, line.P1.Y, line.P1.Z), new Point3d(line.P2.X, line.P2.Y, line.P2.Z), MyCol.LightBlue);
                                }

                                Dictionary<string, List<EntityData>> SecPart2D = new Dictionary<string, List<EntityData>>();
                                for (int h = 0; h < ListCentalSuppoData[i].ListSecondrySuppo.Count; h++)
                                {
                                    List<EntityData> SecParts = new List<EntityData>();
                                    for (int g = 0; g < AllLiness.Count; g++)
                                    {
                                        if (AllLiness[g].IDSegment.Contains(ListCentalSuppoData[i].ListSecondrySuppo[h].SuppoId))
                                        {
                                            SecParts.Add(AllLiness[g]);
                                        }
                                    }
                                    SecPart2D[ListCentalSuppoData[i].ListSecondrySuppo[h].SuppoId] = SecParts;
                                }

                                //Secondary Dimensioning
                                var secondary_sort = ListCentalSuppoData[i].ListSecondrySuppo.OrderBy(e => e.Boundingboxmin.Z);
                                if (secondary_sort.Count() > 0)
                                {
                                    SecondaryDimension(AcadBlockTableRecord, AcadTransaction, AcadDatabase, secondary_sort, SecPart2D, AllParts, minX, maxX, i);
                                }

                                Dictionary<string, List<EntityData>> ConcretePart2D = new Dictionary<string, List<EntityData>>();
                                for (int h = 0; h < ListCentalSuppoData[i].ListConcreteData.Count; h++)
                                {
                                    List<EntityData> ConcreteParts = new List<EntityData>();
                                    for (int g = 0; g < AllLiness.Count; g++)
                                    {
                                        if (AllLiness[g].IDSegment.Contains(ListCentalSuppoData[i].ListConcreteData[h].SuppoId))
                                        {
                                            ConcreteParts.Add(AllLiness[g]);

                                        }
                                    }
                                    ConcretePart2D[ListCentalSuppoData[i].ListConcreteData[h].SuppoId] = ConcreteParts;
                                }

                                var concrete_sort = ListCentalSuppoData[i].ListConcreteData.OrderBy(e => e.Boundingboxmin.Z);

                                if (concrete_sort.Count() > 0)
                                {
                                    ConcretDimension(AcadBlockTableRecord, AcadTransaction, AcadDatabase, concrete_sort.ElementAt(0), ConcretePart2D, i);
                                    FJWELD(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ConcretePart2D, i);
                                }
                                //support name and quantity
                                CreateSupportName(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, i, ListCentalSuppoData[i].Name);

                                tracex += boxlen; Created_TAG.Add(ListCentalSuppoData[i].Name);
                            }
                        }
                        else
                        {
                            SupportNotCreated.Add(ListCentalSuppoData[i].Name);
                        }
                    }


                }

            }
            else
            {
                //box lines
                LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex + boxlen, spaceY + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);

                LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(tracex, spaceY - boxht + 619.1209, 0), new Point3d(tracex + boxlen, spaceY - boxht + 619.1209, 0), MyCol.LightBlue);


                Vector3D normal = new Vector3D(1, 0, 0);
                bool Isnormal = false;
                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0)
                {
                    LinesData.Clear();
                    ProjectedSecEntity.Clear();
                    DimesionBoxData.Clear();

                    var primorder = ListCentalSuppoData[i].ListPrimarySuppo.OrderByDescending(e => e.Centroid.Z);

                    if (primorder.First().ProjectionPlane.Normal == null)
                    {
                        SupportNotCreated.Add(ListCentalSuppoData[i].Name);
                    }
                    else
                    {
                        var normvec = GetVect3DFromPt3D(primorder.First().ProjectionPlane.Normal);
                        if (!(Math.Abs(normvec.Z).Equals(1)))
                        {
                            Isnormal = true;
                            normal = normvec;
                            normal = Vector3D.CrossProduct(normal, new Vector3D(0, 0, 1));
                            normal.Normalize();
                            if (primorder.First().ProjectionPlane != null && primorder.First().ProjectionPlane.Normal != null)
                            {

                                //for Dim boundingBox
                                List<List<GeometRi.Segment3d>> AllParts = new List<List<GeometRi.Segment3d>>();

                                //for Dim boundingBox
                                List<GeometRi.Segment3d> AllSeg = new List<GeometRi.Segment3d>();


                                foreach (SupporSpecData SecData in ListCentalSuppoData[i].ListPrimarySuppo)
                                {
                                    List<FaceData> FacestoProj = new List<FaceData>();
                                    Projection2DData PData = new Projection2DData();

                                    foreach (BoundingBoxFace FaceLoc in SecData.PrimBoundingBoxData)
                                    {
                                        if (Math.Abs(Math.Round(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(FaceLoc.Normal, GetVect3DFromPt3D(primorder.First().ProjectionPlane.Normal), new Vector3D(0, 0, 1))))) >= 0 && Math.Abs(Math.Round(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(FaceLoc.Normal, GetVect3DFromPt3D(primorder.First().ProjectionPlane.Normal), new Vector3D(0, 0, 1))))) < 89)
                                        {

                                            FaceData primface = new FaceData();
                                            primface.FaceNormal = GetVect3dFromVect3D(FaceLoc.Normal);
                                            primface.PtonPlane = GetPoint3dFromPoint3D(FaceLoc.FacePoint);

                                            foreach (BoundingBoxEdge edge in FaceLoc.Edges)
                                            {
                                                Edgeinfo edgeprim = new Edgeinfo();
                                                edgeprim.StPt = GetPt3DFromPoint3D(edge.StartPoint);
                                                edgeprim.EndPt = GetPt3DFromPoint3D(edge.EndPoint);

                                                primface.ListAllEdges.Add(edgeprim);

                                            }


                                            FacestoProj.Add(primface);

                                        }
                                    }

                                    try
                                    {
                                        List<double[]> AllPoints = new List<double[]>();//= new double[][];
                                        for (int j = 0; j < FacestoProj.Count; j++)
                                        {
                                            FaceData faceData = new FaceData();

                                            List<Edgeinfo> Edgelist = new List<Edgeinfo>();

                                            for (int k = 0; k < FacestoProj[j].ListAllEdges.Count; k++)
                                            {


                                                Edgeinfo EdgeData = new Edgeinfo();


                                                //for calculating dist of edge
                                                EdgeData = FacestoProj[j].ListAllEdges[k];
                                                EdgeData.EdgeId = SecData.SuppoId + "," + "Edge" + edgecount.ToString();

                                                Edgelist.Add(EdgeData);

                                                GeometRi.Segment3d GeomSegref = new GeometRi.Segment3d(GetGeometRiPt3DFromPt3D(FacestoProj[j].ListAllEdges[k].StPt), GetGeometRiPt3DFromPt3D(FacestoProj[j].ListAllEdges[k].EndPt));


                                                GeometRi.Plane3d GeomPlaneRef = new GeometRi.Plane3d(GetGeometRiPt3DFromPt3D(ListCentalSuppoData[i].ListPrimarySuppo[0].ProjectionPlane.PointOnPlane), GetGeometRiVect3dFromPt3D(ListCentalSuppoData[i].ListPrimarySuppo[0].ProjectionPlane.Normal));

                                                GeometRi.Segment3d obj2 = GeomSegref.ProjectionTo(GeomPlaneRef) as GeometRi.Segment3d;


                                                if (obj2 != null)

                                                {
                                                    AllPoints.Add(GetArrayOfDoubleFromGeometRiPoint3D(obj2.P1));
                                                    AllPoints.Add(GetArrayOfDoubleFromGeometRiPoint3D(obj2.P2));
                                                }
                                                else
                                                {
                                                    GeometRi.Point3d p1 = GeomSegref.P1;
                                                    GeometRi.Point3d p2 = GeomSegref.P2;
                                                    p1 = p1.ProjectionTo(GeomPlaneRef);
                                                    p2 = p2.ProjectionTo(GeomPlaneRef);

                                                    obj2 = new GeometRi.Segment3d(p1, p2);
                                                    AllPoints.Add(GetArrayOfDoubleFromGeometRiPoint3D(obj2.P1));
                                                    AllPoints.Add(GetArrayOfDoubleFromGeometRiPoint3D(obj2.P2));
                                                }

                                                Tuple<GeometRi.Point3d, GeometRi.Point3d> result = Calculate.ProjectLineOntoXYPlane(obj2.P1, obj2.P2, GetVect3DFromPt3D((ListCentalSuppoData[i].ListPrimarySuppo[0].ProjectionPlane.Normal)));

                                                GeometRi.Segment3d resultLine = new GeometRi.Segment3d(result.Item1, result.Item2);

                                                EntityData entd = new EntityData();
                                                entd.Segment = resultLine;
                                                entd.IDSegment = SecData.SuppoId + "," + "Edge" + edgecount.ToString();
                                                LinesData.Add(entd);

                                                //for Dim boundingBox
                                                AllSeg.Add(resultLine);

                                                edgecount++;
                                            }
                                            //for Dim boundingBox
                                            AllParts.Add(AllSeg);

                                            faceData.ListAllEdges = Edgelist;
                                            faceData.ListPrimEdges = Edgelist;
                                            faceData.FaceIdLocal = "Face" + facecount.ToString();
                                            PData.ListfaceData.Add(faceData);

                                        }



                                    }
                                    catch (Exception e)
                                    {
                                    }


                                }
                                foreach (SupporSpecData SecData in ListCentalSuppoData[i].ListSecondrySuppo)
                                {
                                    List<FaceData> FacestoProj = new List<FaceData>();
                                    Projection2DData PData = new Projection2DData();

                                    foreach (FaceData FaceLoc in SecData.ListfaceData)
                                    {


                                        if (Math.Abs(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(GetVect3dFromVect3D(FaceLoc.FaceNormal), GetVect3DFromPt3D(primorder.First().ProjectionPlane.Normal), new Vector3D(0, 0, 1)))) >= 0 && Math.Abs(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(GetVect3dFromVect3D(FaceLoc.FaceNormal), GetVect3DFromPt3D(primorder.First().ProjectionPlane.Normal), new Vector3D(0, 0, 1)))) < 89 || HasOpenCircularEdge(FaceLoc))
                                        {
                                            FacestoProj.Add(FaceLoc);

                                        }
                                    }
                                    try
                                    {
                                        List<double[]> AllPoints = new List<double[]>();//= new double[][];


                                        for (int j = 0; j < FacestoProj.Count; j++)
                                        {
                                            FaceData faceData = new FaceData();

                                            List<Edgeinfo> Edgelist = new List<Edgeinfo>();

                                            for (int k = 0; k < FacestoProj[j].ListAllEdges.Count; k++)
                                            {
                                                Edgeinfo EdgeData = new Edgeinfo();


                                                //for calculating dist of edge
                                                EdgeData = FacestoProj[j].ListAllEdges[k];
                                                EdgeData.EdgeId = SecData.SuppoId + "," + "Edge" + edgecount.ToString();

                                                Edgelist.Add(EdgeData);

                                                GeometRi.Segment3d GeomSegref = new GeometRi.Segment3d(GetGeometRiPt3DFromPt3D(FacestoProj[j].ListAllEdges[k].StPt), GetGeometRiPt3DFromPt3D(FacestoProj[j].ListAllEdges[k].EndPt));


                                                GeometRi.Plane3d GeomPlaneRef = new GeometRi.Plane3d(GetGeometRiPt3DFromPt3D(primorder.First().ProjectionPlane.PointOnPlane), GetGeometRiVect3dFromPt3D(primorder.First().ProjectionPlane.Normal));

                                                GeometRi.Segment3d obj2 = GeomSegref.ProjectionTo(GeomPlaneRef) as GeometRi.Segment3d;


                                                if (obj2 != null)

                                                {
                                                    AllPoints.Add(GetArrayOfDoubleFromGeometRiPoint3D(obj2.P1));
                                                    AllPoints.Add(GetArrayOfDoubleFromGeometRiPoint3D(obj2.P2));
                                                }
                                                else
                                                {
                                                    GeometRi.Point3d p1 = GeomSegref.P1;
                                                    GeometRi.Point3d p2 = GeomSegref.P2;
                                                    p1 = p1.ProjectionTo(GeomPlaneRef);
                                                    p2 = p2.ProjectionTo(GeomPlaneRef);

                                                    obj2 = new GeometRi.Segment3d(p1, p2);
                                                    AllPoints.Add(GetArrayOfDoubleFromGeometRiPoint3D(obj2.P1));
                                                    AllPoints.Add(GetArrayOfDoubleFromGeometRiPoint3D(obj2.P2));
                                                }

                                                Tuple<GeometRi.Point3d, GeometRi.Point3d> result = Calculate.ProjectLineOntoXYPlane(obj2.P1, obj2.P2, GetVect3DFromPt3D((primorder.First().ProjectionPlane.Normal)));

                                                GeometRi.Segment3d resultLine = new GeometRi.Segment3d(result.Item1, result.Item2);

                                                EntityData entd = new EntityData();
                                                entd.Segment = resultLine;
                                                entd.IDSegment = SecData.SuppoId + "," + "Edge" + edgecount.ToString();
                                                LinesData.Add(entd);

                                                //for Dim boundingBox
                                                AllSeg.Add(resultLine);

                                                edgecount++;
                                            }
                                            //for Dim boundingBox
                                            AllParts.Add(AllSeg);

                                            faceData.ListAllEdges = Edgelist;
                                            faceData.FaceIdLocal = "Face" + facecount.ToString();
                                            PData.ListfaceData.Add(faceData);

                                        }



                                    }
                                    catch (Exception e)
                                    {
                                    }


                                }
                                foreach (SupporSpecData SecData in ListCentalSuppoData[i].ListConcreteData)
                                {
                                    List<FaceData> FacestoProj = new List<FaceData>();
                                    Projection2DData PData = new Projection2DData();

                                    foreach (FaceData FaceLoc in SecData.ListfaceData)
                                    {


                                        if (Math.Abs(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(GetVect3dFromVect3D(FaceLoc.FaceNormal), GetVect3DFromPt3D(primorder.First().ProjectionPlane.Normal), new Vector3D(0, 0, 1)))) >= 0 && Math.Abs(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(GetVect3dFromVect3D(FaceLoc.FaceNormal), GetVect3DFromPt3D(primorder.First().ProjectionPlane.Normal), new Vector3D(0, 0, 1)))) < 89)
                                        {
                                            FacestoProj.Add(FaceLoc);

                                            //primorder.First().ProjectionPlane.Normal

                                        }
                                    }
                                    try
                                    {
                                        List<double[]> AllPoints = new List<double[]>();//= new double[][];


                                        for (int j = 0; j < FacestoProj.Count; j++)
                                        {
                                            FaceData faceData = new FaceData();

                                            List<Edgeinfo> Edgelist = new List<Edgeinfo>();
                                            for (int k = 0; k < FacestoProj[j].ListAllEdges.Count; k++)
                                            {
                                                Edgeinfo EdgeData = new Edgeinfo();


                                                //for calculating dist of edge
                                                EdgeData = FacestoProj[j].ListAllEdges[k];
                                                EdgeData.EdgeId = "Edge" + edgecount.ToString();

                                                Edgelist.Add(EdgeData);

                                                GeometRi.Segment3d GeomSegref = new GeometRi.Segment3d(GetGeometRiPt3DFromPt3D(FacestoProj[j].ListAllEdges[k].StPt), GetGeometRiPt3DFromPt3D(FacestoProj[j].ListAllEdges[k].EndPt));

                                                GeometRi.Plane3d GeomPlaneRef = new GeometRi.Plane3d(GetGeometRiPt3DFromPt3D(primorder.First().ProjectionPlane.PointOnPlane), GetGeometRiVect3dFromPt3D(primorder.First().ProjectionPlane.Normal));



                                                GeometRi.Segment3d obj2 = GeomSegref.ProjectionTo(GeomPlaneRef) as GeometRi.Segment3d;


                                                if (obj2 != null)

                                                {
                                                    AllPoints.Add(GetArrayOfDoubleFromGeometRiPoint3D(obj2.P1));
                                                    AllPoints.Add(GetArrayOfDoubleFromGeometRiPoint3D(obj2.P2));
                                                }
                                                else
                                                {
                                                    GeometRi.Point3d p1 = GeomSegref.P1;
                                                    GeometRi.Point3d p2 = GeomSegref.P2;
                                                    p1 = p1.ProjectionTo(GeomPlaneRef);
                                                    p2 = p2.ProjectionTo(GeomPlaneRef);

                                                    obj2 = new GeometRi.Segment3d(p1, p2);
                                                    AllPoints.Add(GetArrayOfDoubleFromGeometRiPoint3D(obj2.P1));
                                                    AllPoints.Add(GetArrayOfDoubleFromGeometRiPoint3D(obj2.P2));
                                                }

                                                Tuple<GeometRi.Point3d, GeometRi.Point3d> result = Calculate.ProjectLineOntoXYPlane(obj2.P1, obj2.P2, GetVect3DFromPt3D((primorder.First().ProjectionPlane.Normal)));

                                                GeometRi.Segment3d resultLine = new GeometRi.Segment3d(result.Item1, result.Item2);




                                                //LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(result.Item1.X, result.Item1.Y, result.Item1.Z), new Point3d(result.Item2.X, result.Item2.Y, result.Item2.Z), MyCol.White);


                                                EntityData entd = new EntityData();
                                                entd.Segment = resultLine;
                                                entd.IDSegment = SecData.SuppoId + "," + "Edge" + edgecount.ToString();
                                                LinesData.Add(entd);

                                                //for Dim boundingBox
                                                AllSeg.Add(resultLine);

                                                edgecount++;

                                            }
                                            //for Dim boundingBox
                                            AllParts.Add(AllSeg);

                                            faceData.ListAllEdges = Edgelist;
                                            faceData.FaceIdLocal = "Face" + facecount.ToString();
                                            PData.ListfaceData.Add(faceData);

                                        }
                                    }
                                    catch (Exception e)
                                    {
                                    }
                                }
                                var AllLiness = Calculate.FitLinesInRectangle(LinesData, 11000, 7000, tracex + boxlen / 4, spaceY - boxht / 2);

                                List<GeometRi.Segment3d> allsecseg = new List<GeometRi.Segment3d>();
                                foreach (var seg in AllLiness)
                                {
                                    if (seg.IDSegment.ToLower().Contains("s"))
                                    {
                                        allsecseg.Add(seg.Segment);
                                    }
                                }

                                double minX = double.MaxValue;
                                double minY = double.MaxValue;
                                double maxX = double.MinValue;
                                double maxY = double.MinValue;

                                var maxminpts = Calculate.FindMinMaxCoordinates(allsecseg, out minX, out minY, out maxX, out maxY);


                                Dictionary<string, List<GeometRi.Segment3d>> DicIdEdgeList = new Dictionary<string, List<GeometRi.Segment3d>>();

                                foreach (var ent in AllLiness)
                                {
                                    if (ent.IDSegment.Substring(0, 1).ToLower().Equals("s"))
                                    {
                                        List<GeometRi.Segment3d> EdgeList = new List<GeometRi.Segment3d>();

                                        if (DicIdEdgeList.ContainsKey(ent.IDSegment.Split(',')[0]))
                                        {
                                            EdgeList = DicIdEdgeList[ent.IDSegment.Split(',')[0]];
                                            EdgeList.Add(ent.Segment);

                                            DicIdEdgeList[ent.IDSegment.Split(',')[0]] = EdgeList;

                                        }
                                        else
                                        {
                                            EdgeList.Add(ent.Segment);

                                            DicIdEdgeList[ent.IDSegment.Split(',')[0]] = EdgeList;
                                        }
                                    }
                                }

                                List<GeometRi.Segment3d> ListSegs = new List<GeometRi.Segment3d>();

                                foreach (var SecSupo in DicIdEdgeList)
                                {
                                    if (SecSupo.Value.Count < 15)
                                    {
                                        foreach (var line in SecSupo.Value)
                                        {
                                            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(line.P1.X, line.P1.Y, line.P1.Z), new Point3d(line.P2.X, line.P2.Y, line.P2.Z), MyCol.LightBlue);
                                        }
                                    }
                                    else
                                    {

                                        if (Math.Abs(Math.Round(GetRotationFromVec(GetPartbyId(SecSupo.Key, ListCentalSuppoData[i].ListSecondrySuppo).Directionvec).XinDegree)) > 0 && Math.Abs(Math.Round(GetRotationFromVec(GetPartbyId(SecSupo.Key, ListCentalSuppoData[i].ListSecondrySuppo).Directionvec).XinDegree)) % 90 != 0)
                                        {
                                            foreach (var line in SecSupo.Value)
                                            {
                                                LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(line.P1.X, line.P1.Y, line.P1.Z), new Point3d(line.P2.X, line.P2.Y, line.P2.Z), MyCol.LightBlue);
                                            }
                                        }
                                        else
                                        {
                                            List<GeometRi.Segment3d> ListSegOutSide = new List<GeometRi.Segment3d>();
                                            List<GeometRi.Segment3d> ListSegInSide = new List<GeometRi.Segment3d>();
                                            (ListSegInSide, ListSegOutSide) = Calculate.CalculateEdges(SecSupo.Value);
                                            ListSegs.AddRange(ListSegInSide);
                                            ListSegs.AddRange(ListSegOutSide);
                                        }
                                    }
                                }


                                foreach (var ent in AllLiness)
                                {
                                    var line = ent.Segment;
                                    if (ent.IDSegment.ToLower().Contains("c"))
                                    {
                                        LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(line.P1.X, line.P1.Y, line.P1.Z), new Point3d(line.P2.X, line.P2.Y, line.P2.Z), MyCol.Gray);
                                    }
                                    else if (ent.IDSegment.ToLower().Contains("s"))
                                    {
                                        // LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(line.P1.X, line.P1.Y, line.P1.Z), new Point3d(line.P2.X, line.P2.Y, line.P2.Z), MyCol.LightBlue);
                                    }
                                    else
                                    {
                                        LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(line.P1.X, line.P1.Y, line.P1.Z), new Point3d(line.P2.X, line.P2.Y, line.P2.Z), MyCol.Green);
                                    }

                                    // PData.AfterListfaceData.Add();
                                }


                                foreach (var line in ListSegs)
                                {
                                    LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(line.P1.X, line.P1.Y, line.P1.Z), new Point3d(line.P2.X, line.P2.Y, line.P2.Z), MyCol.LightBlue);
                                }

                                Dictionary<string, List<EntityData>> PrimPart2D = new Dictionary<string, List<EntityData>>();
                                for (int h = 0; h < ListCentalSuppoData[i].ListPrimarySuppo.Count; h++)
                                {
                                    List<EntityData> PrimParts = new List<EntityData>();
                                    for (int g = 0; g < AllLiness.Count; g++)
                                    {
                                        if (AllLiness[g].IDSegment.Contains(ListCentalSuppoData[i].ListPrimarySuppo[h].SuppoId))
                                        {
                                            PrimParts.Add(AllLiness[g]);
                                        }
                                    }
                                    PrimPart2D[ListCentalSuppoData[i].ListPrimarySuppo[h].SuppoId] = PrimParts;
                                }


                                Dictionary<string, List<EntityData>> SecPart2D = new Dictionary<string, List<EntityData>>();
                                for (int h = 0; h < ListCentalSuppoData[i].ListSecondrySuppo.Count; h++)
                                {
                                    List<EntityData> SecParts = new List<EntityData>();
                                    for (int g = 0; g < AllLiness.Count; g++)
                                    {
                                        if (AllLiness[g].IDSegment.Contains(ListCentalSuppoData[i].ListSecondrySuppo[h].SuppoId))
                                        {
                                            SecParts.Add(AllLiness[g]);
                                        }
                                    }
                                    SecPart2D[ListCentalSuppoData[i].ListSecondrySuppo[h].SuppoId] = SecParts;
                                }



                                Dictionary<string, List<EntityData>> ConcretePart2D = new Dictionary<string, List<EntityData>>();
                                for (int h = 0; h < ListCentalSuppoData[i].ListConcreteData.Count; h++)
                                {
                                    List<EntityData> ConcreteParts = new List<EntityData>();
                                    for (int g = 0; g < AllLiness.Count; g++)
                                    {
                                        if (AllLiness[g].IDSegment.Contains(ListCentalSuppoData[i].ListConcreteData[h].SuppoId))
                                        {
                                            ConcreteParts.Add(AllLiness[g]);

                                        }
                                    }
                                    ConcretePart2D[ListCentalSuppoData[i].ListConcreteData[h].SuppoId] = ConcreteParts;
                                }



                                var concrete_sort = ListCentalSuppoData[i].ListConcreteData.OrderBy(e => e.Boundingboxmin.Z);

                                if (concrete_sort.Count() > 0)
                                {
                                    ConcretDimension(AcadBlockTableRecord, AcadTransaction, AcadDatabase, concrete_sort.ElementAt(0), ConcretePart2D, i);
                                    FJWELD(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ConcretePart2D, i);
                                }

                                List<SupporSpecData> SecFilter = new List<SupporSpecData>();
                                foreach (SupporSpecData sec in ListCentalSuppoData[i].ListSecondrySuppo)
                                {
                                    if (sec.IsGussetplate == false && sec.IsAnchor == false)
                                    {
                                        SecFilter.Add(sec);
                                    }
                                }

                                //Secondary Dimensioning
                                var secondary_sort = SecFilter.OrderByDescending(e => e.Boundingboxmin.Z);
                                if (secondary_sort.Count() > 0)
                                {
                                    SecondaryDimension(AcadBlockTableRecord, AcadTransaction, AcadDatabase, secondary_sort, SecPart2D, AllParts, minX, maxX, i, PrimPart2D);
                                }

                                //for collecting prim faces edges
                                Dictionary<string, List<GeometRi.Segment3d>> PrimFaceEdges = new Dictionary<string, List<GeometRi.Segment3d>>();

                                foreach (SupporSpecData prim in primorder)
                                {
                                    List<GeometRi.Segment3d> primbyID = new List<GeometRi.Segment3d>();
                                    foreach (var priment in AllLiness)
                                    {
                                        if (priment.IDSegment.Contains(prim.SuppoId))
                                        {
                                            primbyID.Add(priment.Segment);
                                        }
                                    }
                                    PrimFaceEdges.Add(prim.SuppoId, primbyID);
                                }
                                ///////////////
                                PlacePrim(AcadBlockTableRecord, AcadTransaction, AcadDatabase, PrimFaceEdges, secondary_sort, SecPart2D, i);


                                //support name and quantity
                                //CreateSupportName(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, i);

                                //tracex += boxlen; Created_TAG.Add(ListCentalSuppoData[i].Name);
                            }
                        }
                        else
                        {
                            SupportNotCreated.Add(ListCentalSuppoData[i].Name);
                        }
                    }


                }
                else if (ListCentalSuppoData[i].ListSecondrySuppo.Count > 0)
                {
                    LinesData.Clear();
                    ProjectedSecEntity.Clear();
                    DimesionBoxData.Clear();
                    var primorder = ListCentalSuppoData[i].ListSecondrySuppo.OrderByDescending(e => e.Centroid.Z);
                    if (primorder.First().ProjectionPlane.Normal == null)
                    {
                        SupportNotCreated.Add(ListCentalSuppoData[i].Name);
                    }
                    else
                    {
                        var normvec = GetVect3DFromPt3D(primorder.First().ProjectionPlane.Normal);
                        if (!(Math.Abs(normvec.Z).Equals(1)))
                        {
                            Isnormal = true;
                            normal = normvec;
                            normal = Vector3D.CrossProduct(normal, new Vector3D(0, 0, 1));
                            normal.Normalize();
                            if (primorder.First().ProjectionPlane != null && primorder.First().ProjectionPlane.Normal != null)
                            {
                                //for Dim boundingBox
                                List<List<GeometRi.Segment3d>> AllParts = new List<List<GeometRi.Segment3d>>();

                                //for Dim boundingBox
                                List<GeometRi.Segment3d> AllSeg = new List<GeometRi.Segment3d>();
                                foreach (SupporSpecData SecData in ListCentalSuppoData[i].ListSecondrySuppo)
                                {
                                    List<FaceData> FacestoProj = new List<FaceData>();
                                    Projection2DData PData = new Projection2DData();

                                    foreach (FaceData FaceLoc in SecData.ListfaceData)
                                    {


                                        if (Math.Abs(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(GetVect3dFromVect3D(FaceLoc.FaceNormal), GetVect3DFromPt3D(primorder.First().ProjectionPlane.Normal), new Vector3D(0, 0, 1)))) >= 0 && Math.Abs(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(GetVect3dFromVect3D(FaceLoc.FaceNormal), GetVect3DFromPt3D(primorder.First().ProjectionPlane.Normal), new Vector3D(0, 0, 1)))) < 89 || HasOpenCircularEdge(FaceLoc))
                                        {
                                            FacestoProj.Add(FaceLoc);

                                        }
                                    }
                                    try
                                    {
                                        List<double[]> AllPoints = new List<double[]>();//= new double[][];


                                        for (int j = 0; j < FacestoProj.Count; j++)
                                        {
                                            FaceData faceData = new FaceData();

                                            List<Edgeinfo> Edgelist = new List<Edgeinfo>();

                                            for (int k = 0; k < FacestoProj[j].ListAllEdges.Count; k++)
                                            {
                                                Edgeinfo EdgeData = new Edgeinfo();


                                                //for calculating dist of edge
                                                EdgeData = FacestoProj[j].ListAllEdges[k];
                                                EdgeData.EdgeId = SecData.SuppoId + "," + "Edge" + edgecount.ToString();

                                                Edgelist.Add(EdgeData);

                                                GeometRi.Segment3d GeomSegref = new GeometRi.Segment3d(GetGeometRiPt3DFromPt3D(FacestoProj[j].ListAllEdges[k].StPt), GetGeometRiPt3DFromPt3D(FacestoProj[j].ListAllEdges[k].EndPt));


                                                GeometRi.Plane3d GeomPlaneRef = new GeometRi.Plane3d(GetGeometRiPt3DFromPt3D(primorder.First().ProjectionPlane.PointOnPlane), GetGeometRiVect3dFromPt3D(primorder.First().ProjectionPlane.Normal));

                                                GeometRi.Segment3d obj2 = GeomSegref.ProjectionTo(GeomPlaneRef) as GeometRi.Segment3d;


                                                if (obj2 != null)

                                                {
                                                    AllPoints.Add(GetArrayOfDoubleFromGeometRiPoint3D(obj2.P1));
                                                    AllPoints.Add(GetArrayOfDoubleFromGeometRiPoint3D(obj2.P2));
                                                }
                                                else
                                                {
                                                    GeometRi.Point3d p1 = GeomSegref.P1;
                                                    GeometRi.Point3d p2 = GeomSegref.P2;
                                                    p1 = p1.ProjectionTo(GeomPlaneRef);
                                                    p2 = p2.ProjectionTo(GeomPlaneRef);

                                                    obj2 = new GeometRi.Segment3d(p1, p2);
                                                    AllPoints.Add(GetArrayOfDoubleFromGeometRiPoint3D(obj2.P1));
                                                    AllPoints.Add(GetArrayOfDoubleFromGeometRiPoint3D(obj2.P2));
                                                }

                                                Tuple<GeometRi.Point3d, GeometRi.Point3d> result = Calculate.ProjectLineOntoXYPlane(obj2.P1, obj2.P2, GetVect3DFromPt3D((primorder.First().ProjectionPlane.Normal)));

                                                GeometRi.Segment3d resultLine = new GeometRi.Segment3d(result.Item1, result.Item2);

                                                EntityData entd = new EntityData();
                                                entd.Segment = resultLine;
                                                entd.IDSegment = SecData.SuppoId + "," + "Edge" + edgecount.ToString();
                                                LinesData.Add(entd);

                                                //for Dim boundingBox
                                                AllSeg.Add(resultLine);

                                                edgecount++;
                                            }
                                            //for Dim boundingBox
                                            AllParts.Add(AllSeg);

                                            faceData.ListAllEdges = Edgelist;
                                            faceData.FaceIdLocal = "Face" + facecount.ToString();
                                            PData.ListfaceData.Add(faceData);

                                        }



                                    }
                                    catch (Exception e)
                                    {
                                    }


                                }
                                foreach (SupporSpecData SecData in ListCentalSuppoData[i].ListConcreteData)
                                {
                                    List<FaceData> FacestoProj = new List<FaceData>();
                                    Projection2DData PData = new Projection2DData();

                                    foreach (FaceData FaceLoc in SecData.ListfaceData)
                                    {


                                        if (Math.Abs(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(GetVect3dFromVect3D(FaceLoc.FaceNormal), GetVect3DFromPt3D(primorder.First().ProjectionPlane.Normal), new Vector3D(0, 0, 1)))) >= 0 && Math.Abs(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(GetVect3dFromVect3D(FaceLoc.FaceNormal), GetVect3DFromPt3D(primorder.First().ProjectionPlane.Normal), new Vector3D(0, 0, 1)))) < 89)
                                        {
                                            FacestoProj.Add(FaceLoc);

                                            //primorder.First().ProjectionPlane.Normal

                                        }
                                    }
                                    try
                                    {
                                        List<double[]> AllPoints = new List<double[]>();//= new double[][];


                                        for (int j = 0; j < FacestoProj.Count; j++)
                                        {
                                            FaceData faceData = new FaceData();

                                            List<Edgeinfo> Edgelist = new List<Edgeinfo>();
                                            for (int k = 0; k < FacestoProj[j].ListAllEdges.Count; k++)
                                            {
                                                Edgeinfo EdgeData = new Edgeinfo();


                                                //for calculating dist of edge
                                                EdgeData = FacestoProj[j].ListAllEdges[k];
                                                EdgeData.EdgeId = "Edge" + edgecount.ToString();

                                                Edgelist.Add(EdgeData);

                                                GeometRi.Segment3d GeomSegref = new GeometRi.Segment3d(GetGeometRiPt3DFromPt3D(FacestoProj[j].ListAllEdges[k].StPt), GetGeometRiPt3DFromPt3D(FacestoProj[j].ListAllEdges[k].EndPt));

                                                GeometRi.Plane3d GeomPlaneRef = new GeometRi.Plane3d(GetGeometRiPt3DFromPt3D(primorder.First().ProjectionPlane.PointOnPlane), GetGeometRiVect3dFromPt3D(primorder.First().ProjectionPlane.Normal));



                                                GeometRi.Segment3d obj2 = GeomSegref.ProjectionTo(GeomPlaneRef) as GeometRi.Segment3d;


                                                if (obj2 != null)

                                                {
                                                    AllPoints.Add(GetArrayOfDoubleFromGeometRiPoint3D(obj2.P1));
                                                    AllPoints.Add(GetArrayOfDoubleFromGeometRiPoint3D(obj2.P2));
                                                }
                                                else
                                                {
                                                    GeometRi.Point3d p1 = GeomSegref.P1;
                                                    GeometRi.Point3d p2 = GeomSegref.P2;
                                                    p1 = p1.ProjectionTo(GeomPlaneRef);
                                                    p2 = p2.ProjectionTo(GeomPlaneRef);

                                                    obj2 = new GeometRi.Segment3d(p1, p2);
                                                    AllPoints.Add(GetArrayOfDoubleFromGeometRiPoint3D(obj2.P1));
                                                    AllPoints.Add(GetArrayOfDoubleFromGeometRiPoint3D(obj2.P2));
                                                }

                                                Tuple<GeometRi.Point3d, GeometRi.Point3d> result = Calculate.ProjectLineOntoXYPlane(obj2.P1, obj2.P2, GetVect3DFromPt3D((primorder.First().ProjectionPlane.Normal)));

                                                GeometRi.Segment3d resultLine = new GeometRi.Segment3d(result.Item1, result.Item2);




                                                //LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(result.Item1.X, result.Item1.Y, result.Item1.Z), new Point3d(result.Item2.X, result.Item2.Y, result.Item2.Z), MyCol.White);


                                                EntityData entd = new EntityData();
                                                entd.Segment = resultLine;
                                                entd.IDSegment = SecData.SuppoId + "," + "Edge" + edgecount.ToString();
                                                LinesData.Add(entd);

                                                //for Dim boundingBox
                                                AllSeg.Add(resultLine);

                                                edgecount++;

                                            }
                                            //for Dim boundingBox
                                            AllParts.Add(AllSeg);

                                            faceData.ListAllEdges = Edgelist;
                                            faceData.FaceIdLocal = "Face" + facecount.ToString();
                                            PData.ListfaceData.Add(faceData);

                                        }
                                    }
                                    catch (Exception e)
                                    {
                                    }
                                }
                                var AllLiness = Calculate.FitLinesInRectangle(LinesData, 11000, 7000, tracex + boxlen / 4, spaceY - boxht / 2);

                                Dictionary<string, List<GeometRi.Segment3d>> DicIdEdgeList = new Dictionary<string, List<GeometRi.Segment3d>>();

                                foreach (var ent in AllLiness)
                                {
                                    if (ent.IDSegment.Substring(0, 1).ToLower().Equals("s"))
                                    {
                                        List<GeometRi.Segment3d> EdgeList = new List<GeometRi.Segment3d>();

                                        if (DicIdEdgeList.ContainsKey(ent.IDSegment.Split(',')[0]))
                                        {
                                            EdgeList = DicIdEdgeList[ent.IDSegment.Split(',')[0]];
                                            EdgeList.Add(ent.Segment);

                                            DicIdEdgeList[ent.IDSegment.Split(',')[0]] = EdgeList;

                                        }
                                        else
                                        {
                                            EdgeList.Add(ent.Segment);

                                            DicIdEdgeList[ent.IDSegment.Split(',')[0]] = EdgeList;
                                        }
                                    }
                                }

                                List<GeometRi.Segment3d> ListSegs = new List<GeometRi.Segment3d>();

                                foreach (var SecSupo in DicIdEdgeList)
                                {
                                    if (SecSupo.Value.Count < 15)
                                    {
                                        foreach (var line in SecSupo.Value)
                                        {
                                            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(line.P1.X, line.P1.Y, line.P1.Z), new Point3d(line.P2.X, line.P2.Y, line.P2.Z), MyCol.LightBlue);
                                        }
                                    }
                                    else
                                    {

                                        if (Math.Abs(Math.Round(GetRotationFromVec(GetPartbyId(SecSupo.Key, ListCentalSuppoData[i].ListSecondrySuppo).Directionvec).XinDegree)) > 0 && Math.Abs(Math.Round(GetRotationFromVec(GetPartbyId(SecSupo.Key, ListCentalSuppoData[i].ListSecondrySuppo).Directionvec).XinDegree)) % 90 != 0)
                                        {
                                            foreach (var line in SecSupo.Value)
                                            {
                                                LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(line.P1.X, line.P1.Y, line.P1.Z), new Point3d(line.P2.X, line.P2.Y, line.P2.Z), MyCol.LightBlue);
                                            }
                                        }
                                        else
                                        {
                                            List<GeometRi.Segment3d> ListSegOutSide = new List<GeometRi.Segment3d>();
                                            List<GeometRi.Segment3d> ListSegInSide = new List<GeometRi.Segment3d>();
                                            (ListSegInSide, ListSegOutSide) = Calculate.CalculateEdges(SecSupo.Value);
                                            ListSegs.AddRange(ListSegInSide);
                                            ListSegs.AddRange(ListSegOutSide);
                                        }
                                    }
                                }


                                foreach (var ent in AllLiness)
                                {
                                    var line = ent.Segment;
                                    if (ent.IDSegment.ToLower().Contains("c"))
                                    {
                                        LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(line.P1.X, line.P1.Y, line.P1.Z), new Point3d(line.P2.X, line.P2.Y, line.P2.Z), MyCol.Gray);
                                    }
                                    else if (ent.IDSegment.ToLower().Contains("s"))
                                    {
                                        // LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(line.P1.X, line.P1.Y, line.P1.Z), new Point3d(line.P2.X, line.P2.Y, line.P2.Z), MyCol.LightBlue);
                                    }
                                    else
                                    {
                                        LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(line.P1.X, line.P1.Y, line.P1.Z), new Point3d(line.P2.X, line.P2.Y, line.P2.Z), MyCol.Green);
                                    }

                                    // PData.AfterListfaceData.Add();
                                }


                                List<GeometRi.Segment3d> allsecseg = new List<GeometRi.Segment3d>();
                                foreach (var seg in AllLiness)
                                {
                                    if (seg.IDSegment.ToLower().Contains("s"))
                                    {
                                        allsecseg.Add(seg.Segment);
                                    }
                                }

                                double minX = double.MaxValue;
                                double minY = double.MaxValue;
                                double maxX = double.MinValue;
                                double maxY = double.MinValue;

                                var maxminpts = Calculate.FindMinMaxCoordinates(allsecseg, out minX, out minY, out maxX, out maxY);


                                foreach (var line in ListSegs)
                                {
                                    LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(line.P1.X, line.P1.Y, line.P1.Z), new Point3d(line.P2.X, line.P2.Y, line.P2.Z), MyCol.LightBlue);
                                }

                                Dictionary<string, List<EntityData>> SecPart2D = new Dictionary<string, List<EntityData>>();
                                for (int h = 0; h < ListCentalSuppoData[i].ListSecondrySuppo.Count; h++)
                                {
                                    List<EntityData> SecParts = new List<EntityData>();
                                    for (int g = 0; g < AllLiness.Count; g++)
                                    {
                                        if (AllLiness[g].IDSegment.Contains(ListCentalSuppoData[i].ListSecondrySuppo[h].SuppoId))
                                        {
                                            SecParts.Add(AllLiness[g]);
                                        }
                                    }
                                    SecPart2D[ListCentalSuppoData[i].ListSecondrySuppo[h].SuppoId] = SecParts;
                                }

                                //Secondary Dimensioning
                                var secondary_sort = ListCentalSuppoData[i].ListSecondrySuppo.OrderBy(e => e.Boundingboxmin.Z);
                                if (secondary_sort.Count() > 0)
                                {
                                    SecondaryDimension(AcadBlockTableRecord, AcadTransaction, AcadDatabase, secondary_sort, SecPart2D, AllParts, minX, maxX, i);
                                }

                                Dictionary<string, List<EntityData>> ConcretePart2D = new Dictionary<string, List<EntityData>>();
                                for (int h = 0; h < ListCentalSuppoData[i].ListConcreteData.Count; h++)
                                {
                                    List<EntityData> ConcreteParts = new List<EntityData>();
                                    for (int g = 0; g < AllLiness.Count; g++)
                                    {
                                        if (AllLiness[g].IDSegment.Contains(ListCentalSuppoData[i].ListConcreteData[h].SuppoId))
                                        {
                                            ConcreteParts.Add(AllLiness[g]);

                                        }
                                    }
                                    ConcretePart2D[ListCentalSuppoData[i].ListConcreteData[h].SuppoId] = ConcreteParts;
                                }

                                var concrete_sort = ListCentalSuppoData[i].ListConcreteData.OrderBy(e => e.Boundingboxmin.Z);

                                if (concrete_sort.Count() > 0)
                                {
                                    ConcretDimension(AcadBlockTableRecord, AcadTransaction, AcadDatabase, concrete_sort.ElementAt(0), ConcretePart2D, i);
                                    FJWELD(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ConcretePart2D, i);
                                }
                                //support name and quantity
                                CreateSupportName(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, i, ListCentalSuppoData[i].Name);

                                tracex += boxlen; Created_TAG.Add(ListCentalSuppoData[i].Name);
                            }
                        }
                        else
                        {
                            SupportNotCreated.Add(ListCentalSuppoData[i].Name);
                        }
                    }


                }


                if (Isnormal)
                {
                    if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0)
                    {
                        LinesData.Clear();
                        ProjectedSecEntity.Clear();
                        DimesionBoxData.Clear();
                        var primorder = ListCentalSuppoData[i].ListPrimarySuppo.OrderByDescending(e => e.Centroid.Z);

                        if (normal == null)
                        {
                            SupportNotCreated.Add(ListCentalSuppoData[i].Name);
                        }
                        else
                        {
                            var normvec = normal;
                            if (!(Math.Abs(normvec.Z).Equals(1)))
                            {
                                if (normal != null && normal != null)
                                {

                                    //for Dim boundingBox
                                    List<List<GeometRi.Segment3d>> AllParts = new List<List<GeometRi.Segment3d>>();

                                    //for Dim boundingBox
                                    List<GeometRi.Segment3d> AllSeg = new List<GeometRi.Segment3d>();


                                    foreach (SupporSpecData SecData in ListCentalSuppoData[i].ListPrimarySuppo)
                                    {
                                        List<FaceData> FacestoProj = new List<FaceData>();
                                        Projection2DData PData = new Projection2DData();

                                        foreach (BoundingBoxFace FaceLoc in SecData.PrimBoundingBoxData)
                                        {
                                            if (Math.Abs(Math.Round(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(FaceLoc.Normal, normal, new Vector3D(0, 0, 1))))) >= 0 && Math.Abs(Math.Round(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(FaceLoc.Normal, normal, new Vector3D(0, 0, 1))))) < 89)
                                            {

                                                FaceData primface = new FaceData();
                                                primface.FaceNormal = GetVect3dFromVect3D(FaceLoc.Normal);
                                                primface.PtonPlane = GetPoint3dFromPoint3D(FaceLoc.FacePoint);

                                                foreach (BoundingBoxEdge edge in FaceLoc.Edges)
                                                {
                                                    Edgeinfo edgeprim = new Edgeinfo();
                                                    edgeprim.StPt = GetPt3DFromPoint3D(edge.StartPoint);
                                                    edgeprim.EndPt = GetPt3DFromPoint3D(edge.EndPoint);

                                                    primface.ListAllEdges.Add(edgeprim);

                                                }


                                                FacestoProj.Add(primface);

                                            }
                                        }
                                        try
                                        {
                                            List<double[]> AllPoints = new List<double[]>();//= new double[][];
                                            for (int j = 0; j < FacestoProj.Count; j++)
                                            {
                                                FaceData faceData = new FaceData();

                                                List<Edgeinfo> Edgelist = new List<Edgeinfo>();

                                                for (int k = 0; k < FacestoProj[j].ListAllEdges.Count; k++)
                                                {


                                                    Edgeinfo EdgeData = new Edgeinfo();


                                                    //for calculating dist of edge
                                                    EdgeData = FacestoProj[j].ListAllEdges[k];
                                                    EdgeData.EdgeId = SecData.SuppoId + "," + "Edge" + edgecount.ToString();

                                                    Edgelist.Add(EdgeData);

                                                    GeometRi.Segment3d GeomSegref = new GeometRi.Segment3d(GetGeometRiPt3DFromPt3D(FacestoProj[j].ListAllEdges[k].StPt), GetGeometRiPt3DFromPt3D(FacestoProj[j].ListAllEdges[k].EndPt));


                                                    GeometRi.Plane3d GeomPlaneRef = new GeometRi.Plane3d(GetGeometRiPt3DFromPt3D(ListCentalSuppoData[i].ListPrimarySuppo[0].ProjectionPlane.PointOnPlane), GetGeometRiVect3dFromVector3D(normal));

                                                    GeometRi.Segment3d obj2 = GeomSegref.ProjectionTo(GeomPlaneRef) as GeometRi.Segment3d;


                                                    if (obj2 != null)

                                                    {
                                                        AllPoints.Add(GetArrayOfDoubleFromGeometRiPoint3D(obj2.P1));
                                                        AllPoints.Add(GetArrayOfDoubleFromGeometRiPoint3D(obj2.P2));
                                                    }
                                                    else
                                                    {
                                                        GeometRi.Point3d p1 = GeomSegref.P1;
                                                        GeometRi.Point3d p2 = GeomSegref.P2;
                                                        p1 = p1.ProjectionTo(GeomPlaneRef);
                                                        p2 = p2.ProjectionTo(GeomPlaneRef);

                                                        obj2 = new GeometRi.Segment3d(p1, p2);
                                                        AllPoints.Add(GetArrayOfDoubleFromGeometRiPoint3D(obj2.P1));
                                                        AllPoints.Add(GetArrayOfDoubleFromGeometRiPoint3D(obj2.P2));
                                                    }

                                                    Tuple<GeometRi.Point3d, GeometRi.Point3d> result = Calculate.ProjectLineOntoXYPlane(obj2.P1, obj2.P2, normal);

                                                    GeometRi.Segment3d resultLine = new GeometRi.Segment3d(result.Item1, result.Item2);

                                                    EntityData entd = new EntityData();
                                                    entd.Segment = resultLine;
                                                    entd.IDSegment = SecData.SuppoId + "," + "Edge" + edgecount.ToString();
                                                    LinesData.Add(entd);

                                                    //for Dim boundingBox
                                                    AllSeg.Add(resultLine);

                                                    edgecount++;
                                                }
                                                //for Dim boundingBox
                                                AllParts.Add(AllSeg);

                                                faceData.ListAllEdges = Edgelist;
                                                faceData.ListPrimEdges = Edgelist;
                                                faceData.FaceIdLocal = "Face" + facecount.ToString();
                                                PData.ListfaceData.Add(faceData);

                                            }



                                        }
                                        catch (Exception e)
                                        {
                                        }


                                    }
                                    foreach (SupporSpecData SecData in ListCentalSuppoData[i].ListSecondrySuppo)
                                    {
                                        List<FaceData> FacestoProj = new List<FaceData>();
                                        Projection2DData PData = new Projection2DData();

                                        foreach (FaceData FaceLoc in SecData.ListfaceData)
                                        {


                                            if (Math.Abs(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(GetVect3dFromVect3D(FaceLoc.FaceNormal), normal, new Vector3D(0, 0, 1)))) >= 0 && Math.Abs(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(GetVect3dFromVect3D(FaceLoc.FaceNormal), normal, new Vector3D(0, 0, 1)))) < 89 || HasOpenCircularEdge(FaceLoc))
                                            {
                                                FacestoProj.Add(FaceLoc);

                                            }
                                        }
                                        try
                                        {
                                            List<double[]> AllPoints = new List<double[]>();//= new double[][];


                                            for (int j = 0; j < FacestoProj.Count; j++)
                                            {
                                                FaceData faceData = new FaceData();

                                                List<Edgeinfo> Edgelist = new List<Edgeinfo>();

                                                for (int k = 0; k < FacestoProj[j].ListAllEdges.Count; k++)
                                                {
                                                    Edgeinfo EdgeData = new Edgeinfo();


                                                    //for calculating dist of edge
                                                    EdgeData = FacestoProj[j].ListAllEdges[k];
                                                    EdgeData.EdgeId = SecData.SuppoId + "," + "Edge" + edgecount.ToString();

                                                    Edgelist.Add(EdgeData);

                                                    GeometRi.Segment3d GeomSegref = new GeometRi.Segment3d(GetGeometRiPt3DFromPt3D(FacestoProj[j].ListAllEdges[k].StPt), GetGeometRiPt3DFromPt3D(FacestoProj[j].ListAllEdges[k].EndPt));


                                                    GeometRi.Plane3d GeomPlaneRef = new GeometRi.Plane3d(GetGeometRiPt3DFromPt3D(primorder.First().ProjectionPlane.PointOnPlane), GetGeometRiVect3dFromVector3D(normal));

                                                    GeometRi.Segment3d obj2 = GeomSegref.ProjectionTo(GeomPlaneRef) as GeometRi.Segment3d;


                                                    if (obj2 != null)

                                                    {
                                                        AllPoints.Add(GetArrayOfDoubleFromGeometRiPoint3D(obj2.P1));
                                                        AllPoints.Add(GetArrayOfDoubleFromGeometRiPoint3D(obj2.P2));
                                                    }
                                                    else
                                                    {
                                                        GeometRi.Point3d p1 = GeomSegref.P1;
                                                        GeometRi.Point3d p2 = GeomSegref.P2;
                                                        p1 = p1.ProjectionTo(GeomPlaneRef);
                                                        p2 = p2.ProjectionTo(GeomPlaneRef);

                                                        obj2 = new GeometRi.Segment3d(p1, p2);
                                                        AllPoints.Add(GetArrayOfDoubleFromGeometRiPoint3D(obj2.P1));
                                                        AllPoints.Add(GetArrayOfDoubleFromGeometRiPoint3D(obj2.P2));
                                                    }

                                                    Tuple<GeometRi.Point3d, GeometRi.Point3d> result = Calculate.ProjectLineOntoXYPlane(obj2.P1, obj2.P2, normal);

                                                    GeometRi.Segment3d resultLine = new GeometRi.Segment3d(result.Item1, result.Item2);

                                                    EntityData entd = new EntityData();
                                                    entd.Segment = resultLine;
                                                    entd.IDSegment = SecData.SuppoId + "," + "Edge" + edgecount.ToString();
                                                    LinesData.Add(entd);

                                                    //for Dim boundingBox
                                                    AllSeg.Add(resultLine);

                                                    edgecount++;
                                                }
                                                //for Dim boundingBox
                                                AllParts.Add(AllSeg);

                                                faceData.ListAllEdges = Edgelist;
                                                faceData.FaceIdLocal = "Face" + facecount.ToString();
                                                PData.ListfaceData.Add(faceData);

                                            }



                                        }
                                        catch (Exception e)
                                        {
                                        }


                                    }
                                    foreach (SupporSpecData SecData in ListCentalSuppoData[i].ListConcreteData)
                                    {
                                        List<FaceData> FacestoProj = new List<FaceData>();
                                        Projection2DData PData = new Projection2DData();

                                        foreach (FaceData FaceLoc in SecData.ListfaceData)
                                        {


                                            if (Math.Abs(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(GetVect3dFromVect3D(FaceLoc.FaceNormal), normal, new Vector3D(0, 0, 1)))) >= 0 && Math.Abs(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(GetVect3dFromVect3D(FaceLoc.FaceNormal), normal, new Vector3D(0, 0, 1)))) < 89)
                                            {
                                                FacestoProj.Add(FaceLoc);

                                                //primorder.First().ProjectionPlane.Normal

                                            }
                                        }
                                        try
                                        {
                                            List<double[]> AllPoints = new List<double[]>();//= new double[][];


                                            for (int j = 0; j < FacestoProj.Count; j++)
                                            {
                                                FaceData faceData = new FaceData();

                                                List<Edgeinfo> Edgelist = new List<Edgeinfo>();
                                                for (int k = 0; k < FacestoProj[j].ListAllEdges.Count; k++)
                                                {
                                                    Edgeinfo EdgeData = new Edgeinfo();


                                                    //for calculating dist of edge
                                                    EdgeData = FacestoProj[j].ListAllEdges[k];
                                                    EdgeData.EdgeId = "Edge" + edgecount.ToString();

                                                    Edgelist.Add(EdgeData);

                                                    GeometRi.Segment3d GeomSegref = new GeometRi.Segment3d(GetGeometRiPt3DFromPt3D(FacestoProj[j].ListAllEdges[k].StPt), GetGeometRiPt3DFromPt3D(FacestoProj[j].ListAllEdges[k].EndPt));

                                                    GeometRi.Plane3d GeomPlaneRef = new GeometRi.Plane3d(GetGeometRiPt3DFromPt3D(primorder.First().ProjectionPlane.PointOnPlane), GetGeometRiVect3dFromVector3D(normal));



                                                    GeometRi.Segment3d obj2 = GeomSegref.ProjectionTo(GeomPlaneRef) as GeometRi.Segment3d;


                                                    if (obj2 != null)

                                                    {
                                                        AllPoints.Add(GetArrayOfDoubleFromGeometRiPoint3D(obj2.P1));
                                                        AllPoints.Add(GetArrayOfDoubleFromGeometRiPoint3D(obj2.P2));
                                                    }
                                                    else
                                                    {
                                                        GeometRi.Point3d p1 = GeomSegref.P1;
                                                        GeometRi.Point3d p2 = GeomSegref.P2;
                                                        p1 = p1.ProjectionTo(GeomPlaneRef);
                                                        p2 = p2.ProjectionTo(GeomPlaneRef);

                                                        obj2 = new GeometRi.Segment3d(p1, p2);
                                                        AllPoints.Add(GetArrayOfDoubleFromGeometRiPoint3D(obj2.P1));
                                                        AllPoints.Add(GetArrayOfDoubleFromGeometRiPoint3D(obj2.P2));
                                                    }

                                                    Tuple<GeometRi.Point3d, GeometRi.Point3d> result = Calculate.ProjectLineOntoXYPlane(obj2.P1, obj2.P2, normal);

                                                    GeometRi.Segment3d resultLine = new GeometRi.Segment3d(result.Item1, result.Item2);




                                                    //LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(result.Item1.X, result.Item1.Y, result.Item1.Z), new Point3d(result.Item2.X, result.Item2.Y, result.Item2.Z), MyCol.White);


                                                    EntityData entd = new EntityData();
                                                    entd.Segment = resultLine;
                                                    entd.IDSegment = SecData.SuppoId + "," + "Edge" + edgecount.ToString();
                                                    LinesData.Add(entd);

                                                    //for Dim boundingBox
                                                    AllSeg.Add(resultLine);

                                                    edgecount++;

                                                }
                                                //for Dim boundingBox
                                                AllParts.Add(AllSeg);

                                                faceData.ListAllEdges = Edgelist;
                                                faceData.FaceIdLocal = "Face" + facecount.ToString();
                                                PData.ListfaceData.Add(faceData);

                                            }
                                        }
                                        catch (Exception e)
                                        {
                                        }
                                    }
                                    var AllLiness = Calculate.FitLinesInRectangle(LinesData, 11000, 7000, tracex + 3 * (boxlen / 4), spaceY - boxht / 2);

                                    List<GeometRi.Segment3d> allsecseg = new List<GeometRi.Segment3d>();
                                    foreach (var seg in AllLiness)
                                    {
                                        if (seg.IDSegment.ToLower().Contains("s"))
                                        {
                                            allsecseg.Add(seg.Segment);
                                        }
                                    }

                                    double minX = double.MaxValue;
                                    double minY = double.MaxValue;
                                    double maxX = double.MinValue;
                                    double maxY = double.MinValue;

                                    var maxminpts = Calculate.FindMinMaxCoordinates(allsecseg, out minX, out minY, out maxX, out maxY);


                                    Dictionary<string, List<GeometRi.Segment3d>> DicIdEdgeList = new Dictionary<string, List<GeometRi.Segment3d>>();

                                    foreach (var ent in AllLiness)
                                    {
                                        if (ent.IDSegment.Substring(0, 1).ToLower().Equals("s"))
                                        {
                                            List<GeometRi.Segment3d> EdgeList = new List<GeometRi.Segment3d>();

                                            if (DicIdEdgeList.ContainsKey(ent.IDSegment.Split(',')[0]))
                                            {
                                                EdgeList = DicIdEdgeList[ent.IDSegment.Split(',')[0]];
                                                EdgeList.Add(ent.Segment);

                                                DicIdEdgeList[ent.IDSegment.Split(',')[0]] = EdgeList;

                                            }
                                            else
                                            {
                                                EdgeList.Add(ent.Segment);

                                                DicIdEdgeList[ent.IDSegment.Split(',')[0]] = EdgeList;
                                            }
                                        }
                                    }

                                    List<GeometRi.Segment3d> ListSegs = new List<GeometRi.Segment3d>();

                                    foreach (var SecSupo in DicIdEdgeList)
                                    {
                                        if (SecSupo.Value.Count < 15)
                                        {
                                            foreach (var line in SecSupo.Value)
                                            {
                                                LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(line.P1.X, line.P1.Y, line.P1.Z), new Point3d(line.P2.X, line.P2.Y, line.P2.Z), MyCol.LightBlue);
                                            }
                                        }
                                        else
                                        {

                                            if (Math.Abs(Math.Round(GetRotationFromVec(GetPartbyId(SecSupo.Key, ListCentalSuppoData[i].ListSecondrySuppo).Directionvec).XinDegree)) > 0 && Math.Abs(Math.Round(GetRotationFromVec(GetPartbyId(SecSupo.Key, ListCentalSuppoData[i].ListSecondrySuppo).Directionvec).XinDegree)) % 90 != 0)
                                            {
                                                foreach (var line in SecSupo.Value)
                                                {
                                                    LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(line.P1.X, line.P1.Y, line.P1.Z), new Point3d(line.P2.X, line.P2.Y, line.P2.Z), MyCol.LightBlue);
                                                }
                                            }
                                            else
                                            {
                                                List<GeometRi.Segment3d> ListSegOutSide = new List<GeometRi.Segment3d>();
                                                List<GeometRi.Segment3d> ListSegInSide = new List<GeometRi.Segment3d>();
                                                (ListSegInSide, ListSegOutSide) = Calculate.CalculateEdges(SecSupo.Value);
                                                ListSegs.AddRange(ListSegInSide);
                                                ListSegs.AddRange(ListSegOutSide);
                                            }
                                        }
                                    }


                                    foreach (var ent in AllLiness)
                                    {
                                        var line = ent.Segment;
                                        if (ent.IDSegment.ToLower().Contains("c"))
                                        {
                                            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(line.P1.X, line.P1.Y, line.P1.Z), new Point3d(line.P2.X, line.P2.Y, line.P2.Z), MyCol.Gray);
                                        }
                                        else if (ent.IDSegment.ToLower().Contains("s"))
                                        {
                                            // LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(line.P1.X, line.P1.Y, line.P1.Z), new Point3d(line.P2.X, line.P2.Y, line.P2.Z), MyCol.LightBlue);
                                        }
                                        else
                                        {
                                            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(line.P1.X, line.P1.Y, line.P1.Z), new Point3d(line.P2.X, line.P2.Y, line.P2.Z), MyCol.Green);
                                        }

                                        // PData.AfterListfaceData.Add();
                                    }


                                    foreach (var line in ListSegs)
                                    {
                                        LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(line.P1.X, line.P1.Y, line.P1.Z), new Point3d(line.P2.X, line.P2.Y, line.P2.Z), MyCol.LightBlue);
                                    }

                                    Dictionary<string, List<EntityData>> PrimPart2D = new Dictionary<string, List<EntityData>>();
                                    for (int h = 0; h < ListCentalSuppoData[i].ListPrimarySuppo.Count; h++)
                                    {
                                        List<EntityData> PrimParts = new List<EntityData>();
                                        for (int g = 0; g < AllLiness.Count; g++)
                                        {
                                            if (AllLiness[g].IDSegment.Contains(ListCentalSuppoData[i].ListPrimarySuppo[h].SuppoId))
                                            {
                                                PrimParts.Add(AllLiness[g]);
                                            }
                                        }
                                        PrimPart2D[ListCentalSuppoData[i].ListPrimarySuppo[h].SuppoId] = PrimParts;
                                    }


                                    Dictionary<string, List<EntityData>> SecPart2D = new Dictionary<string, List<EntityData>>();
                                    for (int h = 0; h < ListCentalSuppoData[i].ListSecondrySuppo.Count; h++)
                                    {
                                        List<EntityData> SecParts = new List<EntityData>();
                                        for (int g = 0; g < AllLiness.Count; g++)
                                        {
                                            if (AllLiness[g].IDSegment.Contains(ListCentalSuppoData[i].ListSecondrySuppo[h].SuppoId))
                                            {
                                                SecParts.Add(AllLiness[g]);
                                            }
                                        }
                                        SecPart2D[ListCentalSuppoData[i].ListSecondrySuppo[h].SuppoId] = SecParts;
                                    }



                                    Dictionary<string, List<EntityData>> ConcretePart2D = new Dictionary<string, List<EntityData>>();
                                    for (int h = 0; h < ListCentalSuppoData[i].ListConcreteData.Count; h++)
                                    {
                                        List<EntityData> ConcreteParts = new List<EntityData>();
                                        for (int g = 0; g < AllLiness.Count; g++)
                                        {
                                            if (AllLiness[g].IDSegment.Contains(ListCentalSuppoData[i].ListConcreteData[h].SuppoId))
                                            {
                                                ConcreteParts.Add(AllLiness[g]);

                                            }
                                        }
                                        ConcretePart2D[ListCentalSuppoData[i].ListConcreteData[h].SuppoId] = ConcreteParts;
                                    }



                                    var concrete_sort = ListCentalSuppoData[i].ListConcreteData.OrderBy(e => e.Boundingboxmin.Z);

                                    if (concrete_sort.Count() > 0)
                                    {
                                        ConcretDimension(AcadBlockTableRecord, AcadTransaction, AcadDatabase, concrete_sort.ElementAt(0), ConcretePart2D, i);
                                        FJWELD(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ConcretePart2D, i);
                                    }

                                    List<SupporSpecData> SecFilter = new List<SupporSpecData>();
                                    foreach (SupporSpecData sec in ListCentalSuppoData[i].ListSecondrySuppo)
                                    {
                                        if (sec.IsGussetplate == false && sec.IsAnchor == false)
                                        {
                                            SecFilter.Add(sec);
                                        }
                                    }

                                    //Secondary Dimensioning
                                    var secondary_sort = SecFilter.OrderByDescending(e => e.Boundingboxmin.Z);
                                    if (secondary_sort.Count() > 0)
                                    {
                                        SecondaryDimension(AcadBlockTableRecord, AcadTransaction, AcadDatabase, secondary_sort, SecPart2D, AllParts, minX, maxX, i, PrimPart2D);
                                    }

                                    //for collecting prim faces edges
                                    Dictionary<string, List<GeometRi.Segment3d>> PrimFaceEdges = new Dictionary<string, List<GeometRi.Segment3d>>();

                                    foreach (SupporSpecData prim in primorder)
                                    {
                                        List<GeometRi.Segment3d> primbyID = new List<GeometRi.Segment3d>();
                                        foreach (var priment in AllLiness)
                                        {
                                            if (priment.IDSegment.Contains(prim.SuppoId))
                                            {
                                                primbyID.Add(priment.Segment);
                                            }
                                        }
                                        PrimFaceEdges.Add(prim.SuppoId, primbyID);
                                    }
                                    ///////////////
                                    PlacePrim(AcadBlockTableRecord, AcadTransaction, AcadDatabase, PrimFaceEdges, secondary_sort, SecPart2D, i);


                                    //support name and quantity
                                    CreateSupportName(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, i);

                                    tracex += boxlen; Created_TAG.Add(ListCentalSuppoData[i].Name);
                                }
                            }
                            else
                            {
                                SupportNotCreated.Add(ListCentalSuppoData[i].Name);
                            }
                        }


                    }
                    else if (ListCentalSuppoData[i].ListSecondrySuppo.Count > 0)
                    {
                        LinesData.Clear();
                        ProjectedSecEntity.Clear();
                        DimesionBoxData.Clear();
                        var primorder = ListCentalSuppoData[i].ListSecondrySuppo.OrderByDescending(e => e.Centroid.Z);
                        if (normal == null)
                        {
                            SupportNotCreated.Add(ListCentalSuppoData[i].Name);
                        }
                        else
                        {
                            var normvec = normal;
                            if (!(Math.Abs(normvec.Z).Equals(1)))
                            {
                                if (primorder.First().ProjectionPlane != null && normal != null)
                                {
                                    //for Dim boundingBox
                                    List<List<GeometRi.Segment3d>> AllParts = new List<List<GeometRi.Segment3d>>();

                                    //for Dim boundingBox
                                    List<GeometRi.Segment3d> AllSeg = new List<GeometRi.Segment3d>();
                                    foreach (SupporSpecData SecData in ListCentalSuppoData[i].ListSecondrySuppo)
                                    {
                                        List<FaceData> FacestoProj = new List<FaceData>();
                                        Projection2DData PData = new Projection2DData();

                                        foreach (FaceData FaceLoc in SecData.ListfaceData)
                                        {


                                            if (Math.Abs(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(GetVect3dFromVect3D(FaceLoc.FaceNormal), normal, new Vector3D(0, 0, 1)))) >= 0 && Math.Abs(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(GetVect3dFromVect3D(FaceLoc.FaceNormal), normal, new Vector3D(0, 0, 1)))) < 89 || HasOpenCircularEdge(FaceLoc))
                                            {
                                                FacestoProj.Add(FaceLoc);

                                            }
                                        }
                                        try
                                        {
                                            List<double[]> AllPoints = new List<double[]>();//= new double[][];


                                            for (int j = 0; j < FacestoProj.Count; j++)
                                            {
                                                FaceData faceData = new FaceData();

                                                List<Edgeinfo> Edgelist = new List<Edgeinfo>();

                                                for (int k = 0; k < FacestoProj[j].ListAllEdges.Count; k++)
                                                {
                                                    Edgeinfo EdgeData = new Edgeinfo();


                                                    //for calculating dist of edge
                                                    EdgeData = FacestoProj[j].ListAllEdges[k];
                                                    EdgeData.EdgeId = SecData.SuppoId + "," + "Edge" + edgecount.ToString();

                                                    Edgelist.Add(EdgeData);

                                                    GeometRi.Segment3d GeomSegref = new GeometRi.Segment3d(GetGeometRiPt3DFromPt3D(FacestoProj[j].ListAllEdges[k].StPt), GetGeometRiPt3DFromPt3D(FacestoProj[j].ListAllEdges[k].EndPt));


                                                    GeometRi.Plane3d GeomPlaneRef = new GeometRi.Plane3d(GetGeometRiPt3DFromPt3D(primorder.First().ProjectionPlane.PointOnPlane), GetGeometRiVect3dFromVector3D(normal));

                                                    GeometRi.Segment3d obj2 = GeomSegref.ProjectionTo(GeomPlaneRef) as GeometRi.Segment3d;


                                                    if (obj2 != null)

                                                    {
                                                        AllPoints.Add(GetArrayOfDoubleFromGeometRiPoint3D(obj2.P1));
                                                        AllPoints.Add(GetArrayOfDoubleFromGeometRiPoint3D(obj2.P2));
                                                    }
                                                    else
                                                    {
                                                        GeometRi.Point3d p1 = GeomSegref.P1;
                                                        GeometRi.Point3d p2 = GeomSegref.P2;
                                                        p1 = p1.ProjectionTo(GeomPlaneRef);
                                                        p2 = p2.ProjectionTo(GeomPlaneRef);

                                                        obj2 = new GeometRi.Segment3d(p1, p2);
                                                        AllPoints.Add(GetArrayOfDoubleFromGeometRiPoint3D(obj2.P1));
                                                        AllPoints.Add(GetArrayOfDoubleFromGeometRiPoint3D(obj2.P2));
                                                    }

                                                    Tuple<GeometRi.Point3d, GeometRi.Point3d> result = Calculate.ProjectLineOntoXYPlane(obj2.P1, obj2.P2, normal);

                                                    GeometRi.Segment3d resultLine = new GeometRi.Segment3d(result.Item1, result.Item2);

                                                    EntityData entd = new EntityData();
                                                    entd.Segment = resultLine;
                                                    entd.IDSegment = SecData.SuppoId + "," + "Edge" + edgecount.ToString();
                                                    LinesData.Add(entd);

                                                    //for Dim boundingBox
                                                    AllSeg.Add(resultLine);

                                                    edgecount++;
                                                }
                                                //for Dim boundingBox
                                                AllParts.Add(AllSeg);

                                                faceData.ListAllEdges = Edgelist;
                                                faceData.FaceIdLocal = "Face" + facecount.ToString();
                                                PData.ListfaceData.Add(faceData);

                                            }



                                        }
                                        catch (Exception e)
                                        {
                                        }


                                    }
                                    foreach (SupporSpecData SecData in ListCentalSuppoData[i].ListConcreteData)
                                    {
                                        List<FaceData> FacestoProj = new List<FaceData>();
                                        Projection2DData PData = new Projection2DData();

                                        foreach (FaceData FaceLoc in SecData.ListfaceData)
                                        {


                                            if (Math.Abs(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(GetVect3dFromVect3D(FaceLoc.FaceNormal), normal, new Vector3D(0, 0, 1)))) >= 0 && Math.Abs(Calculate.ConvertRadiansToDegrees(Calculate.GetSignedRotation(GetVect3dFromVect3D(FaceLoc.FaceNormal), normal, new Vector3D(0, 0, 1)))) < 89)
                                            {
                                                FacestoProj.Add(FaceLoc);

                                                //primorder.First().ProjectionPlane.Normal

                                            }
                                        }
                                        try
                                        {
                                            List<double[]> AllPoints = new List<double[]>();//= new double[][];


                                            for (int j = 0; j < FacestoProj.Count; j++)
                                            {
                                                FaceData faceData = new FaceData();

                                                List<Edgeinfo> Edgelist = new List<Edgeinfo>();
                                                for (int k = 0; k < FacestoProj[j].ListAllEdges.Count; k++)
                                                {
                                                    Edgeinfo EdgeData = new Edgeinfo();


                                                    //for calculating dist of edge
                                                    EdgeData = FacestoProj[j].ListAllEdges[k];
                                                    EdgeData.EdgeId = "Edge" + edgecount.ToString();

                                                    Edgelist.Add(EdgeData);

                                                    GeometRi.Segment3d GeomSegref = new GeometRi.Segment3d(GetGeometRiPt3DFromPt3D(FacestoProj[j].ListAllEdges[k].StPt), GetGeometRiPt3DFromPt3D(FacestoProj[j].ListAllEdges[k].EndPt));

                                                    GeometRi.Plane3d GeomPlaneRef = new GeometRi.Plane3d(GetGeometRiPt3DFromPt3D(primorder.First().ProjectionPlane.PointOnPlane), GetGeometRiVect3dFromVector3D(normal));



                                                    GeometRi.Segment3d obj2 = GeomSegref.ProjectionTo(GeomPlaneRef) as GeometRi.Segment3d;


                                                    if (obj2 != null)

                                                    {
                                                        AllPoints.Add(GetArrayOfDoubleFromGeometRiPoint3D(obj2.P1));
                                                        AllPoints.Add(GetArrayOfDoubleFromGeometRiPoint3D(obj2.P2));
                                                    }
                                                    else
                                                    {
                                                        GeometRi.Point3d p1 = GeomSegref.P1;
                                                        GeometRi.Point3d p2 = GeomSegref.P2;
                                                        p1 = p1.ProjectionTo(GeomPlaneRef);
                                                        p2 = p2.ProjectionTo(GeomPlaneRef);

                                                        obj2 = new GeometRi.Segment3d(p1, p2);
                                                        AllPoints.Add(GetArrayOfDoubleFromGeometRiPoint3D(obj2.P1));
                                                        AllPoints.Add(GetArrayOfDoubleFromGeometRiPoint3D(obj2.P2));
                                                    }

                                                    Tuple<GeometRi.Point3d, GeometRi.Point3d> result = Calculate.ProjectLineOntoXYPlane(obj2.P1, obj2.P2, normal);

                                                    GeometRi.Segment3d resultLine = new GeometRi.Segment3d(result.Item1, result.Item2);




                                                    //LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(result.Item1.X, result.Item1.Y, result.Item1.Z), new Point3d(result.Item2.X, result.Item2.Y, result.Item2.Z), MyCol.White);


                                                    EntityData entd = new EntityData();
                                                    entd.Segment = resultLine;
                                                    entd.IDSegment = SecData.SuppoId + "," + "Edge" + edgecount.ToString();
                                                    LinesData.Add(entd);

                                                    //for Dim boundingBox
                                                    AllSeg.Add(resultLine);

                                                    edgecount++;

                                                }
                                                //for Dim boundingBox
                                                AllParts.Add(AllSeg);

                                                faceData.ListAllEdges = Edgelist;
                                                faceData.FaceIdLocal = "Face" + facecount.ToString();
                                                PData.ListfaceData.Add(faceData);

                                            }
                                        }
                                        catch (Exception e)
                                        {
                                        }
                                    }
                                    var AllLiness = Calculate.FitLinesInRectangle(LinesData, 11000, 7000, tracex + 3 * (boxlen / 4), spaceY - boxht / 2);

                                    Dictionary<string, List<GeometRi.Segment3d>> DicIdEdgeList = new Dictionary<string, List<GeometRi.Segment3d>>();

                                    foreach (var ent in AllLiness)
                                    {
                                        if (ent.IDSegment.Substring(0, 1).ToLower().Equals("s"))
                                        {
                                            List<GeometRi.Segment3d> EdgeList = new List<GeometRi.Segment3d>();

                                            if (DicIdEdgeList.ContainsKey(ent.IDSegment.Split(',')[0]))
                                            {
                                                EdgeList = DicIdEdgeList[ent.IDSegment.Split(',')[0]];
                                                EdgeList.Add(ent.Segment);

                                                DicIdEdgeList[ent.IDSegment.Split(',')[0]] = EdgeList;

                                            }
                                            else
                                            {
                                                EdgeList.Add(ent.Segment);

                                                DicIdEdgeList[ent.IDSegment.Split(',')[0]] = EdgeList;
                                            }
                                        }
                                    }

                                    List<GeometRi.Segment3d> ListSegs = new List<GeometRi.Segment3d>();

                                    foreach (var SecSupo in DicIdEdgeList)
                                    {
                                        if (SecSupo.Value.Count < 15)
                                        {
                                            foreach (var line in SecSupo.Value)
                                            {
                                                LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(line.P1.X, line.P1.Y, line.P1.Z), new Point3d(line.P2.X, line.P2.Y, line.P2.Z), MyCol.LightBlue);
                                            }
                                        }
                                        else
                                        {

                                            if (Math.Abs(Math.Round(GetRotationFromVec(GetPartbyId(SecSupo.Key, ListCentalSuppoData[i].ListSecondrySuppo).Directionvec).XinDegree)) > 0 && Math.Abs(Math.Round(GetRotationFromVec(GetPartbyId(SecSupo.Key, ListCentalSuppoData[i].ListSecondrySuppo).Directionvec).XinDegree)) % 90 != 0)
                                            {
                                                foreach (var line in SecSupo.Value)
                                                {
                                                    LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(line.P1.X, line.P1.Y, line.P1.Z), new Point3d(line.P2.X, line.P2.Y, line.P2.Z), MyCol.LightBlue);
                                                }
                                            }
                                            else
                                            {
                                                List<GeometRi.Segment3d> ListSegOutSide = new List<GeometRi.Segment3d>();
                                                List<GeometRi.Segment3d> ListSegInSide = new List<GeometRi.Segment3d>();
                                                (ListSegInSide, ListSegOutSide) = Calculate.CalculateEdges(SecSupo.Value);
                                                ListSegs.AddRange(ListSegInSide);
                                                ListSegs.AddRange(ListSegOutSide);
                                            }
                                        }
                                    }


                                    foreach (var ent in AllLiness)
                                    {
                                        var line = ent.Segment;
                                        if (ent.IDSegment.ToLower().Contains("c"))
                                        {
                                            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(line.P1.X, line.P1.Y, line.P1.Z), new Point3d(line.P2.X, line.P2.Y, line.P2.Z), MyCol.Gray);
                                        }
                                        else if (ent.IDSegment.ToLower().Contains("s"))
                                        {
                                            // LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(line.P1.X, line.P1.Y, line.P1.Z), new Point3d(line.P2.X, line.P2.Y, line.P2.Z), MyCol.LightBlue);
                                        }
                                        else
                                        {
                                            LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(line.P1.X, line.P1.Y, line.P1.Z), new Point3d(line.P2.X, line.P2.Y, line.P2.Z), MyCol.Green);
                                        }

                                        // PData.AfterListfaceData.Add();
                                    }


                                    List<GeometRi.Segment3d> allsecseg = new List<GeometRi.Segment3d>();
                                    foreach (var seg in AllLiness)
                                    {
                                        if (seg.IDSegment.ToLower().Contains("s"))
                                        {
                                            allsecseg.Add(seg.Segment);
                                        }
                                    }

                                    double minX = double.MaxValue;
                                    double minY = double.MaxValue;
                                    double maxX = double.MinValue;
                                    double maxY = double.MinValue;

                                    var maxminpts = Calculate.FindMinMaxCoordinates(allsecseg, out minX, out minY, out maxX, out maxY);


                                    foreach (var line in ListSegs)
                                    {
                                        LineDraw(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(line.P1.X, line.P1.Y, line.P1.Z), new Point3d(line.P2.X, line.P2.Y, line.P2.Z), MyCol.LightBlue);
                                    }

                                    Dictionary<string, List<EntityData>> SecPart2D = new Dictionary<string, List<EntityData>>();
                                    for (int h = 0; h < ListCentalSuppoData[i].ListSecondrySuppo.Count; h++)
                                    {
                                        List<EntityData> SecParts = new List<EntityData>();
                                        for (int g = 0; g < AllLiness.Count; g++)
                                        {
                                            if (AllLiness[g].IDSegment.Contains(ListCentalSuppoData[i].ListSecondrySuppo[h].SuppoId))
                                            {
                                                SecParts.Add(AllLiness[g]);
                                            }
                                        }
                                        SecPart2D[ListCentalSuppoData[i].ListSecondrySuppo[h].SuppoId] = SecParts;
                                    }

                                    //Secondary Dimensioning
                                    var secondary_sort = ListCentalSuppoData[i].ListSecondrySuppo.OrderBy(e => e.Boundingboxmin.Z);
                                    if (secondary_sort.Count() > 0)
                                    {
                                        SecondaryDimension(AcadBlockTableRecord, AcadTransaction, AcadDatabase, secondary_sort, SecPart2D, AllParts, minX, maxX, i);
                                    }

                                    Dictionary<string, List<EntityData>> ConcretePart2D = new Dictionary<string, List<EntityData>>();
                                    for (int h = 0; h < ListCentalSuppoData[i].ListConcreteData.Count; h++)
                                    {
                                        List<EntityData> ConcreteParts = new List<EntityData>();
                                        for (int g = 0; g < AllLiness.Count; g++)
                                        {
                                            if (AllLiness[g].IDSegment.Contains(ListCentalSuppoData[i].ListConcreteData[h].SuppoId))
                                            {
                                                ConcreteParts.Add(AllLiness[g]);

                                            }
                                        }
                                        ConcretePart2D[ListCentalSuppoData[i].ListConcreteData[h].SuppoId] = ConcreteParts;
                                    }

                                    var concrete_sort = ListCentalSuppoData[i].ListConcreteData.OrderBy(e => e.Boundingboxmin.Z);

                                    if (concrete_sort.Count() > 0)
                                    {
                                        ConcretDimension(AcadBlockTableRecord, AcadTransaction, AcadDatabase, concrete_sort.ElementAt(0), ConcretePart2D, i);
                                        FJWELD(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ConcretePart2D, i);
                                    }
                                    //support name and quantity
                                    CreateSupportName(AcadBlockTableRecord, AcadTransaction, AcadDatabase, ref tracex, boxlen, boxht, ref spaceY, i, ListCentalSuppoData[i].Name);

                                    tracex += boxlen; Created_TAG.Add(ListCentalSuppoData[i].Name);
                                }
                            }
                            else
                            {
                                SupportNotCreated.Add(ListCentalSuppoData[i].Name);
                            }
                        }


                    }
                }


            }

        }

        public void FJWELD(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, Dictionary<string, List<EntityData>> ConcretePart2D, int i)
        {

            var conparts = ListCentalSuppoData[i].ListConcreteData.OrderByDescending(e => e.Boundingboxmax.Z);

            foreach (SupporSpecData secpart in ListCentalSuppoData[i].ListSecondrySuppo)
            {
                if (secpart.IsGussetplate)
                {
                    return;
                }
            }
            List<GeometRi.Segment3d> conweld = new List<GeometRi.Segment3d>();
            foreach (var conpart in ConcretePart2D)
            {
                if (conpart.Key == conparts.First().SuppoId)
                {

                    foreach (var seg in conpart.Value)
                    {
                        if ((seg.Segment.P1.X).Equals(seg.Segment.P2.X) && (seg.Segment.P1.Y).Equals(seg.Segment.P2.Y))
                        {
                            continue;
                        }
                        if (seg.IDSegment.Contains(conparts.First().SuppoId))
                        {
                            conweld.Add(seg.Segment);
                        }

                    }
                }
            }
            var topedge = GetTopSegment(conweld);
            if (topedge != null)
            {
                double centX = Math.Max(topedge.P1.X, topedge.P2.X) - (topedge.Length / 4);
                double centY = topedge.P2.Y;

                CopyAndModifyEntities(AcadBlockTableRecord, AcadTransaction, AcadDatabase, "LeaderBlock_Gen_R", centX, centY, topedge.Length / 4);

            }


        }
        bool HasOpenCircularEdge(FaceData FaceLoc)
        {
            foreach (Edgeinfo edgeinfo in FaceLoc.ListAllEdges)
            {
                if (edgeinfo.TypeEdge == EdgeType.OpenCircularEdge)
                {
                    return true;
                }
            }
            return false;
        }

        public GeometRi.Segment3d GetTopSegment(List<GeometRi.Segment3d> segments)
        {
            if (segments == null || segments.Count == 0)
                return null;

            GeometRi.Segment3d topSegment = segments[0];
            double maxY = Math.Min(topSegment.P1.Y, topSegment.P2.Y);

            foreach (var segment in segments)
            {
                double currentMaxY = Math.Min(segment.P1.Y, segment.P2.Y);
                if (currentMaxY > maxY)
                {
                    maxY = currentMaxY;
                    topSegment = segment;
                }
            }

            return topSegment;
        }
        public GeometRi.Segment3d GetBottomSegment(List<GeometRi.Segment3d> segments)
        {
            if (segments == null || segments.Count == 0)
                return null;

            GeometRi.Segment3d bottomSegment = segments[0];
            double minY = Math.Max(bottomSegment.P1.Y, bottomSegment.P2.Y);

            foreach (var segment in segments)
            {
                double currentMinY = Math.Max(segment.P1.Y, segment.P2.Y);
                if (currentMinY < minY)
                {
                    minY = currentMinY;
                    bottomSegment = segment;
                }
            }

            return bottomSegment;
        }

        public GeometRi.Segment3d GetLeftSideSegment(List<GeometRi.Segment3d> segments)
        {
            if (segments == null || segments.Count == 0)
                return null;

            GeometRi.Segment3d leftSegment = segments[0];
            double minX = Math.Max(leftSegment.P1.X, leftSegment.P2.X);

            foreach (var segment in segments)
            {
                double currentMinX = Math.Max(segment.P1.X, segment.P2.X);
                if (currentMinX < minX)
                {
                    minX = currentMinX;
                    leftSegment = segment;
                }
            }

            return leftSegment;
        }

        public GeometRi.Segment3d GetRightSideSegment(List<GeometRi.Segment3d> segments)
        {
            if (segments == null || segments.Count == 0)
                return null;

            GeometRi.Segment3d rightSegment = segments[0];
            double maxX = Math.Min(rightSegment.P1.X, rightSegment.P2.X);

            foreach (var segment in segments)
            {
                double currentMaxX = Math.Min(segment.P1.X, segment.P2.X);
                if (currentMaxX > maxX)
                {
                    maxX = currentMaxX;
                    rightSegment = segment;
                }
            }

            return rightSegment;
        }

        public void SecondaryDimension(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, IOrderedEnumerable<SupporSpecData> secondary_sort, Dictionary<string, List<EntityData>> SecPart2D, List<List<GeometRi.Segment3d>> AllParts, double dimminX, double dimmaxX, int i, Dictionary<string, List<EntityData>> PrimPart2D = null)
        {
            var longsec = SecPart2D.Where(e => e.Key == secondary_sort.Where(z => z.BoxData.Z == secondary_sort.Max(m => m.BoxData.Z)).First().SuppoId);

            List<GeometRi.Segment3d> ListLongSecEdges = new List<GeometRi.Segment3d>();
            foreach (var edges in longsec.First().Value)
            {
                ListLongSecEdges.Add(edges.Segment);
            }
            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double maxX = double.MinValue;
            double maxY = double.MinValue;
            double[] MinMaxLOngSecCoord = Calculate.FindMinMaxCoordinates(ListLongSecEdges, out minX, out minY, out maxX, out maxY);

            List<SupporSpecData> longsec3D = new List<SupporSpecData>();
            Angles angle = new Angles();
            foreach (SupporSpecData longsecp in secondary_sort)
            {
                angle = GetRotationFromVec(longsecp.Directionvec);
                if (Math.Abs(Math.Round(angle.XinDegree)).Equals(0) || Math.Abs(Math.Round(angle.XinDegree)).Equals(180))
                {
                    if (longsecp.SupportName != null && longsecp.SupportName.ToLower().Contains("plate"))
                    {
                        continue;
                    }
                    if (longsecp.BoxData.Z > longsecp.BoxData.X && longsecp.BoxData.Z > longsecp.BoxData.Y)
                    {
                        longsec3D.Add(longsecp);
                    }
                }
            }


            //vertical dimension
            foreach (SupporSpecData longver in longsec3D)
            {
                angle = GetRotationFromVec(longver.Directionvec);

                if (longver.SupportName != null && longver.SupportName.ToLower().Contains("plate"))
                {
                    continue;
                }

                //verti
                if (Math.Abs(Math.Round(angle.XinDegree)).Equals(0) || Math.Abs(Math.Round(angle.XinDegree)).Equals(180))
                {
                    foreach (var part in SecPart2D)
                    {
                        if (part.Key == longver.SuppoId)
                        {
                            List<GeometRi.Segment3d> EdgesSec = new List<GeometRi.Segment3d>();
                            foreach (var edges in part.Value)
                            {
                                if ((edges.Segment.P1.X).Equals(edges.Segment.P2.X) && (edges.Segment.P1.Y).Equals(edges.Segment.P2.Y))
                                {
                                    continue;
                                }
                                EdgesSec.Add(edges.Segment);

                            }

                            if (EdgesSec.Count() > 0)
                            {
                                var topedge = GetTopSegment(EdgesSec);
                                var botedge = GetBottomSegment(EdgesSec);
                                var leftedge = GetLeftSideSegment(EdgesSec);
                                var rightedge = GetRightSideSegment(EdgesSec);
                                var sectionsize = 0.0;
                                try
                                {
                                    sectionsize = CSECTIONSIZE(longver.Size);
                                }
                                catch (Exception)
                                {
                                }
                                List<GeometRi.Segment3d> Checkpart = new List<GeometRi.Segment3d>();
                                double Xoffset = 2000;
                                Checkpart = TextBox(new Point3d(Math.Max(topedge.P1.X, topedge.P2.X) + Xoffset, topedge.P1.Y, 0));
                                //while (!Calculate.BoundingBoxIntersectsList(Checkpart, AllParts))
                                //{
                                //    Xoffset += 200;
                                //}

                                Xoffset = 1000;
                                Checkpart = TextBox(new Point3d(Math.Max(rightedge.P1.X, rightedge.P2.X) + Xoffset, (rightedge.P1.Y + rightedge.P2.Y) / 2, 0));
                                //while (!Calculate.BoundingBoxIntersectsList(Checkpart, AllParts))
                                //{
                                //    Xoffset += 200;
                                //}

                                if (longver.Size != null && (longver.Size.ToLower().Contains("angle") || longver.Size.ToLower().Contains("isa")))
                                {
                                    string Size = GettheISA(longver);

                                    ISMCTAG(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(Math.Min(leftedge.P1.X, leftedge.P2.X), (leftedge.P1.Y + leftedge.P2.Y) / 2, 0), "ll", sectionsize, Xoffset, ISASIZE: Size);
                                    AllParts.Add(Checkpart);
                                }
                                else
                                {
                                    ISMCTAG(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(Math.Min(leftedge.P1.X, leftedge.P2.X), (leftedge.P1.Y + leftedge.P2.Y) / 2, 0), "l", sectionsize, Xoffset);
                                }

                                //if (PrimPart2D != null)
                                //{
                                //    PrimDimensionWithSec(longver, PrimPart2D, topedge, leftedge, i);
                                //}
                                //else
                                //{
                                double l_dist_frm_centre = Math.Round(GetDist(new Point3d(longver.StPt), new Point3d(longver.EndPt)));
                                CreateAlighDimen(AcadBlockTableRecord, AcadTransaction, new Point3d(rightedge.P1.X, Math.Max(rightedge.P1.Y, rightedge.P2.Y), 0), new Point3d(rightedge.P1.X, Math.Min(rightedge.P1.Y, rightedge.P2.Y), 0), l_dist_frm_centre.ToString(), -((dimmaxX - dimminX) / 2 + (topedge.Length / 2) + 50));
                                //}
                            }
                        }
                    }
                }
                //angled
                else if (Math.Abs(Math.Round(angle.XinDegree)) > 0 || Math.Abs(Math.Round(angle.XinDegree)) < 90 || Math.Abs(Math.Round(angle.XinDegree)) > 90 || Math.Abs(Math.Round(angle.XinDegree)) < 180)
                {
                    foreach (var part in SecPart2D)
                    {
                        if (part.Key == longver.SuppoId)
                        {
                            List<GeometRi.Segment3d> EdgesSec = new List<GeometRi.Segment3d>();
                            foreach (var edges in part.Value)
                            {
                                if ((edges.Segment.P1.X).Equals(edges.Segment.P2.X) && (edges.Segment.P1.Y).Equals(edges.Segment.P2.Y))
                                {
                                    continue;
                                }
                                EdgesSec.Add(edges.Segment);
                            }

                            if (EdgesSec.Count() > 0)
                            {
                                var topedge = GetTopSegment(EdgesSec);
                                var botedge = GetBottomSegment(EdgesSec);
                                var leftedge = GetLeftSideSegment(EdgesSec);
                                var rightedge = GetRightSideSegment(EdgesSec);
                                var sectionsize = 0.0;
                                try
                                {
                                    sectionsize = CSECTIONSIZE(longver.Size);
                                }
                                catch (Exception)
                                {
                                }
                                if (Math.Max(topedge.P1.X, topedge.P2.X) > Math.Max(botedge.P1.X, botedge.P2.X))
                                {

                                }
                                List<GeometRi.Segment3d> Checkpart = new List<GeometRi.Segment3d>();
                                double Xoffset = 2000;
                                Checkpart = TextBox(new Point3d(Math.Max(topedge.P1.X, topedge.P2.X) + Xoffset, topedge.P1.Y, 0));
                                //while (!Calculate.BoundingBoxIntersectsList(Checkpart, AllParts))
                                //{
                                //    Xoffset += 200;
                                //}

                                Xoffset = 1000;
                                Checkpart = TextBox(new Point3d(Math.Max(rightedge.P1.X, rightedge.P2.X) + Xoffset, (rightedge.P1.Y + rightedge.P2.Y) / 2, 0));
                                //while (!Calculate.BoundingBoxIntersectsList(Checkpart, AllParts))
                                //{
                                //    Xoffset += 200;
                                //}

                                if (longver.Size != null && (longver.Size.ToLower().Contains("angle") || longver.Size.ToLower().Contains("isa")))
                                {
                                    string Size = GettheISA(longver);

                                    ISMCTAG(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(Math.Max(rightedge.P1.X, rightedge.P2.X), (rightedge.P1.Y + rightedge.P2.Y) / 2, 0), "lr", sectionsize, Xoffset, ISASIZE: Size);
                                    AllParts.Add(Checkpart);
                                }
                                else
                                {
                                    ISMCTAG(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d((rightedge.P1.X + rightedge.P2.X) / 2, (rightedge.P1.Y + rightedge.P2.Y) / 2, 0), "r", sectionsize, Xoffset);
                                }


                                AllParts.Add(Checkpart);
                                if (PrimPart2D != null)
                                {
                                    PrimDimensionWithSec(longver, PrimPart2D, topedge, leftedge, i);
                                }
                                else
                                {
                                    double l_dist_frm_centre = Math.Round(GetDist(new Point3d(longver.StPt), new Point3d(longver.EndPt)));
                                    CreateAlighDimen(AcadBlockTableRecord, AcadTransaction, new Point3d(rightedge.P1.X, Math.Max(rightedge.P1.Y, rightedge.P2.Y), 0), new Point3d(rightedge.P1.X, Math.Min(rightedge.P1.Y, rightedge.P2.Y), 0), l_dist_frm_centre.ToString(), topedge.Length);
                                }
                            }
                        }
                    }
                }

            }

            //angled filter
            List<SupporSpecData> angled3D = new List<SupporSpecData>();

            foreach (SupporSpecData ang3D in secondary_sort)
            {
                angle = GetRotationFromVec(ang3D.Directionvec);
                if ((Math.Abs(Math.Round(angle.XinDegree)) > 0 && Math.Abs(Math.Round(angle.XinDegree)) < 90) || (Math.Abs(Math.Round(angle.XinDegree)) > 90 && Math.Abs(Math.Round(angle.XinDegree)) < 180))
                {
                    angled3D.Add(ang3D);
                }
            }

            //angled dimension
            foreach (SupporSpecData angsec in angled3D)
            {
                foreach (var part in SecPart2D)
                {
                    if (part.Key == angsec.SuppoId)
                    {
                        List<GeometRi.Segment3d> EdgesSec = new List<GeometRi.Segment3d>();
                        foreach (var edges in part.Value)
                        {
                            if ((edges.Segment.P1.X).Equals(edges.Segment.P2.X) && (edges.Segment.P1.Y).Equals(edges.Segment.P2.Y))
                            {
                                continue;
                            }
                            EdgesSec.Add(edges.Segment);
                        }

                        bool slantside = false;
                        if (EdgesSec.Count() > 0)
                        {
                            double[] MinMaxLOngAngleCoord = Calculate.FindMinMaxCoordinates(EdgesSec, out minX, out minY, out maxX, out maxY);

                            minX = Math.Round(minX);
                            minY = Math.Round(minY);
                            maxX = Math.Round(maxX);
                            maxY = Math.Round(maxY);

                            foreach (GeometRi.Segment3d slant in EdgesSec)
                            {

                                if (Math.Round(Math.Min(slant.P1.X, slant.P2.X)) == minX && Math.Round(slant.P1.Y) == maxY && Math.Round(slant.P2.Y) == maxY)
                                {
                                    //left
                                    slantside = false;
                                    break;
                                }
                                else if (Math.Round(Math.Max(slant.P1.X, slant.P2.X)) == maxX && Math.Round(slant.P1.Y) == maxY && Math.Round(slant.P2.Y) == maxY)
                                {
                                    //right
                                    slantside = true;
                                    break;
                                }
                            }
                            if (!slantside)
                            {
                                GeometRi.Segment3d tophor = null;
                                GeometRi.Segment3d botver = null;
                                GeometRi.Segment3d slantout = null;
                                GeometRi.Segment3d slantin = null;
                                foreach (GeometRi.Segment3d slant in EdgesSec)
                                {
                                    if (Math.Round(Math.Min(slant.P1.X, slant.P2.X)) == minX && Math.Round(slant.P1.Y) == maxY && Math.Round(slant.P2.Y) == maxY)
                                    {
                                        //top hori line
                                        tophor = slant;
                                    }
                                    else if (Math.Round(slant.P1.X) == Math.Round(slant.P2.X) && Math.Round(slant.P1.Y) != Math.Round(slant.P2.Y))
                                    {
                                        //bottom vetical line
                                        botver = slant;
                                    }
                                    else if (Math.Round(Math.Min(slant.P1.X, slant.P2.X)) == minX && Math.Round(Math.Min(slant.P1.Y, slant.P2.Y)) == minY)
                                    {
                                        //slant outside line
                                        slantout = slant;
                                    }
                                    else if (Math.Round(Math.Min(slant.P1.X, slant.P2.X)) != minX && Math.Round(slant.P1.X) != Math.Round(slant.P2.X) && Math.Round(slant.P1.Y) != Math.Round(slant.P2.Y))
                                    {
                                        //slant inside line
                                        slantin = slant;
                                    }
                                }

                                double longlen = EdgesSec[0].Length;
                                for (int ed = 1; ed < EdgesSec.Count - 1; ed++)
                                {
                                    if (EdgesSec[ed].Length > longlen)
                                    {
                                        slantout = EdgesSec[ed];
                                        longlen = EdgesSec[ed].Length;
                                    }
                                }

                                var sectionsize = 0.0;
                                try
                                {
                                    sectionsize = CSECTIONSIZE(angsec.Size);
                                }
                                catch (Exception)
                                {
                                }
                                List<GeometRi.Segment3d> Checkpart = new List<GeometRi.Segment3d>();
                                double Xoffset = 2000;
                                Checkpart = TextBox(new Point3d(Math.Max(slantout.P1.X, slantout.P2.X) + Xoffset, slantout.P1.Y, 0));
                                //while (!Calculate.BoundingBoxIntersectsList(Checkpart, AllParts))
                                //{
                                //    Xoffset += 200;
                                //}

                                Xoffset = 1000;
                                Checkpart = TextBox(new Point3d(Math.Max(slantout.P1.X, slantout.P2.X) + Xoffset, (slantout.P1.Y + slantout.P2.Y) / 2, 0));
                                //while (!Calculate.BoundingBoxIntersectsList(Checkpart, AllParts))
                                //{
                                //    Xoffset += 200;
                                //}
                                ISMCTAG(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d((slantout.P1.X + slantout.P2.X) / 2, (slantout.P1.Y + slantout.P2.Y) / 2, 0), "l", sectionsize, Xoffset);
                                AllParts.Add(Checkpart);
                                //if (PrimPart2D != null)
                                //{
                                //    PrimDimensionWithSec(angsec, PrimPart2D, topedge, leftedge, i);
                                //}
                                //else
                                //{
                                double l_dist_frm_centre = Math.Round(GetDist(new Point3d(angsec.StPt), new Point3d(angsec.EndPt)));
                                CreateAlighDimen(AcadBlockTableRecord, AcadTransaction, new Point3d(slantout.P1.X, slantout.P1.Y, 0), new Point3d(slantout.P2.X, slantout.P2.Y, 0), l_dist_frm_centre.ToString(), -slantout.Length / 4);
                                //}

                                //slant hori dimen
                                //hori small dimen
                                SupporSpecData horitouch = new SupporSpecData();
                                foreach (SupporSpecData hori in secondary_sort)
                                {
                                    angle = GetRotationFromVec(hori.Directionvec);
                                    if (Math.Abs(Math.Round(angle.XinDegree)).Equals(90) || Math.Abs(Math.Round(angle.XinDegree)).Equals(270))
                                    {
                                        if (angsec.ListtouchingParts.Contains(hori.SuppoId))
                                        {
                                            horitouch = hori;
                                        }
                                    }
                                }
                                var botsec2 = "";
                                try
                                {
                                    List<double> all_dist = new List<double>();

                                    var close_pt = FindClosestPoint_TOLINE(horitouch.StPt, horitouch.EndPt, angsec.StPt, angsec.EndPt);
                                    var firs = GET_PROJECTEDPT_DIST(horitouch, new double[] { close_pt[0], close_pt[1], close_pt[2] });
                                    var toppart = horitouch.EndPt;
                                    var anglepart = angsec.EndPt;

                                    sectionsize = CSECTIONSIZE(angsec.Size);
                                    botsec2 = Math.Round(firs - (sectionsize) / (Math.Sqrt(2) * 2)).ToString();
                                }
                                catch (Exception)
                                {

                                }

                                if (horitouch != null && horitouch.SuppoId != null)
                                {
                                    GeometRi.Segment3d touchbot = null;
                                    List<GeometRi.Segment3d> touchedge = new List<GeometRi.Segment3d>();
                                    foreach (var edges in SecPart2D)
                                    {
                                        if (edges.Key == horitouch.SuppoId)
                                        {
                                            List<GeometRi.Segment3d> touch = new List<GeometRi.Segment3d>();
                                            foreach (var tuch in part.Value)
                                            {
                                                if ((tuch.Segment.P1.X).Equals(tuch.Segment.P2.X) && (tuch.Segment.P1.Y).Equals(tuch.Segment.P2.Y))
                                                {
                                                    continue;
                                                }
                                                if (tuch.IDSegment.Contains(horitouch.SuppoId))
                                                {
                                                    touch.Add(tuch.Segment);
                                                }
                                            }

                                            if (touch.Count() > 0)
                                            {
                                                touchbot = GetBottomSegment(touch);
                                            }
                                        }
                                    }
                                    if (touchbot != null && slantout != null)
                                    {
                                        CreateAlighDimen(AcadBlockTableRecord, AcadTransaction, new Point3d(Math.Min(touchbot.P1.X, touchbot.P2.X), touchbot.P1.Y, 0), new Point3d(Math.Min(slantout.P1.X, slantout.P2.X), Math.Max(slantout.P1.Y, slantout.P2.Y), 0), botsec2, -slantout.Length / 4);
                                    }


                                }


                            }
                            else
                            {
                                GeometRi.Segment3d tophor = null;
                                GeometRi.Segment3d botver = null;
                                GeometRi.Segment3d slantout = null;
                                GeometRi.Segment3d slantin = null;
                                foreach (GeometRi.Segment3d slant in EdgesSec)
                                {
                                    if (Math.Round(Math.Max(slant.P1.X, slant.P2.X)) == maxX && Math.Round(slant.P1.Y) == maxY && Math.Round(slant.P2.Y) == maxY)
                                    {
                                        //top hori line
                                        tophor = slant;
                                    }
                                    else if (Math.Round(slant.P1.X) == Math.Round(slant.P2.X) && Math.Round(slant.P1.Y) != Math.Round(slant.P2.Y))
                                    {
                                        //bottom vetical line
                                        botver = slant;
                                    }
                                    else if (Math.Round(Math.Min(slant.P1.X, slant.P2.X)) == minX && Math.Round(Math.Min(slant.P1.Y, slant.P2.Y)) == minY)
                                    {
                                        //slant outside line
                                        slantout = slant;
                                    }
                                    else if (Math.Round(Math.Min(slant.P1.X, slant.P2.X)) != maxX && Math.Round(Math.Max(slant.P1.X, slant.P2.X)) == maxX)
                                    {
                                        //slant inside line
                                        slantin = slant;
                                    }
                                }

                                double longlen = EdgesSec[0].Length;
                                for (int ed = 1; ed < EdgesSec.Count - 1; ed++)
                                {
                                    if (EdgesSec[ed].Length > longlen)
                                    {
                                        slantout = EdgesSec[ed];
                                        longlen = EdgesSec[ed].Length;
                                    }
                                }

                                var sectionsize = 0.0;
                                try
                                {
                                    sectionsize = CSECTIONSIZE(angsec.Size);
                                }
                                catch (Exception)
                                {
                                }
                                List<GeometRi.Segment3d> Checkpart = new List<GeometRi.Segment3d>();
                                double Xoffset = 2000;
                                Checkpart = TextBox(new Point3d(Math.Max(slantout.P1.X, slantout.P2.X) + Xoffset, slantout.P1.Y, 0));
                                //while (!Calculate.BoundingBoxIntersectsList(Checkpart, AllParts))
                                //{
                                //    Xoffset += 200;
                                //}

                                Xoffset = 1000;
                                Checkpart = TextBox(new Point3d(Math.Max(slantout.P1.X, slantout.P2.X) + Xoffset, (slantout.P1.Y + slantout.P2.Y) / 2, 0));
                                //while (!Calculate.BoundingBoxIntersectsList(Checkpart, AllParts))
                                //{
                                //    Xoffset += 200;
                                //}
                                ISMCTAG(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d((slantout.P1.X + slantout.P2.X) / 2, (slantout.P1.Y + slantout.P2.Y) / 2, 0), "r", sectionsize);
                                AllParts.Add(Checkpart);
                                //if (PrimPart2D != null)
                                //{
                                //    PrimDimensionWithSec(angsec, PrimPart2D, topedge, leftedge, i);
                                //}
                                //else
                                //{
                                double l_dist_frm_centre = Math.Round(GetDist(new Point3d(angsec.StPt), new Point3d(angsec.EndPt)));
                                CreateAlighDimen(AcadBlockTableRecord, AcadTransaction, new Point3d(slantout.P1.X, slantout.P1.Y, 0), new Point3d(slantout.P2.X, slantout.P2.Y, 0), l_dist_frm_centre.ToString(), slantout.Length / 4);
                                //}


                                //slant hori dimen
                                //hori small dimen
                                SupporSpecData horitouch = new SupporSpecData();
                                foreach (SupporSpecData hori in secondary_sort)
                                {
                                    angle = GetRotationFromVec(hori.Directionvec);
                                    if (Math.Abs(Math.Round(angle.XinDegree)).Equals(90) || Math.Abs(Math.Round(angle.XinDegree)).Equals(270))
                                    {

                                        if (angsec.ListtouchingParts.Contains(hori.SuppoId))
                                        {
                                            horitouch = hori;
                                        }
                                    }
                                }
                                var botsec2 = "";
                                try
                                {
                                    List<double> all_dist = new List<double>();

                                    var close_pt = FindClosestPoint_TOLINE(horitouch.StPt, horitouch.EndPt, angsec.StPt, angsec.EndPt);
                                    var firs = GET_PROJECTEDPT_DIST(horitouch, new double[] { close_pt[0], close_pt[1], close_pt[2] });

                                    sectionsize = CSECTIONSIZE(angsec.Size);
                                    botsec2 = Math.Round(firs - (sectionsize) / (Math.Sqrt(2) * 2)).ToString();
                                }
                                catch (Exception)
                                {

                                }

                                if (horitouch != null && horitouch.SuppoId != null)
                                {
                                    GeometRi.Segment3d touchbot = null;
                                    List<GeometRi.Segment3d> touchedge = new List<GeometRi.Segment3d>();
                                    foreach (var edges in SecPart2D)
                                    {
                                        if (edges.Key == horitouch.SuppoId)
                                        {
                                            List<GeometRi.Segment3d> touch = new List<GeometRi.Segment3d>();
                                            foreach (var tued in edges.Value)
                                            {
                                                if ((tued.Segment.P1.X).Equals(tued.Segment.P2.X) && (tued.Segment.P1.Y).Equals(tued.Segment.P2.Y))
                                                {
                                                    continue;
                                                }

                                                if (tued.IDSegment.Contains(horitouch.SuppoId))
                                                {
                                                    touch.Add(tued.Segment);
                                                }


                                            }

                                            if (touch.Count() > 0)
                                            {
                                                touchbot = GetBottomSegment(touch);
                                            }
                                        }
                                    }

                                    CreateAlighDimen(AcadBlockTableRecord, AcadTransaction, new Point3d(Math.Max(touchbot.P1.X, touchbot.P2.X), touchbot.P1.Y, 0), new Point3d(Math.Max(slantout.P1.X, slantout.P2.X), Math.Max(slantout.P1.Y, slantout.P2.Y), 0), botsec2, -slantout.Length / 4);
                                }

                            }

                        }
                    }
                }
            }

            //hori dimension with prim
            if (ListCentalSuppoData[i].ListSecondrySuppo.Count > 1)
            {
                for (int s = 0; s < secondary_sort.Count() - 1; s++)
                {
                    angle = GetRotationFromVec(secondary_sort.ElementAt(s).Directionvec);
                    foreach (var part in SecPart2D)
                    {
                        if (part.Key == secondary_sort.ElementAt(s).SuppoId)
                        {
                            List<GeometRi.Segment3d> EdgesSec = new List<GeometRi.Segment3d>();
                            foreach (var edges in part.Value)
                            {
                                if ((edges.Segment.P1.X).Equals(edges.Segment.P2.X) && (edges.Segment.P1.Y).Equals(edges.Segment.P2.Y))
                                {
                                    continue;
                                }
                                EdgesSec.Add(edges.Segment);
                            }

                            if (EdgesSec.Count() > 0)
                            {
                                var topedge = GetTopSegment(EdgesSec);
                                var botedge = GetBottomSegment(EdgesSec);
                                var leftedge = GetLeftSideSegment(EdgesSec);
                                var rightedge = GetRightSideSegment(EdgesSec);
                                var sectionsize = 0.0;
                                try
                                {
                                    sectionsize = CSECTIONSIZE(secondary_sort.ElementAt(s).Size);
                                }
                                catch (Exception)
                                {
                                }
                                if (Math.Abs(Math.Round(angle.XinDegree)).Equals(90) || Math.Abs(Math.Round(angle.XinDegree)).Equals(270))
                                {
                                    if (Math.Min(topedge.P1.X, topedge.P2.X) < minX)
                                    {
                                        List<GeometRi.Segment3d> Checkpart = new List<GeometRi.Segment3d>();
                                        double Xoffset = 1000;
                                        Checkpart = TextBox(new Point3d(Math.Min(topedge.P1.X, topedge.P2.X) - Xoffset, topedge.P1.Y, 0));
                                        //AllParts.Add(Checkpart);
                                        //while (!Calculate.BoundingBoxIntersectsList(Checkpart, AllParts))
                                        //{
                                        //    Xoffset -= 200;
                                        //}
                                        if (!secondary_sort.ElementAt(s).SupportName.ToLower().Contains("plate"))
                                        {
                                            GENTOSLOC(AcadBlockTableRecord, AcadTransaction, AcadDatabase, Math.Min(topedge.P1.X, topedge.P2.X), topedge.P1.Y, MillimetersToMeters(secondary_sort.ElementAt(s).Boundingboxmax.Z).ToString(), Xoffset: Xoffset);
                                            AllParts.Add(Checkpart);
                                            Xoffset = 1000;
                                            Checkpart = TextBox(new Point3d(Math.Min(leftedge.P1.X, leftedge.P2.X) - Xoffset, (leftedge.P1.Y + leftedge.P2.Y) / 2, 0));
                                            //while (!Calculate.BoundingBoxIntersectsList(Checkpart, AllParts))
                                            //{
                                            //    Xoffset -= 200;
                                            //}

                                            if (secondary_sort.ElementAt(s).Size != null && (secondary_sort.ElementAt(s).Size.ToLower().Contains("angle") || secondary_sort.ElementAt(s).Size.ToLower().Contains("isa")))
                                            {
                                                string Size = GettheISA(secondary_sort.ElementAt(s));

                                                ISMCTAG(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(Math.Min(leftedge.P1.X, leftedge.P2.X), (leftedge.P1.Y + leftedge.P2.Y) / 2, 0), "ll", sectionsize, Xoffset, ISASIZE: Size);
                                                AllParts.Add(Checkpart);
                                            }
                                            else
                                            {
                                                ISMCTAG(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(Math.Min(leftedge.P1.X, leftedge.P2.X), (leftedge.P1.Y + leftedge.P2.Y) / 2, 0), "l", sectionsize, Xoffset);
                                            }


                                            AllParts.Add(Checkpart);

                                        }

                                        if (PrimPart2D != null)
                                        {
                                            PrimDimensionWithSec(secondary_sort.ElementAt(s), PrimPart2D, topedge, leftedge, i);
                                        }
                                        else
                                        {
                                            double l_dist_frm_centre = Math.Round(GetDist(new Point3d(secondary_sort.ElementAt(s).StPt), new Point3d(secondary_sort.ElementAt(s).EndPt)));
                                            CreateDimension(new Point3d(Math.Min(topedge.P1.X, topedge.P2.X), topedge.P1.Y, 0), new Point3d(Math.Max(topedge.P1.X, topedge.P2.X), topedge.P1.Y, 0), l_dist_frm_centre.ToString(), leftedge.Length / 2);
                                        }


                                    }
                                    else if (Math.Max(topedge.P1.X, topedge.P2.X) > minX)
                                    {
                                        List<GeometRi.Segment3d> Checkpart = new List<GeometRi.Segment3d>();
                                        double Xoffset = 2000;
                                        Checkpart = TextBox(new Point3d(Math.Max(topedge.P1.X, topedge.P2.X) + Xoffset, topedge.P1.Y, 0));
                                        //while (!Calculate.BoundingBoxIntersectsList(Checkpart, AllParts))
                                        //{
                                        //    Xoffset += 200;
                                        //}
                                        if (!secondary_sort.ElementAt(s).SupportName.ToLower().Contains("plate"))
                                        {
                                            GENTOSLOC(AcadBlockTableRecord, AcadTransaction, AcadDatabase, Math.Max(topedge.P1.X, topedge.P2.X), topedge.P1.Y, MillimetersToMeters(secondary_sort.ElementAt(s).Boundingboxmax.Z).ToString(), Xoffset: Xoffset);
                                            AllParts.Add(Checkpart);

                                            Xoffset = 1000;
                                            Checkpart = TextBox(new Point3d(Math.Max(rightedge.P1.X, rightedge.P2.X) + Xoffset, (rightedge.P1.Y + rightedge.P2.Y) / 2, 0));
                                            //while (!Calculate.BoundingBoxIntersectsList(Checkpart, AllParts))
                                            //{
                                            //    Xoffset += 200;
                                            //}

                                            if (secondary_sort.ElementAt(s).Size != null && (secondary_sort.ElementAt(s).Size.ToLower().Contains("angle") || secondary_sort.ElementAt(s).Size.ToLower().Contains("isa")))
                                            {
                                                string Size = GettheISA(secondary_sort.ElementAt(s));

                                                ISMCTAG(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(Math.Max(rightedge.P1.X, rightedge.P2.X), (rightedge.P1.Y + rightedge.P2.Y) / 2, 0), "lr", sectionsize, Xoffset, ISASIZE: Size);
                                                AllParts.Add(Checkpart);
                                            }
                                            else
                                            {
                                                ISMCTAG(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(Math.Max(rightedge.P1.X, rightedge.P2.X), (rightedge.P1.Y + rightedge.P2.Y) / 2, 0), "r", sectionsize, Xoffset);
                                            }


                                            AllParts.Add(Checkpart);
                                        }
                                        if (PrimPart2D != null)
                                        {
                                            PrimDimensionWithSec(secondary_sort.ElementAt(s), PrimPart2D, topedge, leftedge, i);
                                        }
                                        else
                                        {
                                            double l_dist_frm_centre = Math.Round(GetDist(new Point3d(secondary_sort.ElementAt(s).StPt), new Point3d(secondary_sort.ElementAt(s).EndPt)));
                                            CreateDimension(new Point3d(Math.Min(topedge.P1.X, topedge.P2.X), topedge.P1.Y, 0), new Point3d(Math.Max(topedge.P1.X, topedge.P2.X), topedge.P1.Y, 0), l_dist_frm_centre.ToString(), leftedge.Length / 2);
                                        }
                                    }
                                    else
                                    {

                                    }
                                }
                            }
                        }
                    }
                }
            }
            else if (ListCentalSuppoData[i].ListSecondrySuppo.Count == 1)
            {
                for (int s = 0; s < secondary_sort.Count(); s++)
                {
                    angle = GetRotationFromVec(secondary_sort.ElementAt(s).Directionvec);
                    foreach (var part in SecPart2D)
                    {
                        if (part.Key == secondary_sort.ElementAt(s).SuppoId)
                        {
                            List<GeometRi.Segment3d> EdgesSec = new List<GeometRi.Segment3d>();
                            foreach (var edges in part.Value)
                            {
                                if ((edges.Segment.P1.X).Equals(edges.Segment.P2.X) && (edges.Segment.P1.Y).Equals(edges.Segment.P2.Y))
                                {
                                    continue;
                                }
                                EdgesSec.Add(edges.Segment);
                            }

                            if (EdgesSec.Count() > 0)
                            {
                                var topedge = GetTopSegment(EdgesSec);
                                var botedge = GetBottomSegment(EdgesSec);
                                var leftedge = GetLeftSideSegment(EdgesSec);
                                var rightedge = GetRightSideSegment(EdgesSec);
                                var sectionsize = 0.0;
                                try
                                {
                                    sectionsize = CSECTIONSIZE(secondary_sort.ElementAt(s).Size);
                                }
                                catch (Exception)
                                {
                                }
                                if (Math.Abs(Math.Round(angle.XinDegree)).Equals(90) || Math.Abs(Math.Round(angle.XinDegree)).Equals(270))
                                {
                                    if (Math.Min(topedge.P1.X, topedge.P2.X) < minX)
                                    {
                                        List<GeometRi.Segment3d> Checkpart = new List<GeometRi.Segment3d>();
                                        double Xoffset = 1000;
                                        Checkpart = TextBox(new Point3d(Math.Min(topedge.P1.X, topedge.P2.X) - Xoffset, topedge.P1.Y, 0));
                                        //AllParts.Add(Checkpart);
                                        //while (!Calculate.BoundingBoxIntersectsList(Checkpart, AllParts))
                                        //{
                                        //    Xoffset -= 200;
                                        //}
                                        if (!secondary_sort.ElementAt(s).SupportName.ToLower().Contains("plate"))
                                        {
                                            GENTOSLOC(AcadBlockTableRecord, AcadTransaction, AcadDatabase, Math.Min(topedge.P1.X, topedge.P2.X), topedge.P1.Y, MillimetersToMeters(secondary_sort.ElementAt(s).Boundingboxmax.Z).ToString(), Xoffset: Xoffset);
                                            AllParts.Add(Checkpart);
                                            Xoffset = 1000;
                                            Checkpart = TextBox(new Point3d(Math.Min(leftedge.P1.X, leftedge.P2.X) - Xoffset, (leftedge.P1.Y + leftedge.P2.Y) / 2, 0));
                                            //while (!Calculate.BoundingBoxIntersectsList(Checkpart, AllParts))
                                            //{
                                            //    Xoffset -= 200;
                                            //}

                                            if (secondary_sort.ElementAt(s).Size != null && (secondary_sort.ElementAt(s).Size.ToLower().Contains("angle") || secondary_sort.ElementAt(s).Size.ToLower().Contains("isa")))
                                            {
                                                string Size = GettheISA(secondary_sort.ElementAt(s));

                                                ISMCTAG(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(Math.Min(leftedge.P1.X, leftedge.P2.X), (leftedge.P1.Y + leftedge.P2.Y) / 2, 0), "ll", sectionsize, Xoffset, ISASIZE: Size);
                                                AllParts.Add(Checkpart);
                                            }
                                            else
                                            {
                                                ISMCTAG(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(Math.Min(leftedge.P1.X, leftedge.P2.X), (leftedge.P1.Y + leftedge.P2.Y) / 2, 0), "l", sectionsize, Xoffset);
                                            }


                                            AllParts.Add(Checkpart);

                                        }

                                        if (PrimPart2D != null)
                                        {
                                            PrimDimensionWithSec(secondary_sort.ElementAt(s), PrimPart2D, topedge, leftedge, i);
                                        }
                                        else
                                        {
                                            double l_dist_frm_centre = Math.Round(GetDist(new Point3d(secondary_sort.ElementAt(s).StPt), new Point3d(secondary_sort.ElementAt(s).EndPt)));
                                            CreateDimension(new Point3d(Math.Min(topedge.P1.X, topedge.P2.X), topedge.P1.Y, 0), new Point3d(Math.Max(topedge.P1.X, topedge.P2.X), topedge.P1.Y, 0), l_dist_frm_centre.ToString(), leftedge.Length / 2);
                                        }


                                    }
                                    else if (Math.Max(topedge.P1.X, topedge.P2.X) > minX)
                                    {
                                        List<GeometRi.Segment3d> Checkpart = new List<GeometRi.Segment3d>();
                                        double Xoffset = 2000;
                                        Checkpart = TextBox(new Point3d(Math.Max(topedge.P1.X, topedge.P2.X) + Xoffset, topedge.P1.Y, 0));
                                        //while (!Calculate.BoundingBoxIntersectsList(Checkpart, AllParts))
                                        //{
                                        //    Xoffset += 200;
                                        //}
                                        if (!secondary_sort.ElementAt(s).SupportName.ToLower().Contains("plate"))
                                        {
                                            GENTOSLOC(AcadBlockTableRecord, AcadTransaction, AcadDatabase, Math.Max(topedge.P1.X, topedge.P2.X), topedge.P1.Y, MillimetersToMeters(secondary_sort.ElementAt(s).Boundingboxmax.Z).ToString(), Xoffset: Xoffset);
                                            AllParts.Add(Checkpart);

                                            Xoffset = 1000;
                                            Checkpart = TextBox(new Point3d(Math.Max(rightedge.P1.X, rightedge.P2.X) + Xoffset, (rightedge.P1.Y + rightedge.P2.Y) / 2, 0));
                                            //while (!Calculate.BoundingBoxIntersectsList(Checkpart, AllParts))
                                            //{
                                            //    Xoffset += 200;
                                            //}

                                            if (secondary_sort.ElementAt(s).Size != null && (secondary_sort.ElementAt(s).Size.ToLower().Contains("angle") || secondary_sort.ElementAt(s).Size.ToLower().Contains("isa")))
                                            {
                                                string Size = GettheISA(secondary_sort.ElementAt(s));

                                                ISMCTAG(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(Math.Max(rightedge.P1.X, rightedge.P2.X), (rightedge.P1.Y + rightedge.P2.Y) / 2, 0), "lr", sectionsize, Xoffset, ISASIZE: Size);
                                                AllParts.Add(Checkpart);
                                            }
                                            else
                                            {
                                                ISMCTAG(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(Math.Max(rightedge.P1.X, rightedge.P2.X), (rightedge.P1.Y + rightedge.P2.Y) / 2, 0), "r", sectionsize, Xoffset);
                                            }


                                            AllParts.Add(Checkpart);
                                        }
                                        if (PrimPart2D != null)
                                        {
                                            PrimDimensionWithSec(secondary_sort.ElementAt(s), PrimPart2D, topedge, leftedge, i);
                                        }
                                        else
                                        {
                                            double l_dist_frm_centre = Math.Round(GetDist(new Point3d(secondary_sort.ElementAt(s).StPt), new Point3d(secondary_sort.ElementAt(s).EndPt)));
                                            CreateDimension(new Point3d(Math.Min(topedge.P1.X, topedge.P2.X), topedge.P1.Y, 0), new Point3d(Math.Max(topedge.P1.X, topedge.P2.X), topedge.P1.Y, 0), l_dist_frm_centre.ToString(), leftedge.Length / 2);
                                        }
                                    }
                                    else
                                    {

                                    }
                                }
                                else if (Math.Abs(Math.Round(angle.XinDegree)).Equals(0) || Math.Abs(Math.Round(angle.XinDegree)).Equals(180))
                                {
                                    List<GeometRi.Segment3d> Checkpart = new List<GeometRi.Segment3d>();
                                    double Xoffset = 2000;
                                    Checkpart = TextBox(new Point3d(Math.Max(topedge.P1.X, topedge.P2.X) + Xoffset, topedge.P1.Y, 0));
                                    //while (!Calculate.BoundingBoxIntersectsList(Checkpart, AllParts))
                                    //{
                                    //    Xoffset += 200;
                                    //}

                                    Xoffset = 1000;
                                    Checkpart = TextBox(new Point3d(Math.Max(rightedge.P1.X, rightedge.P2.X) + Xoffset, (rightedge.P1.Y + rightedge.P2.Y) / 2, 0));
                                    //while (!Calculate.BoundingBoxIntersectsList(Checkpart, AllParts))
                                    //{
                                    //    Xoffset += 200;
                                    //}
                                    if (secondary_sort.ElementAt(s).SupportName == null && !secondary_sort.ElementAt(s).SupportName.ToLower().Contains("plate"))
                                    {
                                        if (secondary_sort.ElementAt(s).Size != null && (secondary_sort.ElementAt(s).Size.ToLower().Contains("angle") || secondary_sort.ElementAt(s).Size.ToLower().Contains("isa")))
                                        {
                                            string Size = GettheISA(secondary_sort.ElementAt(s));

                                            ISMCTAG(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(Math.Min(leftedge.P1.X, leftedge.P2.X), (leftedge.P1.Y + leftedge.P2.Y) / 2, 0), "ll", sectionsize, Xoffset, ISASIZE: Size);
                                            AllParts.Add(Checkpart);
                                        }
                                        else
                                        {
                                            ISMCTAG(AcadBlockTableRecord, AcadTransaction, AcadDatabase, new Point3d(Math.Min(leftedge.P1.X, leftedge.P2.X), (leftedge.P1.Y + leftedge.P2.Y) / 2, 0), "l", sectionsize, Xoffset);
                                        }

                                        AllParts.Add(Checkpart);
                                        //if (PrimPart2D != null)
                                        //{
                                        //    PrimDimensionWithSec(secondary_sort.ElementAt(s), PrimPart2D, topedge, leftedge, i);
                                        //}
                                        //else
                                        //{
                                    }
                                    double l_dist_frm_centre = Math.Round(GetDist(new Point3d(secondary_sort.ElementAt(s).StPt), new Point3d(secondary_sort.ElementAt(s).EndPt)));
                                    CreateAlighDimen(AcadBlockTableRecord, AcadTransaction, new Point3d(rightedge.P1.X, Math.Max(rightedge.P1.Y, rightedge.P2.Y), 0), new Point3d(rightedge.P1.X, Math.Min(rightedge.P1.Y, rightedge.P2.Y), 0), l_dist_frm_centre.ToString(), -topedge.Length);
                                    //}
                                }
                            }
                        }
                    }
                }
            }



        }

        public void PrimDimensionWithSec(SupporSpecData secPart, Dictionary<string, List<EntityData>> PrimPart2D, GeometRi.Segment3d sectopedge, GeometRi.Segment3d secleftedge, int i)
        {
            bool IsPrim = false;
            foreach (var item in secPart.ListtouchingParts)
            {
                if (item.ToLower().Contains("p"))
                {
                    IsPrim = true;
                    break;
                }
            }

            if (IsPrim && secPart.IsGussetplate == false && secPart.IsAnchor == false)
            {

                //generic prim sup
                IOrderedEnumerable<SupporSpecData> allPrim = null;
                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 1)
                {
                    if (ListCentalSuppoData[i].ListPrimarySuppo[0].Centroid.X == ListCentalSuppoData[i].ListPrimarySuppo[1].Centroid.X)
                    {
                        allPrim = ListCentalSuppoData[i].ListPrimarySuppo.OrderBy(e => e.Centroid.Y);
                    }
                    else
                    {
                        allPrim = ListCentalSuppoData[i].ListPrimarySuppo.OrderBy(e => e.Centroid.X);
                    }
                }
                else if (ListCentalSuppoData[i].ListPrimarySuppo.Count == 1)
                {
                    allPrim = ListCentalSuppoData[i].ListPrimarySuppo.OrderBy(e => e.Centroid.X);
                }

                List<SupporSpecData> primtoucingsec = new List<SupporSpecData>();
                foreach (var item in allPrim)
                {
                    if (secPart.ListtouchingParts.Contains(item.SuppoId))
                    {
                        primtoucingsec.Add(item);
                    }
                }
                if (primtoucingsec != null)
                {
                    //hori dimen prim and top sec
                    double l_dist_frm_centre = 0;
                    //double r_dist_frm_centre = 0;
                    double prim1_dist = 0;
                    List<double> final_distances_list = new List<double>();
                    //dimensioning
                    try
                    {
                        if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0)
                        {
                            List<double> all_dimen = new List<double>();
                            all_dimen.Clear();
                            var strpt = secPart.StPt;
                            var endpt = secPart.EndPt;

                            int dir = 0;
                            double[] fin_pt = null;
                            for (int j = 0; j < primtoucingsec.Count(); j++)
                            {
                                var prim1_midpt = primtoucingsec.ElementAt(j).Centroid;
                                Point3d prim1_projectedpt = FindPerpendicularFoot(prim1_midpt, strpt, endpt);

                                var left = 0.0;
                                var right = 0.0;
                                if (j == 0)
                                {
                                    left = GetDist(new Point3d(strpt), new Point3d(prim1_midpt.X, prim1_midpt.Y, prim1_midpt.Z));
                                    right = GetDist(new Point3d(endpt), new Point3d(prim1_midpt.X, prim1_midpt.Y, prim1_midpt.Z));
                                    if (left < right && dir == 0)
                                    {
                                        prim1_dist = Math.Round(GetDist(new Point3d(strpt), prim1_projectedpt));
                                        fin_pt = strpt;
                                        dir = 1;
                                    }
                                    else if (left >= right && dir == 0)
                                    {
                                        prim1_dist = Math.Round(GetDist(new Point3d(endpt), prim1_projectedpt));
                                        fin_pt = endpt;
                                        dir = -1;
                                    }
                                }
                                prim1_dist = Math.Round(GetDist(new Point3d(fin_pt), prim1_projectedpt));
                                all_dimen.Add(prim1_dist);


                            }
                            all_dimen.Sort();
                            final_distances_list = CalculateAccumulation(all_dimen);

                        }
                        else
                        {
                            l_dist_frm_centre = Math.Round(GetDist(new Point3d(secPart.StPt), new Point3d(secPart.EndPt)));
                        }

                    }
                    catch (Exception e)
                    {

                    }

                    //dimensioning
                    if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0)
                    {
                        GeometRi.Segment3d topedge = null;
                        GeometRi.Segment3d botedge = null;
                        GeometRi.Segment3d leftedge = null;
                        GeometRi.Segment3d rightedge = null;

                        GeometRi.Segment3d prevtopedge = null;
                        GeometRi.Segment3d prevbotedge = null;
                        GeometRi.Segment3d prevleftedge = null;
                        GeometRi.Segment3d prevrightedge = null;

                        if (secPart.ListtouchingParts.Contains(primtoucingsec.First().SuppoId))
                        {
                            foreach (var part in PrimPart2D)
                            {
                                if (part.Key == primtoucingsec.First().SuppoId)
                                {
                                    List<GeometRi.Segment3d> EdgesPrim = new List<GeometRi.Segment3d>();
                                    foreach (var edges in part.Value)
                                    {
                                        if ((edges.Segment.P1.X).Equals(edges.Segment.P2.X) && (edges.Segment.P1.Y).Equals(edges.Segment.P2.Y))
                                        {
                                            continue;
                                        }
                                        EdgesPrim.Add(edges.Segment);
                                    }
                                    topedge = GetTopSegment(EdgesPrim);
                                    botedge = GetBottomSegment(EdgesPrim);
                                    leftedge = GetLeftSideSegment(EdgesPrim);
                                    rightedge = GetRightSideSegment(EdgesPrim);
                                }
                            }
                        }

                        if (sectopedge != null && topedge != null && leftedge != null)
                        {
                            CreateDimension(new Point3d(Math.Min(sectopedge.P1.X, sectopedge.P2.X), sectopedge.P1.Y, 0), new Point3d((topedge.P1.X + topedge.P2.X) / 2, (leftedge.P1.Y + leftedge.P2.Y) / 2, 0), final_distances_list.First().ToString(), topedge.P1.Y - (leftedge.P1.Y + leftedge.P2.Y) / 2);
                            for (int k = 1; k < primtoucingsec.Count(); k++)
                            {
                                foreach (var part in PrimPart2D)
                                {
                                    if (part.Key == primtoucingsec.ElementAt(k - 1).SuppoId)
                                    {
                                        List<GeometRi.Segment3d> EdgesPrim = new List<GeometRi.Segment3d>();
                                        foreach (var edges in part.Value)
                                        {
                                            if ((edges.Segment.P1.X).Equals(edges.Segment.P2.X) && (edges.Segment.P1.Y).Equals(edges.Segment.P2.Y))
                                            {
                                                continue;
                                            }
                                            EdgesPrim.Add(edges.Segment);
                                        }
                                        prevtopedge = GetTopSegment(EdgesPrim);
                                        prevbotedge = GetBottomSegment(EdgesPrim);
                                        prevleftedge = GetLeftSideSegment(EdgesPrim);
                                        prevrightedge = GetRightSideSegment(EdgesPrim);
                                    }

                                    if (part.Key == primtoucingsec.ElementAt(k).SuppoId)
                                    {
                                        List<GeometRi.Segment3d> EdgesPrim = new List<GeometRi.Segment3d>();
                                        foreach (var edges in part.Value)
                                        {
                                            if ((edges.Segment.P1.X).Equals(edges.Segment.P2.X) && (edges.Segment.P1.Y).Equals(edges.Segment.P2.Y))
                                            {
                                                continue;
                                            }
                                            EdgesPrim.Add(edges.Segment);
                                        }
                                        topedge = GetTopSegment(EdgesPrim);
                                        botedge = GetBottomSegment(EdgesPrim);
                                        leftedge = GetLeftSideSegment(EdgesPrim);
                                        rightedge = GetRightSideSegment(EdgesPrim);
                                    }
                                }

                                if (k % 2 == 0)
                                {
                                    CreateDimension(new Point3d((prevtopedge.P1.X + prevtopedge.P2.X) / 2, (prevleftedge.P1.Y + prevleftedge.P2.Y) / 2, 0), new Point3d((topedge.P1.X + topedge.P2.X) / 2, (leftedge.P1.Y + leftedge.P2.Y) / 2, 0), final_distances_list[k].ToString(), topedge.P1.Y - (leftedge.P1.Y + leftedge.P2.Y) / 2);
                                }
                                else
                                {
                                    CreateDimension(new Point3d((prevtopedge.P1.X + prevtopedge.P2.X) / 2, (prevleftedge.P1.Y + prevleftedge.P2.Y) / 2, 0), new Point3d((topedge.P1.X + topedge.P2.X) / 2, (leftedge.P1.Y + leftedge.P2.Y) / 2, 0), final_distances_list[k].ToString(), 1.5 * (topedge.P1.Y - (leftedge.P1.Y + leftedge.P2.Y) / 2));
                                }


                            }

                            if (secPart.ListtouchingParts.Contains(primtoucingsec.Last().SuppoId))
                            {
                                foreach (var part in PrimPart2D)
                                {
                                    if (part.Key == primtoucingsec.Last().SuppoId)
                                    {
                                        List<GeometRi.Segment3d> EdgesPrim = new List<GeometRi.Segment3d>();
                                        foreach (var edges in part.Value)
                                        {
                                            if ((edges.Segment.P1.X).Equals(edges.Segment.P2.X) && (edges.Segment.P1.Y).Equals(edges.Segment.P2.Y))
                                            {
                                                continue;
                                            }
                                            EdgesPrim.Add(edges.Segment);
                                        }
                                        topedge = GetTopSegment(EdgesPrim);
                                        botedge = GetBottomSegment(EdgesPrim);
                                        leftedge = GetLeftSideSegment(EdgesPrim);
                                        rightedge = GetRightSideSegment(EdgesPrim);
                                    }
                                }
                            }

                            double last_dist = Math.Round(GetDist(new Point3d(secPart.StPt), new Point3d(secPart.EndPt)) - final_distances_list.Sum());
                            CreateDimension(new Point3d(Math.Max(sectopedge.P1.X, sectopedge.P2.X), sectopedge.P1.Y, 0), new Point3d((topedge.P1.X + topedge.P2.X) / 2, (leftedge.P1.Y + leftedge.P2.Y) / 2, 0), last_dist.ToString(), topedge.P1.Y - (leftedge.P1.Y + leftedge.P2.Y) / 2);


                        }
                        else
                        {
                            CreateDimension(new Point3d(Math.Min(sectopedge.P1.X, sectopedge.P2.X), sectopedge.P1.Y, 0), new Point3d(Math.Max(sectopedge.P1.X, sectopedge.P2.X), sectopedge.P1.Y, 0), l_dist_frm_centre.ToString(), secleftedge.Length / 2);
                        }
                    }

                    foreach (var prim in primtoucingsec)
                    {
                        if (secPart.ListtouchingParts.Contains(prim.SuppoId))
                        {
                            foreach (var part in PrimPart2D)
                            {
                                if (part.Key == prim.SuppoId)
                                {
                                    List<GeometRi.Segment3d> EdgesPrim = new List<GeometRi.Segment3d>();
                                    foreach (var edges in part.Value)
                                    {
                                        if ((edges.Segment.P1.X).Equals(edges.Segment.P2.X) && (edges.Segment.P1.Y).Equals(edges.Segment.P2.Y))
                                        {
                                            continue;
                                        }
                                        EdgesPrim.Add(edges.Segment);
                                    }
                                    var topedge = GetTopSegment(EdgesPrim);
                                    var botedge = GetBottomSegment(EdgesPrim);
                                    var leftedge = GetLeftSideSegment(EdgesPrim);
                                    var rightedge = GetRightSideSegment(EdgesPrim);
                                }

                            }
                        }
                    }
                }
            }
        }


        public List<GeometRi.Segment3d> TextBox(Point3d textpos)
        {
            List<GeometRi.Segment3d> BoxTextData = new List<GeometRi.Segment3d>();
            GeometRi.Segment3d top = new GeometRi.Segment3d(new GeometRi.Point3d(textpos.X - 1000, textpos.Y + 100, 0), new GeometRi.Point3d(textpos.X + 1000, textpos.Y + 100, 0));
            BoxTextData.Add(top);
            GeometRi.Segment3d right = new GeometRi.Segment3d(new GeometRi.Point3d(textpos.X + 1000, textpos.Y + 100, 0), new GeometRi.Point3d(textpos.X + 1000, textpos.Y - 100, 0));
            BoxTextData.Add(right);
            GeometRi.Segment3d bottom = new GeometRi.Segment3d(new GeometRi.Point3d(textpos.X + 1000, textpos.Y - 100, 0), new GeometRi.Point3d(textpos.X - 1000, textpos.Y - 100, 0));
            BoxTextData.Add(bottom);
            GeometRi.Segment3d left = new GeometRi.Segment3d(new GeometRi.Point3d(textpos.X - 1000, textpos.Y - 100, 0), new GeometRi.Point3d(textpos.X - 1000, textpos.Y + 100, 0));
            BoxTextData.Add(left);

            return BoxTextData;
        }
        public void ConcretDimension(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, SupporSpecData concretepart, Dictionary<string, List<EntityData>> ConcretePart2D, int i)
        {

            var conpart = ConcretePart2D.Where(e => e.Key == concretepart.SuppoId);

            var botface = conpart.First().Value;
            if (botface.Count > 0)
            {
                List<GeometRi.Segment3d> botalledges = new List<GeometRi.Segment3d>();
                foreach (var seg in botface)
                {
                    botalledges.Add(seg.Segment);
                }
                var botedge = GetBottomSegment(botalledges);
                var topedge = GetTopSegment(botalledges);
                if (botedge != null && topedge != null)
                {
                    TOSLOC(AcadBlockTableRecord, AcadTransaction, AcadDatabase, Math.Max(botedge.P1.X, botedge.P2.X), botedge.P1.Y, Math.Round(MillimetersToMeters(concretepart.Boundingboxmin.Z), 2).ToString(), HTtype: "HPP(+)");
                    Point3d toppt = new Point3d(Math.Max(topedge.P1.X, topedge.P2.X), topedge.P1.Y, 0);
                    Point3d botpt = new Point3d(Math.Max(botedge.P1.X, botedge.P2.X), botedge.P1.Y, 0);
                    string topsec = concretepart.BoxData.Z.ToString();
                    CreateDimension(toppt, botpt, topsec, text_pos_rotation: -Math.PI / 2, horizontal_offset: 1000);
                }
            }

            var topconcret = ListCentalSuppoData[i].ListConcreteData.OrderByDescending(e => e.Boundingboxmax.Z);


        }

        public void PlacePrim(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, Dictionary<string, List<GeometRi.Segment3d>> PrimFaceEdges, IOrderedEnumerable<SupporSpecData> secondary_sort, Dictionary<string, List<EntityData>> SecPart2D, [Optional] int i)
        {

            var longsec = SecPart2D.Where(e => e.Key == secondary_sort.Where(z => z.BoxData.Z == secondary_sort.Max(m => m.BoxData.Z)).First().SuppoId);

            List<GeometRi.Segment3d> ListLongSecEdges = new List<GeometRi.Segment3d>();
            foreach (var edges in longsec.First().Value)
            {
                ListLongSecEdges.Add(edges.Segment);
            }
            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double maxX = double.MinValue;
            double maxY = double.MinValue;
            double[] MinMaxLOngSecCoord = Calculate.FindMinMaxCoordinates(ListLongSecEdges, out minX, out minY, out maxX, out maxY);

            bool dirPrim = false;
            int oddeven = 0;
            foreach (var item in PrimFaceEdges)
            {

                var bottomedge = GetBottomSegment(item.Value);
                var leftedge = GetLeftSideSegment(item.Value);
                if (bottomedge != null && leftedge != null)
                {
                    if (Math.Min(bottomedge.P1.X, bottomedge.P2.X) < minX)
                    {
                        dirPrim = false;
                        PlacePrimType(AcadBlockTableRecord, AcadTransaction, AcadDatabase, bottomedge, leftedge, item.Key, i, oddeven, dirPrim);
                    }
                    else
                    {
                        dirPrim = true;
                        PlacePrimType(AcadBlockTableRecord, AcadTransaction, AcadDatabase, bottomedge, leftedge, item.Key, i, oddeven, dirPrim);
                    }
                }

            }
        }

        public void PlacePrimType(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, GeometRi.Segment3d bottomedge, GeometRi.Segment3d leftedge, string PrimID, int i, int oddeven = 0, bool dirPrim = false)
        {

            if (bottomedge != null)
            {
                double scalefactor = bottomedge.Length / 2;
                double Yoffset = leftedge.Length;
                double centerX = (bottomedge.P1.X + bottomedge.P2.X) / 2;
                double centerY = bottomedge.P1.Y;
                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0)
                {
                    foreach (var prim in ListCentalSuppoData[i].ListPrimarySuppo)
                    {
                        if (prim.SuppoId == PrimID)
                        {
                            string prim_height = "";
                            try
                            {
                                if (ListCentalSuppoData[i].ListPrimarySuppo.Count > 0)
                                {
                                    prim_height = GET_PRIM_HT_FRM_DATUM(prim);
                                }
                            }
                            catch (Exception)
                            {
                            }
                            try
                            {
                                if (prim.SupportName.ToLower().Contains("clamp") || prim.SupportName.ToLower().Contains("brac"))
                                {

                                    GEN_CHECKPRIM_CLAMP_BRAC(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY + scalefactor, scalefactor, prim);

                                    POS_OF_PRIMLEADER(AcadBlockTableRecord, AcadTransaction, AcadDatabase, prim, centerX, centerY, scalefactor, oddeven, prim_height, Yoffset, dirPrim);
                                }
                                else if (ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.ToLower().Contains("nb") || ListCentalSuppoData[i].ListPrimarySuppo[0].SupportName.Length > 0)
                                {

                                    GEN_CHECKPRIM_CLAMP_BRAC(AcadBlockTableRecord, AcadTransaction, AcadDatabase, centerX, centerY, scalefactor, prim);

                                    POS_OF_PRIMLEADER(AcadBlockTableRecord, AcadTransaction, AcadDatabase, prim, centerX, centerY, scalefactor, oddeven, prim_height, Yoffset, dirPrim);
                                }
                            }
                            catch (Exception)
                            {

                            }
                        }

                    }
                }
            }
        }

        public void POS_OF_PRIMLEADER(BlockTableRecord AcadBlockTableRecord, Transaction AcadTransaction, Database AcadDatabase, SupporSpecData prim, double centerX, double centerY, double scalefactor, int oddeven, string prim_height, double Yoffset, bool dirPrim = false)
        {
            if (dirPrim)
            {
                if (oddeven % 2 == 0)
                {
                    DrawLeader_WITH_TXT_ABOVE_BELOW(AcadBlockTableRecord, AcadTransaction, AcadDatabase, prim.SupportName, "CL.EL.(+)" + prim_height, new Point3d(centerX, centerY + scalefactor, 0), new Point3d(centerX + scalefactor, centerY + scalefactor + Yoffset, 0), new Point3d(centerX + 2 * scalefactor, centerY + scalefactor + Yoffset, 0), 0.15 * scalefactor);
                }
                else
                {
                    DrawLeader_WITH_TXT_ABOVE_BELOW(AcadBlockTableRecord, AcadTransaction, AcadDatabase, prim.SupportName, "CL.EL.(+)" + prim_height, new Point3d(centerX, centerY + scalefactor, 0), new Point3d(centerX + scalefactor, centerY + scalefactor + 1.5 * Yoffset, 0), new Point3d(centerX + 1.5 * scalefactor, centerY + scalefactor + 1.5 * Yoffset, 0), 0.15 * scalefactor);
                }
            }
            else
            {
                if (oddeven % 2 == 0)
                {
                    DrawLeader_WITH_TXT_ABOVE_BELOW(AcadBlockTableRecord, AcadTransaction, AcadDatabase, prim.SupportName, "CL.EL.(+)" + prim_height, new Point3d(centerX, centerY + scalefactor, 0), new Point3d(centerX - scalefactor, centerY + scalefactor + Yoffset, 0), new Point3d(centerX - 2 * scalefactor, centerY + scalefactor + Yoffset, 0), 0.15 * scalefactor);
                }
                else
                {
                    DrawLeader_WITH_TXT_ABOVE_BELOW(AcadBlockTableRecord, AcadTransaction, AcadDatabase, prim.SupportName, "CL.EL.(+)" + prim_height, new Point3d(centerX, centerY + scalefactor, 0), new Point3d(centerX - scalefactor, centerY + scalefactor + 1.5 * Yoffset, 0), new Point3d(centerX - 1.5 * scalefactor, centerY + scalefactor + 1.5 * Yoffset, 0), 0.15 * scalefactor);
                }
            }

        }

        private void ISMCTAG(BlockTableRecord acadBlockTableRecord, Transaction AcadTransaction, Database acadDatabase, Point3d TagPos, [Optional] string ISMCTAGDir, double sectionsize = 100, double xOffset = 2000, double yOffset = 0, string ISASIZE = "ISA 50")
        {

            //for ismc supp
            if (ISMCTAGDir == "L" || ISMCTAGDir == "l")
            {
                DrawLeader(acadBlockTableRecord, AcadTransaction, acadDatabase, "ISMC " + sectionsize, new Point3d(TagPos.X, TagPos.Y + yOffset, 0), new Point3d(TagPos.X - xOffset, TagPos.Y + yOffset, 0));
            }
            else if (ISMCTAGDir == "R" || ISMCTAGDir == "r")
            {
                DrawLeader(acadBlockTableRecord, AcadTransaction, acadDatabase, "ISMC " + sectionsize, new Point3d(TagPos.X, TagPos.Y + yOffset, 0), new Point3d(TagPos.X + xOffset, TagPos.Y + yOffset, 0));
            }
            //for L-section 
            else if (ISMCTAGDir == "LL" || ISMCTAGDir == "ll")
            {
                DrawLeader(acadBlockTableRecord, AcadTransaction, acadDatabase, ISASIZE, new Point3d(TagPos.X, TagPos.Y + yOffset, 0), new Point3d(TagPos.X - xOffset, TagPos.Y + yOffset, 0));
            }
            else if (ISMCTAGDir == "LR" || ISMCTAGDir == "lr")
            {
                DrawLeader(acadBlockTableRecord, AcadTransaction, acadDatabase, ISASIZE, new Point3d(TagPos.X, TagPos.Y + yOffset, 0), new Point3d(TagPos.X + xOffset, TagPos.Y + yOffset, 0));
            }
            //for circular supp
            else if (ISMCTAGDir == "NBL" || ISMCTAGDir == "nbl")
            {
                DrawLeader(acadBlockTableRecord, AcadTransaction, acadDatabase, sectionsize + " NB", new Point3d(TagPos.X, TagPos.Y + yOffset, 0), new Point3d(TagPos.X - xOffset, TagPos.Y + yOffset, 0));
            }
            else if (ISMCTAGDir == "NBR" || ISMCTAGDir == "nbr")
            {
                DrawLeader(acadBlockTableRecord, AcadTransaction, acadDatabase, sectionsize + " NB", new Point3d(TagPos.X, TagPos.Y + yOffset, 0), new Point3d(TagPos.X + xOffset, TagPos.Y + yOffset, 0));
            }
            //for gusset plate
            else if (ISMCTAGDir == "GPL" || ISMCTAGDir == "gpl")
            {
                DrawLeader(acadBlockTableRecord, AcadTransaction, acadDatabase, "GUSSET", new Point3d(TagPos.X, TagPos.Y + yOffset, 0), new Point3d(TagPos.X - xOffset, TagPos.Y + yOffset, 0));
            }
            else if (ISMCTAGDir == "GPR" || ISMCTAGDir == "gpr")
            {
                DrawLeader(acadBlockTableRecord, AcadTransaction, acadDatabase, "GUSSET", new Point3d(TagPos.X, TagPos.Y + yOffset, 0), new Point3d(TagPos.X + xOffset, TagPos.Y + yOffset, 0));
            }

            //for plate
            else if (ISMCTAGDir == "PL" || ISMCTAGDir == "pl")
            {
                DrawLeader(acadBlockTableRecord, AcadTransaction, acadDatabase, "INSERT PLATE ", new Point3d(TagPos.X, TagPos.Y + yOffset, 0), new Point3d(TagPos.X - xOffset, TagPos.Y + yOffset, 0));
            }
            else if (ISMCTAGDir == "PR" || ISMCTAGDir == "pr")
            {
                DrawLeader(acadBlockTableRecord, AcadTransaction, acadDatabase, "INSERT PLATE ", new Point3d(TagPos.X, TagPos.Y + yOffset, 0), new Point3d(TagPos.X + xOffset, TagPos.Y + yOffset, 0));
            }

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
            Support30,
            Support31,
            Support32,
            Support33,
            Support34,
            Support35,
            Support36,
            Support37,
            Support38,
            Support39,
            Support40,
            Support41,
            Support42,
            Support43,
            Support44,
            Support45,
            Support46,
            Support47,
            Support48,
            Support49,
            Support50,
            Support51,
            Support52,
            Support53,
            Support54,
            Support55,
            Support56,
            Support57,
            Support58,
            Support59,

            Support61,
            Support62,
            Support63,
            Support64,
            Support65,
            Support66,
            Support67,
            Support68,
            Support69,
            Support70,
            Support71,
            Support72,
            Support73,
            Support74,
            Support75,
            Support76,
            Support77,
            Support78,
            Support79,
            Support80,
            Support81,
            Support82,
            Support83,

            Support85,
            Support86,
            Support87,
            Support88,
            Support89,
            Support90,
            Support91,
            Support92,
            Support93,
            Support94,
            Support95,
            Support96,
            Support97,
            Support98,
            Support99,
            Support100,
            Support101,
            Support102,
            Support103,
            Support104,
            Support105,
            Support106,
            Support107,
            Support108,

            Support110,





            Support115,






            Elevation,
            S_Type,
            SL_Tyep,
            SR_Tyep,

        }







    }
}
