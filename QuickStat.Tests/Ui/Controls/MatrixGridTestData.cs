using System.Globalization;
using QuickStat.Collectors;
using QuickStat.Domain.DataPoints;
using QuickStat.Domain.Matrix;
using QuickStat.Domain.Patients;

namespace QuickStat.Tests.Ui.Controls;

/// <summary>Matrices to render, built without a database or a UI.</summary>
/// <remarks>
/// <para>
/// Every builder here pins its culture explicitly. This machine is nb-NO, so an assertion about a
/// formatted date or number written without a culture passes locally and fails on an English build
/// agent (PORT-PLAN.md §5 Phase 3); <see cref="Culture"/> is what the grid's
/// <c>CellCulture</c> is set to in every test that looks at text.
/// </para>
/// <para>
/// The three colouring cases the painting tests need come from real ladders rather than invented
/// ones, so the fixtures cannot drift away from what the application shows.
/// </para>
/// </remarks>
internal static class MatrixGridTestData
{
    /// <summary>The variable whose ladder produces a visible cell colour.</summary>
    /// <remarks>
    /// Haemoglobin. Value 10 lands in the <c>&lt; 11</c> band, i.e.
    /// <see cref="RiskPalette.ModerateRisk"/> <c>#FFEDBF</c> - which is exactly the one coloured
    /// cell in <c>Docs/Screenshots/QuickStat bilde 3.png</c>, under <c>B-Hemo…</c>.
    /// </remarks>
    public const string ColouredVarName = StandardDataPointRules.HaemoglobinVarName;

    /// <summary>The value that makes <see cref="ColouredVarName"/> amber.</summary>
    public const double ModerateValue = 10;

    /// <summary>An English culture, so no assertion depends on the machine's locale.</summary>
    public static CultureInfo Culture { get; } = CultureInfo.GetCultureInfo("en-US");

    /// <summary>A Norwegian culture, for the tests that prove the culture is honoured at all.</summary>
    public static CultureInfo NorwegianCulture { get; } = CultureInfo.GetCultureInfo("nb-NO");

    /// <summary>A fixed timestamp; never <c>DateTime.Now</c>.</summary>
    public static DateTime Timestamp { get; } = new(2019, 8, 14, 0, 0, 0, DateTimeKind.Unspecified);

    /// <summary>
    /// Three people and three variables: one plain number, one amber value, one gap.
    /// </summary>
    /// <returns>A locked matrix.</returns>
    /// <remarks>
    /// Row 0 (person 8) has all three values, row 1 (person 13) has only <c>AGE</c>, and row 2
    /// (person 17) has none - so the empty-cell grey, the risk colour and an ordinary white cell are
    /// all on screen at once.
    /// </remarks>
    public static PersonMatrix SmallMatrix()
    {
        PersonMatrix matrix = NewMatrix();

        matrix.PreparePopulation([Person(8), Person(13), Person(17)]);

        matrix.Add("AGE", Row(8, "AGE", 97));
        matrix.Add("AGE", Row(13, "AGE", 95));
        matrix.Add(ColouredVarName, Row(8, ColouredVarName, ModerateValue));
        matrix.Add("SEX", Row(8, "SEX", 1));

        matrix.AddColumns(Names("AGE", ColouredVarName, "SEX"));
        matrix.Lock();

        return matrix;
    }

    /// <summary>A population with rows but no collected variables.</summary>
    /// <returns>An unlocked matrix with three rows and no columns.</returns>
    public static PersonMatrix RowsWithoutColumns()
    {
        PersonMatrix matrix = NewMatrix();

        matrix.PreparePopulation([Person(8), Person(13), Person(17)]);

        return matrix;
    }

    /// <summary>The documented worst case: 1500 people by 1000 variables, sparsely filled.</summary>
    /// <param name="rows">How many people.</param>
    /// <param name="columns">How many variables.</param>
    /// <param name="fillEvery">Give every n-th cell a value; the rest stay empty.</param>
    /// <returns>A locked matrix.</returns>
    public static PersonMatrix LargeMatrix(int rows = 1500, int columns = 1000, int fillEvery = 7)
    {
        PersonMatrix matrix = NewMatrix();
        List<Patient> patients = new(rows);

        for (int row = 0; row < rows; row++)
        {
            patients.Add(Person(row + 1));
        }

        matrix.PreparePopulation(patients);

        VariableNameSet names = matrix.CreateVariableNameSet();

        for (int column = 0; column < columns; column++)
        {
            string varName = string.Create(CultureInfo.InvariantCulture, $"VAR{column:D4}");

            names.Add(varName);

            for (int row = column % fillEvery; row < rows; row += fillEvery)
            {
                matrix.Add(varName, Row(row + 1, varName, (row * 10) + column));
            }
        }

        matrix.AddColumns(names);
        matrix.Lock();

        return matrix;
    }

    /// <summary>A matrix whose single column carries a description, for the header tooltip.</summary>
    /// <param name="varName">The variable.</param>
    /// <param name="title">The heading.</param>
    /// <param name="description">The description the header tooltip should show.</param>
    /// <returns>A locked matrix with one person and one column.</returns>
    public static PersonMatrix DescribedColumn(string varName, string title, string description)
    {
        CaptionDictionary captions = new();

        captions.AddCaption(new CaptionRecord
        {
            VarName = varName,
            Title = title,
            Description = description,
        });

        PersonMatrix matrix = new(new DataPointFactory(), captions);

        matrix.PreparePopulation([Person(8)]);
        matrix.Add(varName, Row(8, varName, 42));
        matrix.AddColumns(Names(varName));
        matrix.Lock();

        return matrix;
    }

    /// <summary>An empty matrix with the standard rules.</summary>
    /// <returns>The matrix.</returns>
    public static PersonMatrix NewMatrix() => new(new DataPointFactory(), new CaptionDictionary());

    /// <summary>One patient with a fixed date of birth and national id.</summary>
    /// <param name="personId">The person id, which is also the sort key.</param>
    /// <param name="last">Last name.</param>
    /// <param name="first">First name.</param>
    /// <returns>The patient.</returns>
    public static Patient Person(int personId, string last = "Hansen", string first = "Ola") => new()
    {
        PersonId = personId,
        LastName = last,
        FirstName = first,
        DateOfBirth = new DateTime(1922, 3, 12, 0, 0, 0, DateTimeKind.Unspecified),
        NationalId = "12032212345",
        GenderId = 1,
        Sex = Sex.Male,
    };

    /// <summary>One collector result row.</summary>
    /// <param name="personId">Whose value it is.</param>
    /// <param name="varName">Which variable.</param>
    /// <param name="value">The value.</param>
    /// <param name="caption">Optional free text, which makes the cell left-aligned and truncated.</param>
    /// <returns>The row.</returns>
    public static CollectorResultRow Row(int personId, string varName, double value, string? caption = null) => new()
    {
        PersonId = personId,
        VarName = varName,
        Value = value,
        Timestamp = Timestamp,
        RowId = personId * 1000,
        Caption = caption,
    };

    /// <summary>An ordered variable-name set.</summary>
    /// <param name="names">The names, in column order.</param>
    /// <returns>The set.</returns>
    public static VariableNameSet Names(params string[] names)
    {
        VariableNameSet set = new();

        foreach (string name in names)
        {
            set.Add(name);
        }

        return set;
    }
}
