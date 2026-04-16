using System;
using System.Collections.Generic;
using System.Text;

namespace Payzen.Domain.Common;

/// <summary>
/// Représente le résultat d'une opération métier qui retourne une valeur.
/// Évite les exceptions pour les erreurs attendues (validation, règle métier).
///
/// Exemple d'usage :
///   var result = await _payrollService.CalculateAsync(employeeId);
///   if (!result.IsSuccess) return BadRequest(result.Error);
///   return Ok(result.Value);
/// </summary>
public class Result<T>
{
    public bool IsSuccess { get; private set; }

    /// <summary>La valeur retournée si l'opération a réussi</summary>
    public T? Value { get; private set; }

    /// <summary>Message d'erreur si l'opération a échoué</summary>
    public string? Error { get; private set; }

    private Result(bool isSuccess, T? value, string? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    /// <summary>Crée un résultat de succès avec une valeur</summary>
    public static Result<T> Success(T value) => new(true, value, null);

    /// <summary>Crée un résultat d'échec avec un message d'erreur</summary>
    public static Result<T> Failure(string error) => new(false, default, error);
}

/// <summary>
/// Version sans valeur de retour pour les opérations "fire and forget"
/// comme créer, supprimer, mettre à jour.
/// </summary>
public class Result
{
    public bool IsSuccess { get; private set; }
    public string? Error { get; private set; }

    private Result(bool isSuccess, string? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, null);

    public static Result Failure(string error) => new(false, error);
}
