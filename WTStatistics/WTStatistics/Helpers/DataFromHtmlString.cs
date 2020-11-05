﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using WTStatistics.Models;
using Xamarin.Forms;

namespace WTStatistics.Helpers
{
    class DataFromHtmlString
    {
        #region List of collection

        Player player;
        List<string> listTableMain;
        List<string> listTableAvia;
        List<string> listTableTanks;
        List<string> listTableShips;
        List<string> listTableVehicle;
        List<string> squadron;
        List<string> signUpDate;
        #endregion

        #region Constructor

        public DataFromHtmlString(string datastring)
        {
            player = new Player();
            listTableMain = GetTableData(datastring, "AllStatTable-TableData");
            listTableAvia = GetTableData(datastring, "AviationTable-TableData");
            listTableTanks = GetTableData(datastring, "GroundTable-TableData");
            listTableShips = GetTableData(datastring, "FleetTable-TableData");
            listTableVehicle = GetTableData(datastring, "GeoStatTable-TableData");
            squadron = GetTableData(datastring, "community/claninfo");
            signUpDate = GetTableData(datastring, "regDate");
        }
        #endregion

        #region Methods

        // Put info from html table to list
        private List<string> GetTableData(string htmlString, string tableName)
        {
            var listString = new List<string>();
            string[] splittedString = null;

            switch (Device.RuntimePlatform)
            {
                case Device.Android:
                    splittedString = htmlString.Split(new string[] { "u003" }, StringSplitOptions.None);
                    break;
                case Device.iOS:
                    splittedString = htmlString.Split(new string[] { "<" }, StringSplitOptions.None);
                    break;
            }

            foreach (var s in splittedString)
            {
                switch (Device.RuntimePlatform)
                {
                    case Device.Android:
                        if (s.Contains(tableName))
                        {
                            var trimStart = s.Substring(s.IndexOf('>') + 1);
                            var trimEnd = trimStart.Substring(0, trimStart.Length - 1);
                            listString.Add(trimEnd);
                        }
                        break;
                    case Device.iOS:
                        if (s.Contains(tableName))
                        {
                            var trimStart = s.Substring(s.IndexOf('>') + 1);
                            var trimEnd = trimStart.Substring(0, trimStart.Length - 0);
                            listString.Add(trimEnd);
                        }
                        break;
                }
            }
            return listString;
        }


        //  Return Int32 value from string with null check
        private int ToInt(string subjectString)
        {
            var stringWithLetter = string.Concat(subjectString.Where(Char.IsDigit));
            if (!string.IsNullOrEmpty(stringWithLetter))
            {
                return Convert.ToInt32(stringWithLetter);
            }
            else
            {
                return 0;
            }
        }

        // Calculate Kill/Battle ratio
        private double KBCalc(int kills, int battles)
        {
            double KD = battles > 0 ? (double)kills / (double)battles : 0;
            return Math.Round(KD, 1);
        }

        // Calculate total skill for modes where more 100 battles + winrate
        private double CalculateTotalSkill()
        {
            List<double> efficiency = new List<double>();
            if (ToInt(listTableAvia[5]) > 100)
            {
                efficiency.Add(player.KD_AAB);
            }
            if (ToInt(listTableAvia[6]) > 100)
            {
                efficiency.Add(player.KD_ARB);
            }
            if (ToInt(listTableAvia[7]) > 100)
            {
                efficiency.Add(player.KD_ASB);
            }
            if (ToInt(listTableTanks[5]) > 100)
            {
                efficiency.Add(player.KD_TAB);
            }
            if (ToInt(listTableTanks[6]) > 100)
            {
                efficiency.Add(player.KD_TRB);
            }
            if (ToInt(listTableTanks[7]) > 100)
            {
                efficiency.Add(player.KD_TSB);
            }
            if (ToInt(listTableShips[5]) > 100)
            {
                efficiency.Add(player.KD_SAB);
            }
            if (ToInt(listTableShips[6]) > 100)
            {
                efficiency.Add(player.KD_SRB);
            }

            efficiency.Add((double)player.WinRateAB * 2 / 100);
            efficiency.Add((double)player.WinRateRB * 2 / 100);
            efficiency.Add((double)player.WinRateSB * 2 / 100);

            return efficiency.Average();
        }

        private string SetAvatar()
        {
            string result = string.Empty;
            var air = player.CountAAB + player.CountARB + player.CountASB;
            var tank = player.CountTAB + player.CountTRB + player.CountTSB;
            var fleet = player.CountSAB + player.CountSRB;

            if (air >= tank & air >= fleet)
            {
                result = "pilot";
            }
            else
            if (tank >= air & tank >= fleet)
            {
                result = "tankman";
            }
            else
            if (fleet >= air & fleet >= tank)
            {
                result = "sailor";
            }
            return result;
        }

        private string SetHashTag()
        {
            return "";
        }

        //Set gradient color and skill label
        private void SetSkill(double skill)
        {
            if (skill >= 0 & skill <= 0.6)
            {
                player.SkillGradient = "grad_red.png";
                player.SkillDescription = "Bad player";
            }
            if (skill > 0.6 & skill <= 0.9)
            {
                player.SkillGradient = "grad_yellow.png";
                player.SkillDescription = "Average player";
            }
            if (skill > 0.9 & skill <= 1.1)
            {
                player.SkillGradient = "grad_green.png";
                player.SkillDescription = "Good player";
            }
            if (skill > 1.1 & skill <= 1.5)
            {
                player.SkillGradient = "grad_blue.png";
                player.SkillDescription = "Excellent player";
            }
            if (skill > 1.5)
            {
                player.SkillGradient = "grad_violet.png";
                player.SkillDescription = "Outstanding player";
            }
        }

        private string ConvToM(string convertedValue)
        {
            double num = Convert.ToDouble(convertedValue);
            string converted = string.Empty;
            if (num > 1000000)
            {
                converted = Math.Round(num / 1000000, 1) + " M";
            }
            else
            {
                converted = num.ToString();
            }
            return converted;
        }

        // Set players data to model instance
        public Player Info()
        {
            DateConverter date = new DateConverter();
            var battleFinished = (ToInt(listTableMain[9]) + ToInt(listTableMain[10]) + ToInt(listTableMain[11])).ToString();
            var totalTime = date.GetSpendTime(listTableMain[29]) + date.GetSpendTime(listTableMain[30]) + date.GetSpendTime(listTableMain[31]);
            var lionEarned = (ToInt(listTableMain[21]) + ToInt(listTableMain[22]) + ToInt(listTableMain[23])).ToString();

            player.BattleFinished = battleFinished;
            player.TotalTimeSpended = Math.Truncate(totalTime) + " h";
            player.LionEarned = ConvToM(lionEarned);
            player.SignUpDate = signUpDate[0].Substring(18);
            player.Squadron = squadron[0];

            player.WinRateAB = ToInt(listTableMain[13]);
            player.WinRateRB = ToInt(listTableMain[14]);
            player.WinRateSB = ToInt(listTableMain[15]);

            player.CountAAB = ToInt(listTableAvia[5]);
            player.CountARB = ToInt(listTableAvia[6]);
            player.CountASB = ToInt(listTableAvia[7]);
            player.CountTAB = ToInt(listTableTanks[5]);
            player.CountTRB = ToInt(listTableTanks[6]);
            player.CountTSB = ToInt(listTableTanks[7]);
            player.CountSAB = ToInt(listTableShips[5]);
            player.CountSRB = ToInt(listTableShips[6]);

            player.KD_AAB = KBCalc(ToInt(listTableAvia[41]), player.CountAAB);
            player.KD_ARB = KBCalc(ToInt(listTableAvia[42]), player.CountARB);
            player.KD_ASB = KBCalc(ToInt(listTableAvia[43]), player.CountASB);
            player.KD_TAB = KBCalc(ToInt(listTableTanks[53]) + ToInt(listTableTanks[49]), player.CountTAB);
            player.KD_TRB = KBCalc(ToInt(listTableTanks[54]) + ToInt(listTableTanks[50]), player.CountTRB);
            player.KD_TSB = KBCalc(ToInt(listTableTanks[55]) + ToInt(listTableTanks[51]), player.CountTSB);
            player.KD_SAB = KBCalc(ToInt(listTableShips[81]), player.CountSAB);
            player.KD_SRB = KBCalc(ToInt(listTableShips[82]), player.CountSRB);

            var skill = CalculateTotalSkill();
            SetSkill(skill);
            player.Avatar = SetAvatar();
            player.HashTag = SetHashTag();

            return player;
        }
        #endregion
    }
}