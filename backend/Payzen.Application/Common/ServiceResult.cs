namespace Payzen.Application.Common;

/// <summary>
/// Résultat standard d'une opération de service.
/// Utilisé par tous les services Application pour retourner succès/erreur + données.
/// Pattern: if (!result.Success) return BadRequest(result.Errors); return Ok(result.Data);
/// </summary>
public class ServiceResult<T>
{
    public bool Success { get; private set; }
    public T? Data { get; private set; }
    public List<string> Errors { get; private set; } = new();
    public string? Error => Errors.Count > 0 ? Errors[0] : null;

    private ServiceResult() { }

    public static ServiceResult<T> Ok(T data) => new() { Success = true, Data = data };

    public static ServiceResult<T> Fail(string error) => new()
    {
        Success = false,
        Errors = new List<string> { error }
    };

    public static ServiceResult<T> Fail(IEnumerable<string> errors) => new()
    {
        Success = false,
        Errors = errors.ToList()
    };
}

/// <summary>
/// Version sans données de retour (Create, Delete, Update sans retour).
/// </summary>
public class ServiceResult
{
    public bool Success { get; private set; }
    public List<string> Errors { get; private set; } = new();
    public string? Error => Errors.Count > 0 ? Errors[0] : null;

    private ServiceResult() { }

    public static ServiceResult Ok() => new() { Success = true };

    public static ServiceResult Fail(string error) => new()
    {
        Success = false,
        Errors = new List<string> { error }
    };

    public static ServiceResult Fail(IEnumerable<string> errors) => new()
    {
        Success = false,
        Errors = errors.ToList()
    };
}