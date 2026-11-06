namespace IOC.Core.Contracts.Session;

public interface IOperatorSessionService
{
    OperatorProfile CurrentOperator { get; }
    IReadOnlyList<OperatorProfile> AvailableOperators { get; }
    void SetCurrentOperator(string operatorId);
    void SetCurrentOperator(OperatorProfile profile);
    event Action<OperatorProfile>? CurrentOperatorChanged;
}
