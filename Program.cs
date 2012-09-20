using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using NKH.MindSqualls;

namespace ProjectX
{
    class Program
    {
        private static NxtBrick brick;
        static NxtMotor Xmotor;
        static NxtMotor Ymotor;
        static NxtMotor Zmotor;

        static float[] VertsX;
        static float[] VertsY;
        static float[] VertsZ;
        static int[] FacesA;
        static int[] FacesB;
        static int[] FacesC;
        static string objName;

        static float scale;

        static char MotorXaxis;
        static char MotorYaxis;
        static char MotorZaxis;
        static bool Xinverted = false;
        static bool Yinverted = false;
        static bool Zinverted = false;
        static int XRange1;
        static int XRange2;
        static int YRange1;
        static int YRange2;
        static int ZRange1;
        static int ZRange2;
        static int XrealRange;
        static int YrealRange;
        static int ZrealRange;
        static float XSegmentDeg;
        static float ZSegmentDeg;
        static float XObjUnitPerSeg;
        static float YObjUnitPerDeg;
        static float ZObjUnitPerSeg;
        static float Xpresc;
        static float Zpresc;

        static void Main(string[] args)
        {
            VertsX = new float[0];
            VertsY = new float[0];
            VertsZ = new float[0];
            FacesA = new int[0];
            FacesB = new int[0];
            FacesC = new int[0];
           

            Console.WriteLine("\n=== === ===\nWELCOME\n=== === ===");
            Console.WriteLine("Welcome to ProjectX 3D Printing Software!");
            Console.WriteLine("Press any Key to start print Configuration.");
            Console.ReadKey();
            Console.WriteLine("\n\n");

            Console.WriteLine("\n=== === ===\nCONNECTION SETUP\n=== === ===");
            Console.WriteLine("ProjectX will now detect your NXT Device.");
            Console.WriteLine("Type 'B' if you are using a Bluetooth Connection,");
            Console.WriteLine("or 'U' if you are using a USB Cable.");
            Console.WriteLine("Make sure your NXT is turned on and connected properly before continuing.");
            bool hasChosen = false;
            while (!hasChosen)
            {
                ConsoleKeyInfo key = Console.ReadKey();
                if (key.Key == ConsoleKey.B)
                {
                    hasChosen = true;
                    SetupBluetoothConnection();
                }
                if (key.Key == ConsoleKey.U)
                {
                    hasChosen = true;
                    SetupUSBConnection();
                }
            }

            //Verbindungskontrolle
            Console.WriteLine("Trying to connect to NXT ...");
            brick.Connect();
            int timeout = 5000;
            Stopwatch sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeout && !brick.IsConnected)
            {
                System.Threading.Thread.Sleep(100);
            }
            if (brick.IsConnected)
            {
                PingNXT();
            }
            else
            {
                Console.WriteLine("Connection Failed.");
            }

            //Einlesen Der .obj-Datei
            Console.WriteLine("\n=== === ===\nMODEL SELECTION\n=== === ===");
            Console.WriteLine("\n\nPlease give the Path of the 3D model you are willing to print. OBJ Format is supported.");
            string objPath = Console.ReadLine();
            StreamReader objReader = new StreamReader(objPath);
            DecodeObjFile(objReader);
            Console.WriteLine("Object with name " + objName + " found.");
            Console.WriteLine("A total of " + VertsX.Length + " Vertices and " + FacesA.Length + " faces were found.");

            //Kalibrierung der Motoren
            Console.WriteLine("\n=== === ===\nMOTOR CALIBRATION\n=== === ===");
            LinkMotorAxis();
            Calibrate();
            SetDimensions();

            //Einstellung der Auflösung
            Console.WriteLine("\n=== === ===\nRESOLUTION\n=== === ===");
            SetResolution();

            //Anpassen der modellgröße
            FindScale();

            //Drucken
            PreparePrint();
            Print();

            Console.ReadKey();
            brick.Disconnect();
        }

        static void DecodeObjFile(StreamReader objReader)
        {
            while (!objReader.EndOfStream)
            {
                string line = objReader.ReadLine();
                line = line.Replace('.', ',');
                string[] splitted = line.Split(' ');
                
                if (splitted[0] == "o")
                {
                    objName = splitted[1];
                    
                } else if (splitted[0] == "v")
                {
                    addToArray(ref VertsX, Convert.ToSingle(splitted[1]));
                    addToArray(ref VertsY, Convert.ToSingle(splitted[2]));
                    addToArray(ref VertsZ, Convert.ToSingle(splitted[3]));
                } else if (splitted[0] == "f") {
                    addToArray(ref FacesA, Convert.ToInt32(splitted[1]));
                    addToArray(ref FacesB, Convert.ToInt32(splitted[2]));
                    addToArray(ref FacesC, Convert.ToInt32(splitted[3]));
                }
                

            }
        }

        static void addToArray (ref float[] array, float newitem)
        {
            float[] tmpArray = new float[array.Length];
            tmpArray = array;
            array = new float[tmpArray.Length + 1];
            for (int i = 0; i < tmpArray.Length; i++)
               array[i] = tmpArray[i];
            array[array.Length - 1] = newitem;
        }
        static void addToArray(ref int[] array, int newitem)
        {
            int[] tmpArray = new int[array.Length];
            tmpArray = array;
            array = new int[tmpArray.Length + 1];
            for (int i = 0; i < tmpArray.Length; i++)
                array[i] = tmpArray[i];
            array[array.Length - 1] = newitem;
        }

        static void SetupBluetoothConnection()
        {
            Console.WriteLine("\n\nEnter your Bluetooth COM-Port (Default: 40)");
            byte comport = Convert.ToByte(Console.ReadLine());
            brick = new NxtBrick(NxtCommLinkType.Bluetooth, comport);
            Console.WriteLine("\nBluetooth connection set up at port " + comport + ".");
        }

        static void SetupUSBConnection()
        {
            brick = new NxtBrick(NxtCommLinkType.USB, 0);
            Console.WriteLine("\nUSB connection set up.");
        }

        static void PingNXT()
        {
            if (brick.IsConnected)
            {
                brick.PlayTone(300, 400); System.Threading.Thread.Sleep(100);
                brick.PlayTone(400, 400); System.Threading.Thread.Sleep(100);
                brick.PlayTone(500, 400); System.Threading.Thread.Sleep(100);
                brick.PlayTone(600, 400);
                Console.WriteLine("NXT module '" + brick.Name + "' is successfully connected.");
                NxtGetFirmwareVersionReply? reply = brick.CommLink.GetFirmwareVersion();
                if (reply.HasValue)
                {
                    Console.WriteLine(" Firmware Version: " + reply.Value.firmwareVersion);
                }
                Console.WriteLine(" Battery Level: " + brick.BatteryLevel);
            }
            else Console.WriteLine("No Connection.");
        }

        static void LinkMotorAxis()
        {
            Console.WriteLine("\nYou now have to specify which Motor represents which 3-dimensional Axis.");
            Console.WriteLine("Enter ('A'/'B'/'C') the Motor controlling X-Axis (left-right) movement:");
            MotorXaxis = Console.ReadKey().KeyChar;

            Console.WriteLine("\nEnter ('A'/'B'/'C') the Motor controlling Y-Axis (foward-backward) movement: (This should be the axis that pushes the drill foward)");
            MotorYaxis = Console.ReadKey().KeyChar;

            Console.WriteLine("\nEnter ('A'/'B'/'C') the Motor controlling Z-Axis (up-down) movement:");
            MotorZaxis = Console.ReadKey().KeyChar;

            Xmotor = new NxtMotor();
            Ymotor = new NxtMotor();
            Zmotor = new NxtMotor();

            switch (MotorXaxis)
            {
                case 'a':
                    brick.MotorA = Xmotor;
                    break;
                case 'b':
                    brick.MotorB = Xmotor;
                    break;
                case 'c':
                    brick.MotorC = Xmotor;
                    break;
            }

            switch (MotorYaxis)
            {
                case 'a':
                    brick.MotorA = Ymotor;
                    break;
                case 'b':
                    brick.MotorB = Ymotor;
                    break;
                case 'c':
                    brick.MotorC = Ymotor;
                    break;
            }

            switch (MotorZaxis)
            {
                case 'a':
                    brick.MotorA = Zmotor;
                    break;
                case 'b':
                    brick.MotorB = Zmotor;
                    break;
                case 'c':
                    brick.MotorC = Zmotor;
                    break;
            }
        }

        static char getAxis(char motor)
        {
            if (MotorXaxis == motor) return 'x';
            if (MotorYaxis == motor) return 'y';
            if (MotorZaxis == motor) return 'z';
            return '0';
        }

        static void Calibrate()
        {
            Console.WriteLine("\nProjectX now needs to know from where to where it can navigate on each axis.");

            //X
            Console.WriteLine("Motor " + MotorXaxis + ", controlling the X-Axis, will now drive to one end of its movement range. Be prepared to press any Key when the end is reached. Press any Key to start.");
            Console.ReadKey();
            Xmotor.Run(-10, 360000);
            Console.ReadKey();
            Xmotor.Brake();
            XRange1 = Xmotor.TachoCount.Value;
            Console.WriteLine("\nDo you wish this to be the positive or the negative end? ('p'/'n')");
            if (Console.ReadKey().KeyChar == 'p') Xinverted = true;
            Console.WriteLine("Motor " + MotorXaxis + " will now move to the opposite and. Press any key if you're ready.");
            Console.ReadKey();
            Xmotor.Run(10, 36000);
            Console.ReadKey();
            Xmotor.Brake();
            XRange2 = Xmotor.TachoCount.Value;


            //Y
            Console.WriteLine("\nMotor " + MotorYaxis + ", controlling the Y-Axis, will now drive to one end of its movement range. Be prepared to press any Key when the end is reached. Press any Key to start.");
            Console.ReadKey();
            Ymotor.Run(-10, 360000);
            Console.ReadKey();
            Ymotor.Brake();
            YRange1 = Ymotor.TachoCount.Value;
            Console.WriteLine("\nDo you wish this to be the positive or the negative end? ('p'/'n')");
            if (Console.ReadKey().KeyChar == 'p') Yinverted = true;
            Console.WriteLine("Motor " + MotorYaxis + " will now move to the opposite and. Press any key if you're ready.");
            Console.ReadKey();
            Ymotor.Run(10, 36000);
            Console.ReadKey();
            Ymotor.Brake();
            YRange2 = Ymotor.TachoCount.Value;


            //Z
            Console.WriteLine("\nMotor " + MotorZaxis + ", controlling the Z-Axis, will now drive to one end of its movement range. Be prepared to press any Key when the end is reached. Press any Key to start.");
            Console.ReadKey();
            Zmotor.Run(-10, 360000);
            Console.ReadKey();
            Zmotor.Brake();
            ZRange1 = Zmotor.TachoCount.Value;
            Console.WriteLine("\nDo you wish this to be the positive or the negative end? ('p'/'n')");
            if (Console.ReadKey().KeyChar == 'p') Zinverted = true;
            Console.WriteLine("Motor " + MotorZaxis + " will now move to the opposite and. Press any key if you're ready.");
            Console.ReadKey();
            Zmotor.Run(10, 36000);
            Console.ReadKey();
            Zmotor.Brake();
            ZRange2 = Zmotor.TachoCount.Value;

            if (Xinverted)
            {
                int i = XRange1;
                XRange1 = XRange2;
                XRange2 = i;
            }
            if (Yinverted)
            {
                int i = YRange1;
                YRange1 = YRange2;
                YRange2 = i;
            }
            if (Zinverted)
            {
                int i = ZRange1;
                ZRange1 = ZRange2;
                ZRange2 = i;
            }
        }

        static void SetDimensions()
        {
            Console.WriteLine("\nNow, please enter the Length of the movement range of Motor " + MotorXaxis + ". The Unit doesn't matter, the relation is important. Integer required.");
            XrealRange = Convert.ToInt32(Console.ReadLine());

            Console.WriteLine("\nPlease enter the Length of the movement range of Motor " + MotorYaxis + ".");
            YrealRange = Convert.ToInt32(Console.ReadLine());

            Console.WriteLine("\nPlease enter the Length of the movement range of Motor " + MotorZaxis + ".");
            ZrealRange = Convert.ToInt32(Console.ReadLine());
        }

        static void SetResolution()
        {
            Console.WriteLine("\nYou now have to define the precision of your Print.");
            Console.WriteLine("How many units do your want one printing step for Motor " + MotorXaxis + " to be? Float with decimal comma.");
            Xpresc = Convert.ToSingle(Console.ReadLine());
            Console.WriteLine("\nThe X-Axis, having a total length of " + XrealRange + " units, is devided into " + Math.Round(((float)XrealRange / Xpresc) - 0.5F) +
                " segments, each having a length of " + Xpresc + " units, requiring a rotation of " + Math.Abs(XRange2 - XRange1) +
                " degrees for Motor " + MotorXaxis + ".");

            Console.WriteLine("How many units do your want one printing step for Motor " + MotorZaxis + " to be? Float with decimal comma.");
            Zpresc = Convert.ToSingle(Console.ReadLine());
            Console.WriteLine("\nThe Z-Axis, having a total length of " + ZrealRange + " units, is devided into " + Math.Round(((float)ZrealRange / Zpresc) - 0.5F) +
                " segments, each having a length of " + Zpresc + " units, requiring a rotation of " + Math.Abs(ZRange2 - ZRange1) +
                " degrees for Motor " + MotorZaxis + ".");

            Console.WriteLine("\nThe Y-Axis will be as precise as possible, there's no resolution needed.");

            XSegmentDeg = Math.Abs(XRange2 - XRange1);
            ZSegmentDeg = Math.Abs(ZRange2 - ZRange1);
        }

        static void FindScale()
        {
            Console.Write("Scaling Object to fit dimensions ... ");

            float SmXVal = VertsX[0];
            for (int i = 0; i < VertsX.Length; i++) if (VertsX[i] < SmXVal) SmXVal = VertsX[i];
            float BgXVal = VertsX[0];
            for (int i = 0; i < VertsX.Length; i++) if (VertsX[i] > BgXVal) SmXVal = VertsX[i];

            float SmYVal = VertsY[0];
            for (int i = 0; i < VertsY.Length; i++) if (VertsY[i] < SmYVal) SmYVal = VertsY[i];
            float BgYVal = VertsX[0];
            for (int i = 0; i < VertsY.Length; i++) if (VertsY[i] > BgYVal) SmYVal = VertsY[i];

            float SmZVal = VertsZ[0];
            for (int i = 0; i < VertsZ.Length; i++) if (VertsZ[i] < SmZVal) SmZVal = VertsZ[i];
            float BgZVal = VertsZ[0];
            for (int i = 0; i < VertsZ.Length; i++) if (VertsZ[i] > BgZVal) SmZVal = VertsZ[i];


            float xRange = BgXVal - SmXVal;
            float yRange = BgYVal - SmYVal;
            float zRange = BgZVal - SmZVal;

            scale = XrealRange / xRange;
            if (YrealRange / yRange < scale) scale = YrealRange / yRange;
            if (ZrealRange / zRange < scale) scale = ZrealRange / zRange;
            //Eine .obj Einheit = Scale * Unit

            XObjUnitPerSeg = scale / Xpresc;    //so viele OBJ-Einheiten entsprechen einem Segment
            ZObjUnitPerSeg = scale / Zpresc;

            YObjUnitPerDeg = (YrealRange * scale) / Math.Abs(YRange2 - YRange1);

            Console.Write("DONE\n");
        }

        static void PreparePrint()
        {
            Console.WriteLine("\n\nCongratulations, all Configuration is done. \nThe Motors now move towards their Origin.\nWhen in position, place the raw material block precisely on the Printing Area.\nWhen ready, press any Key.");
            if(!Xinverted) Xmotor.Run(-30, ((uint)Math.Abs(XRange2-XRange1)));
            if (!Yinverted) Ymotor.Run(-30, ((uint)Math.Abs(YRange2 - YRange1)));
            if (!Zinverted) Zmotor.Run(-30, ((uint)Math.Abs(ZRange2 - ZRange1)));
            Console.ReadKey();
            Console.WriteLine("\nNext Keypress is point of no return. If religious, pray now.");
            Console.ReadKey();
        }

        static void Print()
        { }
    }
}
