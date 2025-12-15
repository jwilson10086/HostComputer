// 文件：CsvHelperEx.cs
using CustomControls.Controls;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace CustomControls.Helpers
{
    public static class CsvHelperEx
    {
        // Header fields
        private static readonly string[] _cols = new[]
        {
            "Station","J1","J2","J3","J4","J5","J6","J7","J8"
        };

        public static DataTable ReadPoseTable(string path)
        {
            var dt = new DataTable();
            foreach (var c in _cols) dt.Columns.Add(c);

            if (!File.Exists(path)) return dt;

            var lines = File.ReadAllLines(path);
            foreach (var line in lines.Skip(1)) // skip header
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var parts = line.Split(',');
                var row = dt.NewRow();
                for (int i = 0; i < _cols.Length; i++)
                {
                    row[i] = i < parts.Length ? parts[i] : "";
                }
                dt.Rows.Add(row);
            }
            return dt;
        }

        public static List<PoseData> ReadAllPoses(string path)
        {
            var dt = ReadPoseTable(path);
            var list = new List<PoseData>();
            foreach (DataRow r in dt.Rows)
            {
                double Parse(string k)
                {
                    if (double.TryParse(r[k]?.ToString(), out var v)) return v;
                    return 0.0;
                }

                var p = new PoseData
                {
                    Station = r["Station"]?.ToString() ?? "",
                    
                    J1 = Parse("J1"),
                    J2 = Parse("J2"),
                    J3 = Parse("J3"),
                    J4 = Parse("J4"),
                    J5 = Parse("J5"),
                    J6 = Parse("J6"),
                    J7 = Parse("J7"),
                    J8 = Parse("J8"),
                };
                list.Add(p);
            }
            return list;
        }

        public static void WriteAllPoses(string path, IEnumerable<PoseData> poses)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
            using var sw = new StreamWriter(path, false);
            sw.WriteLine(string.Join(",", _cols));
            foreach (var p in poses)
            {
                var line = string.Join(",",
                    p.Station,
                    p.J1, p.J2, p.J3,
                    p.J4, p.J5, p.J6,
                    p.J7, p.J8
                );
                sw.WriteLine(line);
            }
        }

        public static PoseData FindPose(string path, string station)
        {
            var all = ReadAllPoses(path);
            return all.FirstOrDefault(p => string.Equals(p.Station, station, StringComparison.OrdinalIgnoreCase));
        }

        public static void UpsertPose(string path, PoseData pose)
        {
            var list = ReadAllPoses(path);
            var idx = list.FindIndex(p => string.Equals(p.Station, pose.Station, StringComparison.OrdinalIgnoreCase));
            if (idx >= 0) list[idx] = pose;
            else list.Add(pose);
            WriteAllPoses(path, list);
        }

        // Ensure file exists with header (optional: create empty)
        public static void EnsureFile(string path)
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            if (!File.Exists(path))
            {
                using var sw = new StreamWriter(path, false);
                sw.WriteLine(string.Join(",", _cols));
            }
        }
    }
}
