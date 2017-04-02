using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static UsermapConverter.Usermap;

namespace UsermapConverter
{
    class UsermapConversion
    {
        public static void ConvertUsermap(string srcFileName, string destFileName)
        {
            using (var srcStream = new EndianStream(File.Open(srcFileName, FileMode.Open, FileAccess.ReadWrite), EndianStream.EndianType.LittleEndian))
            using (var outStream = new EndianStream(File.Open(destFileName, FileMode.Create, FileAccess.ReadWrite), EndianStream.EndianType.LittleEndian))
            {
                SandboxMap srcMap = Usermap.DeserializeSandboxMap(srcStream);

                if (!File.Exists(GetCanvasFileName(srcMap.MapId)))
                    return;

                using (var canvasStream = new EndianStream(File.OpenRead(GetCanvasFileName(srcMap.MapId)), EndianStream.EndianType.LittleEndian))
                {
                    var canvasMap = Usermap.DeserializeSandboxMap(canvasStream);

                    var newBudgetEntries = new List<BudgetEntry>();
                    var newPlacements = new List<SandboxPlacement>();

                    for (int i = 0; i < 640; i++)
                        newPlacements.Add(canvasMap.Placements[i].Clone());

                    for (int i = 0; i < srcMap.ScnrObjectCount; i++)
                    {
                        var srcPlacement = srcMap.Placements[i];

                        if (srcPlacement.BudgetIndex == -1)
                            continue;

                        var tagIndex = srcMap.Budget[srcPlacement.BudgetIndex].TagIndex;

                        var newBudgetIndex = canvasMap.Budget.FindIndex(e => e.TagIndex == tagIndex);

                        var substitutePlacements = canvasMap.Placements
                            .Where((x, j) =>
                            (int)(newPlacements[j].PlacementFlags & (1 << 5)) != 0 && // deleted
                            x.BudgetIndex == newBudgetIndex).ToList();

                        var newPlacementIndex2 = canvasMap.Placements.IndexOf(substitutePlacements.FirstOrDefault());
                        if (newPlacementIndex2 == -1)
                            continue;

                        newPlacements[newPlacementIndex2] = srcPlacement.Clone();
                        newPlacements[newPlacementIndex2].PlacementFlags |= (1 << 1); //touched
                    }

                    var newPlacementIndex = canvasMap.ScnrObjectCount;
                    for (var i = srcMap.ScnrObjectCount; i < 640; i++, newPlacementIndex++)
                    {
                        var srcPlacement = srcMap.Placements[i];

                        if (srcPlacement.BudgetIndex == -1)
                            continue;

                        var tagIndex = srcMap.Budget[srcPlacement.BudgetIndex].TagIndex;

                        var newBudgetIndex = canvasMap.Budget.FindIndex(e => e.TagIndex == tagIndex);
                        if (newBudgetIndex == -1)
                        {
                            var n = newBudgetEntries.FindIndex(e => e.TagIndex == tagIndex);

                            if (n == -1)
                            {
                                var entry = new BudgetEntry()
                                {
                                    TagIndex = tagIndex,
                                    Cost = -1,
                                    RuntimeMin = 0,
                                    RuntimeMax = 0,
                                    CountOnMap = 0,
                                    DesignTimeMax = 0
                                };

                                newBudgetIndex = canvasMap.BudgetEntryCount + newBudgetEntries.Count();
                                newBudgetEntries.Add(entry);

                                Console.WriteLine("injecting 0x{0} {1}", tagIndex.ToString("X"), newBudgetIndex);
                            }
                            else
                            {
                                newBudgetIndex = canvasMap.BudgetEntryCount + n;
                            }
                        }


                        var newPlacement = newPlacements[newPlacementIndex] = srcPlacement.Clone();
                        newPlacement.BudgetIndex = newBudgetIndex;
                    }

                    canvasStream.SeekTo(0);
                    canvasStream.Stream.CopyTo(outStream.Stream);

                    outStream.SeekTo(0x30);
                    srcStream.SeekTo(0x30);
                    outStream.WriteBytes(srcStream.ReadBytes(0x108)); //chdr
                    outStream.WriteBytes(srcStream.ReadBytes(0x140)); //map

                    outStream.SeekTo(0x242);
                    outStream.WriteInt16(canvasMap.ScnrObjectCount);
                    outStream.WriteInt16((short)(canvasMap.ScnrObjectCount + (srcMap.TotalObjectCount - srcMap.ScnrObjectCount)));
                    outStream.WriteInt16((short)(canvasMap.BudgetEntryCount + newBudgetEntries.Count + 1));

                    outStream.SeekTo(0x278);
                    foreach (var placement in newPlacements)
                        Usermap.SerializePlacement(outStream, placement);

                    outStream.SeekTo(0xD498 + canvasMap.BudgetEntryCount * 0xc);
                    foreach (var entry in newBudgetEntries)
                        Usermap.SerializeBudgetEntry(outStream, entry);
                }
            }
        }

        public static string GetCanvasFileName(int mapId)
        {
            return Path.Combine(@"canvas", string.Format("{0}_sandbox.map", mapId));
        }
    }
}
