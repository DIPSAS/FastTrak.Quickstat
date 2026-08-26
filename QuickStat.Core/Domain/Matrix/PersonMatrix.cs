using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using QuickStat.Collectors;
using QuickStat.Domain.DataPoints;
using QuickStat.Domain.Patients;

namespace QuickStat.Domain.Matrix;

/// <summary>
/// The result dataset: people down, variables across. Owns its data.
/// </summary>
/// <remarks>
/// <para>
/// Delphi: <c>TPersonGridData</c> (<c>EPR.QA.Matrix.pas:35</c>), which was <em>not</em> a standalone
/// model - it delegated all cell storage to the grid control through
/// <c>IPersonGridComponent</c> and read cells back out of a sparse array keyed by the string
/// <c>"col:row"</c>. Inverting that is the single largest structural change in the port
/// (<c>Docs/Port/04-matrix-export.md</c> R-11): the matrix owns rows and columns, and the view is a
/// projection.
/// </para>
/// <para>
/// Implementing <see cref="ICollectorResultSink"/> is what makes the matrix the destination of a
/// collector run without either side knowing about the other's internals.
/// </para>
/// </remarks>
public sealed class PersonMatrix : ICollectorResultSink
{
    private readonly IDataPointFactory _dataPointFactory;
    private readonly ITitleDictionary _titles;
    private readonly List<MatrixRow> _rows = [];
    private readonly Dictionary<int, MatrixRow> _rowsByPersonId = [];
    private readonly List<MatrixColumn> _columns = [];
    private readonly Dictionary<string, int> _columnOrdinals = new(StringComparer.Ordinal);
    private MatrixSortOrder _sortBy;

    /// <summary>Creates an empty matrix with no captions, so every column is titled by its own name.</summary>
    /// <param name="dataPointFactory">Creates the cell values and resolves their display rules.</param>
    public PersonMatrix(IDataPointFactory dataPointFactory)
        : this(dataPointFactory, new CaptionDictionary())
    {
    }

    /// <summary>Creates an empty matrix.</summary>
    /// <param name="dataPointFactory">Creates the cell values and resolves their display rules.</param>
    /// <param name="titles">Resolves column headings; consulted once per column, at creation time.</param>
    public PersonMatrix(IDataPointFactory dataPointFactory, ITitleDictionary titles)
    {
        ArgumentNullException.ThrowIfNull(dataPointFactory);
        ArgumentNullException.ThrowIfNull(titles);

        _dataPointFactory = dataPointFactory;
        _titles = titles;
    }

    /// <summary>The rows, sorted and materialised.</summary>
    public IReadOnlyList<MatrixRow> Rows => _rows;

    /// <summary>The data columns, excluding the four fixed identity columns.</summary>
    public IReadOnlyList<MatrixColumn> Columns => _columns;

    /// <summary>Whether the matrix has been frozen by <see cref="Lock"/>.</summary>
    public bool IsLocked { get; private set; }

    /// <summary>
    /// Whether there is anything to export.
    /// </summary>
    /// <remarks>
    /// Delphi <c>HasData</c> counts <em>columns</em>, not rows - a population with people but no
    /// collected variables is "no data". Export must additionally require
    /// <see cref="IsLocked"/>: exporting an unlocked matrix writes the literal <c>(not ready)</c>
    /// into every single cell, and an empty population writes a phantom <c>"nil"</c> row.
    /// </remarks>
    public bool HasData => _columns.Count > 0;

    /// <summary>Study the data belongs to.</summary>
    public int StudyId { get; set; }

    /// <summary>
    /// Row order. QuickStat always sets <see cref="MatrixSortOrder.PersonId"/> before loading.
    /// </summary>
    /// <remarks>
    /// Changing this after <see cref="Lock"/> is an error - the Delphi raised
    /// <c>'Can not change sort order after locking'</c> (<c>EPR.QA.Matrix.pas:512-520</c>), which is
    /// why the assignment happens before the population is prepared.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The matrix is locked.</exception>
    public MatrixSortOrder SortBy
    {
        get => _sortBy;

        set
        {
            if (IsLocked)
            {
                throw new InvalidOperationException("Can not change sort order after locking.");
            }

            if (_sortBy == value)
            {
                return;
            }

            _sortBy = value;

            SortRows();
        }
    }

    /// <summary>
    /// Column-order policy handed to each <see cref="VariableNameSet"/>. Defaults to
    /// <see cref="ColumnOrder.FirstSeen"/>, which is value zero.
    /// </summary>
    public ColumnOrder ColumnOrder { get; set; }

    /// <summary>Creates a variable-name set configured with this matrix's <see cref="ColumnOrder"/>.</summary>
    /// <returns>An empty set.</returns>
    /// <remarks>
    /// A collector run accumulates into a set and then hands it to <see cref="AddColumns"/>, which
    /// reads it in whatever order the set presents. Building the set here is what makes the policy
    /// on this matrix take effect rather than being a field nobody reads.
    /// </remarks>
    public VariableNameSet CreateVariableNameSet() => new(ColumnOrder);

    /// <summary>Replaces the rows with a fresh cohort.</summary>
    /// <param name="patients">The loaded population.</param>
    /// <remarks>
    /// De-duplicates by <see cref="Patient.PersonId"/>, keeping the first occurrence, then sorts by
    /// <see cref="SortBy"/>. Both behaviours are inherited: the Delphi funnelled rows through a
    /// dictionary keyed on person id, so duplicates were silently dropped and any <c>ORDER BY</c>
    /// in the population procedure was discarded.
    /// </remarks>
    public void PreparePopulation(IEnumerable<Patient> patients)
    {
        ArgumentNullException.ThrowIfNull(patients);

        // The Delphi's PreparePopulation opens with a full Clear, so the columns go too.
        Clear();

        foreach (Patient patient in patients)
        {
            if (_rowsByPersonId.ContainsKey(patient.PersonId))
            {
                continue;
            }

            MatrixRow row = new()
            {
                PersonId = patient.PersonId,
                DateOfBirth = patient.DateOfBirth,
                FullName = patient.DisplayName,
                NationalId = patient.NationalId,
                GenderId = patient.GenderId,
                Sex = patient.Sex,
            };

            _rowsByPersonId.Add(row.PersonId, row);
            _rows.Add(row);
        }

        SortRows();
    }

    /// <summary>Drops the rows and unlocks.</summary>
    public void ClearPopulation()
    {
        _rows.Clear();
        _rowsByPersonId.Clear();

        IsLocked = false;
    }

    /// <summary>Drops the columns and every datapoint, keeping the rows, and unlocks.</summary>
    /// <remarks>
    /// <para>
    /// Called at the start of every collect run.
    /// </para>
    /// <para>
    /// It unlocks because <see cref="AddColumns"/> and <see cref="Add"/> both refuse to run against a
    /// locked matrix, and <see cref="Lock"/> is the last thing a collect run does. Without this, the
    /// second click of <i>Collect data</i> — the most prominent button on the Collections tab —
    /// threw <see cref="InvalidOperationException"/>, and the only way back was to reload the
    /// population and lose the cohort. Found during Phase 3 wave 2 by step 3.3, which had to guard
    /// against it in the view-model.
    /// </para>
    /// <para>
    /// The Delphi has no such restriction: <c>fLocked</c> appears in exactly three places in
    /// <c>EPR.QA.Matrix.pas</c> — set false in <c>ClearPopulation</c> (:214), set true in
    /// <c>StartPainting</c> (:332), and read by <c>GetCellText</c> (:236) for the <c>(not ready)</c>
    /// placeholder. It gates painting and export, never the adding of data, so re-collecting is just
    /// another run. The guards are a port-side addition worth keeping — they catch a genuine misuse,
    /// adding data to a finished matrix without clearing it first — but clearing the variables
    /// <i>is</i> that clearing, so it must lift the lock.
    /// </para>
    /// </remarks>
    public void ClearVariables()
    {
        _columns.Clear();
        _columnOrdinals.Clear();

        foreach (MatrixRow row in _rows)
        {
            row.ClearDataPoints();
        }

        IsLocked = false;
    }

    /// <summary>Drops everything.</summary>
    public void Clear()
    {
        ClearPopulation();
        ClearVariables();
    }

    /// <summary>Appends the columns one collector produced, in that collector's order.</summary>
    /// <param name="variableNames">
    /// From <see cref="CollectorRunSummary.VariableNames"/>. The order here becomes the column order
    /// of the grid and of every exported file.
    /// </param>
    /// <remarks>
    /// De-duplicates across collectors, which the Delphi did not: two collectors emitting the same
    /// variable produced two identical columns, because <c>ContainsVariable</c> existed and was
    /// never called (<c>EPR.QA.Matrix.Column.pas:83</c>).
    /// </remarks>
    /// <exception cref="InvalidOperationException">The matrix is locked.</exception>
    public void AddColumns(VariableNameSet variableNames)
    {
        ArgumentNullException.ThrowIfNull(variableNames);

        ThrowIfLocked(nameof(AddColumns));

        foreach (string variableName in variableNames)
        {
            if (_columnOrdinals.ContainsKey(variableName))
            {
                continue;
            }

            _columnOrdinals.Add(variableName, _columns.Count);

            _columns.Add(new MatrixColumn
            {
                VarName = variableName,
                Title = _titles.GetVarTitle(variableName),
                Description = _titles.GetVarDescription(variableName),
            });
        }
    }

    /// <summary>Freezes the matrix so it can be rendered and exported.</summary>
    /// <remarks>Idempotent, as the Delphi's is.</remarks>
    public void Lock() => IsLocked = true;

    /// <summary>The ordinal of a column, by name.</summary>
    /// <param name="varName">Column name, prefix included.</param>
    /// <param name="columnIndex">Index into <see cref="Columns"/>.</param>
    /// <returns><see langword="true"/> when the column exists.</returns>
    public bool TryGetColumnIndex(string varName, out int columnIndex)
    {
        ArgumentNullException.ThrowIfNull(varName);

        return _columnOrdinals.TryGetValue(varName, out columnIndex);
    }

    /// <summary>The row for a person, by id.</summary>
    /// <param name="personId">The person.</param>
    /// <param name="row">The row.</param>
    /// <returns><see langword="true"/> when the person is in the cohort.</returns>
    public bool TryGetRow(int personId, [NotNullWhen(true)] out MatrixRow? row) =>
        _rowsByPersonId.TryGetValue(personId, out row);

    /// <summary>Reads one cell's datapoint.</summary>
    /// <param name="rowIndex">Index into <see cref="Rows"/>.</param>
    /// <param name="columnIndex">Index into <see cref="Columns"/>.</param>
    /// <param name="dataPoint">The datapoint.</param>
    /// <returns><see langword="true"/> when the cell has a value.</returns>
    /// <remarks>
    /// Out-of-range indices return <see langword="false"/> rather than throwing, matching the
    /// Delphi, which wrapped the whole lookup in a swallowing <c>try</c>.
    /// </remarks>
    public bool TryGetDataPoint(int rowIndex, int columnIndex, [NotNullWhen(true)] out DataPoint? dataPoint)
    {
        if ((uint)rowIndex >= (uint)_rows.Count || (uint)columnIndex >= (uint)_columns.Count)
        {
            dataPoint = null;

            return false;
        }

        return _rows[rowIndex].TryGetDataPoint(_columns[columnIndex].VarName, out dataPoint);
    }

    /// <summary>Computes everything needed to render one cell.</summary>
    /// <param name="rowIndex">Index into <see cref="Rows"/>.</param>
    /// <param name="columnIndex">Index into <see cref="Columns"/>.</param>
    /// <returns>The cell.</returns>
    /// <remarks>
    /// <para>
    /// This is steps 3 to 6 of the Delphi's <c>HandleCellDraw</c>
    /// (<c>EPR.QA.GUI.Grid.Study.pas:144-245</c>), and nothing else: selection, current-row blending
    /// and the fixed-cell colour are the view's, because they depend on where the caret is rather
    /// than on the data.
    /// </para>
    /// <para>
    /// A cell with no datapoint carries the empty-cell colour and no text - the Delphi put the
    /// column object in that slot and rendered its always-empty <c>Subtitle</c>. A datapoint whose
    /// rule returns no colour falls back to white, which is the grid's step 4.
    /// </para>
    /// <para>
    /// Cells are computed the same way whether or not the matrix is
    /// <see cref="IsLocked"/>. The Delphi's <c>(not ready)</c> placeholder is an export-path
    /// artefact and the port guards the export command instead
    /// (<c>Docs/Port/04-matrix-export.md</c> R-10).
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Either index is outside the matrix.</exception>
    public MatrixCell GetCell(int rowIndex, int columnIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(rowIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(rowIndex, _rows.Count);
        ArgumentOutOfRangeException.ThrowIfNegative(columnIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(columnIndex, _columns.Count);

        if (!_rows[rowIndex].TryGetDataPoint(_columns[columnIndex].VarName, out DataPoint? dataPoint))
        {
            return new MatrixCell
            {
                Text = "",
                Background = RiskPalette.EmptyCell,
                HasValue = false,
            };
        }

        _ = _dataPointFactory.TryGetRule(dataPoint.VarName, out DataPointRule? rule);

        return Render(dataPoint, rule);
    }

    /// <summary>Computes one of the four fixed identity cells for a row.</summary>
    /// <param name="rowIndex">Index into <see cref="Rows"/>.</param>
    /// <param name="ordinal">
    /// One of <see cref="FixedColumns.PersonId"/>, <see cref="FixedColumns.DateOfBirth"/>,
    /// <see cref="FixedColumns.NationalId"/> or <see cref="FixedColumns.Name"/>.
    /// </param>
    /// <param name="formatProvider">
    /// Formats the person id and the date of birth; <see langword="null"/> means
    /// <see cref="CultureInfo.CurrentCulture"/>. The Delphi uses <c>DateToStr</c>, i.e. the locale
    /// short date, which is <em>not</em> the ISO format the timestamp columns use.
    /// </param>
    /// <returns>The cell.</returns>
    /// <remarks>
    /// Delphi <c>TPersonGrid.GetFixedFields</c> (<c>EPR.QA.GUI.Grid.pas:185-194</c>). Which of these
    /// a caller may ask for is decided by
    /// <see cref="FixedColumns.VisibleOrdinals"/>: the two anonymous modes omit three of them
    /// outright rather than rendering them blank.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The row index or the ordinal is out of range.</exception>
    public MatrixCell GetFixedCell(int rowIndex, int ordinal, IFormatProvider? formatProvider = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(rowIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(rowIndex, _rows.Count);
        ArgumentOutOfRangeException.ThrowIfNegative(ordinal);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(ordinal, FixedColumns.Count);

        CultureInfo culture = formatProvider as CultureInfo ?? CultureInfo.CurrentCulture;
        MatrixRow row = _rows[rowIndex];

        string text = ordinal switch
        {
            FixedColumns.PersonId => row.PersonId.ToString(culture),
            FixedColumns.DateOfBirth => row.DateOfBirth?.ToString("d", culture) ?? "",
            FixedColumns.NationalId => row.NationalId ?? "",
            _ => row.FullName,
        };

        return new MatrixCell
        {
            Text = text,
            AlignLeft = FixedColumns.IsTextColumn(ordinal),
            HasValue = true,
        };
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">The matrix is locked.</exception>
    public bool Add(string columnName, in CollectorResultRow row)
    {
        ArgumentException.ThrowIfNullOrEmpty(columnName);

        ThrowIfLocked(nameof(Add));

        if (!_rowsByPersonId.TryGetValue(row.PersonId, out MatrixRow? matrixRow))
        {
            // Expected in bulk: the PidBinding.None collectors scan the whole database and the
            // cohort filter happens right here.
            return false;
        }

        DataPoint dataPoint = _dataPointFactory.Create(columnName, row.Value, row.Timestamp, row.RowId);

        dataPoint.ItemId = row.ItemId;

        if (row.Caption is not null)
        {
            dataPoint.Caption = row.Caption;
        }

        return matrixRow.TryAddDataPoint(dataPoint);
    }

    private static MatrixCell Render(DataPoint dataPoint, DataPointRule? rule)
    {
        bool hasCaption = !string.IsNullOrEmpty(dataPoint.Caption);
        string text;

        if (hasCaption && (rule is null || rule.CaptionTakesPrecedence))
        {
            int length = rule?.CaptionLength ?? DataPointRule.DefaultCaptionLength;

            text = dataPoint.Caption!.Length <= length ? dataPoint.Caption : dataPoint.Caption[..length];
        }
        else if (rule?.FormatValue is not null)
        {
            text = rule.FormatValue(dataPoint.Value);
        }
        else
        {
            text = NumericFormat.G(dataPoint.Value);
        }

        // Step 4 of HandleCellDraw: a cell object that offers no colour is painted white, not left
        // unpainted.  Only the fourteen registered rules offer one at all.
        Rgb background = rule?.BrushColor?.Invoke(dataPoint.Value) ?? RiskPalette.NoRisk;

        bool alignLeft = rule?.SetsCaptionFromText == true
            || (hasCaption && (rule?.AlignLeftWhenCaptioned ?? true));

        return new MatrixCell
        {
            Text = text,
            Background = background,
            Foreground = rule?.FontColor?.Invoke(dataPoint.Value),
            AlignLeft = alignLeft,
            HasValue = true,
        };
    }

    private void ThrowIfLocked(string operation)
    {
        if (IsLocked)
        {
            throw new InvalidOperationException($"{operation} is not allowed after the matrix has been locked.");
        }
    }

    private void SortRows()
    {
        // Compare rather than subtract: the Delphi comparer returns Left.PersonId - Right.PersonId,
        // which overflows for ids far enough apart (EPR.QA.Matrix.Row.pas:239-242).
        Comparison<MatrixRow> comparison = _sortBy == MatrixSortOrder.ReverseName
            ? static (left, right) => string.CompareOrdinal(left.FullName, right.FullName)
            : static (left, right) => left.PersonId.CompareTo(right.PersonId);

        _rows.Sort(comparison);
    }
}
