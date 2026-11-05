using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using App = Autodesk.AutoCAD.ApplicationServices.Application;

namespace AutoCAD2024McpAddin;

public static class TitleBlockManager
{
    public static string DumpActiveLayoutTitleBlock(string? outputPath = null)
    {
        var doc = App.DocumentManager.MdiActiveDocument;
        if (doc == null)
        {
            throw new InvalidOperationException("No active drawing document open.");
        }

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            outputPath = Path.Combine(ServerManager.SetupDirectory, "tb_dump2.txt");
        }

        var sb = new StringBuilder();
        var ed = doc.Editor;
        var db = doc.Database;

        using (var docLock = doc.LockDocument())
        using (var tr = db.TransactionManager.StartTransaction())
        {
            var currentSpace = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForRead);
            var layout = (Layout)tr.GetObject(currentSpace.LayoutId, OpenMode.ForRead);

            sb.AppendLine($"# Title Block Dump - Layout: {layout.LayoutName} - Drawing: {Path.GetFileName(doc.Name)}");
            sb.AppendLine($"# Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("# Format: Handle|Space|Type|Layer|Details");

            int entityCount = 0;
            int blkCount = 0;
            int textCount = 0;

            foreach (ObjectId id in currentSpace)
            {
                var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                if (ent == null) continue;
                entityCount++;

                string handle = ent.Handle.ToString();
                string layer = ent.Layer;
                string typeName = ent.GetType().Name;

                if (ent is DBText dbText)
                {
                    textCount++;
                    var pt = dbText.Position;
                    sb.AppendLine($"{handle}|PS|{typeName}|{layer}|{pt.X:F2},{pt.Y:F2}|{dbText.TextString}");
                }
                else if (ent is MText mText)
                {
                    textCount++;
                    var pt = mText.Location;
                    sb.AppendLine($"{handle}|PS|{typeName}|{layer}|{pt.X:F2},{pt.Y:F2}|{mText.Contents}");
                }
                else if (ent is BlockReference blkRef)
                {
                    blkCount++;
                    var btr = (BlockTableRecord)tr.GetObject(blkRef.BlockTableRecord, OpenMode.ForRead);
                    string blkName = btr.Name;
                    bool isXref = btr.IsFromExternalReference;
                    var pt = blkRef.Position;

                    sb.AppendLine($"{handle}|PS|AcDbBlockReference|{layer}|{blkName}|XREF={(isXref ? "Y" : "N")}|{pt.X:F2},{pt.Y:F2}");

                    // Dump block attributes
                    foreach (ObjectId attId in blkRef.AttributeCollection)
                    {
                        var att = tr.GetObject(attId, OpenMode.ForRead) as AttributeReference;
                        if (att != null)
                        {
                            sb.AppendLine($"  {att.Handle}|ATTR|{att.Tag}|{att.Layer}|{att.TextString}");
                        }
                    }

                    // Dump internal definition entities if title block
                    if (blkName.IndexOf("TITLE", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        blkName.IndexOf("TB", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        blkName.IndexOf("BORDER", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        sb.AppendLine($"== DEF {blkName}");
                        foreach (ObjectId subId in btr)
                        {
                            var subEnt = tr.GetObject(subId, OpenMode.ForRead) as Entity;
                            if (subEnt == null) continue;

                            if (subEnt is DBText subTxt)
                            {
                                sb.AppendLine($"{subEnt.Handle}|DBText|{subTxt.Layer}|{subTxt.Position.X:F2},{subTxt.Position.Y:F2}|{subTxt.TextString}");
                            }
                            else if (subEnt is MText subMtx)
                            {
                                sb.AppendLine($"{subEnt.Handle}|MText|{subMtx.Layer}|{subMtx.Location.X:F2},{subMtx.Location.Y:F2}|{subMtx.Contents}");
                            }
                            else if (subEnt is AttributeDefinition attDef)
                            {
                                sb.AppendLine($"{subEnt.Handle}|AttrDef|{attDef.Layer}|{attDef.Tag}|{attDef.TextString}");
                            }
                        }
                    }
                }
            }

            tr.Commit();
            File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);

            ed.WriteMessage($"\n[Title Block] Dump complete: {entityCount} entities, {blkCount} blocks, {textCount} texts -> {outputPath}");
        }

        return outputPath;
    }

    public static bool UpdateRevision(string newRev, string? revDesc = null, string? revDate = null)
    {
        var doc = App.DocumentManager.MdiActiveDocument;
        if (doc == null)
        {
            throw new InvalidOperationException("No active drawing document open.");
        }

        var ed = doc.Editor;
        var db = doc.Database;
        int updatedCount = 0;

        using (var docLock = doc.LockDocument())
        using (var tr = db.TransactionManager.StartTransaction())
        {
            var currentSpace = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForRead);

            // Update attributes in block references
            foreach (ObjectId id in currentSpace)
            {
                if (tr.GetObject(id, OpenMode.ForRead) is BlockReference blkRef)
                {
                    foreach (ObjectId attId in blkRef.AttributeCollection)
                    {
                        var att = tr.GetObject(attId, OpenMode.ForWrite) as AttributeReference;
                        if (att != null)
                        {
                            string tag = att.Tag.ToUpperInvariant();
                            if (tag == "REV" || tag == "REVISION" || tag == "REV_NO" || tag == "REVISION_NO")
                            {
                                att.TextString = newRev;
                                updatedCount++;
                            }
                            else if (!string.IsNullOrEmpty(revDesc) && (tag.Contains("DESC") || tag.Contains("DESCRIPTION")))
                            {
                                att.TextString = revDesc;
                                updatedCount++;
                            }
                            else if (!string.IsNullOrEmpty(revDate) && (tag.Contains("DATE")))
                            {
                                att.TextString = revDate;
                                updatedCount++;
                            }
                        }
                    }
                }
            }

            tr.Commit();
        }

        doc.Editor.Regen();
        ed.WriteMessage($"\n[Title Block] Revision updated to '{newRev}' ({updatedCount} attributes updated).");
        return updatedCount > 0;
    }
}
