using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Z3_CopyGroup
{
    [Transaction(TransactionMode.Manual)]
    public class CopyGroup : IExternalCommand
    {

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Получаем доступ к Revit, активному документу, базе данных документа
                UIApplication uiapp = commandData.Application;
                UIDocument uidoc = uiapp.ActiveUIDocument;
                Document doc = uidoc.Document;

                // выбор группы
                Reference reference = uidoc.Selection.PickObject(ObjectType.Element, new PickGroupFilter(), "Выберите группу мебели");
                Element element = doc.GetElement(reference);
                Group group = element as Group;
                //Определяем смещение группы относительно центра комнаты
                XYZ groupCenter = GetElementCenter(group);
                Room groupRoom = GetRoomByPoint(doc, groupCenter);
                //TaskDialog.Show("Info", $"Группа {group.Name} находится в помещении {groupRoom.Name}");
                XYZ roomCenter = GetElementCenter(groupRoom);
                XYZ offset = groupCenter - roomCenter;
                TaskDialog.Show("Info", $"Смещение {offset.X}");

                // выбор точки
                XYZ point = uidoc.Selection.PickPoint("Укажите точку вставки");
                Room groupNewRoom = GetRoomByPoint(doc, point);
                //TaskDialog.Show("Info", $"Вставка группы в помещение {groupNewRoom.Name}");
                XYZ roomNewCenter = GetElementCenter(groupNewRoom);
                XYZ groupInsertPoint = roomNewCenter + offset;
                XYZ groupInsertPointXY = new XYZ(groupInsertPoint.X, groupInsertPoint.Y, 0);



                //размещение группы
                Transaction transaction = new Transaction(doc);
                transaction.Start("Копирование группы");
                doc.Create.PlaceGroup(groupInsertPointXY, group.GroupType);
                transaction.Commit();
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch(Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }

            return Result.Succeeded;
        }

        public XYZ GetElementCenter (Element element)
        {
            BoundingBoxXYZ boundingBox = element.get_BoundingBox(null);
            XYZ center = (boundingBox.Min + boundingBox.Max) * 0.5;
            return center;
        }

        public Room GetRoomByPoint (Document document, XYZ point)
        {
            var allRooms = new FilteredElementCollector(document)
                .OfCategory(BuiltInCategory.OST_Rooms)
                .Cast<Room>()
                .ToList();
            foreach (var room in allRooms)
            {
                if (room.IsPointInRoom(point))
                {
                    return room;
                }
            }
            return null;
        }
    }

    public class PickGroupFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            return elem is Group;
            //return elem.Category.Id.IntegerValue.Equals((int)BuiltInCategory.OST_IOSModelGroups);
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }
}
