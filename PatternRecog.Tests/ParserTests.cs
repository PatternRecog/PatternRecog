using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.FSharp.Core;
using Xunit;

namespace PatternRecog.Tests
{
    public class ParserTests
    {
        private readonly ConfigType _Config;

        private readonly string[] _Paths =
        {
            "C:\\test\\epS01E01.avi",
            "C:\\test\\ep101.srt",
            "C:\\test\\epS01E02.avi",
            "C:\\test\\ep102.srt",
            "C:\\test\\epS01E10.avi",
            "C:\\test\\epS01E10.srt",
            "C:\\test\\epS01E11.avi",
            "C:\\test\\ep111.srt",
            "C:\\test\\ep112.avi", // NO DUB

            "C:\\test\\epS02E01.avi",
            "C:\\test\\epS02E01.srt",
            "C:\\test\\ep211.srt", // NO VID
            "C:\\test\\epS02E12.avi",
            "C:\\test\\epS02E12.srt",
        };

        private static IEnumerable<string> Generate(int seed, int nb = 10)
        {
            Random rnd = new Random(seed);
            const int nbSeason = 2;
            const int nbEpPerSeason = 12;
            for (int s = 1; s <= nbSeason; s++)
            {
                for (int e = 1; e <= nbEpPerSeason; e++)
                {
                    yield return GenName(rnd, s, e) + ".avi";
                    if (rnd.NextDouble() < 0.9)
                        yield return GenName(rnd, s, e) + ".srt";
                }
            }
        }

        private static string GenName(Random rnd, int s, int e)
        {
            string fmt;
            if (rnd.NextDouble() > 0.5)
                fmt = "C:\\epS{0:D2}E{1:D2}";
            else
                fmt = "C:\\ep{0}{1:D2}";
            string path = string.Format(fmt, s, e);
            return path;
        }

        public ParserTests()
        {
            _Config = new ConfigType(
                new[] { ".avi", ".mp4" },
                new[] { @"S(?<season>\d+)E(?<episode>\d+)", @"(?<season>\d)(?<episode>\d\d)" },
                true);
        }

        [Fact]
        public void ParseVideo()
        {
            var d = Desc.parse(_Config, "C:\\asd\\qwe.S03E04.mp4");
            Assert.Equal(new DescType(d.Value.path, Kind.Video, 3, 4), d.Value);
        }
        [Fact]
        public void ParseSub()
        {
            var d = Desc.parse(_Config, "C:\\asd\\qwe.S03E04.srt");
            Assert.Equal(new DescType(d.Value.path, Kind.Subtitle, 3, 4), d.Value);
        }

        [Fact]
        public void ParseSub2ndRegex()
        {
            var d = Desc.parse(_Config, "C:\\asd\\qwe.304.srt");
            Assert.Equal(new DescType(d.Value.path, Kind.Subtitle, 3, 4), d.Value);
        }

        [Fact]
        public void Match()
        {
            var p1 = "C:\\asd\\qwe.S03E04.mp4";
            var p2 = "C:\\asd\\qwe.S03E04.srt";
            var df = Desc.parse(_Config, p1);
            var ds = Desc.parse(_Config, p2);
            var matchTypes = Matches.fromPaths(_Config, new[] { p1, p2 });
            Assert.Equal(1, matchTypes.Length);
            Assert.Equal(new MatchType(ds.Value, df.Value), matchTypes[0]);
            //Assert.Equal(new DescType(d.path, Kind.Subtitle, 3, 4), d);
        }

        [Fact]
        public void MatchRnd()
        {
            var matchTypes = Matches.fromPaths(_Config, _Paths);
            Assert.Equal(6, matchTypes.Length);
            //Assert.Equal(new DescType(d.path, Kind.Subtitle, 3, 4), d);
        }

        [Fact]
        public void GenFiles()
        {
            foreach (var path in _Paths)
            {
                File.WriteAllText(path, path);
            }
        }

        [Fact]
        public void RenameFiles()
        {
            var matchTypes = Matches.fromPaths(_Config, _Paths);
            var res = Matches.rename(_Config, matchTypes);
        }
    }
}
