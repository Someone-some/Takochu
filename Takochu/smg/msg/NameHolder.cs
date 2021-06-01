﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Takochu.fmt;
using Takochu.io;
using Takochu.util;

namespace Takochu.smg.msg
{
    public class NameHolder
    {
        private static string[] sPossibleLangs = { "UsEnglish", "EuEnglish", "JpJapanese", "KrKorean" };
        public static void Initialize()
        {
            if (GameUtil.IsSMG1())
            {
                foreach (string lang in sPossibleLangs)
                {
                    if (Program.sGame.mFilesystem.DoesDirectoryExist($"/{lang}"))
                    {
                        mFilesystem = new RARCFilesystem(Program.sGame.mFilesystem.OpenFile($"/{lang}/MessageData/Message.arc"));
                        Program.lang = lang;
                    }
                }
                
                mMessages = new BMG(mFilesystem.OpenFile("/message/message.bmg"));
                mMessageTable = new Dictionary<string, int>();

                BCSV tbl = new BCSV(mFilesystem.OpenFile("/message/messageid.tbl"));

                foreach(BCSV.Entry e in tbl.mEntries)
                {
                    mMessageTable.Add(e.Get<string>("MessageId"), e.Get<int>("Index"));
                }

                tbl.Close();
            }
            else
            {
                foreach (string lang in sPossibleLangs)
                {
                    if (Program.sGame.mFilesystem.DoesDirectoryExist($"/LocalizeData/{lang}"))
                    {
                        mFilesystem = new RARCFilesystem(Program.sGame.mFilesystem.OpenFile($"/LocalizeData/{lang}/MessageData/SystemMessage.arc"));
                        Program.lang = lang;
                    }
                }

                mGalaxyNames = new MSBT(mFilesystem.OpenFile("/boop/GalaxyName.msbt"));
                mScenarioNames = new MSBT(mFilesystem.OpenFile("/boop/ScenarioName.msbt"));
            }
        }

        public static bool DoesMsgTblContain(string zone)
        {
            foreach(KeyValuePair<string, int> pair in mMessageTable)
            {
                if (pair.Key.StartsWith(zone))
                    return true;
            }

            return false;
        }

        public static Dictionary<string, List<MessageBase>> GetAllMessagesInZone(string zone)
        {
            Dictionary<string, List<MessageBase>> msgs = new Dictionary<string, List<MessageBase>>();

            foreach (KeyValuePair<string, int> pair in mMessageTable)
            {
                if (pair.Key.StartsWith(zone))
                    msgs.Add(pair.Key, mMessages.GetMessageAtIdx(pair.Value));
            }

            return msgs;
        }

        public static bool HasGalaxyName(string galaxy)
        {
            bool ret;

            if (GameUtil.IsSMG1())
            {
                ret = mMessageTable.ContainsKey($"GalaxyName_{galaxy}");
            }
            else
            {
                if (mGalaxyNames == null)
                    return false;

                ret = mGalaxyNames.HasGalaxyName(galaxy);
            }

            return ret;
        }

        public static string GetGalaxyName(string galaxy)
        {
            string ret;

            string name = $"GalaxyName_{galaxy}";

            if (GameUtil.IsSMG1())
            {
                int idx = mMessageTable[name];
                ret = mMessages.GetStringAtIdx(idx);
            }
            else
            {
                ret = mGalaxyNames.GetStringFromLabelNoTag(name);
            }

            return ret;
        }

        public static string GetScenarioName(string galaxy, int scenarioNo)
        {
            string ret;
            string name = $"ScenarioName_{galaxy}{scenarioNo}";

            if (GameUtil.IsSMG1())
            {
                if (!mMessageTable.ContainsKey(name))
                    ret = "";
                else
                {
                    int idx = mMessageTable[name];
                    ret = mMessages.GetStringAtIdx(idx);
                }
            }
            else
            {
                ret = mScenarioNames.GetStringFromLabelNoTag(name);
            }

            return ret;
        }

        public static void Close()
        {
            if (GameUtil.IsSMG2())
            {
                mGalaxyNames.Close();
                mScenarioNames.Close();
                mFilesystem.Close();
            }
        }

        public static void Save()
        {
            mGalaxyNames.Save();
            mScenarioNames.Save();
            mFilesystem.Save();
        }

        private static RARCFilesystem mFilesystem;
        private static MSBT mGalaxyNames;
        private static MSBT mScenarioNames;

        private static BMG mMessages;

        private static Dictionary<string, int> mMessageTable;
    }
}
