using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace TTXEquipamentos.Utilities
{
    public static class DateCalculationHelper
    {
        private static readonly CultureInfo PortugueseCulture = CultureInfo.GetCultureInfo("pt-BR");

        public static string FormatDate(DateTime date)
        {
            return date.ToString("dd/MM/yyyy", PortugueseCulture);
        }

        public static string FormatDateTime(DateTime dateTime)
        {
            return dateTime.ToString("dd/MM/yyyy HH:mm", PortugueseCulture);
        }

        public static DateTime ParseDate(string dateString)
        {
            if (DateTime.TryParse(dateString, PortugueseCulture, System.Globalization.DateTimeStyles.None, out var result))
            {
                return result;
            }
            throw new FormatException($"Invalid date format: {dateString}");
        }

        /// <summary>
        /// Calculate work hours between two dates, considering Mon-Fri 07:30-17:30, excluding weekends
        /// </summary>
        public static double CalculateWorkHours(DateTime start, DateTime end)
        {
            if (start > end) return 0;

            double totalHours = 0;
            var current = start;

            while (current <= end)
            {
                var dayOfWeek = current.DayOfWeek;
                
                if (dayOfWeek != DayOfWeek.Saturday && dayOfWeek != DayOfWeek.Sunday)
                {
                    DateTime dayStart = current.Date.AddHours(7.5); // 07:30
                    DateTime dayEnd = current.Date.AddHours(17.5);  // 17:30

                    if (dayOfWeek == DayOfWeek.Friday)
                    {
                        dayEnd = current.Date.AddHours(16.5);  // 16:30 on Friday
                    }

                    DateTime effectiveStart = current > dayStart ? current : dayStart;
                    DateTime effectiveEnd = end < dayEnd ? end : dayEnd;

                    if (effectiveStart < effectiveEnd)
                    {
                        totalHours += (effectiveEnd - effectiveStart).TotalHours;
                    }
                }

                current = current.AddDays(1);
                if (current > end) break;
            }

            return totalHours;
        }

        public static int GetWeekNumber(DateTime date)
        {
            return PortugueseCulture.Calendar.GetWeekOfYear(date, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }

        public static string GetMonthName(int month)
        {
            return PortugueseCulture.DateTimeFormat.GetMonthName(month);
        }
    }

    public static class FileHelper
    {
        public static bool EnsureDirectoryExists(string directoryPath)
        {
            try
            {
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool FileExists(string filePath)
        {
            try
            {
                return File.Exists(filePath);
            }
            catch
            {
                return false;
            }
        }

        public static bool DeleteFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static List<string> GetFilesInDirectory(string directoryPath, string pattern = "*.*")
        {
            try
            {
                if (!Directory.Exists(directoryPath))
                    return new List<string>();

                var files = Directory.GetFiles(directoryPath, pattern);
                return files.ToList();
            }
            catch
            {
                return new List<string>();
            }
        }

        public static long GetDirectorySize(string directoryPath)
        {
            try
            {
                if (!Directory.Exists(directoryPath))
                    return 0;

                var directoryInfo = new DirectoryInfo(directoryPath);
                return directoryInfo.EnumerateFiles("*", SearchOption.AllDirectories)
                    .Sum(f => f.Length);
            }
            catch
            {
                return 0;
            }
        }
    }

    public static class CurrencyHelper
    {
        public static string FormatCurrency(double value)
        {
            var cultureInfo = CultureInfo.GetCultureInfo("pt-BR");
            return value.ToString("C", cultureInfo);
        }

        public static double ParseCurrency(string value)
        {
            var cultureInfo = CultureInfo.GetCultureInfo("pt-BR");
            if (double.TryParse(value, NumberStyles.Currency, cultureInfo, out var result))
            {
                return result;
            }
            throw new FormatException($"Invalid currency format: {value}");
        }
    }

    public static class ValidationHelper
    {
        public static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsNotEmpty(string? value)
        {
            return !string.IsNullOrWhiteSpace(value);
        }

        public static bool IsPositiveNumber(double value)
        {
            return value > 0;
        }
    }
}
