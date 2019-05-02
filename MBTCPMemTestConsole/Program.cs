using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ModbusTCP;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace MBTCPMemTestConsole
{
    class Program
    {
        static ModbusTCPConnection[] MBTCPconnections;
        static IPAddress MBTCPServerIP;
        static byte sockNum;
        static short queryNum;
        static byte queryType;

        static void Main(string[] args)
        {
            if (args.Length < 1 || args.Length > 4)
            {
                Console.WriteLine("Неправильно указаны параметры запуска программы.");
                Console.WriteLine("<Файл_программы> <ip-адрес сервера> (кол-во потоков) (кол-во запросов) (чтение(0)/запись(1))");
                return;
            }

            try
            {
                MBTCPServerIP = IPAddress.Parse(args[0]);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Неправильно задан ip-адрес ModBusTCP-сервера.");
                return;
            }

            if (args.Length >= 2)
            {
                try
                {
                    sockNum = Convert.ToByte(args[1]);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Неверно задано количество потоков.");
                    return;
                }
            }
            else
                sockNum = 1;

            if (args.Length >= 3)
            {
                try
                {
                    queryNum = Convert.ToInt16(args[2]);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Неверно задано количество запросов.");
                    return;
                }
            }
            else
                queryNum = 1;

            if (args.Length == 4)
            {
                try
                {
                    queryType = Convert.ToByte(args[3]);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Неверно задан тип запросов.");
                    return;
                }
            }
            else
                queryType = 0;

            ModbusTCPConnection.QueryNum = queryNum;


            MBTCPconnections = new ModbusTCPConnection[sockNum];

            for (byte i = 0; i < sockNum; ++i)
            {
                MBTCPconnections[i] = new ModbusTCPConnection(MBTCPServerIP);                
                Thread t = new Thread(new ParameterizedThreadStart(ThreadMBTCPRor));
                t.Start(i);
            }
           
            Console.ReadLine();    
        }
        
        private static void ThreadMBTCPRor(object number)
        {
            //ReadHoldingRegs(UInt16 baseRegister, UInt16 number)
            byte[] ReadData;
            int index = Convert.ToInt16(number);
            UInt16 baseRegister = Convert.ToUInt16(index * 1000 + 11000);
            UInt16 quantity = 100;
            UInt16 TrID = baseRegister;
            UInt16 CheckValue;
            byte[] WriteData = new byte[200];
            for (byte i = 0; i < 200; ++i)
                WriteData[i] = i;

            int j = 0;

            for (; ; )
            {
                ++j;
                if (MBTCPconnections[index].GetState())
                {
                    if(queryType != 0)
                        ReadData = MBTCPconnections[index].PresetMultipleRegs(baseRegister, quantity, WriteData);
                    else
                        ReadData = MBTCPconnections[index].ReadHoldingRegs(baseRegister, quantity, TrID);
                    
                    if (ReadData != null)
                        Thread.Sleep(50);
                    else
                    {
                        Thread.Sleep(100);
                        DateTime dt = DateTime.Now;
                        //Console.WriteLine("Restarting socket connection...", dt, dt.Millisecond);
                        Console.WriteLine("Thread sleep for 100 milliseconds...", dt, dt.Millisecond);
                        //MBTCPconnections[index].Close();
                        //MBTCPconnections[index] = new ModbusTCPConnection(MBTCPServerIP);
                    }

                    /*
                    CheckValue = Convert.ToUInt16((ReadData[9] << 8) | ReadData[10]);
                    if (CheckValue != baseRegister)
                    {                      
                        DateTime dt = DateTime.Now;
                        Console.WriteLine("Value = {0} => baseRegister = {1}, trID = {2}", CheckValue, baseRegister, TrID);
                        Console.WriteLine("Error time: {0}.{1}", dt, dt.Millisecond);
                        //Thread.Sleep(10);
                    }*/
                }
                else
                {
                    Thread.Sleep(1000);
                    DateTime dt = DateTime.Now;
                    Console.WriteLine("Restarting socket connection...", dt, dt.Millisecond);
                    MBTCPconnections[index].Close();
                    MBTCPconnections[index] = new ModbusTCPConnection(MBTCPServerIP);
                }
            }
        }
    }
}
