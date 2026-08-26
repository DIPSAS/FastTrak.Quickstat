using System.Globalization;
using QuickStat.Configuration;

namespace QuickStat.Data;

/// <summary>
/// Turns a <see cref="SqlRequest"/> into a <see cref="BoundSqlCommand"/>, applying every rule the
/// Delphi did not have.
/// </summary>
/// <remarks>
/// <para>
/// <c>PrepareQueryParameters</c> (<c>Emetra.Database.Simple.pas:415-433</c>) looped to
/// <c>fQuery.Parameters.Count</c> rather than to the length of the supplied open array, so too few
/// values read past the end of the array with no diagnostic at all. Every rule below exists to turn
/// one of those silent failures into a named exception.
/// </para>
/// </remarks>
internal static class SqlRequestBinder
{
    public static BoundSqlCommand Bind(SqlRequest request, ISqlTextRewriter rewriter, SqlOptions options)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(rewriter);
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(request.CommandText))
        {
            throw new SqlParameterBindingException("The request carries no statement.");
        }

        if (request.NamedValues is not null && request.Values.Count > 0)
        {
            throw new SqlParameterBindingException(
                "A request binds either positionally through Values or by name through NamedValues, never both.");
        }

        RewrittenSql rewritten = rewriter.Rewrite(request.CommandText);

        IReadOnlyList<SqlTableParameter> tableParameters = request.TableParameters;
        HashSet<string> tableNames = new(StringComparer.OrdinalIgnoreCase);

        foreach (SqlTableParameter table in tableParameters)
        {
            if (string.IsNullOrWhiteSpace(table.Name))
            {
                throw new SqlParameterBindingException("A table-valued parameter must have a name.");
            }

            if (!tableNames.Add(table.Name))
            {
                throw new SqlParameterBindingException($"Table-valued parameter '{table.Name}' is supplied twice.")
                {
                    ParameterName = table.Name,
                };
            }
        }

        // A table-valued placeholder is bound from TableParameters, so it must not consume one of
        // the positional values or be demanded from NamedValues.
        List<string> scalarNames = [.. rewritten.ParameterNames.Where(name => !tableNames.Contains(name))];

        List<BoundParameter> bound = new(scalarNames.Count);

        if (request.NamedValues is not null)
        {
            Dictionary<string, object?> lookup = new(StringComparer.OrdinalIgnoreCase);

            foreach (KeyValuePair<string, object?> pair in request.NamedValues)
            {
                lookup[pair.Key] = pair.Value;
            }

            foreach (string name in scalarNames)
            {
                if (!lookup.TryGetValue(name, out object? value))
                {
                    throw new SqlParameterBindingException(
                        $"No value was supplied for placeholder ':{name}'.")
                    {
                        ParameterName = name,
                    };
                }

                bound.Add(new BoundParameter(name, Normalise(value)));
            }
        }
        else
        {
            if (rewritten.HasRepeatedPlaceholder && scalarNames.Count > 0)
            {
                throw new SqlParameterBindingException(
                    "This statement repeats a placeholder, so 'the n-th value goes to the n-th placeholder' is " +
                    "ambiguous. Bind it by name through SqlRequest.NamedValues.");
            }

            if (request.Values.Count != scalarNames.Count)
            {
                throw new SqlParameterBindingException(string.Format(
                    CultureInfo.InvariantCulture,
                    "The statement has {0} placeholder(s) ({1}) but {2} value(s) were supplied.",
                    scalarNames.Count,
                    scalarNames.Count == 0 ? "none" : string.Join(", ", scalarNames.Select(n => ":" + n)),
                    request.Values.Count));
            }

            for (int i = 0; i < scalarNames.Count; i++)
            {
                bound.Add(new BoundParameter(scalarNames[i], Normalise(request.Values[i])));
            }
        }

        return new BoundSqlCommand
        {
            CommandText = rewritten.CommandText,
            Parameters = bound,
            TableParameters = tableParameters,
            CommandTimeout = request.CommandTimeout ?? options.DefaultCommandTimeout,
            Label = request.Label,
        };
    }

    /// <summary>
    /// Collapses <see cref="DBNull"/> onto <see langword="null"/> so that the rest of the pipeline -
    /// and every test - has exactly one representation of "no value".
    /// </summary>
    private static object? Normalise(object? value) => value is DBNull ? null : value;
}
