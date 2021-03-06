﻿//SQLIOCommandGenerator thrown together by Wes Brown
//I use this to built test batches then parse the output with
//SQLIOParser
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using NDesk.Options;

namespace SQLIOCommandGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine();
            Console.WriteLine("SQLIOCommandGenerator 0.20");
            Console.WriteLine();
            Console.WriteLine("We assume -F<paramfile> -LS");
            Console.WriteLine("-d,-R,-f,-p,-a,-i,-m,-u,-S,-v, -t not implemented");
            Console.WriteLine();
            int parseerr = ParseCommandLine(args);
            if (parseerr == 1)
                return;

            var commandFile = new StringBuilder();

            var lIOType = new List<string>();
            var lIOPattern = new List<string>();
            var lIOSize = new List<string>();
            var lOutstandingIO = new List<string>();
            var lBuffering = new List<string>();
            string commandLineCall;

            //header and inital setup
            commandFile.Append(@":: Generated by SQLIOCommandGenerator").AppendLine();
            commandFile.Append(@":: This relies on SQLIO.exe being in the same directory.").AppendLine();
            commandFile.AppendLine();
            commandFile.Append(@"@ECHO OFF").AppendLine();
            commandFile.AppendLine();

            if (GlobalVariables.Seconds != null)
            {
                if (GlobalVariables.CoolOffSeconds != null)
                {
                    commandLineCall = ":: " + GlobalVariables.SQLIORunFile + " c:\\paramfile.txt c:\\outputfile.txt \"description of the tests\" \r\n:: param1 sqlio parameter file, param2 output of each test to single txt file, param3 test description";
                    commandFile.Append(commandLineCall).AppendLine();
                    commandFile.AppendLine();
                    commandFile.Append(@"SET paramfile=%1").AppendLine();
                    commandFile.Append(@"SET outfile=%2").AppendLine();
                    commandFile.Append(@"SET runtime=" + GlobalVariables.Seconds).AppendLine();
                    commandFile.Append(@"SET cooloff=" + GlobalVariables.CoolOffSeconds).AppendLine();
                    commandFile.Append(@"SET desc=%3").AppendLine();
                }
                else
                {
                    commandLineCall = ":: " + GlobalVariables.SQLIORunFile + " c:\\paramfile.txt c:\\outputfile.txt 5 \"description of the tests\" \r\n:: param1 sqlio parameter file, param2 output of each test to single txt file, param3 cool down period in seconds, param4 test description";
                    commandFile.Append(commandLineCall).AppendLine(); commandFile.AppendLine();
                    commandFile.Append(@"SET paramfile=%1").AppendLine();
                    commandFile.Append(@"SET outfile=%2").AppendLine();
                    commandFile.Append(@"SET runtime=" + GlobalVariables.Seconds).AppendLine();
                    commandFile.Append(@"SET cooloff=%3").AppendLine();
                    commandFile.Append(@"SET desc=%4").AppendLine();
                }
            }
            else
            {
                if (GlobalVariables.CoolOffSeconds != null)
                {
                    commandLineCall = ":: " + GlobalVariables.SQLIORunFile + " c:\\paramfile.txt c:\\outputfile.txt 60 \"description of the tests\" \r\n:: param1 sqlio parameter file, param2 output of each test to single txt file, param3 single test run in seconds, param4 test description";
                    commandFile.Append(commandLineCall).AppendLine();
                    commandFile.AppendLine();
                    commandFile.Append(@"SET paramfile=%1").AppendLine();
                    commandFile.Append(@"SET outfile=%2").AppendLine();
                    commandFile.Append(@"SET runtime=%3").AppendLine();
                    commandFile.Append(@"SET cooloff=" + GlobalVariables.CoolOffSeconds).AppendLine();
                    commandFile.Append(@"SET desc=%4").AppendLine();
                }
                else
                {
                    commandLineCall = ":: " + GlobalVariables.SQLIORunFile + " c:\\paramfile.txt c:\\outputfile.txt 60 5 \"description of the tests\" \r\n:: param1 sqlio parameter file, param2 output of each test to single txt file, param3 single test run in seconds, param4 cool down period in seconds, param5 test description";
                    commandFile.Append(commandLineCall).AppendLine(); commandFile.AppendLine();
                    commandFile.Append(@"SET paramfile=%1").AppendLine();
                    commandFile.Append(@"SET outfile=%2").AppendLine();
                    commandFile.Append(@"SET runtime=%3").AppendLine();
                    commandFile.Append(@"SET cooloff=%4").AppendLine();
                    commandFile.Append(@"SET desc=%5").AppendLine();
                }
            }
            commandFile.Append(@"ECHO ComputerName: %COMPUTERNAME% > %OUTFILE%").AppendLine();
            commandFile.Append(@"ECHO Date: %DATE% %TIME% >> %OUTFILE%").AppendLine();
            commandFile.Append(@"ECHO Runtime: %RUNTIME% >> %OUTFILE%").AppendLine();
            commandFile.Append(@"ECHO Cool Off: %COOLOFF% >> %OUTFILE%").AppendLine();
            commandFile.Append(@"ECHO Parameters File: %PARAMFILE% >> %OUTFILE%").AppendLine();
            commandFile.Append(@"ECHO Description: %DESC% >> %OUTFILE%").AppendLine();
            commandFile.Append(@"ECHO Test Start >> %OUTFILE%").AppendLine();
            commandFile.AppendLine();

            
            //Buffering //None, All, Hardware, Software
            switch (GlobalVariables.Buffering.ToUpper())
            {
                case "ALL":
                    lBuffering.Add("-BY");
                    lBuffering.Add("-BN");
                    lBuffering.Add("-BH");
                    lBuffering.Add("-BS");
                    break;
                case "NONE":
                    lBuffering.Add("-BN");
                    break;
                case "HARDWARE":
                    lBuffering.Add("-BH");
                    break;
                case "SOFTWARE":
                    lBuffering.Add("-BS");
                    break;

                default:
                    Console.WriteLine("Default case");
                    break;
            }

            //iopattern //both, random, sequential
            switch (GlobalVariables.IOPattern.ToUpper())
            {
                case "BOTH":
                    lIOPattern.Add("-frandom");
                    lIOPattern.Add("-fsequential");
                    break;
                case "RANDOM":
                    lIOPattern.Add("-frandom");
                    break;
                case "SEQUENTIAL":
                    lIOPattern.Add("-fsequential");
                    break;

                default:
                    lIOPattern.Add("-frandom");
                    break;
            }

            //IOType //Read,Write or Both
            switch (GlobalVariables.IOType.ToUpper())
            {
                case "BOTH":
                    lIOType.Add("-kW");
                    lIOType.Add("-kR");
                    break;
                case "RANDOM":
                    lIOType.Add("-kR");
                    break;
                case "SEQUENTIAL":
                    lIOType.Add("-kW");
                    break;

                default:
                    lIOType.Add("-kR");
                    break;
            }

            //io sizes via list or range
            if (GlobalVariables.IOSizeList != null)
            {
                if (GlobalVariables.IOSizeList.Contains(","))
                {
                    foreach (string s in GlobalVariables.IOSizeList.Split(','))
                    {
                        lIOSize.Add("-b" + s);
                    }
                }
                else
                    lIOSize.Add("-b" + GlobalVariables.IOSizeList);
            }
            else if(GlobalVariables.IOSizeStart != null && GlobalVariables.IOSizeEnd !=null && GlobalVariables.IOSizeIncrament !=null)
            {
                int iIOSize = Convert.ToInt32(GlobalVariables.IOSizeStart);
                while (iIOSize <= Convert.ToInt32(GlobalVariables.IOSizeEnd))
                {
                    lIOSize.Add("-b"+Convert.ToString(iIOSize));
                    iIOSize = (iIOSize * Convert.ToInt32(GlobalVariables.IOSizeIncrament));
                    
                }
            }

            //queue depth via list or range
            if (GlobalVariables.OutstandingIOList != null)
            {
                if (GlobalVariables.OutstandingIOList.Contains(","))
                {
                    foreach (string s in GlobalVariables.OutstandingIOList.Split(','))
                    {
                        lOutstandingIO.Add("-o" + s);
                    }
                }
                else
                    lOutstandingIO.Add("-o" + GlobalVariables.OutstandingIOList);
            }
            else if (GlobalVariables.OutstandingIOStart != null && GlobalVariables.OutstandingIOEnd != null && GlobalVariables.OutstandingIOIncrament != null)
            {
                int iOutstandingIO = Convert.ToInt32(GlobalVariables.OutstandingIOStart);
                while (iOutstandingIO <= Convert.ToInt32(GlobalVariables.OutstandingIOEnd))
                {
                    lOutstandingIO.Add("-o" + Convert.ToString(iOutstandingIO));
                    iOutstandingIO = (iOutstandingIO * Convert.ToInt32(GlobalVariables.OutstandingIOIncrament));
                }
            }

            //Threads
            //Seconds
            //SQLIORunFile
            float tCount = 0;
            foreach (string buff in lBuffering)
            {
                foreach (string iop in lIOPattern)
                {
                    foreach (string iot in lIOType)
                    {
                        foreach (string ios in lIOSize)
                        {
                            foreach (string iost in lOutstandingIO)
                            {
                                tCount++;
                                commandFile.AppendLine();
                                commandFile.Append("ECHO Command Line: sqlio " + iot + " -s%RUNTIME% " + iop + " " + ios + " " + iost + " -LS " + buff + " -F%PARAMFILE% >> %OUTFILE%").AppendLine();
                                commandFile.Append("sqlio " + iot + " -s%RUNTIME% " + iop + " " + ios + " " + iost + " -LS " + buff + " -F%PARAMFILE% >> %OUTFILE%").AppendLine();
                                commandFile.Append("timeout /T %COOLOFF%").AppendLine();
                            }
                        }
                    }
                }
            }
            commandFile.AppendLine();
            commandFile.Append(@"ECHO End Date: %DATE% %TIME% >> %OUTFILE%").AppendLine();

            Console.WriteLine();
            Console.WriteLine("Sample command call");
            Console.WriteLine(commandLineCall.Replace(":: ", ""));
            Console.WriteLine();

            if (GlobalVariables.Seconds != null && GlobalVariables.CoolOffSeconds != null)
            {
                tCount = (tCount * Convert.ToInt32(GlobalVariables.Seconds) + Convert.ToInt32(GlobalVariables.CoolOffSeconds)) / 60;
            }
            if (GlobalVariables.Seconds == null && GlobalVariables.CoolOffSeconds != null)
                tCount = (tCount * Convert.ToInt32(GlobalVariables.CoolOffSeconds)) / 60;

            if (GlobalVariables.Seconds != null && GlobalVariables.CoolOffSeconds == null)
                tCount = (tCount * Convert.ToInt32(GlobalVariables.Seconds) + 5) / 60;

            if (GlobalVariables.Seconds == null && GlobalVariables.CoolOffSeconds == null)
                tCount = (tCount * 65) / 60;

            if (tCount > 300)
            {
                tCount = tCount/60;
                commandFile.Append(@":: This batch will take approximately " + tCount + " Hours to Execute.").AppendLine();
                Console.WriteLine("This batch will take approximately " + tCount + " Hours to Execute.");
            }
            else
            {
                commandFile.Append(@":: This batch will take approximately " + tCount + " Minutes to Execute.").AppendLine();
                Console.WriteLine("This batch will take approximately " + tCount + " Minutes to Execute.");
            }

            //write out our batch file
            using (var sw = new StreamWriter(GlobalVariables.SQLIORunFile))
            {
                sw.Write(commandFile.ToString());
            }
        }
        static public int ParseCommandLine(string[] args)
        {
            var showHelp = false;

            //stuff I've implemented as options in the generator see below.
            //Usage: obj\i386\sqlio [options] [<filename>...]
            //        [options] may include any of the following:
            //        -k<R|W>                 kind of IO (R=reads, W=writes)
            //not implemented            //        -t<threads>             number of threads
            //        -s<secs>                number of seconds to run
            //not implemented            //        -d<drive1>..<driveN>    use same filename on each drive letter given
            //not implemented            //        -R<drive1>,,<driveN>    raw drive letters/number on which to run
            //not implemented            //        -f<stripe factor>       stripe size in blocks, random, or sequential
            //not implemented            //        -p[I]<cpu affinity>     cpu number for affinity (0 based)(I=ideal)
            //not implemented            //        -a[R[I]]<cpu mask>      cpu mask for (R=roundrobin (I=ideal)) affinity
            //        -o<#outstanding>        depth to use for completion routines
            //        -b<io size(KB)>         IO block size in KB
            //not implemented            //        -i<#IOs/run>            number of IOs per IO run
            //not implemented            //        -m<[C|S]><#sub-blks>    do multi blk IO (C=copy, S=scatter/gather)
            //not implemented            //        -L<[S|P][i|]>           latencies from (S=system, P=processor) timer
            //not implemented            //        -U[p]                   report system (p=per processor) utilization
            //        -B<[N|Y|H|S]>           set buffering (N=none, Y=all, H=hdwr, S=sfwr)
            //not implemented            //        -S<#blocks>             start I/Os #blocks into file
            //not implemented            //        -v1.1.1                 I/Os runs use same blocks, as in version 1.1.1
            //not implemented            //        -64                     use 64 bit memory operations
            //not implemented            //        -F<paramfile>           read parameters from <paramfile>


            var p = new OptionSet
                        {

            { "f:|iopattern:", "Random, Sequential or Both",
              v => GlobalVariables.IOPattern = v },

            { "k:|iotype:", "Read,Write or Both",
              v => GlobalVariables.IOType = v },

              { "s:|seconds:", "Number of seconds to run each test 1(60) to 10(600) minutes is normal",
              v => GlobalVariables.Seconds = v},

              { "c:|cooldown:", "Number of seconds pause between tests suggested minimum is 5 seconds.",
              v => GlobalVariables.CoolOffSeconds = v},

              { "os:|outstandingiostart:", "Starting number of outstanding IOs 1",
              v => GlobalVariables.OutstandingIOStart = v},

              { "oi:|outstandingioincrament:", "Multiply Outstanding IO start by X i.e 2",
              v => GlobalVariables.OutstandingIOIncrament = v},

              { "oe:|outstandingioend:", "Ending Number of outstanding IOs i.e. 64",
              v => GlobalVariables.OutstandingIOEnd = v},

              { "ol:|outstandingiolist:", "Specific Outstanding IO List. This is Queue Depth WARNING Setting this value to high can generate out of mememory errors i.e. 1,2,4,8,16,32,64,128",
              v => GlobalVariables.OutstandingIOList = v },

              { "oss:|iosizestart", "Starting Size of the IO request in kilobytes i.e. 1",
              v => GlobalVariables.IOSizeStart = v },

              { "osi:|iosizeincrament:", "Multiply IO size by X in kilobytes i.e. 2",
              v => GlobalVariables.IOSizeIncrament = v },

              { "ose:|iosizeend:", "Ending number of outstanding IOs in kilobytes i.e. 1024",
              v => GlobalVariables.IOSizeEnd = v },

              { "osl:|iosizeList:", "Specific IO Sizes in kilobytes i.e. 1,2,4,8,16,32,64,128,256,512,1024",
              v => GlobalVariables.IOSizeList = v },

              { "b:|buffering:", "Set the type of buffering None, All, Hardware, Software. None is the default for SQL Server",
              v => GlobalVariables.Buffering = v},

              { "bat:|sqliobatchfilename:", "The name of the output batch file that will be created",
              v => GlobalVariables.SQLIORunFile = v},

              { "?|h|help",  "show this message and exit", 
              v => showHelp = v != null },
            };

            try
            {
                p.Parse(args);
            }

            catch (OptionException e)
            {
                Console.Write("SQLIOCommandGenerator Error: ");
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `SQLIOCommandGenerator --help' for more information.");
                return 1;
            }

            if (args.Length == 0)
            {
                ShowHelp("Error: please specify some commands....", p);
                return 1;
            }

            if (GlobalVariables.SQLIORunFile == null && !showHelp)
            {
                ShowHelp("Error: You must specify a file to write to --bat.", p);
                return 1;
            }

            if (showHelp)
            {
                ShowHelp(p);
                return 1;
            }
            return 0;
        }

        static void ShowHelp(string message, OptionSet p)
        {
            Console.WriteLine(message);
            Console.WriteLine("Usage: SQLIOCommandGenerator [OPTIONS]");
            Console.WriteLine("Generates the command line syntax for the SQLIO.exe program output into a batch file.");
            Console.WriteLine();
            Console.WriteLine("Options:");
            p.WriteOptionDescriptions(Console.Out);
        }

        static void ShowHelp(OptionSet p)
        {
            Console.WriteLine("Usage: SQLIOCommandGenerator [OPTIONS]");
            Console.WriteLine("Generates the command line syntax for the SQLIO.exe program output into a batch file.");
            Console.WriteLine();
            Console.WriteLine("Options:");
            p.WriteOptionDescriptions(Console.Out);
        }
    }

    public static class GlobalVariables
    {
        //single instance vars not used in looping
        //public static string Threads { get; set; } //not used with the -f<paramfile> option
        public static string Seconds { get; set; }
        public static string CoolOffSeconds { get; set; }
        public static string SQLIORunFile { get; set; }

        //vars used to construct the loops
        public static string IOPattern { get; set; }
        public static string IOType { get; set; }
        public static string Buffering { get; set; }
        public static string OutstandingIOStart { get; set; }
        public static string OutstandingIOIncrament { get; set; }
        public static string OutstandingIOEnd { get; set; }
        public static string OutstandingIOList { get; set; }
        public static string IOSizeStart { get; set; }
        public static string IOSizeIncrament { get; set; }
        public static string IOSizeEnd { get; set; }
        public static string IOSizeList { get; set; }
    }
}
