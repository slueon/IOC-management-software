using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using IOC.Core.Enums;
using IOC.Core.Models.AccessControl;
using IOC.Core.Models.FireAlarm;
using IOC.Core.Models.LanTel;
using IOC.Core.Models.PublicAddress;

namespace IOC.App.Converters;

public class SeverityToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is AlarmSeverity severity)
        {
            return severity switch
            {
                AlarmSeverity.Critical => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E53E3E")),
                AlarmSeverity.Major => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DD6B20")),
                AlarmSeverity.Minor => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D69E2E")),
                AlarmSeverity.Info => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3182CE")),
                _ => new SolidColorBrush(Colors.Gray)
            };
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class StatusToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DeviceStatus status)
        {
            return status switch
            {
                DeviceStatus.Online => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#38A169")),
                DeviceStatus.Warning => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D69E2E")),
                DeviceStatus.Critical => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E53E3E")),
                DeviceStatus.Offline => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#718096")),
                DeviceStatus.Maintenance => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4A5568")),
                _ => new SolidColorBrush(Colors.Gray)
            };
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class BoolToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool b = value is bool boolVal && boolVal;
        if (Invert) b = !b;
        return b ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class BooleanToStatusTextConverter : IValueConverter
{
    public string TrueText { get; set; } = "HOẠT ĐỘNG";
    public string FalseText { get; set; } = "SỰ CỐ";

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool b = value is bool boolVal && boolVal;
        if (parameter is string s && s.Contains('|'))
        {
            var parts = s.Split('|');
            return b ? parts[0] : parts[1];
        }
        return b ? TrueText : FalseText;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class BooleanToBrushConverter : IValueConverter
{
    public Brush TrueBrush { get; set; } = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#38A169"));
    public Brush FalseBrush { get; set; } = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E53E3E"));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int intVal)
        {
            return intVal == 0 ? TrueBrush : FalseBrush;
        }

        bool b = value is bool boolVal && boolVal;
        return b ? TrueBrush : FalseBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class PercentageToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double d)
        {
            if (d >= 80.0) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
            if (d >= 60.0) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class TemperatureToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double temp)
        {
            if (temp >= 70.0) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
            if (temp >= 55.0) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class ExtensionStatusToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is PbxExtensionStatus status)
        {
            return status switch
            {
                PbxExtensionStatus.Registered => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")),
                PbxExtensionStatus.InCall => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6")),
                PbxExtensionStatus.Ringing => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B")),
                PbxExtensionStatus.Disconnected => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7280")),
                _ => new SolidColorBrush(Colors.Gray)
            };
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class SpeakerZoneFaultToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is SpeakerZoneFaultType fault)
        {
            return fault switch
            {
                SpeakerZoneFaultType.Normal => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")),
                SpeakerZoneFaultType.OpenCircuit => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444")),
                SpeakerZoneFaultType.ShortCircuit => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626")),
                SpeakerZoneFaultType.Degraded => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B")),
                _ => new SolidColorBrush(Colors.Gray)
            };
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class SpeakerZoneFaultToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is SpeakerZoneFaultType fault)
        {
            return fault switch
            {
                SpeakerZoneFaultType.Normal => "Bình thường",
                SpeakerZoneFaultType.OpenCircuit => "ĐỨT TUYẾN DÂY",
                SpeakerZoneFaultType.ShortCircuit => "CHẬP TẢI DÂY LOA",
                SpeakerZoneFaultType.Degraded => "LỆCH TRỞ KHÁNG",
                _ => "Không rõ"
            };
        }
        return "Bình thường";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class BatteryVoltageToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double volts)
        {
            if (volts < 21.0) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
            if (volts < 23.5) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class PowerLoadToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double percent)
        {
            if (percent >= 90.0) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
            if (percent >= 75.0) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class RaidStatusToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string? status = value?.ToString();
        return status switch
        {
            "Normal" or "Optimal" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")),
            "Degraded" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B")),
            "Failed" or "Rebuilding" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444")),
            _ => new SolidColorBrush(Colors.Gray)
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class DoorStatusToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DoorPointInfo door)
        {
            if (!door.IsConnected) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B"));
            if (door.IsForcedOpen) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"));
            if (door.IsHeldOpen) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D97706"));
            if (door.IsOpen) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
            if (door.IsLocked) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6"));
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class DoorStatusToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DoorPointInfo door)
        {
            if (!door.IsConnected) return "MẤT KẾT NỐI";
            if (door.IsForcedOpen) return "BỊ CẠY PHÁ";
            if (door.IsHeldOpen) return "CHÈN GIỮ";
            if (door.IsOpen) return "ĐANG MỞ";
            if (door.IsLocked) return "ĐANG KHÓA";
            return "MỞ KHÓA";
        }
        return "BÌNH THƯỜNG";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class BatteryPercentToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double percent)
        {
            if (percent >= 50.0) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
            if (percent >= 20.0) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class AlarmBoolToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int count)
        {
            return count > 0
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
        }
        bool isAlarm = value is bool b && b;
        return isAlarm
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"))
            : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class GasPpmToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double ppm)
        {
            if (ppm >= 50.0) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
            if (ppm >= 25.0) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class WaterLevelPercentToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double lvl)
        {
            if (lvl <= 20.0) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
            if (lvl <= 35.0) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class FireAlarmStatusToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int count)
        {
            return count > 0
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
        }
        if (value is bool isAlarm)
        {
            return isAlarm
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class FireAlarmTypeToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is FireAlarmSensorType type)
        {
            return type switch
            {
                FireAlarmSensorType.SmokeDetector => "💨",
                FireAlarmSensorType.HeatDetector => "🌡️",
                FireAlarmSensorType.ManualCallPoint => "🚨",
                _ => "🔥"
            };
        }
        return "🔥";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class FireLoopStatusToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int faultCount)
        {
            return faultCount > 0
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
        }
        if (value is bool isFault)
        {
            return isFault
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class TabSelectedToBrushConverter : IValueConverter
{
    public Brush ActiveBrush { get; set; } = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7E22CE"));
    public Brush InactiveBrush { get; set; } = Brushes.Transparent;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value != null && parameter != null)
        {
            if (value.ToString() == parameter.ToString())
                return ActiveBrush;
        }
        return InactiveBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class TabSelectedToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value != null && parameter != null)
        {
            if (value.ToString() == parameter.ToString())
                return Visibility.Visible;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}


public class PositiveCountToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int count)
        {
            return count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class SeverityToBorderBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is AlarmSeverity severity)
        {
            return severity switch
            {
                AlarmSeverity.Critical => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626")),
                AlarmSeverity.Major => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EA580C")),
                AlarmSeverity.Minor => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D97706")),
                _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"))
            };
        }
        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1"));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class SeverityToBackgroundPillConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is AlarmSeverity severity)
        {
            return severity switch
            {
                AlarmSeverity.Critical => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEE2E2")),
                AlarmSeverity.Major => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFEDD5")),
                AlarmSeverity.Minor => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEF3C7")),
                _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ECFDF5"))
            };
        }
        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9"));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class SeverityToSymbolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is AlarmSeverity severity)
        {
            return severity switch
            {
                AlarmSeverity.Critical => "[!]",
                AlarmSeverity.Major => "[▲]",
                AlarmSeverity.Minor => "[i]",
                _ => "[✓]"
            };
        }
        return "[?]";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}


public class AcknowledgementStatusToBackgroundBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isAck)
        {
            return isAck
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ECFDF5"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEE2E2"));
        }
        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEE2E2"));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class AcknowledgementStatusToBorderBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isAck)
        {
            return isAck
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
        }
        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class AcknowledgementStatusToTextBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isAck)
        {
            return isAck
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#065F46"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#991B1B"));
        }
        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#991B1B"));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class CountToBadgeStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int count)
        {
            return count > 99 ? "99+" : count.ToString();
        }
        return "0";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class BooleanToKioskTooltipConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true ? "Thoát Toàn Màn Hình (F11)" : "Toàn Màn Hình / Video Wall (F11)";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class BooleanToMuteTooltipConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true ? "Bật Lại Còi Cảnh Báo (Đang Tắt)" : "Tắt Còi Cảnh Báo (Đang Bật)";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class BooleanToMuteButtonBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEE2E2"))
            : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC"));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class BooleanToMuteButtonBorderConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"))
            : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1"));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class BooleanToMuteIconBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"))
            : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569"));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class BooleanToKioskButtonBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EDE9FE"))
            : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC"));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class BooleanToKioskButtonBorderConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7C3AED"))
            : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1"));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class BooleanToKioskIconBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B21A8"))
            : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569"));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}


public class CollapsedBadgeVisibilityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length >= 2 && values[0] is bool isExpanded && values[1] is int count)
        {
            return (!isExpanded && count > 0) ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class ExpandedBadgeVisibilityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length >= 2 && values[0] is bool isExpanded && values[1] is int count)
        {
            return (isExpanded && count > 0) ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class BooleanToDarkThemeBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0B0F19"))
            : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F3F9"));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class BooleanToHeaderThemeBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B"))
            : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class BooleanToHeaderTextColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC"))
            : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A"));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
