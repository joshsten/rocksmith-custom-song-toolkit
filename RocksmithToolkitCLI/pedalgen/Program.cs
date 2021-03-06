﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using RocksmithToolkitLib.Tone;

namespace pedalgen
{
    class Program
    {
        static void Main(string[] args)
        {
        	var dirs = new List<string>();
        	for (int i=0; i < args.Length; i++)
        	{dirs.Add(args[i]);}
            var pedals = new List<RsTone>();
            foreach (string file in dirs)
            {
                try
                {
                    var pedalsJson = File.ReadAllText(file);
                    var junk = pedalsJson.IndexOf("\n}");
                    if (junk > -1)
                    {
                        var lastCurly = junk + 2;
                        while (lastCurly < pedalsJson.Length && (pedalsJson[lastCurly] == '}' || pedalsJson[lastCurly] == ' '))
                        {
                            ++lastCurly;
                        }
                        if (lastCurly < pedalsJson.Length)
                        {
                            pedalsJson = pedalsJson.Substring(0, lastCurly);
                        }
                    }
                    pedalsJson.Replace("TonePedalRTPCName", "Key");
                    var filePedals = JsonConvert.DeserializeObject<RsJsonFile<RsTone>>(pedalsJson);
                    if ("GRPedal".Equals(filePedals.ModelName))
                    {
                        pedals.AddRange(filePedals.Entries);
                    }
                }
                catch { }
            }
            var bad = pedals.Where(p => p.Type == null);
            var goodPedals = pedals.Where(p => p.Type != null);
            var toolkitPedals = goodPedals.Select(pedal => pedal.ToPedal());
            foreach (var i in dirs)
            File.WriteAllText(Path.Combine(i, "pedals.json"), JsonConvert.SerializeObject(toolkitPedals, Formatting.Indented));
        }


    }
}
