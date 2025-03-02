﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Takochu.fmt;
using Takochu.smg;
using Takochu.smg.msg;
using Takochu.util;

namespace Takochu
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            BCSV.PopulateHashTable();
            CameraUtil.InitCameras();
            ObjectDB.Load();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainWindow());
        }

        public static Translator sTranslator;
        public static string lang;
        public static Game sGame;
    }
}
