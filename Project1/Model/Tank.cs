using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project1.Model
{
    public class Tank
    {
        public double Radius { get; set; }
        public double Thickness { get; set; }

        public double Height { get; set; }


        public bool DrawTank()
        {
            try
            {

                Document AcadDoc = null;
                Transaction AcadTransaction = null;
                BlockTable AcadBlockTable = null;
                BlockTableRecord AcadBlockTableRecord = null;
                Database AcadDatabase = null;

                AcadDoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;

                AcadDatabase = AcadDoc.Database;
                using (AcadDoc.LockDocument())
                {
                    AcadTransaction = AcadDatabase.TransactionManager.StartTransaction();

                    AcadBlockTable = AcadTransaction.GetObject(AcadDatabase.BlockTableId,
                                                                  OpenMode.ForWrite) as BlockTable;

                    AcadBlockTableRecord = AcadTransaction.GetObject(AcadBlockTable[BlockTableRecord.ModelSpace],
                                                   OpenMode.ForWrite) as BlockTableRecord;

                    using (Circle acCirc = new Circle())
                    {
                        acCirc.Center = new Point3d(0, 0, 0);
                        acCirc.Radius = Radius;
                        acCirc.Thickness = Thickness;

                        // Add the new object to the block table record and the transaction
                        AcadBlockTableRecord.AppendEntity(acCirc);
                        AcadTransaction.AddNewlyCreatedDBObject(acCirc, true);
                    }

                    using (Solid3d acSolid = new Solid3d())
                    {
                        acSolid.CreateFrustum(Height, Radius, Radius, Radius);
                        acSolid.Visible = true;
                        //acCirc.Radius = Radius;
                        //acCirc.Thickness = Thickness;

                        // Add the new object to the block table record and the transaction
                        AcadBlockTableRecord.AppendEntity(acSolid);
                        AcadTransaction.AddNewlyCreatedDBObject(acSolid, true);
                    }


                    AcadTransaction.Commit();
                }
            }
            catch(Exception)
            {
                return false;
            }

            return true;
        }
    }
}
