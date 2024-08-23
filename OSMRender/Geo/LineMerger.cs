using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSMRender.Geo
{

    internal static class LineMerger
    {
        private class LineRef
        {
            public long LineId;
        }

        /// <summary>
        /// Merges lines by joining two adjacents lines together if they share the same properties.
        /// </summary>
        /// <typeparam name="T">the type of line being merged, must implement IMergeableLine</typeparam>
        /// <param name="points">points which the lines are made of</param>
        /// <param name="lineToDraw">a mapping from line id to line</param>
        /// <param name="remove">an action used to remove lines that were merged</param>
        /// <exception cref="Exception">only in case of bugs</exception>
        public static void CombineAdjacentFor<T>(IEnumerable<long> points, Dictionary<long, T> lineToDraw, Action<T> remove) where T : IMergeableLine
        {
            Dictionary<long, HashSet<long>> nodeToLine = [];
            Dictionary<long, HashSet<long>> endNodeToLine = [];
            foreach (var drawCmd in lineToDraw.Values)
            {
                if (drawCmd.Nodes.Count >= 2)
                {
                    foreach (var node in drawCmd.Nodes)
                    {
                        if (!nodeToLine.ContainsKey(node.Id))
                        {
                            nodeToLine[node.Id] = [];
                        }
                        nodeToLine[node.Id].Add(drawCmd.MergeableLineId);
                    }

                    var startId = drawCmd.Nodes[0].Id;
                    var endId = drawCmd.Nodes.Last().Id;
                    foreach (var node in new long[] { startId, endId })
                    {
                        if (!endNodeToLine.ContainsKey(node))
                        {
                            endNodeToLine[node] = [];
                        }
                        endNodeToLine[node].Add(drawCmd.MergeableLineId);
                    }
                }
            }

            var lineToRef = lineToDraw.Values.Select(l => new LineRef() { LineId = l.MergeableLineId }).ToDictionary(l => l.LineId);

            var nodeToRef = nodeToLine
                .Select(p => (p.Key, Ref: p.Value.Select(l => lineToRef[l])))
                .ToDictionary(p => p.Key, p => p.Ref);

            var endNodeToRef = endNodeToLine
                .Select(p => (p.Key, Ref: p.Value.Select(l => lineToRef[l])))
                .ToDictionary(p => p.Key, p => p.Ref);

            foreach (var node in points)
            {
                if (!endNodeToRef.ContainsKey(node))
                {
                    continue;
                }

                foreach (var endNode in endNodeToRef[node])
                {
                    var tags = lineToDraw[endNode.LineId].MergeableLineProperties;
                    List<long> matchingLines = [];
                    foreach (var endNode2 in endNodeToRef[node])
                    {
                        if (endNode2.LineId == endNode.LineId) continue;

                        if (lineToDraw[endNode2.LineId].MergeableLineProperties.All(p => tags.ContainsKey(p.Key) && tags[p.Key].Equals(p.Value)))
                        {
                            matchingLines.Add(endNode2.LineId);
                        }
                    }

                    if (matchingLines.Count == 1)
                    {
                        var line1 = lineToDraw[endNode.LineId];
                        var line2 = lineToDraw[matchingLines[0]];

                        //Console.WriteLine($"Merging {line2.MergeableLineId} {line2.Feature} to {line1.MergeableLineId} {line1.Feature}");

                        // Change refs
                        lineToRef.Values.ToList().ForEach(r => {
                            if (r.LineId == line2.MergeableLineId)
                            {
                                r.LineId = line1.MergeableLineId;
                            }
                        });

                        // Remove fromlines
                        lineToDraw.Remove(line2.MergeableLineId);
                        remove.Invoke(line2);

                        // Merge
                        if (line1.Nodes[0].Id == line2.Nodes[0].Id)
                        {
                            line2.Nodes.Reverse();
                            line1.Nodes.InsertRange(0, line2.Nodes);
                        }
                        else if (line1.Nodes.Last().Id == line2.Nodes[0].Id)
                        {
                            line1.Nodes.AddRange(line2.Nodes);
                        }
                        else if (line1.Nodes[0].Id == line2.Nodes.Last().Id)
                        {
                            line1.Nodes.InsertRange(0, line2.Nodes);
                        }
                        else if (line1.Nodes.Last().Id == line2.Nodes.Last().Id)
                        {
                            line2.Nodes.Reverse();
                            line1.Nodes.AddRange(line2.Nodes);
                        }
                        else
                        {
                            //($"cannot merge lines {line1.MergeableLineId}, {line2.MergeableLineId}");
                        }
                    }
                }
            }
        }
    }
}
