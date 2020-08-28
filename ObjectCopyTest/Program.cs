﻿using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ObjectCopyTest
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Start Object Copy Test");
            Console.WriteLine("Create Source");
            var number = 100000;
            var stopwatch = new Stopwatch();
            var sources = new List<ITestObject>(number);
            for (var i = 0; i < number; i++)
                sources.Add(new TestObject
                {
                    Number1 = i,
                    Number2 = i + 1,
                    Number3 = i + 2,
                    Number4 = i + 3,
                    Number5 = i + 4,
                    Text1 = "Text 1",
                    Text2 = "Text 2",
                    Text3 = "Text 3",
                    Text4 = "Text 4",
                    Text5 = "Text 5"
                });

            Console.WriteLine("Start direct copy");
            stopwatch.Reset();
            stopwatch.Start();
            {
                var targets = new List<ITestObject>(number);
                foreach (var source in sources)
                {
                    var target = new TestObject();
                    source.CopyContent(target);
                    targets.Add(target);
                }

                var time = stopwatch.Elapsed;
                Console.WriteLine($"Direct copy time {time}");
            }

            stopwatch.Reset();
            stopwatch.Start();
            {
                var targets = new List<ITestObject>(number);
                foreach (var source in sources)
                {
                    var target = new TestObject();
                    source.CopyContentByAttribute(target);
                    targets.Add(target);
                }

                var time = stopwatch.Elapsed;
                Console.WriteLine($"Attribute copy time {time}");
            }

            stopwatch.Reset();
            stopwatch.Start();
            {
                var targets = new List<ITestObject>(number);
                foreach (var source in sources)
                {
                    var target = new TestObject();
                    source.CopyContentByBufferdAttribute(target, target.GetPropertyWithAttribute<CopyableAttribute>());
                    targets.Add(target);
                }

                var time = stopwatch.Elapsed;
                Console.WriteLine($"Attribute bufferd Property infos copy time {time}");
            }

            stopwatch.Reset();
            stopwatch.Start();
            {
                var targets = new List<ITestObject>(number);
                CopyHelper.GenerateCopyClass<ITestObject, TestObject>();
                CopyHelper.GenerateCopyClass<ITestObject, ITestObject>();

                foreach (var source in sources)
                {
                    var target = new TestObject();
                    CopyHelper.CopyWithDom(source, target);
                    targets.Add(target);
                }

                var time = stopwatch.Elapsed;
                Console.WriteLine($"DOM with code generation Property infos copy time {time}");
            }

            stopwatch.Reset();
            stopwatch.Start();
            {
                var targets = new List<ITestObject>(number);
                foreach (var source in sources)
                {
                    var target = new TestObject();
                    CopyHelper.CopyWithDom(source, target);
                    targets.Add(target);
                }

                var time = stopwatch.Elapsed;
                Console.WriteLine($"DOM Property infos copy time {time}");
            }

            stopwatch.Reset();
            stopwatch.Start();
            {
                var targets = new List<ITestObject>(number);
                var action = CopyHelper.ActionCopyWithDom<ITestObject, TestObject>();
                foreach (var source in sources)
                {
                    var target = new TestObject();
                    action(source, target);
                    targets.Add(target);
                }

                var time = stopwatch.Elapsed;
                Console.WriteLine($"DOM Property infos copy time {time}");
            }

            stopwatch.Reset();
            stopwatch.Start();
            {
                var targets = new List<ITestObject>(number);
                foreach (var source in sources)
                {
                    var target = new TestObject();
                    source.CopyContent(target);
                    targets.Add(target);
                }

                var time = stopwatch.Elapsed;
                Console.WriteLine($"Direct copy time {time}");
            }
        }
    }
}