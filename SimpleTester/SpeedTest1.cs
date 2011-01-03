﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using BTDB;

namespace SimpleTester
{
    public class SpeedTest1
    {
        readonly Stopwatch _sw = new Stopwatch();
        readonly List<double> _results = new List<double>();

        static IStream CreateTestStream()
        {
            if (false)
            {
                if (File.Exists("data.btdb"))
                    File.Delete("data.btdb");
                return new BTDB.StreamProxy("data.btdb");
            }
            else
            {
                return new ManagedMemoryStream();
            }
        }

        void WarmUp()
        {
            using (var stream = CreateTestStream())
            using (ILowLevelDB db = new LowLevelDB())
            {
                db.Open(stream, false);
                using (var tr = db.StartTransaction())
                {
                    tr.CreateKey(new byte[10000]);
                    tr.SetValueSize(10000);
                    tr.SetValue(new byte[1000], 0, 1000);
                    tr.Commit();
                }
            }
        }

        void DoWork()
        {
            var key = new byte[1000];

            using (var stream = CreateTestStream())
            using (ILowLevelDB db = new LowLevelDB())
            {
                db.Open(stream, false);
                _sw.Restart();
                for (int i = 0; i < 100000; i++)
                {
                    key[504] = (byte)(i % 256);
                    key[503] = (byte)(i / 256 % 256);
                    key[502] = (byte)(i / 256 / 256 % 256);
                    key[501] = (byte)(i / 256 / 256 / 256);
                    using (var tr = db.StartTransaction())
                    {
                        tr.CreateKey(key);
                        tr.SetValueSize(10000);
                        tr.Commit();
                    }
                    _sw.Stop();
                    _results.Add(_sw.Elapsed.TotalMilliseconds);
                    if (i % 1000 == 0) Console.WriteLine("{0} {1}", i, _sw.Elapsed.TotalSeconds);
                    _sw.Start();
                }
                _sw.Stop();
                using (var trStat = db.StartTransaction())
                {
                    Console.WriteLine(trStat.CalculateStats().ToString());
                    Console.WriteLine("Total miliseconds:   {0,15}", _sw.Elapsed.TotalMilliseconds);
                }
            }
        }

        void DoWork2()
        {
            var key = new byte[4];

            using (var stream = CreateTestStream())
            using (ILowLevelDB db = new LowLevelDB())
            {
                db.Open(stream, false);
                _sw.Restart();
                using (var tr = db.StartTransaction())
                {
                    for (int i = 0; i < 1000000; i++)
                    {
                        key[3] = (byte)(i % 256);
                        key[2] = (byte)(i / 256 % 256);
                        key[1] = (byte)(i / 256 / 256 % 256);
                        key[0] = (byte)(i / 256 / 256 / 256);
                        tr.CreateOrUpdateKeyValue(key, key);
                    }
                    tr.Commit();
                }
                _sw.Stop();
                using (var trStat = db.StartTransaction())
                {
                    Console.WriteLine(trStat.CalculateStats().ToString());
                    Console.WriteLine("Insert:              {0,15}ms", _sw.Elapsed.TotalMilliseconds);
                }
                _sw.Restart();
                for (int i = 0; i < 1000000; i++)
                {
                    key[3] = (byte)(i % 256);
                    key[2] = (byte)(i / 256 % 256);
                    key[1] = (byte)(i / 256 / 256 % 256);
                    key[0] = (byte)(i / 256 / 256 / 256);
                    using (var tr = db.StartTransaction())
                    {
                        tr.FindExactKey(key);
                        tr.ReadValue();
                    }
                }
                _sw.Stop();
                using (var trStat = db.StartTransaction())
                {
                    Console.WriteLine(trStat.CalculateStats().ToString());
                    Console.WriteLine("Find+Get in sep tr:  {0,15}ms", _sw.Elapsed.TotalMilliseconds);
                }
                _sw.Restart();
                using (var tr = db.StartTransaction())
                {
                    for (int i = 0; i < 1000000; i++)
                    {
                        key[3] = (byte)(i % 256);
                        key[2] = (byte)(i / 256 % 256);
                        key[1] = (byte)(i / 256 / 256 % 256);
                        key[0] = (byte)(i / 256 / 256 / 256);
                        tr.FindExactKey(key);
                        tr.ReadValue();
                    }
                }
                _sw.Stop();
                using (var trStat = db.StartTransaction())
                {
                    Console.WriteLine(trStat.CalculateStats().ToString());
                    Console.WriteLine("Find+Get in whole tr:{0,15}ms", _sw.Elapsed.TotalMilliseconds);
                }
            }
        }

        void DoWork3()
        {
            var key = new byte[4000];

            using (var stream = CreateTestStream())
            using (ILowLevelDB db = new LowLevelDB())
            {
                db.Open(stream, false);
                _sw.Restart();
                for (int i = 0; i < 30000; i++)
                {
                    using (var tr = db.StartTransaction())
                    {
                        key[3] = (byte)(i % 256);
                        key[2] = (byte)(i / 256 % 256);
                        key[1] = (byte)(i / 256 / 256 % 256);
                        key[0] = (byte)(i / 256 / 256 / 256);
                        tr.CreateKey(key);
                        tr.SetValue(key, 0, 4000);
                        tr.Commit();
                    }
                }
                _sw.Stop();
                using (var trStat = db.StartTransaction())
                {
                    Console.WriteLine(trStat.CalculateStats().ToString());
                    Console.WriteLine("Insert:              {0,15}ms", _sw.Elapsed.TotalMilliseconds);
                }
            }
        }

        void WriteCSV()
        {
            using (var sout = new StreamWriter("data.csv"))
            {
                sout.WriteLine("Order,Time");
                for (int i = 0; i < _results.Count; i += 100)
                {
                    sout.WriteLine("{0},{1}", i, _results[i].ToString(CultureInfo.InvariantCulture));
                }
            }
        }

        public void Test()
        {
            WarmUp();
            DoWork2();
            //WriteCSV();
        }
    }
}