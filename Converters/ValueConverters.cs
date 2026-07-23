using System.Globalization;
using System.Windows;
using System.Windows.Data;
using TTXEquipamentos.Models;
using System.Text.Json;
using System.Collections;
using System.Collections.Generic;
using System;

namespace TTXEquipamentos.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool invert = parameter?.ToString()?.Equals("invert", StringComparison.OrdinalIgnoreCase) ?? false;
            bool isVisible = false;

            if (value is bool b)
                isVisible = b;
            else if (value is int count)
                isVisible = count > 0;
            else if (value is string s)
                isVisible = !string.IsNullOrEmpty(s);
            else if (value is double d)
                isVisible = d > 0;
            else if (value is float f)
                isVisible = f > 0;
            else if (value is decimal dec)
                isVisible = dec > 0;
            else if (value != null)
                isVisible = true;

            if (invert) isVisible = !isVisible;
            return isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool invert = parameter?.ToString()?.Equals("invert", StringComparison.OrdinalIgnoreCase) ?? false;
            if (value is Visibility v)
            {
                var result = v == Visibility.Visible;
                return invert ? !result : result;
            }
            return invert ? false : true;
        }
    }

    public class RoleToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string role && parameter is string allowedRoles)
            {
                var roles = allowedRoles.Split(',');
                return roles.Contains(role) ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class EnumToDisplayNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return string.Empty;
            var enumValue = value.ToString();
            if (enumValue == null) return string.Empty;
            return enumValue.Replace("_", " ");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DateTimeToShortDateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dt)
                return dt.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("pt-BR"));
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (DateTime.TryParse(value?.ToString(), CultureInfo.GetCultureInfo("pt-BR"), DateTimeStyles.None, out var result))
                return result;
            return null;
        }
    }

    public class InitialsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string name && !string.IsNullOrWhiteSpace(name))
            {
                var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                    return $"{parts[0][0]}{parts[1][0]}".ToUpper();
                return name[0].ToString().ToUpper();
            }
            return "?";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }

    public class ZeroToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count) return count == 0 ? Visibility.Visible : Visibility.Collapsed;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }

    public class InverseBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return !b ? Visibility.Visible : Visibility.Collapsed;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility v)
                return v == Visibility.Collapsed;
            return false;
        }
    }

    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }

    public class CountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count) return count > 0 ? Visibility.Visible : Visibility.Collapsed;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }

    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !string.IsNullOrWhiteSpace(value?.ToString()) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }

    public class TypeToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // Oculta o equipamento para instalações (Type = "instalacao")
            if (values.Length > 0 && values[0] is string type && type == "instalacao")
                return Visibility.Collapsed;
            return Visibility.Visible;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }

    public class NatureDisplayConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // Se Nature é "outro", retorna o CustomNature
            // caso contrário, retorna a Nature
            if (values.Length >= 2)
            {
                var nature = values[0]?.ToString();
                var customNature = values[1]?.ToString();

                if (nature?.Equals("outro", StringComparison.OrdinalIgnoreCase) == true)
                    return !string.IsNullOrWhiteSpace(customNature) ? customNature : "outro";
                
                return nature ?? string.Empty;
            }
            return string.Empty;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }

    public class MachineDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Machine machine)
                return $"{machine.Tag} - {machine.Name}";
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString() ?? string.Empty;
        }
    }

    public class MachineTextDisplayConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // values[0] = SelectedMachine (Machine)
            // values[1] = MachineSearchText (string)
            if (values.Length >= 1 && values[0] is Machine machine && machine != null)
            {
                return $"{machine.Tag} - {machine.Name}";
            }
            return values.Length >= 2 ? (values[1]?.ToString() ?? string.Empty) : string.Empty;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            // Quando o usuário digita, retorna o texto para MachineSearchText
            return new object[] { null, value?.ToString() ?? string.Empty };
        }
    }

    // Retorna somente os itens do checklist cujo Status == ChecklistStatus.ok
    public class ChecklistOkItemsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is IEnumerable items)
            {
                var list = new List<object>();
                foreach (var it in items)
                {
                    // Strongly-typed ChecklistItem
                    if (it is ChecklistItem ci)
                    {
                        if (ci.Status == ChecklistStatus.ok) list.Add(ci);
                        continue;
                    }

                    // JsonElement coming from non-normalized JSON
                    if (it is JsonElement je)
                    {
                        if (je.ValueKind == JsonValueKind.Object)
                        {
                            string status = null;
                            if (je.TryGetProperty("status", out var ps) && ps.ValueKind == JsonValueKind.String)
                                status = ps.GetString();

                            if (!string.IsNullOrEmpty(status) && string.Equals(status, "ok", StringComparison.OrdinalIgnoreCase))
                            {
                                var desc = je.TryGetProperty("description", out var pd) && pd.ValueKind == JsonValueKind.String ? pd.GetString()
                                           : (je.TryGetProperty("item", out var pi) && pi.ValueKind == JsonValueKind.String ? pi.GetString() : null);
                                var id = je.TryGetProperty("id", out var pid) && pid.ValueKind == JsonValueKind.String ? pid.GetString() : Guid.NewGuid().ToString();
                                var notes = je.TryGetProperty("notes", out var pn) && pn.ValueKind == JsonValueKind.String ? pn.GetString() : null;
                                list.Add(new ChecklistItem { Id = id, Description = desc, Status = ChecklistStatus.ok, Notes = notes });
                            }
                            else if (je.ValueKind == JsonValueKind.String)
                            {
                                // treat string as description and include
                                list.Add(new ChecklistItem { Id = Guid.NewGuid().ToString(), Description = je.GetString(), Status = ChecklistStatus.ok });
                            }
                        }
                        else if (je.ValueKind == JsonValueKind.String)
                        {
                            list.Add(new ChecklistItem { Id = Guid.NewGuid().ToString(), Description = je.GetString(), Status = ChecklistStatus.ok });
                        }
                        continue;
                    }

                    // IDictionary<string, object> (deserialized loosely)
                    if (it is IDictionary<string, object> dict)
                    {
                        dict.TryGetValue("status", out var sObj);
                        var status = sObj?.ToString();
                        if (!string.IsNullOrEmpty(status) && string.Equals(status, "ok", StringComparison.OrdinalIgnoreCase))
                        {
                            dict.TryGetValue("description", out var d);
                            dict.TryGetValue("id", out var idv);
                            dict.TryGetValue("notes", out var n);
                            list.Add(new ChecklistItem { Id = idv?.ToString() ?? Guid.NewGuid().ToString(), Description = d?.ToString(), Status = ChecklistStatus.ok, Notes = n?.ToString() });
                        }
                        continue;
                    }

                    // plain string
                    if (it is string s)
                    {
                        list.Add(new ChecklistItem { Id = Guid.NewGuid().ToString(), Description = s, Status = ChecklistStatus.ok });
                        continue;
                    }
                }
                return list;
            }
            return Array.Empty<object>();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    // Visível somente quando houver pelo menos um item do checklist com Status == ok
    public class ChecklistOkVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is IEnumerable items)
            {
                foreach (var it in items)
                {
                    if (it is ChecklistItem ci && ci.Status == ChecklistStatus.ok)
                        return Visibility.Visible;

                    if (it is JsonElement je)
                    {
                        if (je.ValueKind == JsonValueKind.Object && je.TryGetProperty("status", out var ps) && ps.ValueKind == JsonValueKind.String && string.Equals(ps.GetString(), "ok", StringComparison.OrdinalIgnoreCase))
                            return Visibility.Visible;
                        if (je.ValueKind == JsonValueKind.String)
                            return Visibility.Visible; // treat as present
                    }

                    if (it is IDictionary<string, object> dict)
                    {
                        if (dict.TryGetValue("status", out var sval) && sval?.ToString()?.Equals("ok", StringComparison.OrdinalIgnoreCase) == true)
                            return Visibility.Visible;
                        if (dict.TryGetValue("description", out var dval) && dval != null)
                            return Visibility.Visible;
                    }

                    if (it is string)
                        return Visibility.Visible;
                }
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class HistoricChecklistVisibilityConverter : IValueConverter
    {
        private static readonly DateTime CutoffDate = new DateTime(2026, 6, 1);

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MaintenanceRecord record)
            {
                if (record.CreatedAt >= CutoffDate)
                    return Visibility.Collapsed;

                return HasVisibleChecklistItems(record.ChecklistItems) ? Visibility.Visible : Visibility.Collapsed;
            }

            return Visibility.Collapsed;
        }

        private bool HasVisibleChecklistItems(IEnumerable? items)
        {
            if (items == null)
                return false;

            foreach (var it in items)
            {
                if (it is ChecklistItem ci && ci.Status == ChecklistStatus.ok)
                    return true;

                if (it is JsonElement je)
                {
                    if (je.ValueKind == JsonValueKind.Object && je.TryGetProperty("status", out var ps) && ps.ValueKind == JsonValueKind.String && string.Equals(ps.GetString(), "ok", StringComparison.OrdinalIgnoreCase))
                        return true;
                    if (je.ValueKind == JsonValueKind.String)
                        return true;
                }

                if (it is IDictionary<string, object> dict)
                {
                    if (dict.TryGetValue("status", out var sval) && sval?.ToString()?.Equals("ok", StringComparison.OrdinalIgnoreCase) == true)
                        return true;
                    if (dict.TryGetValue("description", out var dval) && dval != null)
                        return true;
                }

                if (it is string)
                    return true;
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
