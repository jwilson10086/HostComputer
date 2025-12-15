using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HostComputer.Common.Base
{
    public static class Log
    {
        public static void Info(string message)
        {
            App.Logger?.Info(message);
        }

        public static void Debug(string message) => App.Logger?.Debug(message);

        public static void Error(string message) => App.Logger?.Error(message);

        public static void Warn(string message) => App.Logger?.Warning(message);

        public static void Fatal(string message) => App.Logger?.Fatal(message);

        //public static void Operation(string message) => App.Logger?.Operation(message);

        public static void Thread(string message) => App.Logger?.ThreadLog(message);

        //public static void System(string message) => App.Logger?.SystemLog(message);
    }
}
