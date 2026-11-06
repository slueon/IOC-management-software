using IOC.Core.Contracts.Session;

namespace IOC.Infrastructure.Session;

public class OperatorSessionService : IOperatorSessionService
{
    private readonly object _lock = new();
    private OperatorProfile _currentOperator;
    private readonly List<OperatorProfile> _availableOperators;

    public event Action<OperatorProfile>? CurrentOperatorChanged;

    public OperatorProfile CurrentOperator
    {
        get
        {
            lock (_lock)
            {
                return _currentOperator;
            }
        }
    }

    public IReadOnlyList<OperatorProfile> AvailableOperators
    {
        get
        {
            lock (_lock)
            {
                return _availableOperators.AsReadOnly();
            }
        }
    }

    public OperatorSessionService()
    {
        _availableOperators = new List<OperatorProfile>
        {
            new()
            {
                OperatorId = "OP-8821",
                FullName = "Nguyễn Văn An",
                Role = "Trưởng Ca Trực Ban",
                ShiftName = "Ca Trực 24/7 (Đội A)",
                BadgeId = "IOC-CARD-8821",
                LoginTime = DateTime.UtcNow
            },
            new()
            {
                OperatorId = "OP-8822",
                FullName = "Trần Thị Mai",
                Role = "Kỹ Sư Giám Sát CCTV & An Ninh",
                ShiftName = "Ca Ngày (06:00 - 18:00)",
                BadgeId = "IOC-CARD-8822",
                LoginTime = DateTime.UtcNow
            },
            new()
            {
                OperatorId = "OP-8823",
                FullName = "Lê Hoàng Nam",
                Role = "Kỹ Sư Cơ Điện BMS & PCCC",
                ShiftName = "Ca Đêm (18:00 - 06:00)",
                BadgeId = "IOC-CARD-8823",
                LoginTime = DateTime.UtcNow
            },
            new()
            {
                OperatorId = "ADMIN-01",
                FullName = "Hệ Thống Quản Trị IOC",
                Role = "Quản Trị Viên Cấp Cao",
                ShiftName = "Toàn Thời Gian",
                BadgeId = "IOC-ADMIN-01",
                LoginTime = DateTime.UtcNow
            }
        };

        _currentOperator = _availableOperators[0];
    }

    public void SetCurrentOperator(string operatorId)
    {
        lock (_lock)
        {
            var match = _availableOperators.FirstOrDefault(o => o.OperatorId.Equals(operatorId, StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                _currentOperator = match;
                CurrentOperatorChanged?.Invoke(_currentOperator);
            }
        }
    }

    public void SetCurrentOperator(OperatorProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        lock (_lock)
        {
            _currentOperator = profile;
            if (!_availableOperators.Any(o => o.OperatorId == profile.OperatorId))
            {
                _availableOperators.Add(profile);
            }
            CurrentOperatorChanged?.Invoke(_currentOperator);
        }
    }
}
